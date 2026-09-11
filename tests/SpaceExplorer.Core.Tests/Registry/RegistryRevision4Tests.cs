using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Registry;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Registry;

/// <summary>
/// Category registry revision 4: what a stored quantity means, what a template's silence means, and what
/// a decoder may allocate are all in the record now (decision 0060, review finding 50).
/// </summary>
public class RegistryRevision4Tests
{
    private static readonly CategoryRegistry Revision4 = CategoryRegistryRevision4.Registry;

    [Fact]
    public void Revision_4_round_trips_and_leaves_the_earlier_revisions_byte_identical()
    {
        Assert.Equal(Revision4.Hash, CategoryRegistry.Decode(Revision4.Bytes, 4).Hash);
        Assert.Throws<FormatException>(() => CategoryRegistry.Decode(Revision4.Bytes, 3));
        Assert.NotEqual(CategoryRegistryRevision3.Registry.Hash, Revision4.Hash);

        Assert.False(CategoryRegistryRevision3.Registry.CarriesUnits);
        Assert.True(Revision4.CarriesUnits);
        Assert.Equal(8, CategoryRegistries.Supported.Registries.Count);
    }

    [Fact]
    public void Every_stored_parameter_carries_a_unit_and_every_kind_that_admits_one_a_default()
    {
        foreach (CategoryDefinition category in Revision4.Categories)
        {
            foreach (ParameterDescriptor parameter in category.Parameters)
            {
                Assert.NotNull(parameter.Unit);
                Assert.Contains(parameter.Unit!.Value.Symbol, ParameterUnit.Symbols);

                // A reference is the one kind with no default, because a default is written when the
                // registry is and a reference names a definition that does not exist then.
                if (parameter.Kind == ParameterKind.PrimitiveRef)
                {
                    Assert.Null(parameter.Default);
                }
                else
                {
                    Assert.NotNull(parameter.Default);
                }
            }
        }
    }

    [Fact]
    public void The_units_are_the_ones_the_code_already_assumed()
    {
        // Three that decide a description: a mass in 10^20 kg, a radius in 1/256 m, a temperature in K.
        Assert.Equal(("kg", 20), Unit(Star, "mass"));
        Assert.Equal(("m", 0), Unit(Star, "radius"));
        Assert.Equal((byte)PlanetFractionBits, Revision4.Find(Star).FindParameter("radius")!.FractionBits);
        Assert.Equal(("K", 0), Unit(Star, "effective-temperature"));

        // A fraction reads against its own maximum, and a count is not a quantity at all.
        Assert.Equal((ParameterUnit.Ratio, 0), Unit(Planet, "albedo"));
        Assert.Equal(65_535, Revision4.Find(Planet).FindParameter("albedo")!.Max);
        Assert.Equal((ParameterUnit.Count, 0), Unit(GeometryRecipe, "segments"));

        // And the one row decision 0060 had to decide rather than record: grams.
        Assert.Equal(("kg", -3), Unit(ArtifactPart, "mass"));
    }

    [Fact]
    public void A_derived_parameter_carries_the_descriptor_a_stored_one_carries()
    {
        DerivedParameter luminosity = Revision4.Find(Star).DerivedParameters.Single(parameter => parameter.Label == "luminosity");
        DerivedParameter spectralClass = Revision4.Find(Star).DerivedParameters.Single(parameter => parameter.Label == "spectral-class");

        Assert.Equal(DerivationRules.LuminosityRevision, luminosity.Rule);
        Assert.Equal(("W", 20), (luminosity.Descriptor!.Unit!.Value.Symbol, luminosity.Descriptor.Unit!.Value.Exponent));

        // The seven classes lived only in the rule; they are in the record now.
        Assert.Equal(["O", "B", "A", "F", "G", "K", "M"], spectralClass.Descriptor!.EnumLabels);
        Assert.Null(spectralClass.Descriptor.Default);
    }

    [Fact]
    public void A_barycentre_derives_the_mass_decision_0037_gives_it()
    {
        DerivedParameter mass = Assert.Single(Revision4.Find(Barycentre).DerivedParameters);

        Assert.Equal("mass", mass.Label);
        Assert.Equal(CategoryRegistryRevision4.MassAggregationRule, mass.Rule);
        Assert.Equal("aggregate-sum/1", mass.Rule);
        Assert.Equal(("kg", 20), (mass.Descriptor!.Unit!.Value.Symbol, mass.Descriptor.Unit!.Value.Exponent));

        // Revision 3 declared nothing, which is what let a published pair contradict its own masses.
        Assert.Empty(CategoryRegistryRevision3.Registry.Find(Barycentre).DerivedParameters);
    }

    [Fact]
    public void The_record_carries_the_bounds_a_decoder_allocates_against()
    {
        // A record whose bounds are not this build's is refused rather than reinterpreted, which is the
        // point of moving them out of the build and into the bytes.
        Assert.Equal(7, CategoryRegistry.DecoderBounds.Count);
        Assert.Contains(CategoryRegistry.MaxCategories, CategoryRegistry.DecoderBounds);
        Assert.Contains(CategoryDefinition.MaxParameters, CategoryRegistry.DecoderBounds);
        Assert.Contains(ParameterDescriptor.MaxEnumLabels, CategoryRegistry.DecoderBounds);

        // The fan-out bound defaults to what the connectors already admit.
        CategoryDefinition planet = Revision4.Find(Planet);
        Assert.Equal(planet.Connectors.Sum(connector => (int)connector.MaxCount), planet.MaxChildrenTotal);

        // And the record carries them: decoding under this build succeeds, and the bounds are what the
        // decoder compares against before it allocates anything after the header.
        Assert.Equal(Revision4.Hash, CategoryRegistry.Decode(Revision4.Bytes, 4).Hash);
    }

    private static (string Symbol, int Exponent) Unit(uint category, string label)
    {
        ParameterUnit unit = Revision4.Find(category).FindParameter(label)!.Unit!.Value;
        return (unit.Symbol, unit.Exponent);
    }
}
