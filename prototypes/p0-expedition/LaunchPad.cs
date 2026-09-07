using Godot;
using System;

namespace P0Expedition;

/// <summary>The home anchor: where the player starts and returns to end the expedition. See decision 0015.</summary>
public partial class LaunchPad : Node3D, IInteractable
{
    public Func<int>? CarriedCountProvider { get; set; }
    public Action? OnActivate { get; set; }

    public string PromptText => $"[E] Launch and return to the dealer (cargo {CarriedCountProvider?.Invoke() ?? 0}/2)";

    public static LaunchPad Create(Vector3 position)
    {
        var pad = new LaunchPad { Name = "LaunchPad", Position = position };

        var mesh = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 2.2f, BottomRadius = 2.2f, Height = 0.2f },
            Position = new Vector3(0, 0.1f, 0),
            MaterialOverride = Greybox.Flat(new Color(0.9f, 0.85f, 0.2f)),
        };
        pad.AddChild(mesh);

        return pad;
    }

    public void Activate() => OnActivate?.Invoke();
}
