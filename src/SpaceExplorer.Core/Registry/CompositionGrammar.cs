using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>One eligible category with the integer weight it is drawn by (decision 0031).</summary>
public sealed record CategoryChoice(uint Category, uint Weight);

/// <summary>
/// What a grammar allows on one connector kind: how many children, the bounded transform they attach
/// with, and the weighted categories they may belong to (decisions 0031, 0036).
/// </summary>
public sealed record ConnectorRule(uint MinCount, uint MaxCount, TransformRange Transform, IReadOnlyList<CategoryChoice> Choices);

/// <summary>The rules for one category: one <see cref="ConnectorRule"/> per connector kind, in the category's own order.</summary>
public sealed record Production(uint Category, IReadOnlyList<ConnectorRule> ConnectorRules);

/// <summary>The weighted categories a graph of one composition domain may be rooted at.</summary>
public sealed record RootRule(CompositionDomain Domain, IReadOnlyList<CategoryChoice> Choices);

/// <summary>
/// A versioned composition grammar as one canonical record, laid out under a category-registry revision
/// and identified exactly by its content hash: the depth, node, and retry bounds, the root categories per
/// composition domain, and one production per reachable category (decision 0031). A graph specification
/// pins the version and the hash, so changing any weight, count, or transform range is a new grammar
/// version, exactly as a parameter-schema change is a new registry revision (decision 0035).
/// </summary>
public sealed class CompositionGrammar
{
    /// <summary>The grammar version this build implements; every specification and manifest pins it.</summary>
    public const uint CurrentVersion = 1;

    /// <summary>The deepest a graph may nest, which also bounds the decoder's recursion.</summary>
    public const uint MaxDepthLimit = 64;

    /// <summary>The most instances one graph may hold.</summary>
    public const uint MaxNodeLimit = 1 << 16;

    private readonly Production[] _productions;
    private readonly RootRule[] _roots;
    private readonly Dictionary<uint, (uint Depth, long Nodes)> _minima;
    private readonly Lazy<(byte[] Bytes, ContentHash Hash)> _encoded;

    private CompositionGrammar(uint version, uint registryRevision, uint maxDepth, uint maxNodes, uint retryBudget, RootRule[] roots, Production[] productions, Dictionary<uint, (uint, long)> minima, CategoryRegistry registry)
    {
        _minima = minima;
        Version = version;
        RegistryRevision = registryRevision;
        MaxDepth = maxDepth;
        MaxNodes = maxNodes;
        RetryBudget = retryBudget;
        _roots = roots;
        _productions = productions;
        _encoded = new Lazy<(byte[], ContentHash)>(() =>
        {
            CanonicalWriter writer = Write(registry);
            return (writer.ToArray(), writer.ToContentHash());
        });
    }

    /// <summary>The version this record defines; part of the domain label.</summary>
    public uint Version { get; }

    /// <summary>The category-registry revision this grammar names categories and connectors of.</summary>
    public uint RegistryRevision { get; }

    /// <summary>The deepest instance this grammar may produce, the root being depth zero.</summary>
    public uint MaxDepth { get; }

    /// <summary>The most instances one graph may hold.</summary>
    public uint MaxNodes { get; }

    /// <summary>How many times a child slot may redraw a category before it is treated as unfilled.</summary>
    public uint RetryBudget { get; }

    /// <summary>The root rules in ascending domain order.</summary>
    public IReadOnlyList<RootRule> Roots => _roots;

    /// <summary>The productions in ascending category order.</summary>
    public IReadOnlyList<Production> Productions => _productions;

    /// <summary>The canonical bytes of this record.</summary>
    public byte[] Bytes => [.. _encoded.Value.Bytes];

    /// <summary>The content hash a graph specification pins.</summary>
    public ContentHash Hash => _encoded.Value.Hash;

    /// <summary>The root rule of <paramref name="domain"/>, or null when this grammar composes no such graph.</summary>
    public RootRule? TryFindRoot(CompositionDomain domain) => _roots.FirstOrDefault(root => root.Domain == domain);

    /// <summary>The production for <paramref name="category"/>, or null.</summary>
    public Production? TryFindProduction(uint category) => _productions.FirstOrDefault(production => production.Category == category);

    /// <summary>
    /// How far below an instance of <paramref name="category"/> its required children reach at the least,
    /// zero for a category that requires none. An instance may be placed only where its whole required
    /// structure still fits the depth bound, so this is what stops an optional child from becoming an
    /// instance that cannot be completed.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No production reaches that category.</exception>
    public uint RequiredDepth(uint category) => Minimum(category).Depth;

    /// <summary>How many instances an instance of <paramref name="category"/> and its required children come to at the least, itself included.</summary>
    /// <exception cref="KeyNotFoundException">No production reaches that category.</exception>
    public long RequiredNodes(uint category) => Minimum(category).Nodes;

