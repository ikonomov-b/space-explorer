using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Description;

/// <summary>
/// What the geometry of a region demands of a body, as one canonical record with a version and a content
/// hash: today the least reference radius a body may have and still carry a region, which
/// [decision 0041](../../../docs/decisions/0041-planet-fields-tangent-regions-minimum-radius-and-far-field.md)
/// derives from a maximal region on a sphere. It sits beside the suit profile rather than inside it,
/// because a suit profile says what a person survives and this says what a body's shape allows, and the
/// region rules of [decision 0044](../../../docs/decisions/0044-tiered-system-composition-one-star-and-explorability-by-validation.md)
/// will grow here when regions exist ([decision 0059](../../../docs/decisions/0059-region-limits-are-a-pinned-record.md)).
/// </summary>
/// <remarks>
/// A landing candidate is decided by four numbers: three suit limits and this one. The other three have
/// been a pinned record since decision 0049 precisely so a stored verdict can be re-checked against the
/// limits that gave it; this one was a code constant outside every record, which is what
/// [review finding 51](../../../docs/review.md#51-the-minimum-landable-radius-decides-a-verdict-from-a-code-constant)
/// records.
/// </remarks>
public sealed class RegionLimits
{
    public const string Domain = "region-limits/1";

    /// <summary>The version this build defines.</summary>
    public const uint CurrentVersion = 1;

    private readonly byte[] _bytes;

    private RegionLimits(uint version, long minimumLandableRadiusUnits, byte[] bytes, ContentHash hash)
    {
        Version = version;
        MinimumLandableRadiusUnits = minimumLandableRadiusUnits;
        _bytes = bytes;
        Hash = hash;
    }

    /// <summary>Version 1: the 524,288 m minimum landable radius of decision 0041, in the planet-fixed unit of decision 0036.</summary>
    public static RegionLimits Version1 { get; } = Create(CurrentVersion, CategoryRegistryRevision1.MinimumLandableRadius);

    public uint Version { get; }

    /// <summary>The least reference radius, in 1/256 m, at which a body may carry a region.</summary>
    public long MinimumLandableRadiusUnits { get; }

    public byte[] Bytes => [.. _bytes];

    /// <summary>The content hash of the canonical record: what a destination pins to name these limits exactly.</summary>
    public ContentHash Hash { get; }

    /// <summary>Whether a body of <paramref name="radiusUnits"/> is large enough to carry a region.</summary>
    public bool AdmitsRegion(long radiusUnits) => radiusUnits >= MinimumLandableRadiusUnits;

    /// <exception cref="ArgumentException">The version is zero or the radius is not positive.</exception>
    public static RegionLimits Create(uint version, long minimumLandableRadiusUnits)
    {
        if (version == 0)
        {
            throw new ArgumentException("Region-limit versions start at 1.", nameof(version));
        }

        if (minimumLandableRadiusUnits <= 0)
        {
            throw new ArgumentException("A minimum landable radius is positive.", nameof(minimumLandableRadiusUnits));
        }

        var writer = new CanonicalWriter(Domain);
        writer.WriteUInt32(version);
        writer.WriteVarInt(minimumLandableRadiusUnits);

        return new RegionLimits(version, minimumLandableRadiusUnits, writer.ToArray(), writer.ToContentHash());
    }

    /// <summary>Decodes a record, rejecting trailing bytes and a non-canonical encoding.</summary>
    /// <exception cref="FormatException">The bytes are not a valid region-limits record.</exception>
    public static RegionLimits Decode(byte[] bytes)
    {
        var reader = new CanonicalReader(bytes, Domain);
        uint version = reader.ReadUInt32();
        long radius = reader.ReadVarInt();

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The region-limits record has trailing bytes.");
        }

        RegionLimits limits;
        try
        {
            limits = Create(version, radius);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        return limits._bytes.AsSpan().SequenceEqual(bytes)
            ? limits
            : throw new FormatException("The region-limits record is not in canonical form: re-encoding it yields different bytes.");
    }
}
