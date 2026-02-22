using System.Drawing;
using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Helpers;

/// <summary>
/// Wz_Node 扩展方法，简化节点值读取
/// </summary>
public static class WzNodeExtensions
{
    /// <summary>
    /// 获取子节点的 int 值，不存在则返回 null
    /// </summary>
    public static int? GetIntValue(this Wz_Node node, string childName)
    {
        var child = node.Nodes[childName];
        if (child == null) return null;

        var val = child.Value;
        return val switch
        {
            int i => i,
            long l => (int)l,
            short s => s,
            float f => (int)f,
            double d => (int)d,
            string str when int.TryParse(str, out var parsed) => parsed,
            _ => null,
        };
    }

    /// <summary>
    /// 获取子节点的 string 值
    /// </summary>
    public static string? GetStringValue(this Wz_Node node, string childName)
    {
        var child = node.Nodes[childName];
        return child?.Value?.ToString();
    }

    /// <summary>
    /// 获取子节点的 bool 值 (WZ 中通常以 int 0/1 存储)
    /// </summary>
    public static bool GetBoolValue(this Wz_Node node, string childName, bool defaultValue = false)
    {
        var val = node.GetIntValue(childName);
        return val.HasValue ? val.Value != 0 : defaultValue;
    }

    /// <summary>
    /// 确保 Wz_Image 被解压，返回其 Node
    /// </summary>
    public static Wz_Node? ExtractAndGetNode(this Wz_Node node)
    {
        if (node.Value is Wz_Image img)
        {
            if (img.TryExtract())
                return img.Node;
            return null;
        }
        return node;
    }

    /// <summary>
    /// 遍历所有含 Wz_Image 的子节点 (递归)
    /// </summary>
    public static IEnumerable<Wz_Node> EnumerateImageNodes(this Wz_Node node)
    {
        foreach (var child in node.Nodes)
        {
            if (child.Value is Wz_Image)
            {
                yield return child;
            }
            else
            {
                foreach (var descendant in child.EnumerateImageNodes())
                    yield return descendant;
            }
        }
    }

    /// <summary>
    /// 解析 _inlink / _outlink 引用，返回实际的源节点。
    /// _inlink 是同一 img 内部的路径引用，_outlink 是跨 img 引用。
    /// ownerWzImage 可选，用于在已解压内容树中正确定位 img 根节点。
    /// </summary>
    public static Wz_Node? ResolveLinkedSourceNode(this Wz_Node node, Wz_Image? ownerWzImage = null)
    {
        // 处理 _inlink: 在同一 Wz_Image 内部按路径查找
        var inlinkNode = node.Nodes["_inlink"];
        if (inlinkNode?.Value is string inlink && !string.IsNullOrEmpty(inlink))
        {
            // 优先通过 Wz_Png.WzImage 获取 img 根节点（解压后节点树中唯一可靠方式）
            Wz_Node? imgRoot = null;
            if (ownerWzImage != null)
            {
                ownerWzImage.TryExtract();
                imgRoot = ownerWzImage.Node;
            }
            if (imgRoot == null && node.Value is Wz_Png png && png.WzImage != null)
            {
                png.WzImage.TryExtract();
                imgRoot = png.WzImage.Node;
            }

            if (imgRoot != null)
            {
                var target = NavigateByPath(imgRoot, inlink);
                if (target != null)
                    return target;
            }
        }

        // 处理 _outlink: 从 WZ 虚拟文件系统根节点按完整路径查找
        var outlinkNode = node.Nodes["_outlink"];
        if (outlinkNode?.Value is string outlink && !string.IsNullOrEmpty(outlink))
        {
            // 通过 Wz_Image.OwnerNode 回到 WZ 文件树，再向上找根
            Wz_Node? wzRoot = null;
            var wzImage = ownerWzImage
                ?? (node.Value is Wz_Png p ? p.WzImage : null);
            if (wzImage?.OwnerNode != null)
            {
                wzRoot = FindWzRootNode(wzImage.OwnerNode);
            }

            if (wzRoot != null)
            {
                var target = NavigateByPath(wzRoot, outlink);
                if (target != null)
                    return target;
            }
        }

        return null;
    }

    /// <summary>
    /// 从节点解析出 Bitmap，依次处理: UOL → 1x1链接占位图 → 直接 Wz_Png
    /// 关键: WZ 中的链接 PNG 特征为 Width=1, Height=1，必须先解析 _inlink/_outlink
    /// </summary>
    public static Bitmap? ResolvePng(this Wz_Node? node, int maxDepth = 10)
    {
        if (node == null || maxDepth <= 0)
            return null;

        var value = node.Value;

        // 1. UOL (用户对象链接) — 解引用后递归
        if (value is Wz_Uol uol)
        {
            var target = uol.HandleUol(node);
            return ResolvePng(target, maxDepth - 1);
        }

        // 2. Wz_Png — 检查是否为 1x1 链接占位图
        if (value is Wz_Png png)
        {
            if (png.Width <= 1 && png.Height <= 1)
            {
                // 1x1 占位图，真实数据在 _inlink/_outlink 中
                var linked = node.ResolveLinkedSourceNode(png.WzImage);
                if (linked != null)
                {
                    var result = ResolvePng(linked, maxDepth - 1);
                    if (result != null)
                        return result;
                }
                // 链接解析失败，不返回 1x1 占位图，让调用方走回退路径
                return null;
            }
            // 正常尺寸 PNG 直接提取
            return png.ExtractPng();
        }

        // 3. 非 Wz_Png 节点但可能有 _inlink/_outlink 子节点
        var linkedNode = node.ResolveLinkedSourceNode();
        if (linkedNode != null)
            return ResolvePng(linkedNode, maxDepth - 1);

        return null;
    }

    /// <summary>
    /// 向上遍历找到 WZ 虚拟文件系统根节点
    /// </summary>
    private static Wz_Node? FindWzRootNode(Wz_Node node)
    {
        var current = node;
        while (current.ParentNode != null)
            current = current.ParentNode;
        return current;
    }

    /// <summary>
    /// 按 "/" 分隔的路径导航节点
    /// </summary>
    private static Wz_Node? NavigateByPath(Wz_Node root, string path)
    {
        var current = root;
        foreach (var segment in path.Split('/'))
        {
            if (string.IsNullOrEmpty(segment))
                continue;

            var child = current.Nodes[segment];
            if (child == null)
            {
                // 尝试自动解压 Wz_Image
                if (current.Value is Wz_Image img && img.TryExtract())
                {
                    child = img.Node.Nodes[segment];
                }
                if (child == null)
                    return null;
            }

            // 如果子节点是 Wz_Image，需要解压后继续
            if (child.Value is Wz_Image childImg)
            {
                if (!childImg.TryExtract())
                    return null;
                current = childImg.Node;
            }
            else
            {
                current = child;
            }
        }
        return current;
    }
}
