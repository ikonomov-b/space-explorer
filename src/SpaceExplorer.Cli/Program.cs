using System.Runtime.InteropServices;
using SpaceExplorer.Cli;
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
        ["generate-set", .. string[] rest] => GenerateSet(rest),
        ["list", .. string[] rest] => List(rest),
        ["inspect", string pack, .. string[] rest] => Inspect(pack, rest),
        ["validate", string pack, .. string[] rest] => Validate(pack, rest),
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
    Console.WriteLine("  generate-set --vocabulary <json> --seed <n> [--retries <n>] [--data-root <dir>]");
    Console.WriteLine("                                      Generate a set from an authored vocabulary and publish it.");
    Console.WriteLine("  list [--data-root <dir>]            List published packs.");
    Console.WriteLine("  inspect <pack-id> [--data-root <dir>]   Load a pack without generating and print its definitions.");
    Console.WriteLine("  validate <pack-id> [--data-root <dir>]  Load and verify a pack; exit 0 when it is intact.");
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
        Console.WriteLine($"{pack} manifest {manifest} definitions {count}");
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
