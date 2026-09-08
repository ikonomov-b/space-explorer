using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// One instance in a composition graph: its deterministic path, the exact definition it instantiates,
/// the transform it attaches to its parent with, and its children grouped by connector kind in the
/// category's order. Nodes may repeat a definition and keep their order, so a graph is not set
/// membership (decision 0031).
/// </summary>
public sealed class GraphNode
{
    public GraphNode(string path, uint depth, PrimitiveDefinition definition, InstanceTransform? transform, IReadOnlyList<IReadOnlyList<GraphNode>> children)
    {
        StreamPath.Validate(path);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(children);

        Path = path;
        Depth = depth;
        Definition = definition;
        Transform = transform;
        Children = [.. children.Select(list => (IReadOnlyList<GraphNode>)[.. list])];
    }

    /// <summary>The stable canonical path that addresses this instance's random stream, overlay, and cache entries (decision 0031).</summary>
    public string Path { get; }

    /// <summary>Zero at the root.</summary>
    public uint Depth { get; }

    public PrimitiveDefinition Definition { get; }

    /// <summary>The exact reference this instance uses.</summary>
    public PrimitiveRevisionRef Reference => Definition.Reference;

    /// <summary>How this instance attaches to its parent, of the one type its connector kind admits; null at the root, which attaches to nothing (decision 0036).</summary>
    public InstanceTransform? Transform { get; }

    /// <summary>The children per connector kind, in the category's connector order.</summary>
    public IReadOnlyList<IReadOnlyList<GraphNode>> Children { get; }

    /// <summary>This node and every descendant, in the order the record stores them.</summary>
    public IEnumerable<GraphNode> Preorder
    {
        get
        {
            yield return this;
            foreach (GraphNode child in Children.SelectMany(list => list))
            {
                foreach (GraphNode descendant in child.Preorder)
                {
                    yield return descendant;
                }
            }
        }
    }

    /// <summary>The path of the <paramref name="index"/>th child on the connector kind labelled <paramref name="connectorLabel"/>.</summary>
    public static string ChildPath(string parentPath, string connectorLabel, int index) =>
        $"{parentPath}{StreamPath.Separator}{connectorLabel}{StreamPath.Separator}{index}";
}

/// <summary>
/// A bounded, ordered composition graph as one canonical record: the specification it came from, laid
/// out inline so the graph reproduces from its own bytes, then the instances in depth-first order with
/// their definitions, transforms, and child counts. Its content hash is the graph hash a run is frozen
/// by, and its pack identifier is the leading 16 bytes of its specification hash (decisions 0006, 0020,
/// 0031, 0036).
/// </summary>
public sealed class CompositionGraph
{
    private readonly byte[] _bytes;

    private CompositionGraph(GraphSpecification specification, PrimitiveSet source, GraphNode root, int nodeCount, uint depth, byte[] bytes, ContentHash hash)
    {
        Specification = specification;
        Source = source;
        Root = root;
        NodeCount = nodeCount;
        Depth = depth;
        _bytes = bytes;
        Hash = hash;
    }

    public GraphSpecification Specification { get; }

    /// <summary>The stored set every instance draws from, already checked against the specification.</summary>
    public PrimitiveSet Source { get; }

    public GraphNode Root { get; }

    /// <summary>How many instances the graph holds, explicit in the record so a reader bounds its work before it recurses.</summary>
    public int NodeCount { get; }

    /// <summary>The greatest depth reached, the root being zero.</summary>
    public uint Depth { get; }

    /// <summary>The pack this graph publishes.</summary>
    public PackId Pack => Specification.PackId;

    public byte[] Bytes => [.. _bytes];

    /// <summary>The content hash of the canonical record: the graph hash a fixed-seed run is frozen by.</summary>
    public ContentHash Hash { get; }

    /// <summary>Every instance in the order the record stores them.</summary>
    public IEnumerable<GraphNode> Nodes => Root.Preorder;

    /// <summary>Checks <paramref name="root"/> against <paramref name="specification"/>, <paramref name="source"/>, and the registry, then encodes and hashes it once.</summary>
    /// <exception cref="ArgumentException">A path, depth, definition, transform, or child count does not fit.</exception>
    public static CompositionGraph Create(GraphSpecification specification, PrimitiveSet source, CategoryRegistry registry, GraphNode root)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(root);

