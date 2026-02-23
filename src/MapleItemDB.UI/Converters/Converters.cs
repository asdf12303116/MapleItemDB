using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MapleItemDB.Core.Models;

namespace MapleItemDB.UI.Converters;

/// <summary>
/// null/空字符串/空byte[] → Collapsed, 否则 Visible
/// </summary>
public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => Visibility.Collapsed,
            byte[] bytes => bytes.Length > 0 ? Visibility.Visible : Visibility.Collapsed,
            string s => string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible,
            _ => Visibility.Visible,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// bool 取反
/// </summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

/// <summary>
/// ItemCategory 枚举 → 中文分类名
/// </summary>
public class ItemCategoryConverter : IValueConverter
{
    private static readonly Dictionary<ItemCategory, string> CategoryNames = new()
    {
        [ItemCategory.Equip] = "装备",
        [ItemCategory.Consume] = "消耗",
        [ItemCategory.Etc] = "其他",
        [ItemCategory.Setup] = "设置",
        [ItemCategory.Cash] = "点装",
        [ItemCategory.Pet] = "宠物",
        [ItemCategory.Skill] = "技能",
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ItemCategory cat && CategoryNames.TryGetValue(cat, out var name))
            return name;
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 冒险岛职业 ID → 中文名
/// </summary>
public static class JobNameHelper
{
    private static readonly Dictionary<int, string> JobNames = new()
    {
        // ===== 冒险家 (Explorers) =====
        [0] = "初心者",
        // 战士 → 英雄
        [100] = "战士", [110] = "剑客", [111] = "勇士", [112] = "英雄",
        // 战士 → 圣骑士
        [120] = "准骑士", [121] = "骑士", [122] = "圣骑士",
        // 战士 → 黑骑士
        [130] = "枪战士", [131] = "龙骑士", [132] = "黑骑士",
        // 魔法师 → 火毒魔导师
        [200] = "魔法师", [210] = "火毒法师", [211] = "火毒巫师", [212] = "火毒魔导师",
        // 魔法师 → 冰雷魔导师
        [220] = "冰雷法师", [221] = "冰雷巫师", [222] = "冰雷魔导师",
        // 魔法师 → 主教
        [230] = "牧师", [231] = "祭司", [232] = "主教",
        // 弓箭手 → 神射手
        [300] = "弓箭手", [310] = "猎人", [311] = "射手", [312] = "神射手",
        // 弓箭手 → 箭神
        [320] = "弩弓手", [321] = "游侠", [322] = "箭神",
        // 弓箭手 → 古迹猎人
        [301] = "古迹猎人", [330] = "古迹猎人", [331] = "古迹猎人", [332] = "古迹猎人",
        // 飞侠 → 隐士
        [400] = "飞侠", [410] = "刺客", [411] = "隐士", [412] = "隐士",
        // 飞侠 → 侠盗
        [420] = "侠客", [421] = "独行客", [422] = "侠盗",
        // 飞侠 → 暗影双刀
        [431] = "暗影双刀", [433] = "暗影双刀", [434] = "暗影双刀",
        // 海盗 → 冲锋队长
        [500] = "海盗", [510] = "拳手", [511] = "斗士", [512] = "冲锋队长",
        // 海盗 → 船长
        [520] = "火枪手", [521] = "大副", [522] = "船长",
        // 海盗 → 神炮王
        [501] = "神炮王", [530] = "神炮王", [531] = "神炮王", [532] = "神炮王",
        // 海盗 → 墨玄
        [505] = "墨玄", [570] = "墨玄", [571] = "墨玄", [572] = "墨玄",

        // ===== 皇家骑士团 (Cygnus Knights) =====
        [1000] = "贵族",
        // 魂骑士
        [1100] = "魂骑士", [1110] = "魂骑士", [1111] = "魂骑士", [1112] = "魂骑士",
        // 炎术士
        [1200] = "炎术士", [1210] = "炎术士", [1211] = "炎术士", [1212] = "炎术士",
        // 风灵使者
        [1300] = "风灵使者", [1310] = "风灵使者", [1311] = "风灵使者", [1312] = "风灵使者",
        // 夜行者
        [1400] = "夜行者", [1410] = "夜行者", [1411] = "夜行者", [1412] = "夜行者",
        // 奇袭者
        [1500] = "奇袭者", [1510] = "奇袭者", [1511] = "奇袭者", [1512] = "奇袭者",
        // 米哈尔
        [5000] = "米哈尔", [5100] = "米哈尔", [5110] = "米哈尔", [5111] = "米哈尔", [5112] = "米哈尔",

        // ===== 英雄 (Heroes) =====
        // 战神
        [2000] = "战神", [2100] = "战神", [2110] = "战神", [2111] = "战神", [2112] = "战神",
        // 龙神
        [2001] = "龙神", [2200] = "龙神", [2210] = "龙神", [2211] = "龙神", [2214] = "龙神", [2217] = "龙神", [2218] = "龙神",
        // 双弩精灵
        [2300] = "双弩精灵", [2310] = "双弩精灵", [2311] = "双弩精灵", [2312] = "双弩精灵",
        // 幻影
        [2003] = "幻影", [2400] = "幻影", [2410] = "幻影", [2411] = "幻影", [2412] = "幻影",
        // 隐月
        [2005] = "隐月", [2500] = "隐月", [2510] = "隐月", [2511] = "隐月", [2512] = "隐月",
        // 夜光法师
        [2004] = "夜光法师", [2700] = "夜光法师", [2710] = "夜光法师", [2711] = "夜光法师", [2712] = "夜光法师",

        // ===== 反抗者 & 恶魔 (Resistance & Demons) =====
        // 恶魔猎手
        [3001] = "恶魔猎手", [3100] = "恶魔猎手", [3110] = "恶魔猎手", [3111] = "恶魔猎手", [3112] = "恶魔猎手",
        // 恶魔复仇者
        [3002] = "恶魔复仇者", [3101] = "恶魔复仇者", [3120] = "恶魔复仇者", [3121] = "恶魔复仇者", [3122] = "恶魔复仇者",
        // 唤灵斗师
        [3200] = "唤灵斗师", [3210] = "唤灵斗师", [3211] = "唤灵斗师", [3212] = "唤灵斗师",
        // 豹弩游侠
        [3300] = "豹弩游侠", [3310] = "豹弩游侠", [3311] = "豹弩游侠", [3312] = "豹弩游侠",
        // 机械师
        [3500] = "机械师", [3510] = "机械师", [3511] = "机械师", [3512] = "机械师",
        // 尖兵
        [3600] = "尖兵", [3610] = "尖兵", [3611] = "尖兵", [3612] = "尖兵",
        // 爆破手
        [3700] = "爆破手", [3710] = "爆破手", [3711] = "爆破手", [3712] = "爆破手",

        // ===== 超新星 (Nova) =====
        // 狂龙战士
        [6000] = "狂龙战士", [6100] = "狂龙战士", [6110] = "狂龙战士", [6111] = "狂龙战士", [6112] = "狂龙战士",
        // 该隐
        [6300] = "该隐", [6310] = "该隐", [6311] = "该隐", [6312] = "该隐",
        // 卡德娜
        [6400] = "卡德娜", [6410] = "卡德娜", [6411] = "卡德娜", [6412] = "卡德娜",
        // 爆莉萌天使
        [6500] = "爆莉萌天使", [6510] = "爆莉萌天使", [6511] = "爆莉萌天使", [6512] = "爆莉萌天使",

        // ===== 翼人族 (Flora / Lef) =====
        // 阿黛尔
        [15100] = "阿黛尔", [15110] = "阿黛尔", [15111] = "阿黛尔", [15112] = "阿黛尔",
        // 伊利恩
        [15200] = "伊利恩", [15210] = "伊利恩", [15211] = "伊利恩", [15212] = "伊利恩",
        // 卡莉
        [15400] = "卡莉", [15410] = "卡莉", [15411] = "卡莉", [15412] = "卡莉",
        // 亚克
        [15500] = "亚克", [15510] = "亚克", [15511] = "亚克", [15512] = "亚克",

        // ===== 阿尼玛 (Anima) =====
        // 菈菈
        [16200] = "菈菈", [16210] = "菈菈", [16211] = "菈菈", [16212] = "菈菈",
        // 虎影
        [16400] = "虎影", [16410] = "虎影", [16411] = "虎影", [16412] = "虎影",

        // ===== 其他特殊职业 =====
        // 剑豪
        [4001] = "剑豪", [4100] = "剑豪", [4110] = "剑豪", [4111] = "剑豪", [4112] = "剑豪",
        // 阴阳师
        [4002] = "阴阳师", [4200] = "阴阳师", [4210] = "阴阳师", [4211] = "阴阳师", [4212] = "阴阳师",
        // 超能力者
        [14000] = "超能力者", [14200] = "超能力者", [14210] = "超能力者", [14211] = "超能力者", [14212] = "超能力者",
        // 琳恩
        [11200] = "琳恩", [11210] = "琳恩", [11211] = "琳恩", [11212] = "琳恩",
        // 神之子
        [10000] = "神之子", [10100] = "神之子", [10110] = "神之子", [10111] = "神之子", [10112] = "神之子",

        // ===== 通用/系统技能 =====
        [7000] = "内在能力",
        [7100] = "联盟",
        [7200] = "怪物农庄",
        [9100] = "公会",
        [9200] = "专业技术", [9201] = "专业技术", [9202] = "专业技术", [9203] = "专业技术", [9204] = "专业技术",

        // ===== 额外职业族系映射 (CMS 命名) =====
        [15000] = "伊利恩",
        [15001] = "亚克",
        [15002] = "阿黛尔",
        [15003] = "卡莉",
        [16000] = "虎影",
        [16001] = "虎影",
        [16002] = "菈菈",
        [17000] = "墨玄",
        [17001] = "琳恩",
        [18000] = "施亚",

        // ===== 高阶通用技能 (5转/6转) =====
        [40000] = "5转",
        [40001] = "5转(战士)",
        [40002] = "5转(魔法师)",
        [40003] = "5转(弓箭手)",
        [40004] = "5转(飞侠)",
        [40005] = "5转(海盗)",
        [50000] = "6转",
        [50006] = "6转(强化核心)",
        [50007] = "6转(HEXA属性)",
        // 超级属性
        [800004] = "超级属性"
    };

