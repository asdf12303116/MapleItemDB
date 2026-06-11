using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapleItemDB.Application.Contracts;
using MapleItemDB.Application.UseCases;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
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
/// SN 筛选选项 (ComboBox 绑定用)
/// </summary>
public record SnFilterOption(string Label, bool? Value);

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IInitializeCatalogUseCase _initializeCatalogUseCase;
    private readonly ISearchItemsUseCase _searchItemsUseCase;
    private readonly IGetItemByIdUseCase _getItemByIdUseCase;
    private readonly IGetItemDetailUseCase _getItemDetailUseCase;
    private readonly IExtractAndImportUseCase _extractAndImportUseCase;
    private readonly IUpdateSnDataUseCase _updateSnDataUseCase;
    private readonly ISearchIndex _searchIndex;
    private readonly ILogger<MainViewModel> _logger;

    private Dictionary<int, SetItemInfo> _setItems = [];
    private IReadOnlyDictionary<int, SkillEntity> _skillSearchCache = new Dictionary<int, SkillEntity>();

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
    /// 是否有属性需要显示
    /// </summary>
    [ObservableProperty]
    private bool _hasFormattedStats;

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
    /// 详情面板的分类显示文本 (如 "装备 / 武器 / 双手斧")
    /// </summary>
    [ObservableProperty]
    private string _selectedItemCategoryDisplay = "";

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
        new("神秘徽章", "ArcaneForce"),
        new("原初徽章", "AuthenticForce"),
    ];

    [ObservableProperty]
    private SubCategoryOption _selectedSubCategoryOption;

    [ObservableProperty]
    private bool _isEquipSelected;

    [ObservableProperty]
    private bool _isCashSelected;

    /// <summary>
    /// 是否显示 SN 相关筛选/列/右键菜单 (装备或现金时显示)
    /// </summary>
    [ObservableProperty]
    private bool _showSnFeatures;

    /// <summary>
    /// 是否显示限时道具列 (消耗品/其他/设置时显示)
    /// </summary>
    [ObservableProperty]
    private bool _showTimeLimitedColumn;

    /// <summary>
    /// 是否显示子分类和等级列 (现金/消耗品/其他/设置时隐藏)
    /// </summary>
    [ObservableProperty]
    private bool _showSubCategoryAndLevel = true;

    /// <summary>
    /// SN 筛选选项列表
    /// </summary>
    public static List<SnFilterOption> SnFilterOptions { get; } =
    [
        new("全部", null),
        new("仅存在SN", true),
    ];

    [ObservableProperty]
    private SnFilterOption _selectedSnFilterOption;

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

    public MainViewModel(
        IInitializeCatalogUseCase initializeCatalogUseCase,
        ISearchItemsUseCase searchItemsUseCase,
        IGetItemByIdUseCase getItemByIdUseCase,
        IGetItemDetailUseCase getItemDetailUseCase,
        IExtractAndImportUseCase extractAndImportUseCase,
        IUpdateSnDataUseCase updateSnDataUseCase,
        ISearchIndex searchIndex,
        ILogger<MainViewModel> logger)
    {
        _initializeCatalogUseCase = initializeCatalogUseCase;
        _searchItemsUseCase = searchItemsUseCase;
        _getItemByIdUseCase = getItemByIdUseCase;
        _getItemDetailUseCase = getItemDetailUseCase;
        _extractAndImportUseCase = extractAndImportUseCase;
        _updateSnDataUseCase = updateSnDataUseCase;
        _searchIndex = searchIndex;
        _logger = logger;
        _selectedCategoryOption = CategoryOptions[0];
        _selectedSubCategoryOption = EquipSubCategoryOptions[0];
        _selectedSnFilterOption = SnFilterOptions[0];
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
            var (searchIndex, setItems) = await _initializeCatalogUseCase.ExecuteAsync();
            _searchIndex.Load(searchIndex);
            _setItems = setItems;
            StatusText = $"就绪 — 已加载 {_searchIndex.Count} 个道具索引";
            _logger.LogInformation("搜索索引加载完成: {Count} 条, 套装: {SetCount} 个",
                _searchIndex.Count, _setItems.Count);
        }
        catch (Exception ex)
        {
            StatusText = $"索引加载失败: {ex.Message}（请先提取数据）";
            _logger.LogError(ex, "索引加载失败");
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
        IsCashSelected = value.Value == ItemCategory.Cash;
        ShowSnFeatures = IsEquipSelected || IsCashSelected;
        ShowTimeLimitedColumn = value.Value is ItemCategory.Consume or ItemCategory.Etc or ItemCategory.Setup;
        ShowSubCategoryAndLevel = !IsCashSelected && !ShowTimeLimitedColumn;
        SelectedSubCategoryOption = EquipSubCategoryOptions[0];
        SelectedSnFilterOption = SnFilterOptions[0];

        // 切换大类时，清空当前搜索结果与详情状态，避免显示上一次分类结果。
        SearchResults = [];
        SelectedItem = null;
        IsSkillMode = false;
        SelectedSkill = null;
        SkillDetailText = null;
        _skillSearchCache = new Dictionary<int, SkillEntity>();
        StatusText = "就绪";
    }

    partial void OnSelectedItemChanged(ItemEntity? value)
    {
        _ = UpdateItemDetailAsync(value);
    }

    private async Task UpdateItemDetailAsync(ItemEntity? value)
    {
        try
        {
            var result = await _getItemDetailUseCase.ExecuteAsync(value, _skillSearchCache, _setItems);
            SelectedItemCategoryDisplay = result.CategoryDisplay;
            SelectedSkill = result.Skill;
            SkillDetailText = result.SkillDetailText;
            FormattedStats = result.FormattedStats;
            HasFormattedStats = result.FormattedStats.Count > 0;
            HasSetItem = result.HasSetItem;
            SetItemDisplayText = result.SetItemDisplayText;
            IsSkillMode = result.Skill != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新详情失败: ItemId={ItemId}", value?.ItemId);
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _logger.LogInformation("搜索: \"{SearchText}\", 分类: {Category}",
            SearchText, SelectedCategoryOption.Label);
        StatusText = "正在搜索...";

        try
        {
            var request = new SearchRequest
            {
                Keyword = SearchText,
                Category = SelectedCategoryOption.Value,
                SubCategory = SelectedSubCategoryOption?.Value,
                HasSn = SelectedSnFilterOption?.Value,
                Limit = 0,
                IncludeSkillsWhenAllCategories = true,
            };

            var result = await _searchItemsUseCase.ExecuteAsync(request);

            _skillSearchCache = result.SkillsById;
            IsSkillMode = result.IsSkillMode;
            SearchResults = new ObservableCollection<ItemEntity>(result.Items);

            if (result.SkillCount > 0 && result.ItemCount > 0)
                StatusText = $"找到 {result.ItemCount} 个道具, {result.SkillCount} 个技能";
            else if (result.SkillCount > 0)
                StatusText = $"找到 {result.SkillCount} 个技能";
            else
                StatusText = $"找到 {result.ItemCount} 个结果";

            _logger.LogInformation("搜索完成: {ItemCount} 个道具, {SkillCount} 个技能",
                result.ItemCount, result.SkillCount);
        }
        catch (Exception ex)
        {
            StatusText = $"搜索失败: {ex.Message}";
            _logger.LogError(ex, "搜索失败");
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = "";
        SearchResults = [];
        SelectedItem = null;
        SelectedCategoryOption = CategoryOptions[0];
        SelectedSubCategoryOption = EquipSubCategoryOptions[0];
        SelectedSnFilterOption = SnFilterOptions[0];
        IsSkillMode = false;
        SelectedSkill = null;
        SkillDetailText = null;
        _skillSearchCache = new Dictionary<int, SkillEntity>();
        StatusText = "就绪";
    }

    [RelayCommand]
    private void CopySelectedItemName()
    {
        if (SelectedItem != null)
            SafeSetClipboard(SelectedItem.Name);
    }

    [RelayCommand]
    private void CopySelectedItemId()
    {
        if (SelectedItem != null)
            SafeSetClipboard(SelectedItem.ItemId.ToString());
    }

    [RelayCommand]
    private void CopySelectedItemSn()
    {
        if (SelectedItem?.Sn != null)
            SafeSetClipboard(SelectedItem.Sn.Value.ToString());
    }

    private static void SafeSetClipboard(string text)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return;
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
    }

    [RelayCommand]
    private async Task LoadItemDetailAsync(int itemId)
    {
        try
        {
            var result = await _getItemByIdUseCase.ExecuteAsync(itemId);
            SelectedItem = result?.Item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载道具详情失败: ItemId={ItemId}", itemId);
        }
    }

    [RelayCommand]
    private async Task ExtractDataAsync(string gameDirectory)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory))
            return;

        IsExtracting = true;
        ExtractionProgress = 0;

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

        var extractionProgress = new Progress<ExtractionProgress>(p =>
        {
            ExtractionStatus = p.Message ?? p.Phase;
            if (phaseWeights.TryGetValue(p.Phase, out var weight) && p.Total > 0)
                ExtractionProgress = weight.Start + weight.Weight * ((double)p.Current / p.Total);
            else if (p.Total > 0)
                ExtractionProgress = (double)p.Current / p.Total * 100;
        });

        var itemWriteProgress = new Progress<(int current, int total)>(p =>
        {
            if (p.total <= 0)
                return;

            ExtractionProgress = 90 + 5.0 * p.current / p.total;
            ExtractionStatus = $"正在写入道具 ({p.current}/{p.total})...";
        });

        var skillWriteProgress = new Progress<(int current, int total)>(p =>
        {
            if (p.total <= 0)
                return;

            ExtractionProgress = 95 + 2.0 * p.current / p.total;
            ExtractionStatus = $"正在写入技能 ({p.current}/{p.total})...";
        });

        try
        {
            _logger.LogInformation("开始提取 WZ 数据: {GameDirectory}", gameDirectory);
            var result = await _extractAndImportUseCase.ExecuteAsync(new ExtractImportRequest
            {
                GameDirectory = gameDirectory,
                ExtractionProgress = extractionProgress,
                ItemWriteProgress = itemWriteProgress,
                SkillWriteProgress = skillWriteProgress,
            });

            _logger.LogInformation(
                "提取并导入完成: {ItemCount} 个道具, {SkillCount} 个技能",
                result.Bundle.Items.Count,
                result.Bundle.Skills.Count);

            ExtractionStatus = "正在刷新索引...";
            ExtractionProgress = 97;
            _searchIndex.Load(result.SearchIndex);
            _setItems = result.SetItems;

            ExtractionProgress = 100;
            StatusText = $"提取完成 — 共 {result.Bundle.Items.Count} 个道具, {result.Bundle.Skills.Count} 个技能";
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

    [RelayCommand]
    private async Task UpdateSnDataAsync(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            return;

        StatusText = "正在更新 SN 数据...";

        try
        {
            var result = await _updateSnDataUseCase.ExecuteAsync(new UpdateSnDataRequest
            {
                SourceFilePath = sourceFilePath,
            });

            StatusText = $"SN 更新完成 — 映射 {result.SavedPairs} 条, 回填 {result.UpdatedItemRows} 条道具";

            // 若已加载索引，刷新当前搜索结果以显示最新 SN
            if (!string.IsNullOrWhiteSpace(SearchText) || SearchResults.Count > 0)
                await SearchAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"SN 更新失败: {ex.Message}";
            _logger.LogError(ex, "更新 SN 数据失败");
        }
    }
}
