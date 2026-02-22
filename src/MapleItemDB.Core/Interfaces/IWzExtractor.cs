using MapleItemDB.Core.Models;

namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// WZ 数据提取器接口
/// </summary>
public interface IWzExtractor
{
    /// <summary>
    /// 从游戏目录提取所有道具数据和套装信息
    /// </summary>
    /// <param name="gameDirectory">游戏安装目录 (包含 Base.wz 的目录)</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提取结果 (道具 + 套装)</returns>
    Task<ExtractionResult> ExtractAllAsync(
        string gameDirectory,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 提取结果 — 包含道具、套装和技能信息
/// </summary>
public record ExtractionResult(
    IReadOnlyList<ItemEntity> Items,
    IReadOnlyDictionary<int, SetItemInfo> SetItems,
    IReadOnlyList<SkillEntity> Skills);

/// <summary>
/// 提取进度信息
/// </summary>
public record ExtractionProgress(
    string Phase,       // 当前阶段名称
    int Current,        // 当前进度
    int Total,          // 总数
    string? Message = null  // 可选详细信息
);
