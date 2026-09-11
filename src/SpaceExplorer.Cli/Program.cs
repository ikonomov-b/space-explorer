using System.Runtime.InteropServices;
using SpaceExplorer.Cli;
using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using SpaceExplorer.Persistence;

// Primitive-set generator, validator, and inspector (M0a). Every command works without Godot; player data
// goes to the data root of decision 0018, overridable with SPACE_EXPLORER_DATA_DIR or --data-root.
try
{
    return args switch
    {
        [] or ["--help" or "-h"] => Usage(),
        ["diagnostics"] => Diagnostics(),
        ["registry"] => Registry(),
        ["grammar"] => Grammar(),
        ["generate-set", .. string[] rest] => GenerateSet(rest),
        ["list", .. string[] rest] => List(rest),
        ["inspect", string pack, .. string[] rest] => Inspect(pack, rest),
        ["validate", string pack, .. string[] rest] => Validate(pack, rest),
        ["compose", .. string[] rest] => Compose(rest),
        ["inspect-graph", string pack, .. string[] rest] => InspectGraph(pack, rest),
        ["validate-graph", string pack, .. string[] rest] => ValidateGraph(pack, rest),
        ["describe", string pack, .. string[] rest] => Describe(pack, rest),
        ["iterate", .. string[] rest] => Iterate(rest),
        ["destination", .. string[] rest] => Destination(rest),
        _ => Unknown(args[0]),
    };
}
catch (Exception exception) when (exception is FormatException or ArgumentException or GenerationException or ReproductionFailureException or PackageIntegrityException or PackageNotFoundException or CompatibilityException or IOException)
{
    Console.Error.WriteLine($"error: {exception.Message}");
    return 1;
}

static int Usage()
{
    Console.WriteLine("Usage: SpaceExplorer.Cli <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  diagnostics                         Print runtime, platform, and SQLite library versions.");
    Console.WriteLine("  registry                            Print every supported category registry revision, hash, and categories.");
    Console.WriteLine("  grammar                             Print the composition grammar version, hash, bounds, and rules.");
    Console.WriteLine("  generate-set --vocabulary <json> --seed <n> [--retries <n>] [--data-root <dir>]");
    Console.WriteLine("                                      Generate a set from an authored vocabulary and publish it.");
    Console.WriteLine("  list [--data-root <dir>]            List published packs.");
    Console.WriteLine("  inspect <pack-id> [--data-root <dir>]   Load a pack without generating and print its definitions.");
    Console.WriteLine("  validate <pack-id> [--data-root <dir>]  Load and verify a pack; exit 0 when it is intact.");
    Console.WriteLine("  compose --set <pack-id> --domain <name> --seed <n> [--data-root <dir>]");
    Console.WriteLine("                                      Compose a graph from a published set and publish it.");
    Console.WriteLine("  inspect-graph <pack-id> [--data-root <dir>]  Load a graph without composing and print its instances.");
    Console.WriteLine("  validate-graph <pack-id> [--data-root <dir>] Load and verify a graph; exit 0 when it is intact.");
    Console.WriteLine("  describe <pack-id> [--tier <name>] [--data-root <dir>]");
    Console.WriteLine("                                      Print the system description derived from a published graph.");
    Console.WriteLine("  iterate --set <pack-id> --seeds <a-b> [--tier <name>] [--data-root <dir>]");
    Console.WriteLine("                                      Compose one system per seed without publishing and describe each.");
    Console.WriteLine("  destination --tier <name> --seed <n> [--no-publish] [--set <pack-id>] [--data-root <dir>]");
    Console.WriteLine("                                      Load or compose the destination the two levers name, publish it, and describe it.");
    return 0;
}

