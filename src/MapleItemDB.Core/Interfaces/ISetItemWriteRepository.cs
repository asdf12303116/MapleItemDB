using MapleItemDB.Core.Models;

namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 套装写模型仓储接口
/// </summary>
public interface ISetItemWriteRepository
{
    /// <summary>批量插入或更新套装信息</summary>
    Task BulkUpsertSetItemsAsync(
        IEnumerable<SetItemInfo> setItems,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken cancellationToken = default);
}
