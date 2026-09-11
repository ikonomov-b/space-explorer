using Godot;
using SpaceExplorer.Core.Appearance;
using SpaceExplorer.Core.Registry;

namespace SpaceExplorer.Game;

/// <summary>
/// The Godot adapter of [decision 0031](../../docs/decisions/0031-primitive-complete-composition-and-storage.md):
/// it builds engine resources from data-only primitive definitions and never treats an engine resource as
/// the definition. A body's material comes from the `surface-material` its definition references and the
/// `texture-recipe` that material references, both read out of the stored set by their registry labels
/// ([decision 0055](../../docs/decisions/0055-definition-level-tags-and-tagged-references.md)).
/// </summary>
/// <remarks>
/// The pixels are not computed here. <see cref="SurfaceAppearance.Raster"/> in the engine-independent core
/// produces them by integer arithmetic over the appearance's own content hash, so the same stored material
/// is the same image on any machine and in any renderer, and the core's tests can prove it; this class
/// wraps those bytes in an image and a material and does nothing else with them.
/// </remarks>
public sealed class PrimitiveResources
{
    /// <summary>The side of the texture built from a recipe: enough for a pattern to read on a body.</summary>
    private const int TextureSide = 128;

    private readonly PrimitiveSet _set;
    private readonly CategoryRegistry _registry;
    private readonly Dictionary<SurfaceAppearance, StandardMaterial3D> _materials = [];
    private readonly Dictionary<(SurfaceAppearance Appearance, float Repeats), StandardMaterial3D> _grounds = [];

    public PrimitiveResources(PrimitiveSet set, CategoryRegistry registry)
    {
        _set = set;
        _registry = registry;
    }

    /// <summary>
    /// The material a node draws with, or null where its category names no surface: a star, an atmosphere
    /// and a barycentre carry no reference, and a planet carries one only from registry revision 3, so a
    /// view over older content stands in for it and says so.
    /// </summary>
    public StandardMaterial3D? For(PrimitiveDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.TryParameter(_registry, CategoryRegistryRevision3.SurfaceParameter) is not { } parameter
            || parameter.Descriptor.Kind != ParameterKind.PrimitiveRef)
        {
            return null;
        }

        PrimitiveRevisionRef reference = parameter.Value.AsRef;
        PrimitiveDefinition material = _set.TryFind(reference.Id) is { } found && found.Hash == reference.Hash
            ? found
            : throw new ArgumentException($"Definition {definition.Id} names surface {reference.Id} at {reference.Hash}, which the pinned set does not hold with that hash.", nameof(definition));

        return MaterialFor(SurfaceAppearance.From(material, _set, _registry));
    }

    /// <summary>
    /// The material a region's ground draws with: <paramref name="body"/>'s own stored surface, tiled at
    /// the length its recipe's scale claims over <paramref name="extentMetres"/> of ground rather than at
    /// the repeat <see cref="MaterialFor(SurfaceAppearance)"/> clamps a distant body to. A region is drawn
    /// at its true size, so the stored scale is directly usable here and clamping it would be the view
    /// inventing what the content stores (decisions 0057, 0061).
    /// </summary>
    public StandardMaterial3D GroundFor(PrimitiveDefinition body, long extentMetres)
    {
        ArgumentNullException.ThrowIfNull(body);

        SurfaceAppearance appearance = AppearanceOf(body);
        float repeats = (float)(extentMetres * (double)SurfaceAppearance.ScaleUnit / appearance.ScaleUnits);
        var key = (appearance, repeats);
        if (_grounds.TryGetValue(key, out StandardMaterial3D? cached))
        {
            return cached;
        }

        StandardMaterial3D ground = (StandardMaterial3D)MaterialFor(appearance).Duplicate();
        ground.Uv1Scale = new Vector3(repeats, repeats, 1f);
        _grounds[key] = ground;
        return ground;
    }

    /// <summary>How many times a body's texture repeats across <paramref name="extentMetres"/>, as the ground draws it.</summary>
    public float GroundRepeats(PrimitiveDefinition body, long extentMetres) =>
        (float)(extentMetres * (double)SurfaceAppearance.ScaleUnit / AppearanceOf(body).ScaleUnits);

    /// <summary>The appearance a body's stored surface material describes.</summary>
    private SurfaceAppearance AppearanceOf(PrimitiveDefinition body)
    {
        if (body.TryParameter(_registry, CategoryRegistryRevision3.SurfaceParameter) is not { } parameter
            || parameter.Descriptor.Kind != ParameterKind.PrimitiveRef)
        {
            throw new ArgumentException($"Definition {body.Id} stores no surface material, so nothing says what its ground is made of.", nameof(body));
        }

        PrimitiveRevisionRef reference = parameter.Value.AsRef;
        PrimitiveDefinition material = _set.TryFind(reference.Id) is { } found && found.Hash == reference.Hash
            ? found
            : throw new ArgumentException($"Definition {body.Id} names surface {reference.Id} at {reference.Hash}, which the pinned set does not hold with that hash.", nameof(body));

        return SurfaceAppearance.From(material, _set, _registry);
    }

    /// <summary>The engine material an appearance describes, built once per appearance.</summary>
    public StandardMaterial3D MaterialFor(SurfaceAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        if (_materials.TryGetValue(appearance, out StandardMaterial3D? cached))
        {
            return cached;
        }

        Color colour = Colour(appearance.BaseColour);
        var built = new StandardMaterial3D
        {
            AlbedoColor = colour,
            Roughness = Fraction(appearance.RoughnessFraction),
            Metallic = Fraction(appearance.MetallicFraction),
            AlbedoTexture = TextureFor(appearance),

            // The recipe's scale is a length, and a body is drawn far from its own size here, so it sets
            // how often the pattern repeats rather than how large it is: a fine grain repeats more.
            Uv1Scale = Vector3.One * Mathf.Clamp(4f * SurfaceAppearance.ScaleUnit / appearance.ScaleUnits, 1f, 8f),
        };

        switch (appearance.Shader)
        {
            case "metallic":
                built.MetallicSpecular = 0.9f;
                break;
            case "emissive":
                built.EmissionEnabled = true;
                built.Emission = colour;
                built.EmissionEnergyMultiplier = 0.6f;
                break;
        }

        _materials[appearance] = built;
        return built;
    }

    /// <summary>
    /// The core's raster, wrapped as a texture; the bytes are straight RGBA in row order. Mipmaps are
    /// generated before wrapping: a region tiles this 128-pixel image thousands of times across its
    /// ground, and sampling that without a mip chain aliases into a false large-scale gradient that is
    /// not in the source image at all, rather than resolving to the flat average a minified tile should
    /// show (decision 0061's own reading of rung one).
    /// </summary>
    private static ImageTexture TextureFor(SurfaceAppearance appearance)
    {
        Image image = Image.CreateFromData(TextureSide, TextureSide, false, Image.Format.Rgba8, appearance.Raster(TextureSide));
        image.GenerateMipmaps();
        return ImageTexture.CreateFromImage(image);
    }

    private static Color Colour(Rgba colour) => Color.Color8(colour.Red, colour.Green, colour.Blue, colour.Alpha);

    private static float Fraction(long value) => Mathf.Clamp((float)value / SurfaceAppearance.FractionUnit, 0f, 1f);
}
