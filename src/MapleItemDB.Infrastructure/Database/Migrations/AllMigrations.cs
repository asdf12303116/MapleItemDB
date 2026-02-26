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
        catch (SqliteException) { /* 列已存在 */ }
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
        catch (SqliteException) { }
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
        catch (SqliteException) { }
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
        catch (SqliteException) { }
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
        catch (SqliteException) { }
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
        catch (SqliteException) { }
    }
}
