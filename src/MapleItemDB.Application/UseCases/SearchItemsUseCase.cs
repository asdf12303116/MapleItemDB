using MapleItemDB.Application.Contracts;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

public sealed class SearchItemsUseCase : ISearchItemsUseCase
{
    private readonly IItemReadRepository _itemReadRepository;
    private readonly ISkillReadRepository _skillReadRepository;

    public SearchItemsUseCase(
        IItemReadRepository itemReadRepository,
        ISkillReadRepository skillReadRepository)
    {
        _itemReadRepository = itemReadRepository;
        _skillReadRepository = skillReadRepository;
    }

    public async Task<SearchResult> ExecuteAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var isAllCategory = request.Category is null;
        var isSkillCategory = request.Category == ItemCategory.Skill;
        var includeSkills = isSkillCategory || (isAllCategory && request.IncludeSkillsWhenAllCategories);

        IReadOnlyList<ItemEntity> itemResults = [];
        IReadOnlyList<SkillEntity> skillResults = [];

        if (!isSkillCategory)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filter = new ItemQueryFilter
            {
                Keyword = request.Keyword,
                Category = request.Category,
                SubCategory = request.SubCategory,
                MinLevel = request.MinLevel,
                MaxLevel = request.MaxLevel,
                IsCash = request.IsCash,
                HasSn = request.HasSn,
                MinBossDmg = request.MinBossDmg,
                MinIed = request.MinIed,
                Limit = request.Limit,
            };
            itemResults = await _itemReadRepository.QueryAsync(filter);
        }

        if (includeSkills)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var skillLimit = isSkillCategory && request.Limit > 0 ? request.Limit : 0;
            skillResults = await _skillReadRepository.SearchSkillsByNameAsync(request.Keyword ?? string.Empty, skillLimit);
        }

        var skillsById = skillResults.ToDictionary(s => s.SkillId);

        var skillItems = skillResults.Select(s => new ItemEntity
        {
            ItemId = s.SkillId,
            Name = s.Name,
            Description = s.Description,
            Category = ItemCategory.Skill,
            SubCategory = $"job:{s.JobId}",
            ReqLevel = s.MaxLevel,
            IconData = s.IconData,
        });

        IReadOnlyList<ItemEntity> mergedItems = isSkillCategory
            ? skillItems.ToList()
            : includeSkills
                ? itemResults.Concat(skillItems).ToList()
                : itemResults;

        return new SearchResult
        {
            Items = mergedItems,
            SkillsById = skillsById,
            IsSkillMode = includeSkills,
            ItemCount = itemResults.Count,
            SkillCount = skillResults.Count,
        };
    }
}

