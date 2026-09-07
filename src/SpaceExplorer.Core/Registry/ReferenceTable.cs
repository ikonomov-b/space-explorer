namespace SpaceExplorer.Core.Registry;

/// <summary>
/// An immutable, package-local table resolving a <see cref="Handle"/> to the exact
/// <see cref="PrimitiveRevisionRef"/> it names, for composition nodes that reference primitives across
/// mixed packs (technical design, primitive sets and compact references; decision 0031). Entry order is
/// the handle: entry <c>i</c> resolves from <c>new Handle((uint)i)</c> (decision 0029). One
/// <see cref="PrimitiveId"/> appears at most once, because a generated ID has one hash (decision 0033).
/// </summary>
public sealed class ReferenceTable
{
    private readonly PrimitiveRevisionRef[] _entries;

    private ReferenceTable(PrimitiveRevisionRef[] entries) => _entries = entries;

    /// <summary>How many entries the table holds.</summary>
    public int Count => _entries.Length;

    /// <summary>Builds a table from <paramref name="entries"/>, in the order their handles will resolve.</summary>
    /// <exception cref="ArgumentException">An entry is unset, or the same <see cref="PrimitiveId"/> appears more than once.</exception>
    public static ReferenceTable Create(IReadOnlyList<PrimitiveRevisionRef> entries)
    {
        var seen = new HashSet<PrimitiveId>();
        var copy = new PrimitiveRevisionRef[entries.Count];

        for (int index = 0; index < entries.Count; index++)
        {
            PrimitiveRevisionRef entry = entries[index];
            if (entry.IsUnset)
            {
                throw new ArgumentException($"Reference table entry {index} is unset.", nameof(entries));
            }

            if (!seen.Add(entry.Id))
            {
                throw new ArgumentException($"Reference table entry {index} repeats identity {entry.Id}.", nameof(entries));
            }

            copy[index] = entry;
        }

        return new ReferenceTable(copy);
    }

    /// <summary>Resolves <paramref name="handle"/> to the exact reference it names.</summary>
    /// <exception cref="ArgumentOutOfRangeException">This table has no entry at <paramref name="handle"/>.</exception>
    public PrimitiveRevisionRef Resolve(Handle handle)
    {
        if (handle.Index >= (uint)_entries.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(handle), handle, $"This table has {_entries.Length} entries.");
        }

        return _entries[(int)handle.Index];
    }
}
