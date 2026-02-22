namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 缓存管理接口 (图标文件 + 内存搜索索引)
/// </summary>
public interface ICacheManager
{
    /// <summary>获取图标文件本地路径，如不存在则返回 null</summary>
    string? GetIconPath(int itemId);

    /// <summary>保存图标到缓存目录</summary>
    Task SaveIconAsync(int itemId, byte[] pngData);

    /// <summary>清除所有缓存</summary>
    Task ClearAllAsync();
}
