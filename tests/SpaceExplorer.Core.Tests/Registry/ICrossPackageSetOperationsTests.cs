using System.Reflection;
using SpaceExplorer.Core.Registry;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class ICrossPackageSetOperationsTests
{
    [Theory]
    [InlineData("Union")]
    [InlineData("Intersect")]
    [InlineData("Except")]
    public void Each_operation_takes_two_identity_sets_and_returns_one(string name)
    {
        MethodInfo method = typeof(ICrossPackageSetOperations).GetMethod(name)!;

        Assert.Equal(typeof(IReadOnlySet<PrimitiveId>), method.ReturnType);
        Assert.Equal(
            [typeof(IReadOnlySet<PrimitiveId>), typeof(IReadOnlySet<PrimitiveId>)],
            method.GetParameters().Select(p => p.ParameterType));
    }

    [Fact]
    public void The_interface_declares_exactly_these_three_operations()
    {
        // Criterion 4 is the contract, not the behavior: a fourth method appearing here unnoticed
        // would be a spec change nobody recorded a decision for.
        string[] names = [.. typeof(ICrossPackageSetOperations).GetMethods().Select(m => m.Name)];
        Assert.Equal(["Union", "Intersect", "Except"], names);
    }

    [Fact]
    public void Nothing_in_Core_implements_the_contract_yet()
    {
        // "The operations are implemented when a feature consumes them" (technical design). Once one
        // does, this test is expected to fail and should be deleted, not fixed.
        bool anyImplementation = typeof(ICrossPackageSetOperations).Assembly
            .GetTypes()
            .Any(type => type != typeof(ICrossPackageSetOperations) && typeof(ICrossPackageSetOperations).IsAssignableFrom(type));

        Assert.False(anyImplementation);
    }
}
