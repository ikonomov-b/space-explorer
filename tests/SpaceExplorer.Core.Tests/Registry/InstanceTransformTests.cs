using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class InstanceTransformTests
{
    [Theory]
    [InlineData(TransformKind.Rigid, 6)]
    [InlineData(TransformKind.OrbitalElements, 7)]
    [InlineData(TransformKind.SurfaceAnchor, 4)]
    public void Every_transform_type_round_trips_through_its_components(TransformKind kind, int componentCount)
    {
        IReadOnlyList<TransformComponent> schema = TransformSchema.For(kind);
        Assert.Equal(componentCount, schema.Count);

        // The extremes of each component's own width, which is what a range may not exceed.
        var extreme = new InstanceTransform(kind, [.. schema.Select(component => component.Max)]);
        var writer = new CanonicalWriter("test-transform/1");
        extreme.Encode(writer);

        var reader = new CanonicalReader(writer.ToArray(), "test-transform/1");
        InstanceTransform decoded = InstanceTransform.Decode(reader, kind);

        Assert.True(reader.IsAtEnd);
        Assert.Equal(extreme.Kind, decoded.Kind);
        Assert.Equal(extreme.Components, decoded.Components);
        Assert.Equal(schema[0].Max, decoded.Component(schema[0].Label));
    }

    [Fact]
    public void A_component_outside_the_width_its_type_admits_is_refused()
    {
        // Orbital elements: the eccentricity is an unsigned 32-bit fraction and the inclination a binary
        // turn, so neither takes a negative or a 64-bit value (decision 0036).
        Assert.Throws<ArgumentException>(() => new InstanceTransform(TransformKind.OrbitalElements, [0, -1, 0, 0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new InstanceTransform(TransformKind.OrbitalElements, [0, 1L << 32, 0, 0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new InstanceTransform(TransformKind.OrbitalElements, [-1, 0, 0, 0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new InstanceTransform(TransformKind.Rigid, [0, 0, 0, 0, 0, 1L << 31]));
        Assert.Throws<ArgumentException>(() => new InstanceTransform(TransformKind.Rigid, [0, 0, 0]));
    }

    [Fact]
    public void A_range_outside_the_width_or_empty_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new TransformRange(TransformKind.Rigid, [(1, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0)]));
        Assert.Throws<ArgumentException>(() => new TransformRange(TransformKind.Rigid, [(0, 0), (0, 0), (0, 0), (0, 1L << 31), (0, 0), (0, 0)]));
        Assert.Equal(TransformKind.SurfaceAnchor, TransformRange.Origin(TransformKind.SurfaceAnchor).Kind);
    }
}
