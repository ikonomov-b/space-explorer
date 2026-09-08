using System.Globalization;
using System.Text;
using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Description;

/// <summary>What a body is in its system: the root star, a planet of the star or of a barycentre, a moon of a planet, or a barycentre itself.</summary>
public enum BodyRole : byte
{
    Star = 1,
    Planet = 2,
    Moon = 3,
    Barycentre = 4,
}

/// <summary>
/// One body as the description states it. Every field is read from the composition graph or derived from
/// it by a named rule; nothing is stored and nothing is invented (decisions 0031, 0045).
/// </summary>
public sealed record BodyDescription(
    string Number,
    string Path,
    BodyRole Role,
    string Type,
    long DistanceMetres,
    long OrbitMetres,
    long MassUnits,
    long RadiusUnits,
    long DensityKilogramsPerCubicMetre,
    long GravityMillimetres,
    long PressurePascals,
    string Atmosphere,
    long TemperatureKelvin,
    bool LandingCandidate,
    IReadOnlyList<string> Refusals,
    bool LifeBearing);

/// <summary>
/// The text description of a composed solar system, derived on demand from its graph and never stored
/// (decision 0045). Version 1 states the star's class and, for every body in orbit order, its type,
/// distance, mass, radius, the surface gravity and equilibrium temperature the rules of decision 0037
/// derive, the surface pressure of its atmosphere primitive, whether it is a landing candidate, and
/// whether it bears life.
/// </summary>
/// <remarks>
/// Until regions exist a body is a landing candidate when its reference radius is at least the minimum
/// of decision 0041 and its derived gravity, pressure, and temperature fall inside the suit profile;
/// the validation outcome of decision 0044 replaces that flag when regions are generated, which is a
/// version change of this description (decision 0047). Life follows the presence of a life primitive, so
/// under a registry revision that has no life category no body bears life.
/// </remarks>
public sealed class SystemDescription
{
    /// <summary>The version of this description's content and wording; a change to either is a new version.</summary>
    public const uint Version = 2;

    private const string LifeCategoryLabel = "life";

    private SystemDescription(CompositionGraph graph, SuitProfile suit, BodyDescription star, BodyDescription[] bodies)
    {
        Graph = graph;
        Suit = suit;
        Star = star;
        Bodies = bodies;
        Text = Render();
    }

    public CompositionGraph Graph { get; }

    /// <summary>The profile the landing-candidate flag was decided against; its hash is part of the description.</summary>
    public SuitProfile Suit { get; }

    public BodyDescription Star { get; }

    /// <summary>Every body but the star, in the graph's own order.</summary>
    public IReadOnlyList<BodyDescription> Bodies { get; }

    /// <summary>The description as the tool prints it and the arrival screen will show it.</summary>
    public string Text { get; }

    /// <summary>
    /// What the description says about the star, without the column the table prints it in, so a view can
    /// put it on the star itself and the two cannot drift apart.
    /// </summary>
    public string StarLine
    {
        get
        {
            long luminosity = DerivationRules.Luminosity(Star.RadiusUnits, Star.TemperatureKelvin);
            return $"class {Star.Type}, {Star.TemperatureKelvin} K, {Scaled(Rescale(Star.MassUnits, CategoryRegistryRevision1.SolarMass, 1_000), 1_000, 3)} Msun, {Kilometres(Star.RadiusUnits)} km, {Scaled(Rescale(luminosity, CategoryRegistryRevision1.SolarLuminosity, 1_000), 1_000, 3)} Lsun";
        }
    }

    /// <summary>The line the table gives <paramref name="body"/>, without its indent or line break.</summary>
    public static string LineFor(BodyDescription body) => Line(body).Trim();

    /// <summary>
    /// The same body in one short line: what it is, where it is, how warm, and the verdict. It is what a
    /// view shows when there is no room for the whole line, and it is built here so the short and the
    /// long form cannot say different things.
    /// </summary>
    public static string BriefFor(BodyDescription body)
    {
        string where = $"{Scaled(Rescale(body.DistanceMetres, PhysicalConstants.AstronomicalUnitMetres, 1_000), 1_000, 3)} AU";
        return body.Role == BodyRole.Barycentre
            ? $"{body.Number} barycentre  {where}"
            : $"{body.Number} {body.Type}  {where}  {body.TemperatureKelvin} K  {(body.LandingCandidate ? "landing candidate" : $"no: {string.Join(" ", body.Refusals)}")}";
    }

