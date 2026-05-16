using Godot;

/// <summary>
/// Cyber Shogun character — extends BaseCharacter with sword and karate attack animations.
/// Converted from cyber_shogun.gd.
/// </summary>
public partial class CyberShogun : BaseCharacter
{
    // ─── Attack state ────────────────────────────────────────────────────────
    private bool _isAttacking = false;
    private bool _mouseClicked = false;
    private bool _qPressed = false;

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    public override void _Ready()
    {
        base._Ready();
        GD.Print("=== Cyber Shogun Initialized ===");
    }

    // ─── Input ───────────────────────────────────────────────────────────────
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                _mouseClicked = true;
        }
        else if (@event is InputEventKey key)
        {
            if (key.Keycode == Key.Q && key.Pressed && !key.Echo)
                _qPressed = true;
        }
    }

    // ─── Override: special actions ───────────────────────────────────────────
    protected override bool HandleSpecialActions()
    {
        if (IsOnFloor() && !_isAttacking)
        {
            if (_mouseClicked && Sprite2d.SpriteFrames.HasAnimation("sword_attack"))
            {
                _mouseClicked = false;
                Velocity = Velocity with { X = 0f };
                Sprite2d.Play("sword_attack");
                _ = PlayAttackAsync();   // fire-and-forget
                return true;
            }

            if (_qPressed && Sprite2d.SpriteFrames.HasAnimation("karate_attack"))
            {
                _qPressed = false;
                Velocity = Velocity with { X = 0f };
                Sprite2d.Play("karate_attack");
                _ = PlayAttackAsync();
                return true;
            }
        }

        // Consume stale input flags
        if (_mouseClicked) _mouseClicked = false;
        if (_qPressed)     _qPressed = false;

        return _isAttacking;
    }

    // ─── Async attack helper ─────────────────────────────────────────────────
    private async System.Threading.Tasks.Task PlayAttackAsync()
    {
        _isAttacking = true;
        await ToSignal(Sprite2d, AnimatedSprite2D.SignalName.AnimationFinished);
        _isAttacking = false;
    }
}

