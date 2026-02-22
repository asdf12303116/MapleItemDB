using MapleItemDB.Core.Models;
using MapleItemDB.WzExtraction.Helpers;
using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// 从 Etc/SetItemInfo.img 提取套装信息
/// </summary>
public class SetItemExtractor
{
    private readonly IReadOnlyDictionary<int, StringPoolBuilder.StringEntry>? _stringPool;

    public SetItemExtractor(IReadOnlyDictionary<int, StringPoolBuilder.StringEntry>? stringPool = null)
    {
        _stringPool = stringPool;
    }

    /// <summary>
    /// 从 WZ 根节点提取所有套装信息
    /// </summary>
    public Dictionary<int, SetItemInfo> Extract(Wz_Node wzRoot)
    {
        var result = new Dictionary<int, SetItemInfo>();

        // 定位 Etc/SetItemInfo.img
        var etcNode = wzRoot.Nodes["Etc"];
        if (etcNode == null) return result;

        Wz_Node? setItemImgNode = null;
        foreach (var child in etcNode.Nodes)
        {
            if (child.Text == "SetItemInfo.img" || child.Text == "SetItemInfo")
            {
                setItemImgNode = child;
                break;
            }
        }

        if (setItemImgNode == null) return result;

        // 解压 img
        var extracted = setItemImgNode.ExtractAndGetNode();
        if (extracted == null) return result;

        // 获取 ItemOption.img 节点 (用于解析 Option 引用)
        var itemOptionNode = FindItemOptionNode(wzRoot);

        // 遍历套装: 每个子节点是一个套装 (键为套装ID)
        foreach (var setNode in extracted.Nodes)
        {
            if (!int.TryParse(setNode.Text, out var setId))
                continue;

            var info = ParseSetItem(setId, setNode, itemOptionNode);
            if (info != null)
                result[setId] = info;
        }

        return result;
    }

    /// <summary>
    /// 定位 Item/ItemOption.img 节点
    /// </summary>
    private static Wz_Node? FindItemOptionNode(Wz_Node wzRoot)
    {
        var itemNode = wzRoot.Nodes["Item"];
        if (itemNode == null) return null;

        foreach (var child in itemNode.Nodes)
        {
            if (child.Text is "ItemOption.img" or "ItemOption")
            {
                return child.ExtractAndGetNode();
            }
        }
        return null;
    }

    private SetItemInfo? ParseSetItem(int setId, Wz_Node setNode, Wz_Node? itemOptionNode)
    {
        var info = new SetItemInfo
        {
            SetItemId = setId,
            SetItemName = setNode.GetStringValue("setItemName") ?? $"套装-{setId}",
            CompleteCount = setNode.GetIntValue("completeCount") ?? 0,
        };

        // 解析部件
        var itemIDNode = setNode.Nodes["ItemID"];
        if (itemIDNode != null)
        {
            foreach (var partNode in itemIDNode.Nodes)
            {
                if (!int.TryParse(partNode.Text, out var partIndex))
                    continue;

                var part = new SetItemPart
                {
                    PartIndex = partIndex,
                    RepresentName = partNode.GetStringValue("representName") ?? "",
                    TypeName = partNode.GetStringValue("typeName") ?? "",
                };

                if (partNode.Value is int singleId)
                {
                    part.ItemIds.Add(singleId);
                }
                else
                {
                    foreach (var idChild in partNode.Nodes)
                    {
                        if (idChild.Value is int itemId)
                        {
                            part.ItemIds.Add(itemId);
                        }
                        else if (int.TryParse(idChild.Text, out _) && idChild.Value is int itemIdVal)
                        {
                            part.ItemIds.Add(itemIdVal);
                        }
                    }
                }

                info.Parts.Add(part);
            }
        }

        // 解析效果
        var effectNode = setNode.Nodes["Effect"];
        if (effectNode != null)
        {
            foreach (var countNode in effectNode.Nodes)
            {
                if (!int.TryParse(countNode.Text, out var reqCount))
                    continue;

                var effect = new SetItemEffect
                {
                    RequiredCount = reqCount,
                };

                foreach (var propNode in countNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        // Option: 潜能引用，需查 ItemOption.img 解析实际属性
                        case "Option":
                            ParseOptionProps(propNode, effect, itemOptionNode);
                            break;

                        case "OptionToMob":
                        case "bonusByTime":
                            break;

                        // 套装激活技能
                        case "activeSkill":
                            ParseActiveSkills(propNode, effect);
                            break;

                        // 普通数值属性: 值直接在 propNode.Value 上
                        default:
                            try
                            {
                                var intVal = Convert.ToInt32(propNode.Value);
                                effect.Props[propNode.Text] = intVal;
                            }
                            catch
                            {
                                // 值无法转为 int，跳过
                            }
                            break;
                    }
                }

                if (effect.Props.Count > 0 || effect.ActiveSkills.Count > 0)
                    info.Effects.Add(effect);
            }

            info.Effects.Sort((a, b) => a.RequiredCount.CompareTo(b.RequiredCount));
        }

