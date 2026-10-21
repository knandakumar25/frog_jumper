using Godot;

/// <summary>Draws approved hand-drawn poses on the real physics body, without preview motion offsets.</summary>
public partial class TrainingArtwork : Node2D
{
    private CyberShogun _player = null!;
    private AnimatedSprite2D _source = null!;
    private OperativeStudyAtlas? _art;
    private Texture2D? _poses, _stride;
    private string _state = "";
    private float _time;
    private readonly Rect2[] _shogun = {
        new(40,70,240,260),new(360,70,220,260),new(680,70,230,260),new(990,70,240,260),
        new(20,400,260,230),new(350,400,260,230),new(660,420,260,210),new(990,390,240,235),
        new(20,670,270,260),new(350,700,320,230),new(690,680,230,250),new(970,680,270,250),
        new(20,1010,330,200),new(370,950,270,280),new(680,950,250,280),new(950,1010,300,244)
    };

    /// <summary>Loads the selected operative's original illustrated sheets, retaining the hidden animation driver.</summary>
    public override void _Ready()
    {
        _player = GetParent<CyberShogun>();
        _source = _player.GetNode<AnimatedSprite2D>("Sprite2D");
        _source.Visible = false;
        if (_player.ActiveProfile.Id == "cyber_shogun")
        {
            _poses = GD.Load<Texture2D>("res://assets/characters/shogun_study/poses.png");
            _stride = GD.Load<Texture2D>("res://assets/characters/shogun_study/locomotion.png");
        }
        else _art = OperativeStudyAtlas.Load(_player.ActiveProfile.Id);
    }

    /// <summary>Resets pose time on actual state changes; movement comes exclusively from physics.</summary>
    public override void _Process(double delta)
    {
        string next = _player.ActiveAction != "idle" ? _player.ActiveAction :
            !_player.IsOnFloor() ? "jump" : Mathf.Abs(_player.Velocity.X) > 5 ? "run" : "idle";
        if (next != _state) { _state = next; _time = 0; }
        _time += (float)delta;
        Modulate = _source.Modulate;
        QueueRedraw();
    }

    /// <summary>Uses stable row baselines and horizontal cell anchors to avoid silhouette recentering.</summary>
    public override void _Draw()
    {
        if (_source == null) return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_source.FlipH ? -1 : 1, 1));
        const float feet = 34;
        if (_art != null)
        {
            int pose = _state switch {
                "run" => 5 + (int)(_time * 6) % 4, "jump" => 9,
                "primary" => _time < .15f ? 10 : 11, "secondary" => 12, "dash" => 13, _ => 0 };
            var r = _art.Regions[pose];
            float scale = 104 / _art.StandingHeight;
            float center = (pose % 4 + .5f) * _art.Texture.GetWidth() / 4;
            DrawTextureRectRegion(_art.Texture, new Rect2(
                (r.Position.X-center)*scale, feet+(r.Position.Y-_art.Baselines[pose/4])*scale,
                r.Size.X*scale,r.Size.Y*scale),r);
        }
        else if (_poses != null && _stride != null)
        {
            if (_state == "run")
            {
                int frame = 8 + (int)(_time*12)%8, row=frame/4;
                float[] tops={40,370,700,975}, heights={310,310,270,279}, baselines={300,300,260,245};
                float width=_stride.GetWidth()/4f, scale=104/260f;
                DrawTextureRectRegion(_stride,new Rect2(-width*scale/2,feet-baselines[row]*scale,width*scale,heights[row]*scale),
                    new Rect2(frame%4*width,tops[row],width,heights[row]));
            }
            else
            {
                int pose=_state switch {"jump"=>7,"primary"=>_time<.15f?8:9,
                    "secondary"=>_time<.15f?10:11,"dash"=>12,_=>0};
                var r=_shogun[pose];
                float scale=104/260f;
                // Explicit source regions preserve scarf and weapon extents.
                float center=(pose%4+.5f)*_poses.GetWidth()/4f;
                float baseline=(pose/4) switch {0=>330,1=>630,2=>930,_=>1230};
                DrawTextureRectRegion(_poses,new Rect2((r.Position.X-center)*scale,
                    feet+(r.Position.Y-baseline)*scale,r.Size.X*scale,r.Size.Y*scale),r);
            }
        }
    }
}
