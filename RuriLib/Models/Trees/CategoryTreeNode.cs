using System.Collections.Generic;
using System.Linq;
using RuriLib.Models.Blocks;

namespace RuriLib.Models.Trees;

/// <summary>
/// A node in the hierarchical block category tree.
/// </summary>
public class CategoryTreeNode
{
    /// <summary>
    /// The parent category node, if any.
    /// </summary>
    public CategoryTreeNode? Parent { get; set; }

    /// <summary>
    /// The child categories of this node.
    /// </summary>
    public List<CategoryTreeNode> SubCategories { get; set; } = [];

    /// <summary>
    /// The descriptors that belong directly to this node.
    /// </summary>
    public List<BlockDescriptor> Descriptors { get; set; } = [];

    /// <summary>
    /// The category name represented by the node.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this node is the root of the tree.
    /// </summary>
    public bool IsRoot => Parent is null;

    /// <summary>
    /// Gets the category metadata represented by this node.
    /// </summary>
    public BlockCategory Category
    {
        get
        {
            if (Descriptors.Count > 0)
            {
                return Descriptors.First().Category;
            }

            var category = SubCategories.First().Category;
            category.Name = Name;
            return category;
        }
    }
}
