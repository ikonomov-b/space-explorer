using Godot;
using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Shared;
using SpaceExplorer.Persistence;
using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using GraphNode = SpaceExplorer.Core.Registry.GraphNode;

namespace SpaceExplorer.Game;

/// <summary>
/// The review view of one stored region: the ground a planet carries, drawn at its true size and wearing
/// the material the planet stores, from the two frames rung one is read in — a walk frame at eye height
/// and a top-down frame of the whole region with a scale bar
/// ([decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)).
/// From rung 5 the walk frame's ground no longer ends at the region's edge: a generalized far field
/// continues it out to the geometric horizon
/// ([decision 0071](../../../docs/decisions/0071-the-explorable-planet-is-the-subject-the-far-field-before-features-and-a-two-window-inspection.md)).
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
/// legend rather than passed off as derived; the far field's horizon distance is the flat-model geometric
/// one decision 0041 already derives the minimum landable radius from, not decision 0032's celestial one.
/// </remarks>
public partial class SurfaceView : Node3D
{
    /// <summary>Eye height in metres: the height decision 0041's minimum landable radius is derived from.</summary>
    private const float EyeHeight = 2f;

    /// <summary>
    /// The sea, where the planet declares one: a flat plane at the stored datum, spanning the region.
    /// </summary>
    /// <remarks>
    /// Drawn flat and not curved, which is decision 0041's already-accepted flat-model error rather than a
    /// new one: the datum is a sphere of radius R + sea-level and the region a plane tangent at R, so the
    /// water stands up to 2.0 m proud at a maximal region's corner on the smallest landable body. Decision
    /// 0070 clause 6 names that and declines to pay for it twice.
    ///
    /// Its colour is the harness's own and the legend says so. What the content stores is which biome lies
    /// under the water and what that biome is made of, not what the water looks like from above.
    /// </remarks>
    private MeshInstance3D? Sea()
    {
        if (_seaLevelMetres <= CategoryRegistryRevision9.SeaLevelFloorMetres || _palette.Count == 0)
        {
            return null;
        }

        var water = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.16f, 0.34f, 0.52f, 0.72f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.12f,
            Metallic = 0.1f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };

