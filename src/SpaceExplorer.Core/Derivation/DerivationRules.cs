using SpaceExplorer.Core.Registry;

namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// The derivation rules of [decision 0037] the system description needs, each a named revision with an
/// integer implementation and recorded vectors: no floating point takes part, every division rounds to
/// nearest, and the intermediates are 128-bit so nothing overflows within the registry's parameter
/// ranges. The remaining rules decision 0037 lists arrive when a consumer needs them.
/// </summary>
public static class DerivationRules
{
    /// <summary>The generator revision identifier of <see cref="SurfaceGravity"/> (decision 0035).</summary>
    public const string SurfaceGravityRevision = "derive-surface-gravity/1";

    /// <summary>The generator revision identifier of <see cref="EquilibriumTemperature"/> (decision 0035).</summary>
    public const string EquilibriumTemperatureRevision = "derive-equilibrium-temperature/1";

    /// <summary>The generator revision identifier of <see cref="Radius"/> (decision 0035).</summary>
    public const string RadiusRevision = "derive-radius/1";

    /// <summary>The generator revision identifier of <see cref="Luminosity"/> (decision 0035).</summary>
    public const string LuminosityRevision = "derive-luminosity/1";

    /// <summary>The generator revision identifier of <see cref="SpectralClass"/> (decision 0035).</summary>
    public const string SpectralClassRevision = "derive-spectral-class/1";

    /// <summary>The generator revision identifier of <see cref="OrbitScale"/> (decision 0035).</summary>
    public const string OrbitScaleRevision = "derive-orbit-scale/1";

    /// <summary>The unit of <see cref="OrbitScale"/>: a scale of one.</summary>
    public const long OrbitScaleUnit = 1_024;

    /// <summary>The gravitational constant, 6.6743 x 10^-11 m^3 kg^-1 s^-2, as its 2018 CODATA digits over a power of ten.</summary>
    private const long GravitationalConstantDigits = 66_743;

    /// <summary>3/(4 pi), the factor between a sphere's volume and the cube of its radius, as ten digits over 10^10.</summary>
    private const long VolumeFactorDigits = 2_387_324_146;

    /// <summary>4 pi sigma with the Stefan-Boltzmann constant 5.670374419 x 10^-8, as six digits over 10^12.</summary>
    private const long RadiantFactorDigits = 712_520;

    /// <summary>The spectral classes in descending order of the temperature that begins each, hottest first (decision 0044 draws from them).</summary>
    private static readonly (long Kelvin, string Class)[] SpectralClasses =
    [
        (33_000, "O"), (10_000, "B"), (7_500, "A"), (6_000, "F"), (5_200, "G"), (3_700, "K"), (0, "M"),
    ];

    /// <summary>
    /// Surface gravity <c>G M / R^2</c> in mm/s^2, from a mass in units of 10^20 kg and a reference
    /// radius in 1/256 m, which are the units the category registry's <c>mass</c> and <c>radius</c>
    /// parameters carry.
    /// </summary>
    /// <remarks>
    /// The digits fold into one multiplier: <c>G M / R^2</c> with those units and that output scale is
    /// <c>66743 x 2^16 x 10^8 x mass / radius^2</c>, so the implementation is one multiplication, one
    /// division, and no constant that has been rounded twice.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mass is negative or the radius is not positive.</exception>
    public static long SurfaceGravity(long massUnits, long radiusUnits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(massUnits);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusUnits);

