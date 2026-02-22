using System.Text.Json;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.WzExtraction.Helpers;
using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// 通用道具提取器 — 处理 Item.wz 下的 Consume/Etc/Install/Cash/Pet
/// </summary>
public class GeneralItemExtractor
{
    private readonly IStringResolver _stringResolver;

    public GeneralItemExtractor(IStringResolver stringResolver)
    {
        _stringResolver = stringResolver;
    }

    /// <summary>
    /// 从 Item.wz 的某个分类节点提取道具
    /// </summary>
    /// <param name="categoryNode">如 Item.wz 下的 Consume, Etc, Install, Cash, Pet 节点</param>
    /// <param name="category">目标分类</param>
    public List<ItemEntity> Extract(Wz_Node categoryNode, ItemCategory category)
    {
        var items = new List<ItemEntity>();

        // Item.wz/Consume/ 下按 ID 前缀分组: 0200.img, 0201.img, ...
        // 每个 .img 内包含多个 itemId 节点
        foreach (var imgNode in categoryNode.Nodes)
        {
            if (imgNode.Value is not Wz_Image img)
                continue;
            if (!img.TryExtract())
                continue;

            foreach (var idNode in img.Node.Nodes)
            {
                if (!int.TryParse(idNode.Text, out var itemId))
                    continue;

                var entity = ParseItemNode(itemId, idNode, category);
                if (entity != null)
                    items.Add(entity);
            }
        }

        return items;
    }

    private ItemEntity? ParseItemNode(int itemId, Wz_Node idNode, ItemCategory category)
    {
        var infoNode = idNode.Nodes["info"];

        var entity = new ItemEntity
        {
            ItemId = itemId,
            Name = _stringResolver.GetName(itemId) ?? $"Unknown-{itemId}",
            Description = _stringResolver.GetDescription(itemId),
            Category = category,
            IsCash = infoNode?.GetBoolValue("cash") ?? false,
            Price = infoNode?.GetIntValue("price"),
            ExtractedAt = DateTime.UtcNow,
        };

        // 消耗品特有属性
        if (category == ItemCategory.Consume)
        {
            var specNode = idNode.Nodes["spec"];
            if (specNode != null)
            {
                var spec = new Dictionary<string, object>();
                TryAddSpec(spec, specNode, "hp");
                TryAddSpec(spec, specNode, "mp");
                TryAddSpec(spec, specNode, "hpR");
                TryAddSpec(spec, specNode, "mpR");
                TryAddSpec(spec, specNode, "time");
                TryAddSpec(spec, specNode, "pad");
                TryAddSpec(spec, specNode, "mad");
                TryAddSpec(spec, specNode, "pdd");
                TryAddSpec(spec, specNode, "mdd");
                TryAddSpec(spec, specNode, "speed");
                TryAddSpec(spec, specNode, "jump");
                TryAddSpec(spec, specNode, "eva");
                TryAddSpec(spec, specNode, "acc");

                if (spec.Count > 0)
                    entity.ConsumeSpec = JsonSerializer.Serialize(spec);
            }
        }

        return entity;
    }

    private static void TryAddSpec(Dictionary<string, object> dict, Wz_Node specNode, string key)
    {
        var val = specNode.GetIntValue(key);
        if (val.HasValue)
            dict[key] = val.Value;
    }
}
