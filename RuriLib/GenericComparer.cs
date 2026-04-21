using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RuriLib;

/// <summary>
/// Compares objects by their hash code values.
/// </summary>
public class GenericComparer<T> : IEqualityComparer<T>
{
    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    /// <param name="x">The first value to compare.</param>
    /// <param name="y">The second value to compare.</param>
    /// <returns><see langword="true"/> if both values are considered equal.</returns>
    public bool Equals(T? x, T? y)
    {
        if (x == null && y == null)
        {
            return true;
        }

        if (x == null || y == null)
        {
            return false;
        }
            
        return x.GetHashCode() == y.GetHashCode();
    }

    /// <summary>
    /// Returns the hash code for the specified object.
    /// </summary>
    /// <param name="obj">The value whose hash code should be returned.</param>
    /// <returns>The hash code for <paramref name="obj"/>.</returns>
    public int GetHashCode([DisallowNull] T obj)
        => obj.GetHashCode();
}
