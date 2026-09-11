using Godot;
using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using SpaceExplorer.Persistence;
using GraphNode = SpaceExplorer.Core.Registry.GraphNode;

namespace SpaceExplorer.Game;

/// <summary>
/// Root node of the game. Runs the exported-build smoke check when launched with <c>-- --smoke</c>, opens
/// the solar-system review view of <see cref="SystemView"/> with <c>-- --system</c>, opens the surface
/// review view of <see cref="SurfaceView"/> with <c>-- --surface</c>, and otherwise shows the materials
/// and environment preview the scene holds.
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
            return;
        }

        if (arguments.Contains("--surface"))
        {
            OpenSurfaceView(arguments);
        }
    }

    /// <summary>
    /// Shows one region of the destination the levers name: the ground a planet carries, at its true size
    /// and in the body's own stored material, from the walk frame or, with <c>--from-above</c>, the map
    /// frame (decision 0061). <c>--region &lt;n&gt;</c> picks among the destination's regions in graph
    /// order, and every region is listed on the way, so a sweep can name what it read.
    /// </summary>
    private void OpenSurfaceView(string[] arguments)
    {
        try
        {
            DataRoot root = Option(arguments, "--data-root") is { } path ? DataRoot.At(path) : DataRoot.Resolve();
            DistanceTier tier = TierRules.TryParse(Option(arguments, "--tier") ?? "starter")
                ?? throw new ArgumentException($"--tier takes one of {string.Join(", ", TierRules.Labels)}.");
            ulong seed = ulong.Parse(Option(arguments, "--seed") ?? "1", System.Globalization.CultureInfo.InvariantCulture);

            PrimitiveSet set = SourceSet(root, Option(arguments, "--set"));
            CategoryRegistry registry = CategoryRegistries.Supported.Find(set.Manifest.RegistryRevision);
            CompositionGrammar grammar = GrammarOver(registry);
            Destination destination = Draw(root, tier, seed, set, grammar, registry, arguments.Contains("--no-publish"));

            (GraphNode Body, GraphNode Region)[] regions = [.. Regions(destination.Graph.Root)];
            if (regions.Length == 0)
            {
                throw new ArgumentException($"The destination of tier {tier} seed {seed} carries no region: no body of it is both large enough and made of ground.");
            }

            // Every region is listed with the material it wears, so a sweep can count the distinct
            // surfaces a sample shows without opening each picture: repetition made visible rather than
            // corrected, which is the cheap first step review finding 55 asks for.
            foreach ((GraphNode body, GraphNode region) in regions)
            {
                PrimitiveRevisionRef surface = body.Definition.TryParameter(registry, CategoryRegistryRevision3.SurfaceParameter)!.Value.Value.AsRef;
                uint template = destination.Graph.Source.TryFind(surface.Id)?.Provenance.TemplateId ?? 0;
                GD.Print($"region        {region.Path} on {body.Path} wearing surface {surface.Id.LocalId} from template {template}");
            }

            // Listing and quitting is how a sweep counts a sample's regions and the distinct surfaces they
            // wear without opening a window, which the iteration row needs and a screenshot run cannot give
            // headlessly: there is no viewport to save.
            if (arguments.Contains("--list"))
            {
                GetTree().Quit(0);
                return;
            }

            // A sweep that always drew the first region drew a rocky body twenty-four times, because the
            // first region of every starter seed sits on one — for the cycle whose whole change was that
            // the ground varies by what a body is made of. `--kind` draws the first region of a named body
            // type instead, and falls back to the first region where the destination has none, so a sweep
            // rotating the kind by seed covers rock, ice and ocean in the same forty-eight frames
            // ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
            // clause 12).
            int index = int.Parse(Option(arguments, "--region") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            if (Option(arguments, "--kind") is { } wanted)
            {
                int found = Array.FindIndex(regions, region =>
                    destination.Description.Bodies.First(body => body.Path == region.Body.Path).Type == wanted);

                GD.Print($"kind          {wanted}: {(found >= 0 ? $"region {found}" : $"absent, falling back to region {index}")}");
                if (found >= 0)
                {
                    index = found;
                }
            }

            if (index < 0 || index >= regions.Length)
            {
                throw new ArgumentException($"--region {index} is outside the {regions.Length} this destination carries.");
            }

            (GraphNode chosenBody, GraphNode chosenRegion) = regions[index];
            BodyDescription description = destination.Description.Bodies.First(body => body.Path == chosenBody.Path);
            GD.Print(destination.Description.Text);

            foreach (Node child in GetChildren())
            {
                child.QueueFree();
            }

            AddChild(new SurfaceView(
                chosenBody,
                chosenRegion,
                description,
                new PrimitiveResources(destination.Graph.Source, registry),
                registry,
                destination.Graph.Specification.Seed,
                root,
                destination.Graph.Pack,
                Option(arguments, "--screenshot"),
                arguments.Contains("--from-above")));
        }
        catch (Exception e)
        {
            GD.PrintErr($"error: {e.Message}");
            GetTree().Quit(1);
        }
    }

    /// <summary>Every region the graph holds, in graph order, with the body that carries it.</summary>
    private static IEnumerable<(GraphNode Body, GraphNode Region)> Regions(GraphNode node) =>
        node.Children
            .SelectMany(connector => connector)
            .SelectMany(child => child.Definition.Category == CategoryRegistryRevision5.Region
                ? [(node, child)]
                : Regions(child));

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
                    SystemDescription.Derive(graph, registry, SuitProfile.Version1, RegionLimits.Version1));
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

            AddChild(new SystemView(destination, registry, Option(arguments, "--screenshot"), Option(arguments, "--focus"), !arguments.Contains("--no-panel")));
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
        DestinationSpecification specification = DestinationSpecification.For(tier, seed, set, grammar, registry, SuitProfile.Version1, TierProfile.Version1, RegionLimits.Version1);
        if (DestinationStore.Find(root, specification) is { } stored)
        {
            CompositionGraph loaded = GraphLoader.Load(root, stored.GraphPack, CategoryRegistries.Supported, grammar, set);
            GD.Print($"destination   {stored.Pack} loaded; graph {stored.GraphPack}");
            return new Destination(tier, seed, stored.Attempt, stored.CompositionSeed, loaded, SystemDescription.Derive(loaded, registry, SuitProfile.Version1, RegionLimits.Version1));
        }

        Destination composed = DestinationComposer.Compose(tier, seed, set, grammar, registry, SuitProfile.Version1, TierProfile.Version1, RegionLimits.Version1);
        if (!noPublish)
        {
            GraphPublisher.Publish(root, composed.Graph);
            GroundPublisher.Publish(root, composed.Graph, registry);
            DestinationStore.Publish(root, DestinationRecord.Of(composed, set, grammar, registry, SuitProfile.Version1, TierProfile.Version1, RegionLimits.Version1));
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
