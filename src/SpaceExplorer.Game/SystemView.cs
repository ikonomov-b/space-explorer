using Godot;
using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using GraphNode = SpaceExplorer.Core.Registry.GraphNode;

namespace SpaceExplorer.Game;

/// <summary>
/// The review view of one composed solar system: the bodies in three dimensions beside the text
/// description they are judged by. It is a harness for reading a generator's output, like the `iterate`
/// command, not a view the core release gives a player; decision 0045's rule that other bodies are seen
/// only in the surface sky is about the game and is unchanged.
/// </summary>
/// <remarks>
/// Nothing here is to scale, and it says so on screen: real orbits span four orders of magnitude and real
/// bodies are invisible beside them. Orbit radii are placed on a logarithmic scale between the system's
/// own innermost and outermost, and body radii likewise between its own smallest and largest, so what the
/// view shows faithfully is order, ratio, and kind. The true figures are in the description panel.
/// </remarks>
public partial class SystemView : Node3D
{
    private const float InnerRing = 6f;
    private const float OuterRing = 28f;
    /// <summary>The drawn radius of the largest object in the system, which every other is a share of.</summary>
    private const float LargestDrawn = 3.0f;

    /// <summary>
    /// The exponent the drawn radius follows the stored one by. A logarithmic map flattened a star of
    /// 1,058,024 km and a gas giant of 112,032 km to within a third of each other, where the truth is
    /// nine times; at 0.4 the star reads two and a half times the giant and fourteen times a small moon,
    /// which is understated but ordered, and a moon is still some ten pixels across when the whole system
    /// is framed (decision 0057).
    /// </summary>
    private const double RadiusExponent = 0.4;

    /// <summary>A barycentre marks a place rather than a body, and keeps the smallest mark on screen.</summary>
    private const float BarycentreDrawn = 0.08f;

    /// <summary>How near the camera must be for a body to show its whole line rather than its name alone.</summary>
    private const float ReadingDistance = 14f;

    private readonly Destination _destination;
    private readonly CategoryRegistry _registry;
    private readonly PrimitiveResources _resources;

    /// <summary>Somewhere to fly to: the star, a body, or the whole system, with the distance it reads well from.</summary>
    private sealed record Target(string Name, Vector3 Position, float Radius);

    private readonly List<Target> _targets = [];

    /// <summary>
    /// What each body's caption needs to be placed: the body it belongs to, its two forms, the label and
    /// leader line that carry them, and the body's own radius, which the leader rises from.
    /// </summary>
    private sealed record Caption(Node3D Body, string Full, string Brief, Label Label, Line2D Leader, float Radius);

    private readonly List<Caption> _captions = [];

    private Camera3D _camera = null!;
    private CanvasLayer _canvas = null!;
    private CanvasLayer _marks = null!;
    private Label _description = null!;
    private ColorRect _behind = null!;
    private Label _hud = null!;
    private Vector3 _focus = Vector3.Zero;
    private float _yaw = 0.6f;
    private float _pitch = 0.9f;
    private float _distance = 34f;
    private int _focused = -1;
    private bool _dragging;
    private bool _panelShown = true;
    private bool _tableShown;

    private readonly string? _screenshot;
    private readonly string? _openOn;
    private readonly bool _openWithPanel;
    private int _framesDrawn;

    public SystemView(Destination destination, CategoryRegistry registry, string? screenshot = null, string? openOn = null, bool panel = true)
    {
        _destination = destination;
        _registry = registry;
        _resources = new PrimitiveResources(destination.Graph.Source, registry);
        _screenshot = screenshot;
        _openOn = openOn;
        _openWithPanel = panel;
    }

