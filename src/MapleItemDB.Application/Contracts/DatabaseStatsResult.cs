namespace MapleItemDB.Application.Contracts;

/// <summary>
/// 数据库统计结果
/// </summary>
public sealed class DatabaseStatsResult
{
    public int TotalItems { get; init; }

    public int TotalSetItems { get; init; }

    public int TotalSkills { get; init; }

    public required Dictionary<string, int> Categories { get; init; }

    public int CashItems { get; init; }

    public int SnItems { get; init; }
}
