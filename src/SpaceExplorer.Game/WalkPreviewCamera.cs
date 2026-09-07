using Godot;

namespace SpaceExplorer.Game;

/// <summary>
/// Right-drag look plus WASD ground movement for the debug preview scene, gliding at a fixed
/// eye height. Not the player controller: no physics, no collision, no region-local coordinates.
///
/// Deliberately not captured-relative-motion look: entering MouseModeEnum.Captured relies on the
/// platform warping and confining the OS cursor, which was unreliable under this XWayland setup
/// (an initial spurious jump, then no further tracking). Sampling absolute position deltas only
/// while the right button is held has no such dependency.
/// </summary>
public partial class WalkPreviewCamera : Camera3D
{
    [Export]
    public float MouseSensitivityDegrees { get; set; } = 0.15f;

    [Export]
    public float WalkSpeed { get; set; } = 1.5f;

    private float _yawDegrees;
    private float _pitchDegrees;
    private float _eyeHeight;
    private bool _looking;
    private Vector2 _lastMousePosition;

    public override void _Ready()
    {
        _yawDegrees = RotationDegrees.Y;
        _pitchDegrees = RotationDegrees.X;
        _eyeHeight = Position.Y;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right } button)
        {
            _looking = button.Pressed;
            _lastMousePosition = button.Position;
        }
        else if (@event is InputEventMouseMotion motion && _looking)
        {
            Vector2 delta = motion.Position - _lastMousePosition;
            _lastMousePosition = motion.Position;

            _yawDegrees -= delta.X * MouseSensitivityDegrees;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees - delta.Y * MouseSensitivityDegrees, -89f, 89f);
            RotationDegrees = new Vector3(_pitchDegrees, _yawDegrees, 0);
        }
    }

    public override void _Process(double delta)
    {
        Vector3 direction = Vector3.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W))
        {
            direction -= Basis.Z;
        }

        if (Input.IsPhysicalKeyPressed(Key.S))
        {
            direction += Basis.Z;
        }

        if (Input.IsPhysicalKeyPressed(Key.A))
        {
            direction -= Basis.X;
        }

        if (Input.IsPhysicalKeyPressed(Key.D))
        {
            direction += Basis.X;
        }

        direction.Y = 0;
        if (direction == Vector3.Zero)
        {
            return;
        }

        Vector3 step = direction.Normalized() * WalkSpeed * (float)delta;
        Position = new Vector3(Position.X + step.X, _eyeHeight, Position.Z + step.Z);
    }
}
