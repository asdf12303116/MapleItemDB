using MapleItemDB.Application.Contracts;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

public sealed class GetItemByIdUseCase : IGetItemByIdUseCase
{
    private readonly IItemReadRepository _itemReadRepository;
    private readonly ISetItemReadRepository _setItemReadRepository;

    public GetItemByIdUseCase(
        IItemReadRepository itemReadRepository,
        ISetItemReadRepository setItemReadRepository)
    {
        _itemReadRepository = itemReadRepository;
        _setItemReadRepository = setItemReadRepository;
    }

    public async Task<ItemLookupResult?> ExecuteAsync(int itemId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var item = await _itemReadRepository.GetByIdAsync(itemId);
        if (item is null)
            return null;

        SetItemInfo? setItem = null;
        if (item.SetItemId is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var setItems = await _setItemReadRepository.GetAllSetItemsAsync();
            setItems.TryGetValue(item.SetItemId.Value, out setItem);
        }

        return new ItemLookupResult
        {
            Item = item,
            SetItem = setItem,
        };
    }
}
