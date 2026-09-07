namespace SpaceExplorer.Core.Registry;

/// <summary>The transform type a connector kind carries; each admits exactly one (decision 0036).</summary>
public enum TransformKind : byte
{
    /// <summary>A translation in the containing frame's position type plus a yaw, pitch, roll triple of binary turns.</summary>
    Rigid = 1,

    /// <summary>Orbital elements plus epoch, attaching a body to its parent body or barycentre.</summary>
    OrbitalElements = 2,

    /// <summary>Latitude, longitude, height, and heading, attaching a region to a planet.</summary>
    SurfaceAnchor = 3,
}
