namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Union, intersection, and difference across mixed packs and revisions, operating on full identities
/// rather than package-local handles, since a handle is meaningless outside the table that resolved it
/// ("union/intersection/difference across packages must resolve or remap full identities first",
/// technical design, primitive sets and compact references). Specified here, not implemented: "the
/// operations are implemented when a feature consumes them" (same source). No production code
/// implements this interface yet — M0a exit criterion 4 is the contract, not the behavior
/// (decision 0029).
/// </summary>
public interface ICrossPackageSetOperations
{
    /// <summary>Every identity in either operand.</summary>
    IReadOnlySet<PrimitiveId> Union(IReadOnlySet<PrimitiveId> first, IReadOnlySet<PrimitiveId> second);

    /// <summary>Every identity in both operands.</summary>
    IReadOnlySet<PrimitiveId> Intersect(IReadOnlySet<PrimitiveId> first, IReadOnlySet<PrimitiveId> second);

    /// <summary>Every identity in <paramref name="first"/> that is not also in <paramref name="second"/>.</summary>
    IReadOnlySet<PrimitiveId> Except(IReadOnlySet<PrimitiveId> first, IReadOnlySet<PrimitiveId> second);
}
