using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Description;

/// <summary>
/// The tier profile: what a tier demands is a record with a version and a hash, which a destination pins
/// so that a stored verdict can be re-checked against the rules that gave it (decision 0058).
/// </summary>
public class TierProfileTests
{
    private static readonly CompositionGrammar Grammar = ZoneFixture.Grammar(moonsInherit: false, moons: 0);

    [Fact]
    public void Version_1_is_what_decision_0044_states_and_round_trips()
    {
        TierProfile profile = TierProfile.Version1;
        TierDemand starter = profile.For(DistanceTier.Starter);
        TierDemand second = profile.For(DistanceTier.Second);

        Assert.Equal((1u, 1u), (starter.MinimumExplorable, starter.MaximumExplorable));
        Assert.Equal((0u, 0u), (starter.MinimumLifeBearing, starter.MaximumLifeBearing));
        Assert.Equal(2u, second.MinimumExplorable);
        Assert.Equal(1u, second.MinimumLifeBearing);
        Assert.Equal(["F", "G", "K", "M"], second.LifeBearingClasses);

        Assert.Equal(profile.Hash, TierProfile.Decode(profile.Bytes).Hash);
        Assert.Throws<FormatException>(() => TierProfile.Decode([.. profile.Bytes, 0x00]));
    }

    [Fact]
    public void A_tuned_profile_is_a_different_record_and_judges_differently()
    {
        // The whole point of the record: change what a tier demands and both the hash and the verdict
        // move together, so a destination pinning the old hash is not silently reinterpreted.
        TierProfile lenient = TierProfile.Create(2,
        [
            new TierDemand(1, 3, 0, 0, []),
            new TierDemand(2, TierDemand.Unbounded, 1, TierDemand.Unbounded, ["F", "G", "K", "M"]),
        ]);

        Assert.NotEqual(TierProfile.Version1.Hash, lenient.Hash);

        SystemDescription three = EarthlikeFixture.Describe(planets: 3);
        Assert.NotEmpty(TierRules.Check(three, DistanceTier.Starter, TierProfile.Version1));
        Assert.Empty(TierRules.Check(three, DistanceTier.Starter, lenient));
    }

    [Fact]
    public void A_destination_pins_the_profile_its_verdict_came_from()
    {
        DestinationSpecification under1 = DestinationSpecification.For(
            DistanceTier.Starter, 3, ZoneFixture.Set, Grammar, ZoneFixture.Registry, SuitProfile.Version1, TierProfile.Version1, RegionLimits.Version1);
        DestinationSpecification underOther = DestinationSpecification.For(
            DistanceTier.Starter, 3, ZoneFixture.Set, Grammar, ZoneFixture.Registry, SuitProfile.Version1,
            TierProfile.Create(2, [new TierDemand(1, 2, 0, 0, []), new TierDemand(2, TierDemand.Unbounded, 1, TierDemand.Unbounded, ["G"])]),
            RegionLimits.Version1);

        Assert.Equal(TierProfile.Version1.Hash, under1.TierProfileHash);
        Assert.NotEqual(under1.PackId, underOther.PackId);
    }

    [Fact]
    public void A_profile_that_does_not_cover_this_builds_tiers_or_contradicts_itself_is_refused()
    {
        Assert.Throws<ArgumentException>(() => TierProfile.Create(1, [new TierDemand(1, 1, 0, 0, [])]));
        Assert.Throws<ArgumentException>(() => TierProfile.Create(0, TierProfile.Version1.Demands));
        Assert.Throws<ArgumentException>(() => TierProfile.Create(1,
            [new TierDemand(3, 1, 0, 0, []), new TierDemand(2, TierDemand.Unbounded, 1, TierDemand.Unbounded, [])]));
    }
}
