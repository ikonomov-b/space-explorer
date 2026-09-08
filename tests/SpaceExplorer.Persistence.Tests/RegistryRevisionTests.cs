using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Persistence.Tests;

/// <summary>
/// The loader accepts every category-registry revision the build supports and refuses the rest by name,
/// never by substituting a schema (decision 0035). This is M0a exit criterion 10 exercised with a
/// revision 2 that expands revision 1, published beside it in one data root.
/// </summary>
public sealed class RegistryRevisionTests : IDisposable
{
    private static readonly CategoryRegistry Revision1 = CategoryRegistryRevision1.Registry;

    /// <summary>What an expansion looks like: every revision 1 category unchanged, plus one more.</summary>
    private static readonly CategoryRegistry Revision2 = ExpandedRevision2("beacon");

    private static readonly PackId TemplatePack = PackId.Parse("0f1e2d3c4b5a69788796a5b4c3d2e1f0");

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "space-explorer-tests", Guid.NewGuid().ToString("n"));
    private readonly DataRoot _root;

    public RegistryRevisionTests() => _root = DataRoot.At(_directory);

    [Fact]
    public void Packs_of_two_supported_revisions_load_side_by_side_from_one_root()
    {
        PrimitiveSet first = Generate(Revision1);
        PrimitiveSet second = Generate(Revision2);
        SetPublisher.Publish(_root, first);
        SetPublisher.Publish(_root, second);

        // The same templates under another revision are laid out under another domain label, so the two
        // runs are different specifications and different packs (decision 0035).
        Assert.NotEqual(first.Manifest.Pack, second.Manifest.Pack);

        CategoryRegistries supported = CategoryRegistries.Of(Revision1, Revision2);
        PrimitiveSet loadedFirst = SetLoader.Load(_root, first.Manifest.Pack, supported);
        PrimitiveSet loadedSecond = SetLoader.Load(_root, second.Manifest.Pack, supported);

        Assert.Equal(1u, loadedFirst.Manifest.RegistryRevision);
        Assert.Equal(2u, loadedSecond.Manifest.RegistryRevision);
        Assert.Equal(Revision1.Hash, loadedFirst.Manifest.RegistryHash);
        Assert.Equal(Revision2.Hash, loadedSecond.Manifest.RegistryHash);
        Assert.Equal(first.Definitions.Select(definition => definition.Hash), loadedFirst.Definitions.Select(definition => definition.Hash));
        Assert.Equal(second.Definitions.Select(definition => definition.Hash), loadedSecond.Definitions.Select(definition => definition.Hash));
    }

    [Fact]
    public void A_pack_of_an_unsupported_revision_is_refused_by_name()
    {
        PrimitiveSet set = Generate(Revision2);
        SetPublisher.Publish(_root, set);

        var exception = Assert.Throws<CompatibilityException>(() => SetLoader.Load(_root, set.Manifest.Pack, CategoryRegistries.Of(Revision1)));
        Assert.Contains("revision 2", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_pack_pinning_a_supported_revision_with_another_hash_is_refused()
    {
        PrimitiveSet set = Generate(Revision2);
        SetPublisher.Publish(_root, set);

        // Revision 2 is immutable, so a differing record under that number is a different build's
        // registry, not a revision this one can decode against (decision 0035).
        CategoryRegistries divergent = CategoryRegistries.Of(Revision1, ExpandedRevision2("landmark"));

        var exception = Assert.Throws<CompatibilityException>(() => SetLoader.Load(_root, set.Manifest.Pack, divergent));
        Assert.Contains(set.Manifest.RegistryHash.ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void One_revision_cannot_be_listed_twice()
    {
        Assert.Throws<ArgumentException>(() => CategoryRegistries.Of(Revision2, ExpandedRevision2("landmark")));
    }

    private static CategoryRegistry ExpandedRevision2(string addedLabel) => CategoryRegistry.Create(2,
    [
        .. Revision1.Categories,
        new CategoryDefinition(1_000, addedLabel, [CompositionDomain.Site], [ParameterDescriptor.Integer("range", 1, 4_096)], [], StoragePolicies.Materialize, 0, []),
    ]);

    private static PrimitiveSet Generate(CategoryRegistry registry)
    {
        TemplateVocabulary vocabulary = TemplateVocabulary.Create(registry, TemplatePack,
        [
            PrimitiveTemplate.Create(registry, 1, "texture", TextureRecipe,
            [
                ParameterRange.Enum(0, 1, 2),
                ParameterRange.Integer(65, 4_096),
                ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)),
                ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)),
                ParameterRange.Integer(0, 65_535),
            ], []),
        ]);

        return SetGenerator.Generate(
            SetSpecification.Create(registry.Revision, registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, 11, TemplatePack, vocabulary.Hash, [new SetRequest(1, 3)], 8),
            vocabulary,
            registry);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
