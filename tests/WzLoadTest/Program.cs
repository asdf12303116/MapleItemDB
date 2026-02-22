using MapleItemDB.Core.Models;
using MapleItemDB.WzExtraction.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var dataDir = @"D:\GAME\MapleStory228\Data";
var iconDir = ""; // 暂时不导出图标

Console.WriteLine($"开始提取: {dataDir}");

var progress = new Progress<MapleItemDB.Core.Interfaces.ExtractionProgress>(p =>
{
    Console.WriteLine($"[{p.Phase}] {p.Current}/{p.Total} - {p.Message}");
});

using var extractor = new WzExtractionService();

try
{
    var items = await extractor.ExtractAllAsync(dataDir, iconDir, progress);

    Console.WriteLine($"\n=== 提取完成 ===");
    Console.WriteLine($"总道具数: {items.Count}");

    // 按分类统计
    var grouped = items.GroupBy(i => i.Category).OrderBy(g => g.Key);
    foreach (var g in grouped)
    {
        Console.WriteLine($"  {g.Key}: {g.Count()}");
    }

    // 示例道具
    Console.WriteLine("\n--- 前 10 个装备 ---");
    foreach (var item in items.Where(i => i.Category == ItemCategory.Equip).Take(10))
    {
        Console.WriteLine($"  [{item.ItemId}] {item.Name} (Lv.{item.ReqLevel}) ATK:{item.IncPAD} MATK:{item.IncMAD} | {item.SubCategory}");
    }

    Console.WriteLine("\n--- 前 10 个消耗品 ---");
    foreach (var item in items.Where(i => i.Category == ItemCategory.Consume).Take(10))
    {
        Console.WriteLine($"  [{item.ItemId}] {item.Name} | spec: {item.ConsumeSpec ?? "null"}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
