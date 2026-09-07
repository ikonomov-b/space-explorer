using SpaceExplorer.Core.Registry;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class TemplateVocabularyTests
{
    [Fact]
    public void The_vocabulary_round_trips_and_keeps_its_hash_and_template_hashes()
    {
        TemplateVocabulary decoded = TemplateVocabulary.Decode(Fixture.Vocabulary.Bytes, Fixture.Registry);

        Assert.Equal(Fixture.Vocabulary.Hash, decoded.Hash);
        Assert.Equal(Fixture.Vocabulary.Templates.Select(t => t.Hash), decoded.Templates.Select(t => t.Hash));
    }

    [Fact]
    public void A_range_outside_the_schema_is_refused()
    {
        Assert.Throws<ArgumentException>(() => PrimitiveTemplate.Create(Fixture.Registry, 9, "wide", CategoryRegistryRevision1.Artifact,
            [ParameterRange.Enum(0), ParameterRange.Ref(), ParameterRange.Integer(1, 3)], []));
    }

    [Fact]
    public void Template_ids_must_ascend()
    {
        PrimitiveTemplate a = Fixture.Vocabulary.Templates[0];
        PrimitiveTemplate b = Fixture.Vocabulary.Templates[1];
        Assert.Throws<ArgumentException>(() => TemplateVocabulary.Create(Fixture.Registry, Fixture.TemplatePack, [b, a]));
    }
}
