using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Cache;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.UI.ViewModels;

/// <summary>
/// 分类选项 (ComboBox 绑定用)
/// </summary>
public record CategoryOption(string Label, ItemCategory? Value);

/// <summary>
/// 装备子分类选项 (ComboBox 绑定用)
/// </summary>
public record SubCategoryOption(string Label, string? Value);

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IItemRepository _repository;
    private readonly IWzExtractor _extractor;
    private readonly InMemorySearchIndex _searchIndex;
    private readonly ILogger<MainViewModel> _logger;

    private Dictionary<int, SetItemInfo> _setItems = [];

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private ObservableCollection<ItemEntity> _searchResults = [];

    [ObservableProperty]
    private ItemEntity? _selectedItem;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _isExtracting;

    [ObservableProperty]
    private double _extractionProgress;

    [ObservableProperty]
    private string _extractionStatus = "";

    [ObservableProperty]
    private ObservableCollection<(int Id, string Name)> _autoCompleteItems = [];

    /// <summary>
    /// 格式化后的属性行列表 (游戏风格)
    /// </summary>
    [ObservableProperty]
    private List<StatsLine> _formattedStats = [];

    /// <summary>
    /// 套装效果文本
    /// </summary>
    [ObservableProperty]
    private string? _setItemDisplayText;

    /// <summary>
    /// 当前选中道具是否有套装信息
    /// </summary>
    [ObservableProperty]
    private bool _hasSetItem;

    /// <summary>
    /// 分类筛选选项列表
    /// </summary>
    public List<CategoryOption> CategoryOptions { get; } =
    [
        new("全部", null),
        new("装备", ItemCategory.Equip),
        new("消耗品", ItemCategory.Consume),
        new("其他", ItemCategory.Etc),
        new("设置", ItemCategory.Setup),
        new("现金", ItemCategory.Cash),
        new("宠物", ItemCategory.Pet),
        new("技能", ItemCategory.Skill),
    ];

    [ObservableProperty]
    private CategoryOption _selectedCategoryOption;

    /// <summary>
    /// 装备子分类选项列表
    /// </summary>
    public static List<SubCategoryOption> EquipSubCategoryOptions { get; } =
    [
        new("全部子分类", null),
        new("帽子", "Cap"),
        new("上衣", "Coat"),
        new("套服", "Longcoat"),
        new("裤子", "Pants"),
        new("鞋子", "Shoes"),
        new("手套", "Glove"),
        new("腰带", "Belt"),
        new("肩饰", "Shoulder"),
        new("披风", "Cape"),
        new("盾牌", "Shield"),
        new("戒指", "Ring"),
        new("吊坠", "Pendant"),
        new("勋章", "Medal"),
        new("耳环", "Earring"),
        new("徽章", "Badge"),
        new("纹章", "Emblem"),
        new("口袋", "Pocket"),
        new("机器心脏", "Heart"),
        new("图腾", "Totem"),
        new("武器", "Weapon"),
        new("辅助武器", "SecondWeapon"),
        new("发型", "Hair"),
        new("脸型", "Face"),
        new("脸饰", "Accessory"),
        new("眼饰", "EyeDecoration"),
        new("机器人", "Android"),
        new("骑宠", "TamingMob"),
        new("宠物装备", "PetEquip"),
        new("符号(ARC)", "ArcaneForce"),
        new("符号(AUT)", "AuthenticForce"),
    ];

    [ObservableProperty]
    private SubCategoryOption _selectedSubCategoryOption;

    [ObservableProperty]
    private bool _isEquipSelected;

    public MainViewModel(
        IItemRepository repository,
        IWzExtractor extractor,
        InMemorySearchIndex searchIndex,
        ILogger<MainViewModel> logger)
    {
        _repository = repository;
        _extractor = extractor;
        _searchIndex = searchIndex;
        _logger = logger;
        _selectedCategoryOption = CategoryOptions[0];
        _selectedSubCategoryOption = EquipSubCategoryOptions[0];
    }

    /// <summary>
    /// 初始化 — 预热内存搜索索引 + 从数据库加载套装信息
    /// </summary>
    public async Task InitializeAsync()
    {
        StatusText = "正在加载搜索索引...";
        _logger.LogInformation("开始加载搜索索引...");
        try
        {
            var index = await _repository.GetIdNameIndexAsync();
            _searchIndex.Load(index);
            StatusText = $"就绪 — 已加载 {_searchIndex.Count} 个道具索引";
            _logger.LogInformation("搜索索引加载完成: {Count} 条", _searchIndex.Count);
        }
        catch (Exception ex)
        {
            StatusText = $"索引加载失败: {ex.Message}（请先提取数据）";
            _logger.LogError(ex, "索引加载失败");
        }

        // 从数据库加载套装信息
        await LoadSetItemsFromDbAsync();
    }

    private async Task LoadSetItemsFromDbAsync()
    {
        try
        {
            _setItems = await _repository.GetAllSetItemsAsync();
            if (_setItems.Count > 0)
                _logger.LogInformation("套装信息加载完成: {Count} 个套装", _setItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "套装信息加载失败");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AutoCompleteItems.Clear();
            return;
        }

        // 内存索引实时搜索
        if (_searchIndex.IsLoaded)
        {
            var results = _searchIndex.Search(value, 20);
            AutoCompleteItems = new ObservableCollection<(int Id, string Name)>(results);
        }
    }

    partial void OnSelectedCategoryOptionChanged(CategoryOption value)
    {
        IsEquipSelected = value.Value == ItemCategory.Equip;
        SelectedSubCategoryOption = EquipSubCategoryOptions[0];
    }

    partial void OnSelectedItemChanged(ItemEntity? value)
    {
        if (value != null && IsSkillMode && _skillSearchCache.TryGetValue(value.ItemId, out var skill))
        {
            SelectedSkill = skill;
            SkillDetailText = BuildSkillDetailText(skill);
            FormattedStats = [];
            HasSetItem = false;
            SetItemDisplayText = null;
            return;
        }

        SelectedSkill = null;
        SkillDetailText = null;
        FormattedStats = value != null ? StatsDisplayHelper.FormatStats(value) : [];
        UpdateSetItemDisplay(value);
    }

    private static string BuildSkillDetailText(SkillEntity skill)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"技能ID: {skill.SkillId}");
        sb.AppendLine($"职业ID: {skill.JobId}");
        sb.AppendLine($"最大等级: {skill.MaxLevel}");
        if (skill.IsHidden)
            sb.AppendLine("(隐藏技能)");
        if (!string.IsNullOrEmpty(skill.Description))
        {
            sb.AppendLine();
            sb.AppendLine(skill.Description);
        }
        if (!string.IsNullOrEmpty(skill.LevelEffectsJson))
        {
            sb.AppendLine();
            sb.AppendLine("── 等级效果 ──");
            try
            {
                var effects = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(skill.LevelEffectsJson);
                if (effects != null)
                {
                    foreach (var (level, props) in effects)
                    {
                        var propTexts = props
                            .Where(p => !p.Key.StartsWith("hs") && p.Key != "lt" && p.Key != "rb")
                            .Select(p => $"{p.Key}={p.Value}")
                            .ToList();
                        if (propTexts.Count > 0)
                            sb.AppendLine($"Lv.{level}: {string.Join(", ", propTexts)}");
                    }
                }
            }
            catch
            {
                // JSON 解析失败，忽略
            }
        }
        return sb.ToString().TrimEnd();
    }

    private void UpdateSetItemDisplay(ItemEntity? item)
    {
        if (item?.SetItemId == null || !_setItems.TryGetValue(item.SetItemId.Value, out var setInfo))
        {
            SetItemDisplayText = null;
            HasSetItem = false;
            return;
        }

        HasSetItem = true;
        SetItemDisplayText = BuildEffectsText(setInfo);
    }

    private string BuildEffectsText(SetItemInfo setInfo)
    {
        var sb = new StringBuilder();
        sb.Append(setInfo.SetItemName);

        foreach (var effect in setInfo.Effects)
        {
            sb.AppendLine();

            // 检查 boss 标志位: boss=1 时 incDAMr 应显示为 Boss 伤害
            var hasBossFlag = effect.Props.TryGetValue("boss", out var bossVal) && bossVal != 0;

            var props = new List<string>();
            foreach (var (key, val) in effect.Props)
            {
                // 跳过 boss 标志位本身
                if (key == "boss") continue;

                // 当 boss=1 时，incDAMr 表示 Boss 伤害而非总伤害
                if (key == "incDAMr" && hasBossFlag)
                {
                    props.Add($"攻击首领怪时的伤害 : +{val}%");
                    continue;
                }

                var display = StatsDisplayHelper.FormatSingleStat(key, val);
                props.Add(display ?? $"{key} : +{val}");
            }

            // 技能信息
            foreach (var skill in effect.ActiveSkills)
            {
                var skillName = skill.SkillName ?? $"技能#{skill.SkillId}";
                var skillText = $"{skillName} Lv.{skill.Level}";
                if (!string.IsNullOrEmpty(skill.Description))
                    skillText += $" ({skill.Description})";
                props.Add(skillText);
            }

            sb.Append($"【{effect.RequiredCount}件】{string.Join(", ", props)}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 当前选中的技能 (搜索技能分类时)
    /// </summary>
    [ObservableProperty]
    private SkillEntity? _selectedSkill;

    /// <summary>
    /// 技能详情文本
    /// </summary>
    [ObservableProperty]
    private string? _skillDetailText;

    /// <summary>
    /// 是否正在查看技能
    /// </summary>
    [ObservableProperty]
    private bool _isSkillMode;

    // 技能搜索结果缓存 (skillId → SkillEntity)
    private Dictionary<int, SkillEntity> _skillSearchCache = [];

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        _logger.LogInformation("搜索: \"{SearchText}\", 分类: {Category}",
            SearchText, SelectedCategoryOption.Label);
        StatusText = "正在搜索...";

        if (SelectedCategoryOption.Value == ItemCategory.Skill)
        {
            // 技能搜索
            IsSkillMode = true;
            var skills = await _repository.SearchSkillsByNameAsync(SearchText);
            _skillSearchCache = skills.ToDictionary(s => s.SkillId);

            // 转换为 ItemEntity 以复用 DataGrid 显示
            var items = skills.Select(s => new ItemEntity
            {
                ItemId = s.SkillId,
                Name = s.Name,
                Description = s.Description,
                Category = ItemCategory.Skill,
                SubCategory = $"职业{s.JobId}",
                ReqLevel = s.MaxLevel,
                IconData = s.IconData,
            }).ToList();

            SearchResults = new ObservableCollection<ItemEntity>(items);
            StatusText = $"找到 {skills.Count} 个技能";
            _logger.LogInformation("技能搜索完成: {Count} 个结果", skills.Count);
        }
        else
        {
            // 道具搜索
            IsSkillMode = false;
            _skillSearchCache.Clear();
            var filter = new ItemQueryFilter
            {
                Keyword = SearchText,
                Category = SelectedCategoryOption.Value,
                SubCategory = SelectedSubCategoryOption?.Value,
            };
            var results = await _repository.QueryAsync(filter);
            SearchResults = new ObservableCollection<ItemEntity>(results);
            StatusText = $"找到 {results.Count} 个结果";
            _logger.LogInformation("搜索完成: {Count} 个结果", results.Count);
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = "";
        SearchResults.Clear();
        SelectedItem = null;
        SelectedCategoryOption = CategoryOptions[0];
        SelectedSubCategoryOption = EquipSubCategoryOptions[0];
        IsSkillMode = false;
        SelectedSkill = null;
        SkillDetailText = null;
        _skillSearchCache.Clear();
        StatusText = "就绪";
    }

    [RelayCommand]
    private async Task LoadItemDetailAsync(int itemId)
    {
        var item = await _repository.GetByIdAsync(itemId);
        SelectedItem = item;
    }

    [RelayCommand]
    private async Task ExtractDataAsync(string gameDirectory)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory)) return;

        IsExtracting = true;
        ExtractionProgress = 0;

        // 各阶段加权进度映射: Phase → (起始百分比, 权重)
        var phaseWeights = new Dictionary<string, (double Start, double Weight)>
        {
            ["加载 WZ 文件"] = (0, 5),
            ["构建字符串池"] = (5, 5),
            ["提取装备"] = (10, 15),
            ["提取道具"] = (25, 15),
            ["提取套装"] = (40, 5),
            ["提取技能"] = (45, 10),
            ["导出图标"] = (55, 35),
            ["完成"] = (90, 10),
        };

        var progress = new Progress<ExtractionProgress>(p =>
        {
            ExtractionStatus = p.Message ?? p.Phase;
            if (phaseWeights.TryGetValue(p.Phase, out var w) && p.Total > 0)
                ExtractionProgress = w.Start + w.Weight * ((double)p.Current / p.Total);
            else if (p.Total > 0)
                ExtractionProgress = (double)p.Current / p.Total * 100;
        });

        try
        {
            _logger.LogInformation("开始提取 WZ 数据: {GameDirectory}", gameDirectory);
            var result = await _extractor.ExtractAllAsync(gameDirectory, progress);
            _logger.LogInformation("WZ 提取完成: {Count} 个道具, {SetCount} 个套装",
                result.Items.Count, result.SetItems.Count);

            ExtractionStatus = "正在写入数据库...";
            ExtractionProgress = 92;
            _logger.LogInformation("开始写入数据库...");
            await _repository.BulkUpsertAsync(result.Items);
            await _repository.BulkUpsertSetItemsAsync(result.SetItems.Values);
            if (result.Skills.Count > 0)
                await _repository.BulkUpsertSkillsAsync(result.Skills);
            _logger.LogInformation("数据库写入完成");

            ExtractionStatus = "正在刷新索引...";
            ExtractionProgress = 97;
            _logger.LogDebug("刷新搜索索引...");
            await InitializeAsync();

            ExtractionProgress = 100;
            StatusText = $"提取完成 — 共 {result.Items.Count} 个道具, {result.Skills.Count} 个技能";
            _logger.LogInformation("全部完成: {Count} 个道具, {SkillCount} 个技能",
                result.Items.Count, result.Skills.Count);
        }
        catch (Exception ex)
        {
            StatusText = $"提取失败: {ex.Message}";
            _logger.LogError(ex, "提取失败");
        }
        finally
        {
            IsExtracting = false;
            ExtractionProgress = 0;
        }
    }
}

