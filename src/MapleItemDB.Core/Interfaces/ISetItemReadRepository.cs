using MapleItemDB.Core.Models;

namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 套装读模型仓储接口
/// </summary>
public interface ISetItemReadRepository
{
    /// <summary>获取全部套装信息</summary>
    Task<Dictionary<int, SetItemInfo>> GetAllSetItemsAsync();
}
