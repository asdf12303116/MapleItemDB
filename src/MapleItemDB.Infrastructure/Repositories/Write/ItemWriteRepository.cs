using Dapper;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Repositories.Shared;
using Microsoft.Extensions.Logging;
using static MapleItemDB.Infrastructure.Repositories.Shared.RepositoryHelper;

namespace MapleItemDB.Infrastructure.Repositories.Write;

/// <summary>
/// 道具写仓储：批量写入
/// </summary>
public class ItemWriteRepository : IItemWriteRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<ItemWriteRepository> _logger;

    public ItemWriteRepository(SqliteConnectionFactory connectionFactory, ILogger<ItemWriteRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task BulkUpsertAsync(IEnumerable<ItemEntity> items,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dim_items (
                item_id, name, description, category, sub_category,
                req_level, req_str, req_dex, req_int, req_luk, req_job,
                inc_str, inc_dex, inc_int, inc_luk,
                inc_pad, inc_mad, inc_pdd, inc_mdd, inc_mhp, inc_mmp,
                dynamic_stats, consume_spec,
                is_cash, price, icon_blob_id, preview_blob_id, setitem_id, sn, time_limited, extracted_at
            ) VALUES (
                @item_id, @name, @description, @category, @sub_category,
                @req_level, @req_str, @req_dex, @req_int, @req_luk, @req_job,
                @inc_str, @inc_dex, @inc_int, @inc_luk,
                @inc_pad, @inc_mad, @inc_pdd, @inc_mdd, @inc_mhp, @inc_mmp,
                @dynamic_stats, @consume_spec,
                @is_cash, @price, @icon_blob_id, @preview_blob_id, @setitem_id, @sn, @time_limited, @extracted_at
            )
            ON CONFLICT(item_id) DO UPDATE SET
                name=excluded.name, description=excluded.description,
                category=excluded.category, sub_category=excluded.sub_category,
                req_level=excluded.req_level, req_str=excluded.req_str,
                req_dex=excluded.req_dex, req_int=excluded.req_int, req_luk=excluded.req_luk,
                req_job=excluded.req_job,
                inc_str=excluded.inc_str, inc_dex=excluded.inc_dex,
                inc_int=excluded.inc_int, inc_luk=excluded.inc_luk,
                inc_pad=excluded.inc_pad, inc_mad=excluded.inc_mad,
                inc_pdd=excluded.inc_pdd, inc_mdd=excluded.inc_mdd,
                inc_mhp=excluded.inc_mhp, inc_mmp=excluded.inc_mmp,
                dynamic_stats=excluded.dynamic_stats, consume_spec=excluded.consume_spec,
                is_cash=excluded.is_cash, price=excluded.price,
                icon_blob_id=excluded.icon_blob_id, preview_blob_id=excluded.preview_blob_id,
                setitem_id=excluded.setitem_id, sn=excluded.sn,
                time_limited=excluded.time_limited,
                extracted_at=excluded.extracted_at
            """;

        var rows = items.Select(ItemRow.FromEntity).ToList();
        _logger.LogInformation("开始批量写入 {Count} 条道具记录...", rows.Count);

        using var conn = _connectionFactory.Create();
        await conn.OpenAsync(cancellationToken);
        ApplyWritePragmas(conn);

        int written = 0;
        foreach (var batch in Chunk(rows, 1000))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var tx = await conn.BeginTransactionAsync(cancellationToken);

            var blobCandidates = new Dictionary<string, BlobAssetCandidate>(StringComparer.Ordinal);
            var blobLookupByRow = new Dictionary<ItemRow, (string? IconKey, string? PreviewKey)>();

            foreach (var row in batch)
            {
                var iconKey = BlobAssetHelper.AddCandidate(row.icon_data, blobCandidates);
                var previewKey = BlobAssetHelper.AddCandidate(row.preview_data, blobCandidates);
                blobLookupByRow[row] = (iconKey, previewKey);
            }

            var blobIdMap = await BlobAssetHelper.UpsertAndResolveBlobIdsAsync(
                conn,
                tx,
                blobCandidates.Values,
                cancellationToken);

            foreach (var row in batch)
            {
                var keys = blobLookupByRow[row];
                row.icon_blob_id = BlobAssetHelper.TryGetBlobId(keys.IconKey, blobIdMap);
                row.preview_blob_id = BlobAssetHelper.TryGetBlobId(keys.PreviewKey, blobIdMap);
                row.icon_data = null;
                row.preview_data = null;
            }

            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                batch,
                transaction: tx,
                cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            written += batch.Count;
            progress?.Report((written, rows.Count));
        }

        RestoreDefaultPragmas(conn);
        _logger.LogInformation("批量写入完成");
    }
}
