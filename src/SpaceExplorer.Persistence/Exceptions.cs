using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>A second manifest with a different hash was offered for a pack identifier that already has one (decision 0039).</summary>
public sealed class ReproductionFailureException(PackId pack, ContentHash existing, ContentHash attempted)
    : Exception($"Pack {pack} already holds manifest {existing}; publishing manifest {attempted} for the same specification is a reproduction failure, not a revision (decision 0039).")
{
    public PackId Pack { get; } = pack;
    public ContentHash Existing { get; } = existing;
    public ContentHash Attempted { get; } = attempted;
}

/// <summary>A stored record is missing, does not hash to its name, or does not fit its manifest; never substituted (decision 0031).</summary>
public sealed class PackageIntegrityException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>No package is indexed under the identifier.</summary>
public sealed class PackageNotFoundException(PackId pack) : Exception($"No package is published under pack identifier {pack}.")
{
    public PackId Pack { get; } = pack;
}

/// <summary>Content pins a registry revision this build does not support; an explicit compatibility error, never a substitution (decision 0035).</summary>
public sealed class CompatibilityException(string message) : Exception(message);
