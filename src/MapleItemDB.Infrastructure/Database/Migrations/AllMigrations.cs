using MapleItemDB.Infrastructure.Repositories.Shared;
using Microsoft.Data.Sqlite;

namespace MapleItemDB.Infrastructure.Database.Migrations;

/// <summary>
/// 迁移 001: dim_items 新增 setitem_id 列
/// </summary>
public class Migration001_AddSetItemId : IDbMigration
{
    public int Version => 1;
    public string Description => "dim_items 新增 setitem_id 列";

    public async Task ExecuteAsync(SqliteConnection conn)
    {
        await SafeAlterAsync(conn, "ALTER TABLE dim_items ADD COLUMN setitem_id INTEGER;");
    }

    private static async Task SafeAlterAsync(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            // 列已存在
        }
    }
}

/// <summary>
/// 迁移 002: dim_items 新增 icon_data, preview_data (BLOB) 列
/// </summary>
public class Migration002_AddBlobColumns : IDbMigration
{
    public int Version => 2;
    public string Description => "dim_items 新增 icon_data/preview_data BLOB 列";

    public async Task ExecuteAsync(SqliteConnection conn)
    {
        await SafeAlterAsync(conn, "ALTER TABLE dim_items ADD COLUMN icon_data BLOB;");
        await SafeAlterAsync(conn, "ALTER TABLE dim_items ADD COLUMN preview_data BLOB;");
    }

    private static async Task SafeAlterAsync(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            // 列已存在
        }
    }
}

/// <summary>
/// 迁移 003: dim_skills 新增 skill_h, common_props 列
/// </summary>
public class Migration003_AddSkillColumns : IDbMigration
{
    public int Version => 3;
    public string Description => "dim_skills 新增 skill_h/common_props 列";

    public async Task ExecuteAsync(SqliteConnection conn)
    {
        await SafeAlterAsync(conn, "ALTER TABLE dim_skills ADD COLUMN skill_h TEXT;");
        await SafeAlterAsync(conn, "ALTER TABLE dim_skills ADD COLUMN common_props TEXT;");
    }

    private static async Task SafeAlterAsync(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            // 列已存在
        }
    }
}

/// <summary>
/// 迁移 004: dim_items 新增 sn 列
/// </summary>
public class Migration004_AddSnColumn : IDbMigration
{
    public int Version => 4;
    public string Description => "dim_items 新增 sn 列";

    public async Task ExecuteAsync(SqliteConnection conn)
    {
        await SafeAlterAsync(conn, "ALTER TABLE dim_items ADD COLUMN sn INTEGER;");
    }

    private static async Task SafeAlterAsync(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            // 列已存在
        }
    }
}

/// <summary>
/// 迁移 005: dim_items 新增 time_limited 列
/// </summary>
public class Migration005_AddTimeLimited : IDbMigration
{
    public int Version => 5;
    public string Description => "dim_items 新增 time_limited 列";

    public async Task ExecuteAsync(SqliteConnection conn)
    {
        await SafeAlterAsync(conn, "ALTER TABLE dim_items ADD COLUMN time_limited INTEGER DEFAULT 0;");
    }

    private static async Task SafeAlterAsync(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            // 列已存在
        }
    }
}

/// <summary>
/// 迁移 006: dim_items 新增 req_job 列
/// </summary>
public class Migration006_AddReqJob : IDbMigration
{
    public int Version => 6;
    public string Description => "dim_items 新增 req_job 列";

    public async Task ExecuteAsync(SqliteConnection conn)
    {
        await SafeAlterAsync(conn, "ALTER TABLE dim_items ADD COLUMN req_job INTEGER;");
    }

    private static async Task SafeAlterAsync(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            // 列已存在
        }
    }
}

/// <summary>
/// 迁移 007: 将图片 BLOB 拆分到独立资源表并进行哈希去重
/// </summary>
public class Migration007_SplitBlobAssetsWithHashDedup : IDbMigration
{
    public int Version => 7;
    public string Description => "拆分 dim_items/dim_skills BLOB 到 dim_blob_assets，并按 hash+length 去重";

    public async Task ExecuteAsync(SqliteConnection conn)
    {
        var hasItemIconData = await ColumnExistsAsync(conn, "dim_items", "icon_data");
        var hasItemPreviewData = await ColumnExistsAsync(conn, "dim_items", "preview_data");
        var hasSkillIconData = await ColumnExistsAsync(conn, "dim_skills", "icon_data");

        await EnsureBlobAssetsTableAsync(conn);

        if (!hasItemIconData && !hasItemPreviewData && !hasSkillIconData)
        {
            await EnsureIndexesAsync(conn);
            return;
        }

        await MigrateBlobAssetsAsync(conn, hasItemIconData, hasItemPreviewData, hasSkillIconData);

        if (hasItemIconData || hasItemPreviewData)
        {
            await RebuildDimItemsAsync(conn, hasItemIconData, hasItemPreviewData);
        }

        if (hasSkillIconData)
        {
            await RebuildDimSkillsAsync(conn);
        }

        await EnsureIndexesAsync(conn);
    }

