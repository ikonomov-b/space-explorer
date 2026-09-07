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
