using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// 从 String.wz 构建道具字符串池 (id → name/desc)
/// </summary>
public class StringPoolBuilder
{
    /// <summary>字符串条目 (名称 + 描述)</summary>
    public record StringEntry(string? Name, string? Description);

    private readonly Dictionary<int, StringEntry> _pool = new();

    /// <summary>
    /// 获取已构建的字符串池
    /// </summary>
    public IReadOnlyDictionary<int, StringEntry> Pool => _pool;

    /// <summary>
    /// 从 String 节点构建字符串池
    /// </summary>
    public void Build(Wz_Node stringWzNode)
    {
        _pool.Clear();

        // String 下包含: Eqp.img, Consume.img, Etc.img, Ins.img (Setup), Cash.img, Pet.img 等
        BuildFromEqp(stringWzNode);
        BuildFromCategory(stringWzNode, "Consume.img");
        BuildFromCategory(stringWzNode, "Etc.img");
        BuildFromCategory(stringWzNode, "Ins.img");    // Setup/Install
        BuildFromCategory(stringWzNode, "Cash.img");
        BuildFromCategory(stringWzNode, "Pet.img");

        // 技能字符串
        BuildFromSkill(stringWzNode);
    }

    /// <summary>
    /// 装备字符串有额外一层子分类 (Eqp.img → Eqp → Hat/Coat/Weapon/...)
    /// </summary>
    private void BuildFromEqp(Wz_Node stringNode)
    {
        var eqpRoot = GetExtractedImgNode(stringNode, "Eqp.img");
        if (eqpRoot == null) return;

        // Eqp.img → Eqp → (Hat/Coat/Weapon/...) → (itemId nodes)
        var eqpCategory = eqpRoot.Nodes["Eqp"];
        if (eqpCategory == null) return;

        foreach (var subCat in eqpCategory.Nodes)
        {
            foreach (var idNode in subCat.Nodes)
            {
                if (int.TryParse(idNode.Text, out var itemId))
                {
                    AddEntry(itemId, idNode);
                }
            }
        }
    }

    /// <summary>
    /// 通用分类字符串解析 (Consume, Etc, Ins, Cash, Pet)
    /// </summary>
    private void BuildFromCategory(Wz_Node stringNode, string imgName)
    {
        var root = GetExtractedImgNode(stringNode, imgName);
        if (root == null) return;

        foreach (var idNode in root.Nodes)
        {
            if (int.TryParse(idNode.Text, out var itemId))
            {
                AddEntry(itemId, idNode);
            }
        }
    }

    /// <summary>
    /// 技能字符串解析 (Skill.img → skillId → name/bookName/desc)
    /// 技能 ID 与道具 ID 不重叠，可以放在同一个池中
    /// </summary>
    private void BuildFromSkill(Wz_Node stringNode)
    {
        var root = GetExtractedImgNode(stringNode, "Skill.img");
        if (root == null) return;

        foreach (var idNode in root.Nodes)
        {
            if (int.TryParse(idNode.Text, out var skillId))
            {
                // 技能名优先 bookName (技能书名)，然后 name
                string? name = null;
                var nameNode = idNode.Nodes["name"];
                if (nameNode != null)
                    name = nameNode.Value?.ToString();

                var bookNameNode = idNode.Nodes["bookName"];
                // bookName 作为备用描述，name 仍为主名称
                string? desc = null;
                if (bookNameNode != null)
                    desc = bookNameNode.Value?.ToString();

                var descNode = idNode.Nodes["desc"];
                if (descNode != null)
                    desc = descNode.Value?.ToString();

                if (name != null)
                    _pool[skillId] = new StringEntry(name, desc);
            }
        }
    }

    private void AddEntry(int itemId, Wz_Node idNode)
    {
        string? name = null;
        string? desc = null;

        var nameNode = idNode.Nodes["name"];
        if (nameNode != null)
            name = nameNode.Value?.ToString();

        var descNode = idNode.Nodes["desc"];
        if (descNode != null)
            desc = descNode.Value?.ToString();

        _pool[itemId] = new StringEntry(name, desc);
    }

    /// <summary>
    /// 安全获取并提取 Wz_Image 节点，返回其内部 Node
    /// 不使用 FindNodeByPath(extractImage: true)，避免在文件夹式格式下出现问题
    /// </summary>
    private static Wz_Node? GetExtractedImgNode(Wz_Node parent, string imgName)
    {
        var node = parent.Nodes[imgName];
        if (node == null) return null;

        if (node.Value is Wz_Image img)
        {
            return img.TryExtract() ? img.Node : null;
        }

        // 已经被提取过或不是 Wz_Image
        return node;
    }
}
