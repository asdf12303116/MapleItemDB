using Dapper;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Repositories.Shared;
using Microsoft.Extensions.Logging;
using static MapleItemDB.Infrastructure.Repositories.Shared.RepositoryHelper;

namespace MapleItemDB.Infrastructure.Repositories.Write;

/// <summary>
/// 技能写仓储：批量写入
/// </summary>
public class SkillWriteRepository : ISkillWriteRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<SkillWriteRepository> _logger;

    public SkillWriteRepository(SqliteConnectionFactory connectionFactory, ILogger<SkillWriteRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task BulkUpsertSkillsAsync(IEnumerable<SkillEntity> skills,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dim_skills (
                skill_id, name, description, job_id, max_level,
                icon_blob_id, is_hidden, level_effects, skill_h, common_props, extracted_at
            ) VALUES (
                @skill_id, @name, @description, @job_id, @max_level,
                @icon_blob_id, @is_hidden, @level_effects, @skill_h, @common_props, @extracted_at
            )
            ON CONFLICT(skill_id) DO UPDATE SET
                name=excluded.name, description=excluded.description,
                job_id=excluded.job_id, max_level=excluded.max_level,
                icon_blob_id=excluded.icon_blob_id, is_hidden=excluded.is_hidden,
                level_effects=excluded.level_effects,
                skill_h=excluded.skill_h, common_props=excluded.common_props,
                extracted_at=excluded.extracted_at
            """;

        var rows = skills.Select(SkillRow.FromEntity).ToList();
        _logger.LogInformation("开始批量写入技能 {Count} 条...", rows.Count);

        using var conn = _connectionFactory.Create();
        await conn.OpenAsync(cancellationToken);
        ApplyWritePragmas(conn);

        int written = 0;
        foreach (var batch in Chunk(rows, 500))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var tx = await conn.BeginTransactionAsync(cancellationToken);

            var blobCandidates = new Dictionary<string, BlobAssetCandidate>(StringComparer.Ordinal);
            var iconKeyByRow = new Dictionary<SkillRow, string?>();

            foreach (var row in batch)
            {
                var iconKey = BlobAssetHelper.AddCandidate(row.icon_data, blobCandidates);
                iconKeyByRow[row] = iconKey;
            }

            var blobIdMap = await BlobAssetHelper.UpsertAndResolveBlobIdsAsync(
                conn,
                tx,
                blobCandidates.Values,
                cancellationToken);

            foreach (var row in batch)
            {
                row.icon_blob_id = BlobAssetHelper.TryGetBlobId(iconKeyByRow[row], blobIdMap);
                row.icon_data = null;
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
        _logger.LogInformation("技能写入完成");
    }
}
