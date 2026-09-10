// Data-root statistics: what the primitive database holds, and what the structures built on it cost.
//
//   dotnet run tools/stats.cs                       report on the data root of decision 0018
//   dotnet run tools/stats.cs -- --data-root <dir>  report on another one, as the tool takes it
//
// Every count comes from the command-line tool's own reload path -- `list`, `inspect` and `inspect-graph`,
// which verify each record against its hash before decoding it -- and every size is the record file on disk.
// Nothing is decoded a second time here and nothing is regenerated, so a figure in this report cannot
// disagree with what the tool loads; where a parsed count and the tool's own count differ, the run fails
// rather than reporting the difference. Needs only the pinned .NET SDK: it builds the tool once and then
// calls its assembly directly, because a report over every pack makes one call per pack.

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

// Every figure is formatted invariantly, so a report is comparable between machines and locales.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

string repository = RepositoryRoot();
string? given = Option(args, "--data-root");
string dataRoot = given ?? DefaultDataRoot();
string[] rootArgs = given is null ? [] : ["--data-root", given];

if (!Directory.Exists(Path.Combine(dataRoot, "packs")))
{
    Console.Error.WriteLine($"error: no pack directory under {dataRoot}; nothing has been published there.");
    return 1;
}

string tool = CommandLineTool(repository);
Dictionary<string, List<long>> packFiles = PackFiles(dataRoot);
List<string[]> listing = Lines(Run(tool, ["list", .. rootArgs])).Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();

List<SetPack> sets = [];
foreach (string[] row in listing.Where(r => r[0] == "set"))
{
    string pack = row[1];
    string[] output = Lines(Run(tool, ["inspect", pack, .. rootArgs]));
    Dictionary<string, string> header = Header(output);
    List<Definition> definitions = [];
    foreach (string line in output)
    {
        Match match = Regex.Match(line, @"^\s+(?<id>\d+) (?<category>\S+)\s+(?<hash>[0-9a-f]{12}) template (?<template>\d+):(?<rest>.*)$");
        if (match.Success)
        {
            definitions.Add(new Definition(
                int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture),
                match.Groups["category"].Value,
                match.Groups["hash"].Value,
                int.Parse(match.Groups["template"].Value, CultureInfo.InvariantCulture),
                Regex.Matches(match.Groups["rest"].Value, @"=->\d+").Count,
                SizeOf(packFiles, dataRoot, pack, match.Groups["hash"].Value)));
        }
    }
    int claimed = int.Parse(Field(row, "definitions"), CultureInfo.InvariantCulture);
    if (definitions.Count != claimed)
    {
        Console.Error.WriteLine($"error: pack {pack} lists {claimed} definitions and {definitions.Count} were read from `inspect`; the output format changed.");
        return 1;
    }
    sets.Add(new SetPack(pack, header["manifest"], int.Parse(header["registry"].Split(' ')[0], CultureInfo.InvariantCulture), definitions,
        SizeOf(packFiles, dataRoot, pack, header["manifest"]), packFiles[pack].Sum(), packFiles[pack].Count));
}

List<Structure> structures = [];
foreach (string[] row in listing.Where(r => r[0] == "graph"))
{
    string pack = row[1];
    string[] output = Lines(Run(tool, ["inspect-graph", pack, .. rootArgs]));
    Dictionary<string, string> header = Header(output);
    List<Instance> instances = [];
    foreach (string line in output)
    {
        Match match = Regex.Match(line, @"^\s+(?<path>[a-z][a-z0-9\-/]*) (?<category>\S+) #(?<id>\d+)");
        if (match.Success)
        {
            instances.Add(new Instance(match.Groups["path"].Value, match.Groups["category"].Value,
                int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture)));
        }
    }
    Match counted = Regex.Match(header["seed"], @"instances (?<n>\d+); depth (?<depth>\d+)");
    int claimed = int.Parse(counted.Groups["n"].Value, CultureInfo.InvariantCulture);
    if (instances.Count != claimed)
    {
        Console.Error.WriteLine($"error: graph {pack} reports {claimed} instances and {instances.Count} were read from `inspect-graph`; the output format changed.");
        return 1;
    }
    structures.Add(new Structure(pack, Field(row, "domain"), header["set"].Split(' ')[0],
        int.Parse(header["registry"].Split(' ')[0], CultureInfo.InvariantCulture),
        int.Parse(header["grammar"].Split(' ')[0], CultureInfo.InvariantCulture),
        int.Parse(counted.Groups["depth"].Value, CultureInfo.InvariantCulture), instances,
        SizeOf(packFiles, dataRoot, pack, header["graph"])));
}

