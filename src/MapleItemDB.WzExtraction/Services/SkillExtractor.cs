using System.Drawing.Imaging;
using System.Text.Json;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.WzExtraction.Helpers;
using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// 从 Skill.wz 提取技能数据
/// </summary>
public class SkillExtractor
{
    private readonly IReadOnlyDictionary<int, StringPoolBuilder.StringEntry> _skillStringPool;

    public SkillExtractor(IReadOnlyDictionary<int, StringPoolBuilder.StringEntry> skillStringPool)
    {
        _skillStringPool = skillStringPool;
    }

    /// <summary>
    /// 从 WZ 根节点提取所有技能
    /// </summary>
    public List<SkillEntity> Extract(
        Wz_Node wzRoot,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken ct = default)
    {
        var result = new List<SkillEntity>();
        var skillNode = wzRoot.Nodes["Skill"];
        if (skillNode == null) return result;

        // 收集所有 jobId.img 节点
        var jobImgNodes = new List<Wz_Node>();
        foreach (var child in skillNode.Nodes)
        {
            // 技能 img 格式: {jobId}.img (纯数字)
            var name = child.Text;
            if (name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                name = name[..^4];
            else if (child.Value is not Wz_Image)
                continue;

            if (int.TryParse(name, out _))
                jobImgNodes.Add(child);
        }

        int total = jobImgNodes.Count;
        int current = 0;
        var now = DateTime.UtcNow;

        foreach (var jobImgNode in jobImgNodes)
        {
            ct.ThrowIfCancellationRequested();

            var jobName = jobImgNode.Text;
            if (jobName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                jobName = jobName[..^4];
            if (!int.TryParse(jobName, out var jobId))
                continue;

            var extracted = jobImgNode.ExtractAndGetNode();
            if (extracted == null)
            {
                current++;
                continue;
            }

            var skillContainer = extracted.Nodes["skill"];
            if (skillContainer == null)
            {
                current++;
                continue;
            }

            foreach (var skillIdNode in skillContainer.Nodes)
            {
                if (!int.TryParse(skillIdNode.Text, out var skillId))
                    continue;

                // 从技能字符串池获取名称，跳过无名技能
                if (!_skillStringPool.TryGetValue(skillId, out var entry) || string.IsNullOrEmpty(entry.Name))
                    continue;

                // 优先从 common 子节点读取 maxLevel
                var commonNode = skillIdNode.Nodes["common"];
                var maxLevel = commonNode?.GetIntValue("maxLevel")
                    ?? skillIdNode.GetIntValue("maxLevel")
                    ?? 0;
                var invisible = skillIdNode.GetBoolValue("invisible");

                // 提取图标
                byte[]? iconData = null;
                var iconNode = skillIdNode.Nodes["icon"];
                if (iconNode != null)
                    iconData = ExportPngNode(iconNode);

                // 提取 common 属性公式 (字符串形式, 用于 Calculator 计算)
                string? commonPropsJson = null;
                if (commonNode != null)
                    commonPropsJson = ExtractCommonProps(commonNode);

                // 提取各等级效果 (采样: 用于无 common 的 pre-BB 技能)
                string? levelEffectsJson = null;
                var levelNode = skillIdNode.Nodes["level"];
                if (levelNode != null)
                    levelEffectsJson = ExtractLevelEffects(levelNode, maxLevel);

                result.Add(new SkillEntity
                {
                    SkillId = skillId,
                    Name = entry.Name,
                    Description = entry.Description,
                    JobId = jobId,
                    MaxLevel = maxLevel,
                    IconData = iconData,
                    IsHidden = invisible,
                    LevelEffectsJson = levelEffectsJson,
                    SkillH = entry.SkillH,
                    CommonPropsJson = commonPropsJson,
                    ExtractedAt = now,
                });
            }

            current++;
            if (current % 10 == 0 || current == total)
                progress?.Report(new ExtractionProgress("提取技能", current, total, $"已处理 {current}/{total} 个职业"));
        }

        return result;
    }

    /// <summary>
    /// 提取 common 子节点中的属性公式 (保留字符串形式供 Calculator 求值)
    /// </summary>
    private static string? ExtractCommonProps(Wz_Node commonNode)
    {
        var props = new Dictionary<string, string>();
        foreach (var propNode in commonNode.Nodes)
        {
            var val = propNode.Value;
            if (val != null)
                props[propNode.Text] = val.ToString()!;
        }
        return props.Count > 0 ? JsonSerializer.Serialize(props) : null;
    }

    /// <summary>
    /// 提取 level 子节点中各等级的数值属性，序列化为 JSON
    /// </summary>
    private static string? ExtractLevelEffects(Wz_Node levelNode, int maxLevel)
    {
        // 只提取前几个和最大等级以控制数据量
        var sampled = new Dictionary<string, Dictionary<string, object>>();
        var sampleLevels = GetSampleLevels(maxLevel);

        foreach (var lvNode in levelNode.Nodes)
        {
            if (!int.TryParse(lvNode.Text, out var level))
                continue;

            if (!sampleLevels.Contains(level))
                continue;

            var props = new Dictionary<string, object>();
            foreach (var propNode in lvNode.Nodes)
            {
                try
                {
                    var val = propNode.Value;
                    if (val is int i) props[propNode.Text] = i;
                    else if (val is long l) props[propNode.Text] = l;
                    else if (val is float f) props[propNode.Text] = f;
                    else if (val is double d) props[propNode.Text] = d;
                    else if (val is short s) props[propNode.Text] = (int)s;
                    else if (val is string str) props[propNode.Text] = str;
                }
                catch
                {
                    // 跳过无法解析的属性
                }
            }

            if (props.Count > 0)
                sampled[level.ToString()] = props;
        }

        return sampled.Count > 0 ? JsonSerializer.Serialize(sampled) : null;
    }

    /// <summary>
    /// 采样等级: 1、中间、最大
    /// </summary>
    private static HashSet<int> GetSampleLevels(int maxLevel)
    {
        var levels = new HashSet<int> { 1 };
        if (maxLevel > 0)
        {
            levels.Add(maxLevel);
            if (maxLevel > 2)
                levels.Add(maxLevel / 2);
        }
        return levels;
    }

    private static byte[]? ExportPngNode(Wz_Node? iconNode)
    {
        using var bitmap = iconNode.ResolvePng();
        if (bitmap == null) return null;

        try
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
