using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// One category of the registry: a stable numeric ID with zero reserved, a label, the domains its
/// primitives may join, the ordered parameter schema, the connector kinds, the permitted storage
/// policies, the derived-instance budget, and the generator revision identifiers that may produce it
/// (decisions 0031, 0034, 0035, 0038).
/// </summary>
public sealed record CategoryDefinition
{
    public const int MaxParameters = 256;
    public const int MaxConnectors = 64;
    public const int MaxDomains = 7;
    public const int MaxGeneratorRevisions = 64;

    public CategoryDefinition(
        uint id,
        string label,
        IReadOnlyList<CompositionDomain> domains,
        IReadOnlyList<ParameterDescriptor> parameters,
        IReadOnlyList<ConnectorKind> connectors,
        StoragePolicies permittedPolicies,
        uint maxDerivedInstances,
        IReadOnlyList<string> generatorRevisions)
    {
        StreamPath.Validate(label);
        ArgumentNullException.ThrowIfNull(domains);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(connectors);
        ArgumentNullException.ThrowIfNull(generatorRevisions);

        if (id == 0)
        {
            throw new ArgumentException($"Category '{label}' cannot take the reserved ID zero.", nameof(id));
        }

        if (domains.Count == 0 || domains.Count > MaxDomains)
        {
            throw new ArgumentException($"Category '{label}' must join 1 to {MaxDomains} domains.", nameof(domains));
        }

        var seenDomains = new HashSet<CompositionDomain>();
        foreach (CompositionDomain domain in domains)
        {
            if (!Enum.IsDefined(domain) || !seenDomains.Add(domain))
            {
                throw new ArgumentException($"Category '{label}' has an unknown or repeated domain {(byte)domain}.", nameof(domains));
            }
        }

        if (parameters.Count > MaxParameters)
        {
            throw new ArgumentException($"Category '{label}' declares {parameters.Count} parameters; the bound is {MaxParameters}.", nameof(parameters));
        }

        RejectRepeatedLabels(label, parameters.Select(parameter => parameter.Label), "parameter");

        if (connectors.Count > MaxConnectors)
        {
            throw new ArgumentException($"Category '{label}' declares {connectors.Count} connectors; the bound is {MaxConnectors}.", nameof(connectors));
        }

        RejectRepeatedLabels(label, connectors.Select(connector => connector.Label), "connector");

        const StoragePolicies known = StoragePolicies.Regenerate | StoragePolicies.Materialize | StoragePolicies.Hybrid;
        if (permittedPolicies == StoragePolicies.None || (permittedPolicies & ~known) != 0)
        {
            throw new ArgumentException($"Category '{label}' must permit at least one known storage policy.", nameof(permittedPolicies));
        }

        if (generatorRevisions.Count > MaxGeneratorRevisions)
        {
            throw new ArgumentException($"Category '{label}' lists {generatorRevisions.Count} generator revisions; the bound is {MaxGeneratorRevisions}.", nameof(generatorRevisions));
        }

        foreach (string revision in generatorRevisions)
        {
            GeneratorRevisionIdentifier.Validate(revision);
        }

        RejectRepeatedLabels(label, generatorRevisions, "generator revision");

        bool needsGenerator = (permittedPolicies & (StoragePolicies.Regenerate | StoragePolicies.Hybrid)) != 0;
        if (needsGenerator != generatorRevisions.Count > 0)
        {
            throw new ArgumentException(
                $"Category '{label}' lists generator revisions exactly when it permits regenerate or hybrid storage (decision 0035).",
                nameof(generatorRevisions));
        }

        Id = id;
        Label = label;
        Domains = [.. domains];
        Parameters = [.. parameters];
        Connectors = [.. connectors];
        PermittedPolicies = permittedPolicies;
        MaxDerivedInstances = maxDerivedInstances;
        GeneratorRevisions = [.. generatorRevisions];
    }