    /// <summary>
    /// 根据职业 ID 获取对应中文职业名，支持模糊匹配 (回退到基础职业)
    /// </summary>
    public static string GetJobName(int jobId)
    {
        // 精确匹配
        if (JobNames.TryGetValue(jobId, out var name))
            return name;

        // 回退: 尝试十位取整
        var baseJob = jobId / 10 * 10;
        if (JobNames.TryGetValue(baseJob, out name))
            return name;

        // 百位取整
        baseJob = jobId / 100 * 100;
        if (JobNames.TryGetValue(baseJob, out name))
            return name;

        // 千位取整
        baseJob = jobId / 1000 * 1000;
        if (JobNames.TryGetValue(baseJob, out name))
            return name;

        return "其他";
    }
}

/// <summary>
/// 根据武器道具 ID 前缀获取具体武器类型中文名
/// </summary>
public static class WeaponTypeHelper
{
    private static readonly Dictionary<int, string> WeaponTypeNames = new()
    {
        // 单手武器
        [122] = "灵魂手铳(单手)",
        [123] = "亡命剑(单手)",
        [124] = "能量剑(单手)",
        [125] = "魔法棒(单手)",
        [126] = "ESP限制器(单手)",
        [127] = "锁链(单手)",
        [128] = "魔力手套(单手)",
        [129] = "扇子(单手)",
        [130] = "单手剑(单手)",
        [131] = "单手斧(单手)",
        [132] = "单手钝器(单手)",
        [133] = "短剑(单手)",
        [134] = "小刀(单手)",
        [136] = "手杖(单手)",
        [137] = "短杖(单手)",
        [138] = "长杖(单手)",
        [1212] = "双头杖(单手)",
        [1213] = "调谐器(单手)",
        [1214] = "龙息臂箭(单手)",
        // 双手武器
        [121] = "拳封(双手)",
        [140] = "双手剑(双手)",
        [141] = "双手斧(双手)",
        [142] = "双手钝器(双手)",
        [143] = "枪(双手)",
        [144] = "矛(双手)",
        [145] = "弓(双手)",
        [146] = "弩(双手)",
        [147] = "拳套(双手)",
        [148] = "拳甲(双手)",
        [149] = "短枪(双手)",
        [152] = "双弩枪(双手)",
        [153] = "手炮(双手)",
        [154] = "武士刀(双手)",
        [155] = "折扇(双手)",
        [156] = "锋利之影(双手)",
        [157] = "阔影剑(双手)",
        [158] = "拳炮(双手)",
        [159] = "远古弓(双手)",
        // 副手武器
        [1092] = "盾牌(副手)",
        [1093] = "盾牌(副手)",
        [1094] = "盾牌(副手)",
        [1095] = "盾牌(副手)",
        [1096] = "盾牌(副手)",
        [1097] = "盾牌(副手)",
        [1098] = "灵魂盾(副手)",
        [1099] = "精气盾(副手)",
        [1342] = "小刀(副手)",
        // 135 系列副手武器 (5 位前缀)
        [13520] = "魔法箭矢(副手)",
        [13521] = "卡片(副手)",
        // 135 系列副手武器 (6 位前缀)
        [135220] = "吊坠(副手)",
        [135221] = "念珠(副手)",
        [135222] = "铁链(副手)",
        [135223] = "魔导书(副手)",
        [135224] = "魔导书(副手)",
        [135225] = "魔导书(副手)",
        [135226] = "箭羽(副手)",
        [135227] = "扳指(副手)",
        [135228] = "短剑剑鞘(副手)",
        [135229] = "护身符(副手)",
        [135230] = "八卦宝盒(副手)",
        [135240] = "宝珠(副手)",
        [135250] = "龙之精髓(副手)",
        [135260] = "灵魂手镯(副手)",
        [135270] = "麦林(副手)",
        [135280] = "小太刀(副手)",
        [135281] = "哨子(副手)",
        [135282] = "拳套(副手)",
        [135283] = "小太刀(副手)",
        [135286] = "拳天(副手)",
        [135290] = "手腕护带(副手)",
        [135291] = "瞄准器(副手)",
        [135292] = "火药桶(副手)",
        [135293] = "砝码(副手)",
        [135294] = "文件(副手)",
        [135295] = "魔法球(副手)",
        [135296] = "箭轴(副手)",
        [135297] = "宝石(副手)",
        [135300] = "控制器(副手)",
        [135310] = "狐狸珠(副手)",
        [135320] = "棋子(副手)",
        [135330] = "武器传送装置(副手)",
        [135340] = "装弹(副手)",
        [135350] = "魔法之翼(副手)",
        [135360] = "深渊精气珠(副手)",
        [135370] = "遗物(副手)",
        [135380] = "扇坠(副手)",
        [135400] = "手链(副手)",
        [135402] = "饰品(副手)",
        // 135 通用兜底
        [135] = "辅助武器(副手)",
    };

