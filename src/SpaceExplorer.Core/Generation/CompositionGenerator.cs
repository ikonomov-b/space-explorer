using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Generation;

/// <summary>
/// Composes one bounded, ordered graph of instances from a stored set under a versioned grammar
/// (decisions 0031, 0036). Every instance has a deterministic path, and its own stream, derived from the
/// seed and that path, draws its definition, its attachment transform, and how many children each of its
/// connector kinds takes; a parent's stream draws only the counts, so no instance's content depends on
/// its siblings. A child is eligible when its category is one the connector and the grammar admit and its
/// declaration for the like-labelled connector provides every tag the parent's declaration requires.
/// Depth, instance count, and retries are bounded by the grammar, and an unfillable required child fails
/// explicitly rather than falling back to something else.
/// </summary>
public static class CompositionGenerator
{
    /// <summary>Composes the graph <paramref name="specification"/> describes from <paramref name="source"/> under <paramref name="grammar"/>.</summary>
    /// <exception cref="ArgumentException">The specification does not pin this registry, set, generator, or grammar.</exception>
    /// <exception cref="GenerationException">The domain has no root, a required child has no compatible definition, or a required child would breach the depth or instance bound.</exception>
    public static CompositionGraph Generate(GraphSpecification specification, PrimitiveSet source, CompositionGrammar grammar, CategoryRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(registry);

        if (specification.RegistryRevision != registry.Revision || specification.RegistryHash != registry.Hash)
        {
            throw new ArgumentException("The specification pins another category-registry revision or hash (decision 0035).", nameof(specification));
        }

        if (grammar.RegistryRevision != registry.Revision)
        {
            throw new ArgumentException($"The grammar is laid out under registry revision {grammar.RegistryRevision}, not {registry.Revision}.", nameof(grammar));
        }

        if (specification.GrammarVersion != grammar.Version || specification.GrammarHash != grammar.Hash)
        {
            throw new ArgumentException("The specification pins another composition grammar.", nameof(specification));
        }

        if (specification.GeneratorVersion != GeneratorVersion.Current)
        {
            throw new ArgumentException($"The specification pins generator {specification.GeneratorVersion}; this build is generator {GeneratorVersion.Current}.", nameof(specification));
        }

        if (specification.SourcePack != source.Manifest.Pack || specification.SourceManifestHash != source.Manifest.Hash)
        {
            throw new ArgumentException("The specification pins another stored set.", nameof(specification));
        }

        var state = new State(specification, source, grammar, registry);
        RootRule root = grammar.TryFindRoot(specification.CompositionDomain)
            ?? throw new GenerationException($"Grammar version {grammar.Version} roots no graph of domain '{specification.RootPath}'.");

        string path = specification.RootPath;
        Pcg32 stream = RandomStream.Derive(specification.Seed, path);
        PrimitiveDefinition definition = state.Choose(stream, root.Choices, [], connectorLabel: null, depth: 0)
            ?? throw new GenerationException($"No definition of the pinned set roots a '{path}' graph: the set holds none of the categories the grammar roots it at.");

        state.Count++;
        GraphNode node = state.Build(definition, path, depth: 0, stream, transform: null, zone: null);
        return CompositionGraph.Create(specification, source, registry, node);
    }

    private sealed class State(GraphSpecification specification, PrimitiveSet source, CompositionGrammar grammar, CategoryRegistry registry)
    {
        private readonly Dictionary<uint, List<PrimitiveDefinition>> _byCategory = source.Definitions
            .GroupBy(definition => definition.Category)
            .ToDictionary(group => group.Key, group => group.ToList());

        /// <summary>How many instances the graph holds so far, against the grammar's bound.</summary>
        public uint Count { get; set; }