    private (uint Depth, long Nodes) Minimum(uint category) =>
        _minima.TryGetValue(category, out (uint Depth, long Nodes) minimum)
            ? minimum
            : throw new KeyNotFoundException($"Grammar version {Version} reaches no category {category}.");

    /// <summary>Builds and validates a grammar against <paramref name="registry"/>.</summary>
    /// <exception cref="ArgumentException">A bound, a weight, a count, a transform type, or the reachable-category closure is wrong.</exception>
    public static CompositionGrammar Create(CategoryRegistry registry, uint version, uint maxDepth, uint maxNodes, uint retryBudget, IReadOnlyList<RootRule> roots, IReadOnlyList<Production> productions)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(roots);
        ArgumentNullException.ThrowIfNull(productions);

        if (version == 0)
        {
            throw new ArgumentException("Grammar versions start at 1.", nameof(version));
        }

        if (maxDepth == 0 || maxDepth > MaxDepthLimit)
        {
            throw new ArgumentException($"A grammar admits a depth of 1 to {MaxDepthLimit}; received {maxDepth}.", nameof(maxDepth));
        }

        if (maxNodes == 0 || maxNodes > MaxNodeLimit)
        {
            throw new ArgumentException($"A grammar admits 1 to {MaxNodeLimit} instances; received {maxNodes}.", nameof(maxNodes));
        }

        if (retryBudget == 0)
        {
            throw new ArgumentException("The retry budget is at least one attempt.", nameof(retryBudget));
        }

        RootRule[] orderedRoots = ValidateRoots(registry, roots);
        Production[] orderedProductions = ValidateProductions(registry, productions);
        ValidateClosure(orderedRoots, orderedProductions);
        Dictionary<uint, (uint Depth, long Nodes)> minima = Minima(orderedProductions);

        foreach (CategoryChoice choice in orderedRoots.SelectMany(root => root.Choices))
        {
            (uint depth, long nodes) = minima[choice.Category];
            if (depth > maxDepth || nodes > maxNodes)
            {
                throw new ArgumentException($"A graph rooted at category {choice.Category} needs depth {depth} and {nodes} instances at the least, beyond the grammar's {maxDepth} and {maxNodes}.", nameof(productions));
            }
        }