    /// <summary>The hash of <see cref="Text"/>, which is what a fixed-seed vector records.</summary>
    public ContentHash Hash => ContentHash.Of(Encoding.UTF8.GetBytes(Text));

    public int PlanetCount => Bodies.Count(body => body.Role == BodyRole.Planet);
    public int MoonCount => Bodies.Count(body => body.Role == BodyRole.Moon);
    public int BarycentreCount => Bodies.Count(body => body.Role == BodyRole.Barycentre);
    public int LandingCandidateCount => Bodies.Count(body => body.LandingCandidate);
    public int LifeBearingCount => Bodies.Count(body => body.LifeBearing);

    /// <summary>Derives the description of <paramref name="graph"/>, which must be a composed solar system.</summary>
    /// <exception cref="ArgumentException">The graph is of another domain or is not rooted at a star.</exception>
    public static SystemDescription Derive(CompositionGraph graph, CategoryRegistry registry, SuitProfile suit)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(suit);

        if (graph.Specification.CompositionDomain != CompositionDomain.SolarSystem)
        {
            throw new ArgumentException($"A system description needs a solar-system graph; this one is '{graph.Specification.RootPath}'.", nameof(graph));
        }

        CategoryDefinition rootSchema = registry.Find(graph.Root.Definition.Category);
        if (rootSchema.Label != "star")
        {
            throw new ArgumentException($"A system description needs a star at the root; this graph is rooted at a '{rootSchema.Label}'.", nameof(graph));
        }

        uint? lifeCategory = registry.TryFindByLabel(LifeCategoryLabel)?.Id;
        long starTemperature = Integer(graph.Root, registry, "effective-temperature");
        long starRadius = Integer(graph.Root, registry, "radius");

        // A spectral class the registry still stores is read; from revision 2 it follows from the
        // effective temperature instead, and can no longer contradict it (review finding 47).
        string spectralClass = TryChoice(graph.Root, registry, "spectral-class") ?? DerivationRules.SpectralClass(starTemperature);

        var star = new BodyDescription(
            Number: "-",
            graph.Root.Path,
            BodyRole.Star,
            spectralClass,
            DistanceMetres: 0,
            OrbitMetres: 0,
            Integer(graph.Root, registry, "mass"),
            starRadius,
            DensityKilogramsPerCubicMetre: 0,
            GravityMillimetres: 0,
            PressurePascals: 0,
            Atmosphere: "-",
            starTemperature,
            LandingCandidate: false,
            Refusals: [],
            LifeBearing: false);

        var bodies = new List<BodyDescription>();
        Walk(graph.Root, registry, suit, lifeCategory, starTemperature, starRadius, parentNumber: string.Empty, parentDistance: 0, parentRole: BodyRole.Star, bodies);

