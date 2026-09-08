using Godot;
using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using SpaceExplorer.Persistence;

namespace SpaceExplorer.Game;

/// <summary>
/// Root node of the game. Runs the exported-build smoke check when launched with <c>-- --smoke</c>, opens
/// the solar-system review view of <see cref="SystemView"/> with <c>-- --system</c>, and otherwise shows
/// the materials and environment preview the scene holds.
/// </summary>
public partial class Main : Node
{
    public override void _Ready()
    {
        string[] arguments = [.. OS.GetCmdlineUserArgs()];

        if (arguments.Contains("--smoke"))
        {
            RunSmokeCheck();
            return;
        }

        if (arguments.Contains("--system"))
        {
            OpenSystemView(arguments);
        }
    }

    private void RunSmokeCheck()
    {
        try
        {
            string godotVersion = (string)Engine.GetVersionInfo()["string"];
            string sqliteVersion = SqliteRuntime.GetLibraryVersion();
            GD.Print($"SMOKE OK godot={godotVersion} dotnet={System.Environment.Version} sqlite={sqliteVersion}");
            GetTree().Quit(0);
        }
        catch (Exception e)
        {
            GD.PrintErr($"SMOKE FAIL {e}");
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// Composes the destination the two levers name and shows it, replacing the preview the scene holds.
    /// The description is printed as well as displayed, so a run from a terminal is reviewable without a
    /// window and the two cannot disagree.
    /// </summary>
    private void OpenSystemView(string[] arguments)
    {
        try
        {
            DistanceTier tier = TierRules.TryParse(Option(arguments, "--tier") ?? "starter")
                ?? throw new ArgumentException($"--tier takes one of {string.Join(", ", TierRules.Labels)}.");
            ulong seed = ulong.Parse(Option(arguments, "--seed") ?? "1", System.Globalization.CultureInfo.InvariantCulture);

            DataRoot root = Option(arguments, "--data-root") is { } path ? DataRoot.At(path) : DataRoot.Resolve();
            PrimitiveSet set = SourceSet(root, Option(arguments, "--set"));
            CategoryRegistry registry = CategoryRegistries.Supported.Find(set.Manifest.RegistryRevision);
            CompositionGrammar grammar = CompositionGrammars.Supported.Newest(registry.Revision)
                ?? throw new ArgumentException($"This build has no composition grammar over category-registry revision {registry.Revision}.");

            Destination destination = DestinationComposer.Compose(tier, seed, set, grammar, registry, SuitProfile.Version1);
            GD.Print(destination.Description.Text);

            foreach (Node child in GetChildren())
            {
                child.QueueFree();
            }

            AddChild(new SystemView(destination, registry, Option(arguments, "--screenshot"), Option(arguments, "--focus")));
        }
        catch (Exception e)
        {
            GD.PrintErr($"error: {e.Message}");
            GetTree().Quit(1);
        }
    }

    /// <summary>The set to compose from: the one named, or the only one published under a supported revision.</summary>
    private static PrimitiveSet SourceSet(DataRoot root, string? named)
    {
        if (named is not null)
        {
            return SetLoader.Load(root, PackId.Parse(named), CategoryRegistries.Supported);
        }

        IReadOnlyList<(PackId Pack, ContentHash Manifest, int Count)> published = SetLoader.List(root);
        return published.Count == 1
            ? SetLoader.Load(root, published[0].Pack, CategoryRegistries.Supported)
            : throw new ArgumentException(published.Count == 0
                ? $"No set is published in {root.Path}; run generate-set first."
                : $"{published.Count} sets are published in {root.Path}; name one with --set <pack-id>.");
    }

    private static string? Option(string[] arguments, string name)
    {
        int index = Array.IndexOf(arguments, name);
        return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
    }
}
