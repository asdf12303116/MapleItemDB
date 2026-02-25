using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

public sealed class SearchSkillsUseCase : ISearchSkillsUseCase
{
    private readonly ISkillReadRepository _skillReadRepository;

    public SearchSkillsUseCase(ISkillReadRepository skillReadRepository)
    {
        _skillReadRepository = skillReadRepository;
    }

    public Task<IReadOnlyList<SkillEntity>> ExecuteAsync(
        string? keyword,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedKeyword = keyword ?? string.Empty;
        return _skillReadRepository.SearchSkillsByNameAsync(normalizedKeyword, limit);
    }
}
