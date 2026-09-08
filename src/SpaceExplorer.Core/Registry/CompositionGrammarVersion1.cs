using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 1 over category registry revision 1: the first structures that compose
/// from a stored set: a solar system rooted at one star (decision 0044) and an artifact rooted at one
/// part. Defined in code and frozen by its recorded hash, as registry
/// revision 1 is; a second version or an authoring tool moves the source form to <c>content/</c>
/// (decision 0035). Every count, weight, and transform range here is untuned numeric content: the
/// solar-system iterations judge it and revise it under a new version (decision 0047).
/// </summary>
public static class CompositionGrammarVersion1
{
    /// <summary>A turn is 2^32 binary turns, so this is the scale every angular bound below is derived from (decision 0036).</summary>
    private const long Turn = 1L << 32;

    /// <summary>Artifact-local lengths are 1/65,536 m, so this is a quarter metre per attachment (decision 0036).</summary>
    private const long QuarterMetre = 1L << (ArtifactFractionBits - 2);

    /// <summary>
    /// The deepest instance a graph may hold: a solar system reaches star, planet, moon, and the moon's
    /// atmosphere, and an artifact reaches three attachment edges, which at <see cref="QuarterMetre"/>
    /// each keeps a part within 0.75 m of the root and so inside the two-metre cap of decision 0046.
    /// </summary>
    public const uint MaxDepth = 3;

    /// <summary>The instance budget: above the couple of hundred a system of this grammar produces, and far below the format's own bound.</summary>
    public const uint MaxNodes = 1_024;

    /// <summary>How many times a child slot redraws a category when the first draw has no compatible definition.</summary>
    public const uint RetryBudget = 8;

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision1.Registry,
        CompositionGrammar.CurrentVersion,
        MaxDepth,
        MaxNodes,
        RetryBudget,
        [
            // One star roots a system; a barycentre never does, so a system has exactly one star (decision 0044).
            new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)]),
            new RootRule(CompositionDomain.Artifact, [new CategoryChoice(ArtifactPart, 1)]),
        ],
        [
            new Production(Star,
            [
                new ConnectorRule(1, 8, Orbit(
                    semiMajorAxis: (PhysicalConstants.AstronomicalUnitMetres / 20, 40 * PhysicalConstants.AstronomicalUnitMetres),
                    eccentricity: (0, Fraction(3, 10)),
                    inclination: Degrees(7)),
                [
                    new CategoryChoice(Barycentre, 1),
                    new CategoryChoice(Planet, 9),
                ]),
            ]),

            new Production(Barycentre,
            [
                // Two bodies about a common centre, close together and nearly coplanar.
                new ConnectorRule(2, 2, Orbit(
                    semiMajorAxis: (100_000_000L, 2_000_000_000L),
                    eccentricity: (0, Fraction(1, 20)),
                    inclination: Degrees(7)),
                [new CategoryChoice(Planet, 1)]),
            ]),

            new Production(Planet,
            [
                // Every body carries exactly one atmosphere primitive, an airless one where there is no
                // air, as the registry's cardinality and decision 0031 require; it is centred on its
                // body, so its rigid transform is the body's own frame.
                new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),

                // Registry revision 1 models a moon as a planet on a planet's orbit connector, so the
                // depth bound is what stops a moon of a moon; the iterations of decision 0047 judge that.
                new ConnectorRule(0, 2, Orbit(
                    semiMajorAxis: (150_000_000L, 5_000_000_000L),
                    eccentricity: (0, Fraction(1, 10)),
                    inclination: Degrees(18)),
                [new CategoryChoice(Planet, 1)]),
            ]),

            new Production(Atmosphere, []),

            new Production(ArtifactPart,
            [
                new ConnectorRule(0, 3, new TransformRange(TransformKind.Rigid,
                [
                    (-QuarterMetre, QuarterMetre),
                    (-QuarterMetre, QuarterMetre),
                    (-QuarterMetre, QuarterMetre),
                    (int.MinValue, int.MaxValue),
                    (int.MinValue, int.MaxValue),
                    (int.MinValue, int.MaxValue),
                ]),
                [new CategoryChoice(ArtifactPart, 1)]),
            ]),
        ]);

    /// <summary>An orbit range: the three elements worth bounding, with the node, periapsis, and anomaly angles left free and the epoch at the world epoch (decision 0037).</summary>
    private static TransformRange Orbit((long Min, long Max) semiMajorAxis, (long Min, long Max) eccentricity, long inclination) =>
        new(TransformKind.OrbitalElements,
        [
            semiMajorAxis,
            eccentricity,
            (-inclination, inclination),
            (int.MinValue, int.MaxValue),
            (int.MinValue, int.MaxValue),
            (int.MinValue, int.MaxValue),
            (0, 0),
        ]);

    /// <summary>A fraction of 2^32, the unit the eccentricity element is stored in.</summary>
    private static long Fraction(long numerator, long denominator) => Turn * numerator / denominator;

    /// <summary>An angle in whole degrees as binary turns.</summary>
    private static long Degrees(long degrees) => Turn * degrees / 360;
}
