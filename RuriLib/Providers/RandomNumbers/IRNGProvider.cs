using System;

namespace RuriLib.Providers.RandomNumbers;

/// <summary>
/// Provides random number generator instances.
/// </summary>
public interface IRNGProvider
{
    /// <summary>
    /// Creates a new <see cref="Random"/> instance.
    /// </summary>
    Random GetNew();
}
