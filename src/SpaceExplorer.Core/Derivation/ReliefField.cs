using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// The ground shape one planet instance owns, as the few parameters that define it rather than as the
/// samples it would produce
/// ([decision 0063](../../../docs/decisions/0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md)
/// clause 7). A stored grid at the 2 m cells rung 2 asks for costs 1.57 TiB on a body of the minimum
/// landable radius, so relief is a recipe evaluated on demand and nothing of it is stored.
/// </summary>
/// <remarks>
/// The composition seed and the instance path are what make this the field of a planet <em>instance</em>
/// rather than of a planet definition: a definition's parameters are drawn once per set, so without them
/// two instances of one definition would carry the same landscape and the `basic` vocabulary's bodies
/// would give one ground each across every destination. Both are already covered by the graph record's
/// own hash, so this pins nothing new, and it takes no draw of its own — the parent's path is inherited
/// and the parent's stream is not ([decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)
/// clause 14).
/// </remarks>
/// <param name="ReferenceRadiusUnits">The body's reference radius in 1/256 m, as `derive-radius/1` gives it.</param>
/// <param name="AmplitudeMetres">The greatest height the field reaches from the tangent plane, in metres.</param>
/// <param name="RoughnessFraction">How much of each octave's amplitude the next one keeps, out of 65,535.</param>
/// <param name="WavelengthMetres">The coarsest feature's wavelength in metres; each octave halves it.</param>
/// <param name="CompositionSeed">The seed of the graph specification this planet was composed under.</param>
/// <param name="InstancePath">The planet node's instance path within that graph.</param>
public sealed record ReliefField(
    long ReferenceRadiusUnits,
    long AmplitudeMetres,
    long RoughnessFraction,
    long WavelengthMetres,
    ulong CompositionSeed,
    string InstancePath)
{
    /// <summary>The fraction <see cref="RoughnessFraction"/> is out of, which is the range the registry gives it.</summary>
    public const long RoughnessUnit = 65_535;

    /// <summary>The finest wavelength any field reaches, in metres: twice decision 0010's 2 m cell, which is the finest a 2 m lattice carries.</summary>
    public const long FinestWavelengthMetres = 4;

    /// <summary>The unit heights are given in, as int16 samples: 1/16 m.</summary>
    public const long HeightUnit = 16;

    /// <summary>
    /// The field's identity, and the only seed <see cref="TerrainHeightfield"/> takes: two equal fields
    /// are one landscape on any machine, and two different ones are two.
    /// </summary>
    public ContentHash Hash
    {
        get
        {
            var writer = new CanonicalWriter("relief-field/1");
            writer.WriteVarInt(ReferenceRadiusUnits);
            writer.WriteVarInt(AmplitudeMetres);
            writer.WriteVarInt(RoughnessFraction);
            writer.WriteVarInt(WavelengthMetres);
            writer.WriteUInt64(CompositionSeed);
            writer.WritePath(InstancePath);
            return writer.ToContentHash();
        }
    }

    /// <summary>
    /// How many octaves this field's wavelength gives: derived and never chosen, so the finest octave is
    /// always <see cref="FinestWavelengthMetres"/> whatever the coarsest is.
    /// </summary>
    public int Octaves
    {
        get
        {
            int octaves = 1;
            for (long wavelength = WavelengthMetres; wavelength > FinestWavelengthMetres; wavelength /= 2)
            {
                octaves++;
            }

            return octaves;
        }
    }
}
