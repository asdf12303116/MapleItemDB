using MapleItemDB.Core.Models;

namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// WZ 提取器接口
/// </summary>
public interface IWzExtractor
{
    /// <summary>
    /// 从游戏目录提取所有道具、套装和技能数据
    /// </summary>
    Task<ExtractionResult> ExtractAllAsync(
        string gameDirectory,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 提取结果请求参数
/// </summary>
public record ExtractionRequest(
    string GameDirectory,
    IProgress<ExtractionProgress>? Progress = null,
    CancellationToken CancellationToken = default);

/// <summary>
/// 提取结果包，聚合提取出的领域实体
/// </summary>
public record RawExtractionBundle(
    IReadOnlyList<ItemEntity> Items,
    IReadOnlyDictionary<int, SetItemInfo> SetItems,
    IReadOnlyList<SkillEntity> Skills);

/// <summary>
/// 提取结果（兼容旧命名）
/// </summary>
public record ExtractionResult(
    IReadOnlyList<ItemEntity> Items,
    IReadOnlyDictionary<int, SetItemInfo> SetItems,
    IReadOnlyList<SkillEntity> Skills)
    : RawExtractionBundle(Items, SetItems, Skills);

/// <summary>
/// 提取进度信息
/// </summary>
public record ExtractionProgress(
    string Phase,
    int Current,
    int Total,
    string? Message = null);
