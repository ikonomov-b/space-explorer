namespace SpaceExplorer.Core.Registry;

/// <summary>
/// An immutable, package-local table resolving a <see cref="Handle"/> to the full
/// <see cref="PrimitiveId"/> it names, for composition nodes that reference primitives across mixed
/// packs and revisions ("store an immutable reference table mapping package-local uint handles to full
/// exact identities", technical design, primitive sets and compact references). Entry order is the
/// handle: entry <c>i</c> resolves from <c>new Handle((uint)i)</c> (decision 0029).
/// </summary>
public sealed class ReferenceTable
{
    private readonly PrimitiveId[] _entries;

    private ReferenceTable(PrimitiveId[] entries) => _entries = entries;

    /// <summary>How many entries the table holds.</summary>
    public int Count => _entries.Length;

    /// <summary>Builds a table from <paramref name="entries"/>, in the order their handles will resolve.</summary>
    /// <exception cref="ArgumentException">An entry is unset, or the same identity appears more than once.</exception>
    public static ReferenceTable Create(IReadOnlyList<PrimitiveId> entries)
    {
        var seen = new HashSet<PrimitiveId>();
        var copy = new PrimitiveId[entries.Count];

        for (int index = 0; index < entries.Count; index++)
        {
            PrimitiveId entry = entries[index];
            if (entry.IsUnset)
            {
                throw new ArgumentException($"Reference table entry {index} is unset.", nameof(entries));
            }

            if (!seen.Add(entry))
            {
                throw new ArgumentException($"Reference table entry {index} repeats identity {entry}.", nameof(entries));
            }

            copy[index] = entry;
        }

        return new ReferenceTable(copy);
    }

    /// <summary>Resolves <paramref name="handle"/> to the full identity it names.</summary>
    /// <exception cref="ArgumentOutOfRangeException">This table has no entry at <paramref name="handle"/>.</exception>
    public PrimitiveId Resolve(Handle handle)
    {
        if (handle.Index >= (uint)_entries.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(handle), handle, $"This table has {_entries.Length} entries.");
        }

        return _entries[(int)handle.Index];
    }
}
