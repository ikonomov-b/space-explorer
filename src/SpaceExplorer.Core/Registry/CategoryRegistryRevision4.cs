using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 4: revision 3 with the three things a stored quantity lacked — a unit, a
/// default, and the decoder's own bounds — plus a full descriptor on every derived parameter and the mass
/// decision 0037 already gives a barycentre
/// ([decision 0060](../../../docs/decisions/0060-registry-revision-4-units-defaults-and-validation-limits.md),
/// clearing [review finding 50](../../../docs/review.md#50-a-stored-quantity-carries-no-unit-no-default-and-no-validation-limit)).
/// </summary>
/// <remarks>
/// A unit fixes the reading of a value by one equation, <c>quantity = stored ÷ 2^fractionBits ×
/// 10^exponent</c>, so what every number in a stored solar system means is now in the record rather than
/// in this assembly. A default gives a template's silence a stated meaning: from this revision a template
/// that ranges nothing for a parameter pins it to the default, where a command-line tool used to widen it
/// to the whole range. Revisions 1 to 3 keep their bytes and their hashes, and every pack published under
/// them keeps loading.
/// </remarks>
public static class CategoryRegistryRevision4
{
    /// <summary>The aggregation rule that gives a barycentre the mass of its children (decisions 0031, 0037, 0060).</summary>
    public const string MassAggregationRule = "aggregate-sum/1";

