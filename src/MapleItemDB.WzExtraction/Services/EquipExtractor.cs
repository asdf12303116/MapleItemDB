using System.Text.Json;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.WzExtraction.Helpers;
using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// 装备类道具提取器 — 从 Character 节点提取装备数据
/// </summary>
public class EquipExtractor
{
    private readonly IStringResolver _stringResolver;

    public EquipExtractor(IStringResolver stringResolver)
    {
        _stringResolver = stringResolver;
    }

    /// <summary>
    /// 从 Character 节点提取所有装备道具
    /// 支持传统格式和 KMST1125 文件夹式格式
    /// </summary>
    public List<ItemEntity> Extract(Wz_Node characterNode)
    {
        var items = new List<ItemEntity>();

        // Character 下有子分类目录: Weapon, Cap, Coat, Pants, Shoes, ...
        foreach (var subCatNode in characterNode.Nodes)
        {
            var subCatName = subCatNode.Text;

            // 跳过特殊节点 (如 _Canvas, Afterimage, Bits 等非装备目录)
            if (subCatName.StartsWith("_") || subCatName == "Afterimage")
                continue;

            // 遍历子分类下的所有 .img 节点
            // 文件夹式格式: 子节点直接就是 Wz_Image (如 01002000.img)
            // 传统格式: 子节点也是 Wz_Image
            foreach (var imgNode in subCatNode.Nodes)
            {
                if (imgNode.Value is not Wz_Image img)
                    continue;

                // 从文件名提取 itemId: "01002000.img" → 1002000
                var imgName = imgNode.Text;
                if (!imgName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                    continue;

                var idStr = imgName[..^4]; // 去掉 .img
                if (!int.TryParse(idStr, out var itemId))
                    continue;

                if (!img.TryExtract())
                    continue;

                var entity = ParseEquipNode(itemId, img.Node, subCatName);
                if (entity != null)
                    items.Add(entity);
            }
        }

        return items;
    }

    private ItemEntity? ParseEquipNode(int itemId, Wz_Node imgRoot, string subCategory)
    {
        var infoNode = imgRoot.Nodes["info"];
        if (infoNode == null)
            return null;

        var entity = new ItemEntity
        {
            ItemId = itemId,
            Name = _stringResolver.GetName(itemId) ?? $"Unknown-{itemId}",
            Description = _stringResolver.GetDescription(itemId),
            Category = ItemCategory.Equip,
            SubCategory = ResolveSubCategory(itemId, subCategory),

            // 需求属性
            ReqLevel = infoNode.GetIntValue("reqLevel"),
            ReqStr = infoNode.GetIntValue("reqSTR"),
            ReqDex = infoNode.GetIntValue("reqDEX"),
            ReqInt = infoNode.GetIntValue("reqINT"),
            ReqLuk = infoNode.GetIntValue("reqLUK"),

            // 核心数值
            IncSTR = infoNode.GetIntValue("incSTR"),
            IncDEX = infoNode.GetIntValue("incDEX"),
            IncINT = infoNode.GetIntValue("incINT"),
            IncLUK = infoNode.GetIntValue("incLUK"),
            IncPAD = infoNode.GetIntValue("incPAD"),
            IncMAD = infoNode.GetIntValue("incMAD"),
            IncPDD = infoNode.GetIntValue("incPDD"),
            IncMDD = infoNode.GetIntValue("incMDD"),
            IncMHP = infoNode.GetIntValue("incMHP"),
            IncMMP = infoNode.GetIntValue("incMMP"),

            // 元数据
            IsCash = infoNode.GetBoolValue("cash"),
            Price = infoNode.GetIntValue("price"),
            SetItemId = infoNode.GetIntValue("setItemID"),
            ExtractedAt = DateTime.UtcNow,
        };

        // 收集动态属性
        var dynamicStats = new Dictionary<string, object>();
        TryAddDynamic(dynamicStats, infoNode, "bdR", "boss_dmg");
        TryAddDynamic(dynamicStats, infoNode, "imdR", "ied");
        TryAddDynamic(dynamicStats, infoNode, "damR", "total_dmg");
        TryAddDynamic(dynamicStats, infoNode, "statR", "all_stat_pct");
        TryAddDynamic(dynamicStats, infoNode, "incAllStat", "all_stat");
        TryAddDynamic(dynamicStats, infoNode, "incSpeed", "speed");
        TryAddDynamic(dynamicStats, infoNode, "incJump", "jump");
        TryAddDynamic(dynamicStats, infoNode, "knockback", "knockback");
        TryAddDynamic(dynamicStats, infoNode, "attackSpeed", "attack_speed");
        TryAddDynamic(dynamicStats, infoNode, "tuc", "upgrade_slots");

        // 特殊标志 (存为 1/0)
        TryAddFlag(dynamicStats, infoNode, "tradeBlock",       "_flag_tradeBlock");
        TryAddFlag(dynamicStats, infoNode, "equipTradeBlock",   "_flag_equipTradeBlock");
        TryAddFlag(dynamicStats, infoNode, "only",              "_flag_only");
        TryAddFlag(dynamicStats, infoNode, "accountSharable",   "_flag_accountSharable");
        TryAddFlag(dynamicStats, infoNode, "timeLimited",       "_flag_timeLimited");
        TryAddFlag(dynamicStats, infoNode, "superiorEqp",       "_flag_superiorEqp");
        TryAddFlag(dynamicStats, infoNode, "noPotential",       "_flag_noPotential");
        TryAddFlag(dynamicStats, infoNode, "fixedPotential",    "_flag_fixedPotential");

        if (dynamicStats.Count > 0)
            entity.DynamicStats = JsonSerializer.Serialize(dynamicStats);

        return entity;
    }

    private static void TryAddDynamic(Dictionary<string, object> dict, Wz_Node infoNode, string wzKey, string jsonKey)
    {
        var val = infoNode.GetIntValue(wzKey);
        if (val.HasValue)
            dict[jsonKey] = val.Value;
    }

    private static void TryAddFlag(Dictionary<string, object> dict, Wz_Node infoNode, string wzKey, string jsonKey)
    {
        if (infoNode.GetBoolValue(wzKey))
            dict[jsonKey] = 1;
    }

    /// <summary>
    /// 根据道具 ID 前缀修正子分类
    /// WZ 中部分道具（如护肩、眼饰）存放在 Accessory 文件夹下，
    /// 需要通过 ID 段区分实际类型
    /// </summary>
    private static readonly Dictionary<int, string> IdPrefixToSubCategory = new()
    {
        [100] = "Cap",
        [101] = "Accessory",    // 脸饰
        [102] = "EyeDecoration", // 眼饰
        [103] = "Earring",
        [104] = "Coat",
        [105] = "Longcoat",
        [106] = "Pants",
        [107] = "Shoes",
        [108] = "Glove",
        [109] = "Shield",
        [110] = "Cape",
        [111] = "Ring",
        [112] = "Pendant",
        [113] = "Belt",
        [114] = "Medal",
        [115] = "Shoulder",
        [116] = "Pocket",
        [117] = "Badge",
        [118] = "Badge",
        [119] = "Emblem",
        // 高段位装备
        [167] = "Heart",
    };

    private static string ResolveSubCategory(int itemId, string folderName)
    {
        var prefix = itemId / 10000;
        if (IdPrefixToSubCategory.TryGetValue(prefix, out var resolved))
            return resolved;
        return folderName;
    }
}
