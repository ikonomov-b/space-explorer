using Godot;
using System.Collections.Generic;
using System.Linq;

namespace P0Expedition;

/// <summary>
/// Root node and phase state machine for the P0 greybox expedition prototype.
/// Explore the one hard-coded region, carry at most two of the four artifacts, return to the
/// home anchor, sell, and choose (or decline) a next destination. See decision 0015 and 0025.
/// </summary>
public partial class Main : Node
{
    private const int CargoCapacity = 2;
    private const float InteractRange = 2.5f;

    private enum Phase { Explore, SwapChoice, Sale, Destination, Done }

    private Player _player = null!;
    private Hud _hud = null!;
    private List<Artifact> _artifacts = null!;
    private LaunchPad _launchPad = null!;
    private readonly List<Artifact> _carried = new();
    private Artifact? _pendingSwap;
    private Phase _phase = Phase.Explore;

    public override void _Ready()
    {
        var world = new Node3D { Name = "World" };
        AddChild(world);

        WorldBuilder.Build(world, out _artifacts, out _launchPad, out Vector3 spawn);
        foreach (Artifact artifact in _artifacts)
        {
            Artifact captured = artifact;
            captured.OnActivate = () => HandleArtifactActivate(captured);
        }
        _launchPad.CarriedCountProvider = () => _carried.Count;
        _launchPad.OnActivate = HandleLaunch;

        _player = Player.Create(spawn);
        AddChild(_player);

        _hud = Hud.Create();
        AddChild(_hud);

        Input.MouseMode = Input.MouseModeEnum.Captured;

        if (OS.GetCmdlineUserArgs().Contains("--smoke"))
        {
            GetTree().CreateTimer(0.3).Timeout += () =>
            {
                GD.Print("SMOKE OK");
                GetTree().Quit(0);
            };
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_phase != Phase.Explore)
        {
            return;
        }

        IInteractable? nearest = FindNearestInteractable();
        _hud.SetPrompt(nearest?.PromptText);
        _hud.SetCargoStatus(_carried);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        switch (_phase)
        {
            case Phase.Explore when key.PhysicalKeycode == Key.E:
                FindNearestInteractable()?.Activate();
                break;
            case Phase.SwapChoice:
                HandleSwapKey(key.PhysicalKeycode);
                break;
            case Phase.Sale when key.PhysicalKeycode is Key.Enter or Key.KpEnter:
                EnterDestinationPhase();
                break;
            case Phase.Destination:
                HandleDestinationKey(key.PhysicalKeycode);
                break;
        }
    }

    private IInteractable? FindNearestInteractable()
    {
        Vector3 origin = _player.GlobalPosition;
        IInteractable? best = null;
        float bestDistance = InteractRange;

        foreach (Artifact artifact in _artifacts.Where(a => !a.Collected))
        {
            float distance = origin.DistanceTo(artifact.GlobalPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = artifact;
            }
        }

        float launchDistance = origin.DistanceTo(_launchPad.GlobalPosition);
        if (launchDistance < bestDistance)
        {
            best = _launchPad;
        }

        return best;
    }

    private void HandleArtifactActivate(Artifact artifact)
    {
        if (_carried.Count < CargoCapacity)
        {
            artifact.Collected = true;
            _carried.Add(artifact);
            GD.Print($"Collected {artifact.ArtifactName} ({artifact.SalePrice}c) — cargo {_carried.Count}/{CargoCapacity}");
            return;
        }

        _pendingSwap = artifact;
        _phase = Phase.SwapChoice;
        _hud.ShowSwapChoice(artifact, _carried[0], _carried[1]);
    }

    private void HandleSwapKey(Key keycode)
    {
        if (_pendingSwap is null)
        {
            return;
        }

        int slot = keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            _ => -1,
        };

        if (slot >= 0)
        {
            Artifact dropped = _carried[slot];
            _carried[slot] = _pendingSwap;
            _pendingSwap.Collected = true;
            GD.Print($"Left {dropped.ArtifactName} behind, took {_pendingSwap.ArtifactName}");
            _pendingSwap = null;
            _phase = Phase.Explore;
            _hud.HideSwapChoice();
        }
        else if (keycode == Key.Escape)
        {
            GD.Print($"Left {_pendingSwap.ArtifactName} behind");
            _pendingSwap = null;
            _phase = Phase.Explore;
            _hud.HideSwapChoice();
        }
    }

    private void HandleLaunch()
    {
        _phase = Phase.Sale;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        int total = _carried.Sum(a => a.SalePrice);
        _hud.ShowSalePhase(_carried, total);
        GD.Print($"Returned with {_carried.Count} artifact(s), sale total {total}c");
    }

    private void EnterDestinationPhase()
    {
        _phase = Phase.Destination;
        _hud.ShowDestinationPhase();
    }

    private void HandleDestinationKey(Key keycode)
    {
        string? choice = keycode switch
        {
            Key.Key1 => "Nearby Ridge Field",
            Key.Key2 => "Frozen Moonlet",
            Key.Key3 => "Deep Belt Outpost",
            Key.Key4 => "end the session here",
            _ => null,
        };

        if (choice is null)
        {
            return;
        }

        _phase = Phase.Done;
        GD.Print($"Next destination choice: {choice}");
        _hud.ShowDone(choice);
    }
}
