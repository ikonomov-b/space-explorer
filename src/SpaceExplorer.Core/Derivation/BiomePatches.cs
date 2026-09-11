using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// One patch of a region's ground: which biome of the planet's palette claims it, where its centre lies in
/// the region's own frame, and how far its claim reaches
/// ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
/// clause 5).
/// </summary>
/// <param name="Ordinal">An index into the planet's `biomes` palette.</param>
/// <param name="CentreX">The centre's region-local x in 1/256 m.</param>
/// <param name="CentreZ">The centre's region-local z in 1/256 m.</param>
/// <param name="ReachMetres">How far the claim carries, in metres.</param>
public readonly record struct BiomePatch(int Ordinal, long CentreX, long CentreZ, long ReachMetres);

/// <summary>
/// `biome-patches/1`: the derived set a region owns, drawn from the region's own stream
/// ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
/// clause 5). It is the first use in this project of the derived instances
/// [decision 0038](../../../docs/decisions/0038-derived-instances.md) specifies.
/// </summary>
/// <remarks>
/// Both constants are derived rather than chosen. The count gives a mean patch spacing of 256 m at every
/// extent, which is 128 cells at decision 0010's 2 m cell, so the smallest patch still takes a walker about
/// two minutes to cross at decision 0017's 2 m/s. The reach spans half that spacing to twice it, so patches
/// neither tile without overlapping nor drown one another.
///
/// Every draw is the bounded sampling decision 0019 froze, so this rule introduces no construction of its
/// own and owes no anchor beyond the ones that foundation already has.
/// </remarks>
public static class BiomePatches
{
    /// <summary>The generator revision identifier this rule is known by (decision 0035).</summary>
    public const string Rule = "biome-patches/1";

    /// <summary>How many metres of extent one patch stands for, which sets both the count and the spacing.</summary>
    public const long MetresPerPatch = 256;

    /// <summary>The closest two patch centres are expected to sit, and the shortest reach.</summary>
    public const long MinimumReachMetres = 128;

    /// <summary>The longest reach, twice the mean spacing.</summary>
    public const long MaximumReachMetres = 512;

    /// <summary>How many patches a region of <paramref name="extentMetres"/> carries: 64 at the 2,048 m cap, 1 at the 256 m minimum.</summary>
    public static int CountFor(long extentMetres)
    {
        long across = extentMetres / MetresPerPatch;
        return (int)(across * across);
    }

    /// <summary>
    /// The patches of one region, in index order, drawn from the stream its instance path names.
    /// </summary>
    /// <param name="compositionSeed">The seed of the graph specification the region was composed under.</param>
    /// <param name="instancePath">The region node's instance path, which names its stream.</param>
    /// <param name="extentMetres">The region's stored extent, per axis.</param>
    /// <param name="paletteLength">How many biomes the planet's palette holds; an ordinal indexes it.</param>
    /// <exception cref="ArgumentOutOfRangeException">The extent is under one patch, or the palette is empty.</exception>
    public static BiomePatch[] Draw(ulong compositionSeed, string instancePath, long extentMetres, int paletteLength)
    {
        ArgumentNullException.ThrowIfNull(instancePath);

        int count = CountFor(extentMetres);
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(extentMetres), extentMetres, $"A region carries at least one patch, so its extent is at least {MetresPerPatch} m.");
        }

        if (paletteLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(paletteLength), paletteLength, "A patch names a biome of the planet's palette, so the palette holds at least one.");
        }

        // The region's own stream, named by its own path — the parent's path is inherited and the parent's
        // stream is not (decision 0061 clause 14) — but under this rule's label rather than at the bare
        // path. Decision 0070 clause 5 says "from the region node's own stream over the instance path",
        // and taking that literally would start where the node's own parameter draws started and hand this
        // rule the same numbers, correlating a patch's ordinal with the region's extent. A stream per
        // purpose is what decision 0031 means by one thing never consuming another's.
        Pcg32 stream = RandomStream.Derive(compositionSeed, instancePath + StreamPath.Separator + "biome-patches");

        long half = extentMetres * 256 / 2;
        var patches = new BiomePatch[count];
        for (int index = 0; index < count; index++)
        {
            patches[index] = new BiomePatch(
                (int)ParameterSampler.SampleInclusive(stream, 0, paletteLength - 1),
                ParameterSampler.SampleInclusive(stream, -half, half),
                ParameterSampler.SampleInclusive(stream, -half, half),
                ParameterSampler.SampleInclusive(stream, MinimumReachMetres, MaximumReachMetres));
        }

        return patches;
    }
}
