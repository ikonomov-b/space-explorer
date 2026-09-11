using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>What grounding one destination did.</summary>
public sealed record GroundPublishResult(int Regions, int Generated, long BytesWritten);

/// <summary>
/// Produces and publishes the ground of every region a composed graph carries, at the moment the graph is
/// published ([decision 0067](../../../docs/decisions/0067-a-destinations-ground-is-generated-when-it-is-composed.md)).
/// </summary>
/// <remarks>
/// A destination on disk is then a destination whose ground is on disk, so exploring one reads records
/// written before exploring began and the view's claim to call no generation rule is true of the ordinary
/// path rather than of the interface alone. It is affordable because it was measured: a destination
/// carries a mean of 4.6 regions at 0.38 to 0.76 MiB each, a second or two of generation inside a travel
/// wait [decision 0004](../../../docs/decisions/0004-preparation-is-real-generation-time.md) already makes
/// real. What would change it is a body offering many regions rather than one, which is decision 0067
/// clause 2's threshold.
/// </remarks>
public static class GroundPublisher
{
    /// <summary>Grounds every region of <paramref name="graph"/>, skipping those already stored.</summary>
    public static GroundPublishResult Publish(DataRoot root, CompositionGraph graph, CategoryRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(registry);

        int regions = 0;
        int generated = 0;
        long bytes = 0;

        foreach ((GraphNode body, GraphNode region) in Regions(graph.Root))
        {
            regions++;

            long extent = region.Definition.TryParameter(registry, CategoryRegistryRevision5.ExtentParameter)?.Value.AsInteger
                ?? throw new PackageIntegrityException($"Instance '{region.Path}' stores no extent, so nothing says how far its ground reaches.");

            RegionPayload payload = PayloadStore.Resolve(
                root,
                graph.Pack,
                region.Path,
                ReliefFieldOf(body, registry, graph.Specification.Seed),
                (int)(region.Transform?.Component("latitude") ?? 0),
                (int)(region.Transform?.Component("longitude") ?? 0),
                (int)(region.Transform?.Component("heading") ?? 0),
                extent,
                out bool wasGenerated);

            if (wasGenerated)
            {
                generated++;
                bytes += payload.Bytes.Length;
            }
        }

        return new GroundPublishResult(regions, generated, bytes);
    }

    /// <summary>
    /// The relief field a planet instance owns: its stored parameters, with the composition seed and the
    /// instance path that make it this instance's rather than its definition's (decision 0063 clause 7).
    /// </summary>
    public static ReliefField ReliefFieldOf(GraphNode body, CategoryRegistry registry, ulong compositionSeed)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(registry);

        return new ReliefField(
            DerivationRules.Radius(Parameter(body, registry, "mass"), Parameter(body, registry, "density")),
            Parameter(body, registry, CategoryRegistryRevision6.AmplitudeParameter),
            Parameter(body, registry, CategoryRegistryRevision6.RoughnessParameter),
            Parameter(body, registry, CategoryRegistryRevision6.WavelengthParameter),
            compositionSeed,
            body.Path,
            body.Definition.TryParameter(registry, CategoryRegistryRevision7.RidgingParameter)?.Value.AsInteger);
    }

    /// <summary>Every region of the graph, with the body that carries it.</summary>
    private static IEnumerable<(GraphNode Body, GraphNode Region)> Regions(GraphNode node) =>
        node.Children
            .SelectMany(connector => connector)
            .SelectMany(child => child.Definition.Category == CategoryRegistryRevision5.Region
                ? [(node, child)]
                : Regions(child));

    private static long Parameter(GraphNode node, CategoryRegistry registry, string label) =>
        node.Definition.TryParameter(registry, label)?.Value.AsInteger
        ?? throw new PackageIntegrityException($"Instance '{node.Path}' stores no '{label}', which its registry revision gives every planet.");
}
