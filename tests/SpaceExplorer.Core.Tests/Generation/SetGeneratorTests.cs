using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using SpaceExplorer.Core.Tests.Registry;
using Xunit;

namespace SpaceExplorer.Core.Tests.Generation;

public class SetGeneratorTests
{
    [Fact]
    public void The_same_specification_generates_the_same_set()
    {
        PrimitiveSet first = SetGenerator.Generate(Fixture.Specification(7, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);
        PrimitiveSet second = SetGenerator.Generate(Fixture.Specification(7, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);

        Assert.Equal(first.Manifest.Hash, second.Manifest.Hash);
        Assert.Equal(16, first.Definitions.Count);
    }

    [Fact]
    public void The_fixture_manifest_hash_is_frozen()
    {
        // Freezes this build's output for seed 7 so an unrecorded change to sampling order, the record
        // layouts, or the stream paths is caught; it is not an external anchor (decision 0042).
        PrimitiveSet set = SetGenerator.Generate(Fixture.Specification(7, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);
        Assert.Equal("704ecc02107fc478cd26048feb6f3943f4eb5e3681cebc49c994c3806a63677c", set.Manifest.Hash.ToString());
    }

    [Fact]
    public void Ids_run_1_to_n_in_generation_order_and_references_point_earlier()
    {
        PrimitiveSet set = SetGenerator.Generate(Fixture.Specification(7, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);

        for (int index = 0; index < set.Definitions.Count; index++)
        {
            PrimitiveDefinition definition = set.Definitions[index];
            Assert.Equal((uint)index + 1, definition.Id.LocalId);
            Assert.Equal(set.Manifest.Pack, definition.Id.Pack);
            Assert.All(definition.Dependencies, dependency => Assert.True(dependency.Id.LocalId < definition.Id.LocalId));
        }
    }

    [Fact]
    public void A_different_seed_generates_a_different_set()
    {
        PrimitiveSet a = SetGenerator.Generate(Fixture.Specification(7, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);
        PrimitiveSet b = SetGenerator.Generate(Fixture.Specification(8, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);

        Assert.NotEqual(a.Manifest.Pack, b.Manifest.Pack);
        Assert.NotEqual(a.Manifest.Hash, b.Manifest.Hash);
    }

    [Fact]
    public void Exhausted_variation_fails_explicitly_within_the_retry_budget()
    {
        // Template 6 fixes every parameter, so a second definition can only be a duplicate.
        var exception = Assert.Throws<GenerationException>(() =>
            SetGenerator.Generate(Fixture.Specification(1, [new SetRequest(6, 2)], retries: 3), Fixture.Vocabulary, Fixture.Registry));

        Assert.Contains("exhausted", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reference_with_no_earlier_definition_fails_explicitly()
    {
        Assert.Throws<GenerationException>(() =>
            SetGenerator.Generate(Fixture.Specification(1, [new SetRequest(2, 1)]), Fixture.Vocabulary, Fixture.Registry));
    }

    [Fact]
    public void A_specification_pinning_another_registry_is_refused()
    {
        SetSpecification foreign = SetSpecification.Create(1, ContentHash.Of("other"u8), GeneratorVersion.Current, SetGenerator.GrammarVersion, 1, Fixture.TemplatePack, Fixture.Vocabulary.Hash, Fixture.StandardRequests, 8);
        Assert.Throws<ArgumentException>(() => SetGenerator.Generate(foreign, Fixture.Vocabulary, Fixture.Registry));
    }

    [Fact]
    public void Inclusive_sampling_stays_inside_the_range_across_spans()
    {
        Pcg32 stream = RandomStream.Derive(3, "test/sampling");
        for (int draw = 0; draw < 1_000; draw++)
        {
            long small = ParameterSampler.SampleInclusive(stream, -3, 3);
            Assert.InRange(small, -3, 3);
            long wide = ParameterSampler.SampleInclusive(stream, long.MinValue / 2, long.MaxValue / 2);
            Assert.InRange(wide, long.MinValue / 2, long.MaxValue / 2);
        }

        Assert.Equal(5, ParameterSampler.SampleInclusive(stream, 5, 5));
    }
}