        return new CompositionGrammar(version, registry.Revision, maxDepth, maxNodes, retryBudget, orderedRoots, orderedProductions, minima, registry);
    }

    /// <summary>Decodes a record of <paramref name="expectedVersion"/> laid out under <paramref name="registry"/>, rejecting trailing bytes and a non-canonical encoding.</summary>
    /// <exception cref="FormatException">The bytes are not a valid grammar record of that version and revision.</exception>
    public static CompositionGrammar Decode(byte[] bytes, CategoryRegistry registry, uint expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var reader = new CanonicalReader(bytes, registry.GrammarDomain(expectedVersion));

        uint maxDepth = reader.ReadUInt32();
        uint maxNodes = reader.ReadUInt32();
        uint retryBudget = reader.ReadUInt32();

        int rootCount = reader.ReadCount();
        if (rootCount > CategoryDefinition.MaxDomains)
        {
            throw new FormatException($"The grammar declares {rootCount} root rules; the bound is {CategoryDefinition.MaxDomains}.");
        }

        var roots = new RootRule[rootCount];
        for (int index = 0; index < rootCount; index++)
        {
            var domain = (CompositionDomain)reader.ReadUInt8();
            roots[index] = new RootRule(domain, ReadChoices(reader));
        }

        int productionCount = reader.ReadCount();
        if (productionCount > CategoryRegistry.MaxCategories)
        {
            throw new FormatException($"The grammar declares {productionCount} productions; the bound is {CategoryRegistry.MaxCategories}.");
        }

        var productions = new Production[productionCount];
        for (int index = 0; index < productionCount; index++)
        {
            uint category = reader.ReadUInt32();
            CategoryDefinition schema = registry.TryFind(category)
                ?? throw new FormatException($"Registry revision {registry.Revision} has no category {category}.");

            var rules = new ConnectorRule[schema.Connectors.Count];
            for (int rule = 0; rule < rules.Length; rule++)
            {
                uint minCount = reader.ReadUInt32();
                uint maxCount = reader.ReadUInt32();
                TransformRange transform = TransformRange.Decode(reader, schema.Connectors[rule].Transform);
                rules[rule] = new ConnectorRule(minCount, maxCount, transform, ReadChoices(reader));
            }

            productions[index] = new Production(category, rules);
        }

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The grammar record has trailing bytes.");
        }

        CompositionGrammar grammar;
        try
        {
            grammar = Create(registry, expectedVersion, maxDepth, maxNodes, retryBudget, roots, productions);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        if (!grammar._encoded.Value.Bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The grammar record is not in canonical form: re-encoding it yields different bytes.");
        }

        return grammar;
    }

    private static RootRule[] ValidateRoots(CategoryRegistry registry, IReadOnlyList<RootRule> roots)
    {
        if (roots.Count == 0)
        {
            throw new ArgumentException("A grammar roots at least one composition domain.", nameof(roots));
        }

        RootRule[] ordered = [.. roots.OrderBy(root => (byte)root.Domain)];
        for (int index = 0; index < ordered.Length; index++)
        {
            RootRule root = ordered[index];
            if (!Enum.IsDefined(root.Domain) || (index > 0 && root.Domain == ordered[index - 1].Domain))
            {
                throw new ArgumentException($"Domain {(byte)root.Domain} is unknown or rooted twice.", nameof(roots));
            }

            if (root.Choices.Count == 0)
            {
                throw new ArgumentException($"Domain '{CompositionDomains.Label(root.Domain)}' roots at no category.", nameof(roots));
            }

            ValidateChoices(root.Choices, $"the root of '{CompositionDomains.Label(root.Domain)}'", nameof(roots));
            foreach (CategoryChoice choice in root.Choices)
            {
                CategoryDefinition schema = registry.TryFind(choice.Category)
                    ?? throw new ArgumentException($"Registry revision {registry.Revision} has no category {choice.Category}.", nameof(roots));

                if (!schema.Domains.Contains(root.Domain))
                {
                    throw new ArgumentException($"Category '{schema.Label}' does not join domain '{CompositionDomains.Label(root.Domain)}', so it cannot root it.", nameof(roots));
                }
            }
        }

        return ordered;
    }

    private static Production[] ValidateProductions(CategoryRegistry registry, IReadOnlyList<Production> productions)
    {
        Production[] ordered = [.. productions.OrderBy(production => production.Category)];
        for (int index = 0; index < ordered.Length; index++)
        {
            Production production = ordered[index];
            if (index > 0 && production.Category == ordered[index - 1].Category)
            {
                throw new ArgumentException($"Category {production.Category} has two productions.", nameof(productions));
            }

            CategoryDefinition schema = registry.TryFind(production.Category)
                ?? throw new ArgumentException($"Registry revision {registry.Revision} has no category {production.Category}.", nameof(productions));

            if (production.ConnectorRules.Count != schema.Connectors.Count)
            {
                throw new ArgumentException($"Category '{schema.Label}' has {schema.Connectors.Count} connector kinds; the production gives {production.ConnectorRules.Count} rules.", nameof(productions));
            }

            for (int rule = 0; rule < production.ConnectorRules.Count; rule++)
            {
                ValidateRule(production.ConnectorRules[rule], schema, schema.Connectors[rule], nameof(productions));
            }
        }

        return ordered;
    }

    private static void ValidateRule(ConnectorRule rule, CategoryDefinition schema, ConnectorKind connector, string parameterName)
    {
        string where = $"'{schema.Label}/{connector.Label}'";

        if (rule.MinCount > rule.MaxCount || rule.MinCount < connector.MinCount || rule.MaxCount > connector.MaxCount)
        {
            throw new ArgumentException($"Rule {where} allows [{rule.MinCount}, {rule.MaxCount}] children, which is empty or outside the connector's [{connector.MinCount}, {connector.MaxCount}].", parameterName);
        }

        if (rule.Transform.Kind != connector.Transform)
        {
            throw new ArgumentException($"Rule {where} carries a {rule.Transform.Kind} range; the connector admits {connector.Transform} (decision 0036).", parameterName);
        }

        if ((rule.MaxCount == 0) != (rule.Choices.Count == 0))
        {
            throw new ArgumentException($"Rule {where} must list eligible categories exactly when it admits a child.", parameterName);
        }

        ValidateChoices(rule.Choices, $"rule {where}", parameterName);
        foreach (CategoryChoice choice in rule.Choices)
        {
            if (!connector.ChildCategories.Contains(choice.Category))
            {
                throw new ArgumentException($"Rule {where} admits category {choice.Category}, which the connector does not (decision 0031).", parameterName);
            }
        }
    }

    private static void ValidateChoices(IReadOnlyList<CategoryChoice> choices, string where, string parameterName)
    {
        for (int index = 0; index < choices.Count; index++)
        {
            if (choices[index].Weight == 0)
            {
                throw new ArgumentException($"A choice of {where} has weight zero; a weight of zero is an absent choice.", parameterName);
            }

            if (index > 0 && choices[index].Category <= choices[index - 1].Category)
            {
                throw new ArgumentException($"The choices of {where} must be in ascending, distinct category order.", parameterName);
            }
        }
    }

    private static void ValidateClosure(RootRule[] roots, Production[] productions)
    {
        var reachable = new HashSet<uint>();
        var pending = new Queue<uint>(roots.SelectMany(root => root.Choices).Select(choice => choice.Category));
        while (pending.Count > 0)
        {
            uint category = pending.Dequeue();
            if (!reachable.Add(category))
            {
                continue;
            }

            Production production = Array.Find(productions, candidate => candidate.Category == category)
                ?? throw new ArgumentException($"Category {category} is reachable but has no production.", nameof(productions));

            foreach (CategoryChoice choice in production.ConnectorRules.SelectMany(rule => rule.Choices))
            {
                pending.Enqueue(choice.Category);
            }
        }

        foreach (Production production in productions)
        {
            if (!reachable.Contains(production.Category))
            {
                throw new ArgumentException($"Category {production.Category} has a production no root reaches; a grammar carries no dead rules.", nameof(productions));
            }
        }
    }

    /// <summary>
    /// The least depth and instance count each category costs, taking the cheapest eligible child of every
    /// required connector. A category that requires itself through such a chain produces no finite graph
    /// and is refused here rather than at generation.
    /// </summary>
    private static Dictionary<uint, (uint Depth, long Nodes)> Minima(Production[] productions)
    {
        var minima = new Dictionary<uint, (uint Depth, long Nodes)>();
        var visiting = new HashSet<uint>();

        foreach (Production production in productions)
        {
            Minimum(production.Category);
        }

        return minima;

        (uint Depth, long Nodes) Minimum(uint category)
        {
            if (minima.TryGetValue(category, out (uint Depth, long Nodes) known))
            {
                return known;
            }

            if (!visiting.Add(category))
            {
                throw new ArgumentException($"Category {category} requires itself through a chain of required children, so no finite graph satisfies this grammar.", nameof(productions));
            }

            Production production = Array.Find(productions, candidate => candidate.Category == category)!;
            uint depth = 0;
            long nodes = 1;

            foreach (ConnectorRule rule in production.ConnectorRules.Where(rule => rule.MinCount > 0))
            {
                uint cheapestDepth = uint.MaxValue;
                long cheapestNodes = long.MaxValue;
                foreach (CategoryChoice choice in rule.Choices)
                {
                    (uint childDepth, long childNodes) = Minimum(choice.Category);
                    cheapestDepth = Math.Min(cheapestDepth, childDepth + 1);
                    cheapestNodes = Math.Min(cheapestNodes, childNodes);
                }

                depth = Math.Max(depth, cheapestDepth);
                nodes = Math.Min(nodes + (rule.MinCount * cheapestNodes), MaxNodeLimit + 1);
            }

            visiting.Remove(category);
            minima[category] = (depth, nodes);
            return (depth, nodes);
        }
    }

    private static CategoryChoice[] ReadChoices(CanonicalReader reader)
    {
        int count = reader.ReadCount();
        if (count > ConnectorKind.MaxChildCategories)
        {
            throw new FormatException($"A rule lists {count} eligible categories; the bound is {ConnectorKind.MaxChildCategories}.");
        }

        var choices = new CategoryChoice[count];
        for (int index = 0; index < count; index++)
        {
            choices[index] = new CategoryChoice(reader.ReadUInt32(), reader.ReadUInt32());
        }

        return choices;
    }

    private CanonicalWriter Write(CategoryRegistry registry)
    {
        var writer = new CanonicalWriter(registry.GrammarDomain(Version));
        writer.WriteUInt32(MaxDepth);
        writer.WriteUInt32(MaxNodes);
        writer.WriteUInt32(RetryBudget);

        writer.WriteCount(_roots.Length);
        foreach (RootRule root in _roots)
        {
            writer.WriteUInt8((byte)root.Domain);
            WriteChoices(writer, root.Choices);
        }

        writer.WriteCount(_productions.Length);
        foreach (Production production in _productions)
        {
            writer.WriteUInt32(production.Category);
            foreach (ConnectorRule rule in production.ConnectorRules)
            {
                writer.WriteUInt32(rule.MinCount);
                writer.WriteUInt32(rule.MaxCount);
                rule.Transform.Encode(writer);
                WriteChoices(writer, rule.Choices);
            }
        }

        return writer;
    }

    private static void WriteChoices(CanonicalWriter writer, IReadOnlyList<CategoryChoice> choices)
    {
        writer.WriteCount(choices.Count);
        foreach (CategoryChoice choice in choices)
        {
            writer.WriteUInt32(choice.Category);
            writer.WriteUInt32(choice.Weight);
        }
    }
}
