namespace SpaceExplorer.Core.Registry;

/// <summary>
/// A loaded or freshly generated set: its manifest and the definitions the manifest lists, checked
/// against each other and against the dependency rule that every reference names an earlier definition
/// of the same pack with the listed hash (decisions 0031, 0033).
/// </summary>
public sealed class PrimitiveSet
{
    private readonly PrimitiveDefinition[] _definitions;

    private PrimitiveSet(SetManifest manifest, PrimitiveDefinition[] definitions)
    {
        Manifest = manifest;
        _definitions = definitions;
    }

    public SetManifest Manifest { get; }

    /// <summary>The definitions in local ID order; entry <c>i</c> has local ID <c>i + 1</c>.</summary>
    public IReadOnlyList<PrimitiveDefinition> Definitions => _definitions;

    /// <summary>Checks <paramref name="definitions"/> against <paramref name="manifest"/> and each other.</summary>
    /// <exception cref="ArgumentException">A definition is missing, misnumbered, of the wrong category or hash, or references a later or foreign definition.</exception>
    public static PrimitiveSet Create(SetManifest manifest, IReadOnlyList<PrimitiveDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Count != manifest.Definitions.Count)
        {
            throw new ArgumentException($"The manifest lists {manifest.Definitions.Count} definitions; received {definitions.Count}.", nameof(definitions));
        }

        var hashes = new HashSet<Shared.ContentHash>();
        for (int index = 0; index < definitions.Count; index++)
        {
            PrimitiveDefinition definition = definitions[index];
            ManifestEntry entry = manifest.Definitions[index];

            if (!hashes.Add(definition.Hash))
            {
                throw new ArgumentException($"Definition {entry.LocalId} repeats the content of an earlier definition; a set holds each content once.", nameof(definitions));
            }

            if (definition.Id.Pack != manifest.Pack || definition.Id.LocalId != entry.LocalId)
            {
                throw new ArgumentException($"Definition at position {index} has identity {definition.Id}; the manifest expects local ID {entry.LocalId} of pack {manifest.Pack}.", nameof(definitions));
            }

            if (definition.Category != entry.Category || definition.Hash != entry.Hash)
            {
                throw new ArgumentException($"Definition {entry.LocalId} does not match the manifest's category or hash.", nameof(definitions));
            }

            foreach (PrimitiveRevisionRef dependency in definition.Dependencies)
            {
                if (dependency.Id.Pack != manifest.Pack || dependency.Id.LocalId >= definition.Id.LocalId)
                {
                    throw new ArgumentException($"Definition {entry.LocalId} references {dependency.Id}, which is not an earlier definition of its own pack.", nameof(definitions));
                }

                if (definitions[(int)dependency.Id.LocalId - 1].Hash != dependency.Hash)
                {
                    throw new ArgumentException($"Definition {entry.LocalId} references {dependency.Id} with a hash that is not that definition's hash (decision 0033).", nameof(definitions));
                }
            }
        }

        return new PrimitiveSet(manifest, [.. definitions]);
    }

    /// <summary>Finds a definition by identity, or null when the identity is of another pack or unlisted.</summary>
    public PrimitiveDefinition? TryFind(PrimitiveId id) =>
        id.Pack == Manifest.Pack && id.LocalId >= 1 && id.LocalId <= (uint)_definitions.Length ? _definitions[(int)id.LocalId - 1] : null;

    /// <summary>Whether <paramref name="reference"/> names a definition of this set exactly.</summary>
    public bool Contains(PrimitiveRevisionRef reference) => TryFind(reference.Id) is { } definition && definition.Hash == reference.Hash;
}
