using System.Reflection;
using System.Text.Json;

namespace MapleItemDB.Core.Data;

/// <summary>
/// SN 映射表 — 从嵌入资源加载 itemId → SN 的映射
/// </summary>
public static class SnMap
{
    private static readonly Lazy<Dictionary<int, int>> _map = new(LoadMap);

    private static Dictionary<int, int> LoadMap()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "MapleItemDB.Core.Data.sn_map.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"嵌入资源 {resourceName} 未找到");

        var entries = JsonSerializer.Deserialize<List<SnEntry>>(stream) ?? [];
        var dict = new Dictionary<int, int>(entries.Count);
        foreach (var entry in entries)
        {
            dict[entry.itemId] = entry.SN;
        }
        return dict;
    }

    public static bool TryGetSn(int itemId, out int sn)
    {
        return _map.Value.TryGetValue(itemId, out sn);
    }

    public static int? GetSn(int itemId)
    {
        return _map.Value.TryGetValue(itemId, out var sn) ? sn : null;
    }

    private record SnEntry(int itemId, int SN);
}
