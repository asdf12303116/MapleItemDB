namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 兼容旧调用的聚合仓储接口。
/// 新代码应优先依赖读写拆分后的接口。
/// </summary>
public interface IItemRepository :
    IItemReadRepository,
    ISetItemReadRepository,
    ISkillReadRepository,
    IItemWriteRepository,
    ISetItemWriteRepository,
    ISkillWriteRepository
{
}