        return new SystemDescription(graph, suit, star, [.. bodies]);
    }

    private static void Walk(GraphNode node, CategoryRegistry registry, SuitProfile suit, uint? lifeCategory, long starTemperature, long starRadius, string parentNumber, long parentDistance, BodyRole parentRole, List<BodyDescription> bodies)
    {
        CategoryDefinition schema = registry.Find(node.Definition.Category);
        for (int index = 0; index < schema.Connectors.Count; index++)
        {
            ConnectorKind connector = schema.Connectors[index];
            if (connector.Transform != TransformKind.OrbitalElements)
            {
                continue;
            }

            // Decision 0045 asks for each planet in orbit order, which is not the order the generator
            // attached them, so the listing sorts by semi-major axis and numbers them from the star out.
            GraphNode[] children = [.. node.Children[index].OrderBy(body => body.Transform!.Component("semi-major-axis"))];
            for (int child = 0; child < children.Length; child++)
            {
                GraphNode body = children[child];
                string number = parentNumber.Length == 0
                    ? (child + 1).ToString(CultureInfo.InvariantCulture)
                    : $"{parentNumber}.{child + 1}";

                // A body's distance from the star is the semi-major axis of the orbit that hangs from the
                // star on its ancestry, so a moon shares its planet's distance and a planet on a
                // barycentre shares the barycentre's.
                long orbit = body.Transform!.Component("semi-major-axis");
                long distance = parentDistance == 0 ? orbit : parentDistance;

                BodyDescription described = Describe(body, registry, suit, lifeCategory, starTemperature, starRadius, number, distance, orbit, parentRole);
                bodies.Add(described);
                Walk(body, registry, suit, lifeCategory, starTemperature, starRadius, number, distance, described.Role, bodies);
            }
        }
    }

    private static BodyDescription Describe(GraphNode node, CategoryRegistry registry, SuitProfile suit, uint? lifeCategory, long starTemperature, long starRadius, string number, long distance, long orbit, BodyRole parentRole)
    {
        CategoryDefinition schema = registry.Find(node.Definition.Category);
        if (schema.Label == "barycentre")
        {
            // A barycentre is a point, not a body: it carries no parameter and reaches no verdict.
            return new BodyDescription(number, node.Path, BodyRole.Barycentre, "-", distance, orbit, 0, 0, 0, 0, 0, "-", 0, false, [], false);
        }

        long mass = Integer(node, registry, "mass");
        long albedo = Integer(node, registry, "albedo");

        // A radius the registry still stores is read; from revision 2 a body carries a mean density and
        // its radius follows from that and its mass, so the two can no longer disagree (finding 47).
        long density = TryInteger(node, registry, "density") ?? 0;
        long radius = TryInteger(node, registry, "radius") ?? DerivationRules.Radius(mass, density);
        long gravity = DerivationRules.SurfaceGravity(mass, radius);
        long temperature = DerivationRules.EquilibriumTemperature(starTemperature, starRadius, distance, albedo);

        (long pressure, string atmosphere) = Atmosphere(node, registry);
        List<string> refusals = [.. suit.Refusals(gravity, pressure, temperature)];
        if (radius < CategoryRegistryRevision1.MinimumLandableRadius)
        {
            refusals.Insert(0, "radius");
        }

        return new BodyDescription(
            number,
            node.Path,
            parentRole == BodyRole.Planet ? BodyRole.Moon : BodyRole.Planet,
            Choice(node, registry, "body-type"),
            distance,
            orbit,
            mass,
            radius,
            density,
            gravity,
            pressure,
            atmosphere,
            temperature,
            refusals.Count == 0,
            refusals,
            LifeBearing: lifeCategory is { } life && node.Preorder.Any(instance => instance.Definition.Category == life));
    }

    private static (long Pressure, string Composition) Atmosphere(GraphNode node, CategoryRegistry registry)
    {
        CategoryDefinition schema = registry.Find(node.Definition.Category);
        for (int index = 0; index < schema.Connectors.Count; index++)
        {
            if (schema.Connectors[index].Label != "atmosphere" || node.Children[index].Count == 0)
            {
                continue;
            }

            GraphNode atmosphere = node.Children[index][0];
            return (Integer(atmosphere, registry, "surface-pressure"), Choice(atmosphere, registry, "composition"));
        }

        return (0, "none");
    }

    private static long Integer(GraphNode node, CategoryRegistry registry, string label) =>
        TryInteger(node, registry, label) ?? throw Missing(node, registry, label);

    private static string Choice(GraphNode node, CategoryRegistry registry, string label) =>
        TryChoice(node, registry, label) ?? throw Missing(node, registry, label);

    private static long? TryInteger(GraphNode node, CategoryRegistry registry, string label) =>
        Parameter(node, registry, label) is { } found ? found.Value.AsInteger : null;

    /// <summary>The label of an enum parameter, or null when this registry revision does not store it and a rule derives it instead.</summary>
    private static string? TryChoice(GraphNode node, CategoryRegistry registry, string label) =>
        Parameter(node, registry, label) is { } found ? found.Descriptor.EnumLabels[(int)found.Value.AsEnumIndex] : null;

    private static (ParameterDescriptor Descriptor, ParameterValue Value)? Parameter(GraphNode node, CategoryRegistry registry, string label)
    {
        CategoryDefinition schema = registry.Find(node.Definition.Category);
        for (int index = 0; index < schema.Parameters.Count; index++)
        {
            if (schema.Parameters[index].Label == label)
            {
                return (schema.Parameters[index], node.Definition.Parameters[index]);
            }
        }

        return null;
    }

    private static ArgumentException Missing(GraphNode node, CategoryRegistry registry, string label) =>
        new($"Category '{registry.Find(node.Definition.Category).Label}' has no parameter '{label}', so registry revision {registry.Revision} cannot be described by version {Version}.", nameof(label));

    private string Render()
    {
        SystemDescription description = this;
        GraphSpecification specification = Graph.Specification;
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"system description {Version}\n");
        text.Append(CultureInfo.InvariantCulture, $"graph         {description.Graph.Pack} seed {specification.Seed}\n");
        text.Append(CultureInfo.InvariantCulture, $"pinned        registry {specification.RegistryRevision}, grammar {specification.GrammarVersion}, generator {specification.GeneratorVersion}, suit profile {description.Suit.Version} {description.Suit.Hash.ToString()[..12]}\n");
        text.Append(CultureInfo.InvariantCulture, $"star          {StarLine}\n");

        foreach (BodyDescription body in description.Bodies)
        {
            text.Append(Line(body));
        }

        text.Append(CultureInfo.InvariantCulture, $"totals        {Count(description.PlanetCount, "planet")}, {Count(description.MoonCount, "moon")}, {Count(description.BarycentreCount, "barycentre")}; {Count(description.LandingCandidateCount, "landing candidate")}; {(description.LifeBearingCount == 0 ? "no life" : Count(description.LifeBearingCount, "life-bearing body"))}\n");
        return text.ToString();
    }

    private static string Line(BodyDescription body)
    {
        string role = body.Role switch
        {
            BodyRole.Moon => "moon",
            BodyRole.Barycentre => "barycentre",
            _ => "planet",
        };

        string head = $"  {body.Number,-6}{role,-11}{body.Type,-11}{Scaled(Rescale(body.DistanceMetres, PhysicalConstants.AstronomicalUnitMetres, 1_000), 1_000, 3) + " AU",10}";
        if (body.Role == BodyRole.Barycentre)
        {
            return head + "\n";
        }

        string verdict = body.LandingCandidate ? "landing candidate" : $"no: {string.Join(" ", body.Refusals)}";
        // A registry revision that still stores a radius derives no density, and prints none.
        string density = body.DensityKilogramsPerCubicMetre == 0
            ? "-"
            : $"{body.DensityKilogramsPerCubicMetre.ToString(CultureInfo.InvariantCulture)} kg/m3";

        return head
            + $"{Scaled(Rescale(body.MassUnits, CategoryRegistryRevision1.EarthMass, 100), 100, 2) + " Me",11}"
            + $"{Kilometres(body.RadiusUnits) + " km",11}"
            + $"{density,13}"
            + $"{Scaled((body.GravityMillimetres + 5) / 10, 100, 2) + " m/s^2",13}"
            + $"{Scaled((body.PressurePascals + 50) / 100, 10, 1) + " kPa",13}"
            + $"  {body.Atmosphere,-16}"
            + $"{body.TemperatureKelvin.ToString(CultureInfo.InvariantCulture) + " K",7}"
            + $"  {verdict}\n";
    }

    /// <summary>A count with its noun, pluralised by the only rule these nouns need.</summary>
    private static string Count(int count, string noun) =>
        $"{count.ToString(CultureInfo.InvariantCulture)} {noun}{(count == 1 ? string.Empty : "s")}";

    /// <summary>Restates <paramref name="value"/> in units of <paramref name="unit"/> at <paramref name="scale"/> steps per unit, rounding to nearest.</summary>
    private static long Rescale(long value, long unit, long scale) => ((value * scale) + (unit / 2)) / unit;

    /// <summary>Renders a value carrying <paramref name="digits"/> decimals, where <paramref name="scale"/> is ten to that power.</summary>
    private static string Scaled(long value, long scale, int digits) =>
        $"{(value / scale).ToString(CultureInfo.InvariantCulture)}.{(value % scale).ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0')}";

    /// <summary>A radius in the registry's 1/256 m units as whole kilometres.</summary>
    private static string Kilometres(long radiusUnits) => (((radiusUnits >> 8) + 500) / 1_000).ToString(CultureInfo.InvariantCulture);
}