    /// <summary>Earth's albedo of 0.306 as a fraction of 65,535, which is what an unranged albedo becomes.</summary>
    private const long EarthAlbedo = 20_054;

    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(4,
    [
        new CategoryDefinition(Star, "star", [CompositionDomain.SolarSystem],
        [
            Quantity("mass", "kg", 20, 1_000_000_000L, 400_000_000_000L, SolarMass),
            Quantity("radius", "m", 0, 10_000_000L << PlanetFractionBits, 2_000_000_000L << PlanetFractionBits, 695_700_000L << PlanetFractionBits, PlanetFractionBits),
            Quantity("effective-temperature", "K", 0, 2_000L, 50_000L, 5_772L),
        ],
        [new ConnectorKind("orbit", TransformKind.OrbitalElements, 0, 16, [Planet, Barycentre])],
        StoragePolicies.Materialize, 0, [],
        [
            Derived("luminosity", DerivationRules.LuminosityRevision, Quantity("luminosity", "W", 20, 0L, long.MaxValue, 0L)),
            Derived("spectral-class", DerivationRules.SpectralClassRevision, Choice("spectral-class", 0, "O", "B", "A", "F", "G", "K", "M")),
        ]),

        new CategoryDefinition(Barycentre, "barycentre", [CompositionDomain.SolarSystem],
        [],
        [new ConnectorKind("orbit", TransformKind.OrbitalElements, 2, 8, [Star, Planet, Barycentre])],
        StoragePolicies.Materialize, 0, [],
        // Decision 0037 says a barycentre's mass is the sum of its children's; nothing declared it until
        // now, so a published pair could contradict its own masses (review finding 53).
        [Derived("mass", MassAggregationRule, Quantity("mass", "kg", 20, 0L, long.MaxValue, 0L))]),

        new CategoryDefinition(Planet, "planet", [CompositionDomain.SolarSystem, CompositionDomain.Planet],
        [
            Quantity("mass", "kg", 20, 1L, 100_000_000L, EarthMass),
            Quantity("density", "kg/m3", 0, CategoryRegistryRevision2.MinimumDensity, CategoryRegistryRevision2.MaximumDensity, 5_514L),
            Fraction("albedo", EarthAlbedo),
            Turn("pole-right-ascension"),
            Turn("pole-declination", -(1 << 30), 1 << 30),
            Quantity("sidereal-period", "tick", 0, 20L * 3_600L, 20L * 86_400L * 1_000L, 20L * 86_164L),
            Turn("prime-meridian-phase"),
            Choice("body-type", 0, "rocky", "icy", "ocean", "gas-giant"),
            Reference(CategoryRegistryRevision3.SurfaceParameter, SurfaceMaterial),
        ],
        [
            new ConnectorKind("atmosphere", TransformKind.Rigid, 1, 1, [Atmosphere]),
            new ConnectorKind("orbit", TransformKind.OrbitalElements, 0, 8, [Planet]),
        ],
        StoragePolicies.Materialize, 0, [],
        [Derived("radius", DerivationRules.RadiusRevision, Quantity("radius", "m", 0, 0L, long.MaxValue, 0L, PlanetFractionBits))]),

        new CategoryDefinition(Atmosphere, "atmosphere", [CompositionDomain.Planet],
        [
            Choice("model", 0, "airless", "thin", "dense"),
            Quantity("surface-pressure", "Pa", 0, 0, 100_000_000L, 0L),
            Choice("composition", 0, "none", "nitrogen-oxygen", "carbon-dioxide", "hydrogen-helium", "methane"),
            Colour("scattering-tint", 0, 0, 0, 0),
        ],
        [],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(SurfaceMaterial, "surface-material", [CompositionDomain.Surface, CompositionDomain.Artifact],
        [
            Choice("shader", 0, "standard-opaque", "metallic", "emissive"),
            Colour("base-colour", 128, 128, 128, 255),
            Fraction("roughness", 32_768),
            Fraction("metallic", 0),
            Reference("texture", TextureRecipe),
        ],
        [],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(TextureRecipe, "texture-recipe", [CompositionDomain.Surface, CompositionDomain.Artifact],
        [
            Choice("pattern", 0, "flat", "noise", "stripes", "cells", "speckle"),
            Quantity("scale", "m", 0, 65L, 4L << ArtifactFractionBits, 1L << ArtifactFractionBits, ArtifactFractionBits),
            Colour("colour-a", 128, 128, 128, 255),
            Colour("colour-b", 128, 128, 128, 255),
            Fraction("contrast", 0),
        ],
        [],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(GeometryRecipe, "geometry-recipe", [CompositionDomain.Artifact],
        [
            Choice("shape", 0, "sphere", "box", "cylinder", "capsule", "cone", "torus"),
            Size("size", 655L, 4L << ArtifactFractionBits, 1L << (ArtifactFractionBits - 2)),
            Count("segments", 3, 64, 16),
            Quantity("displacement", "m", 0, 0, 6_553L, 0L, ArtifactFractionBits),
        ],
        [],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(ArtifactPart, "artifact-part", [CompositionDomain.Artifact],
        [
            Reference("geometry", GeometryRecipe),
            Reference("material", SurfaceMaterial),
            // Grams, decided by decision 0060 because no document held it: a part spans one gram to two
            // hundred kilograms, so an assembled artifact stays something a person recovers.
            Quantity("mass", "kg", -3, 1, 200_000L, 1_000L),
        ],
        [new ConnectorKind("attach", TransformKind.Rigid, 0, 8, [ArtifactPart])],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(Artifact, "artifact", [CompositionDomain.Artifact],
        [
            Choice("family", 0, "mineral", "biological", "relic"),
            Reference("root", ArtifactPart),
            Count("tier", 1, 2, 1),
        ],
        [],
        StoragePolicies.Materialize, 0, []),
    ]);

    /// <summary>An integer quantity in a named unit, with the value a template's silence gives it.</summary>
    private static ParameterDescriptor Quantity(string label, string symbol, int exponent, long min, long max, long standard, byte fractionBits = 0) =>
        new(label, ParameterKind.Integer, fractionBits, min, max, [], 0, ParameterUnit.Of(symbol, exponent), ParameterValue.Integer(standard));

    /// <summary>A fraction of 65,535, whose denominator the record holds as its own maximum.</summary>
    private static ParameterDescriptor Fraction(string label, long standard) =>
        new(label, ParameterKind.Integer, 0, 0, 65_535, [], 0, ParameterUnit.Fraction, ParameterValue.Integer(standard));

    /// <summary>A count or an index, which is not a quantity.</summary>
    private static ParameterDescriptor Count(string label, long min, long max, long standard) =>
        new(label, ParameterKind.Integer, 0, min, max, [], 0, ParameterUnit.None, ParameterValue.Integer(standard));

    private static ParameterDescriptor Turn(string label, int min = int.MinValue, int max = int.MaxValue) =>
        new(label, ParameterKind.BinaryTurn, 0, min, max, [], 0, ParameterUnit.Of("turn"), ParameterValue.BinaryTurn(0));

    private static ParameterDescriptor Choice(string label, uint standard, params string[] labels) =>
        new(label, ParameterKind.Enum, 0, 0, 0, labels, 0, ParameterUnit.None, ParameterValue.Enum(standard));

    private static ParameterDescriptor Colour(string label, byte red, byte green, byte blue, byte alpha) =>
        new(label, ParameterKind.Colour, 0, 0, 0, [], 0, ParameterUnit.None, ParameterValue.Colour(red, green, blue, alpha));

    private static ParameterDescriptor Size(string label, long min, long max, long standard) =>
        new(label, ParameterKind.Vector3, ArtifactFractionBits, min, max, [], 0, ParameterUnit.Of("m"), ParameterValue.Vector3(standard, standard, standard));

    /// <summary>A reference, which carries a unit like everything else and a default like nothing else.</summary>
    private static ParameterDescriptor Reference(string label, uint category) =>
        new(label, ParameterKind.PrimitiveRef, 0, 0, 0, [], category, ParameterUnit.None);

    /// <summary>
    /// A computed quantity: the descriptor a stored one carries, minus the default nothing supplies, and
    /// the rule that produces it.
    /// </summary>
    private static DerivedParameter Derived(string label, string rule, ParameterDescriptor descriptor) =>
        new(label, rule, new ParameterDescriptor(
            descriptor.Label,
            descriptor.Kind,
            descriptor.FractionBits,
            descriptor.Min,
            descriptor.Max,
            descriptor.EnumLabels,
            descriptor.RefCategory,
            descriptor.Unit));
}
