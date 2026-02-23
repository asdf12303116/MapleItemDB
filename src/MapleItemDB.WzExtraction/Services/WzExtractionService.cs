using System.Text.Json;
using MapleItemDB.Core.Data;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using Microsoft.Extensions.Logging;
using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// WZ 数据提取管道主服务
/// </summary>
public class WzExtractionService : IWzExtractor, IDisposable
{
    private readonly ILogger<WzExtractionService> _logger;
    private Wz_Structure? _wzStructure;

    public WzExtractionService(ILogger<WzExtractionService> logger)
    {
        _logger = logger;
    }

    public async Task<ExtractionResult> ExtractAllAsync(
        string gameDirectory,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ExtractAllInternal(gameDirectory, progress, cancellationToken), cancellationToken);
    }

    private ExtractionResult ExtractAllInternal(
        string gameDirectory,
        IProgress<ExtractionProgress>? progress,
        CancellationToken ct)
    {
        var allItems = new List<ItemEntity>();

        // 1. 加载 WZ 文件 — 自动检测格式
        _logger.LogInformation("开始加载 WZ 文件: {GameDirectory}", gameDirectory);
        progress?.Report(new ExtractionProgress("加载 WZ 文件", 0, 1, "正在加载 WZ 数据..."));
        _wzStructure = new Wz_Structure();
        var wzRoot = LoadWzFiles(gameDirectory);
        _logger.LogInformation("WZ 加载完成, 共 {FileCount} 个文件", _wzStructure.wz_files.Count);
        _logger.LogDebug("根节点子节点: {NodeNames}", string.Join(", ", wzRoot.Nodes.Select(n => n.Text)));
        progress?.Report(new ExtractionProgress("加载 WZ 文件", 1, 1, $"WZ 加载完成，共 {_wzStructure.wz_files.Count} 个文件"));
        ct.ThrowIfCancellationRequested();

        // 2. 构建字符串池
        _logger.LogInformation("构建字符串池...");
        progress?.Report(new ExtractionProgress("构建字符串池", 0, 1, "正在解析 String.wz..."));
        var stringNode = wzRoot.Nodes["String"];
        if (stringNode == null)
            throw new InvalidOperationException("未找到 String 节点");

        var stringPoolBuilder = new StringPoolBuilder();
        stringPoolBuilder.Build(stringNode);
        _logger.LogInformation("字符串池: {Count} 个条目", stringPoolBuilder.Pool.Count);
        progress?.Report(new ExtractionProgress("构建字符串池", 1, 1, $"已解析 {stringPoolBuilder.Pool.Count} 个字符串条目"));
        ct.ThrowIfCancellationRequested();

        // 3. 创建宏变量解析器
        var macroResolver = new MacroResolver(stringPoolBuilder.Pool);

        // 4. 提取装备 (Character)
        _logger.LogInformation("开始提取装备 (Character)...");
        progress?.Report(new ExtractionProgress("提取装备", 0, 1, "正在解析 Character..."));
        var characterNode = wzRoot.Nodes["Character"];
        if (characterNode != null)
        {
            var equipExtractor = new EquipExtractor(macroResolver);
            var equips = equipExtractor.Extract(characterNode);
            allItems.AddRange(equips);
            _logger.LogInformation("装备提取完成: {Count} 件", equips.Count);
            progress?.Report(new ExtractionProgress("提取装备", 1, 1, $"已提取 {equips.Count} 件装备"));
        }
        else
        {
            _logger.LogWarning("未找到 Character 节点");
        }
        ct.ThrowIfCancellationRequested();

        // 5. 提取 Item 下各分类
        var itemNode = wzRoot.Nodes["Item"];
        if (itemNode != null)
        {
            _logger.LogInformation("开始提取 Item 分类道具...");
            var generalExtractor = new GeneralItemExtractor(macroResolver);

            var categories = new (string NodeName, ItemCategory Category)[]
            {
                ("Consume", ItemCategory.Consume),
                ("Etc", ItemCategory.Etc),
                ("Install", ItemCategory.Setup),
                ("Cash", ItemCategory.Cash),
                ("Pet", ItemCategory.Pet),
            };

            for (int i = 0; i < categories.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                var (nodeName, category) = categories[i];
                progress?.Report(new ExtractionProgress("提取道具", i, categories.Length, $"正在解析 {nodeName}..."));

                var catNode = itemNode.Nodes[nodeName];
                if (catNode != null)
                {
                    var extracted = generalExtractor.Extract(catNode, category);
                    allItems.AddRange(extracted);
                    _logger.LogInformation("{NodeName}: {Count} 个道具", nodeName, extracted.Count);
                }
                else
                {
                    _logger.LogWarning("未找到 Item/{NodeName} 节点", nodeName);
                }
            }
            progress?.Report(new ExtractionProgress("提取道具", categories.Length, categories.Length, "道具提取完成"));
        }
        else
        {
            _logger.LogWarning("未找到 Item 节点");
        }
        ct.ThrowIfCancellationRequested();

        // 6. 提取套装信息
        _logger.LogInformation("开始提取套装信息...");
        progress?.Report(new ExtractionProgress("提取套装", 0, 1, "正在解析 SetItemInfo..."));
        var setItemExtractor = new SetItemExtractor(stringPoolBuilder.Pool);
        var setItems = setItemExtractor.Extract(wzRoot);
        _logger.LogInformation("套装提取完成: {Count} 个套装", setItems.Count);
        progress?.Report(new ExtractionProgress("提取套装", 1, 1, $"已提取 {setItems.Count} 个套装"));

        // 7. 提取技能
        _logger.LogInformation("开始提取技能...");
        progress?.Report(new ExtractionProgress("提取技能", 0, 1, "正在解析 Skill..."));
        var skillExtractor = new SkillExtractor(stringPoolBuilder.Pool);
        var skills = skillExtractor.Extract(wzRoot, progress, ct);
        _logger.LogInformation("技能提取完成: {Count} 个技能", skills.Count);

        // 8. 导出图标 (内存 BLOB)
        progress?.Report(new ExtractionProgress("导出图标", 0, allItems.Count, "正在导出道具图标..."));
        ExportIcons(wzRoot, allItems, new IconExporter(), progress, ct);

        // 9. 填充 SN 编号
        var snCount = 0;
        foreach (var item in allItems)
        {
            var sn = SnMap.GetSn(item.ItemId);
            if (sn.HasValue)
            {
                item.Sn = sn.Value;
                snCount++;
            }
        }
        _logger.LogInformation("SN 填充完成: {Count} 个道具", snCount);

        progress?.Report(new ExtractionProgress("完成", allItems.Count, allItems.Count, $"共提取 {allItems.Count} 个道具, {skills.Count} 个技能"));
        _logger.LogInformation("全部提取完成: {Count} 个道具, {SkillCount} 个技能", allItems.Count, skills.Count);
        return new ExtractionResult(allItems, setItems, skills);
    }

    /// <summary>
    /// 智能加载 WZ 文件 — 支持传统单文件和 KMST1125 文件夹式格式
    /// </summary>
    private Wz_Node LoadWzFiles(string gameDirectory)
    {
        _logger.LogDebug("LoadWzFiles 输入路径: {GameDirectory}", gameDirectory);

        // 策略1: gameDirectory 直接就是 Base.wz 文件
        if (File.Exists(gameDirectory) && gameDirectory.EndsWith(".wz", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("策略1: 直接加载 Base.wz 文件");
            return LoadFromBaseWzFile(gameDirectory);
        }

        // 策略2: gameDirectory 包含 Base.wz (如用户选择了 Base 子文件夹)
        var baseWzDirect = Path.Combine(gameDirectory, "Base.wz");
        if (File.Exists(baseWzDirect) && _wzStructure!.IsKMST1125WzFormat(baseWzDirect))
        {
            // KMST1125 格式: 需要父目录 (Data) 作为根
            var parentDir = Path.GetDirectoryName(gameDirectory)!;
            _logger.LogDebug("策略2: KMST1125 格式, 父目录: {ParentDir}", parentDir);
            return LoadKmst1125(parentDir);
        }
        if (File.Exists(baseWzDirect))
        {
            _logger.LogDebug("策略2: 传统格式, 加载 {BaseWzPath}", baseWzDirect);
            return LoadFromBaseWzFile(baseWzDirect);
        }

        // 策略3: gameDirectory 是 Data 目录，包含 Base/Base.wz 子文件夹
        var baseSubDir = Path.Combine(gameDirectory, "Base", "Base.wz");
        if (File.Exists(baseSubDir) && _wzStructure!.IsKMST1125WzFormat(baseSubDir))
        {
            _logger.LogDebug("策略3: KMST1125 格式, Data 目录: {GameDirectory}", gameDirectory);
            return LoadKmst1125(gameDirectory);
        }
        if (File.Exists(baseSubDir))
        {
            _logger.LogDebug("策略3: 传统格式, 加载 {BaseWzPath}", baseSubDir);
            return LoadFromBaseWzFile(baseSubDir);
        }

        throw new FileNotFoundException(
            $"未找到 WZ 文件。请选择包含 Base.wz 的游戏数据目录。\n搜索路径:\n  {baseWzDirect}\n  {baseSubDir}");
    }

    private Wz_Node LoadFromBaseWzFile(string baseWzPath)
    {
        _wzStructure!.Load(baseWzPath);
        return _wzStructure.WzNode;
    }

    /// <summary>
    /// 加载 KMST1125 文件夹式 WZ 结构
    /// LoadKMST1125DataWz 会自动读取 Base 目录中的引用并加载所有子文件夹
    /// </summary>
    private Wz_Node LoadKmst1125(string dataDirectory)
    {
        var baseWzPath = Path.Combine(dataDirectory, "Base", "Base.wz");
        if (!File.Exists(baseWzPath))
        {
            baseWzPath = Path.Combine(dataDirectory, "Base.wz");
        }

        _wzStructure!.LoadKMST1125DataWz(baseWzPath);
        return _wzStructure.WzNode;
    }

    // 需要导出预览图的子分类
    private static readonly HashSet<string> PreviewSubCategories =
    [
        "Hair", "Face", "Accessory", "EyeDecoration", "Earring"
    ];

    private void ExportIcons(
        Wz_Node wzRoot,
        List<ItemEntity> items,
        IconExporter exporter,
        IProgress<ExtractionProgress>? progress,
        CancellationToken ct)
    {
        int exported = 0;
        int total = items.Count;
        var itemDict = items.ToDictionary(i => i.ItemId);

        // 遍历 Character 节点导出装备图标
        var characterNode = wzRoot.Nodes["Character"];
        if (characterNode != null)
        {
            foreach (var subCatNode in characterNode.Nodes)
            {
                ct.ThrowIfCancellationRequested();
                if (subCatNode.Text.StartsWith("_") || subCatNode.Text == "Afterimage")
                    continue;

                foreach (var imgNode in subCatNode.Nodes)
                {
                    if (imgNode.Value is not Wz_Image img)
                        continue;

                    var imgName = imgNode.Text;
                    if (!imgName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var idStr = imgName[..^4];
                    if (!int.TryParse(idStr, out var itemId))
                        continue;

                    if (!itemDict.TryGetValue(itemId, out var item))
                        continue;

                    if (!img.TryExtract())
                        continue;

                    var iconData = exporter.ExportFromEquip(img.Node, itemId);
                    if (iconData != null)
                        item.IconData = iconData;

                    // 外观装备预览图
                    if (item.SubCategory != null &&
                        (PreviewSubCategories.Contains(item.SubCategory) || item.IsCash))
                    {
                        var previewData = exporter.ExportPreview(img.Node, itemId, item.SubCategory);
                        if (previewData != null)
                            item.PreviewData = previewData;
                    }

                    exported++;
                    if (exported % 500 == 0)
                        progress?.Report(new ExtractionProgress("导出图标", exported, total, $"已导出 {exported}/{total}"));
                }
            }
        }

        // 遍历 Item 分类节点导出道具图标
        var itemNode = wzRoot.Nodes["Item"];
        if (itemNode != null)
        {
            var categories = new[] { "Consume", "Etc", "Install", "Cash", "Pet" };
            foreach (var catName in categories)
            {
                ct.ThrowIfCancellationRequested();
                var catNode = itemNode.Nodes[catName];
                if (catNode == null) continue;

                foreach (var imgFileNode in catNode.Nodes)
                {
                    if (imgFileNode.Value is not Wz_Image img)
                        continue;
                    if (!img.TryExtract())
                        continue;

                    if (catName == "Pet")
                    {
                        // Pet: .img 文件名本身就是 itemId (如 5000000.img)
                        var petIdStr = imgFileNode.Text;
                        if (petIdStr.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                            petIdStr = petIdStr[..^4];
                        if (int.TryParse(petIdStr, out var petId) && itemDict.TryGetValue(petId, out var petItem))
                        {
                            var iconData = exporter.ExportFromItem(img.Node, petId);
                            if (iconData != null)
                                petItem.IconData = iconData;

                            exported++;
                            if (exported % 500 == 0)
                                progress?.Report(new ExtractionProgress("导出图标", exported, total, $"已导出 {exported}/{total}"));
                        }
                    }
                    else
                    {
                        // 其他分类: .img 内有多个 itemId 子节点
                        foreach (var idNode in img.Node.Nodes)
                        {
                            if (!int.TryParse(idNode.Text, out var itemId))
                                continue;
                            if (!itemDict.TryGetValue(itemId, out var item))
                                continue;

                            var iconData = exporter.ExportFromItem(idNode, itemId);
                            if (iconData != null)
                                item.IconData = iconData;

                            // 提取 sample 预览图 (伤害皮肤等消耗品)
                            var sampleData = exporter.ExportSampleFromItem(idNode);
                            if (sampleData != null)
                                item.PreviewData = sampleData;

                            exported++;
                            if (exported % 500 == 0)
                                progress?.Report(new ExtractionProgress("导出图标", exported, total, $"已导出 {exported}/{total}"));
                        }
                    }
                }
            }
        }

        _logger.LogInformation("图标导出完成: {Count} 个", exported);
        progress?.Report(new ExtractionProgress("导出图标", total, total, $"图标导出完成"));
    }

    public void Dispose()
    {
        _wzStructure?.Clear();
        _wzStructure = null;
    }
}
