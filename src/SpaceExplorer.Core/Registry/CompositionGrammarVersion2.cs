using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 2, over category registry revision 2: version 1 with the orbits ordered
/// and spaced. Iteration 1 drew every semi-major axis uniformly between a twentieth of an astronomical
/// unit and forty, so systems came out scattered, unordered, and mostly too cold to land on; here each
/// child of an orbit connector draws from its own band of a geometric progression, which is how the
/// planets of a real system are spaced and which orders them by construction.
/// </summary>
public static class CompositionGrammarVersion2
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 2;

    /// <summary>A turn is 2^32 binary turns (decision 0036).</summary>
    private const long Turn = 1L << 32;

    /// <summary>Artifact-local lengths are 1/65,536 m, so this is a quarter metre per attachment (decision 0036).</summary>
    private const long QuarterMetre = 1L << (ArtifactFractionBits - 2);

    /// <summary>Unchanged from version 1: star, planet, moon, and the moon's atmosphere.</summary>
    public const uint MaxDepth = CompositionGrammarVersion1.MaxDepth;

    public const uint MaxNodes = CompositionGrammarVersion1.MaxNodes;
    public const uint RetryBudget = CompositionGrammarVersion1.RetryBudget;

    /// <summary>
    /// The ratio between one planet's orbit and the next, 1.55 in 1/256. Eight bands from three tenths of
    /// an astronomical unit reach 10.1, which is the span of a real inner system, and the band a body
    /// falls in decides its temperature far more than anything else the description shows.
    /// </summary>
    public const uint PlanetSpacing = 397;

    /// <summary>The same for moons, 1.8, which separates them enough to read as distinct orbits.</summary>
    public const uint MoonSpacing = 461;

    /// <summary>The innermost planet band begins here, at three tenths of an astronomical unit.</summary>
    private const long InnermostOrbit = PhysicalConstants.AstronomicalUnitMetres * 3 / 10;

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision2.Registry,
        Version,
        MaxDepth,
        MaxNodes,
        RetryBudget,
        [
            new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)]),
            new RootRule(CompositionDomain.Artifact, [new CategoryChoice(ArtifactPart, 1)]),
        ],
        [
            new Production(Star,
            [
                new ConnectorRule(1, 8, Orbit(
                    semiMajorAxis: (InnermostOrbit, 12 * PhysicalConstants.AstronomicalUnitMetres),
                    eccentricity: (0, Fraction(3, 10)),
                    inclination: Degrees(7)),
                [
                    new CategoryChoice(Barycentre, 1),
                    new CategoryChoice(Planet, 9),
                ],
                PlanetSpacing),
            ]),

            new Production(Barycentre,
            [
                // A pair about a common centre is not a progression, so it keeps one band.
                new ConnectorRule(2, 2, Orbit(
                    semiMajorAxis: (100_000_000L, 2_000_000_000L),
                    eccentricity: (0, Fraction(1, 20)),
                    inclination: Degrees(7)),
                [new CategoryChoice(Planet, 1)]),
            ]),

            new Production(Planet,
            [
                new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),

                new ConnectorRule(0, 2, Orbit(
                    semiMajorAxis: (150_000_000L, 5_000_000_000L),
                    eccentricity: (0, Fraction(1, 10)),
                    inclination: Degrees(18)),
                [new CategoryChoice(Planet, 1)],
                MoonSpacing),
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

    private static long Fraction(long numerator, long denominator) => Turn * numerator / denominator;

    private static long Degrees(long degrees) => Turn * degrees / 360;
}
