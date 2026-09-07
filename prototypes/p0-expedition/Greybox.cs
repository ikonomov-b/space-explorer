using Godot;

namespace P0Expedition;

/// <summary>Flat-shaded placeholder geometry helpers shared by the world and its props.</summary>
public static class Greybox
{
    public static StandardMaterial3D Flat(Color color) => new()
    {
        AlbedoColor = color,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
    };

    public static void AddBox(Node3D parent, string name, Vector3 size, Vector3 position, Color color, bool collide = true)
    {
        var mesh = new MeshInstance3D { Name = name, Mesh = new BoxMesh { Size = size }, MaterialOverride = Flat(color) };

        if (!collide)
        {
            mesh.Position = position;
            parent.AddChild(mesh);
            return;
        }

        var body = new StaticBody3D { Name = name + "Body", Position = position };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        body.AddChild(mesh);
        parent.AddChild(body);
    }
}
