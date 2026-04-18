namespace RuriLib.Models.Blocks;

/// <summary>
/// Represents the category metadata attached to a block.
/// </summary>
public struct BlockCategory
{
    /// <summary>
    /// The display name of the category.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The hierarchical category path.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// The namespace that identifies the category.
    /// </summary>
    public string Namespace { get; set; }

    /// <summary>
    /// The category description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The background color shown in the UI.
    /// </summary>
    public string BackgroundColor { get; set; }

    /// <summary>
    /// The foreground color shown in the UI.
    /// </summary>
    public string ForegroundColor { get; set; }
}
