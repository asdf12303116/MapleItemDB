using System.Drawing;
using System.Drawing.Imaging;
using MapleItemDB.WzExtraction.Helpers;
using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// 道具图标导出器 — 将 WZ 中的图标提取为 PNG byte[]
/// </summary>
public class IconExporter
{
    /// <summary>
    /// 从装备 img 节点导出图标
    /// </summary>
    /// <param name="imgNode">装备 .img 节点 (已提取)</param>
    /// <param name="itemId">道具 ID</param>
    /// <returns>PNG 二进制数据，失败则返回 null</returns>
    public byte[]? ExportFromEquip(Wz_Node imgNode, int itemId)
    {
        var infoNode = imgNode.Nodes["info"];
        if (infoNode == null) return null;

        // 优先 info/iconRaw, info/icon
        var iconNode = infoNode.Nodes["iconRaw"] ?? infoNode.Nodes["icon"];
        var result = ExportPngNode(iconNode);
        if (result != null) return result;

        // 回退: 脸型/外挂等道具可能图标在 default/face 或 0/face
        foreach (var fallbackPath in FallbackIconPaths)
        {
            var node = NavigatePath(imgNode, fallbackPath);
            result = ExportPngNode(node);
            if (result != null) return result;
        }

        return null;
    }

    // 脸型/外挂类道具的回退图标路径
    private static readonly string[][] FallbackIconPaths =
    [
        ["default", "face"],
        ["0", "face"],
        ["default", "0"],
        ["0", "0"],
    ];

    // 预览图路径配置 (按子分类)
    private static readonly Dictionary<string, string[][]> PreviewPaths = new()
    {
        ["Hair"] =
        [
            ["default", "hairOverHead"],
            ["stand1", "0", "hairOverHead"],
            ["default", "hair"],
            ["stand1", "0", "hair"],
        ],
        ["Face"] =
        [
            ["default", "face"],
            ["brow", "0", "face"],
            ["0", "face"],
        ],
        ["Accessory"] =
        [
            ["default", "face"],
            ["brow", "0", "face"],
            ["0", "face"],
        ],
        ["EyeDecoration"] =
        [
            ["default", "face"],
            ["brow", "0", "face"],
            ["0", "face"],
        ],
        ["Earring"] =
        [
            ["default", "earring"],
            ["brow", "0", "earring"],
        ],
    };

    // 通用回退预览路径
    private static readonly string[][] FallbackPreviewPaths =
    [
        ["default", "0"],
        ["stand1", "0", "0"],
        ["0", "0"],
    ];

    /// <summary>
    /// 导出外观装备的预览图
    /// </summary>
    /// <param name="imgNode">装备 .img 节点 (已提取)</param>
    /// <param name="itemId">道具 ID</param>
    /// <param name="subCategory">子分类</param>
    /// <returns>PNG 二进制数据，失败则返回 null</returns>
    public byte[]? ExportPreview(Wz_Node imgNode, int itemId, string subCategory)
    {
        // 根据子分类获取预览路径列表
        var paths = PreviewPaths.GetValueOrDefault(subCategory) ?? [];

        // 遍历优先路径
        foreach (var pathSegments in paths)
        {
            var node = NavigatePath(imgNode, pathSegments);
            var result = ExportPngNode(node);
            if (result != null) return result;
        }

        // 通用回退
        foreach (var pathSegments in FallbackPreviewPaths)
        {
            var node = NavigatePath(imgNode, pathSegments);
            var result = ExportPngNode(node);
            if (result != null) return result;
        }

        return null;
    }

    private static Wz_Node? NavigatePath(Wz_Node root, string[] segments)
    {
        var current = root;
        foreach (var seg in segments)
        {
            current = current.Nodes[seg];
            if (current == null) return null;
        }
        return current;
    }

    /// <summary>
    /// 从普通道具 id 节点导出图标
    /// </summary>
    /// <param name="idNode">道具 ID 节点 (在 .img 内部)</param>
    /// <param name="itemId">道具 ID</param>
    /// <returns>PNG 二进制数据，失败则返回 null</returns>
    public byte[]? ExportFromItem(Wz_Node idNode, int itemId)
    {
        // 普通道具图标路径: info/icon 或 info/iconRaw
        var infoNode = idNode.Nodes["info"];
        if (infoNode == null) return null;

        var iconNode = infoNode.Nodes["iconRaw"] ?? infoNode.Nodes["icon"];
        return ExportPngNode(iconNode);
    }

    private byte[]? ExportPngNode(Wz_Node? iconNode)
    {
        // 使用 ResolvePng 处理 UOL / _inlink / _outlink 引用
        using var bitmap = iconNode.ResolvePng();
        if (bitmap == null)
            return null;

        try
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
