using MapleItemDB.Application.Contracts;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

public sealed class GetStatsUseCase : IGetStatsUseCase
{
    private readonly IItemReadRepository _itemReadRepository;
    private readonly ISetItemReadRepository _setItemReadRepository;
    private readonly ISkillReadRepository _skillReadRepository;

    public GetStatsUseCase(
        IItemReadRepository itemReadRepository,
        ISetItemReadRepository setItemReadRepository,
        ISkillReadRepository skillReadRepository)
    {
        _itemReadRepository = itemReadRepository;
        _setItemReadRepository = setItemReadRepository;
        _skillReadRepository = skillReadRepository;
    }

    public async Task<DatabaseStatsResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allItems = await _itemReadRepository.QueryAsync(new ItemQueryFilter { Limit = 0 });

        cancellationToken.ThrowIfCancellationRequested();
        var setItems = await _setItemReadRepository.GetAllSetItemsAsync();

        cancellationToken.ThrowIfCancellationRequested();
        var skills = await _skillReadRepository.SearchSkillsByNameAsync(string.Empty, 0);

        var categories = allItems
            .GroupBy(i => i.Category)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        return new DatabaseStatsResult
        {
            TotalItems = allItems.Count,
            TotalSetItems = setItems.Count,
            TotalSkills = skills.Count,
            Categories = categories,
            CashItems = allItems.Count(i => i.IsCash),
            SnItems = allItems.Count(i => i.Sn is not null),
        };
    }
}
