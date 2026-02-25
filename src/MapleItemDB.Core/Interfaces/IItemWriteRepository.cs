using MapleItemDB.Core.Models;

namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 道具写模型仓储接口
/// </summary>
public interface IItemWriteRepository
{
    /// <summary>批量插入或更新道具</summary>
    Task BulkUpsertAsync(
        IEnumerable<ItemEntity> items,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken cancellationToken = default);
}
