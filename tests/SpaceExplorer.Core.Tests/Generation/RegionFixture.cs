using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision5;

namespace SpaceExplorer.Core.Tests.Generation;

/// <summary>
/// Three bodies against one gate: one large and solid, one solid but below the minimum landable radius,
/// and one as large as the first that declares no ground of its own. Nothing else separates them — the
/// same star, the same atmosphere, the same material, and a rule that demands exactly one region of every
/// parent it is offered on — so what each ends up with is decided by the gate alone (decisions 0041, 0061).
/// </summary>
internal static class RegionFixture
{
    public const uint LargeBody = 4;
    public const uint SmallBody = 5;

    /// <summary>A body as large as the large one and made of nothing to stand on: a gas giant in miniature.</summary>
    public const uint GiantBody = 6;

    public static readonly CategoryRegistry Registry = CategoryRegistryRevision5.Registry;

    /// <summary>Earth's own mass and density, whose derived radius is far above the minimum.</summary>
    public const long LargeMass = EarthMass;

    public const long LargeDensity = 5_514;

    /// <summary>
    /// A body of 20 x 10^20 kg at 5,000 kg/m^3, whose derived radius is about 457 km against the 524 km
    /// minimum: the largest body this vocabulary offers that the gate must still refuse.
    /// </summary>
    public const long SmallMass = 20;

    public const long SmallDensity = 5_000;

    private static readonly TemplateVocabulary Vocabulary = TemplateVocabulary.Create(Registry, PackId.Parse("5000e5c0de00b0dd1e5000fa11ab1e00"),
    [
        PrimitiveTemplate.Create(Registry, 1, "ground-texture", TextureRecipe,
        [
            ParameterRange.Enum(1),
            ParameterRange.Integer(1L << ArtifactFractionBits, 1L << ArtifactFractionBits),
            ParameterRange.Colour((90, 90), (70, 70), (60, 60), (255, 255)),
            ParameterRange.Colour((160, 160), (130, 130), (110, 110), (255, 255)),
            ParameterRange.Integer(24_576, 24_576),
        ], [], ["for-rocky"]),

        PrimitiveTemplate.Create(Registry, 2, "ground-surface", SurfaceMaterial,
        [
            ParameterRange.Enum(0),
            ParameterRange.Colour((110, 110), (90, 90), (74, 74), (255, 255)),
            ParameterRange.Integer(50_000, 50_000),
            ParameterRange.Integer(0, 0),
            ParameterRange.Ref("for-rocky"),
        ], [], ["for-rocky"]),

        PrimitiveTemplate.Create(Registry, 3, "sun", Star,
        [
            ParameterRange.Integer(SolarMass, SolarMass),
            ParameterRange.Integer(695_700_000L << PlanetFractionBits, 695_700_000L << PlanetFractionBits),
            ParameterRange.Integer(5_772, 5_772),
        ], [ConnectorDeclaration.Empty]),

        Body(LargeBody, "large-body", LargeMass, LargeDensity),
        Body(SmallBody, "small-body", SmallMass, SmallDensity),
        Body(GiantBody, "giant-body", LargeMass, LargeDensity, solid: false),

        PrimitiveTemplate.Create(Registry, 7, "air", Atmosphere,
        [
            ParameterRange.Enum(1),
            ParameterRange.Integer(101_325, 101_325),
            ParameterRange.Enum(1),
            ParameterRange.Colour((120, 120), (150, 150), (220, 220), (255, 255)),
        ], []),

        PrimitiveTemplate.Create(Registry, Region, "landing-region", Region,
        [ParameterRange.Integer(MaximumExtentMetres, MaximumExtentMetres)], []),
    ]);

