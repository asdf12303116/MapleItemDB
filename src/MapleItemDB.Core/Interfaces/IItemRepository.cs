using MapleItemDB.Core.Models;

namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 道具数据仓储接口
/// </summary>
public interface IItemRepository
{
    /// <summary>根据 ID 获取道具详情</summary>
    Task<ItemEntity?> GetByIdAsync(int itemId);

    /// <summary>按名称模糊搜索</summary>
    Task<IReadOnlyList<ItemEntity>> SearchByNameAsync(string keyword, int limit = 50);

    /// <summary>按条件组合查询</summary>
    Task<IReadOnlyList<ItemEntity>> QueryAsync(ItemQueryFilter filter);

    /// <summary>批量插入或更新道具</summary>
    Task BulkUpsertAsync(IEnumerable<ItemEntity> items);

    /// <summary>获取全量 Id-Name 索引 (用于内存搜索)</summary>
    Task<IReadOnlyList<(int Id, string Name)>> GetIdNameIndexAsync();
}
