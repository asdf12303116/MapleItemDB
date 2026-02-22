using System.Text.RegularExpressions;
using MapleItemDB.Core.Interfaces;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// 宏变量解析器，处理 WZ 文本中的格式化标记
/// 例如: #c文本# → 文本, #itemName[1234]# 等
/// </summary>
public partial class MacroResolver : IStringResolver
{
    private readonly Dictionary<int, StringPoolBuilder.StringEntry> _stringPool;

    public MacroResolver(IReadOnlyDictionary<int, StringPoolBuilder.StringEntry> stringPool)
    {
        _stringPool = new Dictionary<int, StringPoolBuilder.StringEntry>(stringPool);
    }

    public string? GetName(int itemId)
    {
        if (_stringPool.TryGetValue(itemId, out var entry))
            return entry.Name != null ? ResolveMacros(entry.Name) : null;
        return null;
    }

    public string? GetDescription(int itemId)
    {
        if (_stringPool.TryGetValue(itemId, out var entry))
            return entry.Description != null ? ResolveMacros(entry.Description) : null;
        return null;
    }

    /// <summary>
    /// 解析宏变量文本
    /// 常见标记:
    ///   #c...# — 橙色文本 (移除标记，保留内容)
    ///   #r...# — 红色文本
    ///   #b...# — 蓝色文本
    ///   #n — 普通文本恢复
    ///   \\n — 换行
    /// </summary>
    public string ResolveMacros(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
            return rawText;

        // 移除颜色标记 (#c...#, #r...#, #b...# 等)
        var result = ColorTagRegex().Replace(rawText, "$1");

        // 移除单独的 #n 标记
        result = result.Replace("#n", "");

        // 处理换行
        result = result.Replace("\\n", "\n");
        result = result.Replace("\\r", "");

        return result.Trim();
    }

    [GeneratedRegex(@"#[cCrRbBdDeFfFgGkK]([^#]*)#")]
    private static partial Regex ColorTagRegex();
}
