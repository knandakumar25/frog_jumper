using Godot;

/// <summary>Non-scoring holographic practice target; assessment variants patrol and deal modest contact damage.</summary>
public partial class TrainingTarget : StaticBody2D
{
    /// <summary>True for assessment drones; false for endlessly resettable practice targets.</summary>
    public bool Drone { get; set; }
    /// <summary>Assessment targets have finite health; ordinary targets remain available for retries.</summary>
    public float Health { get; private set; } = 60;
    /// <summary>Raised once when an assessment drone is destroyed.</summary>
    public event System.Action? Defeated;
    private float _time, _flash;
    private Vector2 _origin;

    /// <summary>Builds a non-blocking hurtbox on collision layer two.</summary>
    public override void _Ready()
    {
        CollisionLayer = 2; CollisionMask = 0;
        _origin=Position;
        AddChild(new CollisionShape2D { Shape=new RectangleShape2D {Size=new Vector2(52,70)} });
    }

    /// <summary>Animates hologram rings and patrol motion; contact damage never writes campaign statistics.</summary>
    public override void _PhysicsProcess(double delta)
    {
        _time+=(float)delta; _flash=Mathf.Max(0,_flash-(float)delta);
        if (Drone)
        {
            Position=_origin+new Vector2(Mathf.Sin(_time)*45,Mathf.Sin(_time*2)*5);
            var player=GetParent().GetNodeOrNull<CyberShogun>("Player");
            if(player!=null && Position.DistanceTo(player.Position)<60) player.TakeDamage(8);
        }
        QueueRedraw();
    }

    /// <summary>Accepts real melee collision damage; practice targets cannot be exhausted.</summary>
    public void TakeDamage(float amount)
    {
        if(Health<=0 || amount<=0) return;
        _flash=.15f;
        if(!Drone) return;
        Health=Mathf.Max(0,Health-amount);
        if(Health<=0) { Defeated?.Invoke(); QueueFree(); }
    }

    /// <summary>Draws a readable 2D training hologram with a visible hit response.</summary>
    public override void _Draw()
    {
        var color=_flash>0?Colors.White:new Color(.2f,.9f,1f);
        DrawCircle(Vector2.Zero,30,new Color(.03f,.12f,.2f));
        DrawArc(Vector2.Zero,29,0,Mathf.Tau,32,color,2,true);
        DrawLine(new Vector2(-38,0),new Vector2(38,0),color,3);
        DrawLine(new Vector2(0,-38),new Vector2(0,38),color,3);
        DrawCircle(Vector2.Zero,8,Drone?new Color(1,.6f,.2f):color);
        if(Drone) DrawLine(new Vector2(-30,-45),new Vector2(-30+Health,-45),color,4);
    }
}
