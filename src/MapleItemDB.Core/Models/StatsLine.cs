namespace MapleItemDB.Core.Models;

/// <summary>
/// 属性行模型 — 用于 UI 展示道具属性，支持特殊标志高亮
/// </summary>
/// <param name="Text">显示文本</param>
/// <param name="IsFlag">是否为特殊标志（如"不可交易"等，橙色高亮置顶）</param>
public record StatsLine(string Text, bool IsFlag = false);
