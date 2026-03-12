using Dapper;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Repositories.Shared;
using Microsoft.Extensions.Logging;
using static MapleItemDB.Infrastructure.Repositories.Shared.RepositoryHelper;

namespace MapleItemDB.Infrastructure.Repositories.Write;

/// <summary>
/// SN 映射写仓储
/// </summary>
public sealed class ItemSnRepository : IItemSnRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<ItemSnRepository> _logger;

    public ItemSnRepository(SqliteConnectionFactory connectionFactory, ILogger<ItemSnRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT item_id, sn FROM dim_item_sn_map";
        using var conn = _connectionFactory.Create();
        var rows = await conn.QueryAsync<(int item_id, int sn)>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToDictionary(x => x.item_id, x => x.sn);
    }

    public async Task ReplaceAllAsync(IReadOnlyDictionary<int, int> map, CancellationToken cancellationToken = default)
    {
        const string deleteSql = "DELETE FROM dim_item_sn_map";
        const string insertSql = "INSERT INTO dim_item_sn_map (item_id, sn) VALUES (@item_id, @sn)";

        using var conn = _connectionFactory.Create();
        await conn.OpenAsync(cancellationToken);
        ApplyWritePragmas(conn);

        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        await conn.ExecuteAsync(new CommandDefinition(deleteSql, transaction: tx, cancellationToken: cancellationToken));

        if (map.Count > 0)
        {
            var rows = map.Select(x => new { item_id = x.Key, sn = x.Value }).ToList();
            foreach (var batch in Chunk(rows, 2000))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await conn.ExecuteAsync(new CommandDefinition(insertSql, batch, transaction: tx, cancellationToken: cancellationToken));
            }
        }

        await tx.CommitAsync(cancellationToken);
        RestoreDefaultPragmas(conn);

        _logger.LogInformation("SN 映射全量写入完成: {Count}", map.Count);
    }

    public async Task<int> ApplyToItemsAsync(CancellationToken cancellationToken = default)
    {
        const string clearSql = "UPDATE dim_items SET sn = NULL";
        const string applySql = """
            UPDATE dim_items
            SET sn = (
                SELECT m.sn
                FROM dim_item_sn_map m
                WHERE m.item_id = dim_items.item_id
            )
            WHERE EXISTS (
                SELECT 1
                FROM dim_item_sn_map m
                WHERE m.item_id = dim_items.item_id
            )
            """;

        using var conn = _connectionFactory.Create();
        await conn.OpenAsync(cancellationToken);
        ApplyWritePragmas(conn);

        using var tx = await conn.BeginTransactionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(clearSql, transaction: tx, cancellationToken: cancellationToken));
        var updated = await conn.ExecuteAsync(new CommandDefinition(applySql, transaction: tx, cancellationToken: cancellationToken));
        await tx.CommitAsync(cancellationToken);

        RestoreDefaultPragmas(conn);

        _logger.LogInformation("SN 回填完成: {Updated} 行", updated);
        return updated;
    }
}
