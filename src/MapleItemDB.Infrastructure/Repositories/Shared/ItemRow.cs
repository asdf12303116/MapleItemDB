using MapleItemDB.Core.Models;

namespace MapleItemDB.Infrastructure.Repositories.Shared;

/// <summary>
/// 道具行模型 — 属性名与数据库列名、SQL 参数名对齐
/// </summary>
internal class ItemRow
{
    public int item_id { get; set; }
    public string name { get; set; } = "";
    public string? description { get; set; }
    public string category { get; set; } = "";
    public string? sub_category { get; set; }
    public int? req_level { get; set; }
    public int? req_str { get; set; }
    public int? req_dex { get; set; }
    public int? req_int { get; set; }
    public int? req_luk { get; set; }
    public int? req_job { get; set; }
    public int? inc_str { get; set; }
    public int? inc_dex { get; set; }
    public int? inc_int { get; set; }
    public int? inc_luk { get; set; }
    public int? inc_pad { get; set; }
    public int? inc_mad { get; set; }
    public int? inc_pdd { get; set; }
    public int? inc_mdd { get; set; }
    public int? inc_mhp { get; set; }
    public int? inc_mmp { get; set; }
    public string? dynamic_stats { get; set; }
    public string? consume_spec { get; set; }
    public int is_cash { get; set; }
    public int? price { get; set; }
    public byte[]? icon_data { get; set; }
    public byte[]? preview_data { get; set; }
    public int? setitem_id { get; set; }
    public int? sn { get; set; }
    public int time_limited { get; set; }
    public string extracted_at { get; set; } = "";

    public ItemEntity ToEntity() => new()
    {
        ItemId = item_id,
        Name = name,
        Description = description,
        Category = Enum.TryParse<ItemCategory>(category, out var cat) ? cat : ItemCategory.Etc,
        SubCategory = sub_category,
        ReqLevel = req_level,
        ReqStr = req_str,
        ReqDex = req_dex,
        ReqInt = req_int,
        ReqLuk = req_luk,
        ReqJob = req_job,
        IncSTR = inc_str,
        IncDEX = inc_dex,
        IncINT = inc_int,
        IncLUK = inc_luk,
        IncPAD = inc_pad,
        IncMAD = inc_mad,
        IncPDD = inc_pdd,
        IncMDD = inc_mdd,
        IncMHP = inc_mhp,
        IncMMP = inc_mmp,
        DynamicStats = dynamic_stats,
        ConsumeSpec = consume_spec,
        IsCash = is_cash != 0,
        Price = price,
        IconData = icon_data,
        PreviewData = preview_data,
        SetItemId = setitem_id,
        Sn = sn,
        TimeLimited = time_limited != 0,
        ExtractedAt = DateTime.TryParse(extracted_at, out var dt) ? dt : DateTime.MinValue,
    };

    public static ItemRow FromEntity(ItemEntity e) => new()
    {
        item_id = e.ItemId,
        name = e.Name,
        description = e.Description,
        category = e.Category.ToString(),
        sub_category = e.SubCategory,
        req_level = e.ReqLevel,
        req_str = e.ReqStr,
        req_dex = e.ReqDex,
        req_int = e.ReqInt,
        req_luk = e.ReqLuk,
        req_job = e.ReqJob,
        inc_str = e.IncSTR,
        inc_dex = e.IncDEX,
        inc_int = e.IncINT,
        inc_luk = e.IncLUK,
        inc_pad = e.IncPAD,
        inc_mad = e.IncMAD,
        inc_pdd = e.IncPDD,
        inc_mdd = e.IncMDD,
        inc_mhp = e.IncMHP,
        inc_mmp = e.IncMMP,
        dynamic_stats = e.DynamicStats,
        consume_spec = e.ConsumeSpec,
        is_cash = e.IsCash ? 1 : 0,
        price = e.Price,
        icon_data = e.IconData,
        preview_data = e.PreviewData,
        setitem_id = e.SetItemId,
        sn = e.Sn,
        time_limited = e.TimeLimited ? 1 : 0,
        extracted_at = e.ExtractedAt.ToString("O"),
    };
}
