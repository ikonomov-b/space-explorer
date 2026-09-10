using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Registry;

/// <summary>
/// Grammar version 3: orbit bands scaled to the star, a tag demanded per band, and that tag carried down
/// to a child that shares its parent's distance (decision 0051).
/// </summary>
public class GrammarVersion3Tests
{
    [Fact]
    public void Version_3_round_trips_and_leaves_the_earlier_versions_byte_identical()
    {
        CompositionGrammar version3 = CompositionGrammarVersion3.Grammar;

        Assert.Equal(version3.Hash, CompositionGrammar.Decode(version3.Bytes, CategoryRegistryRevision2.Registry, 3).Hash);
        Assert.Throws<FormatException>(() => CompositionGrammar.Decode(version3.Bytes, CategoryRegistryRevision2.Registry, 2));
        Assert.Equal(5, CompositionGrammars.Supported.Grammars.Count);
        Assert.Equal(3u, CompositionGrammars.Supported.Newest(2)!.Version);
        Assert.Equal(1u, CompositionGrammars.Supported.Newest(1)!.Version);
    }

    [Fact]
    public void A_stars_bands_move_with_its_luminosity()
    {
        ConnectorRule orbit = CompositionGrammarVersion3.Grammar.TryFindProduction(Star)!.ConnectorRules[0];
        Assert.Equal(DerivationRules.OrbitScaleRevision, orbit.BandScale);

        // The Sun's own radius and temperature give back the luminosity the unit is defined from, so its
        // scale is one and its innermost band begins where the grammar says.
        long solar = DerivationRules.OrbitScale(695_700_000L << PlanetFractionBits, 5_772);
        Assert.InRange(solar, DerivationRules.OrbitScaleUnit - 1, DerivationRules.OrbitScaleUnit);
        Assert.InRange(orbit.OrbitBand(0, solar).Min, (PhysicalConstants.AstronomicalUnitMetres * 3 / 10) - 100_000_000, PhysicalConstants.AstronomicalUnitMetres * 3 / 10);

        // A star of half the radius has a quarter of the luminosity, and its bands sit at half the
        // distance, which is what holds a band at one temperature whatever the star.
        long quarter = DerivationRules.OrbitScale(695_700_000L << (PlanetFractionBits - 1), 5_772);
        Assert.InRange(quarter, (solar / 2) - 1, (solar / 2) + 1);

        long half = orbit.OrbitBand(0, solar).Min / 2;
        Assert.InRange(orbit.OrbitBand(0, quarter).Min, half - (half / 200), half + (half / 200));

        // Whatever the scale, a band stays inside the range the rule declares.
        foreach (long scale in new long[] { 1, 34 * DerivationRules.OrbitScaleUnit })
        {
            for (int band = 0; band < 8; band++)
            {
                (long low, long high) = orbit.OrbitBand(band, scale);
                Assert.InRange(low, orbit.Transform.Bounds[0].Min, orbit.Transform.Bounds[0].Max);
                Assert.InRange(high, orbit.Transform.Bounds[0].Min, orbit.Transform.Bounds[0].Max);
            }
        }
    }

    [Fact]
    public void Every_band_demands_the_zone_its_temperature_implies()
    {
        ConnectorRule orbit = CompositionGrammarVersion3.Grammar.TryFindProduction(Star)!.ConnectorRules[0];

        Assert.Equal((int)orbit.MaxCount, orbit.BandTags.Count);
        Assert.Equal("zone-hot", orbit.BandTag(0));
        Assert.Equal("zone-cold", orbit.BandTag(7));

        // A moon and a barycentre's pair share their parent's distance, so they inherit its zone rather
        // than taking one of their own.
        Assert.True(CompositionGrammarVersion3.Grammar.TryFindProduction(Planet)!.ConnectorRules[1].InheritsBandTag);
        Assert.True(CompositionGrammarVersion3.Grammar.TryFindProduction(Barycentre)!.ConnectorRules[0].InheritsBandTag);
    }

    [Fact]
    public void A_body_takes_only_the_band_whose_zone_it_provides()
    {
        // Two bodies, two bands, opposite zones: the hot body can only take the first band and the cold
        // body only the second, so the order is decided by the tags rather than by the draw.
        CompositionGraph graph = ZoneFixture.Compose(moonsInherit: false, moons: 0);
        GraphNode[] planets = [.. graph.Root.Children[0]];

        Assert.Equal(2, planets.Length);
        Assert.Equal(ZoneFixture.HotBody, ZoneFixture.TemplateOf(planets[0]));
        Assert.Equal(ZoneFixture.ColdBody, ZoneFixture.TemplateOf(planets[1]));
    }

    [Fact]
    public void An_inheriting_connector_hands_its_own_zone_to_its_children()
    {
        // With inheritance the hot planet's moon must also suit the hot zone, and the cold planet's the
        // cold one; without it, either body may take either moon slot.
        CompositionGraph inheriting = ZoneFixture.ComposeWithMoons(moonsInherit: true);

        foreach (GraphNode planet in inheriting.Root.Children[0])
        {
            GraphNode moon = Assert.Single(planet.Children[1]);
            Assert.Equal(ZoneFixture.TemplateOf(planet), ZoneFixture.TemplateOf(moon));
        }

        // Without inheritance a moon is drawn from both bodies, so at least one lands beside a planet of
        // the other zone; that difference is the rule doing the work.
        CompositionGraph free = ZoneFixture.ComposeWithMoons(moonsInherit: false);
        Assert.Contains(
            free.Root.Children[0],
            planet => ZoneFixture.TemplateOf(planet) != ZoneFixture.TemplateOf(planet.Children[1][0]));
    }

    [Fact]
    public void A_band_no_body_suits_fails_explicitly_rather_than_placing_a_wrong_one()
    {
        // A third band demanding a zone no body provides cannot be filled, and the star's rule requires
        // every one of its bands, so the run fails naming what it could not find.
        var exception = Assert.Throws<GenerationException>(() =>
            ZoneFixture.Compose(moonsInherit: true, zones: ["zone-hot", "zone-cold", "zone-void"]));

        Assert.Contains("zone-void", exception.Message, StringComparison.Ordinal);
    }
}