/// <summary>
/// 道具属性格式化辅助类 — 将属性转换为游戏风格中文文本
/// </summary>
public static class StatsDisplayHelper
{
    // 核心属性映射 (字段名 → 显示格式)
    private static readonly Dictionary<string, string> StatFormats = new()
    {
        ["reqLevel"] = "需要等级 : {0}",
        ["incSTR"] = "力量 : +{0}",
        ["incDEX"] = "敏捷 : +{0}",
        ["incINT"] = "智力 : +{0}",
        ["incLUK"] = "运气 : +{0}",
        ["incMHP"] = "MaxHP : +{0}",
        ["incMMP"] = "MaxMP : +{0}",
        ["incPAD"] = "攻击力 : +{0}",
        ["incMAD"] = "魔法攻击力 : +{0}",
        ["incPDD"] = "防御力 : +{0}",
        ["incMDD"] = "魔法防御力 : +{0}",
        // 百分比加成
        ["incSTRr"] = "力量 : +{0}%",
        ["incDEXr"] = "敏捷 : +{0}%",
        ["incINTr"] = "智力 : +{0}%",
        ["incLUKr"] = "运气 : +{0}%",
        ["incMHPr"] = "MaxHP : +{0}%",
        ["incMMPr"] = "MaxMP : +{0}%",
        ["incPADr"] = "攻击力 : +{0}%",
        ["incMADr"] = "魔法攻击力 : +{0}%",
        ["incPDDr"] = "防御力 : +{0}%",
        ["incMDDr"] = "魔法防御力 : +{0}%",
        ["incACCr"] = "命中值 : +{0}%",
        ["incEVAr"] = "回避值 : +{0}%",
        // 特殊属性
        ["boss_dmg"] = "BOSS攻击时伤害+{0}%",
        ["bdR"] = "BOSS攻击时伤害+{0}%",
        ["incBDR"] = "BOSS攻击时伤害+{0}%",
        ["ied"] = "无视怪物防御力 : +{0}%",
        ["imdR"] = "无视怪物防御力 : +{0}%",
        ["incIMDR"] = "无视怪物防御力 : +{0}%",
        ["total_dmg"] = "总伤害 : +{0}%",
        ["damR"] = "总伤害 : +{0}%",
        ["incDAMr"] = "总伤害 : +{0}%",
        ["nbdR"] = "普通怪物伤害+{0}%",
        ["all_stat_pct"] = "全部属性 : +{0}%",
        ["statR"] = "全部属性 : +{0}%",
        ["all_stat"] = "全部属性 : +{0}",
        ["incAllStat"] = "全部属性 : +{0}",
        ["speed"] = "移动速度 : +{0}",
        ["incSpeed"] = "移动速度 : +{0}",
        ["jump"] = "跳跃力 : +{0}",
        ["incJump"] = "跳跃力 : +{0}",
        ["attack_speed"] = "攻击速度 : {0}",
        ["attackSpeed"] = "攻击速度 : {0}",
        ["upgrade_slots"] = "升级可用次数 : {0}",
        ["tuc"] = "升级可用次数 : {0}",
        ["knockback"] = "击退 : {0}%",
        ["incACC"] = "命中值 : +{0}",
        ["incEVA"] = "回避值 : +{0}",
        ["incCr"] = "暴击概率 : +{0}%",
        ["incCDr"] = "暴击伤害 : +{0}%",
        ["incTerR"] = "状态异常抗性 : +{0}%",
        ["incAsrR"] = "全部属性抗性 : +{0}%",
        ["incEXPr"] = "经验值获取量 : +{0}%",
        ["reduceCooltime"] = "冷却时间减少 : {0}秒",
        ["incARC"] = "ARC : +{0}",
        ["incAUT"] = "AUT : +{0}",
        ["incMDF"] = "异常状态抗性 : +{0}",
        ["incCraft"] = "手技 : +{0}",
        ["incPVPDamage"] = "PVP伤害 : +{0}",
        ["incMaxDamage"] = "最大伤害 : +{0}",
        ["incAllskill"] = "全部技能等级 : +{0}",
        // ItemOption.img 派生键名 (套装 Option 引用)
        ["ignoreTargetDEF"] = "无视怪物防御率 : +{0}%",
        ["incCriticaldamage"] = "暴击伤害 : +{0}%",
        ["incCriticaldamageMin"] = "最小暴击伤害 : +{0}%",
        ["incCriticaldamageMax"] = "最大暴击伤害 : +{0}%",
        ["RecoveryHP"] = "每10秒回复HP : {0}",
        ["RecoveryMP"] = "每10秒回复MP : {0}",
        ["incMesoProp"] = "金币获取量 : +{0}%",
        ["incRewardProp"] = "道具掉落率 : +{0}%",
        ["mpconReduce"] = "MP消耗减少 : {0}%",
        ["incPQEXPr"] = "组队任务经验 : +{0}%",
    };

