namespace MapleItemDB.Core.Interfaces;

/// <summary>
/// 字符串/宏变量解析接口
/// </summary>
public interface IStringResolver
{
    /// <summary>根据道具 ID 获取名称</summary>
    string? GetName(int itemId);

    /// <summary>根据道具 ID 获取描述</summary>
    string? GetDescription(int itemId);

    /// <summary>
    /// 解析宏变量文本，如 #c[text]# 等格式化标记
    /// </summary>
    string ResolveMacros(string rawText);
}
