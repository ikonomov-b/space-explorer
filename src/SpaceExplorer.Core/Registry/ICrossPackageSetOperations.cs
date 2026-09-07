namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Union, intersection, and difference across mixed packs, operating on exact references rather than
/// package-local handles, since a handle is meaningless outside the table that resolved it
/// ("union/intersection/difference across packages or revisions must resolve or remap exact references
/// first", technical design, primitive sets and compact references; decision 0031). Specified here, not
/// implemented: "the operations are implemented when a feature consumes them" (same source). No
/// production code implements this interface yet; M0a exit criterion 4 is the contract, not the behavior
/// (decision 0029).
/// </summary>
public interface ICrossPackageSetOperations
{
    /// <summary>Every reference in either operand.</summary>
    IReadOnlySet<PrimitiveRevisionRef> Union(IReadOnlySet<PrimitiveRevisionRef> first, IReadOnlySet<PrimitiveRevisionRef> second);

    /// <summary>Every reference in both operands.</summary>
    IReadOnlySet<PrimitiveRevisionRef> Intersect(IReadOnlySet<PrimitiveRevisionRef> first, IReadOnlySet<PrimitiveRevisionRef> second);

    /// <summary>Every reference in <paramref name="first"/> that is not also in <paramref name="second"/>.</summary>
    IReadOnlySet<PrimitiveRevisionRef> Except(IReadOnlySet<PrimitiveRevisionRef> first, IReadOnlySet<PrimitiveRevisionRef> second);
}
