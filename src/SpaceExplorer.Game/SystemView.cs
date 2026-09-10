using Godot;
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
    private const float SmallestBody = 0.5f;
    private const float LargestBody = 1.8f;
    private const float StarRadius = 2.4f;

    /// <summary>How near the camera must be for a body to show its whole line rather than its name alone.</summary>
    private const float ReadingDistance = 14f;

    private readonly Destination _destination;
    private readonly CategoryRegistry _registry;

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

    private readonly string? _screenshot;
    private readonly string? _openOn;
    private int _framesDrawn;

    public SystemView(Destination destination, CategoryRegistry registry, string? screenshot = null, string? openOn = null)
    {
        _destination = destination;
        _registry = registry;
        _screenshot = screenshot;
        _openOn = openOn;
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
        AddChild(Star());
        _targets.Add(new Target($"star, class {_destination.Description.Star.Type}", Vector3.Zero, StarRadius));

        BodyDescription[] bodies = [.. _destination.Description.Bodies];
        (double smallest, double largest) = RadiusSpan(bodies);
        (double inner, double outer) = OrbitSpan(bodies);

        var placed = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        foreach (BodyDescription body in bodies)
        {
            int dot = body.Number.IndexOf('.', StringComparison.Ordinal);
            Vector3 position;
            if (dot < 0)
            {
                float ring = Ring(body.DistanceMetres, inner, outer);
                AddChild(OrbitRing(ring, Angle(body, "inclination")));
                position = Place(ring, Angle(body, "mean-anomaly"), Angle(body, "inclination"));
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
            float radius = Size(body, smallest, largest);
            AddChild(Body(body, position, radius));
            _targets.Add(new Target($"{body.Number} {body.Type}{(body.LandingCandidate ? ", landable" : string.Empty)}", position, radius));
        }

        AddChild(Light());
        _camera = new Camera3D { Far = 2_000f, Near = 0.02f };
        AddChild(_camera);

        // A review can open straight at a body, so a picture of one can be taken without a hand on the keys.
        int opening = _openOn is null ? -1 : _targets.FindIndex(target => target.Name.StartsWith(_openOn + " ", StringComparison.Ordinal) || target.Name.StartsWith(_openOn + ",", StringComparison.Ordinal));
        Focus(_openOn is null ? -1 : Math.Max(opening, 0));
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
        List<Rect2> taken = [new Rect2(_description.Position, _description.GetMinimumSize()).Grow(6f), new Rect2(0f, viewport.Y - 40f, viewport.X, 40f)];

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

        return $"at {where}    tab/shift-tab body, 1-9 body, 0 whole system, w a s d q e move, shift faster, drag turn, wheel closer, esc quit";
    }

    private static WorldEnvironment Environment() => new()
    {
        Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.02f, 0.02f, 0.05f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.25f, 0.25f, 0.32f),
            AmbientLightEnergy = 1.0f,
        },
    };

    private static OmniLight3D Light() => new() { Position = Vector3.Zero, OmniRange = 400f, LightEnergy = 2.0f };

    private Node3D Star()
    {
        Color colour = StarColour(_destination.Description.Star.Type);
        var star = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = StarRadius, Height = StarRadius * 2f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = colour, EmissionEnabled = true, Emission = colour, EmissionEnergyMultiplier = 1.0f },
        };

        // The star wears exactly what the description says of it, on the same rule and the same leader.
        _captions.Add(Wear(
            star,
            _destination.Description.StarLine,
            $"star, class {_destination.Description.Star.Type}, {_destination.Description.Star.TemperatureKelvin} K",
            colour.Lightened(0.3f),
            StarRadius));

        return star;
    }

    private Node3D Body(BodyDescription body, Vector3 position, float radius)
    {
        Color colour = BodyColour(body.Type);
        var node = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = radius, Height = radius * 2f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = colour, Roughness = 0.85f },
            Position = position,
        };

        // Every body wears its own line of the description, the same one the panel lists, so what is read
        // on the body and what is read in the text cannot disagree. Its short form stands there always and
        // the whole line appears as the camera comes near, which is the only way eight of them fit at once.
        // The caption takes the body's own colour and stands on a leader line rising from it, so which
        // text belongs to which object is never in doubt even where two bodies sit close together.
        _captions.Add(Wear(node, SystemDescription.LineFor(body), SystemDescription.BriefFor(body), colour.Lightened(0.45f), radius));

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

    private static MeshInstance3D OrbitRing(float radius, float inclination)
    {
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        for (int step = 0; step <= 96; step++)
        {
            float angle = Mathf.Tau * step / 96f;
            mesh.SurfaceAddVertex(Place(radius, angle, inclination));
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
            Text = $"{Heading()}\n{_destination.Description.Text}{Verdict()}\n{Legend()}",
            Position = new Vector2(16f, 12f),
        };

        _description.AddThemeFontOverride("font", font);
        _description.AddThemeFontSizeOverride("font_size", 13);
        _description.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));

        // The text the system is judged by is read over whatever the view draws behind it, so it keeps its
        // own ground rather than competing with an orbit ring for the same pixels.
        _behind = new ColorRect { Color = new Color(0.02f, 0.02f, 0.05f, 0.55f), Position = new Vector2(8f, 6f), MouseFilter = Control.MouseFilterEnum.Ignore };

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

    private string Heading() =>
        $"destination   tier {TierRules.Label(_destination.Tier)}, seed {_destination.Seed}\n" +
        $"drawn         attempt {_destination.Attempt + 1} of at most {DestinationComposer.MaxAttempts}; composition seed {_destination.CompositionSeed}";

    private string Verdict()
    {
        IReadOnlyList<string> failures = TierRules.Check(_destination.Description, _destination.Tier);
        return failures.Count == 0
            ? $"tier          pass ({TierRules.Label(_destination.Tier)})"
            : $"tier          fail ({TierRules.Label(_destination.Tier)}): {string.Join("; ", failures)}";
    }

    private static string Legend() =>
        "\nview          not to scale: orbit radii and body radii are each placed logarithmically between\n" +
        "              this system's own smallest and largest, so order and ratio are faithful and size is\n" +
        "              not. A moon is offset beside its planet rather than on its own orbit.";

    /// <summary>The angle of a body's named orbital element, from the binary turn the graph stores (decision 0036).</summary>
    private float Angle(BodyDescription body, string component)
    {
        GraphNode? node = _destination.Graph.Nodes.FirstOrDefault(candidate => candidate.Path == body.Path);
        return node?.Transform is { } transform ? (float)(transform.Component(component) / 4294967296.0 * Mathf.Tau) : 0f;
    }

    private static Vector3 Place(float radius, float angle, float inclination) =>
        new(radius * Mathf.Cos(angle), radius * Mathf.Sin(inclination) * Mathf.Cos(angle), radius * Mathf.Sin(angle));

    private static float Ring(long distanceMetres, double inner, double outer) =>
        InnerRing + ((OuterRing - InnerRing) * Fraction(distanceMetres, inner, outer));

    private static float Size(BodyDescription body, double smallest, double largest) =>
        body.Role == BodyRole.Barycentre ? 0.08f : SmallestBody + ((LargestBody - SmallestBody) * Fraction(body.RadiusUnits, smallest, largest));

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

    private static (double Smallest, double Largest) RadiusSpan(BodyDescription[] bodies)
    {
        long[] radii = [.. bodies.Select(body => body.RadiusUnits).Where(radius => radius > 0)];
        return radii.Length == 0 ? (1.0, 2.0) : (radii.Min(), Math.Max(radii.Max(), radii.Min() * 1.5));
    }

    private static Color StarColour(string spectralClass) => spectralClass switch
    {
        "O" => new Color(0.61f, 0.69f, 1f),
        "B" => new Color(0.67f, 0.75f, 1f),
        "A" => new Color(0.79f, 0.84f, 1f),
        "F" => new Color(0.97f, 0.97f, 1f),
        "G" => new Color(1f, 0.96f, 0.92f),
        "K" => new Color(1f, 0.82f, 0.63f),
        _ => new Color(1f, 0.70f, 0.44f),
    };

    private static Color BodyColour(string bodyType) => bodyType switch
    {
        "rocky" => new Color(0.69f, 0.55f, 0.41f),
        "icy" => new Color(0.81f, 0.91f, 0.96f),
        "ocean" => new Color(0.18f, 0.44f, 0.69f),
        "gas-giant" => new Color(0.85f, 0.63f, 0.40f),
        _ => new Color(0.5f, 0.5f, 0.55f),
    };
}