    public override void _Ready()
    {
        AddChild(Environment());

        // The panel comes first: every caption is a child of it, and where it sits is what the captions
        // stand clear of. Leader lines go on the layer under it, so one drawn across the panel passes
        // behind the text rather than through it.
        _marks = new CanvasLayer { Layer = 0 };
        AddChild(_marks);
        _canvas = Panel();
        AddChild(_canvas);
        // The scale comes before anything is drawn on it, because the star is sized by what it stores
        // like every other object (decision 0057).
        BodyDescription[] bodies = [.. _destination.Description.Bodies];
        double largestRadius = LargestRadius(bodies);
        float starRadius = Size(_destination.Description.Star.RadiusUnits, largestRadius);

        AddChild(Star(starRadius));
        _targets.Add(new Target($"star, class {_destination.Description.Star.Type}", Vector3.Zero, starRadius));

        (double inner, double outer) = OrbitSpan(bodies);

        var placed = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        foreach (BodyDescription body in bodies)
        {
            int dot = body.Number.IndexOf('.', StringComparison.Ordinal);
            Vector3 position;
            if (dot < 0)
            {
                // The orbit the stored elements describe, on a ring scaled from its semi-major axis: its
                // shape, its tilt, and where the body stands on it are the content's, and only the size
                // of the ring is the view's (decision 0057).
                Orbit orbit = OrbitOf(body, Ring(body.DistanceMetres, inner, outer));
                AddChild(OrbitRing(orbit));
                position = orbit.At(orbit.TrueAnomaly());
            }
            else
            {
                // A moon or a barycentre's companion sits beside its parent: its own orbit is far too
                // small to show beside the system, so it is offset by a fixed step instead.
                Vector3 parent = placed.TryGetValue(body.Number[..dot], out Vector3 found) ? found : Vector3.Zero;
                float step = 2.4f + (1.4f * (body.Number[^1] - '0'));
                position = parent + (new Vector3(Mathf.Cos(Angle(body, "mean-anomaly")), 0f, Mathf.Sin(Angle(body, "mean-anomaly"))) * step);
            }

            placed[body.Number] = position;
            float radius = body.Role == BodyRole.Barycentre ? BarycentreDrawn : Size(body.RadiusUnits, largestRadius);
            AddChild(Body(body, position, radius));
            _targets.Add(new Target($"{body.Number} {body.Type}{(body.LandingCandidate ? ", landable" : string.Empty)}", position, radius));
        }

        AddChild(Light());
        _camera = new Camera3D { Far = 2_000f, Near = 0.02f };
        AddChild(_camera);

        // A review can open straight at a body, so a picture of one can be taken without a hand on the keys.
        int opening = _openOn is null ? -1 : _targets.FindIndex(target => target.Name.StartsWith(_openOn + " ", StringComparison.Ordinal) || target.Name.StartsWith(_openOn + ",", StringComparison.Ordinal));
        Focus(_openOn is null ? -1 : Math.Max(opening, 0));

        // A picture of the system alone can be asked for from the command line, as the panel key asks for
        // it at the window.
        ShowPanel(_openWithPanel);
    }

    public override void _Process(double delta)
    {
        PlaceCaptions();

        if (_screenshot is not null)
        {
            // A few frames in, the scene is drawn; saving it and leaving makes a system reviewable as a
            // picture without anyone sitting at the window.
            if (++_framesDrawn >= 4)
            {
                Error saved = GetViewport().GetTexture().GetImage().SavePng(_screenshot);
                GD.Print(saved == Error.Ok ? $"screenshot {_screenshot}" : $"screenshot failed: {saved}");
                GetTree().Quit(saved == Error.Ok ? 0 : 1);
            }

            return;
        }

        // Free movement drifts the point the camera looks at, so the view can be taken anywhere between
        // the bodies as well as to them.
        var move = new Vector3(
            Pressed(Key.D) - Pressed(Key.A),
            Pressed(Key.E) - Pressed(Key.Q),
            Pressed(Key.S) - Pressed(Key.W));

        if (move == Vector3.Zero)
        {
            return;
        }

        // Speed follows the distance being viewed from, so the same keys cross a system and creep up on a
        // moon without changing gear.
        float speed = Mathf.Max(0.6f, _distance) * (float)delta * (Input.IsKeyPressed(Key.Shift) ? 3f : 1f);
        Basis basis = _camera.GlobalTransform.Basis;
        _focus += ((basis.X * move.X) + (Vector3.Up * move.Y) + (basis.Z * move.Z)) * speed;
        _focused = -2;
        MoveCamera();
    }

