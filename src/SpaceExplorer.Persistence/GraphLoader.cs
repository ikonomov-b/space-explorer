using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>A published graph as the index lists it, without reading the record.</summary>
public sealed record GraphEntry(PackId Pack, ContentHash GraphHash, PackId SourcePack, uint RegistryRevision, uint GrammarVersion, int NodeCount, CompositionDomain Domain);

/// <summary>
/// Loads a published composition graph without composing anything: the index names the record and the set
/// it was composed from, that set is reloaded and verified first, the record is verified against its hash
/// before it is decoded, and the graph's pinned registry revision and grammar must be ones this build
/// holds (decisions 0031, 0035).
/// </summary>
public static class GraphLoader
{
    public static CompositionGraph Load(DataRoot root, PackId pack, CategoryRegistries registries, CompositionGrammar grammar) =>
        Read(root, pack, registries, grammar, source: null);

    /// <summary>
    /// The same load, over a set the caller already holds, which is verified to be the set the graph pins
    /// rather than trusted: the pack identifier and the manifest hash must both be the ones the index
    /// names, so nothing is accepted here that the reloading form would have refused.
    /// </summary>
    /// <remarks>
    /// It exists because every stored-destination load reads the set twice. The destination specification
    /// needs the manifest hash to derive the pack identifier it looks up, so the caller loads the set
    /// first; this overload lets it hand that set over instead of having 118 files opened and hashed a
    /// second time. A view that only reads makes this the one path a world is read through
    /// ([decision 0066](../../../docs/decisions/0066-generation-writes-to-the-database-and-the-view-only-reads.md)),
    /// which is what turns a tens-of-milliseconds waste into one worth removing.
    /// </remarks>
    /// <exception cref="CompatibilityException">The set offered is not the set the graph was composed from.</exception>
    public static CompositionGraph Load(DataRoot root, PackId pack, CategoryRegistries registries, CompositionGrammar grammar, PrimitiveSet source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Read(root, pack, registries, grammar, source);
    }

    private static CompositionGraph Read(DataRoot root, PackId pack, CategoryRegistries registries, CompositionGrammar grammar, PrimitiveSet? source)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(registries);
        ArgumentNullException.ThrowIfNull(grammar);

        GraphRow row;
        ContentHash pinnedManifest;
        using (PackageIndex index = PackageIndex.Open(root))
        {
            row = index.FindGraph(pack) ?? throw new PackageNotFoundException(pack);

            // Read in the same open as the row, so offering a set costs no extra index connection: the
            // whole point of the overload is to open fewer things, not to move where they are opened.
            pinnedManifest = source is null ? default : index.FindManifest(row.SourcePack);
        }

        CategoryRegistry registry = registries.TryFind(row.RegistryRevision)
            ?? throw new CompatibilityException(
                $"Graph {pack} was composed under category-registry revision {row.RegistryRevision}; this build supports revision(s) {string.Join(", ", registries.Revisions)}.");

        if (row.GrammarVersion != grammar.Version)
        {
            throw new CompatibilityException($"Graph {pack} was composed under grammar version {row.GrammarVersion}; this build holds version {grammar.Version}.");
        }

        if (source is null)
        {
            source = SetLoader.Load(root, row.SourcePack, registries);
        }
        else if (source.Manifest.Pack != row.SourcePack || source.Manifest.Hash != pinnedManifest)
        {
            throw new CompatibilityException(
                $"Graph {pack} was composed from pack {row.SourcePack}; the set offered is {source.Manifest.Pack} at manifest {source.Manifest.Hash}.");
        }

        CompositionGraph graph;
        try
        {
            graph = CompositionGraph.Decode(SetLoader.ReadVerified(root.RecordPath(pack, row.GraphHash), row.GraphHash), source, registry);
        }
        catch (FormatException exception)
        {
            throw new PackageIntegrityException($"The record of graph {pack} is not a valid graph: {exception.Message}", exception);
        }

        if (graph.Pack != pack)
        {
            throw new PackageIntegrityException($"The graph indexed under {pack} derives pack identifier {graph.Pack} from its own specification.");
        }

        if (graph.Specification.GrammarHash != grammar.Hash)
        {
            throw new CompatibilityException(
                $"Graph {pack} pins grammar version {graph.Specification.GrammarVersion} with hash {graph.Specification.GrammarHash}; this build holds {grammar.Hash} for that version.");
        }

        return graph;
    }

    /// <summary>Every published graph with what the index knows of it.</summary>
    public static IReadOnlyList<GraphEntry> List(DataRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        using PackageIndex index = PackageIndex.Open(root);
        return [.. index.ListGraphs().Select(row => new GraphEntry(row.Pack, row.GraphHash, row.SourcePack, row.RegistryRevision, row.GrammarVersion, row.NodeCount, row.Domain))];
    }
}
