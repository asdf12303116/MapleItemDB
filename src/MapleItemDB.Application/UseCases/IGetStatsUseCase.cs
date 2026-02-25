using MapleItemDB.Application.Contracts;

namespace MapleItemDB.Application.UseCases;

public interface IGetStatsUseCase
{
    Task<DatabaseStatsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