    private static async Task EnsureBlobAssetsTableAsync(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS dim_blob_assets (
                blob_id         INTEGER PRIMARY KEY,
                content_hash    BLOB NOT NULL,
                content_length  INTEGER NOT NULL,
                blob_data       BLOB NOT NULL,
                created_at      TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS idx_blob_hash_len ON dim_blob_assets(content_hash, content_length);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task MigrateBlobAssetsAsync(
        SqliteConnection conn,
        bool hasItemIconData,
        bool hasItemPreviewData,
        bool hasSkillIconData)
    {
        var blobCandidates = new Dictionary<string, BlobAssetCandidate>(StringComparer.Ordinal);

        if (hasItemIconData && hasItemPreviewData)
        {
            await CollectBlobCandidatesAsync(conn,
                "SELECT icon_data, preview_data FROM dim_items WHERE icon_data IS NOT NULL OR preview_data IS NOT NULL",
                reader =>
                {
                    if (!reader.IsDBNull(0))
                    {
                        _ = BlobAssetHelper.AddCandidate((byte[])reader[0], blobCandidates);
                    }

                    if (!reader.IsDBNull(1))
                    {
                        _ = BlobAssetHelper.AddCandidate((byte[])reader[1], blobCandidates);
                    }
                });
        }
        else if (hasItemIconData)
        {
            await CollectBlobCandidatesAsync(conn,
                "SELECT icon_data FROM dim_items WHERE icon_data IS NOT NULL",
                reader =>
                {
                    if (!reader.IsDBNull(0))
                    {
                        _ = BlobAssetHelper.AddCandidate((byte[])reader[0], blobCandidates);
                    }
                });
        }
        else if (hasItemPreviewData)
        {
            await CollectBlobCandidatesAsync(conn,
                "SELECT preview_data FROM dim_items WHERE preview_data IS NOT NULL",
                reader =>
                {
                    if (!reader.IsDBNull(0))
                    {
                        _ = BlobAssetHelper.AddCandidate((byte[])reader[0], blobCandidates);
                    }
                });
        }

        if (hasSkillIconData)
        {
            await CollectBlobCandidatesAsync(conn,
                "SELECT icon_data FROM dim_skills WHERE icon_data IS NOT NULL",
                reader =>
                {
                    if (!reader.IsDBNull(0))
                    {
                        _ = BlobAssetHelper.AddCandidate((byte[])reader[0], blobCandidates);
                    }
                });
        }

        using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();
        await BlobAssetHelper.UpsertAndResolveBlobIdsAsync(conn, tx, blobCandidates.Values);
        await tx.CommitAsync();
    }

    private static async Task CollectBlobCandidatesAsync(
        SqliteConnection conn,
        string sql,
        Action<SqliteDataReader> collector)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            collector(reader);
        }
    }

