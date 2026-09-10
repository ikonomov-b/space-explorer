using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Generation;

/// <summary>
/// A registry revision 3 vocabulary in miniature: a texture and a material for ice, the same for rock,
/// and two bodies that each require the material made for their own kind. The pool holds both materials
/// throughout, so anything but the tag rule of decision 0055 would mix them up.
/// </summary>
internal static class TaggedFixture
{
    public static readonly CategoryRegistry Registry = CategoryRegistryRevision3.Registry;

    public static readonly PackId TemplatePack = PackId.Parse("3000e5c0de00b0dd1e5000fa11ab1e00");

    /// <summary>Four of each material and body, over one texture apiece.</summary>
    public static readonly SetRequest[] Requests =
    [
        new SetRequest(1, 1), new SetRequest(2, 1), new SetRequest(3, 1), new SetRequest(4, 1),
        new SetRequest(5, 4), new SetRequest(6, 4),
    ];

    public static readonly TemplateVocabulary Vocabulary = Compose();

    public static readonly PrimitiveSet Set = Generate(Vocabulary, Requests);

    /// <summary>The vocabulary, with the tag the icy body requires of its material overridable for the failing case.</summary>
    public static TemplateVocabulary Compose(string icyBodyRequires = "for-icy") => TemplateVocabulary.Create(Registry, TemplatePack,
    [
        Texture(1, "ice-texture", "for-icy", "speckle"),
        Texture(2, "rock-texture", "for-rocky", "cells"),
        Material(3, "ice-surface", "for-icy"),
        Material(4, "rock-surface", "for-rocky"),
        Body(5, "icy-body", "icy", icyBodyRequires),
        Body(6, "rocky-body", "rocky", "for-rocky"),
    ]);

    public static PrimitiveSet Generate(TemplateVocabulary vocabulary, IReadOnlyList<SetRequest> requests) =>
        SetGenerator.Generate(
            SetSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, 7, TemplatePack, vocabulary.Hash, requests, 8),
            vocabulary,
            Registry);

    private static PrimitiveTemplate Texture(uint id, string label, string tag, string pattern) =>
        PrimitiveTemplate.Create(Registry, id, label, TextureRecipe,
        [
            ParameterRange.Enum((uint)Array.IndexOf(["flat", "noise", "stripes", "cells", "speckle"], pattern)),
            ParameterRange.Integer(65_536, 65_536),
            ParameterRange.Colour((10, 250), (10, 250), (10, 250), (255, 255)),
            ParameterRange.Colour((10, 250), (10, 250), (10, 250), (255, 255)),
            ParameterRange.Integer(0, 65_535),
        ],
        [],
        [tag]);

    private static PrimitiveTemplate Material(uint id, string label, string tag) =>
        PrimitiveTemplate.Create(Registry, id, label, SurfaceMaterial,
        [
            ParameterRange.Enum(0),
            ParameterRange.Colour((10, 250), (10, 250), (10, 250), (255, 255)),
            ParameterRange.Integer(0, 65_535),
            ParameterRange.Integer(0, 65_535),
            ParameterRange.Ref(tag),
        ],
        [],
        [tag]);

    private static PrimitiveTemplate Body(uint id, string label, string bodyType, string requires) =>
        PrimitiveTemplate.Create(Registry, id, label, Planet,
        [
            ParameterRange.Integer(100, 2_000),
            ParameterRange.Integer(1_000, 5_000),
            ParameterRange.Integer(3_000, 45_000),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Integer(720_000, 172_800_000),
            ParameterRange.BinaryTurn(0, 0),
            ParameterRange.Enum((uint)Array.IndexOf(["rocky", "icy", "ocean", "gas-giant"], bodyType)),
            ParameterRange.Ref(requires),
        ],
        [ConnectorDeclaration.Empty, ConnectorDeclaration.Empty]);
}
