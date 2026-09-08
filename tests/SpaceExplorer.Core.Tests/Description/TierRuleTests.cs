using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Description;

public class TierRuleTests
{
    private static SystemDescription Describe(ulong seed) =>
        SystemDescription.Derive(
            CompositionGenerator.Generate(CompositionFixture.Specification(seed, CompositionDomain.SolarSystem), CompositionFixture.Set, CompositionFixture.Grammar, CompositionFixture.Registry),
            CompositionFixture.Registry,
            SuitProfile.Version1);

    [Fact]
    public void The_starter_tier_wants_exactly_one_explorable_planet_and_no_life()
    {
        // One Earth passes; two Earths are one planet too many; no planet at all is one too few.
        Assert.Empty(TierRules.Check(EarthlikeFixture.Describe(planets: 1), DistanceTier.Starter));

        IReadOnlyList<string> two = TierRules.Check(EarthlikeFixture.Describe(planets: 2), DistanceTier.Starter);
        Assert.Contains("this system has 2", Assert.Single(two), StringComparison.Ordinal);
    }

    [Fact]
    public void A_landable_moon_is_not_counted_as_an_explorable_planet()
    {
        // Decision 0044 counts moons separately from planets. This system's one planet is a gas giant no
        // suit can stand on and its one moon is an Earth, so it has a landing candidate and no explorable
        // planet, and the starter tier refuses it.
        SystemDescription description = EarthlikeFixture.GiantWithLandableMoon();

        Assert.Equal(1, description.PlanetCount);
        Assert.Equal(1, description.MoonCount);
        Assert.Equal(1, description.LandingCandidateCount);
        Assert.Equal(BodyRole.Moon, Assert.Single(description.Bodies, body => body.LandingCandidate).Role);

        Assert.Contains("this system has 0", Assert.Single(TierRules.Check(description, DistanceTier.Starter)), StringComparison.Ordinal);
    }

    [Fact]
    public void A_tier_above_the_starter_names_the_rules_a_system_of_registry_revision_1_breaks()
    {
        // Nothing this build composes can bear life, so the second tier's rules always fail and the
        // failure names each one; that is what decision 0047 means by an iteration judged on the starter
        // tier and the structural rules alone.
        IReadOnlyList<string> failures = TierRules.Check(Describe(9), DistanceTier.Second);

        Assert.Contains(failures, failure => failure.Contains("life-bearing planet", StringComparison.Ordinal));
    }

    [Fact]
    public void Tier_labels_round_trip()
    {
        Assert.Equal(DistanceTier.Starter, TierRules.TryParse("starter"));
        Assert.Equal(DistanceTier.Second, TierRules.TryParse("second"));
        Assert.Null(TierRules.TryParse("third"));
        Assert.Equal(["starter", "second"], TierRules.Labels);
    }
}
