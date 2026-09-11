using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Appearance;

/// <summary>A colour as the records carry one: four bytes, no floating point.</summary>
public readonly record struct Rgba(byte Red, byte Green, byte Blue, byte Alpha)
{
    /// <summary>Mixes towards <paramref name="other"/> by <paramref name="weight"/> parts in 256, in integers.</summary>
    public Rgba Towards(Rgba other, int weight)
    {
        int mix = Math.Clamp(weight, 0, 256);
        return new Rgba(Blend(Red, other.Red, mix), Blend(Green, other.Green, mix), Blend(Blue, other.Blue, mix), Blend(Alpha, other.Alpha, mix));
    }

    private static byte Blend(byte from, byte to, int weight) => (byte)(((from * (256 - weight)) + (to * weight)) / 256);
}

/// <summary>
/// How a surface looks, in exactly the fields the `surface-material` and `texture-recipe` categories
/// carry: a shader, a base colour, a roughness and a metallic fraction of 65,535, and the pattern, scale,
/// two colours and contrast of the texture. <see cref="From"/> reads it out of a stored material
/// primitive and the texture recipe that material references, which is the only source it has: what a
/// body looks like is generated content, not something a view derives for itself
/// ([decision 0055](../../../docs/decisions/0055-definition-level-tags-and-tagged-references.md)). The
/// engine adapter takes this rather than a definition so that the pixels are computed in the core, where
/// they can be tested, and the engine only wraps them
/// ([decision 0031](../../../docs/decisions/0031-primitive-complete-composition-and-storage.md)).
/// </summary>
public sealed record SurfaceAppearance(
    string Shader,
    Rgba BaseColour,
    long RoughnessFraction,
    long MetallicFraction,
    string Pattern,
    long ScaleUnits,
    Rgba ColourA,
    Rgba ColourB,
    long ContrastFraction)
{
    /// <summary>The fraction every fraction field is out of, which is the range the registry gives them.</summary>
    public const long FractionUnit = 65_535;

    /// <summary>The unit of <see cref="ScaleUnits"/>: 1/65,536 m, the artifact-local length unit of decision 0036.</summary>
    public const long ScaleUnit = 1L << 16;

    /// <summary>The appearance a stored `surface-material` describes, with the `texture-recipe` it references.</summary>
    /// <exception cref="ArgumentException">The definition is of another category, or its texture reference does not resolve in the set.</exception>
    public static SurfaceAppearance From(PrimitiveDefinition material, PrimitiveSet set, CategoryRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(registry);

        string shader = Choice(material, registry, "shader");
        Rgba baseColour = Colour(material, registry, "base-colour");
        long roughness = Integer(material, registry, "roughness");
        long metallic = Integer(material, registry, "metallic");

        PrimitiveDefinition recipe = Resolve(material, set, registry, "texture");
        return new SurfaceAppearance(
            shader,
            baseColour,
            roughness,
            metallic,
            Choice(recipe, registry, "pattern"),
            Integer(recipe, registry, "scale"),
            Colour(recipe, registry, "colour-a"),
            Colour(recipe, registry, "colour-b"),
            Integer(recipe, registry, "contrast"));
    }

    /// <summary>
    /// The seed everything derived from this appearance uses: the content hash of its own canonical bytes,
    /// so two equal appearances raster identically on any machine and two different ones do not.
    /// </summary>
    public ContentHash Hash
    {
        get
        {
            var writer = new CanonicalWriter("surface-appearance/1");
            writer.WriteText(Shader);
            WriteColour(writer, BaseColour);
            writer.WriteVarInt(RoughnessFraction);
            writer.WriteVarInt(MetallicFraction);
            writer.WriteText(Pattern);
            writer.WriteVarInt(ScaleUnits);
            WriteColour(writer, ColourA);
            WriteColour(writer, ColourB);
            writer.WriteVarInt(ContrastFraction);
            return writer.ToContentHash();
        }
    }

    /// <summary>
    /// The albedo texture this appearance describes, as <paramref name="side"/> by <paramref name="side"/>
    /// straight RGBA bytes in row order. Integer arithmetic over the appearance's own hash throughout, so
    /// the same appearance is the same image on every operating system and in every renderer; the engine
    /// wraps these bytes and never generates them.
    /// </summary>
    /// <param name="version">
    /// Which version of the rule draws it: 1 is the frozen original, 2 the fine-grained seamless one that
    /// exists because a tile laid at its true metre length repeats every few centimetres under a walker's
    /// eye, where version 1's seams and mid-scale motifs read as a grid rather than as ground
    /// ([decision 0064](../../../docs/decisions/0064-surface-raster-version-2-tiles-seamlessly-and-finely.md)).
    /// Version 1 is retained because the content published under it must still draw as it drew.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The side is not between 8 and 512, or the version is not 1 or 2.</exception>
    public byte[] Raster(int side, int version)
    {
        if (side is < 8 or > 512)
        {
            throw new ArgumentOutOfRangeException(nameof(side), side, "A raster side is between 8 and 512 pixels.");
        }

        if (version is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "This build carries derive-surface-raster versions 1 and 2.");
        }

        ulong seed = Seed(Hash);
        var pixels = new byte[side * side * 4];

        // Version 2 draws every feature small enough that no motif survives the repeat: the coarsest
        // octave is an eighth of version 1's, the cells are a quarter of the size, and the stripes are
        // half the period. What a reader sees close up is grain, and what a reader sees far off is the
        // average of it, which is what version 1 already got right.
        int cell = version == 1 ? Math.Max(4, side / 6) : Math.Max(3, side / 24);
        int stripe = version == 1 ? Math.Max(2, side / 16) : Math.Max(1, side / 32);

        for (int y = 0; y < side; y++)
        {
            for (int x = 0; x < side; x++)
            {
                int weight = Pattern switch
                {
                    "noise" => version == 1 ? Fbm(seed, x, y, side) : FineFbm(seed, x, y, side),
                    "stripes" => (y / stripe) % 2 == 0 ? 0 : 256,
                    "cells" => version == 1 ? Cells(seed, x, y, cell) : SeamlessCells(seed, x, y, cell, side),
                    "speckle" => Value(seed, x, y, 1) > 220 ? 256 : 0,
                    _ => 0,
                };

                // Contrast pulls the weight towards the ends: a flat recipe stays on colour A, and a
                // high-contrast one lands almost wholly on one colour or the other.
                int contrast = (int)(64 + (192 * ContrastFraction / FractionUnit));
                weight = Math.Clamp(128 + ((weight - 128) * contrast / 128), 0, 256);

                Rgba colour = ColourA.Towards(ColourB, weight);
                int offset = ((y * side) + x) * 4;
                pixels[offset] = colour.Red;
                pixels[offset + 1] = colour.Green;
                pixels[offset + 2] = colour.Blue;
                pixels[offset + 3] = colour.Alpha;
            }
        }

        return pixels;
    }

    private static void WriteColour(CanonicalWriter writer, Rgba colour)
    {
        writer.WriteUInt8(colour.Red);
        writer.WriteUInt8(colour.Green);
        writer.WriteUInt8(colour.Blue);
        writer.WriteUInt8(colour.Alpha);
    }

    private static ulong Seed(ContentHash hash)
    {
        ReadOnlySpan<byte> bytes = hash.Bytes;
        ulong seed = 0;
        for (int index = 0; index < 8; index++)
        {
            seed = (seed << 8) | bytes[index];
        }

        return seed | 1;
    }

    /// <summary>A value in 0 to 255 from the seed and a grid position, by integer mixing.</summary>
    private static int Value(ulong seed, int x, int y, int octave)
    {
        ulong state = seed ^ ((ulong)(uint)x * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)y * 0xC2B2AE3D27D4EB4FUL) ^ ((ulong)(uint)octave * 0x165667B19E3779F9UL);
        state ^= state >> 33;
        state = unchecked(state * 0xFF51AFD7ED558CCDUL);
        state ^= state >> 29;
        state = unchecked(state * 0xC4CEB9FE1A85EC53UL);
        state ^= state >> 32;
        return (int)(state & 0xFF);
    }

    /// <summary>Three octaves of value noise, bilinear between grid points, in 0 to 256.</summary>
    private static int Fbm(ulong seed, int x, int y, int side)
    {
        int total = 0;
        int amplitude = 128;
        int step = Math.Max(4, side / 8);
        for (int octave = 0; octave < 3 && step >= 1; octave++)
        {
            total += amplitude * Lattice(seed, x, y, step, octave) / 256;
            amplitude /= 2;
            step /= 2;
        }

        return Math.Clamp(total * 256 / 224, 0, 256);
    }

    /// <summary>
    /// Version 2's noise: the same construction as <see cref="Fbm"/> over finer octaves and on a lattice
    /// that wraps at the tile's edge, so a tile abuts its own copy without a seam and carries no feature
    /// large enough to be recognised when it repeats.
    /// </summary>
    private static int FineFbm(ulong seed, int x, int y, int side)
    {
        int total = 0;
        int amplitude = 128;
        int step = Math.Max(2, side / 16);
        for (int octave = 0; octave < 3 && step >= 1; octave++)
        {
            total += amplitude * SeamlessLattice(seed, x, y, step, octave, side) / 256;
            amplitude /= 2;
            step /= 2;
        }

        return Math.Clamp(total * 256 / 224, 0, 256);
    }

    /// <summary>
    /// The bilinear reading of an octave whose lattice wraps every <c>side / step</c> cells, which is what
    /// makes the tile seamless: the corner past the last cell is the corner at the first.
    /// </summary>
    private static int SeamlessLattice(ulong seed, int x, int y, int step, int octave, int side)
    {
        int cells = Math.Max(1, side / step);
        int cellX = x / step;
        int cellY = y / step;
        int fractionX = ((x % step) * 256) / step;
        int fractionY = ((y % step) * 256) / step;

        int topLeft = Value(seed, cellX % cells, cellY % cells, octave + 1);
        int topRight = Value(seed, (cellX + 1) % cells, cellY % cells, octave + 1);
        int bottomLeft = Value(seed, cellX % cells, (cellY + 1) % cells, octave + 1);
        int bottomRight = Value(seed, (cellX + 1) % cells, (cellY + 1) % cells, octave + 1);

        int top = topLeft + ((topRight - topLeft) * fractionX / 256);
        int bottom = bottomLeft + ((bottomRight - bottomLeft) * fractionX / 256);
        return top + ((bottom - top) * fractionY / 256);
    }

    /// <summary>Version 2's cells: <see cref="Cells"/> on a grid that wraps with the tile, so no cell is cut at an edge.</summary>
    private static int SeamlessCells(ulong seed, int x, int y, int cell, int side)
    {
        int cells = Math.Max(1, side / cell);
        long nearest = long.MaxValue;
        int gridX = x / cell;
        int gridY = y / cell;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int atX = gridX + offsetX;
                int atY = gridY + offsetY;
                int wrappedX = ((atX % cells) + cells) % cells;
                int wrappedY = ((atY % cells) + cells) % cells;
                long centreX = (((long)atX * cell) + (Value(seed, wrappedX, wrappedY, 2) * cell / 256)) - x;
                long centreY = (((long)atY * cell) + (Value(seed, wrappedX, wrappedY, 3) * cell / 256)) - y;
                nearest = Math.Min(nearest, (centreX * centreX) + (centreY * centreY));
            }
        }

        return (int)Math.Clamp(IntegerSqrt(nearest) * 256 / cell, 0, 256);
    }

    /// <summary>The bilinear reading of the octave's lattice at a pixel, in 0 to 255.</summary>
    private static int Lattice(ulong seed, int x, int y, int step, int octave)
    {
        int cellX = x / step;
        int cellY = y / step;
        int fractionX = ((x % step) * 256) / step;
        int fractionY = ((y % step) * 256) / step;

        int topLeft = Value(seed, cellX, cellY, octave + 1);
        int topRight = Value(seed, cellX + 1, cellY, octave + 1);
        int bottomLeft = Value(seed, cellX, cellY + 1, octave + 1);
        int bottomRight = Value(seed, cellX + 1, cellY + 1, octave + 1);

        int top = topLeft + ((topRight - topLeft) * fractionX / 256);
        int bottom = bottomLeft + ((bottomRight - bottomLeft) * fractionX / 256);
        return top + ((bottom - top) * fractionY / 256);
    }

    /// <summary>The distance to the nearest scattered cell centre, in 0 to 256: cracked, scaled, or pebbled.</summary>
    private static int Cells(ulong seed, int x, int y, int cell)
    {
        long nearest = long.MaxValue;
        int gridX = x / cell;
        int gridY = y / cell;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int atX = gridX + offsetX;
                int atY = gridY + offsetY;
                long centreX = (((long)atX * cell) + (Value(seed, atX, atY, 2) * cell / 256)) - x;
                long centreY = (((long)atY * cell) + (Value(seed, atX, atY, 3) * cell / 256)) - y;
                nearest = Math.Min(nearest, (centreX * centreX) + (centreY * centreY));
            }
        }

        return (int)Math.Clamp(IntegerSqrt(nearest) * 256 / cell, 0, 256);
    }

    /// <summary>The integer square root, so the pattern needs no floating point.</summary>
    private static long IntegerSqrt(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        long root = 0;
        for (long bit = 1L << 30; bit > 0; bit >>= 1)
        {
            long candidate = root + bit;
            if (candidate * candidate <= value)
            {
                root = candidate;
            }
        }

        return root;
    }

    private static PrimitiveDefinition Resolve(PrimitiveDefinition definition, PrimitiveSet set, CategoryRegistry registry, string label)
    {
        (ParameterDescriptor descriptor, ParameterValue value) = Parameter(definition, registry, label);
        if (descriptor.Kind != ParameterKind.PrimitiveRef)
        {
            throw new ArgumentException($"Parameter '{label}' of category {definition.Category} is not a reference.", nameof(label));
        }

        PrimitiveRevisionRef reference = value.AsRef;
        PrimitiveDefinition? target = set.TryFind(reference.Id);
        return target is not null && target.Hash == reference.Hash
            ? target
            : throw new ArgumentException($"Definition {definition.Id} references {reference.Id} at {reference.Hash}, which the pinned set does not hold with that hash.", nameof(definition));
    }

    private static string Choice(PrimitiveDefinition definition, CategoryRegistry registry, string label)
    {
        (ParameterDescriptor descriptor, ParameterValue value) = Parameter(definition, registry, label);
        return descriptor.EnumLabels[(int)value.AsEnumIndex];
    }

    private static long Integer(PrimitiveDefinition definition, CategoryRegistry registry, string label) =>
        Parameter(definition, registry, label).Value.AsInteger;

    private static Rgba Colour(PrimitiveDefinition definition, CategoryRegistry registry, string label)
    {
        (byte red, byte green, byte blue, byte alpha) = Parameter(definition, registry, label).Value.AsColour;
        return new Rgba(red, green, blue, alpha);
    }

    private static (ParameterDescriptor Descriptor, ParameterValue Value) Parameter(PrimitiveDefinition definition, CategoryRegistry registry, string label) =>
        definition.TryParameter(registry, label)
        ?? throw new ArgumentException($"Category {definition.Category} of registry revision {registry.Revision} has no parameter '{label}'.", nameof(label));
}
