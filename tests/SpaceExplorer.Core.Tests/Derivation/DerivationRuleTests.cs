using SpaceExplorer.Core.Derivation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Derivation;

/// <summary>
/// The vectors of `derive-surface-gravity/1` and `derive-equilibrium-temperature/1` (decision 0037).
/// They are the solar system's own bodies, so the expected values come from published measurements
/// rather than from this implementation: masses, radii, and Bond albedos are NASA planetary fact-sheet
/// figures, and the results are compared with the surface gravity and black-body temperature those same
/// sheets state. Continuous integration runs them on Linux and Windows.
/// </summary>
public class DerivationRuleTests
{
    /// <summary>The Sun's effective temperature in kelvin.</summary>
    private const long SunTemperature = 5_772;

    /// <summary>The Sun's radius, 6.957 x 10^8 m, in the registry's 1/256 m units.</summary>
    private const long SunRadius = 695_700_000L << 8;

    [Theory]
    // Mass in units of 10^20 kg, radius in metres, then G M / R^2 in mm/s^2 computed outside this
    // repository in floating point from the same two figures, and the fact sheet's own surface gravity.
    [InlineData("Earth", 59_720L, 6_371_000L, 9_820L, "9.80")]
    [InlineData("Mars", 6_417L, 3_389_500L, 3_728L, "3.71")]
    [InlineData("Jupiter", 18_980_000L, 71_492_000L, 24_785L, "24.79")]
    [InlineData("Moon", 735L, 1_737_400L, 1_625L, "1.62")]
    public void Surface_gravity_matches_an_independent_computation_from_published_figures(string body, long massUnits, long radiusMetres, long expected, string published)
    {
        // The tabulated surface gravity is the effective value at the equator, which rotation lowers by
        // a few parts in a thousand, and it is quoted from a more precisely measured GM than the
        // four-digit mass beside it; this reproduces G M / R^2 from the tabulated mass and radius.
        long gravity = DerivationRules.SurfaceGravity(massUnits, radiusMetres << 8);

        Assert.True(Math.Abs(gravity - expected) <= 1, $"{body}: {gravity} mm/s^2 against {expected}, the fact sheet stating {published} m/s^2");
    }

    [Theory]
    // Distance in metres and Bond albedo as a fraction of 2^16, then the fact sheet's own black-body
    // temperature, which this reproduces to within a kelvin from the tabulated distance and albedo.
    [InlineData("Mercury", 57_900_000_000L, 4_456L, 440L)]
    [InlineData("Venus", 108_200_000_000L, 50_463L, 227L)]
    [InlineData("Earth", 149_600_000_000L, 20_054L, 254L)]
    [InlineData("Mars", 227_900_000_000L, 16_384L, 210L)]
    [InlineData("Jupiter", 778_500_000_000L, 22_479L, 110L)]
    public void Equilibrium_temperature_matches_the_published_black_body_temperature(string body, long distanceMetres, long albedo, long expected)
    {
        long temperature = DerivationRules.EquilibriumTemperature(SunTemperature, SunRadius, distanceMetres, albedo);

        Assert.True(Math.Abs(temperature - expected) <= 1, $"{body}: {temperature} K against the published {expected} K");
    }

    [Fact]
    public void The_extremes_of_the_registrys_ranges_do_not_overflow_or_divide_by_zero()
    {
        // The widest inputs registry revision 1 admits, so the 128-bit intermediates are shown to hold.
        Assert.True(DerivationRules.SurfaceGravity(100_000_000L, 100_000L << 8) > 0);
        Assert.Equal(0, DerivationRules.SurfaceGravity(0, 1));
        Assert.True(DerivationRules.EquilibriumTemperature(50_000, 2_000_000_000L << 8, 1, 0) > 0);
        Assert.Equal(0, DerivationRules.EquilibriumTemperature(0, 1, 1, 65_535));

        Assert.Throws<ArgumentOutOfRangeException>(() => DerivationRules.SurfaceGravity(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DerivationRules.EquilibriumTemperature(1, 1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DerivationRules.EquilibriumTemperature(1, 1, 1, 65_536));
    }
}
