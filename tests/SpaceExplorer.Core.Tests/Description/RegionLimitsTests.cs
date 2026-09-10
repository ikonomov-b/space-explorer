using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using Xunit;

namespace SpaceExplorer.Core.Tests.Description;

/// <summary>
/// The region limits: the fourth number a landing candidate turns on, held in a record a destination
/// pins, where it was a code constant outside every record (decision 0059, review finding 51).
/// </summary>
public class RegionLimitsTests
{
    [Fact]
    public void Version_1_is_decision_0041s_minimum_and_round_trips()
    {
        RegionLimits limits = RegionLimits.Version1;

        Assert.Equal(CategoryRegistryRevision1.MinimumLandableRadius, limits.MinimumLandableRadiusUnits);
        Assert.Equal(524_288L, limits.MinimumLandableRadiusUnits >> CategoryRegistryRevision1.PlanetFractionBits);
        Assert.Equal(limits.Hash, RegionLimits.Decode(limits.Bytes).Hash);
        Assert.Throws<FormatException>(() => RegionLimits.Decode([.. limits.Bytes, 0x00]));
    }

    [Fact]
    public void A_body_is_admitted_by_its_radius_and_the_limit_moves_the_hash()
    {
        RegionLimits limits = RegionLimits.Version1;
        long minimum = limits.MinimumLandableRadiusUnits;

        Assert.True(limits.AdmitsRegion(minimum));
        Assert.False(limits.AdmitsRegion(minimum - 1));

        // Tuning the limit is a different record, which is the whole point: a stored verdict names the
        // limits that gave it.
        RegionLimits smaller = RegionLimits.Create(2, minimum / 2);
        Assert.True(smaller.AdmitsRegion(minimum - 1));
        Assert.NotEqual(limits.Hash, smaller.Hash);
    }

    [Fact]
    public void A_destination_pins_the_limits_its_landing_verdict_used()
    {
        DestinationSpecification under1 = Specification(RegionLimits.Version1);
        DestinationSpecification underOther = Specification(RegionLimits.Create(2, CategoryRegistryRevision1.MinimumLandableRadius / 2));

        Assert.Equal(RegionLimits.Version1.Hash, under1.RegionLimitsHash);
        Assert.NotEqual(under1.PackId, underOther.PackId);
    }

    [Fact]
    public void A_nonsense_limit_is_refused()
    {
        Assert.Throws<ArgumentException>(() => RegionLimits.Create(0, 1_000));
        Assert.Throws<ArgumentException>(() => RegionLimits.Create(1, 0));
        Assert.Throws<ArgumentException>(() => RegionLimits.Create(1, -1));
    }

    private static DestinationSpecification Specification(RegionLimits regions) => DestinationSpecification.For(
        DistanceTier.Starter,
        3,
        Generation.ZoneFixture.Set,
        Generation.ZoneFixture.Grammar(moonsInherit: false, moons: 0),
        Generation.ZoneFixture.Registry,
        SuitProfile.Version1,
        TierProfile.Version1,
        regions);
}
