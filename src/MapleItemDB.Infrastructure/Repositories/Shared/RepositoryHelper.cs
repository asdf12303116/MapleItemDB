using Microsoft.Data.Sqlite;

namespace MapleItemDB.Infrastructure.Repositories.Shared;

/// <summary>
/// 仓储共用辅助方法 — PRAGMA 优化与分批工具
/// </summary>
internal static class RepositoryHelper
{
    /// <summary>
    /// 写入前设置 PRAGMA 优化参数
    /// </summary>
    internal static void ApplyWritePragmas(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            PRAGMA synchronous = NORMAL;
            PRAGMA cache_size = -64000;
            PRAGMA temp_store = MEMORY;
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 写入完成后恢复默认 PRAGMA
    /// </summary>
    internal static void RestoreDefaultPragmas(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            PRAGMA synchronous = FULL;
            PRAGMA cache_size = -2000;
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 将列表按指定大小分批
    /// </summary>
    internal static IEnumerable<List<T>> Chunk<T>(List<T> source, int chunkSize)
    {
        for (int i = 0; i < source.Count; i += chunkSize)
        {
            yield return source.GetRange(i, Math.Min(chunkSize, source.Count - i));
        }
    }
}
