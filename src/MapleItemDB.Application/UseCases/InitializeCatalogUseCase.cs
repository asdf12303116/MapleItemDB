using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

public sealed class InitializeCatalogUseCase : IInitializeCatalogUseCase
{
    private readonly IItemReadRepository _itemReadRepository;
    private readonly ISetItemReadRepository _setItemReadRepository;

    public InitializeCatalogUseCase(
        IItemReadRepository itemReadRepository,
        ISetItemReadRepository setItemReadRepository)
    {
        _itemReadRepository = itemReadRepository;
        _setItemReadRepository = setItemReadRepository;
    }

    public async Task<(IReadOnlyList<(int Id, string Name)> SearchIndex, Dictionary<int, SetItemInfo> SetItems)> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var searchIndex = await _itemReadRepository.GetIdNameIndexAsync();
        cancellationToken.ThrowIfCancellationRequested();
        var setItems = await _setItemReadRepository.GetAllSetItemsAsync();
        return (searchIndex, setItems);
    }
}

