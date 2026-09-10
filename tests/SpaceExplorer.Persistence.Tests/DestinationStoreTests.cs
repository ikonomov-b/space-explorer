using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;

namespace SpaceExplorer.Persistence.Tests;

/// <summary>
/// A destination publishes and is found again by its levers alone, through the protocol sets and graphs
/// use: the graph it names must be published with the hash the record pins, and a differing record under
/// one pack identifier is a reproduction failure (decisions 0039, 0042, 0053).
/// </summary>
public sealed class DestinationStoreTests : IDisposable
{
    private static readonly CategoryRegistry Registry = ZoneFixture.Registry;
    private static readonly CategoryRegistries Registries = CategoryRegistries.Supported;
    private static readonly CompositionGrammar Grammar = ZoneFixture.Grammar(moonsInherit: false, moons: 0);
    private static readonly SuitProfile Suit = SuitProfile.Version1;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "space-explorer-tests", Guid.NewGuid().ToString("n"));
    private readonly DataRoot _root;
    private readonly PrimitiveSet _set;

    public DestinationStoreTests()
    {
        _root = DataRoot.At(_directory);
        _set = ZoneFixture.Set;
    }

    [Fact]
    public void A_published_destination_is_found_by_its_levers_and_reloads_without_composing()
    {
        SetPublisher.Publish(_root, _set);
        Destination destination = Compose(DistanceTier.Starter, 21);
        GraphPublisher.Publish(_root, destination.Graph);

        DestinationRecord record = DestinationRecord.Of(destination, _set, Grammar, Registry, Suit);
        DestinationPublishResult result = DestinationStore.Publish(_root, record);

        Assert.False(result.AlreadyPublished);
        Assert.Equal(1, result.RecordsWritten);
        Assert.True(File.Exists(_root.RecordPath(record.Pack, record.Hash)));

        // The levers alone find it: no composing, no search, one address.
        DestinationRecord found = Assert.IsType<DestinationRecord>(DestinationStore.Find(_root, DestinationSpecification.For(DistanceTier.Starter, 21, _set, Grammar, Registry, Suit)));
        Assert.Equal(record.Hash, found.Hash);
        Assert.Equal(destination.Graph.Pack, found.GraphPack);
        Assert.Equal(destination.Attempt, found.Attempt);

        // And what it points at still describes the same system, derived rather than stored.
        CompositionGraph loaded = GraphLoader.Load(_root, found.GraphPack, Registries, Grammar);
        Assert.Equal(destination.Graph.Hash, loaded.Hash);
        Assert.Equal(destination.Description.Hash, SystemDescription.Derive(loaded, Registry, Suit).Hash);

        DestinationEntry entry = Assert.Single(DestinationStore.List(_root));
        Assert.Equal(record.Pack, entry.Pack);
        Assert.Equal(DistanceTier.Starter, entry.Tier);
        Assert.Equal(21ul, entry.Seed);

        Assert.True(DestinationStore.Publish(_root, record).AlreadyPublished);
    }

    [Fact]
    public void Levers_no_destination_was_published_for_find_nothing()
    {
        SetPublisher.Publish(_root, _set);

        Assert.Null(DestinationStore.Find(_root, DestinationSpecification.For(DistanceTier.Starter, 21, _set, Grammar, Registry, Suit)));
        Assert.Throws<PackageNotFoundException>(() => DestinationStore.Load(_root, DestinationSpecification.For(DistanceTier.Starter, 21, _set, Grammar, Registry, Suit).PackId));
    }

    [Fact]
    public void A_destination_whose_graph_is_absent_or_altered_is_refused_before_anything_is_written()
    {
        SetPublisher.Publish(_root, _set);
        Destination destination = Compose(DistanceTier.Starter, 21);
        DestinationRecord record = DestinationRecord.Of(destination, _set, Grammar, Registry, Suit);

        Assert.Throws<PackageNotFoundException>(() => DestinationStore.Publish(_root, record));
        Assert.False(File.Exists(_root.RecordPath(record.Pack, record.Hash)));

        GraphPublisher.Publish(_root, destination.Graph);
        DestinationRecord altered = DestinationRecord.Create(record.Specification, record.Attempt, record.CompositionSeed, record.GraphPack, ContentHash.Of([1, 2, 3]));

        Assert.Throws<CompatibilityException>(() => DestinationStore.Publish(_root, altered));
        Assert.False(File.Exists(_root.RecordPath(altered.Pack, altered.Hash)));
    }

    [Fact]
    public void A_differing_record_under_an_existing_destination_pack_is_refused()
    {
        // One pack identifier, two results: what a composer that drew a different attempt for the same
        // levers would produce, which the store refuses rather than overwrite (decision 0039).
        SetPublisher.Publish(_root, _set);
        Destination destination = Compose(DistanceTier.Starter, 21);
        GraphPublisher.Publish(_root, destination.Graph);
        DestinationRecord record = DestinationRecord.Of(destination, _set, Grammar, Registry, Suit);
        DestinationStore.Publish(_root, record);

        uint other = destination.Attempt + 1;
        DestinationRecord differing = DestinationRecord.Create(
            record.Specification, other, DestinationComposer.CompositionSeed(DistanceTier.Starter, 21, other), record.GraphPack, record.GraphHash);

        Assert.Equal(record.Pack, differing.Pack);
        ReproductionFailureException failure = Assert.Throws<ReproductionFailureException>(() => DestinationStore.Publish(_root, differing));
        Assert.Equal(record.Hash, failure.Existing);
        Assert.Equal(differing.Hash, failure.Attempted);
    }

    [Fact]
    public void A_corrupt_destination_record_fails_explicitly_on_load()
    {
        SetPublisher.Publish(_root, _set);
        Destination destination = Compose(DistanceTier.Starter, 21);
        GraphPublisher.Publish(_root, destination.Graph);
        DestinationRecord record = DestinationRecord.Of(destination, _set, Grammar, Registry, Suit);
        DestinationStore.Publish(_root, record);

        string path = _root.RecordPath(record.Pack, record.Hash);
        byte[] bytes = File.ReadAllBytes(path);
        bytes[^1] ^= 0x01;
        File.WriteAllBytes(path, bytes);

        Assert.Throws<PackageIntegrityException>(() => DestinationStore.Load(_root, record.Pack));
    }

    private Destination Compose(DistanceTier tier, ulong seed) => DestinationComposer.Compose(tier, seed, _set, Grammar, Registry, Suit);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
