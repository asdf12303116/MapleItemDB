using MapleItemDB.Core.Interfaces;

namespace MapleItemDB.Infrastructure.Cache;

/// <summary>
/// Id-Name 内存搜索索引，支持毫秒级自动补全。
/// </summary>
public class InMemorySearchIndex : ISearchIndex
{
    private List<(int Id, string Name)> _entries = [];
    private Dictionary<int, string> _idToName = [];

    public bool IsLoaded => _entries.Count > 0;

    public int Count => _entries.Count;

    public void Load(IReadOnlyList<(int Id, string Name)> entries)
    {
        _entries = new List<(int Id, string Name)>(entries);
        _idToName = _entries.ToDictionary(e => e.Id, e => e.Name);
    }

    public IReadOnlyList<(int Id, string Name)> Search(string keyword, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var results = new List<(int Id, string Name)>();
        var kw = keyword.Trim();

        if (int.TryParse(kw, out var exactId) && _idToName.TryGetValue(exactId, out var exactName))
            results.Add((exactId, exactName));

        foreach (var entry in _entries)
        {
            if (results.Count >= limit)
                break;

            if (entry.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                && !results.Any(r => r.Id == entry.Id))
            {
                results.Add(entry);
            }
        }

        return results;
    }

    public string? GetName(int itemId)
    {
        return _idToName.TryGetValue(itemId, out var name) ? name : null;
    }
}
