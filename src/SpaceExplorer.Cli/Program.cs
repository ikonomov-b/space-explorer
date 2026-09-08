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
    CompositionGrammar grammar = CompositionGrammarVersion1.Grammar;
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
            Console.WriteLine($"    {category.Connectors[index].Label,-16} [{rule.MinCount}, {rule.MaxCount}] {rule.Transform.Kind} {Choices(rule.Choices, registry)}");
        }
    }

    return 0;
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
    CompositionGrammar grammar = CompositionGrammarVersion1.Grammar;
    PrimitiveSet source = SetLoader.Load(root, set, CategoryRegistries.Supported);
    CategoryRegistry registry = CategoryRegistries.Supported.Find(source.Manifest.RegistryRevision);

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
    CompositionGraph graph = GraphLoader.Load(Root(options), PackId.Parse(packText), CategoryRegistries.Supported, CompositionGrammarVersion1.Grammar);
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
    CompositionGraph graph = GraphLoader.Load(Root(options), PackId.Parse(packText), CategoryRegistries.Supported, CompositionGrammarVersion1.Grammar);
    Console.WriteLine($"OK graph {graph.Pack}: {graph.NodeCount} instances verified against the record hash, the pinned set, the registry, and the connector rules.");
    return 0;
}

static int Describe(string packText, string[] options)
{
    DistanceTier tier = Tier(options);
    CompositionGraph graph = GraphLoader.Load(Root(options), PackId.Parse(packText), CategoryRegistries.Supported, CompositionGrammarVersion1.Grammar);
    CategoryRegistry registry = CategoryRegistries.Supported.Find(graph.Specification.RegistryRevision);
    SystemDescription description = SystemDescription.Derive(graph, registry, SuitProfile.Version1);

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
    CompositionGrammar grammar = CompositionGrammarVersion1.Grammar;
    SuitProfile suit = SuitProfile.Version1;
    PrimitiveSet source = SetLoader.Load(root, set, CategoryRegistries.Supported);
    CategoryRegistry registry = CategoryRegistries.Supported.Find(source.Manifest.RegistryRevision);

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
        SystemDescription description = SystemDescription.Derive(CompositionGenerator.Generate(specification, source, grammar, registry), registry, suit);

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

static string Verdict(SystemDescription description, DistanceTier tier)
{
    IReadOnlyList<string> failures = TierRules.Check(description, tier);
    return failures.Count == 0
        ? $"tier          pass ({TierRules.Label(tier)})"
        : $"tier          fail ({TierRules.Label(tier)}): {string.Join("; ", failures)}";
}

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
    CategoryRegistry registry = CategoryRegistryRevision1.Registry;
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
