using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Generation;

public class CompositionGeneratorTests
{
    private static CompositionGraph Compose(ulong seed, CompositionDomain domain) =>
        CompositionGenerator.Generate(CompositionFixture.Specification(seed, domain), CompositionFixture.Set, CompositionFixture.Grammar, CompositionFixture.Registry);

    [Fact]
    public void The_same_specification_composes_the_same_graph()
    {
        CompositionGraph first = Compose(9, CompositionDomain.SolarSystem);
        CompositionGraph second = Compose(9, CompositionDomain.SolarSystem);

        Assert.Equal(first.Hash, second.Hash);
        Assert.Equal(first.Pack, second.Pack);
        Assert.NotEqual(first.Hash, Compose(10, CompositionDomain.SolarSystem).Hash);
    }

    [Fact]
    public void The_fixture_graph_hashes_are_frozen()
    {
        // Freezes this build's composition for one seed per domain, so an unrecorded change to the draw
        // order, the grammar, the record layout, or the instance paths is caught. Continuous integration
        // runs this on Linux and Windows, which is what makes it a cross-platform vector (decision 0047).
        Assert.Equal("44cb2255b6aa8d38bf81360786417dfc3fbbed6872242b4f4d95a2761fde03d2", Compose(9, CompositionDomain.SolarSystem).Hash.ToString());
        Assert.Equal("f03a197dbe3f6713779af11abc2b028ec560947f83bb6da632c329ced3cde322", Compose(4, CompositionDomain.Artifact).Hash.ToString());
    }

    [Fact]
    public void Every_instance_has_a_distinct_path_derived_from_its_place_in_the_graph()
    {
        CompositionGraph graph = Compose(9, CompositionDomain.SolarSystem);
        GraphNode[] nodes = [.. graph.Nodes];

        Assert.Equal("solar-system", graph.Root.Path);
        Assert.Equal(nodes.Length, nodes.Select(node => node.Path).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(graph.NodeCount, nodes.Length);
        Assert.All(nodes, node => StreamPath.Validate(node.Path));

        GraphNode firstPlanet = graph.Root.Children[0][0];
        Assert.Equal("solar-system/orbit/0", firstPlanet.Path);
        Assert.Equal(1u, firstPlanet.Depth);
    }

    [Fact]
    public void The_graph_stays_inside_the_grammars_depth_and_instance_bounds()
    {
        CompositionGraph graph = Compose(9, CompositionDomain.SolarSystem);

        Assert.InRange(graph.NodeCount, 2, (int)CompositionFixture.Grammar.MaxNodes);
        Assert.InRange(graph.Depth, 1u, CompositionFixture.Grammar.MaxDepth);
        Assert.All(graph.Nodes, node => Assert.True(node.Depth <= CompositionFixture.Grammar.MaxDepth));
    }

    [Fact]
    public void Every_child_carries_the_transform_its_connector_admits_and_stays_in_its_grammar_range()
    {
        CompositionGraph graph = Compose(9, CompositionDomain.SolarSystem);

        Assert.Null(graph.Root.Transform);
        foreach (GraphNode node in graph.Nodes)
        {
            CategoryDefinition schema = CompositionFixture.Registry.Find(node.Definition.Category);
            Production production = CompositionFixture.Grammar.TryFindProduction(node.Definition.Category)!;
            for (int index = 0; index < schema.Connectors.Count; index++)
            {
                TransformRange range = production.ConnectorRules[index].Transform;
                foreach (GraphNode child in node.Children[index])
                {
                    Assert.Equal(schema.Connectors[index].Transform, child.Transform!.Kind);
                    for (int component = 0; component < range.Bounds.Count; component++)
                    {
                        Assert.InRange(child.Transform.Components[component], range.Bounds[component].Min, range.Bounds[component].Max);
                    }
                }
            }
        }
    }

    [Fact]
    public void Only_a_definition_that_provides_the_required_tag_is_attached()
    {
        // Every part requires "plug" of its children and only the plug part provides it. A root is drawn
        // with nothing required of it, so socket parts must appear there and nowhere else: that
        // difference is the tag rule at work rather than an absence in the set (decision 0031).
        var roots = new HashSet<uint>();
        var attached = new HashSet<uint>();
        for (ulong seed = 1; seed <= 40; seed++)
        {
            CompositionGraph graph = Compose(seed, CompositionDomain.Artifact);
            roots.Add(CompositionFixture.TemplateOf(graph.Root));
            attached.UnionWith(graph.Nodes.Skip(1).Select(CompositionFixture.TemplateOf));
        }

        Assert.Contains(CompositionFixture.SocketPart, roots);
        Assert.Contains(CompositionFixture.PlugPart, attached);
        Assert.DoesNotContain(CompositionFixture.SocketPart, attached);
    }

    [Fact]
    public void A_required_child_with_no_compatible_definition_fails_explicitly()
    {
        // A star needs at least one body on its orbit connector; this set holds no planet or barycentre.
        PrimitiveSet starsOnly = CompositionFixture.Generate([new SetRequest(6, 2)]);

        var exception = Assert.Throws<GenerationException>(() => CompositionGenerator.Generate(
            CompositionFixture.Specification(9, CompositionDomain.SolarSystem, starsOnly), starsOnly, CompositionFixture.Grammar, CompositionFixture.Registry));

        Assert.Contains("holds no definition", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_graph_round_trips_through_its_record_and_a_changed_byte_is_refused()
    {
        CompositionGraph graph = Compose(9, CompositionDomain.SolarSystem);
        CompositionGraph decoded = CompositionGraph.Decode(graph.Bytes, CompositionFixture.Set, CompositionFixture.Registry);

        Assert.Equal(graph.Hash, decoded.Hash);
        Assert.Equal(graph.NodeCount, decoded.NodeCount);
        Assert.Equal(graph.Nodes.Select(node => node.Path), decoded.Nodes.Select(node => node.Path));
        Assert.Equal(graph.Nodes.Select(node => node.Reference), decoded.Nodes.Select(node => node.Reference));

        byte[] mutated = graph.Bytes;
        mutated[^1] ^= 0x01;
        Assert.Throws<FormatException>(() => CompositionGraph.Decode(mutated, CompositionFixture.Set, CompositionFixture.Registry));
    }

    [Fact]
    public void A_specification_pinning_another_grammar_or_set_is_refused()
    {
        GraphSpecification specification = CompositionFixture.Specification(9, CompositionDomain.SolarSystem);
        CompositionGrammar other = CompositionGrammar.Create(CompositionFixture.Registry, 2, 2, 8, 1,
            [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(CategoryRegistryRevision1.Star, 1)])],
            [
                new Production(CategoryRegistryRevision1.Star, [new ConnectorRule(0, 0, TransformRange.Origin(TransformKind.OrbitalElements), [])]),
            ]);

        Assert.Throws<ArgumentException>(() => CompositionGenerator.Generate(specification, CompositionFixture.Set, other, CompositionFixture.Registry));

        PrimitiveSet otherSet = CompositionFixture.Generate([new SetRequest(6, 1)]);
        Assert.Throws<ArgumentException>(() => CompositionGenerator.Generate(specification, otherSet, CompositionFixture.Grammar, CompositionFixture.Registry));
    }
}
