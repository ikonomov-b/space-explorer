using SpaceExplorer.Core.Registry;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Registry;

public class CompositionGrammarTests
{
    private static readonly CategoryRegistry Revision1 = CategoryRegistryRevision1.Registry;

    [Fact]
    public void Version_1_round_trips_through_its_canonical_record()
    {
        CompositionGrammar grammar = CompositionGrammarVersion1.Grammar;
        CompositionGrammar decoded = CompositionGrammar.Decode(grammar.Bytes, Revision1, CompositionGrammar.CurrentVersion);

        Assert.Equal(grammar.Hash, decoded.Hash);
        Assert.Equal("composition-grammar/1/registry/1", Revision1.GrammarDomain(CompositionGrammar.CurrentVersion));
        Assert.Equal(2, decoded.Roots.Count);
        Assert.Equal(5, decoded.Productions.Count);
        Assert.Throws<FormatException>(() => CompositionGrammar.Decode(grammar.Bytes, Revision1, 2));
        Assert.Throws<FormatException>(() => CompositionGrammar.Decode([.. grammar.Bytes, 0], Revision1, CompositionGrammar.CurrentVersion));
    }

    [Fact]
    public void Version_1_hash_is_frozen()
    {
        // This build's own output, recorded so an unrecorded change to a count, weight, or transform range
        // is caught; every graph specification pins it (decision 0047).
        Assert.Equal(
            "611829f1b7e9c547a11e88f1b795fef6c51adcd120a601c6eea3eb5b1a4506c7",
            CompositionGrammarVersion1.Grammar.Hash.ToString());
    }

    [Fact]
    public void Version_1_states_what_a_star_and_a_planet_need_at_the_least()
    {
        CompositionGrammar grammar = CompositionGrammarVersion1.Grammar;

        // A planet needs its one atmosphere; a star needs one body, whose cheapest form is that planet.
        Assert.Equal(0u, grammar.RequiredDepth(Atmosphere));
        Assert.Equal((1u, 2L), (grammar.RequiredDepth(Planet), grammar.RequiredNodes(Planet)));
        Assert.Equal((2u, 3L), (grammar.RequiredDepth(Star), grammar.RequiredNodes(Star)));
    }

    [Fact]
    public void A_rule_the_registry_does_not_admit_is_refused()
    {
        // The registry gives a planet exactly one atmosphere and a rigid transform for it (decision 0036).
        Assert.Throws<ArgumentException>(() => Planets(new ConnectorRule(0, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)])));
        Assert.Throws<ArgumentException>(() => Planets(new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.OrbitalElements), [new CategoryChoice(Atmosphere, 1)])));
        Assert.Throws<ArgumentException>(() => Planets(new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Star, 1)])));
        Assert.Throws<ArgumentException>(() => Planets(new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 0)])));
    }

    [Fact]
    public void A_grammar_whose_productions_and_reachable_categories_disagree_is_refused()
    {
        // The atmosphere production is missing, so a required child has no rules.
        Assert.Throws<ArgumentException>(() => CompositionGrammar.Create(Revision1, 1, 3, 16, 4,
            [new RootRule(CompositionDomain.Planet, [new CategoryChoice(Planet, 1)])],
            [PlanetProduction()]));

        // The star production is there but no root reaches it.
        Assert.Throws<ArgumentException>(() => CompositionGrammar.Create(Revision1, 1, 3, 16, 4,
            [new RootRule(CompositionDomain.Planet, [new CategoryChoice(Planet, 1)])],
            [PlanetProduction(), new Production(Atmosphere, []), StarProduction()]));
    }

    [Fact]
    public void A_root_whose_required_structure_does_not_fit_the_bounds_is_refused()
    {
        // A star needs three instances and a depth of two; neither bound allows it.
        Assert.Throws<ArgumentException>(() => System(maxDepth: 1, maxNodes: 16));
        Assert.Throws<ArgumentException>(() => System(maxDepth: 3, maxNodes: 2));
        Assert.NotNull(System(maxDepth: 2, maxNodes: 3));
    }

    [Fact]
    public void A_category_that_requires_itself_is_refused()
    {
        // A planet that must carry a moon must carry a moon, without end.
        Assert.Throws<ArgumentException>(() => CompositionGrammar.Create(Revision1, 1, 3, 16, 4,
            [new RootRule(CompositionDomain.Planet, [new CategoryChoice(Planet, 1)])],
            [
                new Production(Atmosphere, []),
                new Production(Planet,
                [
                    new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
                    new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.OrbitalElements), [new CategoryChoice(Planet, 1)]),
                ]),
            ]));
    }

    private static CompositionGrammar System(uint maxDepth, uint maxNodes) => CompositionGrammar.Create(Revision1, 1, maxDepth, maxNodes, 4,
        [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)])],
        [StarProduction(), PlanetProduction(), new Production(Atmosphere, [])]);

    private static Production StarProduction() => new(Star,
        [new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.OrbitalElements), [new CategoryChoice(Planet, 1)])]);

    private static Production PlanetProduction() => new(Planet,
    [
        new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
        new ConnectorRule(0, 0, TransformRange.Origin(TransformKind.OrbitalElements), []),
    ]);

    private static CompositionGrammar Planets(ConnectorRule atmosphere) => CompositionGrammar.Create(Revision1, 1, 3, 16, 4,
        [new RootRule(CompositionDomain.Planet, [new CategoryChoice(Planet, 1)])],
        [
            new Production(Atmosphere, []),
            new Production(Planet, [atmosphere, new ConnectorRule(0, 0, TransformRange.Origin(TransformKind.OrbitalElements), [])]),
        ]);
}