    // 攻击速度等级映射
    private static readonly Dictionary<int, string> AttackSpeedNames = new()
    {
        [2] = "更快(2)",
        [3] = "更快(3)",
        [4] = "快(4)",
        [5] = "快(5)",
        [6] = "普通(6)",
        [7] = "慢(7)",
        [8] = "慢(8)",
        [9] = "更慢(9)",
    };

    // 特殊标志键名集合
    private static readonly HashSet<string> FlagKeys =
    [
        "_flag_only", "_flag_tradeBlock", "_flag_equipTradeBlock",
        "_flag_accountSharable", "_flag_timeLimited", "_flag_superiorEqp",
        "_flag_noPotential", "_flag_fixedPotential",
    ];

    private static readonly Dictionary<string, string> FlagTexts = new()
    {
        ["_flag_only"]              = "唯一道具",
        ["_flag_tradeBlock"]        = "不可交易",
        ["_flag_equipTradeBlock"]   = "装备后不可交易",
        ["_flag_accountSharable"]   = "账号内共享",
        ["_flag_timeLimited"]       = "限时道具",
        ["_flag_superiorEqp"]       = "星之力增强道具",
        ["_flag_noPotential"]       = "不可使用潜能",
        ["_flag_fixedPotential"]    = "固定潜能",
    };

