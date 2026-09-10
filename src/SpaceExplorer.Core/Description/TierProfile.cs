using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Description;

/// <summary>
/// What one distance tier demands of a system: how many explorable planets, how many life-bearing
/// bodies, and which star classes it may draw from where it requires life (decision 0044). A count of
/// <see cref="Unbounded"/> is no upper limit.
/// </summary>
public sealed record TierDemand(uint MinimumExplorable, uint MaximumExplorable, uint MinimumLifeBearing, uint MaximumLifeBearing, IReadOnlyList<string> LifeBearingClasses)
{
    /// <summary>The value that means a count is not bounded above.</summary>
    public const uint Unbounded = uint.MaxValue;
}

/// <summary>
/// The numbers the per-tier rules of [decision 0044](../../../docs/decisions/0044-tiered-system-composition-one-star-and-explorability-by-validation.md)
/// apply, as one canonical record with a version and a content hash, exactly as the suit profile carries
/// the limits that decide a landing candidate ([decision 0049](../../../docs/decisions/0049-suit-profile-and-system-description-version-1-and-derivation-rules-as-named-implementations.md)).
/// A destination pins the profile its verdict was given under, so a stored "pass" can be re-checked
/// against the rules that produced it and a later tuning of those rules cannot silently reinterpret it
/// ([decision 0058](../../../docs/decisions/0058-tier-rules-are-a-pinned-record.md)).
/// </summary>
/// <remarks>
/// The rules themselves — that a tier counts planets and not moons, and that life follows the presence of
/// a life primitive — are implementation, named by the version, as a derivation rule is named by its
/// identifier. What this record holds is every value a tuning would move.
/// </remarks>
public sealed class TierProfile
{
    public const string Domain = "tier-profile/1";

    /// <summary>The version this build defines.</summary>
    public const uint CurrentVersion = 1;

    private readonly byte[] _bytes;
    private readonly TierDemand[] _demands;

    private TierProfile(uint version, TierDemand[] demands, byte[] bytes, ContentHash hash)
    {
        Version = version;
        _demands = demands;
        _bytes = bytes;
        Hash = hash;
    }

    /// <summary>
    /// Version 1, which is what decision 0044 states and what iterations 1 to 4 were judged against: the
    /// starter tier has exactly one explorable planet and no life, and the tier above it has at least two
    /// explorable planets and at least one life-bearing body, around a star cool enough to have one.
    /// </summary>
    public static TierProfile Version1 { get; } = Create(CurrentVersion,
    [
        new TierDemand(1, 1, 0, 0, []),
        new TierDemand(2, TierDemand.Unbounded, 1, TierDemand.Unbounded, ["F", "G", "K", "M"]),
    ]);

    public uint Version { get; }

    /// <summary>What each tier demands, in <see cref="DistanceTier"/> order.</summary>
    public IReadOnlyList<TierDemand> Demands => _demands;

    public byte[] Bytes => [.. _bytes];

    /// <summary>The content hash of the canonical record: what a destination pins to name these rules exactly.</summary>
    public ContentHash Hash { get; }

    /// <summary>What <paramref name="tier"/> demands under this profile.</summary>
    /// <exception cref="ArgumentException">The tier is not one this profile carries.</exception>
    public TierDemand For(DistanceTier tier) =>
        (int)tier - 1 is >= 0 and int index && index < _demands.Length
            ? _demands[index]
            : throw new ArgumentException($"Tier profile version {Version} carries {_demands.Length} tiers; {(byte)tier} is not one of them.", nameof(tier));

    /// <summary>Builds and validates a profile, one demand per tier in <see cref="DistanceTier"/> order.</summary>
    /// <exception cref="ArgumentException">A demand is inconsistent, or the count does not match the tiers this build knows.</exception>
    public static TierProfile Create(uint version, IReadOnlyList<TierDemand> demands)
    {
        ArgumentNullException.ThrowIfNull(demands);

        if (version == 0)
        {
            throw new ArgumentException("Tier profile versions start at 1.", nameof(version));
        }

        if (demands.Count != Enum.GetValues<DistanceTier>().Length)
        {
            throw new ArgumentException($"This build has {Enum.GetValues<DistanceTier>().Length} tiers; the profile carries {demands.Count}.", nameof(demands));
        }

        var writer = new CanonicalWriter(Domain);
        writer.WriteUInt32(version);
        writer.WriteCount(demands.Count);
        foreach (TierDemand demand in demands)
        {
            if (demand.MaximumExplorable < demand.MinimumExplorable || demand.MaximumLifeBearing < demand.MinimumLifeBearing)
            {
                throw new ArgumentException("A tier's maximum count is below its minimum.", nameof(demands));
            }

            writer.WriteUInt32(demand.MinimumExplorable);
            writer.WriteUInt32(demand.MaximumExplorable);
            writer.WriteUInt32(demand.MinimumLifeBearing);
            writer.WriteUInt32(demand.MaximumLifeBearing);
            writer.WriteCount(demand.LifeBearingClasses.Count);
            foreach (string spectralClass in demand.LifeBearingClasses)
            {
                writer.WriteText(spectralClass);
            }
        }

        return new TierProfile(version, [.. demands], writer.ToArray(), writer.ToContentHash());
    }

    /// <summary>Decodes a record, rejecting trailing bytes and a non-canonical encoding.</summary>
    /// <exception cref="FormatException">The bytes are not a valid tier profile.</exception>
    public static TierProfile Decode(byte[] bytes)
    {
        var reader = new CanonicalReader(bytes, Domain);
        uint version = reader.ReadUInt32();
        int count = reader.ReadCount();
        var demands = new TierDemand[count];
        for (int index = 0; index < count; index++)
        {
            uint minimumExplorable = reader.ReadUInt32();
            uint maximumExplorable = reader.ReadUInt32();
            uint minimumLife = reader.ReadUInt32();
            uint maximumLife = reader.ReadUInt32();
            int classes = reader.ReadCount();
            var spectralClasses = new string[classes];
            for (int entry = 0; entry < classes; entry++)
            {
                spectralClasses[entry] = reader.ReadText();
            }

            demands[index] = new TierDemand(minimumExplorable, maximumExplorable, minimumLife, maximumLife, spectralClasses);
        }

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The tier profile record has trailing bytes.");
        }

        TierProfile profile;
        try
        {
            profile = Create(version, demands);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        return profile._bytes.AsSpan().SequenceEqual(bytes)
            ? profile
            : throw new FormatException("The tier profile record is not in canonical form: re-encoding it yields different bytes.");
    }
}
