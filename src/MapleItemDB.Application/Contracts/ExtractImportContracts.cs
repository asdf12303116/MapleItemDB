using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.Contracts;

/// <summary>
/// 导入请求
/// </summary>
public sealed class ExtractImportRequest
{
    public required string GameDirectory { get; init; }

    public required IProgress<ExtractionProgress> ExtractionProgress { get; init; }

    public IProgress<(int current, int total)>? ItemWriteProgress { get; init; }

    public IProgress<(int current, int total)>? SkillWriteProgress { get; init; }

    public CancellationToken CancellationToken { get; init; } = default;
}

/// <summary>
/// 导入结果
/// </summary>
public sealed class ExtractImportResult
{
    public required RawExtractionBundle Bundle { get; init; }

    public required IReadOnlyList<(int Id, string Name)> SearchIndex { get; init; }

    public required Dictionary<int, SetItemInfo> SetItems { get; init; }
}