static int Diagnostics()
{
    Console.WriteLine($"runtime  {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"platform {RuntimeInformation.RuntimeIdentifier} ({RuntimeInformation.OSDescription})");
    Console.WriteLine($"sqlite   {SqliteRuntime.GetLibraryVersion()}");
    return 0;
}

static int Registry()
{
    foreach (CategoryRegistry registry in CategoryRegistries.Supported.Registries)
    {
        Console.WriteLine($"revision {registry.Revision}");
        Console.WriteLine($"hash     {registry.Hash}");
        Console.WriteLine($"domain   {registry.Domain}");
        foreach (CategoryDefinition category in registry.Categories)
        {
            Console.WriteLine($"  {category.Id,3} {category.Label,-18} domains {string.Join(",", category.Domains)}; {category.Parameters.Count} parameters; {category.Connectors.Count} connectors; policies {category.PermittedPolicies}");
        }
    }

    return 0;
}

static int Grammar()
{
    foreach (CompositionGrammar supported in CompositionGrammars.Supported.Grammars)
    {
        PrintGrammar(supported);
    }

    return 0;
}

static void PrintGrammar(CompositionGrammar grammar)
{
    CategoryRegistry registry = CategoryRegistries.Supported.Find(grammar.RegistryRevision);
    Console.WriteLine($"version  {grammar.Version}");
    Console.WriteLine($"hash     {grammar.Hash}");
    Console.WriteLine($"domain   {registry.GrammarDomain(grammar.Version)}");
    Console.WriteLine($"bounds   depth {grammar.MaxDepth}; instances {grammar.MaxNodes}; retries {grammar.RetryBudget}");
    foreach (RootRule root in grammar.Roots)
    {
        Console.WriteLine($"  root {CompositionDomains.Label(root.Domain),-14} {Choices(root.Choices, registry)}");
    }

    foreach (Production production in grammar.Productions)
    {
        CategoryDefinition category = registry.Find(production.Category);
        Console.WriteLine($"  {category.Label,-18} needs depth {grammar.RequiredDepth(production.Category)}, {grammar.RequiredNodes(production.Category)} instances");
        for (int index = 0; index < production.ConnectorRules.Count; index++)
        {
            ConnectorRule rule = production.ConnectorRules[index];
            string spacing = rule.SpacingRatio == 0 ? string.Empty : $"; spacing {rule.SpacingRatio}/{ConnectorRule.RatioUnit}";
            Console.WriteLine($"    {category.Connectors[index].Label,-16} [{rule.MinCount}, {rule.MaxCount}] {rule.Transform.Kind} {Choices(rule.Choices, registry)}{spacing}");
        }
    }
}

static string Choices(IReadOnlyList<CategoryChoice> choices, CategoryRegistry registry) =>
    string.Join(", ", choices.Select(choice => $"{registry.Find(choice.Category).Label} x{choice.Weight}"));

static int Compose(string[] options)
{
    PackId set = PackId.Parse(Option(options, "--set") ?? throw new ArgumentException("compose needs --set <pack-id>."));
    string domainText = Option(options, "--domain") ?? throw new ArgumentException($"compose needs --domain <{string.Join("|", CompositionDomains.Labels)}>.");
    CompositionDomain domain = CompositionDomains.TryParse(domainText) ?? throw new ArgumentException($"'{domainText}' is not a composition domain; one of {string.Join(", ", CompositionDomains.Labels)}.");
    ulong seed = ulong.Parse(Option(options, "--seed") ?? throw new ArgumentException("compose needs --seed <n>."), System.Globalization.CultureInfo.InvariantCulture);

    DataRoot root = Root(options);
    PrimitiveSet source = SetLoader.Load(root, set, CategoryRegistries.Supported);
    CategoryRegistry registry = CategoryRegistries.Supported.Find(source.Manifest.RegistryRevision);
    CompositionGrammar grammar = GrammarFor(registry);

    GraphSpecification specification = GraphSpecification.Create(
        registry.Revision, registry.Hash, GeneratorVersion.Current, grammar.Version, grammar.Hash, seed, source.Manifest.Pack, source.Manifest.Hash, domain);
    CompositionGraph graph = CompositionGenerator.Generate(specification, source, grammar, registry);
    GraphPublishResult result = GraphPublisher.Publish(root, graph);

    Console.WriteLine($"data-root     {root.Path}");
    Console.WriteLine($"set           {source.Manifest.Pack} {source.Manifest.Hash}");
    Console.WriteLine($"grammar       {grammar.Version} {grammar.Hash}");
    Console.WriteLine($"specification {specification.Hash}");
    Console.WriteLine($"pack          {result.Pack}");
    Console.WriteLine($"graph         {result.GraphHash}");
    Console.WriteLine($"instances     {graph.NodeCount} in domain {domainText}, depth {graph.Depth}");
    Console.WriteLine(result.AlreadyPublished ? "status        already published; nothing written" : $"status        published; {result.RecordsWritten} record written");
    return 0;
}

static int InspectGraph(string packText, string[] options)
{
    CompositionGraph graph = LoadGraph(packText, options);
    CategoryRegistry registry = CategoryRegistries.Supported.Find(graph.Specification.RegistryRevision);

    Console.WriteLine($"pack          {graph.Pack}");
    Console.WriteLine($"graph         {graph.Hash}");
    Console.WriteLine($"specification {graph.Specification.Hash}");
    Console.WriteLine($"set           {graph.Specification.SourcePack} {graph.Specification.SourceManifestHash}");
    Console.WriteLine($"registry      {graph.Specification.RegistryRevision} {graph.Specification.RegistryHash}");
    Console.WriteLine($"grammar       {graph.Specification.GrammarVersion} {graph.Specification.GrammarHash}");
    Console.WriteLine($"seed          {graph.Specification.Seed}; instances {graph.NodeCount}; depth {graph.Depth}");
    foreach (GraphNode node in graph.Nodes)
    {
        CategoryDefinition category = registry.Find(node.Definition.Category);
        string indent = new(' ', 2 * (int)node.Depth);
        Console.WriteLine($"  {indent}{node.Path} {category.Label} #{node.Definition.Id.LocalId}{Attachment(node)}");
    }

    return 0;
}

static string Attachment(GraphNode node)
{
    if (node.Transform is not { } transform)
    {
        return string.Empty;
    }

    IReadOnlyList<TransformComponent> schema = TransformSchema.For(transform.Kind);
    IEnumerable<string> shown = schema
        .Select((component, index) => (component.Label, Value: transform.Components[index]))
        .Where(pair => pair.Value != 0)
        .Select(pair => $"{pair.Label}={pair.Value}");
    return $" [{transform.Kind}: {string.Join(", ", shown)}]";
}

static int ValidateGraph(string packText, string[] options)
{
    CompositionGraph graph = LoadGraph(packText, options);
    Console.WriteLine($"OK graph {graph.Pack}: {graph.NodeCount} instances verified against the record hash, the pinned set, the registry, and the connector rules.");
    return 0;
}

static int Describe(string packText, string[] options)
{
    DistanceTier tier = Tier(options);
    CompositionGraph graph = LoadGraph(packText, options);
    CategoryRegistry registry = CategoryRegistries.Supported.Find(graph.Specification.RegistryRevision);
    SystemDescription description = SystemDescription.Derive(graph, registry, SuitProfile.Version1, RegionLimits.Version1);

    Console.Write(description.Text);
    Console.WriteLine(Verdict(description, tier));
    return 0;
}

static int Iterate(string[] options)
{
    PackId set = PackId.Parse(Option(options, "--set") ?? throw new ArgumentException("iterate needs --set <pack-id>."));
    (ulong first, ulong last) = Seeds(Option(options, "--seeds") ?? throw new ArgumentException("iterate needs --seeds <first>-<last>."));
    DistanceTier tier = Tier(options);

    DataRoot root = Root(options);
    SuitProfile suit = SuitProfile.Version1;
    TierProfile tiers = TierProfile.Version1;
    RegionLimits regions = RegionLimits.Version1;
    PrimitiveSet source = SetLoader.Load(root, set, CategoryRegistries.Supported);
    CategoryRegistry registry = CategoryRegistries.Supported.Find(source.Manifest.RegistryRevision);
    CompositionGrammar grammar = GrammarFor(registry);

    Console.WriteLine($"iteration over seeds {first} to {last}, tier {TierRules.Label(tier)}");
    Console.WriteLine($"pinned        registry {registry.Revision} {registry.Hash}");
    Console.WriteLine($"              vocabulary {source.Manifest.VocabularyHash}");
    Console.WriteLine($"              grammar {grammar.Version} {grammar.Hash}");
    Console.WriteLine($"              generator {GeneratorVersion.Current}; description {SystemDescription.Version}; suit profile {suit.Version} {suit.Hash}");
    Console.WriteLine($"              set {source.Manifest.Pack} {source.Manifest.Hash}");

    // Systems generated in an iteration are disposable and are never kept destinations (decision 0047),
    // so nothing here is published.
    int passed = 0;
    int planets = 0;
    int moons = 0;
    int candidates = 0;
    var refusals = new Dictionary<string, int>(StringComparer.Ordinal);
    for (ulong seed = first; seed <= last; seed++)
    {
        GraphSpecification specification = GraphSpecification.Create(
            registry.Revision, registry.Hash, GeneratorVersion.Current, grammar.Version, grammar.Hash, seed, source.Manifest.Pack, source.Manifest.Hash, CompositionDomain.SolarSystem);
        SystemDescription description = SystemDescription.Derive(CompositionGenerator.Generate(specification, source, grammar, registry), registry, suit, RegionLimits.Version1);

        Console.WriteLine();
        Console.Write(description.Text);
        string verdict = Verdict(description, tier);
        Console.WriteLine(verdict);
        passed += verdict.StartsWith("tier          pass", StringComparison.Ordinal) ? 1 : 0;

        planets += description.PlanetCount;
        moons += description.MoonCount;
        candidates += description.LandingCandidateCount;
        foreach (string refusal in description.Bodies.SelectMany(body => body.Refusals))
        {
            refusals[refusal] = refusals.TryGetValue(refusal, out int seen) ? seen + 1 : 1;
        }
    }

    ulong count = last - first + 1;
    Console.WriteLine();
    Console.WriteLine($"summary       {passed} of {count} systems satisfy the {TierRules.Label(tier)} tier's rules");
    Console.WriteLine($"bodies        {planets} planets, {moons} moons; {candidates} landing candidates");
    Console.WriteLine($"refused by    {string.Join(", ", refusals.OrderByDescending(entry => entry.Value).ThenBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key} {entry.Value}"))}");
    return 0;
}

