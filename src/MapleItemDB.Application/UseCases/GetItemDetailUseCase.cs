using MapleItemDB.Application.Contracts;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

public sealed class GetItemDetailUseCase : IGetItemDetailUseCase
{
    public Task<ItemDetailResult> ExecuteAsync(
        ItemEntity? selectedItem,
        IReadOnlyDictionary<int, SkillEntity> skillsById,
        IReadOnlyDictionary<int, SetItemInfo> setItems,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (selectedItem == null)
        {
            return Task.FromResult(new ItemDetailResult
            {
                Item = null,
                Skill = null,
                SkillDetailText = null,
                FormattedStats = [],
                SetItemDisplayText = null,
                HasSetItem = false,
                CategoryDisplay = string.Empty,
            });
        }

        var categoryDisplay = ItemDetailFormattingHelper.BuildCategoryDisplay(selectedItem);

        if (selectedItem.Category == ItemCategory.Skill
            && skillsById.TryGetValue(selectedItem.ItemId, out var skill))
        {
            return Task.FromResult(new ItemDetailResult
            {
                Item = selectedItem,
                Skill = skill,
                SkillDetailText = ItemDetailFormattingHelper.BuildSkillDetailText(skill),
                FormattedStats = [],
                SetItemDisplayText = null,
                HasSetItem = false,
                CategoryDisplay = categoryDisplay,
            });
        }

        var formattedStats = ItemDetailFormattingHelper.FormatStats(selectedItem);
        string? setItemDisplayText = null;
        var hasSetItem = false;

        if (selectedItem.SetItemId is int setItemId
            && setItems.TryGetValue(setItemId, out var setItemInfo))
        {
            hasSetItem = true;
            setItemDisplayText = ItemDetailFormattingHelper.BuildSetItemDisplayText(setItemInfo);
        }

        return Task.FromResult(new ItemDetailResult
        {
            Item = selectedItem,
            Skill = null,
            SkillDetailText = null,
            FormattedStats = formattedStats,
            SetItemDisplayText = setItemDisplayText,
            HasSetItem = hasSetItem,
            CategoryDisplay = categoryDisplay,
        });
    }
}
