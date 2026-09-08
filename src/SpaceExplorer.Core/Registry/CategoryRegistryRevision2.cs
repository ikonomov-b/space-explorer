using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 2: revision 1 with the parameters that could contradict each other removed
/// and derived instead (review finding 47). A planet carries a mean density
/// and no radius, because a radius drawn beside a mass fixes a density that nothing checked, and a star
/// carries neither a spectral class nor a luminosity, because both follow from its effective temperature
/// and radius. Each category declares what it no longer stores as a derived parameter naming the rule
/// that computes it, which is decision 0037's computed default and the pin decision 0035 requires.
/// Everything else is revision 1 unchanged.
/// </summary>
public static class CategoryRegistryRevision2
{
    /// <summary>The least mean density in kg/m^3 a body may draw: below Saturn's 687, and above a comet's.</summary>
    public const long MinimumDensity = 300;

    /// <summary>The greatest mean density in kg/m^3: above iron's 7,874 and below osmium's 22,590.</summary>
    public const long MaximumDensity = 12_000;

    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(2,
    [
        new CategoryDefinition(Star, "star", [CompositionDomain.SolarSystem],
        [
            ParameterDescriptor.Integer("mass", 1_000_000_000L, 400_000_000_000L),
            ParameterDescriptor.Integer("radius", 10_000_000L << PlanetFractionBits, 2_000_000_000L << PlanetFractionBits, PlanetFractionBits),
            ParameterDescriptor.Integer("effective-temperature", 2_000L, 50_000L),
        ],
        [new ConnectorKind("orbit", TransformKind.OrbitalElements, 0, 16, [Planet, Barycentre])],
        StoragePolicies.Materialize, 0, [],
        [
            new DerivedParameter("luminosity", DerivationRules.LuminosityRevision),
            new DerivedParameter("spectral-class", DerivationRules.SpectralClassRevision),
        ]),

        new CategoryDefinition(Barycentre, "barycentre", [CompositionDomain.SolarSystem],
        [],
        [new ConnectorKind("orbit", TransformKind.OrbitalElements, 2, 8, [Star, Planet, Barycentre])],
        StoragePolicies.Materialize, 0, []),

        new CategoryDefinition(Planet, "planet", [CompositionDomain.SolarSystem, CompositionDomain.Planet],
        [
            ParameterDescriptor.Integer("mass", 1L, 100_000_000L),
            ParameterDescriptor.Integer("density", MinimumDensity, MaximumDensity),
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
        StoragePolicies.Materialize, 0, [],
        [new DerivedParameter("radius", DerivationRules.RadiusRevision)]),

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