static int Destination(string[] options)
{
    DistanceTier tier = Tier(options);
    ulong seed = ulong.Parse(Option(options, "--seed") ?? throw new ArgumentException("destination needs --seed <n>."), System.Globalization.CultureInfo.InvariantCulture);

    DataRoot root = Root(options);
    PrimitiveSet source = SourceSet(root, options);
    CategoryRegistry registry = CategoryRegistries.Supported.Find(source.Manifest.RegistryRevision);
    CompositionGrammar grammar = GrammarFor(registry);
    SuitProfile suit = SuitProfile.Version1;
    TierProfile tiers = TierProfile.Version1;
    RegionLimits regions = RegionLimits.Version1;

    // The two levers name their destination's pack before anything is composed, so a destination already
    // stored is read rather than drawn again, and its bounded retry is paid once (decision 0053).
    DestinationSpecification specification = DestinationSpecification.For(tier, seed, source, grammar, registry, suit, tiers, regions);
    DestinationRecord? stored = DestinationStore.Find(root, specification);

    string status;
    Destination destination;
    if (stored is not null)
    {
        CompositionGraph graph = GraphLoader.Load(root, stored.GraphPack, CategoryRegistries.Supported, grammar, source);
        destination = new Destination(tier, seed, stored.Attempt, stored.CompositionSeed, graph, SystemDescription.Derive(graph, registry, suit, RegionLimits.Version1));
        status = "loaded from the data root; nothing composed";
    }
    else
    {
        destination = DestinationComposer.Compose(tier, seed, source, grammar, registry, suit, tiers, regions);
        if (Array.IndexOf(options, "--no-publish") >= 0)
        {
            status = "composed; not published";
        }
        else
        {
            GraphPublisher.Publish(root, destination.Graph);

            // Every region's ground before the destination row names the world, so a stored destination is
            // never one whose ground is missing (decision 0067).
            GroundPublishResult ground = GroundPublisher.Publish(root, destination.Graph, registry);
            DestinationPublishResult published = DestinationStore.Publish(root, DestinationRecord.Of(destination, source, grammar, registry, suit, tiers, regions));
            // Invariant, like every other figure this tool prints: under a decimal-comma locale the size
            // would read "10,70 MiB" beside an invariant "0.534 AU", and a sweep parsing it would break by
            // machine rather than by input.
            Console.WriteLine(FormattableString.Invariant($"ground        {ground.Regions} region(s), {ground.Generated} generated, {ground.BytesWritten / 1024.0 / 1024.0:0.00} MiB written"));
            status = published.AlreadyPublished ? "composed; already published" : "composed and published";
        }
    }

    Console.WriteLine($"destination   tier {TierRules.Label(tier)}, seed {seed}");
    Console.WriteLine($"pack          {specification.PackId}");
    Console.WriteLine($"drawn         attempt {destination.Attempt + 1} of at most {DestinationComposer.MaxAttempts}; composition seed {destination.CompositionSeed}");
    Console.WriteLine($"set           {source.Manifest.Pack} {source.Manifest.Hash}");
    Console.Write(destination.Description.Text);
    Console.WriteLine(Verdict(destination.Description, tier));
    Console.WriteLine($"status        {status}");
    return 0;
}

