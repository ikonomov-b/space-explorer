using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// An exact primitive reference: the definition's <see cref="PrimitiveId"/> plus the content hash of the
/// revision meant (decision 0031). For a generated definition, which has one revision for life, the hash
/// verifies that the loaded content is the referenced content rather than distinguishing revisions
/// (decision 0033). The default value is unset.
/// </summary>
public readonly record struct PrimitiveRevisionRef(PrimitiveId Id, ContentHash Hash)
{
    /// <summary>Whether either half is unset.</summary>
    public bool IsUnset => Id.IsUnset || Hash.IsUnset;
}
