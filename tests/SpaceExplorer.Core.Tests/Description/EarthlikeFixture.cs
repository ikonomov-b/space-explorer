using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Description;

/// <summary>
/// One system pinned to the real Sun and Earth, so the description of a known world can be read back
/// exactly. Every template range is a single value, so the set holds one definition of each and the
/// composition has nothing to draw; what is being checked is the derivation and the wording, not the
/// generator. The figures are the NASA fact-sheet ones the derivation vectors use.
/// </summary>
internal static class EarthlikeFixture
{
    public static readonly CategoryRegistry Registry = CategoryRegistryRevision1.Registry;
    public static readonly PackId TemplatePack = PackId.Parse("5011ab00c0ffee00d00d00feed00face");

    /// <summary>Earth's orbit, 1 AU exactly, with every element and the epoch pinned to zero.</summary>
    private static readonly TransformRange EarthOrbit = new(TransformKind.OrbitalElements,
    [
        (PhysicalConstants.AstronomicalUnitMetres, PhysicalConstants.AstronomicalUnitMetres),
        (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0),
    ]);

    public static readonly TemplateVocabulary Vocabulary = TemplateVocabulary.Create(Registry, TemplatePack,
    [
        // The Sun: 1.989 x 10^30 kg, 6.957 x 10^8 m, 3.828 x 10^26 W, 5772 K, class G.
        PrimitiveTemplate.Create(Registry, 1, "sun", Star,
        [
            ParameterRange.Integer(SolarMass, SolarMass),
            ParameterRange.Integer(695_700_000L << PlanetFractionBits, 695_700_000L << PlanetFractionBits),
            ParameterRange.Integer(3_828_000, 3_828_000),
            ParameterRange.Integer(5_772, 5_772),
            ParameterRange.Enum(4),
        ], [ConnectorDeclaration.Empty]),

        // Earth: 5.972 x 10^24 kg, 6,371 km, Bond albedo 0.306, one sidereal day, rocky.
        PrimitiveTemplate.Create(Registry, 2, "earth", Planet,
        [
            ParameterRange.Integer(EarthMass, EarthMass),
            ParameterRange.Integer(6_371_000L << PlanetFractionBits, 6_371_000L << PlanetFractionBits),
            ParameterRange.Integer(20_054, 20_054),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Integer(20 * 86_164, 20 * 86_164),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Enum(0),
        ], [ConnectorDeclaration.Empty, ConnectorDeclaration.Empty]),

        // Its atmosphere at 101,325 Pa.
        PrimitiveTemplate.Create(Registry, 3, "air", Atmosphere,
        [
            ParameterRange.Enum(1),
            ParameterRange.Integer(101_325, 101_325),
            ParameterRange.Enum(1),
            ParameterRange.Colour((120, 120), (150, 150), (220, 220), (255, 255)),
        ], []),
    ]);

    public static readonly PrimitiveSet Set = SetGenerator.Generate(
        SetSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, 1, TemplatePack, Vocabulary.Hash, [new SetRequest(1, 1), new SetRequest(2, 1), new SetRequest(3, 1)], 4),
        Vocabulary,
        Registry);

    /// <summary>A grammar that gives the star exactly <paramref name="planets"/> planets, each with its atmosphere and no moon.</summary>
    public static CompositionGrammar Grammar(uint planets) => CompositionGrammar.Create(Registry, 1, 2, 8, 4,
        [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)])],
        [
            new Production(Star, [new ConnectorRule(planets, planets, EarthOrbit, [new CategoryChoice(Planet, 1)])]),
            new Production(Planet,
            [
                new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
                new ConnectorRule(0, 0, TransformRange.Origin(TransformKind.OrbitalElements), []),
            ]),
            new Production(Atmosphere, []),
        ]);

    public static SystemDescription Describe(uint planets) => Describe(Set, Grammar(planets), seed: 1);

    /// <summary>
    /// The same Sun and Earth with a Jupiter beside them, and connector tags that decide which is which:
    /// the star admits only a body tagged heavy, and that body admits only one tagged light. So the one
    /// planet of the system is a gas giant no suit can stand on, and the one landable body is its moon.
    /// </summary>
    public static SystemDescription GiantWithLandableMoon()
    {
        CompositionGrammar grammar = CompositionGrammar.Create(Registry, 1, 3, 8, 4,
            [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)])],
            [
                new Production(Star, [new ConnectorRule(1, 1, EarthOrbit, [new CategoryChoice(Planet, 1)])]),
                new Production(Planet,
                [
                    new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
                    // Optional, because a planet that must carry a moon must carry one without end, and
                    // the grammar refuses such a chain outright (decision 0048).
                    new ConnectorRule(0, 1, EarthOrbit, [new CategoryChoice(Planet, 1)]),
                ]),
                new Production(Atmosphere, []),
            ]);

        // The moon is an optional child, so the first seed that draws one is taken; the search is over a
        // fixed range and is as deterministic as the composition itself.
        return Enumerable.Range(1, 50)
            .Select(seed => Describe(GiantSet, grammar, (ulong)seed))
            .First(description => description.MoonCount == 1);
    }

    private static SystemDescription Describe(PrimitiveSet set, CompositionGrammar grammar, ulong seed)
    {
        GraphSpecification specification = GraphSpecification.Create(
            Registry.Revision, Registry.Hash, GeneratorVersion.Current, grammar.Version, grammar.Hash, seed, set.Manifest.Pack, set.Manifest.Hash, CompositionDomain.SolarSystem);

        return SystemDescription.Derive(CompositionGenerator.Generate(specification, set, grammar, Registry), Registry, SuitProfile.Version1);
    }

    /// <summary>The tagged vocabulary of <see cref="GiantWithLandableMoon"/>: the same three templates, plus Jupiter.</summary>
    private static readonly TemplateVocabulary GiantVocabulary = TemplateVocabulary.Create(Registry, PackId.Parse("5011ab00c0ffee00d00d00feed00fade"),
    [
        PrimitiveTemplate.Create(Registry, 1, "sun", Star,
            [.. Vocabulary.Templates[0].Ranges], [new ConnectorDeclaration([], ["heavy"])]),

        PrimitiveTemplate.Create(Registry, 2, "earth", Planet,
            [.. Vocabulary.Templates[1].Ranges], [ConnectorDeclaration.Empty, new ConnectorDeclaration(["light"], [])]),

        PrimitiveTemplate.Create(Registry, 3, "air", Atmosphere, [.. Vocabulary.Templates[2].Ranges], []),

        // Jupiter: 1.898 x 10^27 kg at 71,492 km is 24.79 m/s^2, well past what the suit admits.
        PrimitiveTemplate.Create(Registry, 4, "jupiter", Planet,
        [
            ParameterRange.Integer(18_980_000, 18_980_000),
            ParameterRange.Integer(71_492_000L << PlanetFractionBits, 71_492_000L << PlanetFractionBits),
            ParameterRange.Integer(22_479, 22_479),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Integer(20 * 35_730, 20 * 35_730),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Enum(3),
        ], [ConnectorDeclaration.Empty, new ConnectorDeclaration(["heavy"], ["light"])]),
    ]);

    private static readonly PrimitiveSet GiantSet = SetGenerator.Generate(
        SetSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, 1, GiantVocabulary.Pack, GiantVocabulary.Hash, [new SetRequest(1, 1), new SetRequest(2, 1), new SetRequest(3, 1), new SetRequest(4, 1)], 4),
        GiantVocabulary,
        Registry);
}
