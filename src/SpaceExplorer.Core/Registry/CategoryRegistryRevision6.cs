using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Category registry revision 6: revision 5 with the ground a planet's surface is shaped by. A planet
/// gains the three parameters of its relief field, and a region stops permitting materialisation and
/// declares the rule that regenerates it instead
/// ([decision 0063](../../../docs/decisions/0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md),
/// rung 2 of [decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)'s ladder).
/// </summary>
/// <remarks>
/// Relief is a recipe and not a grid, so this revision adds no bulk parameter kind and no cube-sphere
/// grid: a stored grid at the 2 m cells rung 2 asks for costs 1.57 TiB on a body of the minimum landable
/// radius, and one coarse enough to fit is 370 times coarser than the rung needs. The three parameters
/// below are the whole of a planet's ground, and `terrain-heightfield/1` turns them into samples on
/// demand.
///
/// This is the one category whose storage policy stops being <see cref="StoragePolicies.Materialize"/>,
/// and it stops for the category whose arithmetic already decided it: materialising the smallest landable
/// body costs 804 GiB against the 300 MiB decision 0017 budgets a whole campaign. A category lists
/// generator revisions exactly when it permits regeneration, so the policy and the rule that honours it
/// cannot drift apart.
///
/// Every other category is revision 5's unchanged, taken from it rather than restated; the recorded hash
/// of each revision guards an accidental change to either.
/// </remarks>
public static class CategoryRegistryRevision6
{
    /// <summary>How far a planet's ground rises and falls from its reference sphere, in metres.</summary>
    public const string AmplitudeParameter = "relief-amplitude";

    /// <summary>How much of each octave's amplitude the next one keeps, out of 65,535.</summary>
    public const string RoughnessParameter = "relief-roughness";

    /// <summary>The coarsest relief feature's wavelength in metres; each octave halves it.</summary>
    public const string WavelengthParameter = "relief-wavelength";

    /// <summary>The greatest amplitude a body may declare: half of what an int16 at 1/16 m holds, leaving rungs 3 and 5 the other half.</summary>
    public const long MaximumAmplitudeMetres = 1_024;

    /// <summary>The coarsest wavelength a body may declare, which is twelve octaves above the finest.</summary>
    public const long MaximumWavelengthMetres = 8_192;

    public static readonly CategoryRegistry Registry = CategoryRegistry.Create(6,
    [
        .. CategoryRegistryRevision5.Registry.Categories.Select(Grown),
    ]);

    /// <summary>Revision 5's category, with a planet's relief and a region's regeneration added to it.</summary>
    private static CategoryDefinition Grown(CategoryDefinition category)
    {
        if (category.Id == Planet)
        {
            return new CategoryDefinition(
                category.Id,
                category.Label,
                category.Domains,
                [
                    .. category.Parameters,
                    Quantity(AmplitudeParameter, "m", 0, MaximumAmplitudeMetres, 128),
                    Fraction(RoughnessParameter, 32_768),
                    Quantity(WavelengthParameter, "m", ReliefField.FinestWavelengthMetres, MaximumWavelengthMetres, 2_048),
                ],
                category.Connectors,
                category.PermittedPolicies,
                category.MaxDerivedInstances,
                category.GeneratorRevisions,
                category.DerivedParameters);
        }

        if (category.Id != CategoryRegistryRevision5.Region)
        {
            return category;
        }

        return new CategoryDefinition(
            category.Id,
            category.Label,
            category.Domains,
            category.Parameters,
            category.Connectors,
            StoragePolicies.Regenerate,
            category.MaxDerivedInstances,
            [TerrainHeightfield.Rule],
            category.DerivedParameters);
    }

    private static ParameterDescriptor Quantity(string label, string symbol, long min, long max, long standard) =>
        new(label, ParameterKind.Integer, 0, min, max, [], 0, ParameterUnit.Of(symbol), ParameterValue.Integer(standard));

    private static ParameterDescriptor Fraction(string label, long standard) =>
        new(label, ParameterKind.Integer, 0, 0, ReliefField.RoughnessUnit, [], 0, ParameterUnit.Fraction, ParameterValue.Integer(standard));
}
