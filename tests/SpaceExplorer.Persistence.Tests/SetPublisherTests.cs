using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Persistence.Tests;

public sealed class SetPublisherTests : IDisposable
{
    private static readonly CategoryRegistry Registry = CategoryRegistryRevision1.Registry;
    private static readonly CategoryRegistries Registries = CategoryRegistries.Supported;
    private static readonly PackId TemplatePack = PackId.Parse("00112233445566778899aabbccddeeff");

    private static readonly TemplateVocabulary Vocabulary = TemplateVocabulary.Create(Registry, TemplatePack,
    [
        PrimitiveTemplate.Create(Registry, 1, "texture", TextureRecipe,
            [ParameterRange.Enum(0, 1, 2), ParameterRange.Integer(65, 4096), ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)), ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)), ParameterRange.Integer(0, 65_535)], []),
        PrimitiveTemplate.Create(Registry, 2, "material", SurfaceMaterial,
            [ParameterRange.Enum(0, 1), ParameterRange.Colour((0, 255), (0, 255), (0, 255), (255, 255)), ParameterRange.Integer(0, 65_535), ParameterRange.Integer(0, 65_535), ParameterRange.Ref()], []),
    ]);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "space-explorer-tests", Guid.NewGuid().ToString("n"));
    private readonly DataRoot _root;

    public SetPublisherTests() => _root = DataRoot.At(_directory);

    private static PrimitiveSet Generate(ulong seed) =>
        SetGenerator.Generate(
            SetSpecification.Create(Registry.Revision, Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, seed, TemplatePack, Vocabulary.Hash, [new SetRequest(1, 3), new SetRequest(2, 3)], 8),
            Vocabulary,
            Registry);

    [Fact]
    public void A_published_set_reloads_without_generation_and_matches()
    {
        PrimitiveSet set = Generate(1);
        PublishResult result = SetPublisher.Publish(_root, set);
        PrimitiveSet loaded = SetLoader.Load(_root, set.Manifest.Pack, Registries);

        Assert.False(result.AlreadyPublished);
        Assert.Equal(7, result.RecordsWritten);
        Assert.Equal(set.Manifest.Hash, loaded.Manifest.Hash);
        Assert.Equal(set.Definitions.Select(d => d.Hash), loaded.Definitions.Select(d => d.Hash));
        Assert.True(File.Exists(_root.RecordPath(set.Manifest.Pack, set.Manifest.Hash)));
        Assert.Single(SetLoader.List(_root));
    }

    [Fact]
    public void Publishing_the_same_set_again_writes_nothing()
    {
        PrimitiveSet set = Generate(1);
        SetPublisher.Publish(_root, set);
        PublishResult again = SetPublisher.Publish(_root, set);

        Assert.True(again.AlreadyPublished);
        Assert.Equal(0, again.RecordsWritten);
    }

    [Fact]
    public void A_differing_manifest_under_an_existing_pack_is_refused_before_anything_is_written()
    {
        PrimitiveSet set = Generate(1);
        SetPublisher.Publish(_root, set);

        // Same specification, hence same pack, but one definition fewer: what a non-deterministic generator would produce.
        SetManifest m = set.Manifest;
        SetManifest shorter = SetManifest.Create(m.SpecificationHash, m.Pack, m.RegistryRevision, m.RegistryHash, m.GeneratorVersion, m.GrammarVersion, m.TemplatePack, m.VocabularyHash, [.. m.Definitions.Take(5)], [], [], m.Validation);
        PrimitiveSet differing = PrimitiveSet.Create(shorter, [.. set.Definitions.Take(5)]);

        var exception = Assert.Throws<ReproductionFailureException>(() => SetPublisher.Publish(_root, differing));
        Assert.Equal(m.Hash, exception.Existing);
        Assert.Equal(shorter.Hash, exception.Attempted);
        Assert.False(File.Exists(_root.RecordPath(m.Pack, shorter.Hash)));
    }

    [Fact]
    public void A_corrupt_record_fails_explicitly_on_load()
    {
        PrimitiveSet set = Generate(1);
        SetPublisher.Publish(_root, set);
        string path = _root.RecordPath(set.Manifest.Pack, set.Definitions[2].Hash);
        byte[] bytes = File.ReadAllBytes(path);
        bytes[^1] ^= 0x01;
        File.WriteAllBytes(path, bytes);

        Assert.Throws<PackageIntegrityException>(() => SetLoader.Load(_root, set.Manifest.Pack, Registries));
    }

    [Fact]
    public void A_missing_record_fails_explicitly_on_load()
    {
        PrimitiveSet set = Generate(1);
        SetPublisher.Publish(_root, set);
        File.Delete(_root.RecordPath(set.Manifest.Pack, set.Definitions[0].Hash));

        Assert.Throws<PackageIntegrityException>(() => SetLoader.Load(_root, set.Manifest.Pack, Registries));
    }

    [Fact]
    public void An_interrupted_publication_leaves_only_temporary_files_and_the_next_one_completes()
    {
        PrimitiveSet set = Generate(1);
        string directory = _root.PackDirectory(set.Manifest.Pack);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "abandoned.bin.tmp"), [1, 2, 3]);

        SetPublisher.Publish(_root, set);

        Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));
        Assert.Equal(set.Manifest.Hash, SetLoader.Load(_root, set.Manifest.Pack, Registries).Manifest.Hash);
    }

    [Fact]
    public void An_unknown_pack_and_an_unsupported_registry_fail_explicitly()
    {
        PrimitiveSet set = Generate(1);
        Assert.Throws<PackageNotFoundException>(() => SetLoader.Load(_root, set.Manifest.Pack, Registries));

        SetPublisher.Publish(_root, set);
        CategoryRegistries other = CategoryRegistries.Of(CategoryRegistry.Create(2, [.. Registry.Categories]));
        Assert.Throws<CompatibilityException>(() => SetLoader.Load(_root, set.Manifest.Pack, other));
    }

    [Fact]
    public void The_data_root_honours_the_override_variable()
    {
        string? previous = Environment.GetEnvironmentVariable(DataRoot.OverrideVariable);
        try
        {
            Environment.SetEnvironmentVariable(DataRoot.OverrideVariable, _directory);
            Assert.Equal(Path.GetFullPath(_directory), DataRoot.Resolve().Path);
        }
        finally
        {
            Environment.SetEnvironmentVariable(DataRoot.OverrideVariable, previous);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

public sealed class RecordVerificationTests
{
    [Fact]
    public void A_record_file_that_does_not_hash_to_its_name_is_refused_before_decoding()
    {
        // Pinned on its own because every later check (canonical form, manifest hash, pack identity)
        // would also catch a corrupt file; this one must fire first and without decoding anything.
        string path = Path.Combine(Path.GetTempPath(), "space-explorer-tests", Guid.NewGuid().ToString("n") + ".bin");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            byte[] content = [1, 2, 3, 4];
            File.WriteAllBytes(path, content);

            Assert.Equal(content, SetLoader.ReadVerified(path, ContentHash.Of(content)));
            Assert.Throws<PackageIntegrityException>(() => SetLoader.ReadVerified(path, ContentHash.Of("other"u8)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
