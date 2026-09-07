using Godot;
using System.Collections.Generic;
using System.Linq;

namespace P0Expedition;

/// <summary>All on-screen text for the prototype: prompt, cargo, swap choice, sale, and destination screens.</summary>
public partial class Hud : CanvasLayer
{
    private Label _promptLabel = null!;
    private Label _cargoLabel = null!;
    private ColorRect _crosshair = null!;
    private Panel _swapPanel = null!;
    private Label _swapLabel = null!;
    private Panel _salePanel = null!;
    private Label _saleLabel = null!;
    private Panel _destinationPanel = null!;
    private Label _destinationLabel = null!;
    private Panel _donePanel = null!;
    private Label _doneLabel = null!;

    public static Hud Create()
    {
        var hud = new Hud { Name = "Hud" };

        hud._crosshair = new ColorRect
        {
            Color = new Color(1, 1, 1, 0.85f),
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -1.5f,
            OffsetRight = 1.5f,
            OffsetTop = -1.5f,
            OffsetBottom = 1.5f,
        };
        hud.AddChild(hud._crosshair);

        hud._promptLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 1f,
            AnchorBottom = 1f,
            OffsetLeft = -260,
            OffsetRight = 260,
            OffsetTop = -90,
            OffsetBottom = -60,
            Visible = false,
        };
        hud.AddChild(hud._promptLabel);

        hud._cargoLabel = new Label
        {
            Text = "Cargo 0/2: (empty)",
            OffsetLeft = 16,
            OffsetTop = 16,
            OffsetRight = 420,
            OffsetBottom = 60,
        };
        hud.AddChild(hud._cargoLabel);

        (hud._swapPanel, hud._swapLabel) = BuildOverlay();
        hud.AddChild(hud._swapPanel);

        (hud._salePanel, hud._saleLabel) = BuildOverlay();
        hud.AddChild(hud._salePanel);

        (hud._destinationPanel, hud._destinationLabel) = BuildOverlay();
        hud.AddChild(hud._destinationPanel);

        (hud._donePanel, hud._doneLabel) = BuildOverlay();
        hud.AddChild(hud._donePanel);

        return hud;
    }

    public void SetPrompt(string? text)
    {
        _promptLabel.Text = text ?? "";
        _promptLabel.Visible = !string.IsNullOrEmpty(text);
    }

    public void SetCargoStatus(IReadOnlyList<Artifact> carried)
    {
        string items = carried.Count == 0 ? "(empty)" : string.Join(", ", carried.Select(a => $"{a.ArtifactName} ({a.SalePrice}c)"));
        _cargoLabel.Text = $"Cargo {carried.Count}/2: {items}";
    }

    public void ShowSwapChoice(Artifact pending, Artifact slot0, Artifact slot1)
    {
        _swapLabel.Text =
            $"Cargo full.\nFound: {pending.ArtifactName} ({pending.SalePrice}c)\n\n" +
            $"[1] Leave {slot0.ArtifactName} ({slot0.SalePrice}c), take {pending.ArtifactName}\n" +
            $"[2] Leave {slot1.ArtifactName} ({slot1.SalePrice}c), take {pending.ArtifactName}\n" +
            $"[Esc] Leave {pending.ArtifactName} here";
        _swapPanel.Visible = true;
    }

    public void HideSwapChoice() => _swapPanel.Visible = false;

    public void ShowSalePhase(IReadOnlyList<Artifact> carried, int total)
    {
        _crosshair.Visible = false;
        _promptLabel.Visible = false;
        string items = carried.Count == 0
            ? "Nothing recovered."
            : string.Join("\n", carried.Select(a => $"  {a.ArtifactName} — {a.SalePrice}c"));
        _saleLabel.Text = $"Back at the home anchor.\n\n{items}\n\nTotal sale: {total}c\n\n[Enter] Sell and continue";
        _salePanel.Visible = true;
    }

    public void ShowDestinationPhase()
    {
        _salePanel.Visible = false;
        _destinationLabel.Text =
            "Choose your next destination:\n\n" +
            "[1] Nearby Ridge Field — tier 1, short hop, similar risk\n" +
            "[2] Frozen Moonlet — tier 1, colder site, unknown yield\n" +
            "[3] Deep Belt Outpost — tier 2, longer burn, higher risk and reward\n" +
            "[4] End the session here";
        _destinationPanel.Visible = true;
    }

    public void ShowDone(string choice)
    {
        _destinationPanel.Visible = false;
        _doneLabel.Text = $"Session complete.\nChosen: {choice}\n\nThanks for playing.";
        _donePanel.Visible = true;
    }

    private static (Panel panel, Label label) BuildOverlay()
    {
        var panel = new Panel
        {
            Visible = false,
            AnchorLeft = 0,
            AnchorTop = 0,
            AnchorRight = 1,
            AnchorBottom = 1,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.75f) });

        var label = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.Word,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -320,
            OffsetRight = 320,
            OffsetTop = -160,
            OffsetBottom = 160,
        };
        panel.AddChild(label);

        return (panel, label);
    }
}
