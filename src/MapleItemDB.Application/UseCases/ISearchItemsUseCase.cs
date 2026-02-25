using MapleItemDB.Application.Contracts;

namespace MapleItemDB.Application.UseCases;

public interface ISearchItemsUseCase
{
    Task<SearchResult> ExecuteAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
