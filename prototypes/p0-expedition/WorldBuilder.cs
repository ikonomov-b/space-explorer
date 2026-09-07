using Godot;
using System.Collections.Generic;

namespace P0Expedition;

/// <summary>
/// Builds the one hard-coded landing region: three sites, four artifacts, and the home anchor.
/// No generation, no persistence — see decision 0015 and decision 0025.
/// </summary>
public static class WorldBuilder
{
    private const float GroundSize = 140f;
    private const float WallHeight = 3f;

    public static void Build(Node3D world, out List<Artifact> artifacts, out LaunchPad launchPad, out Vector3 spawn)
    {
        Greybox.AddBox(world, "Ground", new Vector3(GroundSize, 1, GroundSize), new Vector3(0, -0.5f, 0), new Color(0.5f, 0.5f, 0.5f));
        AddBoundary(world);

        spawn = new Vector3(0, 0.1f, 6);
        launchPad = LaunchPad.Create(Vector3.Zero);
        world.AddChild(launchPad);

        AddSiteMarker(world, "RidgeSiteMarker", new Vector3(-40, 0, -30), new Color(0.55f, 0.45f, 0.35f));
        AddSiteMarker(world, "CraterSiteMarker", new Vector3(35, 0, -35), new Color(0.4f, 0.4f, 0.45f));
        AddSiteMarker(world, "WreckSiteMarker", new Vector3(10, 0, 45), new Color(0.5f, 0.2f, 0.2f));

        artifacts = new List<Artifact>
        {
            Artifact.Create("Fused Regolith Core", 90, new Vector3(-42, 0, -26), new Color(0.8f, 0.5f, 0.2f)),
            Artifact.Create("Banded Ice Shard", 140, new Vector3(37, 0, -31), new Color(0.4f, 0.8f, 0.9f)),
            Artifact.Create("Pitted Alloy Fragment", 60, new Vector3(31, 0, -39), new Color(0.7f, 0.7f, 0.75f)),
            Artifact.Create("Sealed Data Spindle", 220, new Vector3(10, 0, 41), new Color(0.8f, 0.2f, 0.8f)),
        };
        foreach (Artifact artifact in artifacts)
        {
            world.AddChild(artifact);
        }
    }

    private static void AddBoundary(Node3D world)
    {
        float half = GroundSize / 2f;
        Greybox.AddBox(world, "WallNorth", new Vector3(GroundSize, WallHeight, 1), new Vector3(0, WallHeight / 2f, -half), new Color(0.35f, 0.33f, 0.3f));
        Greybox.AddBox(world, "WallSouth", new Vector3(GroundSize, WallHeight, 1), new Vector3(0, WallHeight / 2f, half), new Color(0.35f, 0.33f, 0.3f));
        Greybox.AddBox(world, "WallEast", new Vector3(1, WallHeight, GroundSize), new Vector3(half, WallHeight / 2f, 0), new Color(0.35f, 0.33f, 0.3f));
        Greybox.AddBox(world, "WallWest", new Vector3(1, WallHeight, GroundSize), new Vector3(-half, WallHeight / 2f, 0), new Color(0.35f, 0.33f, 0.3f));
    }

    private static void AddSiteMarker(Node3D world, string name, Vector3 position, Color color)
    {
        Greybox.AddBox(world, name, new Vector3(4, 0.4f, 4), position + new Vector3(0, 0.2f, 0), color, collide: false);
    }
}