        return info;
    }

    // ItemOption.img level 节点中的非属性元数据键，解析时需跳过
    private static readonly HashSet<string> OptionMetaKeys = ["fixedGrade"];

    /// <summary>
    /// 解析 Option 子节点: 通过 ItemOption.img 将潜能引用转为实际属性值
    /// WZ 结构: Option/0/{option=40301, level=5} → ItemOption.img/040301/level/5/{bdR=30, ...}
    /// </summary>
    private static void ParseOptionProps(Wz_Node optionContainerNode, SetItemEffect effect, Wz_Node? itemOptionNode)
    {
        if (itemOptionNode == null) return;

        foreach (var pNode in optionContainerNode.Nodes)
        {
            // 获取 option 代码和等级
            var optionCodeNode = pNode.Nodes["option"];
            var levelNode = pNode.Nodes["level"];
            if (optionCodeNode == null || levelNode == null) continue;

            try
            {
                var optionCode = Convert.ToString(optionCodeNode.Value)!.PadLeft(6, '0');
                var level = Convert.ToInt32(levelNode.Value);

                // 在 ItemOption.img 中查找
                var optNode = itemOptionNode.Nodes[optionCode];
                if (optNode == null) continue;

                // 提取并定位 level 子节点
                var optExtracted = optNode.ExtractAndGetNode();
                if (optExtracted == null) continue;

                var levelParent = optExtracted.Nodes["level"];
                if (levelParent == null) continue;

                var levelData = levelParent.Nodes[level.ToString()];
                if (levelData == null) continue;

                // 读取该等级下的所有属性
                foreach (var statNode in levelData.Nodes)
                {
                    // 跳过非属性的元数据键 (如 fixedGrade)
                    if (OptionMetaKeys.Contains(statNode.Text))
                        continue;

                    try
                    {
                        var statVal = Convert.ToInt32(statNode.Value);
                        // 累加到效果属性中 (同一效果可能有多个 Option 引用同一属性)
                        if (effect.Props.ContainsKey(statNode.Text))
                            effect.Props[statNode.Text] += statVal;
                        else
                            effect.Props[statNode.Text] = statVal;
                    }
                    catch
                    {
                        // 非 int 属性跳过 (如 face 等)
                    }
                }
            }
            catch
            {
                // 解析失败，跳过此 Option 条目
            }
        }
    }

    /// <summary>
    /// 解析套装激活技能节点
    /// </summary>
    private void ParseActiveSkills(Wz_Node activeSkillNode, SetItemEffect effect)
    {
        for (int i = 0; ; i++)
        {
            var skillNode = activeSkillNode.Nodes[i.ToString()];
            if (skillNode == null) break;

            var skillId = skillNode.GetIntValue("id");
            var skillLevel = skillNode.GetIntValue("level");
            if (skillId.HasValue)
            {
                string? skillName = null;
                string? skillDesc = null;
                if (_stringPool != null && _stringPool.TryGetValue(skillId.Value, out var entry))
                {
                    skillName = entry.Name;
                    skillDesc = entry.Description;
                }

                effect.ActiveSkills.Add(new SetItemActiveSkill
                {
                    SkillId = skillId.Value,
                    Level = skillLevel ?? 1,
                    SkillName = skillName,
                    Description = skillDesc,
                });
            }
        }
    }
}
