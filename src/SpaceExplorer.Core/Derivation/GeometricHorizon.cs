using SpaceExplorer.Core.Registry;

namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// How far the true horizon stands from an eye above a sphere: the same flat-model approximation
/// [decision 0041](../../../docs/decisions/0041-planet-fields-tangent-regions-minimum-radius-and-far-field.md)
/// already derives the 524,288 m minimum landable radius from, <c>d = sqrt(2Rh)</c>, rather than a second
/// formula for the same geometry. It is a rendering distance and not a stored or hashed quantity, so it is
/// computed in metres rather than the fixed-point unit the rest of a planet-fixed field uses.
/// </summary>
public static class GeometricHorizon
{
    /// <summary>
    /// The distance to the horizon in metres, from a body's reference radius in 1/256 m
    /// (decision 0036's planet-fixed unit) and an eye height in metres.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="referenceRadiusUnits"/> or <paramref name="eyeHeightMetres"/> is not positive.</exception>
    public static double DistanceMetres(long referenceRadiusUnits, double eyeHeightMetres)
    {
        if (referenceRadiusUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceRadiusUnits), referenceRadiusUnits, "A body's reference radius is positive.");
        }

        if (eyeHeightMetres <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eyeHeightMetres), eyeHeightMetres, "An eye height is positive.");
        }

        double radiusMetres = referenceRadiusUnits / (double)(1L << CategoryRegistryRevision1.PlanetFractionBits);
        return Math.Sqrt(2.0 * radiusMetres * eyeHeightMetres);
    }
}