    /// <summary>
    /// 根据道具 ID 获取武器类型名，非武器返回 null
    /// </summary>
    public static string? GetWeaponTypeName(int itemId)
    {
        var idStr = itemId.ToString();
        // 依次尝试 6→5→4→3 位前缀，优先匹配更精确的
        for (var len = 6; len >= 3; len--)
        {
            if (idStr.Length >= len
                && int.TryParse(idStr.AsSpan(0, len), out var prefix)
                && WeaponTypeNames.TryGetValue(prefix, out var name))
                return name;
        }
        return null;
    }
}

/// <summary>
/// 子分类名称查找 (供 Converter 和 ViewModel 共用)
/// </summary>
public static class SubCategoryDisplayHelper
{
    private static readonly Dictionary<string, string> SubCategoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // 防具
        ["Cap"] = "帽子",
        ["Coat"] = "上衣",
        ["Longcoat"] = "套服",
        ["Pants"] = "裤子",
        ["Shoes"] = "鞋子",
        ["Glove"] = "手套",
        ["Belt"] = "腰带",
        ["Shoulder"] = "肩饰",
        // 其他部位
        ["Cape"] = "披风",
        ["Shield"] = "盾牌",
        ["Ring"] = "戒指",
        ["Pendant"] = "吊坠",
        ["Medal"] = "勋章",
        ["Earring"] = "耳环",
        ["Badge"] = "徽章",
        ["Emblem"] = "纹章",
        ["Pocket"] = "口袋",
        ["Heart"] = "机械心脏",
        ["Totem"] = "图腾",
        ["Weapon"] = "武器",
        // 外观
        ["Hair"] = "发型",
        ["Face"] = "脸型",
        ["Accessory"] = "脸饰",
        ["EyeDecoration"] = "眼饰",
        // 特殊装备
        ["Android"] = "机器人",
        ["Dragon"] = "龙神装备",
        ["Mechanic"] = "机械",
        ["TamingMob"] = "骑宠",
        ["PetEquip"] = "宠物装备",
        // 辅助武器细分
        ["SecondWeapon"] = "辅助武器",
        ["DemonShield"] = "力量盾",
        ["MechanicEngine"] = "引擎",
        ["MechanicArm"] = "机械臂",
        ["MechanicLeg"] = "机械腿",
        ["MechanicFrame"] = "机架",
        ["MechanicTransistor"] = "晶体管",
        ["Bits"] = "比特",
        // 符号
        ["ArcaneForce"] = "神秘徽章",
        ["AuthenticForce"] = "原初徽章",
        // 点商 / 外观
        ["Label"] = "标签",
        ["ChatBalloon"] = "聊天气泡",
        ["SkillEffect"] = "技能皮肤",
        ["Effect"] = "特效",
        ["MonsterBattle"] = "口袋怪物",
        ["Familiar"] = "使魔",
        // Item.wz 子分类
        ["Consume"] = "消耗品",
        ["Install"] = "设置",
        ["Etc"] = "其他",
        ["Cash"] = "点装",
        ["Pet"] = "宠物",
        ["Special"] = "特殊",
    };

