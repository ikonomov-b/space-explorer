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
    public static CompositionGraph Load(DataRoot root, PackId pack, CategoryRegistries registries, CompositionGrammar grammar)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(registries);
        ArgumentNullException.ThrowIfNull(grammar);

        GraphRow row;
        using (PackageIndex index = PackageIndex.Open(root))
        {
            row = index.FindGraph(pack) ?? throw new PackageNotFoundException(pack);
        }

        CategoryRegistry registry = registries.TryFind(row.RegistryRevision)
            ?? throw new CompatibilityException(
                $"Graph {pack} was composed under category-registry revision {row.RegistryRevision}; this build supports revision(s) {string.Join(", ", registries.Revisions)}.");

        if (row.GrammarVersion != grammar.Version)
        {
            throw new CompatibilityException($"Graph {pack} was composed under grammar version {row.GrammarVersion}; this build holds version {grammar.Version}.");
        }

        PrimitiveSet source = SetLoader.Load(root, row.SourcePack, registries);

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
