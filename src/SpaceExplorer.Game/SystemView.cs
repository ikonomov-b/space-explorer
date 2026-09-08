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
    /// What each body's caption needs to be placed: the body, its two forms, the leader tying them, the
    /// body's own radius, and how many steps this caption is staggered above its neighbours'.
    /// </summary>
    private sealed record Caption3D(Node3D Body, Label3D Full, Label3D Brief, Node3D Leader, float Radius, float Stagger);

    private readonly List<Caption3D> _captions = [];

    private Camera3D _camera = null!;
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
        AddChild(Panel());

        // A review can open straight at a body, so a picture of one can be taken without a hand on the keys.
        int opening = _openOn is null ? -1 : _targets.FindIndex(target => target.Name.StartsWith(_openOn + " ", StringComparison.Ordinal) || target.Name.StartsWith(_openOn + ",", StringComparison.Ordinal));
        Focus(_openOn is null ? -1 : Math.Max(opening, 0));
    }

    public override void _Process(double delta)
    {
        ShowNearCaptions();

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
    /// Places every caption for the distance it is being read from. A body near the camera shows its whole
    /// line and a far one its short form, and each caption stands off its body in proportion to that
    /// distance, so captions keep the same separation on screen whether the view takes in one body or the
    /// whole system, and none is thrown off the top of it.
    /// </summary>
    private void ShowNearCaptions()
    {
        Vector3 eye = _camera.GlobalPosition;
        foreach (Caption3D caption in _captions)
        {
            float distance = eye.DistanceTo(caption.Body.GlobalPosition);
            bool near = distance < ReadingDistance;
            caption.Full.Visible = near;
            caption.Brief.Visible = !near;

            float lead = caption.Radius + (0.06f * distance * (1f + caption.Stagger));

            // Captions of bodies that sit on top of each other, a planet and its moons, are also drawn
            // apart across the screen: the offset follows the camera's own right, so it stays a sideways
            // step however the view is turned.
            Vector3 sideways = caption.Stagger == 0f
                ? Vector3.Zero
                : _camera.GlobalTransform.Basis.X * (0.05f * distance * (caption.Stagger % 2f == 0f ? 1f : -1f));

            var anchor = new Vector3(0f, lead, 0f);
            caption.Full.Position = anchor + sideways;
            caption.Brief.Position = anchor + sideways;
            caption.Leader.Scale = new Vector3(1f, Mathf.Max(lead - caption.Radius, 0.01f), 1f);
            caption.Leader.Position = new Vector3(0f, caption.Radius, 0f);
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
        Color caption = colour.Lightened(0.3f);
        Label3D brief = Caption($"star, class {_destination.Description.Star.Type}, {_destination.Description.Star.TemperatureKelvin} K", caption);
        Label3D full = Caption(_destination.Description.StarLine, caption);
        MeshInstance3D leader = Leader(caption);
        full.Visible = false;
        star.AddChild(leader);
        star.AddChild(brief);
        star.AddChild(full);
        _captions.Add(new Caption3D(star, full, brief, leader, StarRadius, 0f));

        return star;
    }

    private Node3D Body(BodyDescription body, Vector3 position, float radius)
    {
        List<Caption3D> captions = _captions;
        Color colour = BodyColour(body.Type);
        var node = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = radius, Height = radius * 2f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = colour, Roughness = 0.85f },
            Position = position,
        };

        // Every body wears its own line of the description, the same one the panel lists, so what is read
        // on the body and what is read in the text cannot disagree. Its name stands there always and the
        // whole line appears as the camera comes near, which is the only way eight of them fit at once.
        // The caption takes the body's own colour and stands on a leader line rising from it, so which
        // text belongs to which object is never in doubt even where two bodies sit close together.
        Color caption = colour.Lightened(0.45f);
        Label3D brief = Caption(SystemDescription.BriefFor(body), caption);
        Label3D full = Caption(SystemDescription.LineFor(body), caption);
        MeshInstance3D leader = Leader(caption);
        full.Visible = false;
        node.AddChild(leader);
        node.AddChild(brief);
        node.AddChild(full);
        captions.Add(new Caption3D(node, full, brief, leader, radius, Stagger(body.Number)));

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
    /// A body's own description standing above it, wrapped so a long line reads as a block rather than a
    /// banner, and always facing the camera.
    /// </summary>
    /// <summary>
    /// A caption above a body: one size on screen however near or far the camera is, so it neither
    /// vanishes across the system nor swallows the body it belongs to.
    /// </summary>
    private static Label3D Caption(string text, Color colour) => new()
    {
        Text = Wrap(text),
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        FixedSize = true,
        Modulate = colour,
        OutlineModulate = new Color(0f, 0f, 0f, 0.95f),
        OutlineSize = 20,
        FontSize = 48,
        PixelSize = 0.00042f,
        NoDepthTest = true,
        RenderPriority = 4,
        HorizontalAlignment = HorizontalAlignment.Left,
    };

    /// <summary>Breaks a description line into readable rows without losing a field across the break.</summary>
    private static string Wrap(string text)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rows = new List<string>();
        var row = new System.Text.StringBuilder();
        foreach (string word in words)
        {
            if (row.Length > 0 && row.Length + word.Length + 1 > 26)
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

    /// <summary>The line that ties a caption to the body under it, one unit long and scaled to reach it.</summary>
    private static MeshInstance3D Leader(Color colour)
    {
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        mesh.SurfaceAddVertex(Vector3.Zero);
        mesh.SurfaceAddVertex(new Vector3(0f, 1f, 0f));
        mesh.SurfaceEnd();

        return new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = colour,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                NoDepthTest = true,
            },
        };
    }

    /// <summary>Staggers a moon's caption above its planet's so two captions on one spot do not sit on each other.</summary>
    private static float Stagger(string number) =>
        number.IndexOf('.', StringComparison.Ordinal) < 0 ? 0f : 1f + (number[^1] - '0');

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
        var font = new SystemFont { FontNames = ["monospace", "Monospace", "DejaVu Sans Mono"] };
        var description = new Label
        {
            Text = $"{Heading()}\n{_destination.Description.Text}{Verdict()}\n{Legend()}",
            Position = new Vector2(16f, 12f),
        };

        description.AddThemeFontOverride("font", font);
        description.AddThemeFontSizeOverride("font_size", 13);
        description.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));

        _hud = new Label { Position = new Vector2(16f, 8f), GrowVertical = Control.GrowDirection.Begin, AnchorTop = 1f, AnchorBottom = 1f, OffsetTop = -34f };
        _hud.AddThemeFontOverride("font", font);
        _hud.AddThemeFontSizeOverride("font_size", 13);
        _hud.AddThemeColorOverride("font_color", new Color(0.55f, 0.95f, 0.7f));

        var layer = new CanvasLayer();
        layer.AddChild(description);
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
