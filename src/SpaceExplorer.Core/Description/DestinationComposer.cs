using System.Globalization;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Description;

/// <summary>
/// One destination: the two levers that asked for it, the attempt that produced it, and the graph and
/// description that came out (decision 0043).
/// </summary>
public sealed record Destination(
    DistanceTier Tier,
    ulong Seed,
    uint Attempt,
    ulong CompositionSeed,
    CompositionGraph Graph,
    SystemDescription Description);

/// <summary>
/// Composes the destination the two destination levers ask for: a distance tier and a seed
/// ([decision 0043](../../../docs/decisions/0043-destination-controls-distance-tier-and-seed-lever.md)).
/// The tier's rules are not something a grammar can guarantee, so the composer draws a system, checks it
/// against them, and draws again on the next derived seed until one satisfies them or the attempts run
/// out, which is the bounded retry and explicit content-capacity error the development plan's Destination
/// validity check requires. Every attempt's seed is derived from the lever seed by the frozen stream
/// derivation, so one pair of lever values names one destination on any machine.
/// </summary>
public static class DestinationComposer
{
    /// <summary>How many systems one pair of lever values may draw before the destination is rejected.</summary>
    public const uint MaxAttempts = 64;

    /// <summary>Composes the destination <paramref name="tier"/> and <paramref name="seed"/> name.</summary>
    /// <exception cref="GenerationException">No attempt satisfied the tier's rules within <see cref="MaxAttempts"/>.</exception>
    public static Destination Compose(DistanceTier tier, ulong seed, PrimitiveSet set, CompositionGrammar grammar, CategoryRegistry registry, SuitProfile suit)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(suit);

        IReadOnlyList<string> lastFailures = [];
        for (uint attempt = 0; attempt < MaxAttempts; attempt++)
        {
            ulong composition = CompositionSeed(tier, seed, attempt);
            GraphSpecification specification = GraphSpecification.Create(
                registry.Revision, registry.Hash, GeneratorVersion.Current, grammar.Version, grammar.Hash, composition, set.Manifest.Pack, set.Manifest.Hash, CompositionDomain.SolarSystem);

            var description = SystemDescription.Derive(CompositionGenerator.Generate(specification, set, grammar, registry), registry, suit);
            lastFailures = TierRules.Check(description, tier);
            if (lastFailures.Count == 0)
            {
                return new Destination(tier, seed, attempt, composition, description.Graph, description);
            }
        }

        throw new GenerationException(
            $"No system of the {TierRules.Label(tier)} tier came out of seed {seed.ToString(CultureInfo.InvariantCulture)} within {MaxAttempts} attempts; the last failed because {string.Join("; ", lastFailures)}.");
    }

    /// <summary>
    /// The seed attempt <paramref name="attempt"/> composes with: two draws of the stream the frozen
    /// derivation gives that attempt's path, so the sequence follows from the lever values alone and is
    /// the same on every machine (decisions 0008, 0021).
    /// </summary>
    public static ulong CompositionSeed(DistanceTier tier, ulong seed, uint attempt)
    {
        Pcg32 stream = RandomStream.Derive(seed, $"destination/{TierRules.Label(tier)}/attempt/{attempt.ToString(CultureInfo.InvariantCulture)}");
        return ((ulong)stream.NextUInt32() << 32) | stream.NextUInt32();
    }
}
