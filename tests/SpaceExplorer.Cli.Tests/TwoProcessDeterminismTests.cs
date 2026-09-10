using System.Diagnostics;
using Xunit;

namespace SpaceExplorer.Cli.Tests;

/// <summary>
/// The two-process clause of the Determinism check in the development plan: one set specification and
/// one graph specification are generated and published by separate operating-system processes, into
/// separate data roots, and every byte they write must agree. A shared process, a shared static, or a
/// warm cache cannot account for the result (decisions 0008, 0020, 0042, 0047).
/// </summary>
public sealed class TwoProcessDeterminismTests : IDisposable
{
    private const string Seed = "42";

    private static readonly string Executable = Path.Combine(
        AppContext.BaseDirectory,
        OperatingSystem.IsWindows() ? "SpaceExplorer.Cli.exe" : "SpaceExplorer.Cli");

    private static readonly string Vocabulary = Path.Combine(AppContext.BaseDirectory, "vocabulary.json");

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "space-explorer-tests", Guid.NewGuid().ToString("n"));

    [Fact]
    public void Two_processes_generate_and_publish_the_same_bytes_from_one_specification()
    {
        Dictionary<string, string> first = GenerateSet("first");
        Dictionary<string, string> second = GenerateSet("second");

        Assert.Equal(first["vocabulary"], second["vocabulary"]);
        Assert.Equal(first["specification"], second["specification"]);
        Assert.Equal(first["pack"], second["pack"]);
        Assert.Equal(first["manifest"], second["manifest"]);
        Assert.Equal(first["definitions"], second["definitions"]);

        // The published records themselves, not only the hashes the runs reported.
        string[] firstRecords = Records("first", first["pack"]);
        string[] secondRecords = Records("second", second["pack"]);
        Assert.Equal(firstRecords.Select(Path.GetFileName), secondRecords.Select(Path.GetFileName));
        Assert.NotEmpty(firstRecords);
        foreach ((string left, string right) in firstRecords.Zip(secondRecords))
        {
            Assert.Equal(File.ReadAllBytes(left), File.ReadAllBytes(right));
        }
    }

    [Fact]
    public void A_third_process_validates_what_the_first_published_without_generating()
    {
        Dictionary<string, string> published = GenerateSet("first");

        string output = Run(["validate", published["pack"], "--data-root", Root("first")]);

        Assert.Contains(published["pack"], output, StringComparison.Ordinal);
        Assert.Contains("verified", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_processes_compose_and_publish_the_same_graph_from_the_same_set()
    {
        Dictionary<string, string> first = GenerateSet("first");
        Dictionary<string, string> second = GenerateSet("second");

        Dictionary<string, string> composedFirst = Compose("first", first["pack"]);
        Dictionary<string, string> composedSecond = Compose("second", second["pack"]);

        Assert.Equal(composedFirst["grammar"], composedSecond["grammar"]);
        Assert.Equal(composedFirst["pack"], composedSecond["pack"]);
        Assert.Equal(composedFirst["graph"], composedSecond["graph"]);
        Assert.Equal(composedFirst["instances"], composedSecond["instances"]);
        Assert.Equal(
            File.ReadAllBytes(Records("first", composedFirst["pack"]).Single()),
            File.ReadAllBytes(Records("second", composedSecond["pack"]).Single()));
    }

    [Fact]
    public void The_two_levers_name_the_same_destination_in_two_processes()
    {
        // The destination command is what the review view runs on, so the wiring from the two levers to a
        // system that satisfies its tier is checked here, where no display is needed (decision 0052).
        Dictionary<string, string> first = GenerateSet("first");
        GenerateSet("second");

        string one = Run(["destination", "--tier", "starter", "--seed", "7", "--data-root", Root("first")]);
        string two = Run(["destination", "--tier", "starter", "--seed", "7", "--data-root", Root("second")]);

        Assert.Equal(one, two);
        Assert.Contains("tier          pass (starter)", one, StringComparison.Ordinal);
        Assert.Contains($"set           {first["pack"]}", one, StringComparison.Ordinal);

        // What the two processes stored, not only what they printed: the levers name one pack, and the
        // record in it is the same byte for byte (decision 0053).
        string pack = Fields(one)["pack"];
        Assert.Equal(
            File.ReadAllBytes(Records("first", pack).Single()),
            File.ReadAllBytes(Records("second", pack).Single()));
    }

    [Fact]
    public void A_second_run_of_the_same_levers_loads_the_stored_destination_and_composes_nothing()
    {
        // The retry of decision 0052 is paid once: the second run addresses the record the first wrote and
        // reads its graph, which is what a player reopening a destination will do.
        GenerateSet("first");

        string first = Run(["destination", "--tier", "starter", "--seed", "7", "--data-root", Root("first")]);
        string again = Run(["destination", "--tier", "starter", "--seed", "7", "--data-root", Root("first")]);

        Assert.Contains("status        composed and published", first, StringComparison.Ordinal);
        Assert.Contains("status        loaded from the data root; nothing composed", again, StringComparison.Ordinal);
        Assert.Equal(Description(first), Description(again));

        string listed = Run(["list", "--data-root", Root("first")]);
        Assert.Contains($"dest  {Fields(first)["pack"]}", listed, StringComparison.Ordinal);
    }

    /// <summary>The description and verdict alone, without the status line the two runs differ in.</summary>
    private static string Description(string output) =>
        string.Join('\n', output.Split('\n').Where(line => !line.StartsWith("status", StringComparison.Ordinal)));

    [Fact]
    public void A_different_seed_yields_a_different_pack_across_processes()
    {
        Dictionary<string, string> first = GenerateSet("first");
        Dictionary<string, string> second = GenerateSet("second", seed: "43");

        Assert.NotEqual(first["pack"], second["pack"]);
        Assert.NotEqual(first["manifest"], second["manifest"]);
    }

    private Dictionary<string, string> GenerateSet(string root, string seed = Seed) =>
        Fields(Run(["generate-set", "--vocabulary", Vocabulary, "--seed", seed, "--data-root", Root(root)]));

    /// <summary>Each printed line is a name padded to a column and then its value.</summary>
    private static Dictionary<string, string> Fields(string output) =>
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(' ', 2, StringSplitOptions.TrimEntries))
            .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : string.Empty, StringComparer.Ordinal);

    private Dictionary<string, string> Compose(string root, string set) =>
        Fields(Run(["compose", "--set", set, "--domain", "solar-system", "--seed", Seed, "--data-root", Root(root)]));

    private string Root(string name) => Path.Combine(_directory, name);

    private string[] Records(string root, string pack) =>
        [.. Directory.EnumerateFiles(Path.Combine(Root(root), "packs", pack), "*.bin").Order(StringComparer.Ordinal)];

    private static string Run(string[] arguments)
    {
        Assert.True(File.Exists(Executable), $"The command-line tool was not copied beside these tests: {Executable}");

        var start = new ProcessStartInfo(Executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {Executable}.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(120_000), "The command-line tool did not exit within two minutes.");
        Assert.Equal(string.Empty, error);
        Assert.Equal(0, process.ExitCode);
        return output;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
