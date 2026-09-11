using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 5: revision 4 with the first category of the level below the solar system.
/// A `region` is the bounded landing area a player walks, and a planet gains the `surface-anchor`
/// connector that places one on it, so a stored solar system can at last carry something a person stands
/// on ([decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)).
/// </summary>
/// <remarks>
/// The region stores one quantity, its extent, and wears the material its planet already stores under
/// the `surface` reference revision 3 gave it: rung one of that record's ladder is a flat tangent region
/// and nothing else, so relief, biomes, scatter and chunks each arrive with the rung that needs them and
/// with the revision that carries it.
///
/// The extent lives here rather than in a second region-limits version because it is a bound on a stored
/// quantity, which is exactly what [decision 0060](../../../docs/decisions/0060-registry-revision-4-units-defaults-and-validation-limits.md)
/// put in the registry record, and because moving the region-limits version would move the description's
/// version, its frozen vector, and every destination identity for a number that is neither a suit limit
/// nor a shape limit. Its maximum and its default are both the 2,048 m per axis of
/// [decision 0017](../../../docs/decisions/0017-region-extent-cap-and-storage-derivation.md), which
/// derives the storage budget from it.
///
/// Every other category is revision 4's unchanged, taken from it rather than restated, so the two cannot
/// drift apart; the recorded hash of each revision guards an accidental change to either.
/// </remarks>
public static class CategoryRegistryRevision5
{
    /// <summary>The category a landing area is: one node of the surface level, addressed inside its planet's graph.</summary>
    public const uint Region = 10;

    /// <summary>The connector a planet places a region on, carrying latitude, longitude, height, and heading (decision 0036).</summary>
    public const string SurfaceAnchorConnector = "surface-anchor";

    /// <summary>The one quantity a region stores at rung one: how far it reaches, per axis, in metres.</summary>
    public const string ExtentParameter = "extent";

    /// <summary>The extent cap of decision 0017, in metres: 2,048 m per axis at 2 m cells is 1,024 cells a side.</summary>
    public const long MaximumExtentMetres = 2_048;

    /// <summary>The least extent worth walking: an eighth of the cap, so a region is never a doorstep.</summary>
    public const long MinimumExtentMetres = 256;

    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(5,
    [
        .. CategoryRegistryRevision4.Registry.Categories.Select(Grown),

        new CategoryDefinition(Region, "region", [CompositionDomain.SolarSystem, CompositionDomain.Surface],
        [
            new ParameterDescriptor(
                ExtentParameter,
                ParameterKind.Integer,
                0,
                MinimumExtentMetres,
                MaximumExtentMetres,
                [],
                0,
                ParameterUnit.Of("m"),
                ParameterValue.Integer(MaximumExtentMetres)),
        ],
        [],
        StoragePolicies.Materialize, 0, []),
    ]);

    /// <summary>Revision 4's category, with the planet's surface anchor appended to its connectors.</summary>
    private static CategoryDefinition Grown(CategoryDefinition category) =>
        category.Id != Planet
            ? category
            : new CategoryDefinition(
                category.Id,
                category.Label,
                category.Domains,
                category.Parameters,
                [.. category.Connectors, new ConnectorKind(SurfaceAnchorConnector, TransformKind.SurfaceAnchor, 0, 1, [Region])],
                category.PermittedPolicies,
                category.MaxDerivedInstances,
                category.GeneratorRevisions,
                category.DerivedParameters);
}
