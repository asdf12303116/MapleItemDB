using WzComparerR2.WzLib;

namespace MapleItemDB.WzExtraction.Services;

/// <summary>
/// 从 String.wz 构建字符串池（分离道具与技能，避免同 ID 冲突）
/// </summary>
public class StringPoolBuilder
{
    /// <summary>字符串条目 (名称 + 描述 + 技能等级效果模板)</summary>
    public record StringEntry(string? Name, string? Description, string? SkillH = null);

    private readonly Dictionary<int, StringEntry> _itemPool = new();
    private readonly Dictionary<int, StringEntry> _skillPool = new();

    /// <summary>
    /// 获取道具字符串池
    /// </summary>
    public IReadOnlyDictionary<int, StringEntry> ItemPool => _itemPool;

    /// <summary>
    /// 获取技能字符串池
    /// </summary>
    public IReadOnlyDictionary<int, StringEntry> SkillPool => _skillPool;

    /// <summary>
    /// 从 String 节点构建字符串池
    /// </summary>
    public void Build(Wz_Node stringWzNode)
    {
        _itemPool.Clear();
        _skillPool.Clear();

        // String 下包含: Eqp.img, Consume.img, Etc.img, Ins.img (Setup), Cash.img, Pet.img 等
        BuildFromEqp(stringWzNode);
        BuildFromCategory(stringWzNode, "Consume.img");
        BuildFromNestedCategory(stringWzNode, "Etc.img");
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
                    AddItemEntry(itemId, idNode);
                }
            }
        }
    }

    /// <summary>
    /// 通用分类字符串解析 (Consume, Ins, Cash, Pet)
    /// </summary>
    private void BuildFromCategory(Wz_Node stringNode, string imgName)
    {
        var root = GetExtractedImgNode(stringNode, imgName);
        if (root == null) return;

        foreach (var idNode in root.Nodes)
        {
            if (int.TryParse(idNode.Text, out var itemId))
            {
                AddItemEntry(itemId, idNode);
            }
        }
    }

    /// <summary>
    /// 带子分类的字符串解析 (Etc.img 有额外一层: Etc.img → 子分类 → itemId)
    /// </summary>
    private void BuildFromNestedCategory(Wz_Node stringNode, string imgName)
    {
        var root = GetExtractedImgNode(stringNode, imgName);
        if (root == null) return;

        foreach (var subCatNode in root.Nodes)
        {
            foreach (var idNode in subCatNode.Nodes)
            {
                if (int.TryParse(idNode.Text, out var itemId))
                {
                    AddItemEntry(itemId, idNode);
                }
            }
        }
    }

    /// <summary>
    /// 技能字符串解析 (Skill.img → skillId → name/bookName/desc)
    /// </summary>
    private void BuildFromSkill(Wz_Node stringNode)
    {
        var root = GetExtractedImgNode(stringNode, "Skill.img");
        if (root == null) return;

        foreach (var idNode in root.Nodes)
        {
            if (int.TryParse(idNode.Text, out var skillId))
            {
                string? name = null;
                var nameNode = idNode.Nodes["name"];
                if (nameNode != null)
                    name = nameNode.Value?.ToString();

                string? desc = null;
                var bookNameNode = idNode.Nodes["bookName"];
                if (bookNameNode != null)
                    desc = bookNameNode.Value?.ToString();

                var descNode = idNode.Nodes["desc"];
                if (descNode != null)
                    desc = descNode.Value?.ToString();

                // 读取技能等级效果模板 (h)
                string? skillH = null;
                var hNode = idNode.Nodes["h"];
                if (hNode != null)
                    skillH = hNode.Value?.ToString();

                // 如果没有 h，尝试 h1, h2... 拼接
                if (skillH == null)
                {
                    var parts = new List<string>();
                    for (int i = 1; ; i++)
                    {
                        var hiNode = idNode.Nodes["h" + i];
                        if (hiNode?.Value?.ToString() is string hi && !string.IsNullOrEmpty(hi))
                            parts.Add(hi);
                        else
                            break;
                    }
                    if (parts.Count > 0)
                        skillH = string.Join("\n", parts);
                }

                if (name != null)
                    _skillPool[skillId] = new StringEntry(name, desc, skillH);
            }
        }
    }

    private void AddItemEntry(int itemId, Wz_Node idNode)
    {
        string? name = null;
        string? desc = null;

        var nameNode = idNode.Nodes["name"];
        if (nameNode != null)
            name = nameNode.Value?.ToString();

        var descNode = idNode.Nodes["desc"];
        if (descNode != null)
            desc = descNode.Value?.ToString();

        _itemPool[itemId] = new StringEntry(name, desc);
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
