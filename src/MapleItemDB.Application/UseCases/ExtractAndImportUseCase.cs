using MapleItemDB.Application.Contracts;
using MapleItemDB.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Application.UseCases;

public sealed class ExtractAndImportUseCase : IExtractAndImportUseCase
{
    private readonly IWzExtractor _extractor;
    private readonly IItemWriteRepository _itemWriteRepository;
    private readonly ISetItemWriteRepository _setItemWriteRepository;
    private readonly ISkillWriteRepository _skillWriteRepository;
    private readonly IItemReadRepository _itemReadRepository;
    private readonly ISetItemReadRepository _setItemReadRepository;
    private readonly IItemSnRepository _itemSnRepository;
    private readonly ILogger<ExtractAndImportUseCase> _logger;

    public ExtractAndImportUseCase(
        IWzExtractor extractor,
        IItemWriteRepository itemWriteRepository,
        ISetItemWriteRepository setItemWriteRepository,
        ISkillWriteRepository skillWriteRepository,
        IItemReadRepository itemReadRepository,
        ISetItemReadRepository setItemReadRepository,
        IItemSnRepository itemSnRepository,
        ILogger<ExtractAndImportUseCase> logger)
    {
        _extractor = extractor;
        _itemWriteRepository = itemWriteRepository;
        _setItemWriteRepository = setItemWriteRepository;
        _skillWriteRepository = skillWriteRepository;
        _itemReadRepository = itemReadRepository;
        _setItemReadRepository = setItemReadRepository;
        _itemSnRepository = itemSnRepository;
        _logger = logger;
    }

    public async Task<ExtractImportResult> ExecuteAsync(ExtractImportRequest request)
    {
        request.CancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("开始提取并导入，目录: {GameDirectory}", request.GameDirectory);

        var extraction = await _extractor.ExtractAllAsync(
            request.GameDirectory,
            request.ExtractionProgress,
            request.CancellationToken);

        var snLookup = await _itemSnRepository.GetLookupAsync(request.CancellationToken);
        foreach (var item in extraction.Items)
        {
            item.Sn = snLookup.TryGetValue(item.ItemId, out var sn) ? sn : null;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        await _itemWriteRepository.BulkUpsertAsync(extraction.Items, request.ItemWriteProgress, request.CancellationToken);
        await _setItemWriteRepository.BulkUpsertSetItemsAsync(extraction.SetItems.Values, cancellationToken: request.CancellationToken);

        if (extraction.Skills.Count > 0)
            await _skillWriteRepository.BulkUpsertSkillsAsync(extraction.Skills, request.SkillWriteProgress, request.CancellationToken);

        var index = await _itemReadRepository.GetIdNameIndexAsync();
        var setItems = await _setItemReadRepository.GetAllSetItemsAsync();

        _logger.LogInformation("提取并导入完成，items={ItemCount}, skills={SkillCount}", extraction.Items.Count, extraction.Skills.Count);
        return new ExtractImportResult
        {
            Bundle = extraction,
            SearchIndex = index,
            SetItems = setItems,
        };
    }
}
