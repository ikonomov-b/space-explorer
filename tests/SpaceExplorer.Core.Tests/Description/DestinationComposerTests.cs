using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Description;

/// <summary>
/// The two destination levers, a distance tier and a seed, composing the system they name (decisions
/// 0043, 0052). The fixture's two bodies straddle the suit profile's warm limit, so whether a system has
/// the one explorable planet the starter tier wants depends on where in its band each lands, which is
/// what gives the retry something to do.
/// </summary>
public class DestinationComposerTests
{
    private static readonly CompositionGrammar Grammar = ZoneFixture.Grammar(moonsInherit: false, moons: 0);

    private static Destination Compose(DistanceTier tier, ulong seed) =>
        DestinationComposer.Compose(tier, seed, ZoneFixture.Set, Grammar, ZoneFixture.Registry, SuitProfile.Version1);

    [Fact]
    public void One_pair_of_lever_values_names_one_destination()
    {
        Destination first = Compose(DistanceTier.Starter, 3);
        Destination again = Compose(DistanceTier.Starter, 3);

        Assert.Equal(first.CompositionSeed, again.CompositionSeed);
        Assert.Equal(first.Graph.Hash, again.Graph.Hash);
        Assert.Equal(first.Description.Hash, again.Description.Hash);
        Assert.NotEqual(first.Graph.Hash, Compose(DistanceTier.Starter, 4).Graph.Hash);
    }

    [Fact]
    public void What_comes_back_satisfies_the_tier_that_asked_for_it()
    {
        // Whatever it took to find, the destination the levers name obeys its tier's rules; that is the
        // whole of what the tier lever does until a grammar can be gated on it.
        for (ulong seed = 1; seed <= 12; seed++)
        {
            Destination destination = Compose(DistanceTier.Starter, seed);

            Assert.Empty(TierRules.Check(destination.Description, DistanceTier.Starter));
            Assert.InRange(destination.Attempt, 0u, DestinationComposer.MaxAttempts - 1);
        }
    }

    [Fact]
    public void The_seed_of_each_attempt_follows_from_the_levers_alone()
    {
        // Attempts walk a sequence the two levers fix, so a destination is reproducible from them without
        // recording which attempt succeeded, and two tiers of one seed do not collide.
        Assert.Equal(
            DestinationComposer.CompositionSeed(DistanceTier.Starter, 9, 0),
            DestinationComposer.CompositionSeed(DistanceTier.Starter, 9, 0));
        Assert.NotEqual(
            DestinationComposer.CompositionSeed(DistanceTier.Starter, 9, 0),
            DestinationComposer.CompositionSeed(DistanceTier.Starter, 9, 1));
        Assert.NotEqual(
            DestinationComposer.CompositionSeed(DistanceTier.Starter, 9, 0),
            DestinationComposer.CompositionSeed(DistanceTier.Second, 9, 0));
    }

    [Fact]
    public void A_tier_nothing_can_satisfy_is_rejected_explicitly_rather_than_returning_a_wrong_system()
    {
        // No registry revision defines a life category, so no system can bear life and the second tier
        // can never be met; the levers say so instead of handing back a system that breaks the rule.
        var exception = Assert.Throws<GenerationException>(() => Compose(DistanceTier.Second, 1));

        Assert.Contains("within 64 attempts", exception.Message, StringComparison.Ordinal);
        Assert.Contains("life-bearing planet", exception.Message, StringComparison.Ordinal);
    }
}
