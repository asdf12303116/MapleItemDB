using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.Contracts;

/// <summary>
/// 详情面板数据
/// </summary>
public sealed class ItemDetailResult
{
    public ItemEntity? Item { get; init; }

    public SkillEntity? Skill { get; init; }

    public string? SkillDetailText { get; init; }

    public required List<StatsLine> FormattedStats { get; init; }

    public string? SetItemDisplayText { get; init; }

    public bool HasSetItem { get; init; }

    public string CategoryDisplay { get; init; } = string.Empty;
}
