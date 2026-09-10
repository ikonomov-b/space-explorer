using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// What a stored quantity means: a base symbol and a decimal exponent, fixing the reading of a value by
/// one equation — <c>quantity = stored ÷ 2^fractionBits × 10^exponent</c>, in the base symbol. The
/// exponent carries a magnitude too large for the integer itself, a mass in 10^20 kg; the fraction bits
/// keep carrying sub-unit resolution; neither duplicates the other
/// ([decision 0060](../../../docs/decisions/0060-registry-revision-4-units-defaults-and-validation-limits.md)).
/// </summary>
/// <remarks>
/// <see cref="Ratio"/> and <see cref="Count"/> are the two symbols that do not follow the equation, and
/// they are here to tell apart the two real kinds of dimensionless parameter: a fraction of 65,535, whose
/// denominator the record already holds as the parameter's maximum, and a count such as a segment number
/// or an enumerated choice.
/// </remarks>
public readonly record struct ParameterUnit(string Symbol, int Exponent)
{
    /// <summary>Dimensionless, read as <c>stored ÷ Max</c>: the maximum is the denominator.</summary>
    public const string Ratio = "ratio";

    /// <summary>Dimensionless: a count, an index, a channel, or a parameter that is not a quantity.</summary>
    public const string Count = "1";

    /// <summary>The symbols this registry revision admits; one not on the list is a later revision (decision 0060).</summary>
    public static readonly string[] Symbols = ["kg", "m", "kg/m3", "K", "Pa", "W", "tick", "turn", Ratio, Count];

    /// <summary>A parameter that is not a quantity.</summary>
    public static ParameterUnit None => new(Count, 0);

    /// <summary>A fraction of its own declared maximum.</summary>
    public static ParameterUnit Fraction => new(Ratio, 0);

    /// <summary>The unit <paramref name="symbol"/> at <paramref name="exponent"/>, checked against the vocabulary.</summary>
    /// <exception cref="ArgumentException">The symbol is not one this revision admits, or the exponent is out of range.</exception>
    public static ParameterUnit Of(string symbol, int exponent = 0)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        if (!Symbols.Contains(symbol))
        {
            throw new ArgumentException($"'{symbol}' is not a unit symbol this registry revision admits; they are {string.Join(", ", Symbols)}.", nameof(symbol));
        }

        // Wide enough for 10^20 kg and 10^-3 kg with room to spare, and narrow enough that a mistyped
        // exponent is refused rather than stored.
        if (exponent is < -30 or > 30)
        {
            throw new ArgumentException($"A unit exponent is between -30 and 30; '{symbol}' declares {exponent}.", nameof(exponent));
        }

        if (exponent != 0 && symbol is Ratio or Count)
        {
            throw new ArgumentException($"A dimensionless unit takes no exponent; '{symbol}' declares {exponent}.", nameof(exponent));
        }

        return new ParameterUnit(symbol, exponent);
    }

    /// <summary>How the unit reads in a message: the symbol, with its power of ten where it has one.</summary>
    public override string ToString() => Exponent == 0 ? Symbol : $"1e{Exponent.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Symbol}";

    /// <summary>Written with <c>WritePath</c>, because a compound symbol needs a separator that text does not admit.</summary>
    internal void Encode(CanonicalWriter writer)
    {
        writer.WritePath(Symbol);
        writer.WriteVarInt(Exponent);
    }

    internal static ParameterUnit Decode(CanonicalReader reader)
    {
        string symbol = reader.ReadPath();
        long exponent = reader.ReadVarInt();

        try
        {
            return exponent is >= int.MinValue and <= int.MaxValue
                ? Of(symbol, (int)exponent)
                : throw new ArgumentException($"Unit exponent {exponent} is out of range.");
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
