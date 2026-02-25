using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MapleItemDB.Core.Models;

namespace MapleItemDB.Application.UseCases;

internal static class ItemDetailFormattingHelper
{
    private static readonly Dictionary<ItemCategory, string> CategoryDisplayNames = new()
    {
        [ItemCategory.Equip] = "装备",
        [ItemCategory.Consume] = "消耗",
        [ItemCategory.Etc] = "其他",
        [ItemCategory.Setup] = "设置",
        [ItemCategory.Cash] = "点装",
        [ItemCategory.Pet] = "宠物",
        [ItemCategory.Skill] = "技能",
    };

    private static readonly Dictionary<string, string> SubCategoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cap"] = "帽子",
        ["Coat"] = "上衣",
        ["Longcoat"] = "套服",
        ["Pants"] = "裤子",
        ["Shoes"] = "鞋子",
        ["Glove"] = "手套",
        ["Belt"] = "腰带",
        ["Shoulder"] = "肩饰",
        ["Cape"] = "披风",
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
        ["SecondWeapon"] = "辅助武器",
        ["Hair"] = "发型",
        ["Face"] = "脸型",
        ["Accessory"] = "脸饰",
        ["EyeDecoration"] = "眼饰",
        ["Android"] = "机器人",
        ["TamingMob"] = "骑宠",
        ["PetEquip"] = "宠物装备",
        ["ArcaneForce"] = "神秘徽章",
        ["AuthenticForce"] = "原初徽章",
    };

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

    private static readonly Dictionary<int, string> JobNames = new()
    {
        [0] = "初心者",
        [100] = "战士",
        [200] = "魔法师",
        [300] = "弓箭手",
        [400] = "飞侠",
        [500] = "海盗",
        [1000] = "贵族",
        [1100] = "魂骑士",
        [1200] = "炎术士",
        [1300] = "风灵使者",
        [1400] = "夜行者",
        [1500] = "奇袭者",
        [2000] = "战神",
        [2001] = "龙神",
        [2002] = "双弩精灵",
        [2003] = "幻影",
        [2004] = "夜光法师",
        [2005] = "隐月",
        [3000] = "反抗者",
        [3001] = "恶魔猎手",
        [3002] = "恶魔复仇者",
        [3200] = "唤灵斗师",
        [3300] = "豹弩游侠",
        [3500] = "机械师",
        [3600] = "尖兵",
        [3700] = "爆破手",
        [5000] = "米哈尔",
        [6000] = "狂龙战士",
        [6300] = "凯撒",
        [6400] = "卡蒂娜",
        [6500] = "爆莉萌天使",
        [15100] = "阿黛尔",
        [15200] = "伊利恩",
        [15400] = "卡莉",
        [15500] = "亚克",
        [16000] = "虎影",
        [16200] = "菈菈",
    };

    private static readonly Dictionary<string, string> StatFormats = new()
    {
        ["reqLevel"] = "需要等级 : {0}",
        ["incSTR"] = "力量 : +{0}",
        ["incDEX"] = "敏捷 : +{0}",
        ["incINT"] = "智力 : +{0}",
        ["incLUK"] = "运气 : +{0}",
        ["incMHP"] = "MaxHP : +{0}",
        ["incMMP"] = "MaxMP : +{0}",
        ["incPAD"] = "攻击力 : +{0}",
        ["incMAD"] = "魔法攻击力 : +{0}",
        ["incPDD"] = "防御力 : +{0}",
        ["incMDD"] = "魔法防御力 : +{0}",
        ["incSTRr"] = "力量 : +{0}%",
        ["incDEXr"] = "敏捷 : +{0}%",
        ["incINTr"] = "智力 : +{0}%",
        ["incLUKr"] = "运气 : +{0}%",
        ["incMHPr"] = "MaxHP : +{0}%",
        ["incMMPr"] = "MaxMP : +{0}%",
        ["incPADr"] = "攻击力 : +{0}%",
        ["incMADr"] = "魔法攻击力 : +{0}%",
        ["incPDDr"] = "防御力 : +{0}%",
        ["incMDDr"] = "魔法防御力 : +{0}%",
        ["incACCr"] = "命中值 : +{0}%",
        ["incEVAr"] = "回避值 : +{0}%",
        ["boss_dmg"] = "BOSS攻击时伤害 : +{0}%",
        ["bdR"] = "BOSS攻击时伤害 : +{0}%",
        ["incBDR"] = "BOSS攻击时伤害 : +{0}%",
        ["ied"] = "无视怪物防御力 : +{0}%",
        ["imdR"] = "无视怪物防御力 : +{0}%",
        ["incIMDR"] = "无视怪物防御力 : +{0}%",
        ["total_dmg"] = "总伤害 : +{0}%",
        ["damR"] = "总伤害 : +{0}%",
        ["incDAMr"] = "总伤害 : +{0}%",
        ["nbdR"] = "普通怪物伤害+{0}%",
        ["all_stat_pct"] = "全部属性 : +{0}%",
        ["statR"] = "全部属性 : +{0}%",
        ["all_stat"] = "全部属性 : +{0}",
        ["incAllStat"] = "全部属性 : +{0}",
        ["speed"] = "移动速度 : +{0}",
        ["incSpeed"] = "移动速度 : +{0}",
        ["jump"] = "跳跃力 : +{0}",
        ["incJump"] = "跳跃力 : +{0}",
        ["attack_speed"] = "攻击速度 : {0}",
        ["attackSpeed"] = "攻击速度 : {0}",
        ["upgrade_slots"] = "升级可用次数 : {0}",
        ["tuc"] = "升级可用次数 : {0}",
        ["knockback"] = "击退 : {0}%",
        ["incACC"] = "命中值 : +{0}",
        ["incEVA"] = "回避值 : +{0}",
        ["incCr"] = "暴击概率 : +{0}%",
        ["incCDr"] = "暴击伤害 : +{0}%",
        ["incTerR"] = "状态异常抗性 : +{0}%",
        ["incAsrR"] = "全部属性抗性 : +{0}%",
        ["incEXPr"] = "经验值获取量 : +{0}%",
        ["reduceCooltime"] = "冷却时间减少 : {0}秒",
        ["incARC"] = "ARC : +{0}",
        ["incAUT"] = "AUT : +{0}",
        ["incMDF"] = "异常状态抗性 : +{0}",
        ["incCraft"] = "手技 : +{0}",
        ["incPVPDamage"] = "PVP伤害 : +{0}",
        ["incMaxDamage"] = "最大伤害 : +{0}",
        ["incAllskill"] = "全部技能等级 : +{0}",
        ["ignoreTargetDEF"] = "无视怪物防御率 : +{0}%",
        ["incCriticaldamage"] = "暴击伤害 : +{0}%",
        ["incCriticaldamageMin"] = "最小暴击伤害 : +{0}%",
        ["incCriticaldamageMax"] = "最大暴击伤害 : +{0}%",
        ["RecoveryHP"] = "每10秒回复HP : {0}",
        ["RecoveryMP"] = "每10秒回复MP : {0}",
        ["incMesoProp"] = "金币获取量 : +{0}%",
        ["incRewardProp"] = "道具掉落率 : +{0}%",
        ["mpconReduce"] = "MP消耗减少 : {0}%",
        ["incPQEXPr"] = "组队任务经验 : +{0}%",
    };

    private static readonly Dictionary<int, string> AttackSpeedNames = new()
    {
        [2] = "更快(2)",
        [3] = "更快(3)",
        [4] = "快(4)",
        [5] = "快(5)",
        [6] = "普通(6)",
        [7] = "慢(7)",
        [8] = "慢(8)",
        [9] = "更慢(9)",
    };

    private static readonly Dictionary<string, string> FlagTexts = new()
    {
        ["_flag_only"] = "唯一道具",
        ["_flag_tradeBlock"] = "不可交易",
        ["_flag_equipTradeBlock"] = "装备后不可交易",
        ["_flag_accountSharable"] = "账号内共享",
        ["_flag_timeLimited"] = "限时道具",
        ["_flag_superiorEqp"] = "星之力增强道具",
        ["_flag_noPotential"] = "不可使用潜能",
        ["_flag_fixedPotential"] = "固定潜能",
    };

    private static readonly Dictionary<string, string> ConsumeFormats = new()
    {
        ["hp"] = "HP 回复 : +{0}",
        ["mp"] = "MP 回复 : +{0}",
        ["hpR"] = "HP 回复 : +{0}%",
        ["mpR"] = "MP 回复 : +{0}%",
        ["time"] = "持续时间 : {0}秒",
        ["pad"] = "攻击力 : +{0}",
        ["mad"] = "魔法攻击力 : +{0}",
        ["pdd"] = "防御力 : +{0}",
        ["mdd"] = "魔法防御力 : +{0}",
        ["speed"] = "移动速度 : +{0}",
        ["jump"] = "跳跃力 : +{0}",
        ["eva"] = "回避值 : +{0}",
        ["acc"] = "命中值 : +{0}",
    };

    public static string BuildCategoryDisplay(ItemEntity? item)
    {
        if (item == null)
            return string.Empty;

        var category = CategoryDisplayNames.GetValueOrDefault(item.Category, item.Category.ToString());

        var subCategory = GetSubCategoryName(item.SubCategory);
        if (!string.IsNullOrEmpty(subCategory))
            category += " / " + subCategory;

        if (item.SubCategory is "Weapon" or "SecondWeapon")
        {
            var weaponType = GetWeaponTypeName(item.ItemId);
            if (weaponType != null)
                category += " / " + weaponType;
        }

        if (item.Category == ItemCategory.Skill
            && item.SubCategory is { } sub
            && sub.StartsWith("job:")
            && int.TryParse(sub.AsSpan(4), out var jobId))
        {
            category += " / " + GetJobName(jobId);
        }

        return category;
    }

    public static string BuildSkillDetailText(SkillEntity skill)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[{GetJobName(skill.JobId)}] {skill.Name}");

        if (skill.IsHidden)
            builder.AppendLine("(隐藏技能)");

        var commonProps = ParseCommonProps(skill.CommonPropsJson);

        if (!string.IsNullOrEmpty(skill.Description))
        {
            builder.AppendLine();
            var description = ResolveSkillTemplate(skill.Description, skill.MaxLevel, commonProps);
            builder.Append(description);
        }

        if (!string.IsNullOrEmpty(skill.SkillH) && skill.MaxLevel > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
            var levelText = ResolveSkillTemplate(skill.SkillH, skill.MaxLevel, commonProps);
            builder.Append($"[Lv.{skill.MaxLevel}] {levelText}");
        }

        return builder.ToString().TrimEnd();
    }

    public static List<StatsLine> FormatStats(ItemEntity item)
    {
        var normalLines = new List<StatsLine>();
        var flagLines = new List<StatsLine>();

        AddLine(normalLines, "reqLevel", item.ReqLevel);

        var dynamic = ParseDynamic(item.DynamicStats);
        AddDynamicLine(normalLines, dynamic, "upgrade_slots");

        AddLine(normalLines, "incSTR", item.IncSTR);
        AddLine(normalLines, "incDEX", item.IncDEX);
        AddLine(normalLines, "incINT", item.IncINT);
        AddLine(normalLines, "incLUK", item.IncLUK);
        AddLine(normalLines, "incMHP", item.IncMHP);
        AddLine(normalLines, "incMMP", item.IncMMP);
        AddLine(normalLines, "incPAD", item.IncPAD);
        AddLine(normalLines, "incMAD", item.IncMAD);
        AddLine(normalLines, "incPDD", item.IncPDD);
        AddLine(normalLines, "incMDD", item.IncMDD);

        AddDynamicLine(normalLines, dynamic, "boss_dmg");
        AddDynamicLine(normalLines, dynamic, "ied");
        AddDynamicLine(normalLines, dynamic, "total_dmg");
        AddDynamicLine(normalLines, dynamic, "all_stat_pct");
        AddDynamicLine(normalLines, dynamic, "all_stat");
        AddDynamicLine(normalLines, dynamic, "speed");
        AddDynamicLine(normalLines, dynamic, "jump");
        AddDynamicLine(normalLines, dynamic, "knockback");

        if (dynamic.TryGetValue("attack_speed", out var attackSpeed))
        {
            var speedName = AttackSpeedNames.GetValueOrDefault(attackSpeed, attackSpeed.ToString());
            normalLines.Add(new StatsLine($"攻击速度 : {speedName}"));
        }

        foreach (var (key, text) in FlagTexts)
        {
            if (dynamic.TryGetValue(key, out var value) && value != 0)
                flagLines.Add(new StatsLine(text, true));
        }

        var reqJobText = FormatReqJob(item.ReqJob);
        if (reqJobText != null)
            flagLines.Add(new StatsLine(reqJobText));

        if (item.SubCategory is "Weapon" or "SecondWeapon")
        {
            var weaponType = GetWeaponTypeName(item.ItemId);
            if (weaponType != null)
                flagLines.Add(new StatsLine($"分类 : {weaponType}"));
        }

        if (!string.IsNullOrEmpty(item.ConsumeSpec))
        {
            var consume = ParseDynamic(item.ConsumeSpec);
            if (consume.Count > 0)
            {
                normalLines.Add(new StatsLine("──── 使用效果 ────"));
                foreach (var (key, value) in consume)
                {
                    var display = FormatConsumeStat(key, value);
                    if (display != null)
                        normalLines.Add(new StatsLine(display));
                }
            }
        }

        flagLines.AddRange(normalLines);
        return flagLines;
    }

    public static string? FormatSingleStat(string key, int value)
    {
        if (!StatFormats.TryGetValue(key, out var format))
            return null;

        if (key is "attack_speed" or "attackSpeed")
        {
            var speedName = AttackSpeedNames.GetValueOrDefault(value, value.ToString());
            return string.Format(format, speedName);
        }

        return string.Format(format, value);
    }

    public static string BuildSetItemDisplayText(SetItemInfo setInfo)
    {
        var builder = new StringBuilder();
        builder.Append(setInfo.SetItemName);

        foreach (var effect in setInfo.Effects)
        {
            builder.AppendLine();

            var hasBossFlag = effect.Props.TryGetValue("boss", out var bossValue) && bossValue != 0;

            var props = new List<string>();
            foreach (var (key, value) in effect.Props)
            {
                if (key == "boss")
                    continue;

                if (key == "incDAMr" && hasBossFlag)
                {
                    props.Add($"攻击首领怪时的伤害 : +{value}%");
                    continue;
                }

                var display = FormatSingleStat(key, value);
                props.Add(display ?? $"{key} : +{value}");
            }

            foreach (var skill in effect.ActiveSkills)
            {
                var skillName = skill.SkillName ?? $"技能 {skill.SkillId}";
                var skillText = $"{skillName} Lv.{skill.Level}";
                if (!string.IsNullOrEmpty(skill.Description))
                    skillText += $" ({skill.Description})";
                props.Add(skillText);
            }

            builder.Append($"【{effect.RequiredCount}件】{string.Join(", ", props)}");
        }

        return builder.ToString();
    }

    private static string GetJobName(int jobId)
    {
        if (JobNames.TryGetValue(jobId, out var exact))
            return exact;

        var baseBy10 = jobId / 10 * 10;
        if (JobNames.TryGetValue(baseBy10, out var by10))
            return by10;

        var baseBy100 = jobId / 100 * 100;
        if (JobNames.TryGetValue(baseBy100, out var by100))
            return by100;

        var baseBy1000 = jobId / 1000 * 1000;
        if (JobNames.TryGetValue(baseBy1000, out var by1000))
            return by1000;

        return "其他";
    }

    private static string? GetSubCategoryName(string? subCategory)
    {
        if (subCategory != null && SubCategoryNames.TryGetValue(subCategory, out var name))
            return name;

        return null;
    }

    private static string? GetWeaponTypeName(int itemId)
    {
        var idText = itemId.ToString();
        // 依次尝试 6→5→4→3 位前缀，优先匹配更精确的
        for (var len = 6; len >= 3; len--)
        {
            if (idText.Length < len)
                continue;

            if (int.TryParse(idText.AsSpan(0, len), out var prefix)
                && WeaponTypeNames.TryGetValue(prefix, out var name))
            {
                return name;
            }
        }

        return null;
    }

    private static string ResolveSkillTemplate(string template, int level, IReadOnlyDictionary<string, string> commonProps)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var text = template
            .Replace("\\r", string.Empty)
            .Replace("\\n", "\n");

        var replaced = Regex.Replace(text, "#([_A-Za-z][_A-Za-z0-9]*)", match =>
        {
            var key = match.Groups[1].Value;
            if (string.Equals(key, "c", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (!TryGetIgnoreCase(commonProps, key, out var expression) || string.IsNullOrWhiteSpace(expression))
                return int.TryParse(key, out var number) ? number.ToString(CultureInfo.InvariantCulture) : "0";

            var value = SkillFormulaCalculator.Parse(expression, level);

            if (string.Equals(key, "cooltimeMS", StringComparison.OrdinalIgnoreCase))
                return (value / 1000m).ToString("0.##", CultureInfo.InvariantCulture);

            if (key.EndsWith("PerM", StringComparison.OrdinalIgnoreCase))
                return (value / 100m).ToString("0.#", CultureInfo.InvariantCulture);

            return ((int)value).ToString(CultureInfo.InvariantCulture);
        });

        return replaced.Replace("#", string.Empty);
    }

    private static bool TryGetIgnoreCase(IReadOnlyDictionary<string, string> dictionary, string key, out string? value)
    {
        foreach (var item in dictionary)
        {
            if (item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = item.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string? FormatReqJob(int? reqJob)
    {
        if (reqJob is null or 0)
            return null;

        var mask = reqJob.Value;
        if (mask == -1)
            return "职业:无";

        if ((mask & 0x1f) == 0x1f)
            return null;

        var jobs = new List<string>();
        if ((mask & 1) != 0) jobs.Add("战士");
        if ((mask & 2) != 0) jobs.Add("魔法师");
        if ((mask & 4) != 0) jobs.Add("弓箭手");
        if ((mask & 8) != 0) jobs.Add("飞侠");
        if ((mask & 16) != 0) jobs.Add("海盗");

        return $"职业:{string.Join(", ", jobs)}";
    }

    private static void AddLine(List<StatsLine> lines, string key, int? value)
    {
        if (value is not { } nonNull || nonNull == 0)
            return;

        if (StatFormats.TryGetValue(key, out var format))
            lines.Add(new StatsLine(string.Format(format, nonNull)));
    }

    private static void AddDynamicLine(List<StatsLine> lines, Dictionary<string, int> dynamic, string key)
    {
        if (!dynamic.TryGetValue(key, out var value) || value == 0)
            return;

        if (StatFormats.TryGetValue(key, out var format))
            lines.Add(new StatsLine(string.Format(format, value)));
    }

    private static Dictionary<string, int> ParseDynamic(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<string, string> ParseCommonProps(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? FormatConsumeStat(string key, int value)
    {
        if (ConsumeFormats.TryGetValue(key, out var format))
            return string.Format(format, value);

        return $"{key} : {value}";
    }

    private static class SkillFormulaCalculator
    {
        public static decimal Parse(string expression, decimal level)
        {
            try
            {
                var tokens = Tokenize(expression);
                var rpn = ToRpn(tokens);
                return Evaluate(rpn, level);
            }
            catch
            {
                return 0;
            }
        }

        private enum TokenType
        {
            Number,
            Identifier,
            Operator,
            LeftParenthesis,
            RightParenthesis,
            CallStart,
            CallEnd,
        }

        private enum TokenTag
        {
            None,
            Unary,
            Call,
        }

        private record Token(TokenType Type, string Value)
        {
            public TokenTag Tag { get; set; }
        }

        private static List<Token> Tokenize(string expression)
        {
            var tokens = new List<Token>();
            for (var index = 0; index < expression.Length; index++)
            {
                var character = expression[index];

                if (character is ' ' or '%')
                    continue;

                if (character is '+' or '-' or '*' or '/')
                {
                    tokens.Add(new Token(TokenType.Operator, character.ToString()));
                    continue;
                }

                if (character == '(')
                {
                    tokens.Add(new Token(TokenType.LeftParenthesis, "("));
                    continue;
                }

                if (character == ')')
                {
                    tokens.Add(new Token(TokenType.RightParenthesis, ")"));
                    continue;
                }

                if (character == ',')
                    continue;

                if (char.IsDigit(character) || character == '.')
                {
                    var start = index;
                    while (index + 1 < expression.Length
                           && (char.IsDigit(expression[index + 1]) || expression[index + 1] == '.'))
                    {
                        index++;
                    }

                    tokens.Add(new Token(TokenType.Number, expression[start..(index + 1)]));
                    continue;
                }

                if (char.IsLetter(character) || character == '_')
                {
                    var start = index;
                    while (index + 1 < expression.Length
                           && (char.IsLetterOrDigit(expression[index + 1]) || expression[index + 1] == '_'))
                    {
                        index++;
                    }

                    tokens.Add(new Token(TokenType.Identifier, expression[start..(index + 1)]));
                }
            }

            return tokens;
        }

        private static int GetPriority(Token token)
        {
            if (token.Tag == TokenTag.Unary)
                return 4;

            return token.Value switch
            {
                "+" or "-" => 1,
                "*" or "/" => 2,
                _ => 0,
            };
        }

        private static List<Token> ToRpn(List<Token> tokens)
        {
            var output = new List<Token>();
            var stack = new Stack<Token>();

            for (var index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.Number:
                        output.Add(token);
                        break;

                    case TokenType.Identifier:
                        output.Add(token);
                        if (index + 1 < tokens.Count && tokens[index + 1].Type == TokenType.LeftParenthesis)
                        {
                            tokens[index + 1] = tokens[index + 1] with { Tag = TokenTag.Call };
                        }

                        break;

                    case TokenType.LeftParenthesis:
                        stack.Push(token);
                        if (token.Tag == TokenTag.Call)
                            output.Add(new Token(TokenType.CallStart, string.Empty));
                        break;

                    case TokenType.RightParenthesis:
                        while (stack.Count > 0 && stack.Peek().Type != TokenType.LeftParenthesis)
                            output.Add(stack.Pop());

                        if (stack.Count > 0)
                        {
                            var leftParenthesis = stack.Pop();
                            if (leftParenthesis.Tag == TokenTag.Call)
                                output.Add(new Token(TokenType.CallEnd, string.Empty));
                        }

                        break;

                    case TokenType.Operator:
                        if (index == 0 || tokens[index - 1].Type is TokenType.LeftParenthesis or TokenType.Operator)
                            token = token with { Tag = TokenTag.Unary };

                        while (stack.Count > 0
                               && stack.Peek().Type == TokenType.Operator
                               && GetPriority(token) <= GetPriority(stack.Peek())
                               && !(token.Tag == TokenTag.Unary && stack.Peek().Tag == TokenTag.Unary))
                        {
                            output.Add(stack.Pop());
                        }

                        stack.Push(token);
                        break;
                }
            }

            while (stack.Count > 0)
                output.Add(stack.Pop());

            return output;
        }

        private static decimal Evaluate(List<Token> rpn, decimal level)
        {
            var stack = new Stack<object>();

            foreach (var token in rpn)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                        stack.Push(decimal.Parse(token.Value, CultureInfo.InvariantCulture));
                        break;

                    case TokenType.Identifier:
                        if (token.Value == "x")
                        {
                            stack.Push(level);
                        }
                        else if (token.Value == "u")
                        {
                            stack.Push((Func<decimal, decimal>)Math.Ceiling);
                        }
                        else if (token.Value == "d")
                        {
                            stack.Push((Func<decimal, decimal>)Math.Floor);
                        }
                        else
                        {
                            stack.Push(0m);
                        }

                        break;

                    case TokenType.Operator:
                        if (token.Tag == TokenTag.Unary)
                        {
                            var value = Convert.ToDecimal(stack.Pop(), CultureInfo.InvariantCulture);
                            stack.Push(token.Value == "-" ? -value : value);
                        }
                        else
                        {
                            var right = Convert.ToDecimal(stack.Pop(), CultureInfo.InvariantCulture);
                            var left = Convert.ToDecimal(stack.Pop(), CultureInfo.InvariantCulture);
                            stack.Push(token.Value switch
                            {
                                "+" => left + right,
                                "-" => left - right,
                                "*" => left * right,
                                "/" => right != 0 ? left / right : 0m,
                                _ => 0m,
                            });
                        }

                        break;

                    case TokenType.CallStart:
                        stack.Push(TokenType.CallStart);
                        break;

                    case TokenType.CallEnd:
                        var arguments = new Stack<object>();
                        while (stack.Count > 0 && stack.Peek() is not TokenType)
                            arguments.Push(stack.Pop());

                        if (stack.Count > 0)
                            stack.Pop();

                        if (stack.Count > 0
                            && stack.Peek() is Func<decimal, decimal> function
                            && arguments.Count > 0)
                        {
                            stack.Pop();
                            stack.Push(function(Convert.ToDecimal(arguments.Pop(), CultureInfo.InvariantCulture)));
                        }

                        break;
                }
            }

            return stack.Count > 0
                ? Convert.ToDecimal(stack.Pop(), CultureInfo.InvariantCulture)
                : 0m;
        }
    }
}