List<Destination> destinations = listing.Where(r => r[0] == "dest")
    .Select(row => new Destination(row[1], Field(row, "tier"),
        int.Parse(Field(row, "seed"), CultureInfo.InvariantCulture),
        int.Parse(Field(row, "attempt"), CultureInfo.InvariantCulture),
        Field(row, "graph"), SizeOf(packFiles, dataRoot, row[1], Field(row, "record"))))
    .ToList();

long indexBytes = File.Exists(Path.Combine(dataRoot, "index.db")) ? new FileInfo(Path.Combine(dataRoot, "index.db")).Length : 0;
long setBytes = sets.Sum(s => s.Bytes);
long graphBytes = structures.Sum(s => s.Bytes);
long destinationBytes = destinations.Sum(d => d.Bytes);

Console.WriteLine($"data root     {dataRoot}");
Console.WriteLine($"measured      {DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}");
Console.WriteLine($"packs         {packFiles.Count} directories, {packFiles.Sum(p => p.Value.Count)} record files, {Bytes(packFiles.Sum(p => p.Value.Sum()))}");
Console.WriteLine($"index.db      {Bytes(indexBytes)}, rebuildable from the manifests");
Console.WriteLine($"of that       sets {Bytes(setBytes)}, graphs {Bytes(graphBytes)}, destinations {Bytes(destinationBytes)}");
Console.WriteLine();

Console.WriteLine($"== PRIMITIVES: {sets.Count} set pack(s), {sets.Sum(s => s.Definitions.Count)} definitions");
foreach (SetPack set in sets.OrderBy(s => s.Registry))
{
    Console.WriteLine();
    Console.WriteLine($"pack {set.Pack}  registry {set.Registry}");
    Console.WriteLine($"  {set.Definitions.Count} definitions from {set.Definitions.Select(d => d.Template).Distinct().Count()} templates, "
        + $"{set.Definitions.Count(d => d.References > 0)} of them naming another primitive");
    Console.WriteLine($"  {set.Records} record files, {Bytes(set.Bytes)}: manifest {set.ManifestBytes} B, definitions {Bytes(set.Bytes - set.ManifestBytes)}");
    Console.WriteLine($"  {"category",-18}{"n",4}{"bytes",9}{"mean",7}{"min",6}{"max",6}   drawn from templates");
    foreach (IGrouping<string, Definition> group in set.Definitions.GroupBy(d => d.Category).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal))
    {
        List<long> sizes = group.Select(d => d.Bytes).ToList();
        Console.WriteLine($"  {group.Key,-18}{group.Count(),4}{sizes.Sum(),9:#,##0}{sizes.Sum() / sizes.Count,7}{sizes.Min(),6}{sizes.Max(),6}   "
            + string.Join(",", group.Select(d => d.Template).Distinct().Order()));
    }
}
Console.WriteLine();

