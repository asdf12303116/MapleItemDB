using System.Text.Json;
using Dapper;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Infrastructure.Repositories.Read;

/// <summary>
/// 套装读仓储
/// </summary>
public class SetItemReadRepository : ISetItemReadRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<SetItemReadRepository> _logger;

    public SetItemReadRepository(SqliteConnectionFactory connectionFactory, ILogger<SetItemReadRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
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
}
