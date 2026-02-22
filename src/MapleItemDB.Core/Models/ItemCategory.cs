namespace MapleItemDB.Core.Models;

/// <summary>
/// 道具分类枚举
/// </summary>
public enum ItemCategory
{
    Equip,   // 装备
    Consume, // 消耗品
    Etc,     // 其他
    Setup,   // 设置 (椅子/家具)
    Cash,    // 现金道具
    Pet,     // 宠物
    Skill    // 技能 (仅用于搜索分类，不存储在道具表)
}
