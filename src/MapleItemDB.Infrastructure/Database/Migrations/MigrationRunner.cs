using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Infrastructure.Database.Migrations;

/// <summary>
/// 迁移执行器 — 管理 _migrations 表，按编号顺序执行未执行的迁移
/// </summary>
public class MigrationRunner
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly IReadOnlyList<IDbMigration> _migrations;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(
        SqliteConnectionFactory connectionFactory,
        IEnumerable<IDbMigration> migrations,
        ILogger<MigrationRunner> logger)
    {
        _connectionFactory = connectionFactory;
        _migrations = migrations.OrderBy(m => m.Version).ToList();
        _logger = logger;
    }

    /// <summary>
    /// 执行所有未执行的迁移
    /// </summary>
    public async Task RunAsync()
    {
        using var conn = _connectionFactory.Create();
        await conn.OpenAsync();

        await EnsureMigrationTableAsync(conn);
        var applied = await GetAppliedVersionsAsync(conn);

        foreach (var migration in _migrations)
        {
            if (applied.Contains(migration.Version))
            {
                _logger.LogDebug("迁移 {Version} 已执行，跳过: {Description}", migration.Version, migration.Description);
                continue;
            }

            _logger.LogInformation("执行迁移 {Version}: {Description}", migration.Version, migration.Description);
            await migration.ExecuteAsync(conn);
            await RecordMigrationAsync(conn, migration);
            _logger.LogInformation("迁移 {Version} 完成", migration.Version);
        }
    }

    private static async Task EnsureMigrationTableAsync(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS _migrations (
                version     INTEGER PRIMARY KEY,
                description TEXT NOT NULL,
                applied_at  TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<HashSet<int>> GetAppliedVersionsAsync(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT version FROM _migrations";
        var result = new HashSet<int>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetInt32(0));
        }
        return result;
    }

    private static async Task RecordMigrationAsync(SqliteConnection conn, IDbMigration migration)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO _migrations (version, description, applied_at) VALUES (@v, @d, @t)";
        cmd.Parameters.AddWithValue("@v", migration.Version);
        cmd.Parameters.AddWithValue("@d", migration.Description);
        cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }
}
