using Godot;
using SpaceExplorer.Core.Derivation;
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

    /// <summary>A brisk human walking pace, so crossing a maximal region on foot takes minutes and not seconds.</summary>
    private const float WalkSpeed = 3.2f;

    private readonly GraphNode _body;
    private readonly GraphNode _region;
    private readonly BodyDescription _description;
    private readonly PrimitiveResources _resources;
    private readonly CategoryRegistry _registry;
    private readonly long _extentMetres;
    private readonly float _heading;
    private readonly string? _screenshot;
    private readonly bool _fromAbove;

    private readonly ReliefField _relief;
    private readonly short[] _heights;
    private readonly int _across;

    private Camera3D _camera = null!;
    private Label _legend = null!;
    private ColorRect _behind = null!;
    private Control _scaleBar = null!;
    private int _framesDrawn;

    // Free walking on the ground plane only: the interactive session moves and looks around at fixed
    // eye height, but the two frames a row's screenshot pins ([decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)
    // clause 10) are unaffected, since a screenshot always starts the camera at the region centre.
    private Vector3 _walkedFrom = Vector3.Zero;
    private float _yaw;
    private float _pitch;
    private bool _dragging;

    public SurfaceView(
        GraphNode body,
        GraphNode region,
        BodyDescription description,
        PrimitiveResources resources,
        CategoryRegistry registry,
        ulong compositionSeed,
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

        // The anchor's heading is a binary turn: which way the arrival faces. It turns the camera here and
        // the ground in the core, where the whole anchor is read: from rung 2 the latitude and longitude
        // place the region on the body's own relief, so they move what is under foot as well as labelling
        // where it is.
        _heading = (float)(region.Transform?.Component("heading") ?? 0) / (1L << 31) * Mathf.Pi;

        _relief = new ReliefField(
            _description.RadiusUnits,
            Parameter(body, registry, CategoryRegistryRevision6.AmplitudeParameter),
            Parameter(body, registry, CategoryRegistryRevision6.RoughnessParameter),
            Parameter(body, registry, CategoryRegistryRevision6.WavelengthParameter),
            compositionSeed,
            body.Path);

        _heights = TerrainHeightfield.Sample(
            _relief,
            (int)(region.Transform?.Component("latitude") ?? 0),
            (int)(region.Transform?.Component("longitude") ?? 0),
            (int)(region.Transform?.Component("heading") ?? 0),
            _extentMetres);

        _across = TerrainHeightfield.SamplesAcross(_extentMetres);
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
        if (_screenshot is not null)
        {
            // The first frames come out before the textures and the light have settled, as the system view
            // found; the fourth is what is saved. A screenshot's camera never walks, so it always starts
            // exactly where decision 0061 clause 10 pins it.
            if (++_framesDrawn >= 4)
            {
                Error saved = GetViewport().GetTexture().GetImage().SavePng(_screenshot);
                GD.Print(saved == Error.Ok ? $"screenshot {_screenshot}" : $"screenshot failed: {saved}");
                GetTree().Quit(saved == Error.Ok ? 0 : 1);
            }

            return;
        }

        // The map frame is a fixed overhead reference, not a walk; only the walk frame moves.
        if (_fromAbove)
        {
            return;
        }

        var move = new Vector3(Pressed(Key.D) - Pressed(Key.A), 0f, Pressed(Key.S) - Pressed(Key.W));
        if (move == Vector3.Zero)
        {
            return;
        }

        float speed = WalkSpeed * (Input.IsKeyPressed(Key.Shift) ? 3f : 1f) * (float)delta;
        Basis flat = Basis.FromEuler(new Vector3(0f, _yaw, 0f));
        // The basis's Z points behind the eye, so w giving a negative move.Z is already forward: negating
        // it here is what put the walk in reverse.
        Vector3 stepped = _walkedFrom + ((flat.X * move.X) + (flat.Z * move.Z)) * speed;

        // Held inside the region's own extent: what lies beyond it is rung 5's far field and edge, unbuilt,
        // so walking off shows a missing rung rather than a fault in what rung one actually generated
        // ([decision 0062](../../../docs/decisions/0062-the-surface-harness-walks-in-an-interactive-session.md)).
        float half = _extentMetres / 2f;
        _walkedFrom = new Vector3(Mathf.Clamp(stepped.X, -half, half), 0f, Mathf.Clamp(stepped.Z, -half, half));
        UpdateWalkCamera();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey { Pressed: true, Keycode: Key.Escape }:
                GetTree().Quit(0);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click when !_fromAbove:
                _dragging = click.Pressed;
                break;
            case InputEventMouseMotion motion when _dragging && !_fromAbove:
                _yaw -= motion.Relative.X * 0.008f;
                _pitch = Mathf.Clamp(_pitch - (motion.Relative.Y * 0.008f), -1.5f, 1.5f);
                UpdateWalkCamera();
                break;
        }
    }

    private static float Pressed(Key key) => Input.IsKeyPressed(key) ? 1f : 0f;

    /// <summary>
    /// Places the walk camera at how far the drag has walked, facing where the drag has turned, with the
    /// eye riding the ground rather than holding an altitude: a camera at a fixed height over a landscape
    /// reports the height and not the landscape (decision 0063 clause 11).
    /// </summary>
    private void UpdateWalkCamera()
    {
        _camera.Position = _walkedFrom + new Vector3(0f, GroundUnder(_walkedFrom.X, _walkedFrom.Z) + EyeHeight, 0f);
        _camera.Rotation = new Vector3(_pitch, _yaw, 0f);
    }

    /// <summary>
    /// The region itself: the ground `terrain-heightfield/1` derives from the planet's relief field, at the
    /// size the record says and wearing the body's own surface. The heights are drawn at their true scale,
    /// with no vertical exaggeration, because a reader asked to judge an amplitude must not be shown a
    /// stretched one ([decision 0063](../../../docs/decisions/0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md)
    /// clause 11).
    /// </summary>
    private MeshInstance3D Ground()
    {
        var vertices = new Vector3[_heights.Length];
        var normals = new Vector3[_heights.Length];
        var uvs = new Vector2[_heights.Length];

        float step = TerrainHeightfield.CellMetres;
        float half = (_across - 1) / 2f;
        for (int j = 0; j < _across; j++)
        {
            for (int i = 0; i < _across; i++)
            {
                int at = (j * _across) + i;
                vertices[at] = new Vector3((i - half) * step, HeightAt(i, j), (j - half) * step);

                // Central differences over the field, which is the slope the samples themselves state
                // rather than one the renderer invents from the triangles it happens to have built.
                float acrossX = HeightAt(i + 1, j) - HeightAt(i - 1, j);
                float acrossZ = HeightAt(i, j + 1) - HeightAt(i, j - 1);
                normals[at] = new Vector3(-acrossX, 2f * step, -acrossZ).Normalized();

                // Unchanged from the flat ground: the material still tiles at the length its recipe's
                // scale claims, so rung one's question is asked of rung two's surface on the same terms.
                uvs[at] = new Vector2(i / (float)(_across - 1), j / (float)(_across - 1));
            }
        }

        var indices = new int[(_across - 1) * (_across - 1) * 6];
        int next = 0;
        for (int j = 0; j < _across - 1; j++)
        {
            for (int i = 0; i < _across - 1; i++)
            {
                // Clockwise seen from above, which is the winding Godot takes for a front face: wound the
                // other way the ground is culled and a walker sees straight through the hill in front.
                int corner = (j * _across) + i;
                indices[next++] = corner;
                indices[next++] = corner + 1;
                indices[next++] = corner + _across;
                indices[next++] = corner + 1;
                indices[next++] = corner + _across + 1;
                indices[next++] = corner + _across;
            }
        }

        var surface = new Godot.Collections.Array();
        surface.Resize((int)Mesh.ArrayType.Max);
        surface[(int)Mesh.ArrayType.Vertex] = vertices;
        surface[(int)Mesh.ArrayType.Normal] = normals;
        surface[(int)Mesh.ArrayType.TexUV] = uvs;
        surface[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface);

        return new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = _resources.GroundFor(_body.Definition, _extentMetres),
            Name = "Ground",
        };
    }

    /// <summary>One stored integer parameter of a definition, by the label its registry revision gives it.</summary>
    private static long Parameter(GraphNode node, CategoryRegistry registry, string label) =>
        node.Definition.TryParameter(registry, label)?.Value.AsInteger
        ?? throw new ArgumentException($"Instance '{node.Path}' stores no '{label}', which registry revision 6 gives every planet.", nameof(node));

    /// <summary>The stored height at a sample, in metres, with the edge held rather than wrapped.</summary>
    private float HeightAt(int i, int j) =>
        _heights[(Math.Clamp(j, 0, _across - 1) * _across) + Math.Clamp(i, 0, _across - 1)] / (float)ReliefField.HeightUnit;

    /// <summary>The ground under a point of the region, bilinearly between the four samples around it.</summary>
    private float GroundUnder(float x, float z)
    {
        float step = TerrainHeightfield.CellMetres;
        float half = (_across - 1) / 2f;
        float atX = Math.Clamp((x / step) + half, 0, _across - 1);
        float atZ = Math.Clamp((z / step) + half, 0, _across - 1);

        int i = (int)atX;
        int j = (int)atZ;
        float alongX = atX - i;
        float alongZ = atZ - j;

        float near = Mathf.Lerp(HeightAt(i, j), HeightAt(i + 1, j), alongX);
        float far = Mathf.Lerp(HeightAt(i, j + 1), HeightAt(i + 1, j + 1), alongX);
        return Mathf.Lerp(near, far, alongZ);
    }

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

    /// <summary>
    /// The walk frame: a person's eye starting at the region's centre, looking along the stored heading,
    /// and free to walk from there in an interactive session (a screenshot session never moves it).
    /// </summary>
    private Camera3D AtEyeHeight()
    {
        _yaw = _heading;
        _pitch = -Mathf.DegToRad(4f);
        var camera = new Camera3D { Far = ViewDistance, Fov = 70f, Name = "WalkCamera" };
        _camera = camera;
        UpdateWalkCamera();
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

        // Above the highest sample rather than above the plane, since from rung 2 there is something to
        // clear; orthographic, so the height changes what is lit and not how large anything looks.
        camera.Position = new Vector3(0f, (float)Highest() + _extentMetres, 0f);
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
            Invariant($"relief     {_relief.AmplitudeMetres:N0} m amplitude, roughness {_relief.RoughnessFraction / (double)ReliefField.RoughnessUnit:0.000}, {_relief.WavelengthMetres:N0} m coarsest over {_relief.Octaves} octaves"),
            Invariant($"field      {_relief.Hash.ToString()[..8]} by terrain-heightfield/1, {_across} x {_across} samples, {Lowest():0.0} m to {Highest():0.0} m here"),
            "",
            Invariant($"frame      {(_fromAbove ? $"orthographic, the whole region; the bar below is {ScaleBarMetres():N0} m" : $"walk, eye at {EyeHeight:0} m above the ground along the stored heading")}"),
            "supplied   light direction, sky colour, eye height and shading normals are this harness's",
            "           own: no celestial solution exists yet, so none of them is derived. The relief is",
            "           drawn at true scale, with no vertical exaggeration",
            "stored     extent, material, texture scale, the anchor above, and the relief the field derives",
            Invariant($"controls   {(_fromAbove ? "escape quits" : "w a s d walk, drag turn, shift faster, held inside the region's own extent, escape quits")}"),
        ];

        _legend = new Label { Position = new Vector2(28, 24), Text = string.Join('\n', lines) };
        _legend.AddThemeFontSizeOverride("font_size", 15);
        canvas.AddChild(_legend);

        _scaleBar = new Control { Visible = _fromAbove };
        canvas.AddChild(_scaleBar);

        CallDeferred(nameof(SizeLegend));
    }

    /// <summary>The lowest sample of this region, in metres: half of the map frame's height key.</summary>
    private double Lowest() => _heights.Min() / (double)ReliefField.HeightUnit;

    /// <summary>The highest sample of this region, in metres.</summary>
    private double Highest() => _heights.Max() / (double)ReliefField.HeightUnit;

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

        // The vertical axis gets the same courtesy as the horizontal one from rung 2: a reader must be
        // able to measure the relief rather than guess it, and a top-down frame shows none of it.
        var key = new Label
        {
            Position = new Vector2(28, viewport.Y - 76),
            Text = Invariant($"relief {Lowest():0.0} m to {Highest():0.0} m, {Highest() - Lowest():0.0} m of it"),
        };

        key.AddThemeFontSizeOverride("font_size", 15);
        _scaleBar.AddChild(key);
    }

    private static double Turn(long component) => component / (double)(1L << 32) * 360.0;

    /// <summary>
    /// One line of the legend, formatted invariantly. Godot takes its number format from the system
    /// locale, where the command-line tool's description is invariant, so a decimal comma in the picture
    /// and a decimal point in the text would be the same figure read two ways.
    /// </summary>
    private static string Invariant(FormattableString line) => FormattableString.Invariant(line);
}
