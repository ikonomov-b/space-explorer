using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Generation;

/// <summary>
/// A set wide enough to compose from: the solar-system categories, and two artifact-part templates whose
/// connector tags differ, so a rule that requires a tag admits one and refuses the other. It is separate
/// from <see cref="Registry.Fixture"/> because that vocabulary's hash is frozen by a recorded manifest.
/// </summary>
internal static class CompositionFixture
{
    public const uint PlugPart = 4;
    public const uint SocketPart = 5;

    public static readonly CategoryRegistry Registry = CategoryRegistryRevision1.Registry;
    public static readonly CompositionGrammar Grammar = CompositionGrammarVersion1.Grammar;

    /// <summary>An authored pack identifier drawn once outside the core, as decision 0020 requires.</summary>
    public static readonly PackId TemplatePack = PackId.Parse("b3a1c05d47e2986f1d0c4b7a35e69182");

    public static readonly TemplateVocabulary Vocabulary = TemplateVocabulary.Create(Registry, TemplatePack,
    [
        PrimitiveTemplate.Create(Registry, 1, "texture", TextureRecipe,
        [
            ParameterRange.Enum(0, 1, 2, 3, 4),
            ParameterRange.Integer(65, 4L << ArtifactFractionBits),
            ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)),
            ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)),
            ParameterRange.Integer(0, 65_535),
        ], []),

        PrimitiveTemplate.Create(Registry, 2, "material", SurfaceMaterial,
        [
            ParameterRange.Enum(0, 1, 2),
            ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)),
            ParameterRange.Integer(0, 65_535),
            ParameterRange.Integer(0, 65_535),
            ParameterRange.Ref(),
        ], []),

        PrimitiveTemplate.Create(Registry, 3, "geometry", GeometryRecipe,
        [
            ParameterRange.Enum(0, 1, 2, 3, 4, 5),
            ParameterRange.Vector3((655, 1 << ArtifactFractionBits), (655, 1 << ArtifactFractionBits), (655, 1 << ArtifactFractionBits)),
            ParameterRange.Integer(3, 64),
            ParameterRange.Integer(0, 6_553),
        ], []),

        // Provides the tag its own attach connector requires, so one plug part attaches to another.
        Part(PlugPart, "plug-part", new ConnectorDeclaration(["plug"], ["plug"])),

        // Requires the same tag but provides another, so it may root an artifact and never hangs from one.
        Part(SocketPart, "socket-part", new ConnectorDeclaration(["socket"], ["plug"])),

        PrimitiveTemplate.Create(Registry, 6, "star", Star,
        [
            ParameterRange.Integer(1_600_000_000L, 60_000_000_000L),
            ParameterRange.Integer(25_600_000_000L, 512_000_000_000L),
            ParameterRange.Integer(8_000L, 60_000_000L),
            ParameterRange.Integer(2_800L, 10_000L),
            ParameterRange.Enum(2, 3, 4, 5, 6),
        ], [ConnectorDeclaration.Empty]),

        PrimitiveTemplate.Create(Registry, 7, "planet", Planet,
        [
            ParameterRange.Integer(100L, 200_000L),
            ParameterRange.Integer(MinimumLandableRadius, 2_560_000_000L),
            ParameterRange.Integer(3_000L, 45_000L),
            ParameterRange.BinaryTurn(int.MinValue, int.MaxValue),
            ParameterRange.BinaryTurn(-(1 << 30), 1 << 30),
            ParameterRange.Integer(720_000L, 172_800_000L),
            ParameterRange.BinaryTurn(int.MinValue, int.MaxValue),
            ParameterRange.Enum(0, 1, 2),
        ], [ConnectorDeclaration.Empty, ConnectorDeclaration.Empty]),

        PrimitiveTemplate.Create(Registry, 8, "atmosphere", Atmosphere,
        [
            ParameterRange.Enum(0, 1, 2),
            ParameterRange.Integer(0L, 5_000_000L),
            ParameterRange.Enum(0, 1, 2, 3, 4),
            ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)),
        ], []),

        // No parameters, so the set holds exactly one barycentre however many are requested.
        PrimitiveTemplate.Create(Registry, 9, "barycentre", Barycentre, [], [ConnectorDeclaration.Empty]),
    ]);

    public static readonly IReadOnlyList<SetRequest> Requests =
        [new(1, 2), new(2, 2), new(3, 2), new(PlugPart, 3), new(SocketPart, 2), new(6, 2), new(7, 4), new(8, 2), new(9, 1)];

    public static readonly PrimitiveSet Set = Generate(Requests);

    /// <summary>A set of the requested templates only, for the cases where a category is deliberately absent.</summary>
    public static PrimitiveSet Generate(IReadOnlyList<SetRequest> requests) =>
        SetGenerator.Generate(
            SetSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, 5, TemplatePack, Vocabulary.Hash, requests, 8),
            Vocabulary,
            Registry);

    public static GraphSpecification Specification(ulong seed, CompositionDomain domain, PrimitiveSet? set = null, CompositionGrammar? grammar = null)
    {
        PrimitiveSet source = set ?? Set;
        CompositionGrammar rules = grammar ?? Grammar;
        return GraphSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, rules.Version, rules.Hash, seed, source.Manifest.Pack, source.Manifest.Hash, domain);
    }

    /// <summary>The template a definition came from, which names it in an assertion.</summary>
    public static uint TemplateOf(GraphNode node) => node.Definition.Provenance.TemplateId;

    private static PrimitiveTemplate Part(uint id, string label, ConnectorDeclaration attach) =>
        PrimitiveTemplate.Create(Registry, id, label, ArtifactPart,
            [ParameterRange.Ref(), ParameterRange.Ref(), ParameterRange.Integer(50, 20_000)],
            [attach]);
}
