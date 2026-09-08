using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Description;

/// <summary>
/// The numeric limits a body must fall inside for a suited explorer to work on it: the first of the
/// versioned numeric tables the development plan calls for. It is one canonical record identified by its
/// content hash, defined in code as version 1 and moved to <c>content/</c> when an authoring tool needs
/// it, exactly as the category registry and the composition grammar are (decisions 0035, 0048). Every
/// system description pins the version and the hash, so a change to a limit is a new version and shows
/// in the description it produced.
/// </summary>
/// <remarks>
/// Version 1 states what the suit is built for rather than what a person could survive unaided: a
/// pressure suit already handles vacuum and cold, so the limits are the structural ones, gravity that can
/// be walked and carried in, pressure the suit resists, and the span its life support holds. The values
/// are untuned; the solar-system iterations of decision 0047 revise them.
/// </remarks>
public sealed class SuitProfile
{
    public const string Domain = "suit-profile/1";

    /// <summary>The version this build defines.</summary>
    public const uint CurrentVersion = 1;

    private readonly byte[] _bytes;

    private SuitProfile(uint version, long minimumGravity, long maximumGravity, long maximumPressure, long minimumTemperature, long maximumTemperature, byte[] bytes, ContentHash hash)
    {
        Version = version;
        MinimumGravity = minimumGravity;
        MaximumGravity = maximumGravity;
        MaximumPressure = maximumPressure;
        MinimumTemperature = minimumTemperature;
        MaximumTemperature = maximumTemperature;
        _bytes = bytes;
        Hash = hash;
    }

    /// <summary>Version 1: a tenth of standard gravity to one and a half of it, up to twenty bar, and 120 K to 400 K.</summary>
    public static SuitProfile Version1 { get; } = Create(
        CurrentVersion,
        minimumGravity: (PhysicalConstants.StandardGravityMillimetres + 5) / 10,
        maximumGravity: ((PhysicalConstants.StandardGravityMillimetres * 3) + 1) / 2,
        maximumPressure: 2_000_000,
        minimumTemperature: 120,
        maximumTemperature: 400);

    public uint Version { get; }

    /// <summary>The least surface gravity in mm/s^2 a suited explorer can work in.</summary>
    public long MinimumGravity { get; }

    /// <summary>The greatest surface gravity in mm/s^2.</summary>
    public long MaximumGravity { get; }

    /// <summary>The greatest surface pressure in pascals the suit resists; vacuum is admitted, so there is no lower limit.</summary>
    public long MaximumPressure { get; }

    /// <summary>The least equilibrium temperature in kelvin the life support holds.</summary>
    public long MinimumTemperature { get; }

    /// <summary>The greatest equilibrium temperature in kelvin.</summary>
    public long MaximumTemperature { get; }

    public byte[] Bytes => [.. _bytes];

    /// <summary>The content hash a system description pins.</summary>
    public ContentHash Hash { get; }

    public static SuitProfile Create(uint version, long minimumGravity, long maximumGravity, long maximumPressure, long minimumTemperature, long maximumTemperature)
    {
        if (version == 0)
        {
            throw new ArgumentException("Suit profile versions start at 1.", nameof(version));
        }

        if (minimumGravity <= 0 || minimumGravity > maximumGravity)
        {
            throw new ArgumentException($"The gravity limits [{minimumGravity}, {maximumGravity}] mm/s^2 are empty or admit weightlessness.", nameof(minimumGravity));
        }

        if (maximumPressure < 0 || minimumTemperature <= 0 || minimumTemperature > maximumTemperature)
        {
            throw new ArgumentException("The pressure and temperature limits must be non-negative and non-empty.", nameof(maximumPressure));
        }

        var writer = new CanonicalWriter(Domain);
        writer.WriteUInt32(version);
        writer.WriteVarInt(minimumGravity);
        writer.WriteVarInt(maximumGravity);
        writer.WriteVarInt(maximumPressure);
        writer.WriteVarInt(minimumTemperature);
        writer.WriteVarInt(maximumTemperature);

        return new SuitProfile(version, minimumGravity, maximumGravity, maximumPressure, minimumTemperature, maximumTemperature, writer.ToArray(), writer.ToContentHash());
    }

    /// <summary>The limits <paramref name="gravity"/>, <paramref name="pressure"/>, and <paramref name="temperature"/> fall outside, in a fixed order; empty when the body is within every one.</summary>
    public IReadOnlyList<string> Refusals(long gravity, long pressure, long temperature)
    {
        var refusals = new List<string>(3);
        if (gravity < MinimumGravity || gravity > MaximumGravity)
        {
            refusals.Add("gravity");
        }

        if (pressure > MaximumPressure)
        {
            refusals.Add("pressure");
        }

        if (temperature < MinimumTemperature || temperature > MaximumTemperature)
        {
            refusals.Add("temperature");
        }

        return refusals;
    }
}
