namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// SN 映射仓储接口
/// </summary>
public interface IItemSnRepository
{
    /// <summary>
    /// 获取 item_id -> sn 映射表
    /// </summary>
    Task<IReadOnlyDictionary<int, int>> GetLookupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 全量覆盖写入 SN 映射（先清空再写入）
    /// </summary>
    Task ReplaceAllAsync(IReadOnlyDictionary<int, int> map, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将映射表回填到 dim_items.sn（全量覆盖语义）
    /// </summary>
    Task<int> ApplyToItemsAsync(CancellationToken cancellationToken = default);
}
