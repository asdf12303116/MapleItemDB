namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 内存搜索索引接口
/// </summary>
public interface ISearchIndex
{
    bool IsLoaded { get; }

    int Count { get; }

    void Load(IReadOnlyList<(int Id, string Name)> entries);

    IReadOnlyList<(int Id, string Name)> Search(string keyword, int limit = 20);

    string? GetName(int itemId);
}