    /// <summary>
    /// Places every caption for the distance it is being read from, and apart from every other. A body
    /// near the camera shows its whole line and a far one its short form; the label is then laid out on
    /// screen rather than in the world, because whether two captions collide is a question about the
    /// screen: bodies that sit a long way apart in the system can project a few pixels from each other.
    /// The nearest body is placed first and keeps the place it wants, and each caption after it climbs a
    /// ladder of candidate places until it stands clear of the ones already placed and of the panel, which
    /// is what decision 0052 asks for in saying no caption is in doubt about the object it belongs to.
    /// </summary>
    private void PlaceCaptions()
    {
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector3 eye = _camera.GlobalPosition;
        _behind.Size = _description.GetMinimumSize() + new Vector2(16f, 12f);

        // The panel's own text and the key line at the foot of the screen are places a caption may not
        // take: text over text is unreadable whichever of the two the reader wanted.
        List<Rect2> taken = [new Rect2(0f, viewport.Y - 40f, viewport.X, 40f)];
        if (_panelShown)
        {
            taken.Add(new Rect2(_description.Position, _description.GetMinimumSize()).Grow(6f));
        }

        foreach (Caption caption in _captions.OrderBy(entry => eye.DistanceSquaredTo(entry.Body.GlobalPosition)))
        {
            Vector3 top = caption.Body.GlobalPosition + (Vector3.Up * caption.Radius);
            if (_camera.IsPositionBehind(top))
            {
                caption.Label.Visible = false;
                caption.Leader.Visible = false;
                continue;
            }

            float distance = eye.DistanceTo(caption.Body.GlobalPosition);

            // The panel and the captions divide the work by distance. While the panel carries the whole
            // table, a body too far to be read closely is already listed there, so it wears nothing and
            // the near ones are legible; while the panel carries the summary alone, every body wears its
            // own row, which is the only place it appears (decision 0054).
            if (_tableShown && distance >= ReadingDistance)
            {
                caption.Label.Visible = false;
                caption.Leader.Visible = false;
                continue;
            }

            caption.Label.Text = distance < ReadingDistance ? caption.Full : caption.Brief;
            Vector2 size = caption.Label.GetMinimumSize();
            Vector2 anchor = _camera.UnprojectPosition(top);
            Vector2 place = Clear(anchor, size, viewport, taken);
            taken.Add(new Rect2(place, size).Grow(3f));

            caption.Label.Position = place;
            caption.Label.Size = size;
            caption.Label.Visible = true;

            // The leader runs from the body to the nearest edge of its own label, so a caption standing
            // several rows away from a crowded body is still tied to it and to nothing else.
            var rect = new Rect2(place, size);
            caption.Leader.ClearPoints();
            caption.Leader.AddPoint(anchor);
            caption.Leader.AddPoint(new Vector2(Mathf.Clamp(anchor.X, rect.Position.X, rect.End.X), Mathf.Clamp(anchor.Y, rect.Position.Y, rect.End.Y)));
            caption.Leader.Visible = true;
        }
    }

    /// <summary>
    /// The first candidate place of <paramref name="size"/> that no rectangle in <paramref name="taken"/>
    /// holds, or, where a crowded view leaves no free place at all, the one that covers least of what is
    /// already there, so a caption that cannot stand clear still hides as little as it can.
    /// </summary>
    private static Vector2 Clear(Vector2 anchor, Vector2 size, Vector2 viewport, List<Rect2> taken)
    {
        Vector2 least = Vector2.Zero;
        float leastCovered = float.MaxValue;
        foreach (Vector2 candidate in Candidates(anchor, size))
        {
            Vector2 clamped = new(Mathf.Clamp(candidate.X, 4f, Mathf.Max(4f, viewport.X - size.X - 4f)), Mathf.Clamp(candidate.Y, 4f, Mathf.Max(4f, viewport.Y - size.Y - 4f)));
            var rect = new Rect2(clamped, size);
            float covered = 0f;
            foreach (Rect2 other in taken)
            {
                Rect2 shared = other.Intersection(rect);
                covered += shared.Size.X * shared.Size.Y;
            }

            if (covered <= 0f)
            {
                return clamped;
            }

            if (covered < leastCovered)
            {
                leastCovered = covered;
                least = clamped;
            }
        }

        return least;
    }

