using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 3, over category registry revision 2: version 2 with the orbits scaled to
/// the star and the bodies suited to the orbit they take. Iteration 2 spaced the orbits but measured them
/// from a fixed base, so a faint dwarf and a luminous giant handed their planets the same distances and
/// so wildly different temperatures; and nothing related a body's type to where it sat, so ice appeared
/// at 282 K. Here a star's bands scale by the square root of its luminosity, which holds each band at one
/// range of temperatures around any star, and each band demands a tag that only a body suited to it
/// provides.
/// </summary>
public static class CompositionGrammarVersion3
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 3;

    private const long Turn = 1L << 32;
    private const long QuarterMetre = 1L << (ArtifactFractionBits - 2);

    public const uint MaxDepth = CompositionGrammarVersion1.MaxDepth;
    public const uint MaxNodes = CompositionGrammarVersion1.MaxNodes;
    public const uint RetryBudget = CompositionGrammarVersion1.RetryBudget;

    /// <summary>Unchanged from version 2: 1.55 between one planet's orbit and the next, and 1.8 between moons.</summary>
    public const uint PlanetSpacing = CompositionGrammarVersion2.PlanetSpacing;

    public const uint MoonSpacing = CompositionGrammarVersion2.MoonSpacing;

    /// <summary>The innermost band of a star of one solar luminosity, at three tenths of an astronomical unit.</summary>
    private const long InnermostOrbit = PhysicalConstants.AstronomicalUnitMetres * 3 / 10;

    /// <summary>
    /// The tag each of a star's eight bands demands. At one solar luminosity the bands span 464 K down to
    /// 80 K, so the first two are hot, the next three temperate, and the last three cold; scaling by the
    /// square root of luminosity holds those figures for any star.
    /// </summary>
    public static readonly string[] Zones =
    [
        "zone-hot", "zone-hot",
        "zone-temperate", "zone-temperate", "zone-temperate",
        "zone-cold", "zone-cold", "zone-cold",
    ];

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
                // The declared range is the envelope over every star the vocabulary admits, from a cool
                // dwarf's innermost band at 0.010 AU to a hot star's outermost at 346; the scale moves
                // each system's own progression inside it.
                new ConnectorRule(1, 8, Orbit(
                    semiMajorAxis: (PhysicalConstants.AstronomicalUnitMetres / 200, 400 * PhysicalConstants.AstronomicalUnitMetres),
                    eccentricity: (0, Fraction(3, 10)),
                    inclination: Degrees(7)),
                [
                    new CategoryChoice(Barycentre, 1),
                    new CategoryChoice(Planet, 9),
                ],
                PlanetSpacing,
                DerivationRules.OrbitScaleRevision,
                Zones,
                InnermostOrbit),
            ]),

            new Production(Barycentre,
            [
                // The pair shares the barycentre's distance from the star, so it shares its zone.
                new ConnectorRule(2, 2, Orbit(
                    semiMajorAxis: (100_000_000L, 2_000_000_000L),
                    eccentricity: (0, Fraction(1, 20)),
                    inclination: Degrees(7)),
                [new CategoryChoice(Planet, 1)],
                InheritsBandTag: true),
            ]),

            new Production(Planet,
            [
                new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),

                // A moon shares its planet's distance from the star, so it must suit the same zone; what
                // keeps it smaller than its planet is the mass tag the planet's own declaration requires.
                new ConnectorRule(0, 2, Orbit(
                    semiMajorAxis: (150_000_000L, 5_000_000_000L),
                    eccentricity: (0, Fraction(1, 10)),
                    inclination: Degrees(18)),
                [new CategoryChoice(Planet, 1)],
                MoonSpacing,
                InheritsBandTag: true),
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
