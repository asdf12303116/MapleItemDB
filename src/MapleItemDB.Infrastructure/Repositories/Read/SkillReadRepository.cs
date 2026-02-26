using Dapper;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Repositories.Shared;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Infrastructure.Repositories.Read;

/// <summary>
/// 技能读仓储
/// </summary>
public class SkillReadRepository : ISkillReadRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<SkillReadRepository> _logger;

    public SkillReadRepository(SqliteConnectionFactory connectionFactory, ILogger<SkillReadRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SkillEntity>> SearchSkillsByNameAsync(string keyword, int limit = 50)
    {
        using var conn = _connectionFactory.Create();
        IEnumerable<SkillRow> rows;
        if (string.IsNullOrWhiteSpace(keyword))
        {
            var sql = limit > 0
                ? "SELECT * FROM dim_skills WHERE is_hidden = 0 LIMIT @Limit"
                : "SELECT * FROM dim_skills WHERE is_hidden = 0";
            rows = await conn.QueryAsync<SkillRow>(sql, new { Limit = limit });
        }
        else
        {
            var sql = limit > 0
                ? "SELECT * FROM dim_skills WHERE (name LIKE @Keyword OR description LIKE @Keyword) LIMIT @Limit"
                : "SELECT * FROM dim_skills WHERE (name LIKE @Keyword OR description LIKE @Keyword)";
            rows = await conn.QueryAsync<SkillRow>(sql, new { Keyword = $"%{keyword}%", Limit = limit });
        }
        return rows.Select(r => r.ToEntity()).ToList();
    }
}