    /// <summary>
    /// 将 ItemEntity 的属性格式化为游戏风格文本行 (StatsLine)，标志行置顶
    /// </summary>
    public static List<StatsLine> FormatStats(ItemEntity item)
    {
        var normalLines = new List<StatsLine>();
        var flagLines = new List<StatsLine>();

        // 需求等级
        AddLine(normalLines, "reqLevel", item.ReqLevel);

        // 升级可用次数 (从 DynamicStats)
        var dynamic = ParseDynamic(item.DynamicStats);
        AddDynamicLine(normalLines, dynamic, "upgrade_slots");

        // 核心属性
        AddLine(normalLines, "incSTR", item.IncSTR);
        AddLine(normalLines, "incDEX", item.IncDEX);
        AddLine(normalLines, "incINT", item.IncINT);
        AddLine(normalLines, "incLUK", item.IncLUK);
        AddLine(normalLines, "incMHP", item.IncMHP);
        AddLine(normalLines, "incMMP", item.IncMMP);
        AddLine(normalLines, "incPAD", item.IncPAD);
        AddLine(normalLines, "incMAD", item.IncMAD);
        AddLine(normalLines, "incPDD", item.IncPDD);
        AddLine(normalLines, "incMDD", item.IncMDD);

        // 动态属性
        AddDynamicLine(normalLines, dynamic, "boss_dmg");
        AddDynamicLine(normalLines, dynamic, "ied");
        AddDynamicLine(normalLines, dynamic, "total_dmg");
        AddDynamicLine(normalLines, dynamic, "all_stat_pct");
        AddDynamicLine(normalLines, dynamic, "all_stat");
        AddDynamicLine(normalLines, dynamic, "speed");
        AddDynamicLine(normalLines, dynamic, "jump");
        AddDynamicLine(normalLines, dynamic, "knockback");

        // 攻击速度 (特殊格式)
        if (dynamic.TryGetValue("attack_speed", out var atkSpd))
        {
            var spdName = AttackSpeedNames.GetValueOrDefault(atkSpd, atkSpd.ToString());
            normalLines.Add(new StatsLine($"攻击速度 : {spdName}"));
        }

        // 特殊标志 → flagLines (IsFlag=true，橙色置顶)
        foreach (var (key, text) in FlagTexts)
        {
            if (dynamic.TryGetValue(key, out var val) && val != 0)
                flagLines.Add(new StatsLine(text, IsFlag: true));
        }

        // 消耗品属性
        if (!string.IsNullOrEmpty(item.ConsumeSpec))
        {
            var consume = ParseDynamic(item.ConsumeSpec);
            if (consume.Count > 0)
            {
                normalLines.Add(new StatsLine("──── 使用效果 ────"));
                foreach (var (key, val) in consume)
                {
                    var display = FormatConsumeStat(key, val);
                    if (display != null)
                        normalLines.Add(new StatsLine(display));
                }
            }
        }

        // 标志行置顶
        flagLines.AddRange(normalLines);
        return flagLines;
    }

