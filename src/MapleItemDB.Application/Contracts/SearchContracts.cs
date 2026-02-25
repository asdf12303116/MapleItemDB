using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.Contracts;

/// <summary>
/// 查询输入参数
/// </summary>
public sealed record SearchRequest
{
    public string? Keyword { get; init; }

    public ItemCategory? Category { get; init; }

    public string? SubCategory { get; init; }

    public int? MinLevel { get; init; }

    public int? MaxLevel { get; init; }

    public bool? IsCash { get; init; }

    public bool? HasSn { get; init; }

    public int? MinBossDmg { get; init; }

    public int? MinIed { get; init; }

    public int Limit { get; init; } = 0;

    public bool IncludeSkillsWhenAllCategories { get; init; }
}

/// <summary>
/// 查询结果
/// </summary>
public sealed class SearchResult
{
    public required IReadOnlyList<ItemEntity> Items { get; init; }

    public required IReadOnlyDictionary<int, SkillEntity> SkillsById { get; init; }

    public bool IsSkillMode { get; init; }

    public int ItemCount { get; init; }

    public int SkillCount { get; init; }
}
