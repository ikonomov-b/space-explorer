namespace SpaceExplorer.Core.Registry;

/// <summary>
/// A package-local index into a package's <see cref="ReferenceTable"/>. Not an identity: the same
/// handle in a different revision of the table can resolve to a different <see cref="PrimitiveId"/>,
/// and tables cannot be reordered in place (glossary: Handle).
/// </summary>
public readonly record struct Handle(uint Index);
