using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Generation;

/// <summary>
/// Two bodies that suit opposite zones and a grammar version 3 that demands one per band, so which body
/// takes which orbit is decided entirely by the band tags. The star is the Sun, so its orbit scale is one
/// and the bands are exactly where the grammar puts them.
/// </summary>
internal static class ZoneFixture
{
    public const uint HotBody = 2;
    public const uint ColdBody = 3;

    public static readonly CategoryRegistry Registry = CategoryRegistryRevision2.Registry;

    /// <summary>Where the innermost band begins, and the ratio to the next.</summary>
    public const long Base = PhysicalConstants.AstronomicalUnitMetres * 3 / 10;

    public const uint Spacing = 397;

    private static readonly TemplateVocabulary Vocabulary = TemplateVocabulary.Create(Registry, PackId.Parse("2000e5c0de00b0dd1e5000fa11ab1e00"),
    [
        PrimitiveTemplate.Create(Registry, 1, "sun", Star,
        [
            ParameterRange.Integer(SolarMass, SolarMass),
            ParameterRange.Integer(695_700_000L << PlanetFractionBits, 695_700_000L << PlanetFractionBits),
            ParameterRange.Integer(5_772, 5_772),
        ], [ConnectorDeclaration.Empty]),

        Body(HotBody, "hot-body", "zone-hot"),
        Body(ColdBody, "cold-body", "zone-cold"),

        PrimitiveTemplate.Create(Registry, 4, "air", Atmosphere,
        [
            ParameterRange.Enum(1),
            ParameterRange.Integer(101_325, 101_325),
            ParameterRange.Enum(1),
            ParameterRange.Colour((120, 120), (150, 150), (220, 220), (255, 255)),
        ], []),
    ]);

    public static readonly PrimitiveSet Set = SetGenerator.Generate(
        SetSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, 1, Vocabulary.Pack, Vocabulary.Hash, [new SetRequest(1, 1), new SetRequest(HotBody, 1), new SetRequest(ColdBody, 1), new SetRequest(4, 1)], 4),
        Vocabulary,
        Registry);

    /// <summary>
    /// A grammar whose bands demand the zones <paramref name="zones"/> names, one per band, and whose
    /// moons are optional and may or may not inherit their planet's zone. A moon that must exist would
    /// need a moon of its own without end, which the grammar refuses outright.
    /// </summary>
    public static CompositionGrammar Grammar(bool moonsInherit, uint moons = 1, string[]? zones = null)
    {
        string[] bands = zones ?? ["zone-hot", "zone-cold"];
        return CompositionGrammar.Create(Registry, 3, 3, 32, 8,
            [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)])],
            [
                new Production(Star,
                [
                    new ConnectorRule((uint)bands.Length, (uint)bands.Length, Orbit, [new CategoryChoice(Planet, 1)],
                        SpacingRatio: Spacing,
                        BandScale: DerivationRules.OrbitScaleRevision,
                        BandTags: bands,
                        BandBase: Base),
                ]),
                new Production(Planet,
                [
                    new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
                    moons == 0
                        ? new ConnectorRule(0, 0, Orbit, [])
                        : new ConnectorRule(0, moons, Orbit, [new CategoryChoice(Planet, 1)], InheritsBandTag: moonsInherit),
                ]),
                new Production(Atmosphere, []),
            ]);
    }

    public static CompositionGraph Compose(bool moonsInherit, uint moons = 1, ulong seed = 1, string[]? zones = null)
    {
        CompositionGrammar grammar = Grammar(moonsInherit, moons, zones);
        GraphSpecification specification = GraphSpecification.Create(
            Registry.Revision, Registry.Hash, GeneratorVersion.Current, grammar.Version, grammar.Hash, seed, Set.Manifest.Pack, Set.Manifest.Hash, CompositionDomain.SolarSystem);

        return CompositionGenerator.Generate(specification, Set, grammar, Registry);
    }

    /// <summary>The first seed whose every planet drew a moon, so the inheritance rule has something to act on.</summary>
    public static CompositionGraph ComposeWithMoons(bool moonsInherit) =>
        Enumerable.Range(1, 60)
            .Select(seed => Compose(moonsInherit, moons: 1, (ulong)seed))
            .First(graph => graph.Root.Children[0].All(planet => planet.Children[1].Count == 1));

    /// <summary>The template a node's definition came from: 2 for the hot body, 3 for the cold one.</summary>
    public static uint TemplateOf(GraphNode node) => node.Definition.Provenance.TemplateId;

    private static TransformRange Orbit => new(TransformKind.OrbitalElements,
        [(PhysicalConstants.AstronomicalUnitMetres / 200, 400 * PhysicalConstants.AstronomicalUnitMetres), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0)]);

    private static PrimitiveTemplate Body(uint id, string label, string zone) =>
        PrimitiveTemplate.Create(Registry, id, label, Planet,
        [
            ParameterRange.Integer(EarthMass, EarthMass),
            ParameterRange.Integer(5_514, 5_514),
            ParameterRange.Integer(20_054, 20_054),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Integer(20 * 86_164, 20 * 86_164),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Enum(0),
        ],
        [ConnectorDeclaration.Empty, new ConnectorDeclaration([zone], [])]);
}
