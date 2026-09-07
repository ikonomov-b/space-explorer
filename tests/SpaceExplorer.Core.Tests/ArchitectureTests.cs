using System.Reflection;
using Xunit;

namespace SpaceExplorer.Core.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Core_assembly_does_not_reference_Godot()
    {
        var core = Assembly.Load(new AssemblyName("SpaceExplorer.Core"));

        var godotReferences = core.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("Godot", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(godotReferences);
    }
}