    public static readonly PrimitiveSet Set = SetGenerator.Generate(
        SetSpecification.Create(
            Registry.Revision,
            Registry.Hash,
            GeneratorVersion.Current,
            SetGenerator.GrammarVersion,
            1,
            Vocabulary.Pack,
            Vocabulary.Hash,
            [new SetRequest(1, 1), new SetRequest(2, 1), new SetRequest(3, 1), new SetRequest(LargeBody, 1), new SetRequest(SmallBody, 1), new SetRequest(GiantBody, 1), new SetRequest(7, 1), new SetRequest(Region, 1)],
            4),
        Vocabulary,
        Registry);

    /// <summary>
    /// One star with one planet of <paramref name="body"/>, whose surface anchor is gated unless
    /// <paramref name="gate"/> is empty — the mutation the gate's own test removes.
    /// </summary>
    public static CompositionGraph Compose(uint body, string gate = ConnectorGates.LandableBodyRevision)
    {
        var grammar = CompositionGrammar.Create(Registry, CompositionGrammarVersion6.Version, 3, 32, 8,
            [new RootRule(CompositionDomain.SolarSystem, [new CategoryChoice(Star, 1)])],
            [
                new Production(Star,
                [
                    new ConnectorRule(1, 1, Orbit, [new CategoryChoice(Planet, 1)]),
                ]),
                new Production(Planet,
                [
                    new ConnectorRule(1, 1, TransformRange.Origin(TransformKind.Rigid), [new CategoryChoice(Atmosphere, 1)]),
                    new ConnectorRule(0, 0, Orbit, []),
                    new ConnectorRule(1, 1, CompositionGrammarVersion6.SurfaceAnchor.Transform, [new CategoryChoice(Region, 1)], Gate: gate),
                ]),
                new Production(Atmosphere, []),
                new Production(Region, []),
            ]);

        PrimitiveSet source = OneBody(body);
        GraphSpecification specification = GraphSpecification.Create(
            Registry.Revision, Registry.Hash, GeneratorVersion.Current, grammar.Version, grammar.Hash, 1, source.Manifest.Pack, source.Manifest.Hash, CompositionDomain.SolarSystem);

        return CompositionGenerator.Generate(specification, source, grammar, Registry);
    }

    /// <summary>The set with only one of the two bodies in it, so the grammar's one planet is the body under test.</summary>
    private static PrimitiveSet OneBody(uint template) =>
        SetGenerator.Generate(
            SetSpecification.Create(
                Registry.Revision,
                Registry.Hash,
                GeneratorVersion.Current,
                SetGenerator.GrammarVersion,
                1,
                Vocabulary.Pack,
                Vocabulary.Hash,
                [new SetRequest(1, 1), new SetRequest(2, 1), new SetRequest(3, 1), new SetRequest(template, 1), new SetRequest(7, 1), new SetRequest(Region, 1)],
                4),
            Vocabulary,
            Registry);

    /// <summary>The regions a composed graph holds, at any depth.</summary>
    public static IReadOnlyList<GraphNode> Regions(CompositionGraph graph) =>
        [.. Walk(graph.Root).Where(node => node.Definition.Category == Region)];

    private static IEnumerable<GraphNode> Walk(GraphNode node) =>
        node.Children.SelectMany(connector => connector).SelectMany(Walk).Prepend(node);

    private static TransformRange Orbit => new(TransformKind.OrbitalElements,
        [(PhysicalConstants.AstronomicalUnitMetres, PhysicalConstants.AstronomicalUnitMetres), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0)]);

    private static PrimitiveTemplate Body(uint id, string label, long mass, long density, bool solid = true) =>
        PrimitiveTemplate.Create(Registry, id, label, Planet,
        [
            ParameterRange.Integer(mass, mass),
            ParameterRange.Integer(density, density),
            ParameterRange.Integer(20_054, 20_054),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Integer(20L * 86_164L, 20L * 86_164L),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Enum(0),
            ParameterRange.Ref("for-rocky"),
        ],
        [ConnectorDeclaration.Empty, ConnectorDeclaration.Empty, ConnectorDeclaration.Empty],
        solid ? [ConnectorGates.SolidSurfaceTag] : []);
}
