using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The full identity of a primitive definition: its pack and its pack-local ID ("the full definition
/// identity is (pack_id, primitive_id)", technical design, primitive sets and compact references).
/// Exact content is a separate concern, identified by a definition's own revision and content hash, not
/// by this type. The default value is unset, matching the registry's own reserved-zero rule for the
/// local ID and this project's convention of an unset default rather than an identity of zero
/// (decision 0006, decision 0029).
/// </summary>
public readonly record struct PrimitiveId(PackId Pack, uint LocalId)
{
    /// <summary>Whether this is the default value: an unset pack, or the reserved-invalid local ID zero.</summary>
    public bool IsUnset => Pack.IsUnset || LocalId == 0;
}
