using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision5;

namespace SpaceExplorer.Core.Tests.Registry;

/// <summary>
/// Registry revision 5 and composition grammar version 6: the first category of the level below the
/// solar system, and the gated anchor that places one on a body large enough to carry it. Rung one of
/// the ladder of [decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md).
/// </summary>
public class SurfaceRegionTests
{
    private static readonly CategoryRegistry Revision5 = CategoryRegistryRevision5.Registry;

    [Fact]
    public void Revision_5_and_version_6_round_trip_and_leave_the_earlier_records_byte_identical()
    {
        Assert.Equal(Revision5.Hash, CategoryRegistry.Decode(Revision5.Bytes, 5).Hash);
        Assert.Throws<FormatException>(() => CategoryRegistry.Decode(Revision5.Bytes, 4));
        Assert.NotEqual(CategoryRegistryRevision4.Registry.Hash, Revision5.Hash);

        CompositionGrammar version6 = CompositionGrammarVersion6.Grammar;
        Assert.Equal(version6.Hash, CompositionGrammar.Decode(version6.Bytes, Revision5, 6).Hash);
        Assert.Throws<FormatException>(() => CompositionGrammar.Decode(version6.Bytes, Revision5, 5));

        // A revision and a grammar are added, never edited: every earlier record keeps the bytes and the
        // hash the content published under it was pinned by (decisions 0035, 0050).
        Assert.Equal(6, CategoryRegistries.Supported.Registries.Count);
        Assert.Equal(7, CompositionGrammars.Supported.Grammars.Count);
        Assert.Equal(6u, CompositionGrammars.Supported.Newest(5)!.Version);
        Assert.Equal(5u, CompositionGrammars.Supported.Newest(4)!.Version);
        Assert.Equal(4u, CompositionGrammars.Supported.Newest(3)!.Version);
    }

    [Fact]
    public void A_region_stores_its_extent_and_a_planet_gains_the_anchor_that_places_one()
    {
        CategoryDefinition region = Revision5.Find(Region);
        ParameterDescriptor extent = Assert.Single(region.Parameters);

        // Decision 0060's standard, met without a second region-limits version: the cap of decision 0017
        // is the parameter's own maximum and its default, and the unit is in the record.
        Assert.Equal(ExtentParameter, extent.Label);
        Assert.Equal("m", extent.Unit!.Value.Symbol);
        Assert.Equal(MaximumExtentMetres, extent.Max);
        Assert.Equal(MaximumExtentMetres, extent.Default!.Value.AsInteger);
        Assert.Equal(MinimumExtentMetres, extent.Min);
        Assert.Empty(region.Connectors);

        // The planet gains one connector and keeps the two it had, in that order, because a production
        // states one rule per connector kind in the category's own order.
        CategoryDefinition planet = Revision5.Find(Planet);
        Assert.Equal(2, CategoryRegistryRevision4.Registry.Find(Planet).Connectors.Count);
        ConnectorKind anchor = Assert.Single(planet.Connectors, connector => connector.Label == SurfaceAnchorConnector);
        Assert.Equal(TransformKind.SurfaceAnchor, anchor.Transform);
        Assert.Equal([Region], anchor.ChildCategories);

        // And the grammar demands exactly one region of every planet its gate admits, at the reference
        // radius itself, because rung one has no relief for an anchor to sit above.
        ConnectorRule rule = CompositionGrammarVersion6.Grammar.TryFindProduction(Planet)!.ConnectorRules[2];
        Assert.Equal(ConnectorGates.LandableBodyRevision, rule.Gate);
        Assert.Equal(1u, rule.MinCount);
        Assert.Equal(1u, rule.MaxCount);
        Assert.Equal((0L, 0L), rule.Transform.Bounds[2]);
    }

    [Fact]
    public void The_gate_gives_a_region_to_a_body_large_enough_to_carry_one_and_to_no_other()
    {
        // The two bodies differ in nothing the grammar reads but their mass and density, from which the
        // radius the gate reads is derived — it is declared in no template and no tag could carry it.
        long large = DerivationRules.Radius(RegionFixture.LargeMass, RegionFixture.LargeDensity);
        long small = DerivationRules.Radius(RegionFixture.SmallMass, RegionFixture.SmallDensity);
        Assert.True(RegionLimits.Version1.AdmitsRegion(large));
        Assert.False(RegionLimits.Version1.AdmitsRegion(small));

        Assert.Single(RegionFixture.Regions(RegionFixture.Compose(RegionFixture.LargeBody)));
        Assert.Empty(RegionFixture.Regions(RegionFixture.Compose(RegionFixture.SmallBody)));

        // Size is not the whole of it. The giant is the large body's equal in mass and density and
        // declares no ground, so it takes no region — which is what keeps a 96,000 km ball of hydrogen
        // from carrying a walkable patch, as the first cycle's sample showed it would.
        Assert.Empty(RegionFixture.Regions(RegionFixture.Compose(RegionFixture.GiantBody)));

        // What the gate is worth, stated as the two mutations that remove it: without it the small body
        // and the giant each take a region, which is decision 0041's clause broken twice over.
        Assert.Single(RegionFixture.Regions(RegionFixture.Compose(RegionFixture.SmallBody, gate: "")));
        Assert.Single(RegionFixture.Regions(RegionFixture.Compose(RegionFixture.GiantBody, gate: "")));
    }

    [Fact]
    public void A_gate_is_pinned_by_the_grammar_that_names_it_and_refused_where_it_is_unknown()
    {
        // The gate's identifier is encoded in the grammar record, so the graph that pins the grammar
        // pins what decided its regions; an unknown rule and a rule before version 6 are both refused.
        Assert.Contains("admit-landable-body/1", System.Text.Encoding.UTF8.GetString(CompositionGrammarVersion6.Grammar.Bytes));
        Assert.False(ConnectorGates.IsKnown("admit-landable-body/2"));

        ArgumentException unknown = Assert.Throws<ArgumentException>(() => Gated("admit-anything/1", CompositionGrammarVersion6.Version));
        Assert.Contains("which this build does not retain", unknown.Message);

        ArgumentException early = Assert.Throws<ArgumentException>(() => Gated(ConnectorGates.LandableBodyRevision, CompositionGrammarVersion6.Version - 1));
        Assert.Contains("carries a gate, which no grammar before version 6 encodes", early.Message);
    }

    private static CompositionGrammar Gated(string gate, uint version) =>
        CompositionGrammar.Create(Revision5, version, 3, 32, 8,
            [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)])],
            [
                new Production(Star, [new ConnectorRule(0, 0, TransformRange.Origin(TransformKind.OrbitalElements), [])]),
                new Production(Planet,
                [
                    new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
                    new ConnectorRule(0, 0, TransformRange.Origin(TransformKind.OrbitalElements), []),
                    new ConnectorRule(1, 1, CompositionGrammarVersion6.SurfaceAnchor.Transform, [new CategoryChoice(Region, 1)], Gate: gate),
                ]),
                new Production(Atmosphere, []),
                new Production(Region, []),
            ]);
}
