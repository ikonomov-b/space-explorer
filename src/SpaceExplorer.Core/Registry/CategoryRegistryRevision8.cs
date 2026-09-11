using static SpaceExplorer.Core.Registry.CategoryRegistryRevision5;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 8: revision 7 with the `region` category's ground stored rather than only
/// reproducible ([decision 0066](../../../docs/decisions/0066-generation-writes-to-the-database-and-the-view-only-reads.md)).
/// </summary>
/// <remarks>
/// It permits `Regenerate | Materialize`, which is decision 0034's hybrid: the heightfield is written to
/// the data root when it is first generated and loaded thereafter, and the rule that produced it stays
/// named so the bytes can be made again if they are ever lost. Revision 7 permitted regeneration alone,
/// on the arithmetic that materialising a whole planet costs 804 GiB — true of every region a planet could
/// hold, and not of the handful a player actually stands on, which is the distinction decision 0066 draws.
///
/// Every other category is revision 7's unchanged.
/// </remarks>
public static class CategoryRegistryRevision8
{
    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(8,
    [
        .. CategoryRegistryRevision7.Registry.Categories.Select(Grown),
    ]);

    /// <summary>Revision 7's category, with the region's ground admitted to storage.</summary>
    private static CategoryDefinition Grown(CategoryDefinition category) =>
        category.Id != Region
            ? category
            : new CategoryDefinition(
                category.Id,
                category.Label,
                category.Domains,
                category.Parameters,
                category.Connectors,
                StoragePolicies.Regenerate | StoragePolicies.Materialize,
                category.MaxDerivedInstances,
                category.GeneratorRevisions,
                category.DerivedParameters);
}
