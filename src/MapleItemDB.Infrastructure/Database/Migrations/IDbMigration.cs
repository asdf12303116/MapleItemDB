namespace MapleItemDB.Infrastructure.Database.Migrations;

/// <summary>
/// 数据库迁移接口 — 每个迁移必须幂等
/// </summary>
public interface IDbMigration
{
    /// <summary>
    /// 迁移编号（唯一，按升序执行）
    /// </summary>
    int Version { get; }

    /// <summary>
    /// 迁移描述（记录到 _migrations 表）
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 执行迁移 SQL
    /// </summary>
    Task ExecuteAsync(Microsoft.Data.Sqlite.SqliteConnection conn);
}
