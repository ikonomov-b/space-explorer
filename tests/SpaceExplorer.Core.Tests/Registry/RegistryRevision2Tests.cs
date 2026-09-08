using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Registry;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Registry;

/// <summary>
/// Registry revision 2 and grammar version 2, and the rule that lets them add a field without moving
/// revision 1's or version 1's bytes: a record's domain label carries its version, so the reader
/// dispatches on the label it opened (decisions 0035, 0050).
/// </summary>
public class RegistryRevision2Tests
{
    [Fact]
    public void Revision_2_round_trips_and_leaves_revision_1_byte_identical()
    {
        CategoryRegistry revision2 = CategoryRegistryRevision2.Registry;

        Assert.Equal(revision2.Hash, CategoryRegistry.Decode(revision2.Bytes, 2).Hash);
        Assert.NotEqual(CategoryRegistryRevision1.Registry.Hash, revision2.Hash);
        Assert.Throws<FormatException>(() => CategoryRegistry.Decode(revision2.Bytes, 1));

        // The frozen hash of revision 1 is asserted on its own; this states why it may be trusted after a
        // field was added to the record: revision 1 does not encode it.
        Assert.Empty(CategoryRegistryRevision1.Registry.Find(Planet).DerivedParameters);
        Assert.Equal(2, CategoryRegistries.Supported.Registries.Count);
    }

    [Fact]
    public void Revision_2_derives_what_it_no_longer_stores()
    {
        CategoryDefinition planet = CategoryRegistryRevision2.Registry.Find(Planet);
        CategoryDefinition star = CategoryRegistryRevision2.Registry.Find(Star);

        Assert.DoesNotContain(planet.Parameters, parameter => parameter.Label == "radius");
        Assert.Contains(planet.Parameters, parameter => parameter.Label == "density");
        Assert.Equal(DerivationRules.RadiusRevision, planet.DerivationOf("radius"));

        Assert.DoesNotContain(star.Parameters, parameter => parameter.Label == "spectral-class");
        Assert.DoesNotContain(star.Parameters, parameter => parameter.Label == "luminosity");
        Assert.Equal(DerivationRules.SpectralClassRevision, star.DerivationOf("spectral-class"));
        Assert.Equal(DerivationRules.LuminosityRevision, star.DerivationOf("luminosity"));

        Assert.Null(planet.DerivationOf("mass"));
    }

    [Fact]
    public void A_category_cannot_derive_what_it_stores()
    {
        Assert.Throws<ArgumentException>(() => new CategoryDefinition(1, "a", [CompositionDomain.Artifact],
            [ParameterDescriptor.Integer("mass", 1, 2)], [], StoragePolicies.Materialize, 0, [],
            [new DerivedParameter("mass", "derive-mass/1")]));

        Assert.Throws<ArgumentException>(() => new CategoryDefinition(1, "a", [CompositionDomain.Artifact],
            [], [], StoragePolicies.Materialize, 0, [],
            [new DerivedParameter("radius", "not a path")]));
    }

    [Fact]
    public void Grammar_version_2_orders_and_spaces_the_orbits_it_hands_out()
    {
        CompositionGrammar grammar = CompositionGrammarVersion2.Grammar;
        ConnectorRule orbit = grammar.TryFindProduction(Star)!.ConnectorRules[0];

        Assert.Equal(2u, grammar.Version);
        Assert.Equal(2u, grammar.RegistryRevision);
        Assert.Equal(CompositionGrammarVersion2.PlanetSpacing, orbit.SpacingRatio);

        // Each band begins where the last ended, and every one of the eight fits inside the declared range.
        long previous = 0;
        for (int index = 0; index < 8; index++)
        {
            (long low, long high) = orbit.OrbitBand(index);
            Assert.True(low < high, $"band {index} is empty");
            Assert.True(low >= previous, $"band {index} starts before the last one ended");
            Assert.InRange(high, orbit.Transform.Bounds[0].Min, orbit.Transform.Bounds[0].Max);
            previous = high;
        }

        // Version 1 carries no ratio and hands every child the whole range.
        Assert.Equal(0u, CompositionGrammarVersion1.Grammar.TryFindProduction(Star)!.ConnectorRules[0].SpacingRatio);
    }

    [Fact]
    public void Version_2_round_trips_and_leaves_version_1_byte_identical()
    {
        CompositionGrammar version2 = CompositionGrammarVersion2.Grammar;

        Assert.Equal(version2.Hash, CompositionGrammar.Decode(version2.Bytes, CategoryRegistryRevision2.Registry, 2).Hash);
        Assert.Throws<FormatException>(() => CompositionGrammar.Decode(version2.Bytes, CategoryRegistryRevision2.Registry, 1));
        Assert.Equal(2, CompositionGrammars.Supported.Grammars.Count);
        Assert.Equal(version2.Version, CompositionGrammars.Supported.Newest(2)!.Version);
        Assert.Equal(1u, CompositionGrammars.Supported.Newest(1)!.Version);
        Assert.Null(CompositionGrammars.Supported.Newest(3));
    }

    [Fact]
    public void A_spacing_ratio_is_refused_where_it_cannot_be_carried_or_cannot_fit()
    {
        // Version 1 does not encode a ratio, an orbit is the only connector with an axis to space, and a
        // ratio that runs past the rule's own maximum before its last child is a grammar that cannot work.
        Assert.Throws<ArgumentException>(() => System(version: 1, ratio: 400, maxOrbit: 100_000_000_000L));
        Assert.Throws<ArgumentException>(() => System(version: 2, ratio: 256, maxOrbit: 100_000_000_000L));
        Assert.Throws<ArgumentException>(() => System(version: 2, ratio: 4_000, maxOrbit: 200_000_000L));
        Assert.NotNull(System(version: 2, ratio: 400, maxOrbit: 100_000_000_000L));
    }

    private static CompositionGrammar System(uint version, uint ratio, long maxOrbit) => CompositionGrammar.Create(
        CategoryRegistryRevision2.Registry, version, 3, 16, 4,
        [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)])],
        [
            new Production(Star,
            [
                new ConnectorRule(1, 4, new TransformRange(TransformKind.OrbitalElements,
                    [(100_000_000L, maxOrbit), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0)]),
                [new CategoryChoice(Planet, 1)], ratio),
            ]),
            new Production(Planet,
            [
                new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
                new ConnectorRule(0, 0, TransformRange.Origin(TransformKind.OrbitalElements), []),
            ]),
            new Production(Atmosphere, []),
        ]);
}