        if (specification.RegistryRevision != registry.Revision || specification.RegistryHash != registry.Hash)
        {
            throw new ArgumentException("The specification pins another category-registry revision or hash (decision 0035).", nameof(specification));
        }

        if (specification.SourcePack != source.Manifest.Pack || specification.SourceManifestHash != source.Manifest.Hash)
        {
            throw new ArgumentException("The specification pins another stored set.", nameof(specification));
        }

        int nodeCount = 0;
        uint depth = 0;
        Validate(root, specification.RootPath, 0, source, registry, ref nodeCount, ref depth);

        var writer = new CanonicalWriter(registry.GraphDomain);
        specification.EncodeInline(writer);
        writer.WriteCount(nodeCount);
        Encode(writer, root, registry);

        return new CompositionGraph(specification, source, root, nodeCount, depth, writer.ToArray(), writer.ToContentHash());
    }

    /// <summary>Decodes a record against the set it composes, rejecting trailing bytes and a non-canonical encoding.</summary>
    /// <exception cref="FormatException">The bytes are not a valid graph record over that set and registry.</exception>
    public static CompositionGraph Decode(byte[] bytes, PrimitiveSet source, CategoryRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(registry);

        var reader = new CanonicalReader(bytes, registry.GraphDomain);
        GraphSpecification specification = GraphSpecification.Read(reader);

        int declared = reader.ReadCount();
        if (declared < 1 || declared > CompositionGrammar.MaxNodeLimit)
        {
            throw new FormatException($"The graph declares {declared} instances; the bound is 1 to {CompositionGrammar.MaxNodeLimit}.");
        }

        int remaining = declared;
        GraphNode root = DecodeNode(reader, specification.RootPath, 0, attachedBy: null, source, registry, ref remaining);

        if (remaining != 0)
        {
            throw new FormatException($"The graph declares {declared} instances and holds {declared - remaining}.");
        }

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The graph record has trailing bytes.");
        }

        CompositionGraph graph;
        try
        {
            graph = Create(specification, source, registry, root);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        if (!graph._bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The graph record is not in canonical form: re-encoding it yields different bytes.");
        }

        return graph;
    }

    private static void Validate(GraphNode node, string expectedPath, uint expectedDepth, PrimitiveSet source, CategoryRegistry registry, ref int nodeCount, ref uint depth)
    {
        if (node.Path != expectedPath || node.Depth != expectedDepth)
        {
            throw new ArgumentException($"Instance '{node.Path}' at depth {node.Depth} should be '{expectedPath}' at depth {expectedDepth}; a path addresses an instance's stream and overlay and cannot drift (decision 0031).", nameof(node));
        }

        if (expectedDepth > CompositionGrammar.MaxDepthLimit)
        {
            throw new ArgumentException($"Instance '{node.Path}' is deeper than the {CompositionGrammar.MaxDepthLimit} any grammar admits.", nameof(node));
        }

        if (!source.Contains(node.Reference))
        {
            throw new ArgumentException($"Instance '{node.Path}' names {node.Reference.Id}, which the pinned set does not hold with that hash.", nameof(node));
        }

        nodeCount++;
        depth = Math.Max(depth, expectedDepth);

        CategoryDefinition schema = registry.Find(node.Definition.Category);
        if (node.Children.Count != schema.Connectors.Count)
        {
            throw new ArgumentException($"Instance '{node.Path}' is a '{schema.Label}' with {schema.Connectors.Count} connector kinds; it lists {node.Children.Count}.", nameof(node));
        }

        for (int index = 0; index < schema.Connectors.Count; index++)
        {
            ConnectorKind connector = schema.Connectors[index];
            IReadOnlyList<GraphNode> children = node.Children[index];

            if (children.Count < connector.MinCount || children.Count > connector.MaxCount)
            {
                throw new ArgumentException($"Instance '{node.Path}' attaches {children.Count} children to '{connector.Label}', outside its [{connector.MinCount}, {connector.MaxCount}].", nameof(node));
            }

            for (int child = 0; child < children.Count; child++)
            {
                GraphNode attached = children[child];
                if (attached.Transform is null || attached.Transform.Kind != connector.Transform)
                {
                    throw new ArgumentException($"Instance '{attached.Path}' attaches to '{connector.Label}' with {(attached.Transform is null ? "no transform" : attached.Transform.Kind.ToString())}; that connector admits {connector.Transform} (decision 0036).", nameof(node));
                }

                if (!connector.ChildCategories.Contains(attached.Definition.Category))
                {
                    throw new ArgumentException($"Instance '{attached.Path}' is of category {attached.Definition.Category}, which '{connector.Label}' does not admit (decision 0031).", nameof(node));
                }

                if (!ConnectorDeclarationOf(attached.Definition, registry, connector.Label).Satisfies(node.Definition.Connectors[index].RequiredTags))
                {
                    throw new ArgumentException($"Instance '{attached.Path}' does not provide every tag '{node.Path}' requires on '{connector.Label}' (decision 0031).", nameof(node));
                }

                Validate(attached, GraphNode.ChildPath(node.Path, connector.Label, child), expectedDepth + 1, source, registry, ref nodeCount, ref depth);
            }
        }

        if (node.Transform is null && expectedDepth != 0)
        {
            throw new ArgumentException($"Instance '{node.Path}' carries no transform and is not the root.", nameof(node));
        }
    }

    /// <summary>
    /// The declaration a candidate child offers on the connector kind it is attached by: its own
    /// like-labelled connector, since a connector label names a role (decision 0031), or nothing at all
    /// when its category has no such connector, which fits only where the parent requires no tag.
    /// </summary>
    internal static ConnectorDeclaration ConnectorDeclarationOf(PrimitiveDefinition definition, CategoryRegistry registry, string connectorLabel)
    {
        CategoryDefinition schema = registry.Find(definition.Category);
        for (int index = 0; index < schema.Connectors.Count; index++)
        {
            if (schema.Connectors[index].Label == connectorLabel)
            {
                return definition.Connectors[index];
            }
        }

        return ConnectorDeclaration.Empty;
    }

    private static void Encode(CanonicalWriter writer, GraphNode node, CategoryRegistry registry)
    {
        writer.WriteVarUInt(node.Definition.Id.LocalId);
        node.Transform?.Encode(writer);

        CategoryDefinition schema = registry.Find(node.Definition.Category);
        for (int index = 0; index < schema.Connectors.Count; index++)
        {
            IReadOnlyList<GraphNode> children = node.Children[index];
            writer.WriteCount(children.Count);
            foreach (GraphNode child in children)
            {
                Encode(writer, child, registry);
            }
        }
    }

    /// <param name="attachedBy">The transform type of the connector this node hangs from, which its parent knows before a byte is read; null at the root (decision 0036).</param>
    private static GraphNode DecodeNode(CanonicalReader reader, string path, uint depth, TransformKind? attachedBy, PrimitiveSet source, CategoryRegistry registry, ref int remaining)
    {
        if (remaining == 0)
        {
            throw new FormatException("The graph holds more instances than it declares.");
        }

        if (depth > CompositionGrammar.MaxDepthLimit)
        {
            throw new FormatException($"Instance '{path}' is deeper than the {CompositionGrammar.MaxDepthLimit} any grammar admits.");
        }

        remaining--;

        ulong localId = reader.ReadVarUInt();
        if (localId == 0 || localId > (ulong)source.Definitions.Count)
        {
            throw new FormatException($"Instance '{path}' names definition {localId}, which the pinned set does not hold.");
        }

        PrimitiveDefinition definition = source.Definitions[(int)localId - 1];
        CategoryDefinition schema = registry.TryFind(definition.Category)
            ?? throw new FormatException($"Registry revision {registry.Revision} has no category {definition.Category}.");

        InstanceTransform? transform = attachedBy is { } kind ? InstanceTransform.Decode(reader, kind) : null;

        var children = new List<IReadOnlyList<GraphNode>>(schema.Connectors.Count);
        foreach (ConnectorKind connector in schema.Connectors)
        {
            int count = reader.ReadCount();
            if (count > connector.MaxCount || count > remaining)
            {
                throw new FormatException($"Instance '{path}' attaches {count} children to '{connector.Label}', beyond the connector's {connector.MaxCount} or the graph's remaining instances.");
            }

            var attached = new GraphNode[count];
            for (int index = 0; index < count; index++)
            {
                attached[index] = DecodeNode(reader, GraphNode.ChildPath(path, connector.Label, index), depth + 1, connector.Transform, source, registry, ref remaining);
            }

            children.Add(attached);
        }

        return new GraphNode(path, depth, definition, transform, children);
    }
}
