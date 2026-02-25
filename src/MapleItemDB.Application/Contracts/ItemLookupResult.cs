using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.Contracts;

/// <summary>
/// 按 ID 查询结果
/// </summary>
public sealed class ItemLookupResult
{
    public required ItemEntity Item { get; init; }

    public SetItemInfo? SetItem { get; init; }
}
