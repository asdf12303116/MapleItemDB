namespace MapleItemDB.Core.Models;

/// <summary>
/// 道具主实体，映射 dim_items 表
/// </summary>
public class ItemEntity
{
    /// <summary>道具 ID</summary>
    public int ItemId { get; set; }

    /// <summary>道具名称 (已解析宏变量)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>道具描述</summary>
    public string? Description { get; set; }

    /// <summary>分类: Equip/Consume/Etc/Setup/Cash/Pet</summary>
    public ItemCategory Category { get; set; }

    /// <summary>子分类: Weapon/Cap/Coat 等</summary>
    public string? SubCategory { get; set; }

    // ---- 装备需求属性 ----
    public int? ReqLevel { get; set; }
    public int? ReqStr { get; set; }
    public int? ReqDex { get; set; }
    public int? ReqInt { get; set; }
    public int? ReqLuk { get; set; }

    /// <summary>职业需求 (位掩码: 1=战士, 2=魔法师, 4=弓箭手, 8=飞侠, 16=海盗)</summary>
    public int? ReqJob { get; set; }

    // ---- 装备核心数值 ----
    public int? IncSTR { get; set; }
    public int? IncDEX { get; set; }
    public int? IncINT { get; set; }
    public int? IncLUK { get; set; }
    public int? IncPAD { get; set; } // 攻击力
    public int? IncMAD { get; set; } // 魔攻
    public int? IncPDD { get; set; } // 物防
    public int? IncMDD { get; set; } // 魔防
    public int? IncMHP { get; set; } // HP
    public int? IncMMP { get; set; } // MP

    /// <summary>动态/非结构化属性 (JSON 存储)，如 boss_dmg, ied 等</summary>
    public string? DynamicStats { get; set; }

    /// <summary>消耗品属性 (JSON 存储)，如 hp/mp回复, buff效果等</summary>
    public string? ConsumeSpec { get; set; }

    // ---- 元数据 ----
    /// <summary>是否商城道具</summary>
    public bool IsCash { get; set; }

    /// <summary>NPC 售价</summary>
    public int? Price { get; set; }

    /// <summary>图标 PNG 二进制数据</summary>
    public byte[]? IconData { get; set; }

    /// <summary>外观装备预览图 PNG 二进制数据</summary>
    public byte[]? PreviewData { get; set; }

    /// <summary>套装 ID</summary>
    public int? SetItemId { get; set; }

    /// <summary>商城 SN 编号</summary>
    public int? Sn { get; set; }

    /// <summary>是否限时道具</summary>
    public bool TimeLimited { get; set; }

    /// <summary>提取时间戳</summary>
    public DateTime ExtractedAt { get; set; }
}