    /// <summary>
    /// 格式化单个属性 (供套装效果复用)
    /// </summary>
    public static string? FormatSingleStat(string key, int value)
    {
        if (StatFormats.TryGetValue(key, out var fmt))
        {
            if (key is "attack_speed" or "attackSpeed")
            {
                var spdName = AttackSpeedNames.GetValueOrDefault(value, value.ToString());
                return string.Format(fmt, spdName);
            }
            return string.Format(fmt, value);
        }
        return null;
    }

    private static void AddLine(List<StatsLine> lines, string key, int? value)
    {
        if (value.HasValue && value.Value != 0 && StatFormats.TryGetValue(key, out var fmt))
            lines.Add(new StatsLine(string.Format(fmt, value.Value)));
    }

    private static void AddDynamicLine(List<StatsLine> lines, Dictionary<string, int> dynamic, string key)
    {
        if (dynamic.TryGetValue(key, out var val) && val != 0)
        {
            if (StatFormats.TryGetValue(key, out var fmt))
                lines.Add(new StatsLine(string.Format(fmt, val)));
        }
    }

    private static Dictionary<string, int> ParseDynamic(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static readonly Dictionary<string, string> ConsumeFormats = new()
    {
        ["hp"] = "HP 回复 : +{0}",
        ["mp"] = "MP 回复 : +{0}",
        ["hpR"] = "HP 回复 : +{0}%",
        ["mpR"] = "MP 回复 : +{0}%",
        ["time"] = "持续时间 : {0}秒",
        ["pad"] = "攻击力 : +{0}",
        ["mad"] = "魔法攻击力 : +{0}",
        ["pdd"] = "防御力 : +{0}",
        ["mdd"] = "魔法防御力 : +{0}",
        ["speed"] = "移动速度 : +{0}",
        ["jump"] = "跳跃力 : +{0}",
        ["eva"] = "回避值 : +{0}",
        ["acc"] = "命中值 : +{0}",
    };

    private static string? FormatConsumeStat(string key, int value)
    {
        if (ConsumeFormats.TryGetValue(key, out var fmt))
            return string.Format(fmt, value);
        return $"{key} : {value}";
    }
}
