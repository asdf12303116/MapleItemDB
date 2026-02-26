using System.Text;
using Dapper;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Repositories.Shared;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Infrastructure.Repositories.Read;

/// <summary>
/// 道具读仓储：查询与索引
/// </summary>
public class ItemReadRepository : IItemReadRepository
{
    private const string SelectProjection = """
        SELECT
            i.item_id,
            i.name,
            i.description,
            i.category,
            i.sub_category,
            i.req_level,
            i.req_str,
            i.req_dex,
            i.req_int,
            i.req_luk,
            i.req_job,
            i.inc_str,
            i.inc_dex,
            i.inc_int,
            i.inc_luk,
            i.inc_pad,
            i.inc_mad,
            i.inc_pdd,
            i.inc_mdd,
            i.inc_mhp,
            i.inc_mmp,
            i.dynamic_stats,
            i.consume_spec,
            i.is_cash,
            i.price,
            ba_icon.blob_data AS icon_data,
            ba_preview.blob_data AS preview_data,
            i.icon_blob_id,
            i.preview_blob_id,
            i.setitem_id,
            i.sn,
            i.time_limited,
            i.extracted_at
        FROM dim_items i
        LEFT JOIN dim_blob_assets ba_icon ON ba_icon.blob_id = i.icon_blob_id
        LEFT JOIN dim_blob_assets ba_preview ON ba_preview.blob_id = i.preview_blob_id
        """;

    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<ItemReadRepository> _logger;

    public ItemReadRepository(SqliteConnectionFactory connectionFactory, ILogger<ItemReadRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<ItemEntity?> GetByIdAsync(int itemId)
    {
        var sql = $"{SelectProjection} WHERE i.item_id = @item_id";
        using var conn = _connectionFactory.Create();
        var row = await conn.QueryFirstOrDefaultAsync<ItemRow>(sql, new { item_id = itemId });
        return row?.ToEntity();
    }

    public async Task<IReadOnlyList<ItemEntity>> SearchByNameAsync(string keyword, int limit = 50)
    {
        var sql = $"{SelectProjection} WHERE i.name LIKE @Keyword LIMIT @Limit";
        using var conn = _connectionFactory.Create();
        var rows = await conn.QueryAsync<ItemRow>(sql, new { Keyword = $"%{keyword}%", Limit = limit });
        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<ItemEntity>> QueryAsync(ItemQueryFilter filter)
    {
        var sb = new StringBuilder(SelectProjection + " WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            sb.Append(" AND (i.name LIKE @Keyword OR i.description LIKE @Keyword OR CAST(i.item_id AS TEXT) LIKE @Keyword)");
            parameters.Add("Keyword", $"%{filter.Keyword}%");
        }
        if (filter.Category.HasValue)
        {
            sb.Append(" AND i.category = @Category");
            parameters.Add("Category", filter.Category.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(filter.SubCategory))
        {
            sb.Append(" AND i.sub_category = @SubCategory");
            parameters.Add("SubCategory", filter.SubCategory);
        }
        if (filter.MinLevel.HasValue)
        {
            sb.Append(" AND i.req_level >= @MinLevel");
            parameters.Add("MinLevel", filter.MinLevel.Value);
        }
        if (filter.MaxLevel.HasValue)
        {
            sb.Append(" AND i.req_level <= @MaxLevel");
            parameters.Add("MaxLevel", filter.MaxLevel.Value);
        }
        if (filter.IsCash.HasValue)
        {
            sb.Append(" AND i.is_cash = @IsCash");
            parameters.Add("IsCash", filter.IsCash.Value ? 1 : 0);
        }
        if (filter.HasSn == true)
        {
            sb.Append(" AND i.sn IS NOT NULL");
        }
        if (filter.MinBossDmg.HasValue)
        {
            sb.Append(" AND JSON_EXTRACT(i.dynamic_stats, '$.boss_dmg') >= @MinBossDmg");
            parameters.Add("MinBossDmg", filter.MinBossDmg.Value);
        }
        if (filter.MinIed.HasValue)
        {
            sb.Append(" AND JSON_EXTRACT(i.dynamic_stats, '$.ied') >= @MinIed");
            parameters.Add("MinIed", filter.MinIed.Value);
        }

        if (filter.Limit > 0)
        {
            sb.Append(" LIMIT @Limit OFFSET @Offset");
            parameters.Add("Limit", filter.Limit);
            parameters.Add("Offset", filter.Offset);
        }

        using var conn = _connectionFactory.Create();
        var rows = await conn.QueryAsync<ItemRow>(sb.ToString(), parameters);
        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<(int Id, string Name)>> GetIdNameIndexAsync()
    {
        const string sql = "SELECT item_id, name FROM dim_items";
        using var conn = _connectionFactory.Create();
        var rows = await conn.QueryAsync<(int item_id, string name)>(sql);
        var result = rows.Select(r => (r.item_id, r.name)).ToList();
        _logger.LogInformation("加载索引: {Count} 条", result.Count);
        return result;
    }
}
