using System.Text.Json;
using Dapper;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Database;
using Microsoft.Extensions.Logging;
using static MapleItemDB.Infrastructure.Repositories.Shared.RepositoryHelper;

namespace MapleItemDB.Infrastructure.Repositories.Write;

/// <summary>
/// 套装写仓储 — 批量写入
/// </summary>
public class SetItemWriteRepository : ISetItemWriteRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<SetItemWriteRepository> _logger;

    public SetItemWriteRepository(SqliteConnectionFactory connectionFactory, ILogger<SetItemWriteRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task BulkUpsertSetItemsAsync(IEnumerable<SetItemInfo> setItems,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dim_setitems (setitem_id, name, data_json)
            VALUES (@setitem_id, @name, @data_json)
            ON CONFLICT(setitem_id) DO UPDATE SET
                name=excluded.name, data_json=excluded.data_json
            """;

        var rows = setItems.Select(s => new
        {
            setitem_id = s.SetItemId,
            name = s.SetItemName,
            data_json = JsonSerializer.Serialize(s),
        }).ToList();

        using var conn = _connectionFactory.Create();
        await conn.OpenAsync(cancellationToken);
        ApplyWritePragmas(conn);

        int written = 0;
        foreach (var batch in Chunk(rows, 1000))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await conn.ExecuteAsync(sql, batch, transaction: tx);
            await tx.CommitAsync(cancellationToken);
            written += batch.Count;
            progress?.Report((written, rows.Count));
        }

        RestoreDefaultPragmas(conn);
        _logger.LogInformation("套装信息写入完成: {Count} 条", rows.Count);
    }
}
