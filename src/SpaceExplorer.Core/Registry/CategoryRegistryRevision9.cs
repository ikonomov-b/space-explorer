using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 9: what a planet's ground is made of, where the previous revisions said only
/// what shape it is ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md),
/// rung 3 of [decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)'s ladder).
/// </summary>
/// <remarks>
/// It adds the `biome-element` category, whose generated definitions are what a patch is an instance of and
/// whose authored templates are the biome rule set [decision 0026](../../../docs/decisions/0026-biome-rule-sets-and-recovery-challenges-are-template-vocabularies.md)
/// describes — a record whose premise was that the category already existed, which no revision before this
/// one made true.
///
/// A planet gains two things beside it. `biomes` is the palette its regions paint from, an ordered array of
/// 2 to 6 references, which is the first `RefList` in the schema; it sits on the planet and not the region
/// because a planet has one climate, exactly as its relief field does. `sea-level` is the ocean datum
/// [decision 0041](../../../docs/decisions/0041-planet-fields-tangent-regions-minimum-radius-and-far-field.md)
/// names beside relief, one height in metres, defaulting to the floor so a body that declares nothing has
/// no sea rather than a sea nobody chose.
///
/// Every other category is revision 8's unchanged.
/// </remarks>
public static class CategoryRegistryRevision9
{
    /// <summary>What one kind of ground is: a material, and the conditions under which it claims a cell.</summary>
    public const uint BiomeElement = 11;

    /// <summary>The palette a planet's regions paint from, in the order the vocabulary drew it.</summary>
    public const string BiomesParameter = "biomes";

    /// <summary>The height of the sea surface above the body's reference sphere, in metres.</summary>
    public const string SeaLevelParameter = "sea-level";

    /// <summary>Which material this biome wears, as a reference to a `surface-material`.</summary>
    public const string BiomeSurfaceParameter = "surface";

    /// <summary>Whether this biome is what lies under the sea rather than on land.</summary>
    public const string SubmergedParameter = "biome-submerged";

    /// <summary>The low end of the elevation band this biome claims, as a signed fraction of the body's own relief amplitude.</summary>
    public const string ElevationLowParameter = "biome-elevation-low";

    /// <summary>The high end of that band.</summary>
    public const string ElevationHighParameter = "biome-elevation-high";

    /// <summary>The fewest and most biomes a planet's palette may hold.</summary>
    public const long MinimumBiomes = 2;
    public const long MaximumBiomes = 6;

    /// <summary>
    /// The sea datum's range, which is decision 0063's amplitude bound: no height exceeds the relief
    /// amplitude, itself capped at 1,024 m, so a datum outside this could only mean all sea or none, which
    /// the ends already mean. The default is the floor, because no height is strictly below it.
    /// </summary>
    public const long SeaLevelFloorMetres = -CategoryRegistryRevision6.MaximumAmplitudeMetres;
    public const long SeaLevelCeilingMetres = CategoryRegistryRevision6.MaximumAmplitudeMetres;

    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(9,
    [
        .. CategoryRegistryRevision8.Registry.Categories.Select(Grown),

        new CategoryDefinition(BiomeElement, "biome-element", [CompositionDomain.SolarSystem, CompositionDomain.Surface],
        [
            new ParameterDescriptor(BiomeSurfaceParameter, ParameterKind.PrimitiveRef, 0, 0, 0, [], SurfaceMaterial, ParameterUnit.None),
            new ParameterDescriptor(SubmergedParameter, ParameterKind.Bool, 0, 0, 0, [], 0, ParameterUnit.None, ParameterValue.Bool(false)),
            Band(ElevationLowParameter, -ReliefField.RoughnessUnit),
            Band(ElevationHighParameter, ReliefField.RoughnessUnit),
        ],
        [],
        StoragePolicies.Materialize, 0, []),
    ]);

    /// <summary>Revision 8's category, with the planet's palette and sea datum added to it.</summary>
    private static CategoryDefinition Grown(CategoryDefinition category) =>
        category.Id != Planet
            ? category
            : new CategoryDefinition(
                category.Id,
                category.Label,
                category.Domains,
                [
                    .. category.Parameters,
                    new ParameterDescriptor(BiomesParameter, ParameterKind.RefList, 0, MinimumBiomes, MaximumBiomes, [], BiomeElement, ParameterUnit.None),
                    new ParameterDescriptor(
                        SeaLevelParameter,
                        ParameterKind.Integer,
                        0,
                        SeaLevelFloorMetres,
                        SeaLevelCeilingMetres,
                        [],
                        0,
                        ParameterUnit.Of("m"),
                        ParameterValue.Integer(SeaLevelFloorMetres)),
                ],
                category.Connectors,
                category.PermittedPolicies,
                category.MaxDerivedInstances,
                category.GeneratorRevisions,
                category.DerivedParameters);

    /// <summary>One end of an elevation band: a signed fraction of the body's relief amplitude, widest by default.</summary>
    private static ParameterDescriptor Band(string label, long standard) =>
        new(label, ParameterKind.Integer, 0, -ReliefField.RoughnessUnit, ReliefField.RoughnessUnit, [], 0, ParameterUnit.Fraction, ParameterValue.Integer(standard));
}
