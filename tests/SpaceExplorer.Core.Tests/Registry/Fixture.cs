using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Registry;

/// <summary>A small vocabulary over registry revision 1 that every registry and generation test shares.</summary>
internal static class Fixture
{
    public static readonly CategoryRegistry Registry = CategoryRegistryRevision1.Registry;

    /// <summary>An authored pack identifier drawn once outside the core, as decision 0020 requires; a literal here.</summary>
    public static readonly PackId TemplatePack = PackId.Parse("6f1d2c3b4a59687706f5e4d3c2b1a090");

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
        PrimitiveTemplate.Create(Registry, 4, "part", ArtifactPart,
        [ParameterRange.Ref(), ParameterRange.Ref(), ParameterRange.Integer(1, 200_000)],
        [new ConnectorDeclaration(["socket"], ["plug"])]),
        PrimitiveTemplate.Create(Registry, 5, "artifact", Artifact,
        [ParameterRange.Enum(0, 1, 2), ParameterRange.Ref(), ParameterRange.Integer(1, 2)], []),
        PrimitiveTemplate.Create(Registry, 6, "fixed-texture", TextureRecipe,
        [
            ParameterRange.Enum(0),
            ParameterRange.Integer(65, 65),
            ParameterRange.Colour((0, 0), (0, 0), (0, 0), (255, 255)),
            ParameterRange.Colour((0, 0), (0, 0), (0, 0), (255, 255)),
            ParameterRange.Integer(0, 0),
        ], []),
    ]);

    public static SetSpecification Specification(ulong seed, IReadOnlyList<SetRequest> requests, uint retries = 8) =>
        SetSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, SpaceExplorer.Core.Generation.SetGenerator.GrammarVersion, seed, TemplatePack, Vocabulary.Hash, requests, retries);

    public static readonly IReadOnlyList<SetRequest> StandardRequests =
        [new(1, 3), new(2, 3), new(3, 4), new(4, 4), new(5, 2)];
}
