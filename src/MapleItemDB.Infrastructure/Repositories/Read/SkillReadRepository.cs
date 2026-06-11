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
    private const string SelectProjection = """
        SELECT
            s.skill_id,
            s.name,
            s.description,
            s.job_id,
            s.max_level,
            ba_icon.blob_data AS icon_data,
            s.icon_blob_id,
            s.is_hidden,
            s.level_effects,
            s.skill_h,
            s.common_props,
            s.extracted_at
        FROM dim_skills s
        LEFT JOIN dim_blob_assets ba_icon ON ba_icon.blob_id = s.icon_blob_id
        """;

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
                ? $"{SelectProjection} WHERE s.is_hidden = 0 LIMIT @Limit"
                : $"{SelectProjection} WHERE s.is_hidden = 0";
            rows = await conn.QueryAsync<SkillRow>(sql, new { Limit = limit });
        }
        else
        {
            var sql = limit > 0
                ? $"{SelectProjection} WHERE (s.name LIKE @Keyword OR s.description LIKE @Keyword OR CAST(s.skill_id AS TEXT) LIKE @Keyword) LIMIT @Limit"
                : $"{SelectProjection} WHERE (s.name LIKE @Keyword OR s.description LIKE @Keyword OR CAST(s.skill_id AS TEXT) LIKE @Keyword)";
            rows = await conn.QueryAsync<SkillRow>(sql, new { Keyword = $"%{keyword}%", Limit = limit });
        }
        return rows.Select(r => r.ToEntity()).ToList();
    }
}
