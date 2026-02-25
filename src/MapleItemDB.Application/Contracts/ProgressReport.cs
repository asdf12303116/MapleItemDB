namespace MapleItemDB.Application.Contracts;

/// <summary>
/// 应用层进度信息
/// </summary>
public sealed class ProgressReport
{
    public required string Stage { get; init; }

    public int Current { get; init; }

    public int Total { get; init; }

    public string? Message { get; init; }
}