        /// <param name="zone">The band tag this instance was chosen for, which a connector that inherits passes to its own children.</param>
        public GraphNode Build(PrimitiveDefinition definition, string path, uint depth, Pcg32 stream, InstanceTransform? transform, string? zone)
        {
            CategoryDefinition schema = registry.Find(definition.Category);
            Production production = grammar.TryFindProduction(definition.Category)
                ?? throw new GenerationException($"Grammar version {grammar.Version} has no production for category '{schema.Label}'.");

            var children = new List<IReadOnlyList<GraphNode>>(schema.Connectors.Count);
            for (int index = 0; index < schema.Connectors.Count; index++)
            {
                ConnectorKind connector = schema.Connectors[index];
                ConnectorRule rule = production.ConnectorRules[index];
                IReadOnlyList<string> declared = definition.Connectors[index].RequiredTags;
                uint count = (uint)ParameterSampler.SampleInclusive(stream, rule.MinCount, rule.MaxCount);

                // A rule may scale its bands by something about the parent; a star's scale by the square
                // root of its luminosity, so one set of bands means one range of temperatures around any
                // star (decision 0051).
                long scale = rule.BandScale.Length == 0
                    ? DerivationRules.OrbitScaleUnit
                    : BandScaleRules.Scale(rule.BandScale, definition, registry);

                var attached = new List<GraphNode>((int)count);
                for (uint child = 0; child < count; child++)
                {
                    string childPath = GraphNode.ChildPath(path, connector.Label, (int)child);
                    Pcg32 childStream = RandomStream.Derive(specification.Seed, childPath);

                    // What this band demands, or what this instance itself was chosen for where the
                    // connector passes its zone down, is added to what the parent demands of every child.
                    string? demanded = rule.InheritsBandTag ? zone : rule.BandTag((int)child);
                    IReadOnlyList<string> required = demanded is null ? declared : [.. declared, demanded];
                    PrimitiveDefinition? chosen = Choose(childStream, rule.Choices, required, connector.Label, depth + 1);
                    if (chosen is null)
                    {
                        Require(
                            attached.Count >= rule.MinCount,
                            rule.Choices.Any(choice => Fits(choice.Category, depth + 1))
                                ? $"Instance '{path}' needs {rule.MinCount} children on '{connector.Label}' and the pinned set holds no definition of theirs providing {string.Join(" ", required)} within {grammar.RetryBudget} attempts."
                                : $"Instance '{path}' needs {rule.MinCount} children on '{connector.Label}' and none fits the grammar's depth bound {grammar.MaxDepth} or instance bound {grammar.MaxNodes}.");
                        break;
                    }

                    Count++;
                    attached.Add(Build(chosen, childPath, depth + 1, childStream, Sample(childStream, rule, (int)child, scale), demanded));
                }

                children.Add(attached);
            }

            return new GraphNode(path, depth, definition, transform, children);
        }

        /// <summary>Draws a category by weight, then a definition of it that provides every required tag and whose own required structure still fits, redrawing the category up to the grammar's retry budget.</summary>
        public PrimitiveDefinition? Choose(Pcg32 stream, IReadOnlyList<CategoryChoice> choices, IReadOnlyList<string> required, string? connectorLabel, uint depth)
        {
            if (choices.Count == 0)
            {
                return null;
            }

            for (uint attempt = 0; attempt < grammar.RetryBudget; attempt++)
            {
                uint category = ChooseCategory(stream, choices);
                List<PrimitiveDefinition> eligible = Fits(category, depth) && _byCategory.TryGetValue(category, out List<PrimitiveDefinition>? pool)
                    ? [.. pool.Where(candidate => connectorLabel is null || CompositionGraph.ConnectorDeclarationOf(candidate, registry, connectorLabel).Satisfies(required))]
                    : [];

                if (eligible.Count > 0)
                {
                    return eligible[(int)stream.NextBounded((uint)eligible.Count)];
                }
            }

            return null;
        }

        /// <summary>Whether an instance of <paramref name="category"/> at <paramref name="depth"/> can still be completed inside the grammar's depth and instance bounds.</summary>
        public bool Fits(uint category, uint depth) =>
            depth + grammar.RequiredDepth(category) <= grammar.MaxDepth && Count + grammar.RequiredNodes(category) <= grammar.MaxNodes;

        private static uint ChooseCategory(Pcg32 stream, IReadOnlyList<CategoryChoice> choices)
        {
            long total = choices.Sum(choice => (long)choice.Weight);
            long drawn = ParameterSampler.SampleInclusive(stream, 0, total - 1);
            foreach (CategoryChoice choice in choices)
            {
                if (drawn < choice.Weight)
                {
                    return choice.Category;
                }

                drawn -= choice.Weight;
            }

            return choices[^1].Category;
        }

        /// <summary>
        /// Draws one attachment transform. The first component of an orbit is drawn from the band the
        /// rule's spacing ratio gives the child at <paramref name="index"/>, so the children of one
        /// connector come out ordered and geometrically spaced, and <paramref name="scale"/> moves the
        /// whole progression with the parent; every other component spans its own declared range
        /// (decisions 0050, 0051).
        /// </summary>
        private static InstanceTransform Sample(Pcg32 stream, ConnectorRule rule, int index, long scale)
        {
            TransformRange range = rule.Transform;
            var components = new long[range.Bounds.Count];
            for (int component = 0; component < components.Length; component++)
            {
                (long min, long max) = component == 0 ? rule.OrbitBand(index, scale) : range.Bounds[component];
                components[component] = ParameterSampler.SampleInclusive(stream, min, max);
            }

            return new InstanceTransform(range.Kind, components);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new GenerationException(message);
            }
        }
    }
}
