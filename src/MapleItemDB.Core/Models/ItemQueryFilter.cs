namespace MapleItemDB.Core.Models;

/// <summary>
/// 道具查询筛选条件
/// </summary>
public class ItemQueryFilter
{
    public string? Keyword { get; set; }
    public ItemCategory? Category { get; set; }
    public string? SubCategory { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
    public bool? IsCash { get; set; }

    // JSON 动态属性筛选
    public int? MinBossDmg { get; set; }
    public int? MinIed { get; set; }

    public int Limit { get; set; } = 0;  // 0 = 不限制
    public int Offset { get; set; } = 0;
}
