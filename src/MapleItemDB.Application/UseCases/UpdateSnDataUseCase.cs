using System.Globalization;
using System.Text.RegularExpressions;
using MapleItemDB.Application.Contracts;
using MapleItemDB.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Application.UseCases;

public sealed class UpdateSnDataUseCase : IUpdateSnDataUseCase
{
    private static readonly Regex SnRegex = new("^SN\\s*=\\s*(\\d+)$", RegexOptions.Compiled);
    private static readonly Regex ItemIdRegex = new("^ItemId\\s*=\\s*(\\d+)$", RegexOptions.Compiled);

    private readonly IItemSnRepository _itemSnRepository;
    private readonly ILogger<UpdateSnDataUseCase> _logger;

    public UpdateSnDataUseCase(IItemSnRepository itemSnRepository, ILogger<UpdateSnDataUseCase> logger)
    {
        _itemSnRepository = itemSnRepository;
        _logger = logger;
    }

    public async Task<UpdateSnDataResult> ExecuteAsync(UpdateSnDataRequest request)
    {
        request.CancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.SourceFilePath))
            throw new ArgumentException("SN 文件路径不能为空", nameof(request));

        if (!File.Exists(request.SourceFilePath))
            throw new FileNotFoundException("SN 文件不存在", request.SourceFilePath);

        var map = new Dictionary<int, int>();
        var validSnPairs = 0;
        var ignoredNon9Sn = 0;

        int? pendingSn = null;

        foreach (var raw in File.ReadLines(request.SourceFilePath))
        {
            request.CancellationToken.ThrowIfCancellationRequested();

            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var snMatch = SnRegex.Match(line);
            if (snMatch.Success)
            {
                if (int.TryParse(snMatch.Groups[1].Value, out var snValue))
                    pendingSn = snValue;
                continue;
            }

            var itemMatch = ItemIdRegex.Match(line);
            if (!itemMatch.Success || pendingSn is null)
                continue;

            if (!int.TryParse(itemMatch.Groups[1].Value, out var itemId))
            {
                pendingSn = null;
                continue;
            }

            var snText = pendingSn.Value.ToString(CultureInfo.InvariantCulture);
            if (snText.StartsWith("9", StringComparison.Ordinal))
            {
                validSnPairs++;
                map[itemId] = pendingSn.Value;
            }
            else
            {
                ignoredNon9Sn++;
            }

            pendingSn = null;
        }

        await _itemSnRepository.ReplaceAllAsync(map, request.CancellationToken);
        var updatedRows = await _itemSnRepository.ApplyToItemsAsync(request.CancellationToken);

        _logger.LogInformation("SN 更新完成: valid={ValidSnPairs}, saved={SavedPairs}, updatedItems={UpdatedRows}, ignoredNon9={IgnoredNon9}",
            validSnPairs, map.Count, updatedRows, ignoredNon9Sn);

        return new UpdateSnDataResult
        {
            ParsedPairs = validSnPairs,
            SavedPairs = map.Count,
            UpdatedItemRows = updatedRows,
            IgnoredNon9Sn = ignoredNon9Sn,
        };
    }
}




