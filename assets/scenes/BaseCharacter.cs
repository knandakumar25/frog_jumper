using Godot;

/// <summary>
/// Base class for all playable characters. Provides shared physics, animation and
/// movement logic. Subclasses override HandleSpecialActions() for character-specific
/// behaviour (e.g. attacks).
/// Converted from base_character.gd.
/// </summary>
public partial class BaseCharacter : CharacterBody2D
{
    // ─── Constants ───────────────────────────────────────────────────────────
    protected const float Speed = 400.0f;
    protected const float JumpVelocity = -1000.0f;

    // ─── State ───────────────────────────────────────────────────────────────
    protected bool IsMoving = true;

    // ─── Node references ─────────────────────────────────────────────────────
    protected AnimatedSprite2D Sprite2d;
    protected AudioStreamPlayer JumpSound;

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    public override void _Ready()
    {
        Sprite2d   = GetNode<AnimatedSprite2D>("Sprite2D");
        JumpSound  = GetNode<AudioStreamPlayer>("JumpSound");

        GD.Print("=== BaseCharacter Debug ===");
        GD.Print("Character instance created: ", Name);
        GD.Print("Sprite2D node: ", Sprite2d);

        if (Sprite2d != null)
        {
            GD.Print("Sprite frames: ", Sprite2d.SpriteFrames);
            GD.Print("Current animation: ", Sprite2d.Animation);
            GD.Print("Sprite visible: ", Sprite2d.Visible);
            GD.Print("Sprite modulate: ", Sprite2d.Modulate);

            if (Sprite2d.SpriteFrames != null)
            {
                GD.Print("Available animations: ", Sprite2d.SpriteFrames.GetAnimationNames());
                Sprite2d.Visible = true;
                Sprite2d.Modulate = new Color(1, 1, 1, 1);
                Sprite2d.Play("idle");
                GD.Print("Started playing 'idle' animation");
            }
        }

        GD.Print("Character position: ", GlobalPosition);
        GD.Print("Character scale: ", Scale);
        GD.Print("===========================");

        // Periodic debug timer
        var timer = new Timer();
        AddChild(timer);
        timer.Timeout += () =>
        {
            if (Sprite2d?.SpriteFrames != null)
                GD.Print("[", Time.GetTicksMsec(), "] Sprite playing: ",
                    Sprite2d.IsPlaying(), " | Animation: ", Sprite2d.Animation,
                    " | Frame: ", Sprite2d.Frame);
        };
        timer.WaitTime = 2.0;
        timer.Start();
    }

    // ─── Physics ─────────────────────────────────────────────────────────────
    public override void _PhysicsProcess(double delta)
    {
        var global = GetNode<Node>("/root/Global");
        if ((bool)global.Get("reached_finish"))
            Sprite2d.Animation = "idle";

        if (!IsMoving)
        {
            Sprite2d.Animation = "finished";
            return;
        }

        // Character-specific actions (overridden in subclasses)
        if (HandleSpecialActions()) return;

        // Animations
        var prevAnim = Sprite2d.Animation;
        Sprite2d.Animation = (Velocity.X > 1f || Velocity.X < -1f) ? "running" : "default";

        if (!IsOnFloor())
        {
            Velocity += GetGravity() * (float)delta;
            Sprite2d.Animation = "jumping";
        }

        if (prevAnim != Sprite2d.Animation)
            GD.Print("Animation changed: ", prevAnim, " -> ", Sprite2d.Animation);

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            Velocity = Velocity with { Y = JumpVelocity };
            JumpSound.Play();
        }

        float direction = Input.GetAxis("left", "right");
        if (!Mathf.IsZeroApprox(direction))
            Velocity = Velocity with { X = direction * Speed };
        else
            Velocity = Velocity with { X = Mathf.MoveToward(Velocity.X, 0f, 12f) };

        MoveAndSlide();
        Sprite2d.FlipH = Velocity.X < 0;
    }

    // ─── Virtual / override point ─────────────────────────────────────────────
    /// <summary>
    /// Override in subclasses to handle character-specific actions.
    /// Return true to suppress normal movement processing for this frame.
    /// </summary>
    protected virtual bool HandleSpecialActions() => false;

    // ─── Public API ──────────────────────────────────────────────────────────
    public virtual void StopMovement() => IsMoving = false;

    // GDScript-compatible alias (called by finish.gd via body.stop_movement())
    public void stop_movement() => StopMovement();
}

