using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuriLib.Models.Debugger;

/// <summary>
/// A typed variable snapshot captured while a config is paused in the debugger.
/// </summary>
public sealed class DebuggerVariableSnapshotEntry
{
    /// <summary>
    /// The variable name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The variable type.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The variable value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Creates a new snapshot entry.
    /// </summary>
    public DebuggerVariableSnapshotEntry(string name, Type type, object? value)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("The variable name cannot be null or empty", nameof(name))
            : name;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Value = value;
    }

    /// <summary>
    /// Creates a new snapshot entry by inferring the type from the generic argument.
    /// </summary>
    public static DebuggerVariableSnapshotEntry Create<T>(string name, T value)
        => new(name, typeof(T), value);
}

/// <summary>
/// Stores and retrieves debugger variable snapshots from a bot context.
/// </summary>
public static class DebuggerVariableSnapshot
{
    private const string ObjectKey = "debuggerVariableSnapshot";

    /// <summary>
    /// Stores the latest variable snapshot for a bot.
    /// </summary>
    public static void Store(BotData data, IEnumerable<DebuggerVariableSnapshotEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(entries);

        data.SetObject(ObjectKey, entries.ToList(), disposeExisting: false);
    }

    /// <summary>
    /// Gets the latest variable snapshot for a bot.
    /// </summary>
    public static IReadOnlyList<DebuggerVariableSnapshotEntry> Get(BotData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return data.TryGetObject<List<DebuggerVariableSnapshotEntry>>(ObjectKey) ?? [];
    }
}
