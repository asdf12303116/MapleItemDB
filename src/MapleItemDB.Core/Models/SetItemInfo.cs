namespace MapleItemDB.Core.Models;

/// <summary>
/// 套装信息
/// </summary>
public class SetItemInfo
{
    /// <summary>套装 ID</summary>
    public int SetItemId { get; set; }

    /// <summary>套装名称</summary>
    public string SetItemName { get; set; } = string.Empty;

    /// <summary>集齐所需件数</summary>
    public int CompleteCount { get; set; }

    /// <summary>套装部件列表</summary>
    public List<SetItemPart> Parts { get; set; } = [];

    /// <summary>套装效果列表 (按件数递增)</summary>
    public List<SetItemEffect> Effects { get; set; } = [];
}

/// <summary>
/// 套装中的一个部件位置
/// </summary>
public class SetItemPart
{
    /// <summary>部件序号</summary>
    public int PartIndex { get; set; }

    /// <summary>部件类型名 (如 "帽子", "上衣")</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>代表道具名称</summary>
    public string RepresentName { get; set; } = string.Empty;

    /// <summary>该位置可选的道具 ID 列表</summary>
    public List<int> ItemIds { get; set; } = [];
}

/// <summary>
/// 套装效果 (穿戴 N 件时的加成)
/// </summary>
public class SetItemEffect
{
    /// <summary>所需穿戴件数</summary>
    public int RequiredCount { get; set; }

    /// <summary>属性加成 (键为属性名，值为加成值)</summary>
    public Dictionary<string, int> Props { get; set; } = [];

    /// <summary>套装激活技能列表</summary>
    public List<SetItemActiveSkill> ActiveSkills { get; set; } = [];
}

/// <summary>
/// 套装激活技能
/// </summary>
public class SetItemActiveSkill
{
    /// <summary>技能 ID</summary>
    public int SkillId { get; set; }

    /// <summary>技能等级</summary>
    public int Level { get; set; }

    /// <summary>技能名称</summary>
    public string? SkillName { get; set; }
}
