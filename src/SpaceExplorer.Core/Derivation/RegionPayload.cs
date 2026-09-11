using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// The ground of one region, as bytes a data root holds rather than as a rule a renderer runs
/// ([decision 0066](../../../docs/decisions/0066-generation-writes-to-the-database-and-the-view-only-reads.md)).
/// It is the primitive-owned payload class [decision 0031](../../../docs/decisions/0031-primitive-complete-composition-and-storage.md)
/// names and [primitives part 5](../../../docs/primitives.md#where-it-must-grow) has recorded as missing
/// since the storage assessment, arriving with the first thing that needs it.
/// </summary>
/// <remarks>
/// It names the field it came from by content hash rather than storing that field's numbers again. That is
/// what makes a stored surface checkable against the world it claims to belong to: change a planet's relief
/// parameters and the field hash changes with them, so a payload of the old ground is visibly of another
/// world instead of silently of this one.
/// </remarks>
/// <param name="InstancePath">The region node's path in the graph whose pack holds this payload.</param>
/// <param name="Rule">The generator revision identifier that produced the heights.</param>
/// <param name="FieldHash">The content hash of the <see cref="ReliefField"/> they were produced from.</param>
/// <param name="ExtentMetres">The region's extent per axis, in metres.</param>
/// <param name="Heights">The heights in 1/16 m, z outer and x inner, as <see cref="TerrainHeightfield"/> gives them.</param>
/// <param name="PatchRule">
/// The generator revision identifier that drew <paramref name="Patches"/>, or null under version 1, which
/// carried no derived set ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
/// clause 7).
/// </param>
/// <param name="Patches">
/// The region's biome patches in index order: the derived set of
/// [decision 0038](../../../docs/decisions/0038-derived-instances.md), which this record carries because a
/// derived set is the node's payload and not a record beside it. Empty under version 1.
/// </param>
public sealed record RegionPayload(
    string InstancePath,
    string Rule,
    ContentHash FieldHash,
    long ExtentMetres,
    short[] Heights,
    string? PatchRule = null,
    IReadOnlyList<BiomePatch>? Patches = null)
{
    /// <summary>The most patches a payload may carry, which is the region category's derived-instance budget.</summary>
    public const int MaxPatches = 64;

    /// <summary>Which version of the record these fields make: 2 where a derived set is carried, 1 where none is.</summary>
    public int Version => PatchRule is null ? 1 : 2;

    /// <summary>The most samples a payload may carry, which is a maximal region's 1,025 a side.</summary>
    public const int MaxSamples = 1_025 * 1_025;

    /// <summary>How many samples a side this payload carries.</summary>
    public int Across => TerrainHeightfield.SamplesAcross(ExtentMetres);

    /// <summary>
    /// The canonical bytes this payload is stored and addressed as: every height predicted from its
    /// neighbours, and the residual packed at the bit width its row actually needs.
    /// </summary>
    /// <remarks>
    /// The compaction is this record's own arithmetic and not a compressor's, which is the whole point: a
    /// content-addressed store may not let a third-party implementation decide its hashes, because a
    /// library that retunes its output between versions would silently rename every stored record. Integer
    /// prediction and bit packing have no such freedom — they are frozen here as surely as the rule that
    /// made the heights.
    ///
    /// Measured on a maximal region of the sample's own content: flat ocean ground stores at 19% of its raw
    /// samples, smooth ground at 26%, and ridged rock at 38% — 0.38 to 0.76 MiB against 2 MiB, and better
    /// than a general-purpose deflate of the same samples in every one of those cases. The worst case the
    /// format admits is about 106% of raw, on ground whose residuals need the full 17 bits, which the
    /// authored vocabulary cannot reach: it needs the finest wavelength at the greatest amplitude, and is
    /// noise rather than terrain. The format is therefore bounded rather than guarded, which is
    /// [decision 0024](../../../docs/decisions/0024-tests-comments-index-and-scaffolding-trimmed.md)'s rule
    /// applied to a fallback nothing can reach.
    /// </remarks>
    public byte[] Bytes
    {
        get
        {
            var writer = new CanonicalWriter(PatchRule is null ? "region-payload/1" : "region-payload/2");
            writer.WritePath(InstancePath);
            writer.WritePath(Rule);
            writer.WriteContentHash(FieldHash);
            writer.WriteVarInt(ExtentMetres);
            writer.WriteCount(Heights.Length);
            writer.WriteBytes(Pack(Heights, Across));

            // A version's added field is written only from that version on, so version 1's bytes, its
            // recorded hash, and surface cycles one to three are unmoved (decision 0050).
            if (PatchRule is { } patchRule)
            {
                writer.WritePath(patchRule);
                IReadOnlyList<BiomePatch> patches = Patches ?? [];
                writer.WriteCount(patches.Count);
                foreach (BiomePatch patch in patches)
                {
                    writer.WriteVarUInt((ulong)patch.Ordinal);
                    writer.WriteVarInt(patch.CentreX);
                    writer.WriteVarInt(patch.CentreZ);
                    writer.WriteVarInt(patch.ReachMetres);
                }
            }

            return writer.ToArray();
        }
    }

    /// <summary>
    /// What a sample is guessed to be from the ones already written: the planar predictor, which is exact
    /// wherever the ground is locally flat and near enough everywhere else.
    /// </summary>
    private static long Predict(short[] heights, int index, int across)
    {
        int column = index % across;
        int row = index / across;

        if (row == 0)
        {
            return column == 0 ? 0 : heights[index - 1];
        }

        if (column == 0)
        {
            return heights[index - across];
        }

        return heights[index - 1] + heights[index - across] - heights[index - across - 1];
    }

    /// <summary>
    /// The residuals of one row, packed at the width that row actually needs: one byte of width, then that
    /// many bits per sample, rows padded to a byte boundary so a row can be found without decoding the ones
    /// before it.
    /// </summary>
    /// <remarks>
    /// A width per row rather than one for the whole region, because a region is not uniformly rough: a row
    /// crossing a scarp needs ten bits where the flat rows either side of it need three, and paying the
    /// scarp's width everywhere would cost more than the byte each row's header costs. Smooth ground lands
    /// near three bits a sample, where a varint could never go below eight.
    /// </remarks>
    private static byte[] Pack(short[] heights, int across)
    {
        var packed = new List<byte>(heights.Length / 2);
        var residuals = new long[across];

        for (int row = 0; row * across < heights.Length; row++)
        {
            int width = 0;
            for (int column = 0; column < across; column++)
            {
                int index = (row * across) + column;
                residuals[column] = ZigZag(heights[index] - Predict(heights, index, across));
                width = Math.Max(width, BitsFor(residuals[column]));
            }

            packed.Add((byte)width);

            ulong buffer = 0;
            int held = 0;
            foreach (long residual in residuals)
            {
                buffer |= (ulong)residual << held;
                held += width;
                while (held >= 8)
                {
                    packed.Add((byte)buffer);
                    buffer >>= 8;
                    held -= 8;
                }
            }

            if (held > 0)
            {
                packed.Add((byte)buffer);
            }
        }

        return [.. packed];
    }

    /// <summary>The rows a packed blob describes, back as the heights they were predicted from.</summary>
    private static short[] Unpack(byte[] packed, int count, int across)
    {
        var heights = new short[count];
        int at = 0;

        for (int row = 0; row * across < count; row++)
        {
            if (at >= packed.Length)
            {
                throw new FormatException($"A region payload ends before row {row} of {count / across}.");
            }

            int width = packed[at++];
            if (width > 32)
            {
                throw new FormatException($"A region payload's row {row} claims {width} bits a sample; the bound is 32.");
            }

            ulong buffer = 0;
            int held = 0;
            for (int column = 0; column < across; column++)
            {
                while (held < width)
                {
                    if (at >= packed.Length)
                    {
                        throw new FormatException($"A region payload ends inside row {row}.");
                    }

                    buffer |= (ulong)packed[at++] << held;
                    held += 8;
                }

                long residual = width == 0 ? 0 : (long)(buffer & ((1UL << width) - 1));
                buffer >>= width;
                held -= width;

                int index = (row * across) + column;
                long height = Unzig(residual) + Predict(heights, index, across);
                if (height is < short.MinValue or > short.MaxValue)
                {
                    throw new FormatException($"A region payload's sample {index} decodes to {height}, which is outside a 16-bit height.");
                }

                heights[index] = (short)height;
            }

            // Whatever is left of the last byte of a row is padding, and must be zero: a decoder that
            // ignored it would accept two different records for one surface.
            if (buffer != 0)
            {
                throw new FormatException($"A region payload's row {row} has a non-zero padding bit.");
            }
        }

        if (at != packed.Length)
        {
            throw new FormatException("A region payload has trailing packed bytes.");
        }

        return heights;
    }

    private static long ZigZag(long value) => (value << 1) ^ (value >> 63);

    private static long Unzig(long value) => (long)((ulong)value >> 1) ^ -(value & 1);

    /// <summary>How many bits a zig-zagged residual needs, which is what sets its row's width.</summary>
    private static int BitsFor(long value)
    {
        int bits = 0;
        while (value > 0)
        {
            bits++;
            value >>= 1;
        }

        return bits;
    }

    /// <summary>The content hash that names this payload's record file.</summary>
    public ContentHash Hash => ContentHash.Of(Bytes);

    /// <summary>The payload the given ground makes, checked against the shape its extent implies.</summary>
    /// <exception cref="ArgumentException">The heights are not the square the extent calls for.</exception>
    public static RegionPayload Of(
        string instancePath,
        string rule,
        ContentHash fieldHash,
        long extentMetres,
        short[] heights,
        string? patchRule = null,
        IReadOnlyList<BiomePatch>? patches = null)
    {
        ArgumentNullException.ThrowIfNull(heights);

        int across = TerrainHeightfield.SamplesAcross(extentMetres);
        if (heights.Length != across * across)
        {
            throw new ArgumentException($"A region of {extentMetres} m carries {across} by {across} samples, not {heights.Length}.", nameof(heights));
        }

        if ((patchRule is null) != (patches is null))
        {
            throw new ArgumentException("A payload carries a patch rule and its patches together or neither: a derived set with no rule that made it is not a record.", nameof(patchRule));
        }

        if (patches is { Count: > MaxPatches })
        {
            throw new ArgumentException($"A region carries {patches.Count} patches; the region category's derived budget is {MaxPatches}.", nameof(patches));
        }

        return new RegionPayload(instancePath, rule, fieldHash, extentMetres, heights, patchRule, patches is null ? null : [.. patches]);
    }

    /// <summary>
    /// The domain label these canonical bytes open with, read with the framing the writer wrote: a format
    /// version byte, then the label as length-prefixed text.
    /// </summary>
    private static string DomainOf(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        int at = 1;
        int length = 0;
        int shift = 0;
        while (true)
        {
            if (at >= bytes.Length || shift > 28)
            {
                throw new FormatException("A region payload record does not open with a domain label.");
            }

            byte piece = bytes[at++];
            length |= (piece & 0x7F) << shift;
            if ((piece & 0x80) == 0)
            {
                break;
            }

            shift += 7;
        }

        if (length < 0 || at + length > bytes.Length)
        {
            throw new FormatException("A region payload record's domain label runs past its end.");
        }

        return System.Text.Encoding.UTF8.GetString(bytes, at, length);
    }

    /// <summary>The payload these canonical bytes describe.</summary>
    /// <exception cref="FormatException">The bytes are not a canonical payload record.</exception>
    public static RegionPayload Decode(byte[] bytes)
    {
        // A record says which version it is, and the reader dispatches on the label it opened rather than
        // on what a caller guessed — the rule decision 0035 states for registry revisions, applied to a
        // payload that grew one (decision 0070 clause 7).
        bool carriesPatches = DomainOf(bytes) == "region-payload/2";
        var reader = new CanonicalReader(bytes, carriesPatches ? "region-payload/2" : "region-payload/1");
        string path = reader.ReadPath();
        string rule = reader.ReadPath();
        ContentHash field = reader.ReadContentHash();
        long extent = reader.ReadVarInt();

        int count = reader.ReadCount();
        if (count > MaxSamples)
        {
            throw new FormatException($"A region payload carries {count} samples; the bound is {MaxSamples}.");
        }

        short[] heights = Unpack(reader.ReadBytes(), count, TerrainHeightfield.SamplesAcross(extent));

        string? patchRule = null;
        List<BiomePatch>? patches = null;
        if (carriesPatches)
        {
            patchRule = reader.ReadPath();

            int patchCount = reader.ReadCount();
            if (patchCount > MaxPatches)
            {
                throw new FormatException($"A region payload carries {patchCount} patches; the region category's derived budget is {MaxPatches}.");
            }

            patches = new List<BiomePatch>(patchCount);
            for (int index = 0; index < patchCount; index++)
            {
                ulong ordinal = reader.ReadVarUInt();
                if (ordinal > int.MaxValue)
                {
                    throw new FormatException($"A region payload's patch {index} names biome {ordinal}, which is beyond a palette ordinal.");
                }

                patches.Add(new BiomePatch((int)ordinal, reader.ReadVarInt(), reader.ReadVarInt(), reader.ReadVarInt()));
            }
        }

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The region payload record has trailing bytes.");
        }

        try
        {
            return Of(path, rule, field, extent, heights, patchRule, patches);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
