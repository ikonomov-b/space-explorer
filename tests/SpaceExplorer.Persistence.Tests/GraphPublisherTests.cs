using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Persistence.Tests;

/// <summary>
/// A composition graph publishes and reloads through the protocol sets use: the record is verified before
/// the index row is committed, the graph reloads without composing anything, and a differing graph under
/// one pack identifier is a reproduction failure (decisions 0018, 0031, 0039).
/// </summary>
public sealed class GraphPublisherTests : IDisposable
{
    private static readonly CategoryRegistry Registry = CategoryRegistryRevision1.Registry;
    private static readonly CategoryRegistries Registries = CategoryRegistries.Supported;
    private static readonly CompositionGrammar Grammar = CompositionGrammarVersion1.Grammar;
    private static readonly PackId TemplatePack = PackId.Parse("aabbccdd00112233445566778899eeff");

    private static readonly TemplateVocabulary Vocabulary = TemplateVocabulary.Create(Registry, TemplatePack,
    [
        PrimitiveTemplate.Create(Registry, 1, "star", Star,
        [
            ParameterRange.Integer(1_600_000_000L, 60_000_000_000L),
            ParameterRange.Integer(25_600_000_000L, 512_000_000_000L),
            ParameterRange.Integer(8_000L, 60_000_000L),
            ParameterRange.Integer(2_800L, 10_000L),
            ParameterRange.Enum(2, 3, 4, 5, 6),
        ], [ConnectorDeclaration.Empty]),

        PrimitiveTemplate.Create(Registry, 2, "planet", Planet,
        [
            ParameterRange.Integer(100L, 200_000L),
            ParameterRange.Integer(MinimumLandableRadius, 2_560_000_000L),
            ParameterRange.Integer(3_000L, 45_000L),
            ParameterRange.BinaryTurn(int.MinValue, int.MaxValue),
            ParameterRange.BinaryTurn(-(1 << 30), 1 << 30),
            ParameterRange.Integer(720_000L, 172_800_000L),
            ParameterRange.BinaryTurn(int.MinValue, int.MaxValue),
            ParameterRange.Enum(0, 1, 2),
        ], [ConnectorDeclaration.Empty, ConnectorDeclaration.Empty]),

        PrimitiveTemplate.Create(Registry, 3, "atmosphere", Atmosphere,
        [
            ParameterRange.Enum(0, 1, 2),
            ParameterRange.Integer(0L, 5_000_000L),
            ParameterRange.Enum(0, 1, 2, 3, 4),
            ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)),
        ], []),
    ]);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "space-explorer-tests", Guid.NewGuid().ToString("n"));
    private readonly DataRoot _root;
    private readonly PrimitiveSet _set;

    public GraphPublisherTests()
    {
        _root = DataRoot.At(_directory);
        _set = SetGenerator.Generate(
            SetSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, 3, TemplatePack, Vocabulary.Hash, [new SetRequest(1, 2), new SetRequest(2, 4), new SetRequest(3, 2)], 8),
            Vocabulary,
            Registry);
    }

    [Fact]
    public void A_published_graph_reloads_without_composing_and_matches()
    {
        SetPublisher.Publish(_root, _set);
        CompositionGraph graph = Compose(1);
        GraphPublishResult result = GraphPublisher.Publish(_root, graph);
        CompositionGraph loaded = GraphLoader.Load(_root, graph.Pack, Registries, Grammar);

        Assert.False(result.AlreadyPublished);
        Assert.Equal(1, result.RecordsWritten);
        Assert.Equal(graph.Hash, loaded.Hash);
        Assert.Equal(graph.NodeCount, loaded.NodeCount);
        Assert.Equal(graph.Nodes.Select(node => node.Path), loaded.Nodes.Select(node => node.Path));
        Assert.True(File.Exists(_root.RecordPath(graph.Pack, graph.Hash)));

        GraphEntry entry = Assert.Single(GraphLoader.List(_root));
        Assert.Equal(graph.Pack, entry.Pack);
        Assert.Equal(_set.Manifest.Pack, entry.SourcePack);
        Assert.Equal(CompositionDomain.SolarSystem, entry.Domain);

        GraphPublishResult again = GraphPublisher.Publish(_root, graph);
        Assert.True(again.AlreadyPublished);
        Assert.Equal(0, again.RecordsWritten);
    }

    [Fact]
    public void A_graph_whose_set_is_not_published_is_refused_before_anything_is_written()
    {
        CompositionGraph graph = Compose(1);

        Assert.Throws<PackageNotFoundException>(() => GraphPublisher.Publish(_root, graph));
        Assert.False(File.Exists(_root.RecordPath(graph.Pack, graph.Hash)));
    }

    [Fact]
    public void A_differing_graph_under_an_existing_pack_is_refused()
    {
        SetPublisher.Publish(_root, _set);

        // The first seed whose star holds more than one body, so dropping one leaves a valid graph: the
        // same specification, hence the same pack, with different content, which is what a
        // non-deterministic composer would produce (decision 0039).
        CompositionGraph graph = Enumerable.Range(1, 50).Select(seed => Compose((ulong)seed)).First(candidate => candidate.Root.Children[0].Count > 1);
        GraphPublisher.Publish(_root, graph);

        var shorter = new GraphNode(graph.Root.Path, 0, graph.Root.Definition, null, [[graph.Root.Children[0][0]]]);
        CompositionGraph differing = CompositionGraph.Create(graph.Specification, _set, Registry, shorter);

        var exception = Assert.Throws<ReproductionFailureException>(() => GraphPublisher.Publish(_root, differing));
        Assert.Equal(graph.Hash, exception.Existing);
        Assert.Equal(differing.Hash, exception.Attempted);
        Assert.False(File.Exists(_root.RecordPath(graph.Pack, differing.Hash)));
    }

    [Fact]
    public void A_corrupt_graph_record_and_an_unknown_graph_fail_explicitly_on_load()
    {
        SetPublisher.Publish(_root, _set);
        CompositionGraph graph = Compose(1);

        Assert.Throws<PackageNotFoundException>(() => GraphLoader.Load(_root, graph.Pack, Registries, Grammar));

        GraphPublisher.Publish(_root, graph);
        string path = _root.RecordPath(graph.Pack, graph.Hash);
        byte[] bytes = File.ReadAllBytes(path);
        bytes[^1] ^= 0x01;
        File.WriteAllBytes(path, bytes);

        Assert.Throws<PackageIntegrityException>(() => GraphLoader.Load(_root, graph.Pack, Registries, Grammar));
    }

    [Fact]
    public void A_graph_pinning_another_grammar_version_or_hash_is_refused()
    {
        SetPublisher.Publish(_root, _set);
        CompositionGraph graph = Compose(1);
        GraphPublisher.Publish(_root, graph);

        Assert.Contains("version 2", Assert.Throws<CompatibilityException>(() => GraphLoader.Load(_root, graph.Pack, Registries, Rules(2, 3))).Message, StringComparison.Ordinal);
        Assert.Contains(Grammar.Hash.ToString(), Assert.Throws<CompatibilityException>(() => GraphLoader.Load(_root, graph.Pack, Registries, Rules(1, 2))).Message, StringComparison.Ordinal);
    }

    private CompositionGraph Compose(ulong seed) => CompositionGenerator.Generate(
        GraphSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, Grammar.Version, Grammar.Hash, seed, _set.Manifest.Pack, _set.Manifest.Hash, CompositionDomain.SolarSystem),
        _set,
        Grammar,
        Registry);

    /// <summary>A grammar of <paramref name="version"/> that is not this build's, for the compatibility cases.</summary>
    private static CompositionGrammar Rules(uint version, uint maxDepth) => CompositionGrammar.Create(Registry, version, maxDepth, 32, 4,
        [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)])],
        [
            new Production(Star, [new ConnectorRule(1, 2, TransformRange.Origin(TransformKind.OrbitalElements), [new CategoryChoice(Planet, 1)])]),
            new Production(Planet,
            [
                new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
                new ConnectorRule(0, 0, TransformRange.Origin(TransformKind.OrbitalElements), []),
            ]),
            new Production(Atmosphere, []),
        ]);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