/// <summary>The set a composition draws from: the one named, or the only one published under a supported revision.</summary>
static PrimitiveSet SourceSet(DataRoot root, string[] options)
{
    if (Option(options, "--set") is { } named)
    {
        return SetLoader.Load(root, PackId.Parse(named), CategoryRegistries.Supported);
    }

    IReadOnlyList<(PackId Pack, ContentHash Manifest, int Count)> published = SetLoader.List(root);
    return published.Count == 1
        ? SetLoader.Load(root, published[0].Pack, CategoryRegistries.Supported)
        : throw new ArgumentException(published.Count == 0
            ? $"No set is published in {root.Path}; run generate-set first."
            : $"{published.Count} sets are published in {root.Path}; name one with --set <pack-id>. Run list to see them.");
}

static string Verdict(SystemDescription description, DistanceTier tier)
{
    IReadOnlyList<string> failures = TierRules.Check(description, tier, TierProfile.Version1);
    return failures.Count == 0
        ? $"tier          pass ({TierRules.Label(tier)})"
        : $"tier          fail ({TierRules.Label(tier)}): {string.Join("; ", failures)}";
}

/// <summary>Loads a published graph under whichever grammar version it pins.</summary>
static CompositionGraph LoadGraph(string packText, string[] options)
{
    DataRoot root = Root(options);
    PackId pack = PackId.Parse(packText);
    uint version = GraphLoader.List(root).FirstOrDefault(entry => entry.Pack == pack)?.GrammarVersion
        ?? throw new PackageNotFoundException(pack);

    return GraphLoader.Load(root, pack, CategoryRegistries.Supported, CompositionGrammars.Supported.Find(version));
}

