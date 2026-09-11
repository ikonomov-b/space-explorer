namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// What one biome of a planet's palette claims, as the rule reads it: the band of ground it covers and
/// whether it is what lies under the sea
/// ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
/// clause 2).
/// </summary>
/// <param name="Submerged">True where this biome is the sea floor rather than dry land.</param>
/// <param name="ElevationLow">The low end of its band, a signed fraction of the body's relief amplitude.</param>
/// <param name="ElevationHigh">The high end.</param>
public readonly record struct BiomeClaim(bool Submerged, long ElevationLow, long ElevationHigh);

/// <summary>
/// `derive-biome-index/1`: which biome each cell of a region wears
/// ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
/// clause 5). It is the per-cell index [decision 0017](../../../docs/decisions/0017-region-extent-cap-and-storage-derivation.md)
/// named, derived rather than stored: the patches are permanent and this is what a renderer wants, which is
/// the two tiers requirement R18 asks for.
/// </summary>
/// <remarks>
/// Integer arithmetic throughout in the shape [decision 0056](../../../docs/decisions/0056-surface-raster-rule-and-the-boundary-of-stream-derivation.md)
/// fixes for `derive-surface-raster`: no floating-point value anywhere, no random stream, no draw, and a
/// frozen implementation.
///
/// It is total, and that is the point rather than an accident. A cell below the datum takes the first
/// submerged biome and falls through where the palette holds none; a cell above it takes the nearest patch
/// whose biome admits its elevation, and the nearest patch of any kind where none does. So no cell and no
/// region can fail, which is what keeps decision 0063 clause 15's retry deferral alive for another rung.
/// </remarks>
public static class BiomeIndex
{
    /// <summary>The generator revision identifier this rule is known by (decision 0035).</summary>
    public const string Rule = "derive-biome-index/1";

    /// <summary>
    /// One byte a cell, row-major with z outer and x inner, each an ordinal into the planet's palette.
    /// </summary>
    /// <param name="heights">The region's heights in 1/16 m, as `terrain-heightfield` gives them, one per lattice corner.</param>
    /// <param name="extentMetres">The region's extent per axis.</param>
    /// <param name="patches">The region's stored patches.</param>
    /// <param name="claims">What each biome of the planet's palette claims, in palette order.</param>
    /// <param name="seaLevelMetres">The planet's sea datum, in metres above the reference sphere.</param>
    /// <param name="amplitudeMetres">The body's relief amplitude, which the elevation bands are fractions of.</param>
    /// <exception cref="ArgumentException">The palette is empty, or a patch names a biome outside it.</exception>
    public static byte[] Derive(
        short[] heights,
        long extentMetres,
        IReadOnlyList<BiomePatch> patches,
        IReadOnlyList<BiomeClaim> claims,
        long seaLevelMetres,
        long amplitudeMetres)
    {
        ArgumentNullException.ThrowIfNull(heights);
        ArgumentNullException.ThrowIfNull(patches);
        ArgumentNullException.ThrowIfNull(claims);

        if (claims.Count is 0 or > 255)
        {
            throw new ArgumentException($"A palette holds 1 to 255 biomes, so a cell's index fits one byte; this one holds {claims.Count}.", nameof(claims));
        }

        foreach (BiomePatch patch in patches)
        {
            if (patch.Ordinal < 0 || patch.Ordinal >= claims.Count)
            {
                throw new ArgumentException($"A patch names biome {patch.Ordinal} of a palette of {claims.Count}.", nameof(patches));
            }
        }

        int across = TerrainHeightfield.SamplesAcross(extentMetres);
        int cells = across - 1;
        long half = (long)cells * TerrainHeightfield.CellMetres * 256 / 2;

        // The first submerged biome of the palette, or none: a cell under the datum takes it, and falls
        // through to the land rule where the palette declares no sea floor.
        int submerged = -1;
        for (int index = 0; index < claims.Count; index++)
        {
            if (claims[index].Submerged)
            {
                submerged = index;
                break;
            }
        }

        long seaUnits = seaLevelMetres * ReliefField.HeightUnit;
        var indices = new byte[cells * cells];

        for (int row = 0; row < cells; row++)
        {
            // The cell's centre in the region's own frame, in 1/256 m: a height belongs to a lattice corner
            // and an index to the cell four corners bound, so the centre sits half a cell in from each.
            long centreZ = (((long)row * TerrainHeightfield.CellMetres * 256) + (TerrainHeightfield.CellMetres * 128)) - half;

            for (int column = 0; column < cells; column++)
            {
                long centreX = (((long)column * TerrainHeightfield.CellMetres * 256) + (TerrainHeightfield.CellMetres * 128)) - half;

                // The cell's height is the mean of the four corners that bound it, rounded half away from
                // zero so the rule has one answer rather than a platform's.
                int corner = (row * across) + column;
                long sum = heights[corner] + heights[corner + 1] + heights[corner + across] + heights[corner + across + 1];
                long height = sum >= 0 ? (sum + 2) / 4 : -((-sum + 2) / 4);

                indices[(row * cells) + column] = (byte)Claim(height, centreX, centreZ, patches, claims, seaUnits, submerged, amplitudeMetres);
            }
        }

        return indices;
    }

