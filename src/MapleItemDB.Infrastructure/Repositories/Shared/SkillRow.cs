using MapleItemDB.Core.Models;

namespace MapleItemDB.Infrastructure.Repositories.Shared;

/// <summary>
/// 技能行模型：属性名与数据库列名对齐
/// </summary>
internal class SkillRow
{
    public int skill_id { get; set; }
    public string name { get; set; } = "";
    public string? description { get; set; }
    public int job_id { get; set; }
    public int max_level { get; set; }
    public byte[]? icon_data { get; set; }
    public int? icon_blob_id { get; set; }
    public int is_hidden { get; set; }
    public string? level_effects { get; set; }
    public string? skill_h { get; set; }
    public string? common_props { get; set; }
    public string extracted_at { get; set; } = "";

    public SkillEntity ToEntity() => new()
    {
        SkillId = skill_id,
        Name = name,
        Description = description,
        JobId = job_id,
        MaxLevel = max_level,
        IconData = icon_data,
        IsHidden = is_hidden != 0,
        LevelEffectsJson = level_effects,
        SkillH = skill_h,
        CommonPropsJson = common_props,
        ExtractedAt = DateTime.TryParse(extracted_at, out var dt) ? dt : DateTime.MinValue,
    };

    public static SkillRow FromEntity(SkillEntity e) => new()
    {
        skill_id = e.SkillId,
        name = e.Name,
        description = e.Description,
        job_id = e.JobId,
        max_level = e.MaxLevel,
        icon_data = e.IconData,
        icon_blob_id = null,
        is_hidden = e.IsHidden ? 1 : 0,
        level_effects = e.LevelEffectsJson,
        skill_h = e.SkillH,
        common_props = e.CommonPropsJson,
        extracted_at = e.ExtractedAt.ToString("O"),
    };
}
