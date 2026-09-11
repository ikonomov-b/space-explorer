using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// The named versioned rule that turns a planet's <see cref="ReliefField"/> into the ground of one region:
/// `terrain-heightfield/1`
/// ([decision 0063](../../../docs/decisions/0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md)
/// clause 6). Integer arithmetic throughout, seeded only by the field's own content hash, so one field and
/// one anchor are one landscape on every operating system and in every renderer.
/// </summary>
/// <remarks>
/// The field is evaluated at each sample's <em>planet-fixed</em> position rather than in the region's own
/// tangent frame, which is what makes relief a property of the planet: two regions on one body agree where
/// they meet, and the far field of rung 5 extends the same ground past a region's edge without a second
/// rule.
///
/// This rule fixes its output and not its evaluation. An implementation may cache corner values between
/// neighbouring samples, evaluate octaves in any order, or run the rows in parallel, so long as the bytes
/// are identical.
/// </remarks>
public static class TerrainHeightfield
{
    /// <summary>The generator revision identifier version 1 is known by (decision 0035).</summary>
    public const string Rule = "terrain-heightfield/1";

    /// <summary>
    /// Version 2, which folds each octave into ridges by the body's own `relief-ridging`
    /// ([decision 0065](../../../docs/decisions/0065-terrain-heightfield-version-2-ridges-and-registry-revision-7s-ridging.md)).
    /// At a ridging of zero it computes version 1's number exactly, so the two are one construction with a
    /// dial rather than two rules.
    /// </summary>
    public const string RidgedRule = "terrain-heightfield/2";

    /// <summary>The ground sampling interval in metres, which is decision 0010's cell and not an input.</summary>
    public const long CellMetres = 2;

    /// <summary>The four multipliers of this rule's lattice, none of them shared with `derive-surface-raster/1`.</summary>
    private const ulong LatticeX = 0xD6E8FEB86659FD93UL;
    private const ulong LatticeY = 0xA24BAED4963EE407UL;
    private const ulong LatticeZ = 0x9FB21C651E98DF25UL;
    private const ulong LatticeOctave = 0x2545F4914F6CDD1DUL;

    /// <summary>How many samples a side a region of <paramref name="extentMetres"/> carries.</summary>
    /// <remarks>
    /// One more than the cell count, because a height belongs to the lattice corner four cells share and a
    /// field that closes both edges carries the row and column two adjacent regions will share at rung 5.
    /// </remarks>
    public static int SamplesAcross(long extentMetres) => (int)((extentMetres / CellMetres) + 1);