    private static async Task RebuildDimItemsAsync(
        SqliteConnection conn,
        bool hasItemIconData,
        bool hasItemPreviewData)
    {
        using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

        using (var createCmd = conn.CreateCommand())
        {
            createCmd.Transaction = tx;
            createCmd.CommandText = """
                CREATE TABLE dim_items_new (
                    item_id          INTEGER PRIMARY KEY,
                    name             TEXT NOT NULL,
                    description      TEXT,
                    category         TEXT NOT NULL,
                    sub_category     TEXT,
                    req_level        INTEGER,
                    req_str          INTEGER,
                    req_dex          INTEGER,
                    req_int          INTEGER,
                    req_luk          INTEGER,
                    req_job          INTEGER,
                    inc_str          INTEGER,
                    inc_dex          INTEGER,
                    inc_int          INTEGER,
                    inc_luk          INTEGER,
                    inc_pad          INTEGER,
                    inc_mad          INTEGER,
                    inc_pdd          INTEGER,
                    inc_mdd          INTEGER,
                    inc_mhp          INTEGER,
                    inc_mmp          INTEGER,
                    dynamic_stats    TEXT,
                    consume_spec     TEXT,
                    is_cash          INTEGER DEFAULT 0,
                    price            INTEGER,
                    icon_blob_id     INTEGER,
                    preview_blob_id  INTEGER,
                    setitem_id       INTEGER,
                    sn               INTEGER,
                    time_limited     INTEGER DEFAULT 0,
                    extracted_at     TEXT NOT NULL,
                    FOREIGN KEY(icon_blob_id) REFERENCES dim_blob_assets(blob_id),
                    FOREIGN KEY(preview_blob_id) REFERENCES dim_blob_assets(blob_id)
                );
                """;
            await createCmd.ExecuteNonQueryAsync();
        }

        var iconExpr = "NULL";
        var previewExpr = "NULL";
        var joins = new List<string>();

        if (hasItemIconData)
        {
            iconExpr = "ba_icon.blob_id";
            joins.Add("LEFT JOIN dim_blob_assets ba_icon ON ba_icon.blob_data = i.icon_data");
        }

        if (hasItemPreviewData)
        {
            previewExpr = "ba_preview.blob_id";
            joins.Add("LEFT JOIN dim_blob_assets ba_preview ON ba_preview.blob_data = i.preview_data");
        }

        var joinSql = joins.Count > 0 ? "\n" + string.Join("\n", joins) : string.Empty;

        using (var copyCmd = conn.CreateCommand())
        {
            copyCmd.Transaction = tx;
            copyCmd.CommandText = $"""
                INSERT INTO dim_items_new (
                    item_id, name, description, category, sub_category,
                    req_level, req_str, req_dex, req_int, req_luk, req_job,
                    inc_str, inc_dex, inc_int, inc_luk,
                    inc_pad, inc_mad, inc_pdd, inc_mdd, inc_mhp, inc_mmp,
                    dynamic_stats, consume_spec,
                    is_cash, price, icon_blob_id, preview_blob_id, setitem_id, sn, time_limited, extracted_at
                )
                SELECT
                    i.item_id, i.name, i.description, i.category, i.sub_category,
                    i.req_level, i.req_str, i.req_dex, i.req_int, i.req_luk, i.req_job,
                    i.inc_str, i.inc_dex, i.inc_int, i.inc_luk,
                    i.inc_pad, i.inc_mad, i.inc_pdd, i.inc_mdd, i.inc_mhp, i.inc_mmp,
                    i.dynamic_stats, i.consume_spec,
                    i.is_cash, i.price,
                    {iconExpr},
                    {previewExpr},
                    i.setitem_id, i.sn, i.time_limited, i.extracted_at
                FROM dim_items i{joinSql};
                """;
            await copyCmd.ExecuteNonQueryAsync();
        }

        using (var swapCmd = conn.CreateCommand())
        {
            swapCmd.Transaction = tx;
            swapCmd.CommandText = """
                DROP TABLE dim_items;
                ALTER TABLE dim_items_new RENAME TO dim_items;
                """;
            await swapCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    private static async Task RebuildDimSkillsAsync(SqliteConnection conn)
    {
        using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

        using (var createCmd = conn.CreateCommand())
        {
            createCmd.Transaction = tx;
            createCmd.CommandText = """
                CREATE TABLE dim_skills_new (
                    skill_id       INTEGER PRIMARY KEY,
                    name           TEXT NOT NULL,
                    description    TEXT,
                    job_id         INTEGER NOT NULL,
                    max_level      INTEGER DEFAULT 0,
                    icon_blob_id   INTEGER,
                    is_hidden      INTEGER DEFAULT 0,
                    level_effects  TEXT,
                    skill_h        TEXT,
                    common_props   TEXT,
                    extracted_at   TEXT NOT NULL,
                    FOREIGN KEY(icon_blob_id) REFERENCES dim_blob_assets(blob_id)
                );
                """;
            await createCmd.ExecuteNonQueryAsync();
        }

        using (var copyCmd = conn.CreateCommand())
        {
            copyCmd.Transaction = tx;
            copyCmd.CommandText = """
                INSERT INTO dim_skills_new (
                    skill_id, name, description, job_id, max_level,
                    icon_blob_id, is_hidden, level_effects, skill_h, common_props, extracted_at
                )
                SELECT
                    s.skill_id, s.name, s.description, s.job_id, s.max_level,
                    ba_icon.blob_id,
                    s.is_hidden, s.level_effects, s.skill_h, s.common_props, s.extracted_at
                FROM dim_skills s
                LEFT JOIN dim_blob_assets ba_icon ON ba_icon.blob_data = s.icon_data;
                """;
            await copyCmd.ExecuteNonQueryAsync();
        }

        using (var swapCmd = conn.CreateCommand())
        {
            swapCmd.Transaction = tx;
            swapCmd.CommandText = """
                DROP TABLE dim_skills;
                ALTER TABLE dim_skills_new RENAME TO dim_skills;
                """;
            await swapCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    private static async Task EnsureIndexesAsync(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE INDEX IF NOT EXISTS idx_items_name ON dim_items(name);
            CREATE INDEX IF NOT EXISTS idx_items_category ON dim_items(category, sub_category);
            CREATE INDEX IF NOT EXISTS idx_items_cash ON dim_items(is_cash) WHERE is_cash = 1;
            CREATE INDEX IF NOT EXISTS idx_items_icon_blob_id ON dim_items(icon_blob_id);
            CREATE INDEX IF NOT EXISTS idx_items_preview_blob_id ON dim_items(preview_blob_id);

            CREATE INDEX IF NOT EXISTS idx_skills_name ON dim_skills(name);
            CREATE INDEX IF NOT EXISTS idx_skills_job ON dim_skills(job_id);
            CREATE INDEX IF NOT EXISTS idx_skills_icon_blob_id ON dim_skills(icon_blob_id);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection conn, string tableName, string columnName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName})";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// 迁移 008: 新增 dim_item_sn_map 独立 SN 映射表
/// </summary>
public class Migration008_AddItemSnMapTable : IDbMigration
{
    public int Version => 8;
    public string Description => "新增 dim_item_sn_map 独立 SN 映射表";

    public async Task ExecuteAsync(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS dim_item_sn_map (
                item_id       INTEGER PRIMARY KEY,
                sn            INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_item_sn_map_sn ON dim_item_sn_map(sn);
            """;
        await cmd.ExecuteNonQueryAsync();
    }
}