    public static string? GetSubCategoryName(string? subCategory)
    {
        if (subCategory != null && SubCategoryNames.TryGetValue(subCategory, out var name))
            return name;
        return null;
    }
}

/// <summary>
/// WZ 子分类文件夹名 → 中文（冒险岛术语）
/// </summary>
public class SubCategoryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string sub)
        {
            var name = SubCategoryDisplayHelper.GetSubCategoryName(sub);
            if (name != null)
                return name;
            // 支持技能搜索的职业 ID 子分类 (格式: "job:{jobId}")
            if (sub.StartsWith("job:") && int.TryParse(sub.AsSpan(4), out var jobId))
                return JobNameHelper.GetJobName(jobId);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// byte[] (PNG) → BitmapImage
/// </summary>
public class ByteArrayToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return null;

        var bmp = new BitmapImage();
        using var ms = new MemoryStream(bytes);
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = ms;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// bool 取反转 Visibility: true → Collapsed, false → Visible
/// </summary>
public class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// bool → "限时" / "永久"
/// </summary>
public class TimeLimitedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "限时" : "永久";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// IsFlag (bool) → 橙色 #FF9900 / 黑色
/// </summary>
public class FlagToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xFF, 0x99, 0x00));
    private static readonly SolidColorBrush BlackBrush = new(Colors.Black);

    static FlagToColorConverter()
    {
        OrangeBrush.Freeze();
        BlackBrush.Freeze();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? OrangeBrush : BlackBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 绑定代理 — 让不在可视化树中的元素 (如 DataGridColumn) 能访问 DataContext
/// </summary>
public class BindingProxy : Freezable
{
    protected override Freezable CreateInstanceCore() => new BindingProxy();

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new PropertyMetadata(null));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
}