    /// <summary>Which biome one cell wears, by the three steps clause 5 states in the order it states them.</summary>
    private static int Claim(
        long height,
        long centreX,
        long centreZ,
        IReadOnlyList<BiomePatch> patches,
        IReadOnlyList<BiomeClaim> claims,
        long seaUnits,
        int submerged,
        long amplitudeMetres)
    {
        if (height < seaUnits && submerged >= 0)
        {
            return submerged;
        }

        // Three candidates, preferred in this order: the nearest patch whose biome admits this elevation,
        // the nearest patch of any land biome, and only then the nearest patch at all. The middle one is
        // what keeps a sea floor off dry ground when no land biome's band reaches it — without it the
        // fallback reintroduces the fault the submerged test above removes.
        int nearestAdmitting = -1;
        int nearestLand = -1;
        int nearestOfAny = -1;
        long bestAdmitting = long.MaxValue;
        long bestLand = long.MaxValue;
        long bestOfAny = long.MaxValue;

        for (int index = 0; index < patches.Count; index++)
        {
            BiomePatch patch = patches[index];

            // Distance in units of the patch's own reach, so a wide patch claims further than a narrow one
            // without either being compared in metres against the other: d² × 65,536 / r².
            long deltaX = centreX - patch.CentreX;
            long deltaZ = centreZ - patch.CentreZ;
            Int128 squared = ((Int128)deltaX * deltaX) + ((Int128)deltaZ * deltaZ);
            long reachUnits = patch.ReachMetres * 256;
            var scaled = (long)Int128.Min(squared * 65_536 / ((Int128)reachUnits * reachUnits), long.MaxValue);

            if (scaled < bestOfAny)
            {
                bestOfAny = scaled;
                nearestOfAny = index;
            }

            if (!claims[patch.Ordinal].Submerged && scaled < bestLand)
            {
                bestLand = scaled;
                nearestLand = index;
            }

            if (Admits(claims[patch.Ordinal], height, amplitudeMetres) && scaled < bestAdmitting)
            {
                bestAdmitting = scaled;
                nearestAdmitting = index;
            }
        }

        int chosen = nearestAdmitting >= 0 ? nearestAdmitting
            : nearestLand >= 0 ? nearestLand
            : nearestOfAny;
        return chosen >= 0 ? patches[chosen].Ordinal : 0;
    }

    /// <summary>
    /// Whether a biome's elevation band admits a height, in integers and without a division: the band is a
    /// signed fraction of the body's own amplitude, so one authored biome suits a body of 90 m of relief
    /// and one of 1,024 m without being re-authored for either.
    /// </summary>
    private static bool Admits(BiomeClaim claim, long height, long amplitudeMetres)
    {
        // A sea floor does not claim dry land. Clause 5 states the submerged branch and the elevation
        // branch separately and does not say this, but without it a submerged biome whose band is the
        // default widest one wins land cells on distance alone, and the sea becomes invisible: measured on
        // the sample's own content, one such biome took 62% of the cells at every datum from none to sixty
        // metres, which is the fault this rule exists to remove rather than to introduce.
        if (claim.Submerged)
        {
            return false;
        }

        long scaled = height * ReliefField.RoughnessUnit;
        long amplitudeUnits = amplitudeMetres * ReliefField.HeightUnit;
        return claim.ElevationLow * amplitudeUnits <= scaled && scaled <= claim.ElevationHigh * amplitudeUnits;
    }
}
