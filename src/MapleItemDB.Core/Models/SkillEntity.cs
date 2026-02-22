namespace MapleItemDB.Core.Models;

/// <summary>
/// 技能实体，映射 dim_skills 表
/// </summary>
public class SkillEntity
{
    /// <summary>技能 ID</summary>
    public int SkillId { get; set; }

    /// <summary>技能名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>技能描述</summary>
    public string? Description { get; set; }

    /// <summary>所属职业 ID</summary>
    public int JobId { get; set; }

    /// <summary>最大等级</summary>
    public int MaxLevel { get; set; }

    /// <summary>图标 PNG 二进制数据</summary>
    public byte[]? IconData { get; set; }

    /// <summary>是否隐藏技能</summary>
    public bool IsHidden { get; set; }

    /// <summary>各等级效果 JSON</summary>
    public string? LevelEffectsJson { get; set; }

    /// <summary>技能等级效果描述模板 (来自 String.wz 的 h 字段)</summary>
    public string? SkillH { get; set; }

    /// <summary>common 属性公式字典 JSON (如 {"mastery":"55+u(x/2)","bdR":"x+10"})</summary>
    public string? CommonPropsJson { get; set; }

    /// <summary>提取时间戳</summary>
    public DateTime ExtractedAt { get; set; }
}