        return new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(_extentMetres, _extentMetres) },
            MaterialOverride = water,
            Position = new Vector3(0f, _seaLevelMetres, 0f),
            Name = "HarnessSea",
        };
    }

    /// <summary>How far the walk frame sees at minimum; a maximal region's far corner is 2,896 m from its centre.</summary>
    private const float MinimumViewDistance = 4_096f;

    /// <summary>
    /// The far field's own generalized stride: a fixed sample budget for the whole far diameter, so a huge
    /// body's ground is coarser rather than a huger mesh, and a small one's is finer — the resolution a
    /// generalization needs, where the region itself already carries the fine ground
    /// ([decision 0071](../../../docs/decisions/0071-the-explorable-planet-is-the-subject-the-far-field-before-features-and-a-two-window-inspection.md)
    /// clause 5).
    /// </summary>
    private const int FarSamplesAcross = 129;

    /// <summary>A brisk human walking pace, so crossing a maximal region on foot takes minutes and not seconds.</summary>
    private const float WalkSpeed = 3.2f;

    /// <summary>
    /// What `shift` multiplies the pace by: enough to cross a maximal region in some twenty seconds, because
    /// a reader judging a whole region's relief is surveying it rather than walking it, and at a walking
    /// pace 2,048 m is ten minutes of holding a key.
    /// </summary>
    private const float SurveyMultiplier = 28f;

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
    private readonly int _storedBytes;
    private readonly bool _groundWasGenerated;

    private readonly double _horizonMetres;
    private readonly short[] _farHeights;
    private readonly int _farAcross;
    private readonly long _farCellMetres;
    private readonly long _farExtentMetres;
    private readonly float _viewDistance;

    private readonly IReadOnlyList<PrimitiveDefinition> _palette;
    private readonly byte[] _index;
    private readonly long _seaLevelMetres;
    private readonly int _patchCount;
    private int _surfacesDrawn;

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
        DataRoot root,
        PackId graphPack,
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

        // Built by the same code the composer grounds a destination with, so the view cannot describe a
        // field the generator would not have produced.
        _relief = GroundPublisher.ReliefFieldOf(body, registry, compositionSeed);

        int latitude = (int)(region.Transform?.Component("latitude") ?? 0);
        int longitude = (int)(region.Transform?.Component("longitude") ?? 0);
        int anchorHeading = (int)(region.Transform?.Component("heading") ?? 0);

        // The ground comes off the disk, never out of a rule run here: where none has been stored, the
        // store generates and publishes one and hands back what it wrote, so the picture is of bytes the
        // data root holds either way
        // ([decision 0066](../../../docs/decisions/0066-generation-writes-to-the-database-and-the-view-only-reads.md)
        // clause 1). Nothing in this class calls `terrain-heightfield`.
        RegionPayload payload = PayloadStore.Resolve(
            root,
            graphPack,
            region.Path,
            _relief,
            latitude,
            longitude,
            anchorHeading,
            _extentMetres,
            out bool generated);

        _heights = payload.Heights;
        _across = payload.Across;
        _storedBytes = payload.Bytes.Length;
        _groundWasGenerated = generated;
        _patchCount = payload.Patches?.Count ?? 0;

        // Rung 5: the ground between the region's edge and the geometric horizon, from the same planet-fixed
        // field the region itself reads and never stored, because it is a pure function of what the region's
        // own relief field already is ([decision 0071](../../../docs/decisions/0071-the-explorable-planet-is-the-subject-the-far-field-before-features-and-a-two-window-inspection.md)
        // clause 5, [decision 0041](../../../docs/decisions/0041-planet-fields-tangent-regions-minimum-radius-and-far-field.md)).
        _horizonMetres = GeometricHorizon.DistanceMetres(_relief.ReferenceRadiusUnits, EyeHeight);

        // A fixed sample budget for the whole far diameter, so the cell coarsens with a body's size instead
        // of the mesh growing with it, then rounded outward to a whole number of its own cells so the extent
        // divides the cell exactly, which is what SampleFar demands.
        _farCellMetres = Math.Max(TerrainHeightfield.CellMetres, (long)Math.Ceiling(2.0 * _horizonMetres / (FarSamplesAcross - 1)));
        long farHalfCells = (long)Math.Ceiling(_horizonMetres / _farCellMetres);
        _farExtentMetres = 2 * farHalfCells * _farCellMetres;
        _farAcross = (int)(2 * farHalfCells) + 1;
        _farHeights = TerrainHeightfield.SampleFar(_relief, latitude, longitude, anchorHeading, _farExtentMetres, _farCellMetres);
        _viewDistance = Math.Max(MinimumViewDistance, (float)_horizonMetres * 1.1f);

        // The palette is the planet's and reaches the region as data, which is what decision 0070 clause 3
        // means by parent-to-child: the region draws from it and does not own it.
        _palette = Palette(body, registry, resources);
        _seaLevelMetres = body.Definition.TryParameter(registry, CategoryRegistryRevision9.SeaLevelParameter)?.Value.AsInteger
            ?? CategoryRegistryRevision9.SeaLevelFloorMetres;

        // Derived here and stored nowhere, which is requirement R18's two tiers: the patches are permanent
        // and this is what a renderer wants from them.
        _index = _palette.Count > 0 && payload.Patches is { Count: > 0 } patches
            ? BiomeIndex.Derive(_heights, _extentMetres, patches, Claims(_palette, registry), _seaLevelMetres, _relief.AmplitudeMetres)
            : [];
    }

    public override void _Ready()
    {
        AddChild(Ground());
        AddChild(FarGround());
        if (Sea() is { } sea)
        {
            AddChild(sea);
        }

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

        float speed = WalkSpeed * (Input.IsKeyPressed(Key.Shift) ? SurveyMultiplier : 1f) * (float)delta;
        Basis flat = Basis.FromEuler(new Vector3(0f, _yaw, 0f));
        // The basis's Z points behind the eye, so w giving a negative move.Z is already forward: negating
        // it here is what put the walk in reverse.
        Vector3 stepped = _walkedFrom + ((flat.X * move.X) + (flat.Z * move.Z)) * speed;

        // Held inside the region's own extent: the region edge is the traversal primitive's boundary
        // ([decision 0041](../../../docs/decisions/0041-planet-fields-tangent-regions-minimum-radius-and-far-field.md)),
        // enforced here rather than by the host tick this harness does not run. The far field beyond it is
        // drawn from rung 5 on, but has no collision, no placements and no state, so it is not a place to
        // walk to yet ([decision 0062](../../../docs/decisions/0062-the-surface-harness-walks-in-an-interactive-session.md)).
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

        // One surface per biome over one shared set of vertices: the triangles of a cell go to the surface
        // of the biome that cell wears, so each biome draws with its own stored material and the boundary
        // between two of them falls where `derive-biome-index/1` put it rather than where a shader guessed
        // ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
        // clause 11).
        int cells = _across - 1;
        int surfaces = Math.Max(1, _palette.Count);
        var byBiome = new List<int>[surfaces];
        for (int biome = 0; biome < surfaces; biome++)
        {
            byBiome[biome] = [];
        }

        for (int j = 0; j < cells; j++)
        {
            for (int i = 0; i < cells; i++)
            {
                int biome = _index.Length == 0 ? 0 : Math.Min(_index[(j * cells) + i], surfaces - 1);
                List<int> into = byBiome[biome];

                // Clockwise seen from above, which is the winding Godot takes for a front face: wound the
                // other way the ground is culled and a walker sees straight through the hill in front.
                int corner = (j * _across) + i;
                into.Add(corner);
                into.Add(corner + 1);
                into.Add(corner + _across);
                into.Add(corner + 1);
                into.Add(corner + _across + 1);
                into.Add(corner + _across);
            }
        }

        var mesh = new ArrayMesh();
        var drawn = new MeshInstance3D { Mesh = mesh, Name = "Ground" };

        int written = 0;
        for (int biome = 0; biome < surfaces; biome++)
        {
            if (byBiome[biome].Count == 0)
            {
                continue;
            }

            var surface = new Godot.Collections.Array();
            surface.Resize((int)Mesh.ArrayType.Max);
            surface[(int)Mesh.ArrayType.Vertex] = vertices;
            surface[(int)Mesh.ArrayType.Normal] = normals;
            surface[(int)Mesh.ArrayType.TexUV] = uvs;
            surface[(int)Mesh.ArrayType.Index] = byBiome[biome].ToArray();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface);

            // The biome's own stored material where the palette names one, and the body's own where no
            // palette exists, which is every graph composed before registry revision 9.
            drawn.SetSurfaceOverrideMaterial(written, _palette.Count > biome
                ? _resources.GroundFor(_palette[biome], _extentMetres)
                : _resources.GroundFor(_body.Definition, _extentMetres));
            written++;
        }

        _surfacesDrawn = written;
        return drawn;
    }

    /// <summary>
    /// The ground beyond the region's edge, out to the geometric horizon, generalized rather than cut:
    /// sampled from the same planet-fixed field the region reads, at a coarser stride, and never stored
    /// ([decision 0071](../../../docs/decisions/0071-the-explorable-planet-is-the-subject-the-far-field-before-features-and-a-two-window-inspection.md)
    /// clause 5, [decision 0041](../../../docs/decisions/0041-planet-fields-tangent-regions-minimum-radius-and-far-field.md)).
    /// It wears the planet's own stored surface material rather than a biome's, because a biome's patches
    /// are drawn on the region's own stream and have nothing to say beyond its edge.
    /// </summary>
    /// <remarks>
    /// A far cell whose whole footprint already lies under the region's own fine mesh is left undrawn, so
    /// the two meshes never compete for the same pixels. Their grids do not generally share a vertex at the
    /// seam, since the far field's cell is a generalization's stride and not the region's own 2 m one; that
    /// approximate join is what clause 5 accepts, and the exact one is rung 7's chunking to give.
    /// </remarks>
    private MeshInstance3D FarGround()
    {
        float step = _farCellMetres;
        float half = (_farAcross - 1) / 2f;
        float regionHalf = _extentMetres / 2f;

        var vertices = new Vector3[_farHeights.Length];
        var normals = new Vector3[_farHeights.Length];
        var uvs = new Vector2[_farHeights.Length];
        for (int j = 0; j < _farAcross; j++)
        {
            for (int i = 0; i < _farAcross; i++)
            {
                int at = (j * _farAcross) + i;
                vertices[at] = new Vector3((i - half) * step, FarHeightAt(i, j), (j - half) * step);

                float acrossX = FarHeightAt(i + 1, j) - FarHeightAt(i - 1, j);
                float acrossZ = FarHeightAt(i, j + 1) - FarHeightAt(i, j - 1);
                normals[at] = new Vector3(-acrossX, 2f * step, -acrossZ).Normalized();

                uvs[at] = new Vector2(i / (float)(_farAcross - 1), j / (float)(_farAcross - 1));
            }
        }

        var indices = new List<int>();
        int cells = _farAcross - 1;
        for (int j = 0; j < cells; j++)
        {
            for (int i = 0; i < cells; i++)
            {
                float minX = (i - half) * step;
                float maxX = (i + 1 - half) * step;
                float minZ = (j - half) * step;
                float maxZ = (j + 1 - half) * step;

                if (minX >= -regionHalf && maxX <= regionHalf && minZ >= -regionHalf && maxZ <= regionHalf)
                {
                    continue;
                }

                int corner = (j * _farAcross) + i;
                indices.Add(corner);
                indices.Add(corner + 1);
                indices.Add(corner + _farAcross);
                indices.Add(corner + 1);
                indices.Add(corner + _farAcross + 1);
                indices.Add(corner + _farAcross);
            }
        }

        var mesh = new ArrayMesh();
        var surface = new Godot.Collections.Array();
        surface.Resize((int)Mesh.ArrayType.Max);
        surface[(int)Mesh.ArrayType.Vertex] = vertices;
        surface[(int)Mesh.ArrayType.Normal] = normals;
        surface[(int)Mesh.ArrayType.TexUV] = uvs;
        surface[(int)Mesh.ArrayType.Index] = indices.ToArray();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface);

        var drawn = new MeshInstance3D { Mesh = mesh, Name = "FarGround" };
        drawn.SetSurfaceOverrideMaterial(0, _resources.GroundFor(_body.Definition, _farExtentMetres));
        return drawn;
    }

    /// <summary>The far field's own sampled height at a point, in metres, with the edge held rather than wrapped.</summary>
    private float FarHeightAt(int i, int j) =>
        _farHeights[(Math.Clamp(j, 0, _farAcross - 1) * _farAcross) + Math.Clamp(i, 0, _farAcross - 1)] / (float)ReliefField.HeightUnit;

    /// <summary>The biome definitions a planet's palette names, in palette order, or empty where it has none.</summary>
    private static IReadOnlyList<PrimitiveDefinition> Palette(GraphNode body, CategoryRegistry registry, PrimitiveResources resources)
    {
        if (body.Definition.TryParameter(registry, CategoryRegistryRevision9.BiomesParameter) is not { } parameter)
        {
            return [];
        }

        return [.. parameter.Value.AsRefList.Select(resources.Resolve)];
    }

    /// <summary>What each biome of the palette claims, as `derive-biome-index/1` reads it.</summary>
    private static BiomeClaim[] Claims(IReadOnlyList<PrimitiveDefinition> palette, CategoryRegistry registry) =>
        [.. palette.Select(biome => new BiomeClaim(
            biome.TryParameter(registry, CategoryRegistryRevision9.SubmergedParameter)!.Value.Value.AsBool,
            biome.TryParameter(registry, CategoryRegistryRevision9.ElevationLowParameter)!.Value.Value.AsInteger,
            biome.TryParameter(registry, CategoryRegistryRevision9.ElevationHighParameter)!.Value.Value.AsInteger))];

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

    /// <summary>How far the sun stands above the horizon: low, because a low sun is what makes relief readable.</summary>
    private const float SunElevationDegrees = 26f;

    /// <summary>
    /// The light, which the view invents and the legend admits to: there is no celestial solution yet, so
    /// this is a fixed low sun and not the star the system stores.
    /// </summary>
    /// <remarks>
    /// It stands low deliberately. Relief is read from the shadows it casts as much as from the shape
    /// itself, and a high sun flattens a landscape into one tone; a low one rakes across the ridges and
    /// says which way the ground falls. Its shadow reaches across the whole region rather than Godot's
    /// hundred-metre default, which on a 2,048 m plate would have shadowed only what is underfoot.
    /// </remarks>
    private DirectionalLight3D Sun() => new()
    {
        Rotation = new Vector3(-Mathf.DegToRad(SunElevationDegrees), Mathf.DegToRad(130f), 0f),
        LightEnergy = 1.25f,
        ShadowEnabled = true,
        DirectionalShadowMaxDistance = _extentMetres * 1.5f,
        DirectionalShadowSplit1 = 0.06f,
        DirectionalShadowSplit2 = 0.2f,
        DirectionalShadowSplit3 = 0.5f,
        ShadowNormalBias = 1.4f,
        ShadowBias = 0.06f,

        // The sun is drawn in the sky as well as lighting the ground, so a reader can see where the light
        // he is judging the relief by is coming from.
        SkyMode = DirectionalLight3D.SkyModeEnum.LightAndSky,
        Name = "HarnessLight",
    };

    /// <summary>
    /// A sky with a sun in it. Every part of it is the harness's own and the legend says so: the celestial
    /// solution of [decision 0032](../../../docs/decisions/0032-astronomically-consistent-surface-sky.md)
    /// is rung 6 and unbuilt, so this is not the star the system stores, nor its colour, nor its angular
    /// size, nor where it would actually stand in this region's sky.
    /// </summary>
    private static WorldEnvironment Sky() => new()
    {
        Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky
            {
                SkyMaterial = new ProceduralSkyMaterial
                {
                    SkyTopColor = new Color(0.16f, 0.20f, 0.30f),
                    SkyHorizonColor = new Color(0.42f, 0.38f, 0.36f),
                    GroundBottomColor = new Color(0.08f, 0.08f, 0.09f),
                    GroundHorizonColor = new Color(0.30f, 0.27f, 0.26f),
                    SunAngleMax = 2.5f,
                    SunCurve = 0.12f,
                },
            },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightSkyContribution = 1f,
            AmbientLightEnergy = 0.95f,
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
        var camera = new Camera3D { Far = _viewDistance, Fov = 70f, Name = "WalkCamera" };
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
            Far = _viewDistance,
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
            Invariant($"relief     {_relief.AmplitudeMetres:N0} m amplitude, roughness {_relief.RoughnessFraction / (double)ReliefField.RoughnessUnit:0.000}, ridging {Ridging()}, {_relief.WavelengthMetres:N0} m coarsest over {_relief.Octaves} octaves"),
            Invariant($"field      {_relief.Hash.ToString()[..8]} by {(_relief.RidgingFraction is null ? TerrainHeightfield.Rule : TerrainHeightfield.RidgedRule)}, {_across} x {_across} samples, {Lowest():0.0} m to {Highest():0.0} m here"),
            Invariant($"ground     {(_groundWasGenerated ? "generated and stored" : "loaded from the data root")}, {_storedBytes / 1024.0 / 1024.0:0.00} MiB of records against {_heights.Length * 2 / 1024.0 / 1024.0:0.00} MiB of samples"),
            Invariant($"biomes     {_palette.Count} in the planet's palette, {_patchCount} patches stored, {Present()} present here, drawn in {_surfacesDrawn} surface(s)"),
            // The inner string is built invariantly too: an interpolation nested inside an invariant one is
            // evaluated under the process locale, which is how "100,0%" reached a frame beside an
            // invariant "0.534 AU".
            Invariant($"sea        {SeaLine()}"),
            Invariant($"far field  generalized to the {_horizonMetres:0} m geometric horizon, {_farAcross} x {_farAcross} samples at {_farCellMetres:N0} m,"),
            Invariant($"           carrying {TerrainHeightfield.OctavesCarried(_relief, _farCellMetres)} of the field's {_relief.Octaves} octaves and dropping the rest as detail that stride cannot"),
            "           show, and wearing the planet's own stored surface rather than a biome's, which gives way to it at the edge",
            "",
            Invariant($"frame      {(_fromAbove ? $"orthographic, the whole region; the bar below is {ScaleBarMetres():N0} m" : $"walk, eye at {EyeHeight:0} m above the ground along the stored heading")}"),
            Invariant($"supplied   the sun, {SunElevationDegrees:0} deg up and drawn in the sky, the sky itself, the eye height and"),
            "           the shading normals are all this harness's own: no celestial solution exists yet,",
            "           so none of them is this system's star. The relief is drawn at true scale, with no",
            "           vertical exaggeration, and its shadows are cast across the whole region. The far field's",
            "           horizon distance is the flat-model geometric one, not yet decision 0032's celestial solution",
            "stored     extent, material, texture scale, the anchor above, and the relief the field derives; the",
            "           far field is derived the same way and stores nothing of its own",
            Invariant($"controls   {(_fromAbove ? "escape quits" : Invariant($"w a s d walk, drag turn, shift surveys at {SurveyMultiplier:0}x, held inside the region's extent, escape quits"))}"),
        ];

        _legend = new Label { Position = new Vector2(28, 24), Text = string.Join('\n', lines) };
        _legend.AddThemeFontSizeOverride("font_size", 15);
        canvas.AddChild(_legend);

        _scaleBar = new Control { Visible = _fromAbove };
        canvas.AddChild(_scaleBar);

        CallDeferred(nameof(SizeLegend));
    }

    /// <summary>How ridged this body says its ground is, or that its revision cannot say.</summary>
    private string Ridging() =>
        _relief.RidgingFraction is { } ridging
            ? FormattableString.Invariant($"{ridging / (double)ReliefField.RoughnessUnit:0.000}")
            : "none stored";

    /// <summary>What the legend says about the water, invariantly.</summary>
    private string SeaLine() =>
        _seaLevelMetres <= CategoryRegistryRevision9.SeaLevelFloorMetres
            ? "none: the planet declares no datum"
            : FormattableString.Invariant($"{_seaLevelMetres} m above the reference sphere, {Submerged():0.0}% of cells under it");

    /// <summary>How many of the palette's biomes actually appear here, which a palette of six painting one would not show.</summary>
    private int Present() => _index.Length == 0 ? 0 : _index.Distinct().Count();

    /// <summary>What share of the region's cells lie under the datum.</summary>
    private double Submerged()
    {
        if (_index.Length == 0)
        {
            return 0;
        }

        int under = 0;
        for (int biome = 0; biome < _palette.Count; biome++)
        {
            if (_palette[biome].TryParameter(_registry, CategoryRegistryRevision9.SubmergedParameter)?.Value.AsBool == true)
            {
                foreach (byte at in _index)
                {
                    if (at == biome)
                    {
                        under++;
                    }
                }
            }
        }

        return 100.0 * under / _index.Length;
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
