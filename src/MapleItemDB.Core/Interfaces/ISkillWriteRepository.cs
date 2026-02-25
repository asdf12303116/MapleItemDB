using MapleItemDB.Core.Models;

namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 技能写模型仓储接口
/// </summary>
public interface ISkillWriteRepository
{
    /// <summary>批量插入或更新技能</summary>
    Task BulkUpsertSkillsAsync(
        IEnumerable<SkillEntity> skills,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken cancellationToken = default);
}