/// <summary>The grammar a new graph over <paramref name="registry"/> is composed under.</summary>
static CompositionGrammar GrammarFor(CategoryRegistry registry) =>
    CompositionGrammars.Supported.Newest(registry.Revision)
        ?? throw new ArgumentException($"This build has no composition grammar over category-registry revision {registry.Revision}.");

static DistanceTier Tier(string[] options)
{
    string label = Option(options, "--tier") ?? "starter";
    return TierRules.TryParse(label) ?? throw new ArgumentException($"'{label}' is not a distance tier; one of {string.Join(", ", TierRules.Labels)}.");
}

static (ulong First, ulong Last) Seeds(string text)
{
    string[] parts = text.Split('-', 2);
    ulong first = ulong.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
    ulong last = parts.Length == 2 ? ulong.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : first;
    return last >= first ? (first, last) : throw new ArgumentException($"The seed range {text} runs backwards.");
}

static int GenerateSet(string[] options)
{
    string vocabularyPath = Option(options, "--vocabulary") ?? throw new ArgumentException("generate-set needs --vocabulary <json>.");
    ulong seed = ulong.Parse(Option(options, "--seed") ?? throw new ArgumentException("generate-set needs --seed <n>."), System.Globalization.CultureInfo.InvariantCulture);

    // A new set is generated under the newest revision this build supports; the vocabulary is authored
    // for it, and packs published under an earlier revision keep loading (decision 0035).
    CategoryRegistry registry = CategoryRegistries.Supported.Registries[^1];
    VocabularyFile file = VocabularyFile.Read(vocabularyPath, registry);
    uint retries = Option(options, "--retries") is { } r ? uint.Parse(r, System.Globalization.CultureInfo.InvariantCulture) : file.Retries;

    SetSpecification specification = SetSpecification.Create(registry.Revision, registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, seed, file.Vocabulary.Pack, file.Vocabulary.Hash, file.Requests, retries);
    PrimitiveSet set = SetGenerator.Generate(specification, file.Vocabulary, registry);
    DataRoot root = Root(options);
    PublishResult result = SetPublisher.Publish(root, set);

    Console.WriteLine($"data-root     {root.Path}");
    Console.WriteLine($"vocabulary    {file.Vocabulary.Pack} {file.Vocabulary.Hash}");
    Console.WriteLine($"specification {specification.Hash}");
    Console.WriteLine($"pack          {result.Pack}");
    Console.WriteLine($"manifest      {result.ManifestHash}");
    Console.WriteLine($"definitions   {set.Definitions.Count} (candidates tried {set.Manifest.Validation.CandidatesTried}, rejected {set.Manifest.Validation.CandidatesRejected})");
    Console.WriteLine(result.AlreadyPublished ? "status        already published; nothing written" : $"status        published; {result.RecordsWritten} records written");
    return 0;
}

