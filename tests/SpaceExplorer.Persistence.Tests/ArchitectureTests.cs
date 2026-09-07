using System.Reflection;
using Xunit;

namespace SpaceExplorer.Persistence.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Persistence_assembly_does_not_reference_Godot()
    {
        var persistence = typeof(SqliteRuntime).Assembly;

        var godotReferences = persistence.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("Godot", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(godotReferences);
    }
}
