using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Description;

/// <summary>
/// The stored destination of decision 0053: the two levers and the pins name its pack before anything is
/// composed, and the record holds the result of the retry beside them, refusing a result the levers do
/// not derive.
/// </summary>
public class DestinationRecordTests
{
    private static readonly CompositionGrammar Grammar = ZoneFixture.Grammar(moonsInherit: false, moons: 0);

    private static DestinationSpecification Specification(DistanceTier tier, ulong seed, SuitProfile? suit = null) =>
        DestinationSpecification.For(tier, seed, ZoneFixture.Set, Grammar, ZoneFixture.Registry, suit ?? SuitProfile.Version1);

    [Fact]
    public void The_levers_and_the_pins_name_the_pack_without_composing_anything()
    {
        // What a lookup depends on: the same levers over the same content name the same pack on any
        // machine, and anything that could change which system satisfies the tier changes the pack.
        Assert.Equal(Specification(DistanceTier.Starter, 3).PackId, Specification(DistanceTier.Starter, 3).PackId);
        Assert.NotEqual(Specification(DistanceTier.Starter, 3).PackId, Specification(DistanceTier.Starter, 4).PackId);
        Assert.NotEqual(Specification(DistanceTier.Starter, 3).PackId, Specification(DistanceTier.Second, 3).PackId);

        SuitProfile thinner = SuitProfile.Create(2, 1_000L, 15_000L, 2_000_000L, 120_000L, 400_000L);
        Assert.NotEqual(Specification(DistanceTier.Starter, 3).PackId, Specification(DistanceTier.Starter, 3, thinner).PackId);
    }

    [Fact]
    public void A_record_holds_the_retry_result_and_round_trips()
    {
        Destination destination = DestinationComposer.Compose(DistanceTier.Starter, 3, ZoneFixture.Set, Grammar, ZoneFixture.Registry, SuitProfile.Version1);
        DestinationRecord record = DestinationRecord.Of(destination, ZoneFixture.Set, Grammar, ZoneFixture.Registry, SuitProfile.Version1);
        DestinationRecord decoded = DestinationRecord.Decode(record.Bytes);

        Assert.Equal(Specification(DistanceTier.Starter, 3).PackId, record.Pack);
        Assert.Equal(destination.Graph.Pack, record.GraphPack);
        Assert.Equal(destination.Graph.Hash, record.GraphHash);
        Assert.Equal(destination.Attempt, record.Attempt);
        Assert.Equal(record.Hash, decoded.Hash);
        Assert.Equal(record.Bytes, decoded.Bytes);
        Assert.Equal(record.CompositionSeed, decoded.CompositionSeed);
    }

    [Fact]
    public void A_composition_seed_the_levers_do_not_derive_is_refused()
    {
        // The record cannot claim a system the levers would never draw: the seed of an attempt follows from
        // the two lever values alone, so a wrong one is a broken record rather than another destination.
        Destination destination = DestinationComposer.Compose(DistanceTier.Starter, 3, ZoneFixture.Set, Grammar, ZoneFixture.Registry, SuitProfile.Version1);
        DestinationSpecification specification = Specification(DistanceTier.Starter, 3);

        Assert.Throws<ArgumentException>(() => DestinationRecord.Create(specification, destination.Attempt, destination.CompositionSeed + 1, destination.Graph.Pack, destination.Graph.Hash));
        Assert.Throws<ArgumentException>(() => DestinationRecord.Create(specification, destination.Attempt + 1, destination.CompositionSeed, destination.Graph.Pack, destination.Graph.Hash));
        Assert.Throws<ArgumentException>(() => DestinationRecord.Create(specification, DestinationComposer.MaxAttempts, DestinationComposer.CompositionSeed(DistanceTier.Starter, 3, DestinationComposer.MaxAttempts), destination.Graph.Pack, destination.Graph.Hash));

        DestinationRecord record = DestinationRecord.Of(destination, ZoneFixture.Set, Grammar, ZoneFixture.Registry, SuitProfile.Version1);
        Assert.Throws<FormatException>(() => DestinationRecord.Decode([.. record.Bytes, 0x00]));
    }
}
