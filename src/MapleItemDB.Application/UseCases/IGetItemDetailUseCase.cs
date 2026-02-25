using MapleItemDB.Application.Contracts;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

public interface IGetItemDetailUseCase
{
    Task<ItemDetailResult> ExecuteAsync(
        ItemEntity? selectedItem,
        IReadOnlyDictionary<int, SkillEntity> skillsById,
        IReadOnlyDictionary<int, SetItemInfo> setItems,
        CancellationToken cancellationToken = default);
}
