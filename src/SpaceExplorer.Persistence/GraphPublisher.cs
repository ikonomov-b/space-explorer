using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>What one graph publication did.</summary>
public sealed record GraphPublishResult(PackId Pack, ContentHash GraphHash, bool AlreadyPublished, int RecordsWritten);

/// <summary>
/// Publishes a composition graph through the protocol sets use: the set it composes from must already be
/// published with the manifest the graph pins, the record is written and verified first, and the index
/// row and its reference-index edges are committed last (decisions 0018, 0031, 0040). A differing graph
/// under an existing graph pack identifier is a reproduction failure, exactly as a differing manifest is
/// (decision 0039).
/// </summary>
public static class GraphPublisher
{
    public static GraphPublishResult Publish(DataRoot root, CompositionGraph graph)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(graph);

        GraphSpecification specification = graph.Specification;
        using PackageIndex index = PackageIndex.Open(root);

        ContentHash source = index.FindManifest(specification.SourcePack);
        if (source.IsUnset)
        {
            throw new PackageNotFoundException(specification.SourcePack);
        }

        if (source != specification.SourceManifestHash)
        {
            throw new CompatibilityException(
                $"The graph composes pack {specification.SourcePack} at manifest {specification.SourceManifestHash}; this data root holds manifest {source} for it.");
        }

        if (index.FindGraph(graph.Pack) is { } existing)
        {
            if (existing.GraphHash == graph.Hash)
            {
                return new GraphPublishResult(graph.Pack, graph.Hash, AlreadyPublished: true, RecordsWritten: 0);
            }

            throw new ReproductionFailureException(graph.Pack, existing.GraphHash, graph.Hash);
        }

        RecordFile.PrepareDirectory(root.PackDirectory(graph.Pack));
        int written = RecordFile.Write(root.RecordPath(graph.Pack, graph.Hash), graph.Bytes, graph.Hash);

        index.RegisterGraph(graph);
        return new GraphPublishResult(graph.Pack, graph.Hash, AlreadyPublished: false, written);
    }
}
