using Godot;
using System;

/// <summary>Separate eight-direction, foot-collision exploration controller. Mission combat controllers are untouched.</summary>
public partial class ExplorationPlayer : CharacterBody2D
{
    public const float PixelsPerMetre=16, GroundAngle=.65f;
    public bool MovementEnabled { get; set; }
    public int Facing { get; private set; }
    private Sprite2D _sprite=null!;
    private string _operative="";
    private double _stride;
    /// <summary>Creates the small foot collider and angled-camera sprite at a common foot pivot.</summary>
    public override void _Ready()
    {
        MotionMode=MotionModeEnum.Floating; CollisionLayer=2; CollisionMask=1;
        AddChild(new CollisionShape2D { Shape=new CircleShape2D { Radius=5 } });
        _sprite=new Sprite2D { Hframes=4,Vframes=8,Offset=new(0,-23),Scale=Vector2.One*.65f };
        AddChild(_sprite);
        AddChild(new Camera2D { Position=new(0,-40),PositionSmoothingEnabled=false,Zoom=Vector2.One*.8f });
        RefreshOperative();
    }
    /// <summary>Loads only the currently selected operative atlas; all six share the same animation contract.</summary>
    public void RefreshOperative()
    {
        string active=OperativeRoster.Current.Active;
        if(_operative==active || _sprite==null) return;
        _operative=active;
        int index=Array.IndexOf(new[]{"Cyber Shogun","Oceanus","Apex Titan","Aura Phoenicia","Mirage Blade","Surya Prime"},active);
        string path=$"res://assets/world/operative_{Math.Max(0,index)}.png";
        if(ResourceLoader.Exists(path)) _sprite.Texture=GD.Load<Texture2D>(path);
    }
    /// <summary>Moves at 4/8 metres per second in logical ground space, compressing only the rendered north/south axis.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if(!IsInsideTree()) return;
        Vector2 direction=MovementEnabled?Input.GetVector("world_left","world_right","world_up","world_down"):Vector2.Zero;
        float speed=Input.IsActionPressed("world_run")?8:4;
        Velocity=new(direction.X, direction.Y*GroundAngle); Velocity*=speed*PixelsPerMetre;
        MoveAndSlide();
        if(direction.LengthSquared()>0)
        {
            // Atlas rows rotate the south-facing Blender rig clockwise in world space, so east uses row six.
            Facing=((int)Math.Round(Math.Atan2(-direction.X,direction.Y)/(Math.PI/4))+8)%8;
            _stride+=delta*(speed==8?12:7);
        }
        if(_sprite!=null) _sprite.Frame=Facing*4+(direction.LengthSquared()>0?1+(int)_stride%3:0);
    }
    /// <summary>Projects a small logical offset to the angled ground plane; never use this on absolute world coordinates.</summary>
    public static Vector2 Project(double x,double y) => new((float)x*PixelsPerMetre,(float)y*PixelsPerMetre*GroundAngle);
}
