using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
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
/// WZ 子分类文件夹名 → 中文（冒险岛术语）
/// </summary>
public class SubCategoryConverter : IValueConverter
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
        ["Heart"] = "机器心脏",
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

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string sub && SubCategoryNames.TryGetValue(sub, out var name))
            return name;
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
