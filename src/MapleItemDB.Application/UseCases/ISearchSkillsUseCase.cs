using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

public interface ISearchSkillsUseCase
{
    Task<IReadOnlyList<SkillEntity>> ExecuteAsync(string? keyword, int limit = 50, CancellationToken cancellationToken = default);
}
