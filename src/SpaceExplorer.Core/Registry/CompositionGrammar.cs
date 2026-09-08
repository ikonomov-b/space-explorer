using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>One eligible category with the integer weight it is drawn by (decision 0031).</summary>
public sealed record CategoryChoice(uint Category, uint Weight);

/// <summary>
/// What a grammar allows on one connector kind: how many children, the bounded transform they attach
/// with, the weighted categories they may belong to (decisions 0031, 0036), and, from grammar version 2,
/// the geometric progression their orbits follow.
/// </summary>
/// <param name="SpacingRatio">
/// The ratio between one child's orbit and the next, in 1/256, or zero for none. When it is set, the
/// child at index <c>i</c> draws its semi-major axis from the band that begins at the transform range's
/// own minimum multiplied by the ratio <c>i</c> times, so the children of one connector come out ordered
/// and spaced as a real system is rather than scattered over one wide range. Carried only by grammar
/// version 2 and later, whose domain label says so.
/// </param>
/// <param name="BandScale">
/// The generator revision identifier of a rule that scales every band by something about the parent, or
/// empty for none. A star's bands scale by the square root of its luminosity, so one set of bands means
/// the same temperature around any star. Carried only by grammar version 3 and later.
/// </param>
/// <param name="BandTags">
/// The tag a child must provide to take each band, one per band and empty where a band demands none, or
/// an empty list where no band does. It is what keeps a body suited to the orbit it takes. Carried only
/// by grammar version 3 and later.
/// </param>
/// <param name="InheritsBandTag">
/// Whether a child of this connector must provide the same band tag its parent was chosen for. A body
/// that shares its parent's distance from the star shares its temperature, so it must suit the same
/// zone: it is what carries a band's demand down to a moon and to the pair of a barycentre. Carried only
/// by grammar version 3 and later.
/// </param>
/// <param name="BandBase">
/// Where the innermost band begins at unit scale, or zero to begin at the transform range's own minimum.
/// The range is the envelope every scaled band must stay inside, which is much wider than the
/// progression itself once a scale moves it, so the two are stated separately. Carried only by grammar
/// version 3 and later.
/// </param>
public sealed record ConnectorRule(
    uint MinCount,
    uint MaxCount,
    TransformRange Transform,
    IReadOnlyList<CategoryChoice> Choices,
    uint SpacingRatio = 0,
    string BandScale = "",
    IReadOnlyList<string>? BandTags = null,
    long BandBase = 0,
    bool InheritsBandTag = false)
{
    /// <summary>The unit of <see cref="SpacingRatio"/>: a ratio of one.</summary>
    public const uint RatioUnit = 256;

    /// <summary>The first grammar version that carries a spacing ratio.</summary>
    public const uint FirstVersionWithSpacing = 2;

    /// <summary>The first grammar version that carries a band scale and per-band tags.</summary>
    public const uint FirstVersionWithBands = 3;

    /// <summary>The tags per band, one per band this rule admits, or empty where no band demands one.</summary>
    public IReadOnlyList<string> BandTags { get; } = [.. BandTags ?? []];

    /// <summary>The tag the child at <paramref name="index"/> must provide beyond what the parent requires, or null.</summary>
    public string? BandTag(int index) =>
        index < this.BandTags.Count && this.BandTags[index].Length != 0 ? this.BandTags[index] : null;

    /// <summary>
    /// The bounds the child at <paramref name="index"/> draws its semi-major axis between: its band of
    /// the progression when this rule has a ratio, and the whole declared range when it has none.
    /// <paramref name="scale"/> multiplies the band, in units of the band-scale rule, and stays inside
    /// the declared range whatever it is.
    /// </summary>
    public (long Min, long Max) OrbitBand(int index, long scale = Derivation.DerivationRules.OrbitScaleUnit)
    {
        (long min, long max) = Transform.Bounds[0];
        if (SpacingRatio == 0)
        {
            return (min, max);
        }

        long low = BandBase == 0 ? min : BandBase;
        for (int step = 0; step < index; step++)
        {
            low = Step(low);
        }

        long high = Step(low);
        return (Clamp(low, scale, min, max), Clamp(high, scale, min, max));
    }

    private long Step(long value) => value / RatioUnit * SpacingRatio;

    private static long Clamp(long value, long scale, long min, long max) =>
        Math.Clamp(value / Derivation.DerivationRules.OrbitScaleUnit * scale, min, max);
}

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
        Production[] orderedProductions = ValidateProductions(registry, productions, version);
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
                CategoryChoice[] choices = ReadChoices(reader);
                uint spacing = expectedVersion >= ConnectorRule.FirstVersionWithSpacing ? reader.ReadUInt32() : 0;

                string scale = string.Empty;
                long bandBase = 0;
                bool inheritsBandTag = false;
                var tags = new List<string>();
                if (expectedVersion >= ConnectorRule.FirstVersionWithBands)
                {
                    string named = reader.ReadPath();
                    scale = named == "none" ? string.Empty : named;
                    bandBase = reader.ReadVarInt();

                    byte inherits = reader.ReadUInt8();
                    if (inherits > 1)
                    {
                        throw new FormatException($"A rule's inherit flag must be 0 or 1; found {inherits}.");
                    }

                    inheritsBandTag = inherits == 1;

                    int tagCount = reader.ReadCount();
                    if (tagCount > ConnectorKind.MaxChildCategories)
                    {
                        throw new FormatException($"A rule declares {tagCount} band tags; the bound is {ConnectorKind.MaxChildCategories}.");
                    }

                    for (int tag = 0; tag < tagCount; tag++)
                    {
                        tags.Add(reader.ReadText());
                    }
                }

                rules[rule] = new ConnectorRule(minCount, maxCount, transform, choices, spacing, scale, tags, bandBase, inheritsBandTag);
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

    private static Production[] ValidateProductions(CategoryRegistry registry, IReadOnlyList<Production> productions, uint version)
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
                ValidateRule(production.ConnectorRules[rule], schema, schema.Connectors[rule], version, nameof(productions));
            }
        }

        return ordered;
    }

    private static void ValidateRule(ConnectorRule rule, CategoryDefinition schema, ConnectorKind connector, uint version, string parameterName)
    {
        string where = $"'{schema.Label}/{connector.Label}'";

        if (rule.SpacingRatio != 0)
        {
            if (version < ConnectorRule.FirstVersionWithSpacing)
            {
                throw new ArgumentException($"Rule {where} carries a spacing ratio, which no grammar before version {ConnectorRule.FirstVersionWithSpacing} encodes.", parameterName);
            }

            if (connector.Transform != TransformKind.OrbitalElements)
            {
                throw new ArgumentException($"Rule {where} carries a spacing ratio, which only an orbit connector has a semi-major axis to space.", parameterName);
            }

            if (rule.SpacingRatio <= ConnectorRule.RatioUnit)
            {
                throw new ArgumentException($"Rule {where} has a spacing ratio of {rule.SpacingRatio}/{ConnectorRule.RatioUnit}, which does not grow.", parameterName);
            }

            (long low, long high) = rule.OrbitBand(rule.MaxCount == 0 ? 0 : (int)rule.MaxCount - 1);
            if (high >= rule.Transform.Bounds[0].Max && low >= rule.Transform.Bounds[0].Max)
            {
                throw new ArgumentException($"Rule {where} spaces {rule.MaxCount} orbits past its own maximum of {rule.Transform.Bounds[0].Max} m; widen the range or lower the ratio.", parameterName);
            }

            if (rule.BandBase != 0 && (rule.BandBase < rule.Transform.Bounds[0].Min || rule.BandBase > rule.Transform.Bounds[0].Max))
            {
                throw new ArgumentException($"Rule {where} begins its bands at {rule.BandBase} m, outside the range they must stay inside.", parameterName);
            }
        }

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

        if (rule.BandScale.Length != 0 || rule.BandTags.Count != 0 || rule.BandBase != 0 || rule.InheritsBandTag)
        {
            if (version < ConnectorRule.FirstVersionWithBands)
            {
                throw new ArgumentException($"Rule {where} carries a band scale or band tags, which no grammar before version {ConnectorRule.FirstVersionWithBands} encodes.", parameterName);
            }

            if (connector.Transform != TransformKind.OrbitalElements)
            {
                throw new ArgumentException($"Rule {where} carries a band scale or band tags, which only an orbit connector has bands for.", parameterName);
            }
        }

        if (rule.BandScale.Length != 0 && !Derivation.BandScaleRules.IsKnown(rule.BandScale))
        {
            throw new ArgumentException($"Rule {where} names band-scale rule '{rule.BandScale}', which this build does not retain (decision 0035).", parameterName);
        }

        if (rule.BandTags.Count != 0 && rule.BandTags.Count != rule.MaxCount)
        {
            throw new ArgumentException($"Rule {where} gives {rule.BandTags.Count} band tags for {rule.MaxCount} bands; it declares one per band or none at all.", parameterName);
        }

        foreach (string tag in rule.BandTags.Where(tag => tag.Length != 0))
        {
            StreamPath.Validate(tag);
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

                // A record's domain label carries its version, so a later version may add a field without
                // moving an earlier version's bytes; the reader dispatches on the label it opened.
                if (Version >= ConnectorRule.FirstVersionWithSpacing)
                {
                    writer.WriteUInt32(rule.SpacingRatio);
                }

                if (Version >= ConnectorRule.FirstVersionWithBands)
                {
                    writer.WritePath(rule.BandScale.Length == 0 ? "none" : rule.BandScale);
                    writer.WriteVarInt(rule.BandBase);
                    writer.WriteUInt8(rule.InheritsBandTag ? (byte)1 : (byte)0);
                    writer.WriteCount(rule.BandTags.Count);
                    foreach (string tag in rule.BandTags)
                    {
                        writer.WriteText(tag);
                    }
                }
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
