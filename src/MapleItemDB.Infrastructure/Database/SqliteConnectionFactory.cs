using Microsoft.Data.Sqlite;

namespace MapleItemDB.Infrastructure.Database;

/// <summary>
/// SQLite 连接工厂
/// </summary>
public class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// 从数据库文件路径创建工厂
    /// </summary>
    public static SqliteConnectionFactory FromFile(string dbFilePath)
    {
        return new SqliteConnectionFactory($"Data Source={dbFilePath}");
    }

    /// <summary>
    /// 创建并返回一个新的数据库连接
    /// </summary>
    public SqliteConnection Create()
    {
        return new SqliteConnection(_connectionString);
    }
}
