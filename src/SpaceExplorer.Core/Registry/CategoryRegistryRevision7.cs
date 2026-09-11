using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 7: revision 6 with a fourth relief parameter, how much of a body's ground is
/// folded into ridges rather than left as a smooth rise
/// ([decision 0065](../../../docs/decisions/0065-terrain-heightfield-version-2-ridges-and-registry-revision-7s-ridging.md)).
/// </summary>
/// <remarks>
/// It exists because cycle two's ground was the right height and the wrong shape: `terrain-heightfield/1`
/// sums smoothstep-interpolated noise, which makes dunes, and a rocky body needs ridges. How ridged a
/// world is, though, is a content judgement and not a rule's — ice flows and flattens, and an ocean body's
/// ground is drowned smooth — so it is a parameter a body declares rather than something the rule reads
/// off the body type.
///
/// Its default is zero, which is version 1's ground exactly, so a template that ranges nothing keeps the
/// shape it had. Every other category is revision 6's unchanged.
/// </remarks>
public static class CategoryRegistryRevision7
{
    /// <summary>How much of each octave is folded into ridges, out of 65,535: zero is dunes and the maximum is rock.</summary>
    public const string RidgingParameter = "relief-ridging";

    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(7,
    [
        .. CategoryRegistryRevision6.Registry.Categories.Select(Grown),
    ]);

    /// <summary>Revision 6's category, with the planet's ridging appended to its parameters.</summary>
    private static CategoryDefinition Grown(CategoryDefinition category) =>
        category.Id != Planet
            ? category
            : new CategoryDefinition(
                category.Id,
                category.Label,
                category.Domains,
                [
                    .. category.Parameters,
                    new ParameterDescriptor(
                        RidgingParameter,
                        ParameterKind.Integer,
                        0,
                        0,
                        ReliefField.RoughnessUnit,
                        [],
                        0,
                        ParameterUnit.Fraction,
                        ParameterValue.Integer(0)),
                ],
                category.Connectors,
                category.PermittedPolicies,
                category.MaxDerivedInstances,
                category.GeneratorRevisions,
                category.DerivedParameters);
}