    public uint Id { get; }
    public string Label { get; }
    public IReadOnlyList<CompositionDomain> Domains { get; }
    public IReadOnlyList<ParameterDescriptor> Parameters { get; }
    public IReadOnlyList<ConnectorKind> Connectors { get; }
    public StoragePolicies PermittedPolicies { get; }
    public uint MaxDerivedInstances { get; }
    public IReadOnlyList<string> GeneratorRevisions { get; }

    /// <summary>Finds a parameter by label, or null.</summary>
    public ParameterDescriptor? FindParameter(string label) => Parameters.FirstOrDefault(parameter => parameter.Label == label);

    /// <summary>Finds a connector kind by label, or null.</summary>
    public ConnectorKind? FindConnector(string label) => Connectors.FirstOrDefault(connector => connector.Label == label);

    private static void RejectRepeatedLabels(string category, IEnumerable<string> labels, string what)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string label in labels)
        {
            if (!seen.Add(label))
            {
                throw new ArgumentException($"Category '{category}' repeats {what} label '{label}'.");
            }
        }
    }

    internal void Encode(CanonicalWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteText(Label);
        writer.WriteCount(Domains.Count);
        foreach (CompositionDomain domain in Domains)
        {
            writer.WriteUInt8((byte)domain);
        }

        writer.WriteCount(Parameters.Count);
        foreach (ParameterDescriptor parameter in Parameters)
        {
            parameter.Encode(writer);
        }

        writer.WriteCount(Connectors.Count);
        foreach (ConnectorKind connector in Connectors)
        {
            connector.Encode(writer);
        }

        writer.WriteUInt8((byte)PermittedPolicies);
        writer.WriteUInt32(MaxDerivedInstances);
        writer.WriteCount(GeneratorRevisions.Count);
        foreach (string revision in GeneratorRevisions)
        {
            writer.WriteText(revision);
        }
    }

    internal static CategoryDefinition Decode(CanonicalReader reader)
    {
        uint id = reader.ReadUInt32();
        string label = reader.ReadText();

        int domainCount = reader.ReadCount();
        if (domainCount > MaxDomains)
        {
            throw new FormatException($"Category '{label}' declares {domainCount} domains; the bound is {MaxDomains}.");
        }

        var domains = new CompositionDomain[domainCount];
        for (int index = 0; index < domainCount; index++)
        {
            domains[index] = (CompositionDomain)reader.ReadUInt8();
        }

        int parameterCount = reader.ReadCount();
        if (parameterCount > MaxParameters)
        {
            throw new FormatException($"Category '{label}' declares {parameterCount} parameters; the bound is {MaxParameters}.");
        }

        var parameters = new ParameterDescriptor[parameterCount];
        for (int index = 0; index < parameterCount; index++)
        {
            parameters[index] = ParameterDescriptor.Decode(reader);
        }

        int connectorCount = reader.ReadCount();
        if (connectorCount > MaxConnectors)
        {
            throw new FormatException($"Category '{label}' declares {connectorCount} connectors; the bound is {MaxConnectors}.");
        }

        var connectors = new ConnectorKind[connectorCount];
        for (int index = 0; index < connectorCount; index++)
        {
            connectors[index] = ConnectorKind.Decode(reader);
        }

        var policies = (StoragePolicies)reader.ReadUInt8();
        uint maxDerived = reader.ReadUInt32();

        int revisionCount = reader.ReadCount();
        if (revisionCount > MaxGeneratorRevisions)
        {
            throw new FormatException($"Category '{label}' lists {revisionCount} generator revisions; the bound is {MaxGeneratorRevisions}.");
        }

        var revisions = new string[revisionCount];
        for (int index = 0; index < revisionCount; index++)
        {
            revisions[index] = reader.ReadText();
        }

        try
        {
            return new CategoryDefinition(id, label, domains, parameters, connectors, policies, maxDerived, revisions);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
