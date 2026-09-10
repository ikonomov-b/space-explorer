using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Generation;

/// <summary>The generator could not satisfy a specification within its bounds; nothing is published.</summary>
public sealed class GenerationException(string message) : Exception(message);

/// <summary>
/// Generates one primitive set from a specification: for each request, in order, draws definitions
/// from the template's ranges on a stream derived from the seed and the instance path, rejects
/// duplicates within the retry budget, allocates IDs 1..n in generation order, and returns the set
/// with its manifest (decisions 0006, 0008, 0021, 0031, 0033).
/// </summary>
public static class SetGenerator
{
    /// <summary>The composition grammar version this generator implements; pinned by every specification and manifest.</summary>
    public const uint GrammarVersion = CompositionGrammar.CurrentVersion;

    /// <summary>Generates the set <paramref name="specification"/> describes from <paramref name="vocabulary"/> under <paramref name="registry"/>.</summary>
    /// <exception cref="ArgumentException">The specification does not pin this registry, vocabulary, generator, or grammar.</exception>
    /// <exception cref="GenerationException">A request names an unknown template, needs a reference no earlier definition supplies, or exhausts its retry budget.</exception>
    public static PrimitiveSet Generate(SetSpecification specification, TemplateVocabulary vocabulary, CategoryRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(registry);

        if (specification.RegistryRevision != registry.Revision || specification.RegistryHash != registry.Hash)
        {
            throw new ArgumentException("The specification pins another category-registry revision or hash (decision 0035).", nameof(specification));
        }

        if (specification.TemplatePack != vocabulary.Pack || specification.VocabularyHash != vocabulary.Hash)
        {
            throw new ArgumentException("The specification pins another template vocabulary.", nameof(specification));
        }

        if (specification.GeneratorVersion != GeneratorVersion.Current || specification.GrammarVersion != GrammarVersion)
        {
            throw new ArgumentException($"The specification pins generator {specification.GeneratorVersion} and grammar {specification.GrammarVersion}; this build is generator {GeneratorVersion.Current} and grammar {GrammarVersion}.", nameof(specification));
        }

        PackId pack = specification.PackId;
        var definitions = new List<PrimitiveDefinition>();
        var hashes = new HashSet<ContentHash>();
        var poolsByCategory = new Dictionary<uint, List<PrimitiveDefinition>>();
        uint tried = 0;
        uint rejected = 0;

        foreach (SetRequest request in specification.Requests)
        {
            PrimitiveTemplate template = vocabulary.TryFind(request.TemplateId)
                ?? throw new GenerationException($"The specification requests template {request.TemplateId}, which the vocabulary does not hold.");
            CategoryDefinition schema = registry.Find(template.Category);
            var provenance = new Provenance(ProvenanceKind.Generated, vocabulary.Pack, template.Id, template.Hash);

            for (uint instance = 0; instance < request.Count; instance++)
            {
                Pcg32 stream = RandomStream.Derive(specification.Seed, $"set/template/{template.Id}/instance/{instance}");
                PrimitiveDefinition? accepted = null;

                for (uint attempt = 0; attempt < specification.RetryBudget && accepted is null; attempt++)
                {
                    tried++;
                    var id = new PrimitiveId(pack, (uint)definitions.Count + 1);
                    var values = new ParameterValue[schema.Parameters.Count];
                    for (int index = 0; index < values.Length; index++)
                    {
                        ParameterDescriptor descriptor = schema.Parameters[index];
                        IReadOnlyList<PrimitiveRevisionRef> pool = descriptor.Kind == ParameterKind.PrimitiveRef
                            ? Candidates(poolsByCategory, descriptor.RefCategory, template.Ranges[index].RequiredTags)
                            : [];
                        try
                        {
                            values[index] = ParameterSampler.Sample(stream, template.Ranges[index], descriptor, pool);
                        }
                        catch (InvalidOperationException exception)
                        {
                            string suits = template.Ranges[index].RequiredTags.Count == 0
                                ? string.Empty
                                : $" carrying every tag of '{string.Join(", ", template.Ranges[index].RequiredTags)}'";
                            throw new GenerationException($"Template '{template.Label}': {exception.Message}{suits}");
                        }
                    }

                    PrimitiveDefinition candidate = PrimitiveDefinition.Create(registry, id, template.Category, provenance, values, template.Connectors, string.Empty, template.Tags);
                    if (hashes.Add(candidate.Hash))
                    {
                        accepted = candidate;
                    }
                    else
                    {
                        rejected++;
                    }
                }

                if (accepted is null)
                {
                    throw new GenerationException($"Template '{template.Label}' instance {instance} produced only duplicate definitions within {specification.RetryBudget} attempts: the template's variation is exhausted.");
                }

                definitions.Add(accepted);
                Pool(poolsByCategory, template.Category).Add(accepted);
            }
        }

        SetManifest manifest = SetManifest.Create(
            specification.Hash,
            pack,
            registry.Revision,
            registry.Hash,
            specification.GeneratorVersion,
            specification.GrammarVersion,
            vocabulary.Pack,
            vocabulary.Hash,
            definitions.Select(definition => new ManifestEntry(definition.Id.LocalId, definition.Category, definition.Hash)).ToList(),
            [],
            [],
            new ValidationResult(true, tried, rejected));

        return PrimitiveSet.Create(manifest, definitions);
    }

    /// <summary>
    /// The definitions a reference may draw: those already allocated of <paramref name="category"/> that
    /// carry every tag in <paramref name="requiredTags"/>, in allocation order. A reference to a category
    /// with no tag requirement draws from all of them, which is what every revision before 3 does
    /// (decision 0055).
    /// </summary>
    private static IReadOnlyList<PrimitiveRevisionRef> Candidates(Dictionary<uint, List<PrimitiveDefinition>> pools, uint category, IReadOnlyList<string> requiredTags) =>
        [.. Pool(pools, category).Where(candidate => TagList.Satisfies(candidate.Tags, requiredTags)).Select(candidate => candidate.Reference)];

    private static List<PrimitiveDefinition> Pool(Dictionary<uint, List<PrimitiveDefinition>> pools, uint category)
    {
        if (!pools.TryGetValue(category, out List<PrimitiveDefinition>? pool))
        {
            pool = [];
            pools[category] = pool;
        }

        return pool;
    }
}
