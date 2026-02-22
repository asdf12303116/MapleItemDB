using System.Collections.ObjectModel;
using System.IO;
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
    private List<string> _formattedStats = [];

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
    }

    /// <summary>
    /// 初始化 — 预热内存搜索索引 + 加载套装缓存
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

        // 加载套装缓存
        LoadSetItemCache();
    }

    private void LoadSetItemCache()
    {
        try
        {
            var cachePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Cache", "setitems.json");
            if (File.Exists(cachePath))
            {
                var json = File.ReadAllText(cachePath);
                _setItems = JsonSerializer.Deserialize<Dictionary<int, SetItemInfo>>(json) ?? [];
                _logger.LogInformation("套装缓存加载完成: {Count} 个套装", _setItems.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "套装缓存加载失败");
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

    partial void OnSelectedItemChanged(ItemEntity? value)
    {
        FormattedStats = value != null ? StatsDisplayHelper.FormatStats(value) : [];
        UpdateSetItemDisplay(value);
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
                props.Add($"{skillName} Lv.{skill.Level}");
            }

            sb.Append($"【{effect.RequiredCount}件】{string.Join(", ", props)}");
        }

        return sb.ToString();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        _logger.LogInformation("搜索: \"{SearchText}\"", SearchText);
        StatusText = "正在搜索...";
        var results = await _repository.SearchByNameAsync(SearchText);
        SearchResults = new ObservableCollection<ItemEntity>(results);
        StatusText = $"找到 {results.Count} 个结果";
        _logger.LogInformation("搜索完成: {Count} 个结果", results.Count);
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

        var iconDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Cache", "Icons");

        var progress = new Progress<ExtractionProgress>(p =>
        {
            ExtractionStatus = p.Message ?? p.Phase;
            if (p.Total > 0)
                ExtractionProgress = (double)p.Current / p.Total * 100;
        });

        try
        {
            _logger.LogInformation("开始提取 WZ 数据: {GameDirectory}", gameDirectory);
            var items = await _extractor.ExtractAllAsync(gameDirectory, iconDir, progress);
            _logger.LogInformation("WZ 提取完成: {Count} 个道具", items.Count);

            ExtractionStatus = "正在写入数据库...";
            _logger.LogInformation("开始写入数据库...");
            await _repository.BulkUpsertAsync(items);
            _logger.LogInformation("数据库写入完成");

            ExtractionStatus = "正在刷新索引...";
            _logger.LogDebug("刷新搜索索引...");
            await InitializeAsync();

            StatusText = $"提取完成 — 共 {items.Count} 个道具";
            _logger.LogInformation("全部完成: {Count} 个道具", items.Count);
        }
        catch (Exception ex)
        {
            StatusText = $"提取失败: {ex.Message}";
            _logger.LogError(ex, "提取失败");
        }
        finally
        {
            IsExtracting = false;
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

    /// <summary>
    /// 将 ItemEntity 的属性格式化为游戏风格文本行
    /// </summary>
    public static List<string> FormatStats(ItemEntity item)
    {
        var lines = new List<string>();

        // 需求等级
        AddLine(lines, "reqLevel", item.ReqLevel);

        // 升级可用次数 (从 DynamicStats)
        var dynamic = ParseDynamic(item.DynamicStats);
        AddDynamicLine(lines, dynamic, "upgrade_slots");

        // 核心属性
        AddLine(lines, "incSTR", item.IncSTR);
        AddLine(lines, "incDEX", item.IncDEX);
        AddLine(lines, "incINT", item.IncINT);
        AddLine(lines, "incLUK", item.IncLUK);
        AddLine(lines, "incMHP", item.IncMHP);
        AddLine(lines, "incMMP", item.IncMMP);
        AddLine(lines, "incPAD", item.IncPAD);
        AddLine(lines, "incMAD", item.IncMAD);
        AddLine(lines, "incPDD", item.IncPDD);
        AddLine(lines, "incMDD", item.IncMDD);

        // 动态属性
        AddDynamicLine(lines, dynamic, "boss_dmg");
        AddDynamicLine(lines, dynamic, "ied");
        AddDynamicLine(lines, dynamic, "total_dmg");
        AddDynamicLine(lines, dynamic, "all_stat_pct");
        AddDynamicLine(lines, dynamic, "all_stat");
        AddDynamicLine(lines, dynamic, "speed");
        AddDynamicLine(lines, dynamic, "jump");
        AddDynamicLine(lines, dynamic, "knockback");

        // 攻击速度 (特殊格式)
        if (dynamic.TryGetValue("attack_speed", out var atkSpd))
        {
            var spdName = AttackSpeedNames.GetValueOrDefault(atkSpd, atkSpd.ToString());
            lines.Add($"攻击速度 : {spdName}");
        }

        // 特殊标志
        AddFlagLine(lines, dynamic, "_flag_only",              "唯一道具");
        AddFlagLine(lines, dynamic, "_flag_tradeBlock",        "不可交易");
        AddFlagLine(lines, dynamic, "_flag_equipTradeBlock",   "装备后不可交易");
        AddFlagLine(lines, dynamic, "_flag_accountSharable",   "账号内共享");
        AddFlagLine(lines, dynamic, "_flag_timeLimited",       "限时道具");
        AddFlagLine(lines, dynamic, "_flag_superiorEqp",       "星之力增强道具");
        AddFlagLine(lines, dynamic, "_flag_noPotential",       "不可使用潜能");
        AddFlagLine(lines, dynamic, "_flag_fixedPotential",    "固定潜能");

        // 消耗品属性
        if (!string.IsNullOrEmpty(item.ConsumeSpec))
        {
            var consume = ParseDynamic(item.ConsumeSpec);
            if (consume.Count > 0)
            {
                lines.Add("──── 使用效果 ────");
                foreach (var (key, val) in consume)
                {
                    var display = FormatConsumeStat(key, val);
                    if (display != null)
                        lines.Add(display);
                }
            }
        }

        return lines;
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

    private static void AddLine(List<string> lines, string key, int? value)
    {
        if (value.HasValue && value.Value != 0 && StatFormats.TryGetValue(key, out var fmt))
            lines.Add(string.Format(fmt, value.Value));
    }

    private static void AddDynamicLine(List<string> lines, Dictionary<string, int> dynamic, string key)
    {
        if (dynamic.TryGetValue(key, out var val) && val != 0)
        {
            if (StatFormats.TryGetValue(key, out var fmt))
                lines.Add(string.Format(fmt, val));
        }
    }

    private static void AddFlagLine(List<string> lines, Dictionary<string, int> dynamic, string key, string text)
    {
        if (dynamic.TryGetValue(key, out var val) && val != 0)
            lines.Add(text);
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
