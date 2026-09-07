using Godot;
using System;

namespace P0Expedition;

/// <summary>One of the four hard-coded recoverable objects. See decision 0015.</summary>
public partial class Artifact : Node3D, IInteractable
{
    public string ArtifactName { get; private set; } = "";
    public int SalePrice { get; private set; }
    public bool Collected { get; set; }
    public Action? OnActivate { get; set; }

    public string PromptText => $"[E] Investigate {ArtifactName} — est. {SalePrice}c";

    public static Artifact Create(string name, int salePrice, Vector3 position, Color color)
    {
        var artifact = new Artifact
        {
            Name = name,
            ArtifactName = name,
            SalePrice = salePrice,
            Position = position,
        };

        var mesh = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.5f, 0.5f) },
            Position = new Vector3(0, 0.6f, 0),
            MaterialOverride = Greybox.Flat(color),
        };
        artifact.AddChild(mesh);

        return artifact;
    }

    public void Activate() => OnActivate?.Invoke();
}
