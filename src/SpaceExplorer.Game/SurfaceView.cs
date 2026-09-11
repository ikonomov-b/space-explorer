using Godot;
using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using GraphNode = SpaceExplorer.Core.Registry.GraphNode;

namespace SpaceExplorer.Game;

/// <summary>
/// The review view of one stored region: the ground a planet carries, drawn at its true size and wearing
/// the material the planet stores, from the two frames rung one is read in — a walk frame at eye height
/// and a top-down frame of the whole region with a scale bar
/// ([decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)).
/// Like <see cref="SystemView"/> it is a harness for reading a generator's output and not a view the core
/// release gives a player, so decision 0045's close-views clause stands as decision 0052 left it.
/// </summary>
/// <remarks>
/// What the content stores is drawn and never invented: the extent is the region's own stored parameter
/// in metres, and the material tiles at the length its recipe's scale claims, which is the one question
/// rung one asks and the one the body view structurally cannot, since a body is drawn far from its own
/// size there and its repeat is clamped.
///
/// What the view supplies, it says on screen. There is no celestial solution yet
/// ([decision 0032](../../../docs/decisions/0032-astronomically-consistent-surface-sky.md) is unbuilt),
/// so the light direction, the sky colour, and the eye height are the harness's own and are named in the
/// legend rather than passed off as derived. Rung one has no relief, so the ground is flat by
/// construction and not by simplification.
/// </remarks>
public partial class SurfaceView : Node3D
{
    /// <summary>Eye height in metres: the height decision 0041's minimum landable radius is derived from.</summary>
    private const float EyeHeight = 2f;

    /// <summary>How far the walk frame sees; a maximal region's far corner is 2,896 m from its centre.</summary>
    private const float ViewDistance = 4_096f;

    private readonly GraphNode _body;
    private readonly GraphNode _region;
    private readonly BodyDescription _description;
    private readonly PrimitiveResources _resources;
    private readonly CategoryRegistry _registry;
    private readonly long _extentMetres;
    private readonly float _heading;
    private readonly string? _screenshot;
    private readonly bool _fromAbove;

    private Camera3D _camera = null!;
    private Label _legend = null!;
    private ColorRect _behind = null!;
    private Control _scaleBar = null!;
    private int _framesDrawn;

    public SurfaceView(
        GraphNode body,
        GraphNode region,
        BodyDescription description,
        PrimitiveResources resources,
        CategoryRegistry registry,
        string? screenshot = null,
        bool fromAbove = false)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(registry);

        _body = body;
        _region = region;
        _description = description;
        _resources = resources;
        _registry = registry;
        _screenshot = screenshot;
        _fromAbove = fromAbove;

        _extentMetres = region.Definition.TryParameter(registry, CategoryRegistryRevision5.ExtentParameter)?.Value.AsInteger
            ?? throw new ArgumentException($"Instance '{region.Path}' stores no extent, so nothing says how far its ground reaches.", nameof(region));