    /// <summary>
    /// The places a caption will take, in the order it prefers them: above its body to the right, then in
    /// steps further above, then the same below, then both again to the left. A caption crowded out of its
    /// own place therefore moves the shortest way that still reads as belonging to its body, and one whose
    /// body sits under the panel can walk far enough to come out beneath it.
    /// </summary>
    private static IEnumerable<Vector2> Candidates(Vector2 anchor, Vector2 size)
    {
        const float gap = 10f;
        float step = size.Y + 5f;
        foreach (float side in (float[])[gap, -size.X - gap])
        {
            for (int rung = 0; rung < 8; rung++)
            {
                yield return new Vector2(anchor.X + side, anchor.Y - size.Y - gap - (rung * step));
            }

            for (int rung = 0; rung < 20; rung++)
            {
                yield return new Vector2(anchor.X + side, anchor.Y + gap + (rung * step));
            }
        }
    }

    private static float Pressed(Key key) => Input.IsKeyPressed(key) ? 1f : 0f;

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
                _distance = Mathf.Max(0.05f, _distance * 0.88f);
                MoveCamera();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
                _distance = Mathf.Min(400f, _distance * 1.14f);
                MoveCamera();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                _dragging = click.Pressed;
                break;
            case InputEventMouseMotion motion when _dragging:
                _yaw -= motion.Relative.X * 0.008f;
                _pitch = Mathf.Clamp(_pitch - (motion.Relative.Y * 0.008f), -1.5f, 1.5f);
                MoveCamera();
                break;
            case InputEventKey { Pressed: true, Echo: false } key:
                OnKey(key.Keycode);
                break;
        }
    }

    private void OnKey(Key keycode)
    {
        switch (keycode)
        {
            case Key.Escape:
                GetTree().Quit(0);
                break;
            case Key.Tab:
                Focus(Input.IsKeyPressed(Key.Shift) ? Previous() : Next());
                break;
            case Key.Home or Key.Key0:
                Focus(-1);
                break;
            case >= Key.Key1 and <= Key.Key9:
                Focus(Math.Min((int)(keycode - Key.Key1), _targets.Count - 1));
                break;
            case Key.P:
                ShowPanel(!_panelShown);
                break;
        }
    }

    private int Next() => _focused < 0 ? 0 : (_focused + 1) % _targets.Count;

    private int Previous() => _focused <= 0 ? _targets.Count - 1 : _focused - 1;

    /// <summary>
    /// Puts the camera a readable distance from <paramref name="index"/>, or takes in the whole system at
    /// -1. A body is viewed from a few times its own radius, so a moon is approached closely and the star
    /// from far enough back to see it whole.
    /// </summary>
    private void Focus(int index)
    {
        _focused = index;
        if (index < 0)
        {
            _focus = Vector3.Zero;
            _distance = OuterRing * 1.6f;
        }
        else
        {
            Target target = _targets[index];
            _focus = target.Position;
            _distance = Mathf.Max(target.Radius * 4f, 1.2f);
        }

        MoveCamera();
    }

    private void MoveCamera()
    {
        float horizontal = _distance * Mathf.Cos(_pitch);
        _camera.Position = _focus + new Vector3(horizontal * Mathf.Sin(_yaw), _distance * Mathf.Sin(_pitch), horizontal * Mathf.Cos(_yaw));
        _camera.LookAt(_focus, Vector3.Up);

        // The panel changes form only when the view crosses between a body and the whole system, not on
        // every frame of a drag.
        if (_focused >= 0 != _tableShown)
        {
            _tableShown = _focused >= 0;
            _description.Text = PanelText();
        }

        _hud.Text = Status();
    }

    private string Status()
    {
        string where = _focused switch
        {
            -2 => "free",
            < 0 => "the whole system",
            _ => _targets[_focused].Name,
        };

        return $"at {where}    tab/shift-tab body, 1-9 body, 0 whole system, w a s d q e move, shift faster, drag turn, wheel closer, p {(_panelShown ? "hide" : "show")} panel, esc quit";
    }

    private static WorldEnvironment Environment() => new()
    {
        Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.02f, 0.02f, 0.05f),
            // A review harness, not a simulation: the fill light is what makes a body's own material
            // legible at the outer orbits, where the star alone leaves it too dark to judge.
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.42f, 0.42f, 0.50f),
            AmbientLightEnergy = 1.6f,
        },
    };

    private static OmniLight3D Light() => new() { Position = Vector3.Zero, OmniRange = 400f, LightEnergy = 4.0f, OmniAttenuation = 0.35f };

    private Node3D Star(float radius)
    {
        Color colour = StarColour();
        var star = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = radius, Height = radius * 2f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = colour, EmissionEnabled = true, Emission = colour, EmissionEnergyMultiplier = 1.0f },
        };

        // The star wears exactly what the description says of it, on the same rule and the same leader.
        _captions.Add(Wear(
            star,
            _destination.Description.StarLine,
            $"star, class {_destination.Description.Star.Type}, {_destination.Description.Star.TemperatureKelvin} K",
            colour.Lightened(0.3f),
            radius));

        return star;
    }

    private Node3D Body(BodyDescription body, Vector3 position, float radius)
    {
        // The material the body's own definition names, built from the stored record by the adapter
        // (decisions 0031, 0055); a stand-in coloured by body type only where the content has none, which
        // is every pack published before registry revision 3.
        GraphNode? source = Source(body);
        StandardMaterial3D? stored = source is null ? null : _resources.For(source.Definition);
        Color colour = stored?.AlbedoColor ?? BodyColour(body.Type);
        var node = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = radius, Height = radius * 2f },
            MaterialOverride = stored ?? new StandardMaterial3D { AlbedoColor = colour, Roughness = 0.85f },
            Position = position,
            Basis = source is null ? Basis.Identity : Orientation(source),
        };

        // Every body wears its own line of the description, the same one the panel lists, so what is read
        // on the body and what is read in the text cannot disagree. Its short form stands there always and
        // the whole line appears as the camera comes near, which is the only way eight of them fit at once.
        // The caption takes the body's own colour and stands on a leader line rising from it, so which
        // text belongs to which object is never in doubt even where two bodies sit close together.
        _captions.Add(Wear(node, SystemDescription.LineFor(body), SystemDescription.BriefFor(body), colour.Lightened(0.45f), radius));

        if (source is not null && Air(source, radius) is { } air)
        {
            node.AddChild(air);
        }

        if (body.LandingCandidate)
        {
            node.AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = radius * 1.5f, OuterRadius = radius * 1.7f },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.3f, 1f, 0.4f),
                    EmissionEnabled = true,
                    Emission = new Color(0.3f, 1f, 0.4f),
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
            });
        }

        return node;
    }

    /// <summary>
    /// Hangs a caption on <paramref name="body"/>: a label carrying the body's own line, in the body's own
    /// colour, and the leader that ties the two. Both are drawn on the panel rather than in the world, so
    /// a caption keeps one size on screen however near the camera is and can be placed clear of its
    /// neighbours' by <see cref="PlaceCaptions"/>.
    /// </summary>
    private Caption Wear(Node3D body, string full, string brief, Color colour, float radius)
    {
        var leader = new Line2D { Width = 1f, DefaultColor = colour with { A = 0.55f }, Antialiased = true };
        var label = new Label
        {
            Text = brief,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };

        label.AddThemeFontOverride("font", CaptionFont);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", colour);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", 5);

        _marks.AddChild(leader);
        _canvas.AddChild(label);

        return new Caption(body, Wrap(full), Wrap(brief), label, leader, radius);
    }

    /// <summary>The monospace face the panel and every caption share, so a caption reads as the panel's own row.</summary>
    private static SystemFont CaptionFont => new() { FontNames = ["monospace", "Monospace", "DejaVu Sans Mono"] };

    /// <summary>Breaks a description line into readable rows without losing a field across the break.</summary>
    private static string Wrap(string text)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rows = new List<string>();
        var row = new System.Text.StringBuilder();
        foreach (string word in words)
        {
            if (row.Length > 0 && row.Length + word.Length + 1 > 34)
            {
                rows.Add(row.ToString());
                row.Clear();
            }

            row.Append(row.Length > 0 ? " " : string.Empty).Append(word);
        }

        if (row.Length > 0)
        {
            rows.Add(row.ToString());
        }

        return string.Join("\n", rows);
    }

    private static MeshInstance3D OrbitRing(Orbit orbit)
    {
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        for (int step = 0; step <= 128; step++)
        {
            mesh.SurfaceAddVertex(orbit.At(Mathf.Tau * step / 128f));
        }

        mesh.SurfaceEnd();

        return new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.35f, 0.4f, 0.55f),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            },
        };
    }

    private CanvasLayer Panel()
    {
        SystemFont font = CaptionFont;
        _description = new Label
        {
            Text = PanelText(),
            Position = new Vector2(16f, 12f),
        };

        _description.AddThemeFontOverride("font", font);
        _description.AddThemeFontSizeOverride("font_size", 13);
        _description.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));

        // The text the system is judged by is read over whatever the view draws behind it, so it keeps its
        // own ground rather than competing with an orbit ring for the same pixels.
        _behind = new ColorRect { Color = new Color(0.02f, 0.02f, 0.05f, 0.93f), Position = new Vector2(8f, 6f), MouseFilter = Control.MouseFilterEnum.Ignore };

        _hud = new Label { Position = new Vector2(16f, 8f), GrowVertical = Control.GrowDirection.Begin, AnchorTop = 1f, AnchorBottom = 1f, OffsetTop = -34f };
        _hud.AddThemeFontOverride("font", font);
        _hud.AddThemeFontSizeOverride("font_size", 13);
        _hud.AddThemeColorOverride("font_color", new Color(0.55f, 0.95f, 0.7f));

        var layer = new CanvasLayer { Layer = 1 };
        layer.AddChild(_behind);
        layer.AddChild(_description);
        layer.AddChild(_hud);
        return layer;
    }

    /// <summary>
    /// What the panel says, which follows where the view is (decision 0054). While the whole system is
    /// framed it carries the summary alone — what was asked for, what it is pinned to, the star, the
    /// totals, and the verdict — because every body already wears its own row of the table; at a body the
    /// whole table appears, where one row is what matters and the system is out of frame anyway.
    /// </summary>
    private string PanelText()
    {
        string description = _tableShown ? _destination.Description.Text : Summary(_destination.Description.Text);
        return $"{Heading()}\n{description}{Verdict()}\n{Legend()}";
    }

    /// <summary>The description without its body rows: those are indented and the summary rows are not.</summary>
    private static string Summary(string text) =>
        string.Concat(text.Split('\n').Where(line => !line.StartsWith("  ", StringComparison.Ordinal)).Select(line => line + "\n"));

    /// <summary>Hides or shows the panel, so a picture can be taken of the system alone.</summary>
    private void ShowPanel(bool shown)
    {
        _panelShown = shown;
        _behind.Visible = shown;
        _description.Visible = shown;
        _hud.Text = Status();
    }

    private string Heading() =>
        $"destination   tier {TierRules.Label(_destination.Tier)}, seed {_destination.Seed}\n" +
        $"drawn         attempt {_destination.Attempt + 1} of at most {DestinationComposer.MaxAttempts}; composition seed {_destination.CompositionSeed}";

    private string Verdict()
    {
        IReadOnlyList<string> failures = TierRules.Check(_destination.Description, _destination.Tier, TierProfile.Version1);
        return failures.Count == 0
            ? $"tier          pass ({TierRules.Label(_destination.Tier)})"
            : $"tier          fail ({TierRules.Label(_destination.Tier)}): {string.Join("; ", failures)}";
    }

    private static string Legend() =>
        "\nview          not to scale. Orbit radii are placed logarithmically between this system's own\n" +
        "              innermost and outermost. The star and every body share one scale, each drawn from\n" +
        "              its own stored radius raised to the power 0.4, so order and rank read and true\n" +
        "              proportion does not: a star nine times a gas giant is drawn two and a half times\n" +
        "              it. A body stands on its own pole. A moon is offset beside its planet.";

    /// <summary>The angle of a body's named orbital element, from the binary turn the graph stores (decision 0036).</summary>
    /// <summary>
    /// The shell a body's atmosphere primitive describes, or null where it has none or is airless. Its
    /// colour is the `scattering-tint` the record stores and its depth and opacity follow the surface
    /// pressure, so the commonest reason a body is refused is visible rather than only written
    /// (decision 0057).
    /// </summary>
    private Node3D? Air(GraphNode body, float radius)
    {
        GraphNode? atmosphere = body.Children.SelectMany(children => children)
            .FirstOrDefault(child => _registry.TryFind(child.Definition.Category)?.Label == "atmosphere");

        if (atmosphere is null
            || atmosphere.Definition.TryParameter(_registry, "model") is not { } model
            || model.Descriptor.EnumLabels[(int)model.Value.AsEnumIndex] == "airless"
            || atmosphere.Definition.TryParameter(_registry, "scattering-tint") is not { } tint
            || atmosphere.Definition.TryParameter(_registry, "surface-pressure") is not { } pressure)
        {
            return null;
        }

        // Pressure spans five orders of magnitude, so its logarithm sets both how far the shell stands
        // off the surface and how much of the body it hides.
        double bar = Math.Max(1.0, pressure.Value.AsInteger / 100_000.0);
        float thickness = (float)Math.Clamp(0.03 + (0.05 * Math.Log10(bar)), 0.03, 0.14);
        float opacity = (float)Math.Clamp(0.16 + (0.18 * Math.Log10(bar)), 0.16, 0.62);

        (byte red, byte green, byte blue, _) = tint.Value.AsColour;
        return new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = radius * (1f + thickness), Height = 2f * radius * (1f + thickness) },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(Color.Color8(red, green, blue), opacity),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                CullMode = BaseMaterial3D.CullModeEnum.Back,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
                Roughness = 1f,
            },
        };
    }

    /// <summary>The graph node <paramref name="body"/> was described from.</summary>
    private GraphNode? Source(BodyDescription body) =>
        _destination.Graph.Nodes.FirstOrDefault(candidate => candidate.Path == body.Path);

    /// <summary>
    /// How a body stands: its pole where the content puts it, and its prime meridian turned to where the
    /// content puts that. Every body was drawn upright before, so a banded surface was seen down its own
    /// axis and read as a bullseye rather than as latitude bands (decision 0057).
    /// </summary>
    private Basis Orientation(GraphNode node)
    {
        float rightAscension = Turn(node, "pole-right-ascension");
        float declination = Turn(node, "pole-declination");
        float meridian = Turn(node, "prime-meridian-phase");

        // The pole is the body's own up: swing it to its right ascension, tilt it by its declination, and
        // spin the body about it to its prime meridian.
        return new Basis(Vector3.Up, rightAscension) * new Basis(Vector3.Right, declination) * new Basis(Vector3.Up, meridian);
    }

    /// <summary>A binary-turn parameter of a node's definition, in radians; zero where the category has none (decision 0036).</summary>
    private float Turn(GraphNode node, string label) =>
        node.Definition.TryParameter(_registry, label) is { } parameter && parameter.Descriptor.Kind == ParameterKind.BinaryTurn
            ? (float)(parameter.Value.AsBinaryTurn / 4294967296.0 * Mathf.Tau)
            : 0f;

    private float Angle(BodyDescription body, string component)
    {
        GraphNode? node = _destination.Graph.Nodes.FirstOrDefault(candidate => candidate.Path == body.Path);
        return node?.Transform is { } transform ? (float)(transform.Component(component) / 4294967296.0 * Mathf.Tau) : 0f;
    }

    /// <summary>
    /// One orbit as the stored elements describe it, on a semi-major axis the view scaled: the ellipse of
    /// its eccentricity with the star at a focus, turned by its argument of periapsis, tilted by its
    /// inclination, and swung to its ascending node (decisions 0036, 0057).
    /// </summary>
    private readonly record struct Orbit(float SemiMajor, float Eccentricity, float Inclination, float AscendingNode, float Periapsis, float MeanAnomaly)
    {
        /// <summary>The point at true anomaly <paramref name="trueAnomaly"/>, measured from periapsis.</summary>
        public Vector3 At(float trueAnomaly)
        {
            float radius = SemiMajor * (1f - (Eccentricity * Eccentricity)) / (1f + (Eccentricity * Mathf.Cos(trueAnomaly)));
            var inPlane = new Vector3(radius * Mathf.Cos(trueAnomaly), 0f, radius * Mathf.Sin(trueAnomaly));

            return new Basis(Vector3.Up, AscendingNode)
                * new Basis(Vector3.Right, Inclination)
                * new Basis(Vector3.Up, Periapsis)
                * inPlane;
        }

        /// <summary>
        /// The true anomaly the stored mean anomaly gives, by Kepler's equation. Newton's method from the
        /// mean anomaly converges in a handful of steps at the eccentricities a grammar admits, and this
        /// is presentation rather than a stored quantity, so it is floating point and no record depends
        /// on it; the two-body propagation of decision 0037 is still not implemented and this does not
        /// stand in for it, because nothing here moves with time.
        /// </summary>
        public float TrueAnomaly()
        {
            float eccentric = MeanAnomaly;
            for (int step = 0; step < 8; step++)
            {
                float error = eccentric - (Eccentricity * Mathf.Sin(eccentric)) - MeanAnomaly;
                float slope = 1f - (Eccentricity * Mathf.Cos(eccentric));
                eccentric -= error / Mathf.Max(slope, 0.05f);
            }

            return Mathf.Atan2(
                Mathf.Sqrt(1f - (Eccentricity * Eccentricity)) * Mathf.Sin(eccentric),
                Mathf.Cos(eccentric) - Eccentricity);
        }
    }

    /// <summary>The orbit <paramref name="body"/> stores, on a semi-major axis of <paramref name="ring"/> units.</summary>
    private Orbit OrbitOf(BodyDescription body, float ring) => new(
        ring,
        // Eccentricity is a fraction of 2^32; a grammar bounds it well below one, and the clamp is only
        // so that a record from outside this build cannot produce a parabola.
        Math.Clamp(Fraction32(body, "eccentricity"), 0f, 0.9f),
        Angle(body, "inclination"),
        Angle(body, "ascending-node"),
        Angle(body, "argument-of-periapsis"),
        Angle(body, "mean-anomaly"));

    /// <summary>A transform component stored as a fraction of 2^32 (decision 0036).</summary>
    private float Fraction32(BodyDescription body, string component)
    {
        GraphNode? node = Source(body);
        return node?.Transform is { } transform ? (float)(transform.Component(component) / 4294967296.0) : 0f;
    }

    private static float Ring(long distanceMetres, double inner, double outer) =>
        InnerRing + ((OuterRing - InnerRing) * Fraction(distanceMetres, inner, outer));

    /// <summary>
    /// What a body of <paramref name="radiusUnits"/> is drawn at: the largest object in the system takes
    /// <see cref="LargestDrawn"/> and everything else follows its own stored radius raised to
    /// <see cref="RadiusExponent"/>, so the star is sized by what it stores like every other object and a
    /// dwarf smaller than a gas giant is drawn smaller (decision 0057).
    /// </summary>
    private static float Size(long radiusUnits, double largestRadius) =>
        radiusUnits <= 0 || largestRadius <= 0
            ? BarycentreDrawn
            : (float)Math.Max(BarycentreDrawn, LargestDrawn * Math.Pow(radiusUnits / largestRadius, RadiusExponent));

    /// <summary>Where <paramref name="value"/> falls between <paramref name="low"/> and <paramref name="high"/> on a logarithmic scale.</summary>
    private static float Fraction(long value, double low, double high)
    {
        if (value <= 0 || high <= low)
        {
            return 0f;
        }

        double span = Math.Log(high) - Math.Log(low);
        return span <= 0 ? 0.5f : (float)Math.Clamp((Math.Log(value) - Math.Log(low)) / span, 0.0, 1.0);
    }

    private static (double Inner, double Outer) OrbitSpan(BodyDescription[] bodies)
    {
        long[] distances = [.. bodies.Where(body => !body.Number.Contains('.', StringComparison.Ordinal)).Select(body => body.DistanceMetres).Where(distance => distance > 0)];
        return distances.Length == 0 ? (1.0, 2.0) : (distances.Min(), Math.Max(distances.Max(), distances.Min() * 1.5));
    }

    /// <summary>The largest stored radius in the system, star included, which sets the scale everything else is drawn on.</summary>
    private double LargestRadius(BodyDescription[] bodies) =>
        Math.Max(_destination.Description.Star.RadiusUnits, bodies.Select(body => body.RadiusUnits).DefaultIfEmpty(1L).Max());

    /// <summary>
    /// The star's colour, from the effective temperature it stores, by the named rule
    /// `derive-star-colour/1` in the core; the view held a palette keyed on spectral class before, which
    /// was one more property it invented from content it had (decision 0057).
    /// </summary>
    private Color StarColour()
    {
        (byte red, byte green, byte blue) = DerivationRules.StarColour(_destination.Description.Star.TemperatureKelvin);
        return Color.Color8(red, green, blue);
    }

    private static Color BodyColour(string bodyType) => bodyType switch
    {
        "rocky" => new Color(0.69f, 0.55f, 0.41f),
        "icy" => new Color(0.81f, 0.91f, 0.96f),
        "ocean" => new Color(0.18f, 0.44f, 0.69f),
        "gas-giant" => new Color(0.85f, 0.63f, 0.40f),
        _ => new Color(0.5f, 0.5f, 0.55f),
    };
}
