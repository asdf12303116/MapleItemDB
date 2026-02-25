using MapleItemDB.Core.Models;

namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 技能读模型仓储接口
/// </summary>
public interface ISkillReadRepository
{
    /// <summary>按名称搜索技能</summary>
    Task<IReadOnlyList<SkillEntity>> SearchSkillsByNameAsync(string keyword, int limit = 50);
}
