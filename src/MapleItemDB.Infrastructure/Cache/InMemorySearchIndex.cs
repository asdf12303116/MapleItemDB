namespace MapleItemDB.Infrastructure.Cache;

/// <summary>
/// Id-Name 内存搜索索引 — 实现毫秒级自动补全
/// </summary>
public class InMemorySearchIndex
{
    private List<(int Id, string Name)> _entries = [];
    private Dictionary<int, string> _idToName = [];

    /// <summary>索引是否已加载</summary>
    public bool IsLoaded => _entries.Count > 0;

    /// <summary>索引条目数</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// 从外部数据加载索引
    /// </summary>
    public void Load(IReadOnlyList<(int Id, string Name)> entries)
    {
        _entries = new List<(int Id, string Name)>(entries);
        _idToName = _entries.ToDictionary(e => e.Id, e => e.Name);
    }

    /// <summary>
    /// 按关键词搜索，返回匹配的 (Id, Name) 列表
    /// </summary>
    public IReadOnlyList<(int Id, string Name)> Search(string keyword, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var results = new List<(int Id, string Name)>();
        var kw = keyword.Trim();

        // 优先精确 ID 匹配
        if (int.TryParse(kw, out var exactId) && _idToName.TryGetValue(exactId, out var exactName))
        {
            results.Add((exactId, exactName));
        }

        // 名称包含匹配
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

    /// <summary>
    /// 根据 ID 获取名称
    /// </summary>
    public string? GetName(int itemId)
    {
        return _idToName.TryGetValue(itemId, out var name) ? name : null;
    }
}
