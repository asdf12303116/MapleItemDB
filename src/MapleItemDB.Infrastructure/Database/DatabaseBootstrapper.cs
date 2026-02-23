using Microsoft.Data.Sqlite;

namespace MapleItemDB.Infrastructure.Database;

/// <summary>
/// 数据库初始化器 — 创建表结构和索引
/// </summary>
public class DatabaseBootstrapper
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public DatabaseBootstrapper(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// 确保数据库和表结构已创建
    /// </summary>
    public async Task EnsureCreatedAsync()
    {
        using var conn = _connectionFactory.Create();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = Schema;
        await cmd.ExecuteNonQueryAsync();

        // 安全迁移: 添加 set_item_id 列（已存在则忽略）
        await SafeAddColumnAsync(conn, "ALTER TABLE dim_items ADD COLUMN setitem_id INTEGER;");

        // 安全迁移: icon_path/preview_path → icon_data/preview_data (BLOB)
        await SafeAddColumnAsync(conn, "ALTER TABLE dim_items ADD COLUMN icon_data BLOB;");
        await SafeAddColumnAsync(conn, "ALTER TABLE dim_items ADD COLUMN preview_data BLOB;");

        // 安全迁移: dim_skills 新增 skill_h, common_props 列
        await SafeAddColumnAsync(conn, "ALTER TABLE dim_skills ADD COLUMN skill_h TEXT;");
        await SafeAddColumnAsync(conn, "ALTER TABLE dim_skills ADD COLUMN common_props TEXT;");

        // 安全迁移: dim_items 新增 sn 列
        await SafeAddColumnAsync(conn, "ALTER TABLE dim_items ADD COLUMN sn INTEGER;");

        // 安全迁移: dim_items 新增 time_limited 列
        await SafeAddColumnAsync(conn, "ALTER TABLE dim_items ADD COLUMN time_limited INTEGER DEFAULT 0;");

        // 安全迁移: dim_items 新增 req_job 列 (职业需求位掩码)
        await SafeAddColumnAsync(conn, "ALTER TABLE dim_items ADD COLUMN req_job INTEGER;");

        // 启用 WAL 模式 (持久化设置，提升并发读写性能)
        using var walCmd = conn.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        await walCmd.ExecuteNonQueryAsync();
    }

    private static async Task SafeAddColumnAsync(Microsoft.Data.Sqlite.SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // 列已存在，忽略错误
        }
    }

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS dim_items (
            item_id       INTEGER PRIMARY KEY,
            name          TEXT NOT NULL,
            description   TEXT,
            category      TEXT NOT NULL,
            sub_category  TEXT,

            -- 装备需求属性
            req_level     INTEGER,
            req_str       INTEGER,
            req_dex       INTEGER,
            req_int       INTEGER,
            req_luk       INTEGER,

            -- 装备核心数值
            inc_str       INTEGER,
            inc_dex       INTEGER,
            inc_int       INTEGER,
            inc_luk       INTEGER,
            inc_pad       INTEGER,
            inc_mad       INTEGER,
            inc_pdd       INTEGER,
            inc_mdd       INTEGER,
            inc_mhp       INTEGER,
            inc_mmp       INTEGER,

            -- 非结构化扩展 (JSON TEXT)
            dynamic_stats TEXT,
            consume_spec  TEXT,

            -- 元数据
            is_cash       INTEGER DEFAULT 0,
            price         INTEGER,
            icon_data     BLOB,
            preview_data  BLOB,
            extracted_at  TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_items_name ON dim_items(name);
        CREATE INDEX IF NOT EXISTS idx_items_category ON dim_items(category, sub_category);
        CREATE INDEX IF NOT EXISTS idx_items_cash ON dim_items(is_cash) WHERE is_cash = 1;

        -- 套装信息表 (整条套装序列化为 JSON)
        CREATE TABLE IF NOT EXISTS dim_setitems (
            setitem_id    INTEGER PRIMARY KEY,
            name          TEXT NOT NULL,
            data_json     TEXT NOT NULL
        );

        -- 技能信息表
        CREATE TABLE IF NOT EXISTS dim_skills (
            skill_id       INTEGER PRIMARY KEY,
            name           TEXT NOT NULL,
            description    TEXT,
            job_id         INTEGER NOT NULL,
            max_level      INTEGER DEFAULT 0,
            icon_data      BLOB,
            is_hidden      INTEGER DEFAULT 0,
            level_effects  TEXT,
            skill_h        TEXT,
            common_props   TEXT,
            extracted_at   TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_skills_name ON dim_skills(name);
        CREATE INDEX IF NOT EXISTS idx_skills_job ON dim_skills(job_id);
        """;
}
