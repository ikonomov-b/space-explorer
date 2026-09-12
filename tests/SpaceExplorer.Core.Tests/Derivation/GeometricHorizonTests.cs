using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Registry;
using Xunit;

namespace SpaceExplorer.Core.Tests.Derivation;

/// <summary>
/// The flat-model horizon distance decision 0041 already derives the minimum landable radius from, reused
/// rather than re-derived for rung 5's far field
/// ([decision 0071](../../../docs/decisions/0071-the-explorable-planet-is-the-subject-the-far-field-before-features-and-a-two-window-inspection.md)).
/// </summary>
public class GeometricHorizonTests
{
    [Fact]
    public void At_the_minimum_landable_radius_the_horizon_is_the_regions_own_half_diagonal()
    {
        // Decision 0041 derives 524,288 m as exactly the radius at which the 2 m-eye horizon reaches a
        // maximal region's far corner, "about 1,448 m": the two figures are one fact, not a coincidence to
        // re-check independently.
        double distance = GeometricHorizon.DistanceMetres(CategoryRegistryRevision1.MinimumLandableRadius, eyeHeightMetres: 2);

        Assert.InRange(distance, 1_447.5, 1_448.5);
    }

    [Fact]
    public void Quadrupling_the_radius_doubles_the_horizon()
    {
        long radiusUnits = 10_000_000L << CategoryRegistryRevision1.PlanetFractionBits;
        double near = GeometricHorizon.DistanceMetres(radiusUnits, eyeHeightMetres: 2);
        double far = GeometricHorizon.DistanceMetres(radiusUnits * 4, eyeHeightMetres: 2);

        Assert.Equal(near * 2, far, precision: 6);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-1, 2)]
    [InlineData(1, 0)]
    [InlineData(1, -2)]
    public void A_non_positive_radius_or_eye_height_is_refused_by_name(long radiusUnits, double eyeHeightMetres)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeometricHorizon.DistanceMetres(radiusUnits, eyeHeightMetres));
    }
}