/// <summary>
/// 冒险岛文本格式化附加属性 — 将含 #c...# 标签的文本渲染为富文本 (橙色高亮)
/// </summary>
public static class MapleTextHelper
{
    private static readonly SolidColorBrush OrangeBrush;

    static MapleTextHelper()
    {
        OrangeBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x99, 0x00));
        OrangeBrush.Freeze();
    }

    public static readonly DependencyProperty MapleTextProperty =
        DependencyProperty.RegisterAttached(
            "MapleText",
            typeof(string),
            typeof(MapleTextHelper),
            new PropertyMetadata(null, OnMapleTextChanged));

    public static string? GetMapleText(DependencyObject obj) =>
        (string?)obj.GetValue(MapleTextProperty);

    public static void SetMapleText(DependencyObject obj, string? value) =>
        obj.SetValue(MapleTextProperty, value);

    private static void OnMapleTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock) return;

        textBlock.Inlines.Clear();
        var text = e.NewValue as string;
        if (string.IsNullOrEmpty(text)) return;

        // 解析 #c...# 标签: #c 开始橙色，单独的 # 结束
        // 正则: 将文本拆分为 普通段 和 #c段# 交替
        var segments = Regex.Split(text, @"#c(.*?)#", RegexOptions.Singleline);

        // Split 结果: [普通, 橙色内容, 普通, 橙色内容, ...]
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (string.IsNullOrEmpty(segment)) continue;

            if (i % 2 == 0)
            {
                // 普通文本
                textBlock.Inlines.Add(new Run(segment));
            }
            else
            {
                // 橙色文本 (#c 和 # 之间的内容)
                textBlock.Inlines.Add(new Run(segment)
                {
                    Foreground = OrangeBrush,
                });
            }
        }
    }
}