        // The anchor's heading is a binary turn: which way the arrival faces on a body that has no north
        // to speak of yet. Latitude and longitude place the region on the body and do not move the ground
        // under it, so they are read into the legend rather than into the camera.
        _heading = (float)(region.Transform?.Component("heading") ?? 0) / (1L << 31) * Mathf.Pi;
    }

    public override void _Ready()
    {
        AddChild(Ground());
        AddChild(Sun());
        AddChild(Sky());

        _camera = _fromAbove ? Overhead() : AtEyeHeight();
        AddChild(_camera);

        BuildLegend();
    }

    public override void _Process(double delta)
    {
        if (_screenshot is null)
        {
            return;
        }

        // The first frames come out before the textures and the light have settled, as the system view
        // found; the fourth is what is saved.
        if (++_framesDrawn >= 4)
        {
            Error saved = GetViewport().GetTexture().GetImage().SavePng(_screenshot);
            GD.Print(saved == Error.Ok ? $"screenshot {_screenshot}" : $"screenshot failed: {saved}");
            GetTree().Quit(saved == Error.Ok ? 0 : 1);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape or Key.Q })
        {
            GetTree().Quit(0);
        }
    }

    /// <summary>The region itself: a square of ground the size the record says, wearing the body's own surface.</summary>
    private MeshInstance3D Ground() => new()
    {
        Mesh = new PlaneMesh { Size = new Vector2(_extentMetres, _extentMetres) },
        MaterialOverride = _resources.GroundFor(_body.Definition, _extentMetres),
        Name = "Ground",
    };

    /// <summary>
    /// The light, which the view invents and the legend admits to: there is no celestial solution yet, so
    /// this is a fixed afternoon sun and not the star the system stores.
    /// </summary>
    private static DirectionalLight3D Sun() => new()
    {
        Rotation = new Vector3(-Mathf.DegToRad(38f), Mathf.DegToRad(130f), 0f),
        LightEnergy = 1.1f,
        ShadowEnabled = true,
        Name = "HarnessLight",
    };

    /// <summary>A plain ground-coloured horizon, so the ground reads against something without claiming to be a sky.</summary>
    private static WorldEnvironment Sky() => new()
    {
        Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.10f, 0.11f, 0.13f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.32f, 0.33f, 0.36f),
            AmbientLightEnergy = 0.7f,
        },
        Name = "HarnessSky",
    };

    /// <summary>The walk frame: a person's eye at the region's centre, looking along the stored heading.</summary>
    private Camera3D AtEyeHeight()
    {
        var camera = new Camera3D { Far = ViewDistance, Fov = 70f, Name = "WalkCamera" };
        camera.Position = new Vector3(0f, EyeHeight, 0f);
        camera.Rotation = new Vector3(-Mathf.DegToRad(4f), _heading, 0f);
        return camera;
    }

    /// <summary>
    /// What the map frame spans across the screen's height: the region's diagonal and a margin, because
    /// the frame is turned to the stored heading and a square turned inside its own width loses its
    /// corners.
    /// </summary>
    private float MapMetres => _extentMetres * 1.55f;

    /// <summary>The map frame: the whole region square in orthographic projection, so the extent and the repeat are both legible.</summary>
    private Camera3D Overhead()
    {
        var camera = new Camera3D
        {
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = MapMetres,
            Far = ViewDistance,
            Name = "MapCamera",
        };

        camera.Position = new Vector3(0f, _extentMetres, 0f);
        camera.Rotation = new Vector3(-Mathf.Pi / 2f, _heading, 0f);
        return camera;
    }

    /// <summary>What is drawn, what it cost, and what the view supplied rather than read.</summary>
    private void BuildLegend()
    {
        var canvas = new CanvasLayer();
        AddChild(canvas);

        _behind = new ColorRect { Color = new Color(0f, 0f, 0f, 0.62f), Position = new Vector2(16, 16) };
        canvas.AddChild(_behind);

        float repeats = _resources.GroundRepeats(_body.Definition, _extentMetres);
        double metresPerTile = repeats == 0 ? 0 : _extentMetres / (double)repeats;

        string[] lines =
        [
            Invariant($"{_description.Number} {_description.Role.ToString().ToLowerInvariant()}   {_description.Type}   {_description.RadiusUnits / 256.0 / 1000.0:N0} km   {_description.GravityMillimetres / 1000.0:0.00} m/s^2   {(_description.LandingCandidate ? "landing candidate" : "no: " + string.Join(", ", _description.Refusals))}"),
            Invariant($"region     {_region.Path}"),
            Invariant($"extent     {_extentMetres:N0} m a side, {(_extentMetres * _extentMetres / 1_000_000.0):0.00} km^2"),
            Invariant($"anchor     latitude {Turn(_region.Transform?.Component("latitude") ?? 0):0.0}, longitude {Turn(_region.Transform?.Component("longitude") ?? 0):0.0}, heading {Turn(_region.Transform?.Component("heading") ?? 0):0.0}"),
            Invariant($"material   tiled {repeats:N0} times across, {metresPerTile:0.000} m a tile, {(metresPerTile * 100 / 128):0.00} cm a texel"),
            "",
            Invariant($"frame      {(_fromAbove ? $"orthographic, the whole region; the bar below is {ScaleBarMetres():N0} m" : $"walk, eye at {EyeHeight:0} m along the stored heading")}"),
            "supplied   light direction, sky colour, and eye height are this harness's own:",
            "           no celestial solution exists yet, so none of the three is derived",
            "stored     extent, material, texture scale, and the anchor above",
        ];

        _legend = new Label { Position = new Vector2(28, 24), Text = string.Join('\n', lines) };
        _legend.AddThemeFontSizeOverride("font_size", 15);
        canvas.AddChild(_legend);

        _scaleBar = new Control { Visible = _fromAbove };
        canvas.AddChild(_scaleBar);

        CallDeferred(nameof(SizeLegend));
    }

    /// <summary>A round number of metres near a fifth of the region, for the map frame's bar.</summary>
    private long ScaleBarMetres()
    {
        long fifth = Math.Max(1, _extentMetres / 5);
        long unit = (long)Math.Pow(10, Math.Floor(Math.Log10(fifth)));
        return fifth / unit * unit;
    }

    /// <summary>Fits the panel to its text and draws the map frame's bar, once the label knows its own size.</summary>
    private void SizeLegend()
    {
        Vector2 size = _legend.GetMinimumSize();
        _behind.Size = size + new Vector2(24, 16);

        if (!_fromAbove)
        {
            return;
        }

        // The bar is drawn in screen space from the orthographic camera's own scale, so what it measures
        // is the ground and not the window: the camera frames exactly MapMetres across its height.
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        float pixelsPerMetre = viewport.Y / MapMetres;
        float barPixels = ScaleBarMetres() * pixelsPerMetre;
        var bar = new ColorRect
        {
            Color = Colors.White,
            Position = new Vector2(28, viewport.Y - 48),
            Size = new Vector2(barPixels, 4),
        };

        var caption = new Label { Position = new Vector2(28, viewport.Y - 44), Text = Invariant($"{ScaleBarMetres():N0} m") };
        caption.AddThemeFontSizeOverride("font_size", 15);
        _scaleBar.AddChild(bar);
        _scaleBar.AddChild(caption);
    }

    private static double Turn(long component) => component / (double)(1L << 32) * 360.0;

    /// <summary>
    /// One line of the legend, formatted invariantly. Godot takes its number format from the system
    /// locale, where the command-line tool's description is invariant, so a decimal comma in the picture
    /// and a decimal point in the text would be the same figure read two ways.
    /// </summary>
    private static string Invariant(FormattableString line) => FormattableString.Invariant(line);
}
