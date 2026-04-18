namespace RuriLib.Models.Blocks;

/// <summary>
/// A descriptor for a block generated from an exposed C# method.
/// </summary>
public class AutoBlockDescriptor : BlockDescriptor
{
    /// <summary>
    /// Whether the underlying method is asynchronous.
    /// </summary>
    public bool Async { get; set; }
}
