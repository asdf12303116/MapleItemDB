using MapleItemDB.Application.Contracts;

namespace MapleItemDB.Application.UseCases;

public interface IGetItemByIdUseCase
{
    Task<ItemLookupResult?> ExecuteAsync(int itemId, CancellationToken cancellationToken = default);
}
