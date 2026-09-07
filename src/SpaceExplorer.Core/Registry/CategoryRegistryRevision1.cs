namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 1: the initial vocabulary M0a criterion 1 generates from, covering the
/// basic solar-system, planet, and artifact groups plus the material and texture primitives the owner
/// asked for (decision 0030), under the schema rules of decisions 0031 and 0034 to 0037. Every
/// category permits materialize only, so no generator revision identifiers are listed yet. Defined in
/// code and frozen by its recorded hash; a second revision or an authoring tool moves the source form
/// to <c>content/</c> (decision 0035).
/// </summary>
public static class CategoryRegistryRevision1
{
    public const uint Star = 1;
    public const uint Barycentre = 2;
    public const uint Planet = 3;
    public const uint Atmosphere = 4;
    public const uint SurfaceMaterial = 5;
    public const uint TextureRecipe = 6;
    public const uint GeometryRecipe = 7;
    public const uint ArtifactPart = 8;
    public const uint Artifact = 9;

    /// <summary>Fixed-point fraction bits for planet-fixed radial lengths: 1/256 m (decision 0036).</summary>
    public const byte PlanetFractionBits = 8;

    /// <summary>Fixed-point fraction bits for artifact-local lengths: 1/65,536 m (decision 0036).</summary>
    public const byte ArtifactFractionBits = 16;

    /// <summary>Mass unit: 10^20 kg, given as the exponent because the unit itself exceeds 64 bits. Earth is 59,720; the Sun is 19,890,000,000.</summary>
    public const int MassUnitExponentKilograms = 20;

    /// <summary>Luminosity unit: 10^20 W, given as the exponent. The Sun is 3,828,000.</summary>
    public const int LuminosityUnitExponentWatts = 20;

    /// <summary>The minimum landable reference radius, 524,288 m, in 1/256 m units (decision 0041).</summary>
    public const long MinimumLandableRadius = 524_288L << PlanetFractionBits;

    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(1,
    [
        new CategoryDefinition(Star, "star", [CompositionDomain.SolarSystem],
        [
            ParameterDescriptor.Integer("mass", 1_000_000_000L, 400_000_000_000L),
            ParameterDescriptor.Integer("radius", 10_000_000L << PlanetFractionBits, 2_000_000_000L << PlanetFractionBits, PlanetFractionBits),
            ParameterDescriptor.Integer("luminosity", 1_000L, 100_000_000_000L),
            ParameterDescriptor.Integer("effective-temperature", 2_000L, 50_000L),
            ParameterDescriptor.Choice("spectral-class", "O", "B", "A", "F", "G", "K", "M"),
        ],
        [new ConnectorKind("orbit", TransformKind.OrbitalElements, 0, 16, [Planet, Barycentre])],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(Barycentre, "barycentre", [CompositionDomain.SolarSystem],
        [],
        [new ConnectorKind("orbit", TransformKind.OrbitalElements, 2, 8, [Star, Planet, Barycentre])],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(Planet, "planet", [CompositionDomain.SolarSystem, CompositionDomain.Planet],
        [
            ParameterDescriptor.Integer("mass", 1L, 100_000_000L),
            ParameterDescriptor.Integer("radius", 100_000L << PlanetFractionBits, 100_000_000L << PlanetFractionBits, PlanetFractionBits),
            ParameterDescriptor.Integer("albedo", 0, 65_535),
            ParameterDescriptor.BinaryTurn("pole-right-ascension"),
            ParameterDescriptor.BinaryTurn("pole-declination", -(1 << 30), 1 << 30),
            ParameterDescriptor.Integer("sidereal-period", 20L * 3_600L, 20L * 86_400L * 1_000L),
            ParameterDescriptor.BinaryTurn("prime-meridian-phase"),
            ParameterDescriptor.Choice("body-type", "rocky", "icy", "ocean", "gas-giant"),
        ],
        [
            new ConnectorKind("atmosphere", TransformKind.Rigid, 1, 1, [Atmosphere]),
            new ConnectorKind("orbit", TransformKind.OrbitalElements, 0, 8, [Planet]),
        ],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(Atmosphere, "atmosphere", [CompositionDomain.Planet],
        [
            ParameterDescriptor.Choice("model", "airless", "thin", "dense"),
            ParameterDescriptor.Integer("surface-pressure", 0, 100_000_000L),
            ParameterDescriptor.Choice("composition", "none", "nitrogen-oxygen", "carbon-dioxide", "hydrogen-helium", "methane"),
            ParameterDescriptor.Colour("scattering-tint"),
        ],
        [],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(SurfaceMaterial, "surface-material", [CompositionDomain.Surface, CompositionDomain.Artifact],
        [
            ParameterDescriptor.Choice("shader", "standard-opaque", "metallic", "emissive"),
            ParameterDescriptor.Colour("base-colour"),
            ParameterDescriptor.Integer("roughness", 0, 65_535),
            ParameterDescriptor.Integer("metallic", 0, 65_535),
            ParameterDescriptor.Ref("texture", TextureRecipe),
        ],
        [],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(TextureRecipe, "texture-recipe", [CompositionDomain.Surface, CompositionDomain.Artifact],
        [
            ParameterDescriptor.Choice("pattern", "flat", "noise", "stripes", "cells", "speckle"),
            ParameterDescriptor.Integer("scale", 65L, 4L << ArtifactFractionBits, ArtifactFractionBits),
            ParameterDescriptor.Colour("colour-a"),
            ParameterDescriptor.Colour("colour-b"),
            ParameterDescriptor.Integer("contrast", 0, 65_535),
        ],
        [],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(GeometryRecipe, "geometry-recipe", [CompositionDomain.Artifact],
        [
            ParameterDescriptor.Choice("shape", "sphere", "box", "cylinder", "capsule", "cone", "torus"),
            ParameterDescriptor.Vector3("size", 655L, 4L << ArtifactFractionBits, ArtifactFractionBits),
            ParameterDescriptor.Integer("segments", 3, 64),
            ParameterDescriptor.Integer("displacement", 0, 6_553L, ArtifactFractionBits),
        ],
        [],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(ArtifactPart, "artifact-part", [CompositionDomain.Artifact],
        [
            ParameterDescriptor.Ref("geometry", GeometryRecipe),
            ParameterDescriptor.Ref("material", SurfaceMaterial),
            ParameterDescriptor.Integer("mass", 1, 200_000L),
        ],
        [new ConnectorKind("attach", TransformKind.Rigid, 0, 8, [ArtifactPart])],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(Artifact, "artifact", [CompositionDomain.Artifact],
        [
            ParameterDescriptor.Choice("family", "mineral", "biological", "relic"),
            ParameterDescriptor.Ref("root", ArtifactPart),
            ParameterDescriptor.Integer("tier", 1, 2),
        ],
        [],
        StoragePolicies.Materialize, 0, []),
    ]);
}
