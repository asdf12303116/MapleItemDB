namespace MapleItemDB.Application.UseCases;

public interface IInitializeCatalogUseCase
{
    Task<(IReadOnlyList<(int Id, string Name)> SearchIndex, Dictionary<int, MapleItemDB.Core.Models.SetItemInfo> SetItems)> ExecuteAsync(CancellationToken cancellationToken = default);
}
