using Godot;

namespace P0Expedition;

/// <summary>First-person walk-and-look controller. Exercises movement and physics per decision 0022.</summary>
public partial class Player : CharacterBody3D
{
    private const float WalkSpeed = 5.5f;
    private const float Gravity = 12f;
    private const float MouseSensitivity = 0.0025f;
    private const float MaxPitchDegrees = 85f;

    private Camera3D _camera = null!;
    private float _pitch;

    public static Player Create(Vector3 spawnPosition)
    {
        var player = new Player { Name = "Player", Position = spawnPosition };

        player.AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.8f },
            Position = new Vector3(0, 0.9f, 0),
        });

        var camera = new Camera3D { Name = "Camera3D", Position = new Vector3(0, 1.6f, 0), Current = true };
        player.AddChild(camera);
        player._camera = camera;

        return player;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion motion || Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            return;
        }

        RotateY(-motion.Relative.X * MouseSensitivity);
        _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, Mathf.DegToRad(-MaxPitchDegrees), Mathf.DegToRad(MaxPitchDegrees));
        _camera.Rotation = new Vector3(_pitch, 0, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        velocity.Y -= Gravity * (float)delta;

        var input = Vector2.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W)) input.Y -= 1;
        if (Input.IsPhysicalKeyPressed(Key.S)) input.Y += 1;
        if (Input.IsPhysicalKeyPressed(Key.A)) input.X -= 1;
        if (Input.IsPhysicalKeyPressed(Key.D)) input.X += 1;

        Vector3 direction = (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
        velocity.X = direction.X * WalkSpeed;
        velocity.Z = direction.Z * WalkSpeed;

        Velocity = velocity;
        MoveAndSlide();
    }
}
