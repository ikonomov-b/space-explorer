using SpaceExplorer.Core.Derivation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Derivation;

/// <summary>
/// `derive-star-colour/1`: a star's colour follows the effective temperature it stores, so a view draws
/// the star the content describes rather than a palette of its own (decision 0057).
/// </summary>
public class StarColourTests
{
    [Fact]
    public void The_sun_is_near_white_and_the_ends_are_orange_and_blue()
    {
        // The anchors themselves, which is what the rule interpolates between.
        Assert.Equal((255, 241, 235), DerivationRules.StarColour(5_772));
        Assert.Equal((255, 137, 14), DerivationRules.StarColour(2_000));
        Assert.Equal((155, 188, 255), DerivationRules.StarColour(50_000));

        // Cool stars are red-heavy, hot ones blue-heavy, and the Sun is between.
        (byte red, _, byte blue) = DerivationRules.StarColour(3_000);
        Assert.True(red > blue + 100, "a 3,000 K star is far redder than it is blue");
        (byte hotRed, _, byte hotBlue) = DerivationRules.StarColour(20_000);
        Assert.True(hotBlue > hotRed + 50, "a 20,000 K star is bluer than it is red");
    }

    [Fact]
    public void Between_two_anchors_the_colour_is_between_them()
    {
        (byte red, byte green, byte blue) = DerivationRules.StarColour(4_500);
        (byte coolRed, byte coolGreen, byte coolBlue) = DerivationRules.StarColour(4_000);
        (byte warmRed, byte warmGreen, byte warmBlue) = DerivationRules.StarColour(5_000);

        Assert.InRange(red, Math.Min(coolRed, warmRed), Math.Max(coolRed, warmRed));
        Assert.InRange(green, coolGreen, warmGreen);
        Assert.InRange(blue, coolBlue, warmBlue);
    }

    [Fact]
    public void Blue_rises_and_red_never_does_as_a_star_gets_hotter()
    {
        // The one property a viewer would notice if the table were mistyped.
        byte previousBlue = 0;
        byte previousRed = 255;
        for (long kelvin = 2_000; kelvin <= 40_000; kelvin += 500)
        {
            (byte red, _, byte blue) = DerivationRules.StarColour(kelvin);
            Assert.True(blue >= previousBlue, $"blue fell at {kelvin} K");
            Assert.True(red <= previousRed, $"red rose at {kelvin} K");
            previousBlue = blue;
            previousRed = red;
        }
    }

    [Fact]
    public void A_temperature_outside_the_anchors_clamps_and_a_hopeless_one_is_refused()
    {
        Assert.Equal(DerivationRules.StarColour(2_000), DerivationRules.StarColour(1));
        Assert.Equal(DerivationRules.StarColour(50_000), DerivationRules.StarColour(200_000));
        Assert.Throws<ArgumentOutOfRangeException>(() => DerivationRules.StarColour(0));
    }
}