Console.WriteLine($"== STRUCTURES: {structures.Count} composition graph(s), {structures.Sum(s => s.Instances.Count)} instances, {Bytes(graphBytes)}");
if (structures.Count > 0)
{
    List<int> counts = structures.Select(s => s.Instances.Count).Order().ToList();
    List<long> sizes = structures.Select(s => s.Bytes).Order().ToList();
    Console.WriteLine($"  instances per graph  min {counts[0]}, median {counts[counts.Count / 2]}, mean {counts.Average():0.0}, max {counts[^1]}");
    Console.WriteLine($"  bytes per graph      min {sizes[0]}, median {sizes[sizes.Count / 2]}, mean {sizes.Average():0}, max {sizes[^1]}");
    Console.WriteLine($"  bytes per instance   min {structures.Min(s => (double)s.Bytes / s.Instances.Count):0.0}, mean {structures.Average(s => (double)s.Bytes / s.Instances.Count):0.0}, max {structures.Max(s => (double)s.Bytes / s.Instances.Count):0.0}");
    Console.WriteLine($"  depth                {string.Join(", ", structures.GroupBy(s => s.Depth).OrderBy(g => g.Key).Select(g => $"{g.Count()} at depth {g.Key}"))}");
    Console.WriteLine($"  domains              {string.Join(", ", structures.GroupBy(s => s.Domain).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Count()} {g.Key}"))}");

    Console.WriteLine();
    Console.WriteLine($"  {"set",10}{"registry",10}{"grammar",9}{"graphs",8}{"instances",11}{"bytes",9}   definitions instantiated");
    foreach (IGrouping<(string Set, int Registry, int Grammar), Structure> group in structures
        .GroupBy(s => (s.Set, s.Registry, s.Grammar))
        .OrderBy(g => g.Key.Registry).ThenBy(g => g.Key.Grammar))
    {
        HashSet<int> used = group.SelectMany(s => s.Instances.Select(i => i.Definition)).ToHashSet();
        SetPack? source = sets.Find(s => s.Pack == group.Key.Set);
        Console.WriteLine($"  {group.Key.Set[..8],10}{group.Key.Registry,10}{group.Key.Grammar,9}{group.Count(),8}{group.Sum(s => s.Instances.Count),11}"
            + $"{group.Sum(s => s.Bytes),9:#,##0}   {used.Count} of {source?.Definitions.Count.ToString(CultureInfo.InvariantCulture) ?? "?"}");
    }

    Console.WriteLine();
    Console.WriteLine("  instances per graph, one # per graph");
    foreach (IGrouping<int, Structure> group in structures.GroupBy(s => s.Instances.Count).OrderBy(g => g.Key))
    {
        Console.WriteLine($"  {group.Key,4} {new string('#', group.Count())} ({group.Count()})");
    }

    Console.WriteLine();
    Console.WriteLine("  instances by category, every graph");
    foreach (IGrouping<string, Instance> group in structures.SelectMany(s => s.Instances).GroupBy(i => i.Category).OrderByDescending(g => g.Count()))
    {
        Console.WriteLine($"  {group.Key,-14}{group.Count(),6}   {group.Select(i => i.Definition).Distinct().Count()} distinct definition(s)");
    }

    List<Instance> all = structures.SelectMany(s => s.Instances).ToList();
    Console.WriteLine();
    Console.WriteLine($"  reuse                {all.Count} instances name {all.Select(i => i.Definition).Distinct().Count()} distinct definitions");
    Console.WriteLine($"  deepest path         {all.MaxBy(i => i.Path.Length)!.Path}");
    Console.WriteLine($"  largest              {string.Join(", ", structures.OrderByDescending(s => s.Instances.Count).Take(3).Select(s => $"{s.Pack[..8]} {s.Instances.Count}i/{s.Bytes}B"))}");
    Console.WriteLine($"  smallest             {string.Join(", ", structures.OrderBy(s => s.Instances.Count).Take(3).Select(s => $"{s.Pack[..8]} {s.Instances.Count}i/{s.Bytes}B"))}");
}
Console.WriteLine();

foreach (IGrouping<string, Structure> domain in structures.GroupBy(s => s.Domain).OrderBy(g => g.Key, StringComparer.Ordinal))
{
    Console.WriteLine($"== EVERY STRUCTURE IN DOMAIN {domain.Key}: {domain.Count()}");
    Dictionary<string, Destination> named = destinations
        .Where(d => domain.Any(s => s.Pack == d.Graph))
        .ToDictionary(d => d.Graph, d => d, StringComparer.Ordinal);

    foreach (IGrouping<(string Set, int Registry, int Grammar), Structure> generation in domain
        .GroupBy(s => (s.Set, s.Registry, s.Grammar))
        .OrderBy(g => g.Key.Registry).ThenBy(g => g.Key.Grammar))
    {
        SetPack? source = sets.Find(s => s.Pack == generation.Key.Set);
        long own = generation.Sum(s => s.Bytes) + generation.Sum(s => named.TryGetValue(s.Pack, out Destination? d) ? d.Bytes : 0);
        Console.WriteLine();
        Console.WriteLine($"  set {generation.Key.Set[..8]}, registry {generation.Key.Registry}, grammar {generation.Key.Grammar}: "
            + $"{generation.Count()} structures owning {own:#,##0} B, "
            + $"over a set of {source?.Bytes ?? 0:#,##0} B shared between them ({(source?.Bytes ?? 0) / (double)generation.Count():#,##0.0} B each amortized)");
        Console.WriteLine($"  {"seed",6}{"att",5}{"planets",9}{"moons",7}{"bary",6}{"atmos",7}{"inst",6}{"depth",7}{"graph B",10}{"dest B",8}{"own B",8}   graph     destination");

        foreach (Structure structure in generation
            .OrderBy(s => named.TryGetValue(s.Pack, out Destination? d) ? d.Seed : int.MaxValue)
            .ThenBy(s => s.Pack, StringComparer.Ordinal))
        {
            Bodies bodies = Count(structure.Instances);
            named.TryGetValue(structure.Pack, out Destination? destination);
            long dest = destination?.Bytes ?? 0;
            Console.WriteLine($"  {destination?.Seed.ToString(CultureInfo.InvariantCulture) ?? "-",6}"
                + $"{destination?.Attempt.ToString(CultureInfo.InvariantCulture) ?? "-",5}"
                + $"{bodies.Planets,9}{bodies.Moons,7}{bodies.Barycentres,6}{bodies.Atmospheres,7}"
                + $"{structure.Instances.Count,6}{structure.Depth,7}{structure.Bytes,10:#,##0}{dest,8:#,##0}{structure.Bytes + dest,8:#,##0}"
                + $"   {structure.Pack[..8]}  {destination?.Pack[..8] ?? "-"}");
        }

        Bodies totals = Count(generation.SelectMany(s => s.Instances).ToList());
        Console.WriteLine($"  {"total",6}{"",5}{totals.Planets,9}{totals.Moons,7}{totals.Barycentres,6}{totals.Atmospheres,7}"
            + $"{generation.Sum(s => s.Instances.Count),6}{"",7}{generation.Sum(s => s.Bytes),10:#,##0}"
            + $"{generation.Sum(s => named.TryGetValue(s.Pack, out Destination? d) ? d.Bytes : 0),8:#,##0}{own,8:#,##0}");
    }

    List<long> owned = domain.Select(s => s.Bytes + (named.TryGetValue(s.Pack, out Destination? d) ? d.Bytes : 0)).Order().ToList();
    Console.WriteLine();
    Console.WriteLine($"  bytes owned per structure   min {owned[0]:#,##0}, median {owned[owned.Count / 2]:#,##0}, mean {owned.Average():#,##0}, max {owned[^1]:#,##0}");
    Console.WriteLine($"  every {domain.Key} in this data root costs {owned.Sum():#,##0} B of its own, "
        + $"beside {sets.Sum(s => s.Bytes):#,##0} B of definitions they draw on");
    Console.WriteLine();
}

