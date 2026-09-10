using System.Globalization;

namespace SpaceExplorer.Core.Description;

/// <summary>The core release's two distance tiers (decisions 0043, 0044).</summary>
public enum DistanceTier : byte
{
    Starter = 1,
    Second = 2,
}

/// <summary>
/// The per-tier composition rules of decision 0044, checked mechanically against a system description as
/// decision 0047 requires of a generation iteration. What can be checked is bounded by what the
/// description can see: until regions exist, "explorable" is the landing-candidate flag of decision 0047,
/// and life is the presence of a life primitive, which a registry revision without a life category can
/// never produce. The per-tier planet and region counts wait on the numeric tables.
/// </summary>
public static class TierRules
{
    /// <summary>
    /// The rules <paramref name="description"/> breaks at <paramref name="tier"/> under
    /// <paramref name="profile"/>, in a fixed order; empty when it passes. The counts come from the
    /// profile rather than from here, so what a tier demands is a record a destination can pin
    /// (decision 0058).
    /// </summary>
    public static IReadOnlyList<string> Check(SystemDescription description, DistanceTier tier, TierProfile profile)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(profile);

        // Decision 0044 counts moons separately from planets, so only a planet of the star or of a
        // barycentre counts towards a tier's explorable-planet rule, however landable a moon is. Registry
        // revision 1 has no moon category, so the role comes from the connector a body hangs on.
        int candidates = description.Bodies.Count(body => body.Role == BodyRole.Planet && body.LandingCandidate);
        var failures = new List<string>();

        if (!Enum.IsDefined(tier))
        {
            return [$"unknown distance tier {((byte)tier).ToString(CultureInfo.InvariantCulture)}"];
        }

        TierDemand demand = profile.For(tier);

        if (candidates < demand.MinimumExplorable || candidates > demand.MaximumExplorable)
        {
            failures.Add($"the {Label(tier)} tier has {Count(demand.MinimumExplorable, demand.MaximumExplorable, "explorable planet")}; this system has {candidates.ToString(CultureInfo.InvariantCulture)}");
        }

        if (description.LifeBearingCount < demand.MinimumLifeBearing || description.LifeBearingCount > demand.MaximumLifeBearing)
        {
            failures.Add($"the {Label(tier)} tier has {Count(demand.MinimumLifeBearing, demand.MaximumLifeBearing, "life-bearing body")}; this system has {description.LifeBearingCount.ToString(CultureInfo.InvariantCulture)}");
        }

        // Only a tier that requires life constrains the star, because the class is what makes life
        // possible at all (decision 0044).
        if (demand.MinimumLifeBearing > 0 && demand.LifeBearingClasses.Count > 0 && !demand.LifeBearingClasses.Contains(description.Star.Type))
        {
            failures.Add($"a tier that requires life draws its star from {string.Join(", ", demand.LifeBearingClasses)}; this one is class {description.Star.Type}");
        }

        return failures;
    }

    /// <summary>How a demand reads in a failure: exactly, at least, at most, or between.</summary>
    private static string Count(uint minimum, uint maximum, string what)
    {
        string plural = what + "s";
        if (minimum == maximum)
        {
            return $"exactly {minimum.ToString(CultureInfo.InvariantCulture)} {(minimum == 1 ? what : plural)}";
        }

        if (maximum == TierDemand.Unbounded)
        {
            return $"at least {minimum.ToString(CultureInfo.InvariantCulture)} {(minimum == 1 ? what : plural)}";
        }

        return minimum == 0
            ? $"at most {maximum.ToString(CultureInfo.InvariantCulture)} {plural}"
            : $"between {minimum.ToString(CultureInfo.InvariantCulture)} and {maximum.ToString(CultureInfo.InvariantCulture)} {plural}";
    }

    /// <summary>The tier <paramref name="label"/> names, or null.</summary>
    public static DistanceTier? TryParse(string label) => label switch
    {
        "starter" => DistanceTier.Starter,
        "second" => DistanceTier.Second,
        _ => null,
    };

    /// <summary>The label of <paramref name="tier"/>.</summary>
    public static string Label(DistanceTier tier) => tier == DistanceTier.Starter ? "starter" : "second";

    /// <summary>Every tier's label, for a usage message.</summary>
    public static IEnumerable<string> Labels => Enum.GetValues<DistanceTier>().Select(Label);
}