    /// <summary>
    /// The ground of the region <paramref name="field"/>'s planet carries at the given anchor, as
    /// <c>n * n</c> heights in 1/16 m with <c>z</c> outer and <c>x</c> inner, positive outward from the
    /// tangent plane at the body's reference radius.
    /// </summary>
    /// <param name="field">The planet instance's relief field, whose hash is the only seed.</param>
    /// <param name="latitude">The anchor's latitude as a binary turn (decision 0036).</param>
    /// <param name="longitude">The anchor's longitude as a binary turn.</param>
    /// <param name="heading">The anchor's heading as a binary turn, clockwise from north.</param>
    /// <param name="extentMetres">The region's stored extent in metres, per axis.</param>
    /// <exception cref="ArgumentOutOfRangeException">The extent is not a positive multiple of the 2 m cell.</exception>
    public static short[] Sample(ReliefField field, int latitude, int longitude, int heading, long extentMetres)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (extentMetres <= 0 || extentMetres % CellMetres != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extentMetres), extentMetres, $"A region's extent is a positive multiple of the {CellMetres} m cell.");
        }

        int across = SamplesAcross(extentMetres);
        ulong seed = Seed(field.Hash);
        (Axis x, Axis up, Axis z) = Frame(latitude, longitude, heading);

        int octaves = field.Octaves;
        long[] weights = Weights(field, octaves);
        long weightSum = weights.Sum();
        long amplitudeUnits = field.AmplitudeMetres * ReliefField.HeightUnit;
        long half = (across - 1) / 2;

        // Zero is version 1's construction exactly, and it is what content published before registry
        // revision 7 gets, since that content declares no ridging at all.
        long ridging = field.RidgingFraction ?? 0;

        // Rows are independent and each writes only its own slice, so running them in parallel changes
        // the cost and not one byte, which is the freedom this rule reserves for itself.
        var heights = new short[across * across];
        Parallel.For(0, across, j =>
        {
            long alongZ = (j - half) * CellMetres * 256;
            for (int i = 0; i < across; i++)
            {
                long alongX = (i - half) * CellMetres * 256;

                // The planet-fixed position of this sample, in 1/256 m: the reference sphere's point under
                // the anchor, walked out along the region's own axes. It is not projected back onto the
                // sphere, which is decision 0041's flat model exactly and its accepted error.
                Int128 positionX = Place(x.X, up.X, z.X, field.ReferenceRadiusUnits, alongX, alongZ);
                Int128 positionY = Place(x.Y, up.Y, z.Y, field.ReferenceRadiusUnits, alongX, alongZ);
                Int128 positionZ = Place(x.Z, up.Z, z.Z, field.ReferenceRadiusUnits, alongX, alongZ);

                Int128 total = 0;
                long wavelength = field.WavelengthMetres;
                for (int octave = 0; octave < octaves; octave++)
                {
                    long value = Lattice(seed, positionX, positionY, positionZ, wavelength, octave);
                    total += (Int128)Ridged(value, ridging) * weights[octave];
                    wavelength /= 2;
                }

                heights[(j * across) + i] = (short)Round(total * amplitudeUnits, (Int128)weightSum * 32_768);
            }
        });

        return heights;
    }

    /// <summary>One axis of the region's frame, each component a sine or cosine at <see cref="IntegerTrigonometry.One"/>.</summary>
    private readonly record struct Axis(long X, long Y, long Z);

    /// <summary>
    /// The region-local axes in the planet-fixed frame of clause 4: <b>+Z</b> is the north pole, <b>+X</b>
    /// pierces the prime meridian at the equator, and <b>+Y</b> completes a right-handed set.
    /// </summary>
    private static (Axis X, Axis Up, Axis Z) Frame(int latitude, int longitude, int heading)
    {
        long sinLatitude = IntegerTrigonometry.Sin(latitude);
        long cosLatitude = IntegerTrigonometry.Cos(latitude);
        long sinLongitude = IntegerTrigonometry.Sin(longitude);
        long cosLongitude = IntegerTrigonometry.Cos(longitude);
        long sinHeading = IntegerTrigonometry.Sin(heading);
        long cosHeading = IntegerTrigonometry.Cos(heading);

        var up = new Axis(Product(cosLatitude, cosLongitude), Product(cosLatitude, sinLongitude), sinLatitude);
        var north = new Axis(Product(-sinLatitude, cosLongitude), Product(-sinLatitude, sinLongitude), cosLatitude);
        var east = new Axis(-sinLongitude, cosLongitude, 0);

        var alongX = new Axis(
            Product(east.X, cosHeading) - Product(north.X, sinHeading),
            Product(east.Y, cosHeading) - Product(north.Y, sinHeading),
            Product(east.Z, cosHeading) - Product(north.Z, sinHeading));

        var alongZ = new Axis(
            -(Product(north.X, cosHeading) + Product(east.X, sinHeading)),
            -(Product(north.Y, cosHeading) + Product(east.Y, sinHeading)),
            -(Product(north.Z, cosHeading) + Product(east.Z, sinHeading)));

        return (alongX, up, alongZ);
    }

    /// <summary>Two values at scale 2^30, multiplied back to scale 2^30.</summary>
    private static long Product(long left, long right) => (left * right) >> 30;

    /// <summary>One planet-fixed coordinate of a sample, in 1/256 m.</summary>
    private static Int128 Place(long alongX, long up, long alongZ, long radiusUnits, long x, long z) =>
        (((Int128)radiusUnits * up) + ((Int128)x * alongX) + ((Int128)z * alongZ)) >> 30;

    /// <summary>The octave weights <c>r^o</c> at scale 2^16, so the amplitudes total the stored amplitude whatever the roughness.</summary>
    private static long[] Weights(ReliefField field, int octaves)
    {
        var weights = new long[octaves];
        long weight = 1L << 16;
        for (int octave = 0; octave < octaves; octave++)
        {
            weights[octave] = weight;
            weight = weight * field.RoughnessFraction / ReliefField.RoughnessUnit;
        }

        return weights;
    }

    /// <summary>The seed: the leading eight bytes of the field's hash, big-endian, with the low bit set.</summary>
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

    /// <summary>
    /// One octave's value at a planet-fixed point, in −32,768 to 32,767: the smoothstep-weighted trilinear
    /// reading of the octave's lattice, whose coordinate is a floor division so the lattice is continuous
    /// across zero rather than folding at it.
    /// </summary>
    private static long Lattice(ulong seed, Int128 x, Int128 y, Int128 z, long wavelength, int octave)
    {
        (long cellX, long fractionX) = Coordinate(x, wavelength);
        (long cellY, long fractionY) = Coordinate(y, wavelength);
        (long cellZ, long fractionZ) = Coordinate(z, wavelength);

        long weightX = Smoothstep(fractionX);
        long weightY = Smoothstep(fractionY);
        long weightZ = Smoothstep(fractionZ);

        long nearBottom = Mix(Corner(seed, cellX, cellY, cellZ, octave), Corner(seed, cellX + 1, cellY, cellZ, octave), weightX);
        long nearTop = Mix(Corner(seed, cellX, cellY + 1, cellZ, octave), Corner(seed, cellX + 1, cellY + 1, cellZ, octave), weightX);
        long farBottom = Mix(Corner(seed, cellX, cellY, cellZ + 1, octave), Corner(seed, cellX + 1, cellY, cellZ + 1, octave), weightX);
        long farTop = Mix(Corner(seed, cellX, cellY + 1, cellZ + 1, octave), Corner(seed, cellX + 1, cellY + 1, cellZ + 1, octave), weightX);

        return Mix(Mix(nearBottom, nearTop, weightY), Mix(farBottom, farTop, weightY), weightZ);
    }

    /// <summary>The lattice cell and the fraction within it, from a position in 1/256 m and a wavelength in metres.</summary>
    private static (long Cell, long Fraction) Coordinate(Int128 position, long wavelength)
    {
        // Floor division rather than truncation, which is what makes the lattice continuous across the
        // origin instead of mirroring about it; the shift then floors again to the cell.
        Int128 scaled = position << 8;
        Int128 quotient = scaled / wavelength;
        if (scaled - (quotient * wavelength) < 0)
        {
            quotient -= 1;
        }

        return ((long)(quotient >> 16), (long)(quotient & 0xFFFF));
    }

    /// <summary>
    /// One octave's value, folded into a ridge by <paramref name="ridging"/> parts in 65,535
    /// ([decision 0065](../../../docs/decisions/0065-terrain-heightfield-version-2-ridges-and-registry-revision-7s-ridging.md)
    /// clause 2). At zero this returns <paramref name="value"/> unchanged, which is what makes version 2 a
    /// dial on version 1 rather than a second rule.
    /// </summary>
    /// <remarks>
    /// The fold is what turns a rounded hill into a ridge: where the smooth field crosses zero — the place
    /// that was a gentle shoulder — becomes the high ground, and the smooth field's own peaks and troughs
    /// both become valley floors. Squaring the fold then narrows the ridges and broadens the valleys, which
    /// is the difference between crumpled ground and ground with crests on it.
    /// </remarks>
    private static long Ridged(long value, long ridging)
    {
        if (ridging <= 0)
        {
            return value;
        }

        long folded = 32_767 - Math.Abs(value);
        long sharpened = folded * folded / 32_767;
        long ridged = (2 * sharpened) - 32_767;
        return value + ((ridged - value) * ridging / ReliefField.RoughnessUnit);
    }

    /// <summary>The integer smoothstep of a fraction of 65,536: exactly 0, 32,768, and 65,536 at nothing, a half, and all.</summary>
    private static long Smoothstep(long fraction) => (fraction * fraction * ((3 * 65_536) - (2 * fraction))) >> 32;

    /// <summary>One linear reading between two lattice values, at a weight out of 65,536.</summary>
    private static long Mix(long from, long to, long weight) => from + (((to - from) * weight) >> 16);

    /// <summary>
    /// The lattice value at one integer corner, in −32,768 to 32,767. The finaliser is SplitMix64's, whose
    /// published first output for seed 0 anchors this construction outside this repository.
    /// </summary>
    private static long Corner(ulong seed, long x, long y, long z, int octave)
    {
        ulong state = unchecked(seed
            ^ ((ulong)x * LatticeX)
            ^ ((ulong)y * LatticeY)
            ^ ((ulong)z * LatticeZ)
            ^ ((ulong)(uint)octave * LatticeOctave));

        state ^= state >> 30;
        state = unchecked(state * 0xBF58476D1CE4E5B9UL);
        state ^= state >> 27;
        state = unchecked(state * 0x94D049BB133111EBUL);
        state ^= state >> 31;
        return (long)(state >> 48) - 32_768;
    }

    /// <summary>One division, rounding half away from zero, as the derivation rules already do.</summary>
    private static Int128 Round(Int128 numerator, Int128 denominator) =>
        numerator >= 0
            ? (numerator + (denominator / 2)) / denominator
            : -((-numerator + (denominator / 2)) / denominator);
}
