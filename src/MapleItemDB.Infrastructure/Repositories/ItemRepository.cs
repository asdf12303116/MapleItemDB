using System.Text;
using System.Text.Json;
using Dapper;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Infrastructure.Repositories;

/// <summary>
/// 道具数据仓储 — Dapper 实现
/// </summary>
public class ItemRepository : IItemRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<ItemRepository> _logger;

    public ItemRepository(SqliteConnectionFactory connectionFactory, ILogger<ItemRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<ItemEntity?> GetByIdAsync(int itemId)
    {
        const string sql = "SELECT * FROM dim_items WHERE item_id = @item_id";
        using var conn = _connectionFactory.Create();
        var row = await conn.QueryFirstOrDefaultAsync<ItemRow>(sql, new { item_id = itemId });
        return row?.ToEntity();
    }

    public async Task<IReadOnlyList<ItemEntity>> SearchByNameAsync(string keyword, int limit = 50)
    {
        const string sql = "SELECT * FROM dim_items WHERE name LIKE @Keyword LIMIT @Limit";
        using var conn = _connectionFactory.Create();
        var rows = await conn.QueryAsync<ItemRow>(sql, new { Keyword = $"%{keyword}%", Limit = limit });
        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<ItemEntity>> QueryAsync(ItemQueryFilter filter)
    {
        var sb = new StringBuilder("SELECT * FROM dim_items WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            sb.Append(" AND name LIKE @Keyword");
            parameters.Add("Keyword", $"%{filter.Keyword}%");
        }
        if (filter.Category.HasValue)
        {
            sb.Append(" AND category = @Category");
            parameters.Add("Category", filter.Category.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(filter.SubCategory))
        {
            sb.Append(" AND sub_category = @SubCategory");
            parameters.Add("SubCategory", filter.SubCategory);
        }
        if (filter.MinLevel.HasValue)
        {
            sb.Append(" AND req_level >= @MinLevel");
            parameters.Add("MinLevel", filter.MinLevel.Value);
        }
        if (filter.MaxLevel.HasValue)
        {
            sb.Append(" AND req_level <= @MaxLevel");
            parameters.Add("MaxLevel", filter.MaxLevel.Value);
        }
        if (filter.IsCash.HasValue)
        {
            sb.Append(" AND is_cash = @IsCash");
            parameters.Add("IsCash", filter.IsCash.Value ? 1 : 0);
        }
        if (filter.MinBossDmg.HasValue)
        {
            sb.Append(" AND JSON_EXTRACT(dynamic_stats, '$.boss_dmg') >= @MinBossDmg");
            parameters.Add("MinBossDmg", filter.MinBossDmg.Value);
        }
        if (filter.MinIed.HasValue)
        {
            sb.Append(" AND JSON_EXTRACT(dynamic_stats, '$.ied') >= @MinIed");
            parameters.Add("MinIed", filter.MinIed.Value);
        }

        sb.Append(" LIMIT @Limit OFFSET @Offset");
        parameters.Add("Limit", filter.Limit);
        parameters.Add("Offset", filter.Offset);

        using var conn = _connectionFactory.Create();
        var rows = await conn.QueryAsync<ItemRow>(sb.ToString(), parameters);
        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task BulkUpsertAsync(IEnumerable<ItemEntity> items)
    {
        // SQL 参数名必须与 ItemRow 属性名一致 (Dapper 按属性名匹配)
        const string sql = """
            INSERT INTO dim_items (
                item_id, name, description, category, sub_category,
                req_level, req_str, req_dex, req_int, req_luk,
                inc_str, inc_dex, inc_int, inc_luk,
                inc_pad, inc_mad, inc_pdd, inc_mdd, inc_mhp, inc_mmp,
                dynamic_stats, consume_spec,
                is_cash, price, icon_data, preview_data, setitem_id, extracted_at
            ) VALUES (
                @item_id, @name, @description, @category, @sub_category,
                @req_level, @req_str, @req_dex, @req_int, @req_luk,
                @inc_str, @inc_dex, @inc_int, @inc_luk,
                @inc_pad, @inc_mad, @inc_pdd, @inc_mdd, @inc_mhp, @inc_mmp,
                @dynamic_stats, @consume_spec,
                @is_cash, @price, @icon_data, @preview_data, @setitem_id, @extracted_at
            )
            ON CONFLICT(item_id) DO UPDATE SET
                name=excluded.name, description=excluded.description,
                category=excluded.category, sub_category=excluded.sub_category,
                req_level=excluded.req_level, req_str=excluded.req_str,
                req_dex=excluded.req_dex, req_int=excluded.req_int, req_luk=excluded.req_luk,
                inc_str=excluded.inc_str, inc_dex=excluded.inc_dex,
                inc_int=excluded.inc_int, inc_luk=excluded.inc_luk,
                inc_pad=excluded.inc_pad, inc_mad=excluded.inc_mad,
                inc_pdd=excluded.inc_pdd, inc_mdd=excluded.inc_mdd,
                inc_mhp=excluded.inc_mhp, inc_mmp=excluded.inc_mmp,
                dynamic_stats=excluded.dynamic_stats, consume_spec=excluded.consume_spec,
                is_cash=excluded.is_cash, price=excluded.price,
                icon_data=excluded.icon_data, preview_data=excluded.preview_data,
                setitem_id=excluded.setitem_id,
                extracted_at=excluded.extracted_at
            """;

        _logger.LogInformation("开始批量写入 {Count} 条记录...", items.Count());
        using var conn = _connectionFactory.Create();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var rows = items.Select(ItemRow.FromEntity).ToList();
        await conn.ExecuteAsync(sql, rows, transaction: tx);

        await tx.CommitAsync();
        _logger.LogInformation("批量写入完成");
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

    public async Task BulkUpsertSetItemsAsync(IEnumerable<SetItemInfo> setItems)
    {
        const string sql = """
            INSERT INTO dim_setitems (setitem_id, name, data_json)
            VALUES (@setitem_id, @name, @data_json)
            ON CONFLICT(setitem_id) DO UPDATE SET
                name=excluded.name, data_json=excluded.data_json
            """;

        using var conn = _connectionFactory.Create();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var rows = setItems.Select(s => new
        {
            setitem_id = s.SetItemId,
            name = s.SetItemName,
            data_json = JsonSerializer.Serialize(s),
        });
        await conn.ExecuteAsync(sql, rows, transaction: tx);

        await tx.CommitAsync();
        _logger.LogInformation("套装信息写入完成: {Count} 条", setItems.Count());
    }

    public async Task<Dictionary<int, SetItemInfo>> GetAllSetItemsAsync()
    {
        const string sql = "SELECT setitem_id, data_json FROM dim_setitems";
        using var conn = _connectionFactory.Create();
        var rows = await conn.QueryAsync<(int setitem_id, string data_json)>(sql);

        var result = new Dictionary<int, SetItemInfo>();
        foreach (var (id, json) in rows)
        {
            var info = JsonSerializer.Deserialize<SetItemInfo>(json);
            if (info != null)
                result[id] = info;
        }
        _logger.LogInformation("套装信息加载完成: {Count} 条", result.Count);
        return result;
    }

    public async Task BulkUpsertSkillsAsync(IEnumerable<SkillEntity> skills)
    {
        const string sql = """
            INSERT INTO dim_skills (
                skill_id, name, description, job_id, max_level,
                icon_data, is_hidden, level_effects, extracted_at
            ) VALUES (
                @skill_id, @name, @description, @job_id, @max_level,
                @icon_data, @is_hidden, @level_effects, @extracted_at
            )
            ON CONFLICT(skill_id) DO UPDATE SET
                name=excluded.name, description=excluded.description,
                job_id=excluded.job_id, max_level=excluded.max_level,
                icon_data=excluded.icon_data, is_hidden=excluded.is_hidden,
                level_effects=excluded.level_effects,
                extracted_at=excluded.extracted_at
            """;

        _logger.LogInformation("开始批量写入技能 {Count} 条...", skills.Count());
        using var conn = _connectionFactory.Create();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var rows = skills.Select(SkillRow.FromEntity).ToList();
        await conn.ExecuteAsync(sql, rows, transaction: tx);

        await tx.CommitAsync();
        _logger.LogInformation("技能写入完成");
    }

    public async Task<IReadOnlyList<SkillEntity>> SearchSkillsByNameAsync(string keyword, int limit = 50)
    {
        const string sql = "SELECT * FROM dim_skills WHERE name LIKE @Keyword LIMIT @Limit";
        using var conn = _connectionFactory.Create();
        var rows = await conn.QueryAsync<SkillRow>(sql, new { Keyword = $"%{keyword}%", Limit = limit });
        return rows.Select(r => r.ToEntity()).ToList();
    }

    /// <summary>
    /// 内部行模型 — 属性名与数据库列名、SQL 参数名对齐
    /// </summary>
    private class ItemRow
    {
        public int item_id { get; set; }
        public string name { get; set; } = "";
        public string? description { get; set; }
        public string category { get; set; } = "";
        public string? sub_category { get; set; }
        public int? req_level { get; set; }
        public int? req_str { get; set; }
        public int? req_dex { get; set; }
        public int? req_int { get; set; }
        public int? req_luk { get; set; }
        public int? inc_str { get; set; }
        public int? inc_dex { get; set; }
        public int? inc_int { get; set; }
        public int? inc_luk { get; set; }
        public int? inc_pad { get; set; }
        public int? inc_mad { get; set; }
        public int? inc_pdd { get; set; }
        public int? inc_mdd { get; set; }
        public int? inc_mhp { get; set; }
        public int? inc_mmp { get; set; }
        public string? dynamic_stats { get; set; }
        public string? consume_spec { get; set; }
        public int is_cash { get; set; }
        public int? price { get; set; }
        public byte[]? icon_data { get; set; }
        public byte[]? preview_data { get; set; }
        public int? setitem_id { get; set; }
        public string extracted_at { get; set; } = "";

        public ItemEntity ToEntity() => new()
        {
            ItemId = item_id,
            Name = name,
            Description = description,
            Category = Enum.TryParse<ItemCategory>(category, out var cat) ? cat : ItemCategory.Etc,
            SubCategory = sub_category,
            ReqLevel = req_level,
            ReqStr = req_str,
            ReqDex = req_dex,
            ReqInt = req_int,
            ReqLuk = req_luk,
            IncSTR = inc_str,
            IncDEX = inc_dex,
            IncINT = inc_int,
            IncLUK = inc_luk,
            IncPAD = inc_pad,
            IncMAD = inc_mad,
            IncPDD = inc_pdd,
            IncMDD = inc_mdd,
            IncMHP = inc_mhp,
            IncMMP = inc_mmp,
            DynamicStats = dynamic_stats,
            ConsumeSpec = consume_spec,
            IsCash = is_cash != 0,
            Price = price,
            IconData = icon_data,
            PreviewData = preview_data,
            SetItemId = setitem_id,
            ExtractedAt = DateTime.TryParse(extracted_at, out var dt) ? dt : DateTime.MinValue,
        };

        public static ItemRow FromEntity(ItemEntity e) => new()
        {
            item_id = e.ItemId,
            name = e.Name,
            description = e.Description,
            category = e.Category.ToString(),
            sub_category = e.SubCategory,
            req_level = e.ReqLevel,
            req_str = e.ReqStr,
            req_dex = e.ReqDex,
            req_int = e.ReqInt,
            req_luk = e.ReqLuk,
            inc_str = e.IncSTR,
            inc_dex = e.IncDEX,
            inc_int = e.IncINT,
            inc_luk = e.IncLUK,
            inc_pad = e.IncPAD,
            inc_mad = e.IncMAD,
            inc_pdd = e.IncPDD,
            inc_mdd = e.IncMDD,
            inc_mhp = e.IncMHP,
            inc_mmp = e.IncMMP,
            dynamic_stats = e.DynamicStats,
            consume_spec = e.ConsumeSpec,
            is_cash = e.IsCash ? 1 : 0,
            price = e.Price,
            icon_data = e.IconData,
            preview_data = e.PreviewData,
            setitem_id = e.SetItemId,
            extracted_at = e.ExtractedAt.ToString("O"),
        };
    }

    /// <summary>
    /// 技能行模型 — 属性名与数据库列名对齐
    /// </summary>
    private class SkillRow
    {
        public int skill_id { get; set; }
        public string name { get; set; } = "";
        public string? description { get; set; }
        public int job_id { get; set; }
        public int max_level { get; set; }
        public byte[]? icon_data { get; set; }
        public int is_hidden { get; set; }
        public string? level_effects { get; set; }
        public string extracted_at { get; set; } = "";

        public SkillEntity ToEntity() => new()
        {
            SkillId = skill_id,
            Name = name,
            Description = description,
            JobId = job_id,
            MaxLevel = max_level,
            IconData = icon_data,
            IsHidden = is_hidden != 0,
            LevelEffectsJson = level_effects,
            ExtractedAt = DateTime.TryParse(extracted_at, out var dt) ? dt : DateTime.MinValue,
        };

        public static SkillRow FromEntity(SkillEntity e) => new()
        {
            skill_id = e.SkillId,
            name = e.Name,
            description = e.Description,
            job_id = e.JobId,
            max_level = e.MaxLevel,
            icon_data = e.IconData,
            is_hidden = e.IsHidden ? 1 : 0,
            level_effects = e.LevelEffectsJson,
            extracted_at = e.ExtractedAt.ToString("O"),
        };
    }
}
