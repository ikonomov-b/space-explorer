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
    /// Shows the destination named on the command line, replacing the preview the scene holds: a stored one
    /// with <c>--destination &lt;pack&gt;</c>, or the one the two levers name, which is loaded from the data
    /// root when it is stored there and composed and published when it is not (decision 0053). The
    /// description is printed as well as displayed, so a run from a terminal is reviewable without a window
    /// and the two cannot disagree.
    /// </summary>
    private void OpenSystemView(string[] arguments)
    {
        try
        {
            DataRoot root = Option(arguments, "--data-root") is { } path ? DataRoot.At(path) : DataRoot.Resolve();
            CategoryRegistry registry;
            CompositionGrammar grammar;
            Destination destination;

            if (Option(arguments, "--destination") is { } stored)
            {
                DestinationRecord record = DestinationStore.Load(root, PackId.Parse(stored));
                registry = CategoryRegistries.Supported.Find(record.Specification.RegistryRevision);
                grammar = GrammarOver(registry);
                CompositionGraph graph = GraphLoader.Load(root, record.GraphPack, CategoryRegistries.Supported, grammar);
                destination = new Destination(
                    record.Specification.Tier,
                    record.Specification.Seed,
                    record.Attempt,
                    record.CompositionSeed,
                    graph,
                    SystemDescription.Derive(graph, registry, SuitProfile.Version1));
                GD.Print($"destination   {record.Pack} loaded; graph {record.GraphPack}");
            }
            else
            {
                DistanceTier tier = TierRules.TryParse(Option(arguments, "--tier") ?? "starter")
                    ?? throw new ArgumentException($"--tier takes one of {string.Join(", ", TierRules.Labels)}.");
                ulong seed = ulong.Parse(Option(arguments, "--seed") ?? "1", System.Globalization.CultureInfo.InvariantCulture);

                PrimitiveSet set = SourceSet(root, Option(arguments, "--set"));
                registry = CategoryRegistries.Supported.Find(set.Manifest.RegistryRevision);
                grammar = GrammarOver(registry);
                destination = Draw(root, tier, seed, set, grammar, registry, arguments.Contains("--no-publish"));
            }

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

    /// <summary>
    /// The destination the levers name: the stored one when the data root holds it, otherwise composed and
    /// published, so what was drawn can be reopened and cited (decision 0053).
    /// </summary>
    private static Destination Draw(DataRoot root, DistanceTier tier, ulong seed, PrimitiveSet set, CompositionGrammar grammar, CategoryRegistry registry, bool noPublish)
    {
        DestinationSpecification specification = DestinationSpecification.For(tier, seed, set, grammar, registry, SuitProfile.Version1);
        if (DestinationStore.Find(root, specification) is { } stored)
        {
            CompositionGraph loaded = GraphLoader.Load(root, stored.GraphPack, CategoryRegistries.Supported, grammar);
            GD.Print($"destination   {stored.Pack} loaded; graph {stored.GraphPack}");
            return new Destination(tier, seed, stored.Attempt, stored.CompositionSeed, loaded, SystemDescription.Derive(loaded, registry, SuitProfile.Version1));
        }

        Destination composed = DestinationComposer.Compose(tier, seed, set, grammar, registry, SuitProfile.Version1);
        if (!noPublish)
        {
            GraphPublisher.Publish(root, composed.Graph);
            DestinationStore.Publish(root, DestinationRecord.Of(composed, set, grammar, registry, SuitProfile.Version1));
            GD.Print($"destination   {specification.PackId} composed and published; graph {composed.Graph.Pack}");
        }
        else
        {
            GD.Print($"destination   {specification.PackId} composed; not published");
        }

        return composed;
    }

    /// <summary>The newest grammar this build holds over <paramref name="registry"/>.</summary>
    private static CompositionGrammar GrammarOver(CategoryRegistry registry) =>
        CompositionGrammars.Supported.Newest(registry.Revision)
        ?? throw new ArgumentException($"This build has no composition grammar over category-registry revision {registry.Revision}.");

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
