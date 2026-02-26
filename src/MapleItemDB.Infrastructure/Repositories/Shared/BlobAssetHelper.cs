using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;

namespace MapleItemDB.Infrastructure.Repositories.Shared;

internal sealed record BlobAssetCandidate(
    string LookupKey,
    byte[] ContentHash,
    int ContentLength,
    byte[] BlobData);

internal static class BlobAssetHelper
{
    internal static string? AddCandidate(byte[]? blobData, Dictionary<string, BlobAssetCandidate> candidates)
    {
        if (blobData is not { Length: > 0 })
        {
            return null;
        }

        var contentHash = SHA256.HashData(blobData);
        var lookupKey = BuildLookupKey(contentHash, blobData.Length);

        if (!candidates.ContainsKey(lookupKey))
        {
            candidates[lookupKey] = new BlobAssetCandidate(
                lookupKey,
                contentHash,
                blobData.Length,
                blobData);
        }

        return lookupKey;
    }

    internal static async Task<Dictionary<string, int>> UpsertAndResolveBlobIdsAsync(
        SqliteConnection conn,
        DbTransaction tx,
        IReadOnlyCollection<BlobAssetCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        var blobIdMap = new Dictionary<string, int>(StringComparer.Ordinal);
        if (candidates.Count == 0)
        {
            return blobIdMap;
        }

        const string upsertSql = """
            INSERT OR IGNORE INTO dim_blob_assets (
                content_hash, content_length, blob_data, created_at
            ) VALUES (
                @content_hash, @content_length, @blob_data, @created_at
            )
            """;

        var now = DateTime.UtcNow.ToString("O");
        var upsertRows = candidates.Select(c => new
        {
            content_hash = c.ContentHash,
            content_length = c.ContentLength,
            blob_data = c.BlobData,
            created_at = now,
        });

        await conn.ExecuteAsync(new CommandDefinition(
            upsertSql,
            upsertRows,
            transaction: tx,
            cancellationToken: cancellationToken));

        var candidateList = candidates.ToList();
        foreach (var batch in RepositoryHelper.Chunk(candidateList, 300))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("WITH lookup(content_hash, content_length, lookup_key) AS (");
            sqlBuilder.Append("VALUES ");

            var parameters = new DynamicParameters();
            for (var i = 0; i < batch.Count; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }

                sqlBuilder.Append($"(@hash{i}, @length{i}, @key{i})");
                parameters.Add($"hash{i}", batch[i].ContentHash);
                parameters.Add($"length{i}", batch[i].ContentLength);
                parameters.Add($"key{i}", batch[i].LookupKey);
            }

            sqlBuilder.AppendLine(")");
            sqlBuilder.AppendLine("SELECT a.blob_id, l.lookup_key");
            sqlBuilder.AppendLine("FROM dim_blob_assets a");
            sqlBuilder.AppendLine("JOIN lookup l");
            sqlBuilder.AppendLine("  ON a.content_hash = l.content_hash");
            sqlBuilder.AppendLine(" AND a.content_length = l.content_length");

            var rows = await conn.QueryAsync<(int blob_id, string lookup_key)>(new CommandDefinition(
                sqlBuilder.ToString(),
                parameters,
                transaction: tx,
                cancellationToken: cancellationToken));

            foreach (var row in rows)
            {
                blobIdMap[row.lookup_key] = row.blob_id;
            }
        }

        return blobIdMap;
    }

    internal static int? TryGetBlobId(string? lookupKey, IReadOnlyDictionary<string, int> blobIdMap)
    {
        if (lookupKey is null)
        {
            return null;
        }

        return blobIdMap.TryGetValue(lookupKey, out var blobId)
            ? blobId
            : null;
    }

    internal static string BuildLookupKey(byte[] blobData)
    {
        var contentHash = SHA256.HashData(blobData);
        return BuildLookupKey(contentHash, blobData.Length);
    }

    internal static string BuildLookupKey(byte[] hash, int length) =>
        $"{Convert.ToHexString(hash)}:{length}";
}
