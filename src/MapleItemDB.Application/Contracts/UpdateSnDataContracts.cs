namespace MapleItemDB.Application.Contracts;

/// <summary>
/// 更新 SN 数据请求
/// </summary>
public sealed class UpdateSnDataRequest
{
    public required string SourceFilePath { get; init; }

    public CancellationToken CancellationToken { get; init; } = default;
}

/// <summary>
/// 更新 SN 数据结果
/// </summary>
public sealed class UpdateSnDataResult
{
    public required int ParsedPairs { get; init; }

    public required int SavedPairs { get; init; }

    public required int UpdatedItemRows { get; init; }

    public required int IgnoredNon9Sn { get; init; }
}
