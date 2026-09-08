using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Description;

public class SystemDescriptionTests
{
    private static SystemDescription Describe(ulong seed, CompositionDomain domain = CompositionDomain.SolarSystem) =>
        SystemDescription.Derive(
            CompositionGenerator.Generate(CompositionFixture.Specification(seed, domain), CompositionFixture.Set, CompositionFixture.Grammar, CompositionFixture.Registry),
            CompositionFixture.Registry,
            SuitProfile.Version1);

    [Fact]
    public void The_fixture_description_is_a_frozen_vector()
    {
        // The recorded fixed-seed description of decision 0045, which continuous integration compares on
        // Linux and Windows. It moves when the wording, the derived values, or the composition move, each
        // of which is a version change of something the description names.
        SystemDescription description = Describe(9);

        Assert.Equal("36f83686c01ce4ff5e1516791baf8d0374dda519b58a6a08d9032ac38645ddcc", description.Hash.ToString());
    }

    [Fact]
    public void A_system_pinned_to_the_sun_and_the_earth_reads_back_as_earth()
    {
        // The whole path end to end against a world whose numbers are known: the templates pin the
        // published mass, radius, albedo, and orbit, and the description must recover Earth's surface
        // gravity, sea-level pressure, and black-body temperature (decisions 0037, 0045).
        SystemDescription description = EarthlikeFixture.Describe(planets: 1);
        BodyDescription earth = Assert.Single(description.Bodies);

        Assert.Equal(9_820, earth.GravityMillimetres);
        Assert.Equal(101_325, earth.PressurePascals);
        Assert.Equal(254, earth.TemperatureKelvin);
        Assert.Equal(Core.Derivation.PhysicalConstants.AstronomicalUnitMetres, earth.DistanceMetres);
        Assert.True(earth.LandingCandidate);

        Assert.Contains("star          class G, 5772 K, 1.000 Msun, 695700 km", description.Text, StringComparison.Ordinal);
        Assert.Contains("1.000 AU", description.Text, StringComparison.Ordinal);
        Assert.Contains("1.00 Me", description.Text, StringComparison.Ordinal);
        Assert.Contains("6371 km", description.Text, StringComparison.Ordinal);
        Assert.Contains("9.82 m/s^2", description.Text, StringComparison.Ordinal);
        Assert.Contains("101.3 kPa", description.Text, StringComparison.Ordinal);
        Assert.Contains("254 K", description.Text, StringComparison.Ordinal);
        Assert.Contains("landing candidate", description.Text, StringComparison.Ordinal);
        Assert.EndsWith("totals        1 planet, 0 moons, 0 barycentres; 1 landing candidate; no life\n", description.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_description_states_the_star_then_every_body_in_orbit_order()
    {
        SystemDescription description = Describe(9);
        string[] lines = description.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal($"system description {SystemDescription.Version}", lines[0]);
        Assert.StartsWith("star          class ", lines[3], StringComparison.Ordinal);
        Assert.EndsWith("\n", description.Text, StringComparison.Ordinal);

        // Numbering follows the graph: a star's children are 1..n and their children are n.1, n.2.
        Assert.Equal(BodyRole.Star, description.Star.Role);
        Assert.Equal("1", description.Bodies[0].Number);
        Assert.Equal(description.Bodies.Count, description.PlanetCount + description.MoonCount + description.BarycentreCount);
        Assert.All(description.Bodies, body => Assert.StartsWith(description.Star.Path, body.Path, StringComparison.Ordinal));
    }

    [Fact]
    public void A_moon_takes_its_planets_distance_from_the_star()
    {
        // The distance a body is described at is the semi-major axis of the orbit hanging from the star,
        // so every body of one branch shares it (decision 0037's two-body hierarchy).
        SystemDescription description = Describe(9);

        foreach (BodyDescription moon in description.Bodies.Where(body => body.Role == BodyRole.Moon))
        {
            string planetNumber = moon.Number[..moon.Number.IndexOf('.', StringComparison.Ordinal)];
            BodyDescription planet = description.Bodies.Single(body => body.Number == planetNumber);
            Assert.Equal(planet.DistanceMetres, moon.DistanceMetres);
        }
    }

    [Fact]
    public void The_landing_candidate_flag_follows_the_suit_profile_and_the_minimum_radius()
    {
        SystemDescription description = Describe(9);

        foreach (BodyDescription body in description.Bodies.Where(body => body.Role != BodyRole.Barycentre))
        {
            bool inside = SuitProfile.Version1.Refusals(body.GravityMillimetres, body.PressurePascals, body.TemperatureKelvin).Count == 0;
            bool landable = body.RadiusUnits >= CategoryRegistryRevision1.MinimumLandableRadius;

            Assert.Equal(inside && landable, body.LandingCandidate);
            Assert.Equal(body.LandingCandidate, body.Refusals.Count == 0);
        }
    }

    [Fact]
    public void No_body_bears_life_while_the_registry_has_no_life_category()
    {
        // Life follows the presence of a life primitive, and registry revision 1 defines no such
        // category, so the starter tier's rule holds by construction (decisions 0044, 0047).
        Assert.Null(CompositionFixture.Registry.TryFindByLabel("life"));
        Assert.Equal(0, Describe(9).LifeBearingCount);
    }

    [Fact]
    public void A_graph_that_is_not_a_solar_system_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Describe(4, CompositionDomain.Artifact));
    }

    [Fact]
    public void The_suit_profile_hash_is_frozen_and_its_limits_are_fractions_of_standard_gravity()
    {
        SuitProfile suit = SuitProfile.Version1;

        Assert.Equal(981, suit.MinimumGravity);
        Assert.Equal(14_711, suit.MaximumGravity);
        Assert.Equal(
            "02f7a6864524cfd7a3c0e9f308e0f6f78216f43f27409299d7ed4a99dd04772b",
            suit.Hash.ToString());

        Assert.Equal(["gravity", "pressure", "temperature"], suit.Refusals(0, suit.MaximumPressure + 1, suit.MaximumTemperature + 1));
        Assert.Empty(suit.Refusals(suit.MinimumGravity, 0, suit.MinimumTemperature));
        Assert.Throws<ArgumentException>(() => SuitProfile.Create(1, 100, 10, 0, 100, 200));
    }
}
