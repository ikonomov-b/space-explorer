using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 3: revision 2 with two changes, both about what a primitive suits. Its
/// records carry definition tags, which <see cref="CategoryRegistry.CarriesTags"/> gates, so a leaf
/// category with no connector of its own can still say what it is for; and a planet gains a `surface`
/// reference to a `surface-material`, so a body's appearance is generated content in the database rather
/// than a stand-in the view invents
/// ([decision 0055](../../../docs/decisions/0055-definition-level-tags-and-tagged-references.md)).
/// </summary>
/// <remarks>
/// Every other category is revision 2's unchanged, taken from it rather than restated, so the two cannot
/// drift apart in a way a reader would have to notice; the recorded hash of each revision is what guards
/// an accidental change to either.
/// </remarks>
public static class CategoryRegistryRevision3
{
    /// <summary>The parameter a planet gains: the material its surface is made of.</summary>
    public const string SurfaceParameter = "surface";

    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(3, [.. CategoryRegistryRevision2.Registry.Categories.Select(Grown)]);

    /// <summary>Revision 2's category, with the planet's surface reference appended to its parameters.</summary>
    private static CategoryDefinition Grown(CategoryDefinition category) =>
        category.Id != Planet
            ? category
            : new CategoryDefinition(
                category.Id,
                category.Label,
                category.Domains,
                [.. category.Parameters, ParameterDescriptor.Ref(SurfaceParameter, SurfaceMaterial)],
                category.Connectors,
                category.PermittedPolicies,
                category.MaxDerivedInstances,
                category.GeneratorRevisions,
                category.DerivedParameters);
}
