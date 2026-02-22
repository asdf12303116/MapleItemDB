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
        try
        {
            using var migrateCmd = conn.CreateCommand();
            migrateCmd.CommandText = "ALTER TABLE dim_items ADD COLUMN setitem_id INTEGER;";
            await migrateCmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // 列已存在，忽略错误
        }

        // 安全迁移: 添加 preview_path 列（已存在则忽略）
        try
        {
            using var migrateCmd2 = conn.CreateCommand();
            migrateCmd2.CommandText = "ALTER TABLE dim_items ADD COLUMN preview_path TEXT;";
            await migrateCmd2.ExecuteNonQueryAsync();
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
            icon_path     TEXT,
            extracted_at  TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_items_name ON dim_items(name);
        CREATE INDEX IF NOT EXISTS idx_items_category ON dim_items(category, sub_category);
        CREATE INDEX IF NOT EXISTS idx_items_cash ON dim_items(is_cash) WHERE is_cash = 1;
        """;
}