Console.WriteLine($"== DESTINATIONS: {destinations.Count} record(s), {Bytes(destinationBytes)}");
if (destinations.Count > 0)
{
    Console.WriteLine($"  graphs named         {destinations.Select(d => d.Graph).Distinct().Count()} distinct, of {structures.Count} stored");
    Console.WriteLine($"  bytes per record     min {destinations.Min(d => d.Bytes)}, max {destinations.Max(d => d.Bytes)}");
    foreach (IGrouping<string, Destination> group in destinations.GroupBy(d => d.Tier).OrderBy(g => g.Key, StringComparer.Ordinal))
    {
        List<int> seeds = group.Select(d => d.Seed).Order().ToList();
        Console.WriteLine($"  tier {group.Key,-16}{group.Count(),4} records, seeds {seeds[0]} to {seeds[^1]}, "
            + $"attempts {group.Min(d => d.Attempt)} to {group.Max(d => d.Attempt)}, mean {group.Average(d => d.Attempt):0.00}");
    }
    Console.WriteLine();
    Console.WriteLine("  attempts a tier needed, one # per destination");
    foreach (IGrouping<int, Destination> group in destinations.GroupBy(d => d.Attempt).OrderBy(g => g.Key))
    {
        Console.WriteLine($"  {group.Key,4} {new string('#', group.Count())} ({group.Count()})");
    }
}

return 0;

// A `planet` whose parent is a planet is a moon; under the star or a barycentre it is a planet,
// which is how the derived system description labels the same instances.
static Bodies Count(List<Instance> instances)
{
    Dictionary<string, string> categories = new(StringComparer.Ordinal);
    foreach (Instance instance in instances)
    {
        categories[instance.Path] = instance.Category;
    }
    int planets = 0;
    int moons = 0;
    foreach (Instance instance in instances.Where(i => i.Category == "planet"))
    {
        if (categories.TryGetValue(Parent(instance.Path), out string? parent) && parent == "planet")
        {
            moons++;
        }
        else
        {
            planets++;
        }
    }
    return new Bodies(instances.Count(i => i.Category == "star"), planets, moons,
        instances.Count(i => i.Category == "barycentre"), instances.Count(i => i.Category == "atmosphere"));
}

// One instance path up: the connector segment and the index below it both belong to the child.
static string Parent(string path)
{
    int last = path.LastIndexOf('/');
    if (last < 0)
    {
        return path;
    }
    int connector = path.LastIndexOf('/', last - 1);
    return connector < 0 ? path[..last] : path[..connector];
}