        UInt128 numerator = (UInt128)GravitationalConstantDigits * 65_536 * 100_000_000 * (UInt128)massUnits;
        UInt128 denominator = (UInt128)radiusUnits * (UInt128)radiusUnits;
        return (long)Divide(numerator, denominator);
    }

    /// <summary>
    /// The equilibrium temperature in kelvin of a body of <paramref name="albedo"/>, a fraction of 2^16,
    /// at <paramref name="distanceMetres"/> from a star of <paramref name="starTemperatureKelvin"/> and
    /// <paramref name="starRadiusUnits"/> in 1/256 m: <c>T_star sqrt(R / 2a) (1 - A)^(1/4)</c>.
    /// </summary>
    /// <remarks>
    /// Squaring the identity removes the outer root from the constant folding:
    /// <c>T^2 = T_star^2 R sqrt(1 - A) / (2^25 a)</c>, where <c>sqrt(1 - A)</c> is
    /// <c>isqrt((2^16 - A) 2^16) / 2^16</c>. One integer square root is taken of each, at a scale of 2^16
    /// so the kelvin result rounds rather than truncates.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range or the distance is not positive.</exception>
    public static long EquilibriumTemperature(long starTemperatureKelvin, long starRadiusUnits, long distanceMetres, long albedo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(starTemperatureKelvin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(starRadiusUnits);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(distanceMetres);
        ArgumentOutOfRangeException.ThrowIfNegative(albedo);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(albedo, 65_535);

        UInt128 reflected = Sqrt(((UInt128)65_536 - (UInt128)albedo) * 65_536);
        UInt128 numerator = (UInt128)starTemperatureKelvin * (UInt128)starTemperatureKelvin * (UInt128)starRadiusUnits * reflected * 65_536;
        UInt128 denominator = (UInt128)33_554_432 * (UInt128)distanceMetres;

        // The quotient is T^2 at a scale of 2^16, so its root is T at a scale of 2^8.
        return (long)Divide(Sqrt(Divide(numerator, denominator)), 256);
    }

    /// <summary>
    /// The reference radius in 1/256 m of a body of <paramref name="massUnits"/> in units of 10^20 kg and
    /// a mean <paramref name="densityKilogramsPerCubicMetre"/>, which is <c>cbrt(3 M / 4 pi rho)</c>. It
    /// replaces a drawn radius, so a body's density can no longer contradict its mass and size
    /// (review finding 47).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The mass is negative or the density is not positive.</exception>
    public static long Radius(long massUnits, long densityKilogramsPerCubicMetre)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(massUnits);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(densityKilogramsPerCubicMetre);

        // The cube of the radius in 1/256 m units is 3 M / (4 pi rho) times 256^3, and the mass unit and
        // the factor's own power of ten fold into the multiplier.
        UInt128 numerator = (UInt128)VolumeFactorDigits * (UInt128)massUnits * 10_000_000_000 * 16_777_216;
        return (long)Cbrt(Divide(numerator, (UInt128)densityKilogramsPerCubicMetre));
    }

    /// <summary>
    /// The luminosity in units of 10^20 W of a black body of <paramref name="radiusUnits"/> in 1/256 m at
    /// <paramref name="temperatureKelvin"/>, which is <c>4 pi R^2 sigma T^4</c>. It replaces a drawn
    /// luminosity, which could disagree with the radius and temperature beside it (review finding 47).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A value is negative or the radius is not positive.</exception>
    public static long Luminosity(long radiusUnits, long temperatureKelvin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusUnits);
        ArgumentOutOfRangeException.ThrowIfNegative(temperatureKelvin);

        // Divided in two stages: the widest radius and temperature the registry admits would overflow
        // 128 bits if the factor were applied before the first division.
        UInt128 area = Divide((UInt128)radiusUnits * (UInt128)radiusUnits, 65_536);
        UInt128 fourth = (UInt128)temperatureKelvin * (UInt128)temperatureKelvin * (UInt128)temperatureKelvin * (UInt128)temperatureKelvin;
        UInt128 scaled = Divide(area * fourth, 1_000_000_000_000);
        return (long)Divide(scaled * (UInt128)RadiantFactorDigits, (UInt128)10_000_000_000 * 10_000_000_000);
    }

    /// <summary>
    /// How far a star's orbits stand out from it compared with the Sun's, in units of
    /// <see cref="OrbitScaleUnit"/>: the square root of its luminosity in solar units, because a body at
    /// <c>sqrt(L)</c> times the distance receives the same flux and so reaches the same temperature. It is
    /// what makes one set of orbit bands mean the same thing around any star.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The radius is not positive or the temperature is negative.</exception>
    public static long OrbitScale(long starRadiusUnits, long starTemperatureKelvin)
    {
        long luminosity = Luminosity(starRadiusUnits, starTemperatureKelvin);

        // sqrt(x) at a scale of u is isqrt(x u^2), and the ratio is taken inside the root so the division
        // does not round away a faint star's scale before it is squared.
        UInt128 scaled = Divide((UInt128)luminosity * (UInt128)OrbitScaleUnit * (UInt128)OrbitScaleUnit, (UInt128)CategoryRegistryRevision1.SolarLuminosity);
        return (long)Sqrt(scaled);
    }

    /// <summary>The Morgan-Keenan class of a star of <paramref name="temperatureKelvin"/>, by the conventional boundaries.</summary>
    public static string SpectralClass(long temperatureKelvin)
    {
        foreach ((long kelvin, string spectralClass) in SpectralClasses)
        {
            if (temperatureKelvin >= kelvin)
            {
                return spectralClass;
            }
        }

        return SpectralClasses[^1].Class;
    }

    /// <summary>Divides, rounding half away from zero, so a derived value is never biased low.</summary>
    private static UInt128 Divide(UInt128 numerator, UInt128 denominator) => (numerator + (denominator / 2)) / denominator;

    /// <summary>The integer cube root: the greatest <c>r</c> with <c>r^3 &lt;= value</c>, set one bit at a time from the highest that cannot overflow.</summary>
    private static UInt128 Cbrt(UInt128 value)
    {
        UInt128 result = 0;

        // 2^42 cubed is the largest cube a 128-bit value holds, so no candidate below that overflows.
        for (int shift = 42; shift >= 0; shift--)
        {
            UInt128 candidate = result | ((UInt128)1 << shift);
            if (candidate <= (UInt128)1 << 42 && candidate * candidate * candidate <= value)
            {
                result = candidate;
            }
        }

        return result;
    }

    /// <summary>The integer square root: the greatest <c>r</c> with <c>r^2 &lt;= value</c>, by the bit-at-a-time method, which needs no division and no seed.</summary>
    private static UInt128 Sqrt(UInt128 value)
    {
        UInt128 remainder = value;
        UInt128 result = 0;

        // The highest power of four not above the value; 2^126 is the highest a 128-bit value can hold.
        UInt128 bit = (UInt128)1 << 126;
        while (bit > remainder)
        {
            bit >>= 2;
        }

        while (bit != 0)
        {
            if (remainder >= result + bit)
            {
                remainder -= result + bit;
                result = (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }

            bit >>= 2;
        }

        return result;
    }
}