static int List(string[] options)
{
    DataRoot root = Root(options);
    foreach ((PackId pack, ContentHash manifest, int count) in SetLoader.List(root))
    {
        Console.WriteLine($"set   {pack} manifest {manifest} definitions {count}");
    }

    foreach (GraphEntry entry in GraphLoader.List(root))
    {
        Console.WriteLine($"graph {entry.Pack} record   {entry.GraphHash} instances {entry.NodeCount} domain {CompositionDomains.Label(entry.Domain)} set {entry.SourcePack}");
    }

    foreach (DestinationEntry entry in DestinationStore.List(root))
    {
        Console.WriteLine($"dest  {entry.Pack} record   {entry.RecordHash} tier {TierRules.Label(entry.Tier)} seed {entry.Seed} attempt {entry.Attempt + 1} graph {entry.GraphPack}");
    }

    return 0;
}

static int Inspect(string packText, string[] options)
{
    PrimitiveSet set = SetLoader.Load(Root(options), PackId.Parse(packText), CategoryRegistries.Supported);
    SetManifest manifest = set.Manifest;
    CategoryRegistry registry = CategoryRegistries.Supported.Find(manifest.RegistryRevision);
    Console.WriteLine($"pack          {manifest.Pack}");
    Console.WriteLine($"manifest      {manifest.Hash}");
    Console.WriteLine($"specification {manifest.SpecificationHash}");
    Console.WriteLine($"registry      {manifest.RegistryRevision} {manifest.RegistryHash}");
    Console.WriteLine($"vocabulary    {manifest.TemplatePack} {manifest.VocabularyHash}");
    Console.WriteLine($"generator     {manifest.GeneratorVersion}; grammar {manifest.GrammarVersion}; accepted {manifest.Validation.Accepted}");
    foreach (PrimitiveDefinition definition in set.Definitions)
    {
        CategoryDefinition category = registry.Find(definition.Category);
        string values = string.Join(", ", category.Parameters.Zip(definition.Parameters, (descriptor, value) => $"{descriptor.Label}={Format(value, descriptor)}"));
        Console.WriteLine($"  {definition.Id.LocalId,4} {category.Label,-18} {definition.Hash.ToString()[..12]} template {definition.Provenance.TemplateId}: {values}");
    }

    return 0;
}

static int Validate(string packText, string[] options)
{
    PrimitiveSet set = SetLoader.Load(Root(options), PackId.Parse(packText), CategoryRegistries.Supported);
    Console.WriteLine($"OK pack {set.Manifest.Pack}: manifest and {set.Definitions.Count} definitions verified against their hashes, the registry, and each other.");
    return 0;
}

static string Format(ParameterValue value, ParameterDescriptor descriptor) => value.Kind switch
{
    ParameterKind.Bool => value.AsBool ? "true" : "false",
    ParameterKind.Enum => descriptor.EnumLabels[(int)value.AsEnumIndex],
    ParameterKind.Integer => descriptor.FractionBits == 0 ? value.AsInteger.ToString(System.Globalization.CultureInfo.InvariantCulture) : $"{value.AsInteger}/2^{descriptor.FractionBits}",
    ParameterKind.BinaryTurn => $"{value.AsBinaryTurn}t",
    ParameterKind.Rotation => $"({value.AsRotation.Yaw},{value.AsRotation.Pitch},{value.AsRotation.Roll})t",
    ParameterKind.Vector3 => $"({value.AsVector3.X},{value.AsVector3.Y},{value.AsVector3.Z})",
    ParameterKind.Colour => $"#{value.AsColour.Red:x2}{value.AsColour.Green:x2}{value.AsColour.Blue:x2}{value.AsColour.Alpha:x2}",
    ParameterKind.PrimitiveRef => $"->{value.AsRef.Id.LocalId}",
    _ => "?",
};

static DataRoot Root(string[] options) => Option(options, "--data-root") is { } path ? DataRoot.At(path) : DataRoot.Resolve();

static string? Option(string[] options, string name)
{
    int index = Array.IndexOf(options, name);
    return index >= 0 && index + 1 < options.Length ? options[index + 1] : null;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Run with --help for usage.");
    return 2;
}