static string Bytes(long value) => value >= 1024
    ? $"{value.ToString("#,##0", CultureInfo.InvariantCulture)} B ({value / 1024.0:0.0} KiB)"
    : $"{value} B";

static string[] Lines(string output) => output.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 0).ToArray();

static Dictionary<string, string> Header(string[] output)
{
    Dictionary<string, string> header = new(StringComparer.Ordinal);
    foreach (string line in output.Where(line => line.Length > 0 && !char.IsWhiteSpace(line[0])))
    {
        string[] parts = line.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            header[parts[0]] = parts[1];
        }
    }
    return header;
}

static string Field(string[] row, string label)
{
    int index = Array.IndexOf(row, label);
    return index >= 0 && index + 1 < row.Length ? row[index + 1] : throw new InvalidOperationException($"`list` printed no {label} in: {string.Join(' ', row)}");
}

static Dictionary<string, List<long>> PackFiles(string dataRoot)
{
    Dictionary<string, List<long>> files = new(StringComparer.Ordinal);
    foreach (string directory in Directory.GetDirectories(Path.Combine(dataRoot, "packs")))
    {
        files[Path.GetFileName(directory)] = Directory.GetFiles(directory, "*.bin").Select(file => new FileInfo(file).Length).ToList();
    }
    return files;
}

static long SizeOf(Dictionary<string, List<long>> packFiles, string dataRoot, string pack, string hash)
{
    string directory = Path.Combine(dataRoot, "packs", pack);
    string? file = Directory.GetFiles(directory, hash.Length == 64 ? hash + ".bin" : hash + "*.bin").FirstOrDefault();
    return file is null ? throw new InvalidOperationException($"no record file for {hash} under {directory}") : new FileInfo(file).Length;
}

static string? Option(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

// The resolution of decision 0018, which the command-line tool applies to the same three inputs.
static string DefaultDataRoot()
{
    string? explicitRoot = Environment.GetEnvironmentVariable("SPACE_EXPLORER_DATA_DIR");
    if (!string.IsNullOrWhiteSpace(explicitRoot))
    {
        return explicitRoot;
    }
    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return Path.Combine(local, "SpaceExplorer");
}

static string RepositoryRoot()
{
    string? directory = Environment.CurrentDirectory;
    while (directory is not null && !Directory.Exists(Path.Combine(directory, ".git")) && !File.Exists(Path.Combine(directory, "SpaceExplorer.sln")))
    {
        directory = Path.GetDirectoryName(directory);
    }
    return directory ?? throw new InvalidOperationException("Run from inside the repository.");
}

// One build, then direct calls to the assembly: `dotnet run --project` per pack would cost minutes.
static string CommandLineTool(string repository)
{
    string assembly = Path.Combine(repository, "src", "SpaceExplorer.Cli", "bin", "Debug", "net10.0", "SpaceExplorer.Cli.dll");
    ProcessStartInfo build = new("dotnet", ["build", Path.Combine(repository, "src", "SpaceExplorer.Cli"), "--nologo", "-v", "quiet"]) { WorkingDirectory = repository, RedirectStandardOutput = true };
    using Process process = Process.Start(build) ?? throw new InvalidOperationException("dotnet is required to build the command-line tool.");
    string output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"building the command-line tool failed:{Environment.NewLine}{output}");
    }
    return File.Exists(assembly) ? assembly : throw new InvalidOperationException($"no command-line tool at {assembly}.");
}

static string Run(string assembly, string[] arguments)
{
    ProcessStartInfo start = new("dotnet", [assembly, .. arguments]) { RedirectStandardOutput = true, RedirectStandardError = true };
    using Process process = Process.Start(start) ?? throw new InvalidOperationException("dotnet is required to run the command-line tool.");
    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return process.ExitCode == 0
        ? output
        : throw new InvalidOperationException($"`{string.Join(' ', arguments)}` exited {process.ExitCode}: {error.Trim()}");
}

sealed record Definition(int Id, string Category, string Hash, int Template, int References, long Bytes);

sealed record SetPack(string Pack, string Manifest, int Registry, List<Definition> Definitions, long ManifestBytes, long Bytes, int Records);

sealed record Instance(string Path, string Category, int Definition);

sealed record Structure(string Pack, string Domain, string Set, int Registry, int Grammar, int Depth, List<Instance> Instances, long Bytes);

sealed record Bodies(int Stars, int Planets, int Moons, int Barycentres, int Atmospheres);

sealed record Destination(string Pack, string Tier, int Seed, int Attempt, string Graph, long Bytes);
