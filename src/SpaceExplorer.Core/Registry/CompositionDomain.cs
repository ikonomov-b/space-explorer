namespace SpaceExplorer.Core.Registry;

/// <summary>The broad contexts a primitive may participate in (decision 0031). Values are frozen in the category registry record.</summary>
public enum CompositionDomain : byte
{
    SolarSystem = 1,
    Planet = 2,
    Surface = 3,
    Cave = 4,
    LifeForm = 5,
    Site = 6,
    Artifact = 7,
}

/// <summary>
/// The canonical label of each domain: the name a graph specification names its domain by and the first
/// segment of every instance path in that graph (decision 0031).
/// </summary>
public static class CompositionDomains
{
    /// <summary>The canonical label of <paramref name="domain"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="domain"/> is not a known domain.</exception>
    public static string Label(CompositionDomain domain) => domain switch
    {
        CompositionDomain.SolarSystem => "solar-system",
        CompositionDomain.Planet => "planet",
        CompositionDomain.Surface => "surface",
        CompositionDomain.Cave => "cave",
        CompositionDomain.LifeForm => "life-form",
        CompositionDomain.Site => "site",
        CompositionDomain.Artifact => "artifact",
        _ => throw new ArgumentException($"Unknown composition domain {(byte)domain}.", nameof(domain)),
    };

    /// <summary>The domain <paramref name="label"/> names, or null.</summary>
    public static CompositionDomain? TryParse(string label) =>
        Enum.GetValues<CompositionDomain>().Cast<CompositionDomain?>().FirstOrDefault(domain => Label(domain!.Value) == label);

    /// <summary>Every domain's label, in enum order.</summary>
    public static IEnumerable<string> Labels => Enum.GetValues<CompositionDomain>().Select(Label);
}
