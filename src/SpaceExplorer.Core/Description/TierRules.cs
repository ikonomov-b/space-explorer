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
    /// <summary>The star classes a tier that requires life may draw from (decision 0044).</summary>
    private static readonly string[] LifeBearingClasses = ["F", "G", "K", "M"];

    /// <summary>The rules <paramref name="description"/> breaks at <paramref name="tier"/>, in a fixed order; empty when it passes.</summary>
    public static IReadOnlyList<string> Check(SystemDescription description, DistanceTier tier)
    {
        ArgumentNullException.ThrowIfNull(description);

        // Decision 0044 counts moons separately from planets, so only a planet of the star or of a
        // barycentre counts towards a tier's explorable-planet rule, however landable a moon is. Registry
        // revision 1 has no moon category, so the role comes from the connector a body hangs on.
        int candidates = description.Bodies.Count(body => body.Role == BodyRole.Planet && body.LandingCandidate);
        var failures = new List<string>();

        switch (tier)
        {
            case DistanceTier.Starter:
                if (candidates != 1)
                {
                    failures.Add($"the starter tier has exactly one explorable planet; this system has {candidates.ToString(CultureInfo.InvariantCulture)}");
                }

                if (description.LifeBearingCount != 0)
                {
                    failures.Add($"the starter tier bears no life; this system has {description.LifeBearingCount.ToString(CultureInfo.InvariantCulture)} life-bearing bodies");
                }

                break;

            case DistanceTier.Second:
                if (candidates < 2)
                {
                    failures.Add($"a tier above the starter has at least two explorable planets; this system has {candidates.ToString(CultureInfo.InvariantCulture)}");
                }

                if (description.LifeBearingCount < 1)
                {
                    failures.Add("a tier above the starter has at least one life-bearing planet; this system has none");
                }

                if (!LifeBearingClasses.Contains(description.Star.Type))
                {
                    failures.Add($"a tier that requires life draws its star from {string.Join(", ", LifeBearingClasses)}; this one is class {description.Star.Type}");
                }

                break;

            default:
                failures.Add($"unknown distance tier {((byte)tier).ToString(CultureInfo.InvariantCulture)}");
                break;
        }

        return failures;
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
