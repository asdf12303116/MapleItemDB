using MapleItemDB.Infrastructure.Database.Migrations;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Infrastructure.Database;

/// <summary>
/// 数据库初始化器：创建基础表结构并执行编号迁移
/// </summary>
public class DatabaseBootstrapper
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly MigrationRunner _migrationRunner;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(
        SqliteConnectionFactory connectionFactory,
        MigrationRunner migrationRunner,
        ILogger<DatabaseBootstrapper> logger)
    {
        _connectionFactory = connectionFactory;
        _migrationRunner = migrationRunner;
        _logger = logger;
    }

    /// <summary>
    /// 确保数据库和表结构已创建，并执行所有未执行的迁移
    /// </summary>
    public async Task EnsureCreatedAsync()
    {
        _logger.LogInformation("开始初始化数据库...");

        using var conn = _connectionFactory.Create();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = Schema;
        await cmd.ExecuteNonQueryAsync();

        using var walCmd = conn.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        await walCmd.ExecuteNonQueryAsync();

        _logger.LogInformation("基础表结构已就绪，开始执行迁移...");

        await _migrationRunner.RunAsync();

        _logger.LogInformation("数据库初始化完成");
    }

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS dim_blob_assets (
            blob_id         INTEGER PRIMARY KEY,
            content_hash    BLOB NOT NULL,
            content_length  INTEGER NOT NULL,
            blob_data       BLOB NOT NULL,
            created_at      TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS idx_blob_hash_len ON dim_blob_assets(content_hash, content_length);

        CREATE TABLE IF NOT EXISTS dim_items (
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

        CREATE INDEX IF NOT EXISTS idx_items_name ON dim_items(name);
        CREATE INDEX IF NOT EXISTS idx_items_category ON dim_items(category, sub_category);
        CREATE INDEX IF NOT EXISTS idx_items_cash ON dim_items(is_cash) WHERE is_cash = 1;

        CREATE TABLE IF NOT EXISTS dim_setitems (
            setitem_id    INTEGER PRIMARY KEY,
            name          TEXT NOT NULL,
            data_json     TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS dim_skills (
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

        CREATE INDEX IF NOT EXISTS idx_skills_name ON dim_skills(name);
        CREATE INDEX IF NOT EXISTS idx_skills_job ON dim_skills(job_id);
        """;
}
