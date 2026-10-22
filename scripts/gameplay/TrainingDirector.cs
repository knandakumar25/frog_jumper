using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Protocol Zero: seven checkpointed, non-scoring lessons driven by real physics and combat events.
/// Room geometry uses a 1280x720 design canvas, uniformly fitted to the current viewport.
/// No SessionManager or campaign finish handler is invoked by this scene.
/// </summary>
public partial class TrainingDirector : Node2D
{
    /// <summary>Current zero-based lesson, retained on local retries.</summary>
    public int Lesson { get; private set; }
    /// <summary>True only when the current lesson's actual success conditions have been met.</summary>
    public bool DoorOpen { get; private set; }
    /// <summary>Read-only access for runtime integration tests and HUD telemetry.</summary>
    public CyberShogun Player => _player;
    /// <summary>Read-only evidence for integration diagnostics; never modifies lesson completion.</summary>
    public string ObjectiveEvidence => $"moves={string.Join(",",_moves)} hits={string.Join(",",_hits)} drones={_defeated}";
    private CyberShogun _player = null!;
    private Node2D _world = null!;
    private Control _ui = null!;
    private Camera2D _camera = null!;
    private Label _heading = null!, _objective = null!, _telemetry = null!, _hint = null!;
    private PanelContainer _menu = null!;
    private Button _resume = null!;
    private readonly List<Rect2> _platforms = new();
    private readonly HashSet<string> _moves = new(), _hits = new();
    private bool _paused, _complete, _transition, _pulse;
    private int _dashCount, _defeated;
    private float _elapsed, _ambient;
    private Button _continue = null!, _retry = null!, _skip = null!;
    private string _device = "keyboard";
    private readonly string[] _titles = {"Arrival bay","Jump course","Mobility shaft","Strike range","Dash lane","Shield laboratory","Field assessment"};
    private readonly string[] _objectives = {
        "Welcome to Protocol Zero. Reach the amber marker and come to a stop.",
        "Jump across the raised platforms. Land on the final marked platform.",
        "Jump against the cyan wall. Slide, then jump away to reach the upper walkway.",
        "Land a primary and a secondary strike. Then connect two hits within two seconds.",
        "Dash through the hologram. Wait for recharge, then dash into the amber zone.",
        "Enter the pulse zone. After the shield absorbs the pulse, recover in the cyan alcove.",
        "Reach the upper ledge with a wall jump, dash through the target, and defeat both drones."
    };
    private readonly string[] _hints = {
        "Release movement over the amber floor marker. There is no time limit.",
        "Jump again only after landing. The lower recovery floor lets you try again.",
        "Hold toward the wall while falling. Press jump again to push away; land on the left ledge.",
        "Stand close and face the hologram. Use both melee buttons, then strike twice in quick succession.",
        "Dash starts on the ground and follows your facing. A second dash is available when the meter reaches zero.",
        "The pulse fires only once. Move into the safe alcove and wait for your shield to refill.",
        "Use the skills you learned. The upper walkway returns to the combat floor. Retry restores this room only."
    };

    /// <summary>Initializes a self-contained training session with no campaign persistence side effects.</summary>
    public override void _Ready()
    {
        var global=GetNodeOrNull<Node>("/root/Global");
        global?.Set("reached_finish",false);
        global?.Call("stop_menu_music");
        global?.Call("set_control_prompt_visible",false);
        GetNodeOrNull<Node>("/root/GameManager")?.Call("set_game_state",2);
        _camera=new Camera2D {Position=new Vector2(640,360)}; AddChild(_camera);
        BuildUi();
        BuildRoom();
        GetViewport().SizeChanged += FitCanvas;
        FitCanvas();
    }

    /// <summary>Fits the complete course and HUD without stretching artwork or hiding buttons.</summary>
    private void FitCanvas()
    {
        Vector2 viewport=GetViewportRect().Size;
        float scale=Mathf.Min(viewport.X/1280,viewport.Y/720);
        // Camera zoom changes presentation without scaling physics or jump distances.
        _camera.Zoom=Vector2.One*scale;
    }

    /// <summary>Builds or retries one checkpoint; completed lessons remain intact.</summary>
    private void BuildRoom()
    {
        if(_world!=null) { RemoveChild(_world); _world.QueueFree(); }
        _world=new Node2D {Name="Room"}; AddChild(_world); MoveChild(_world,0);
        _platforms.Clear(); _moves.Clear(); _hits.Clear();
        _elapsed=0; _pulse=false; _dashCount=0; _defeated=0; DoorOpen=false; _transition=false;
        Platform(0,600,1280,120); Platform(-30,0,30,720); Platform(1280,0,30,720);
        if(Lesson==1)
        {
            Platform(270,500,170,22); Platform(490,400,170,22); Platform(730,310,190,22);
        }
        if(Lesson is 2 or 6)
        {
            Platform(360,450,160,22); Platform(620,350,60,250);
            Platform(680,350,Lesson==2?530:140,22);
        }
        _player=GD.Load<PackedScene>("res://scenes/gameplay/cyber_shogun.tscn").Instantiate<CyberShogun>();
        _player.Name="Player"; _player.Scale=Vector2.One; _player.Position=new Vector2(140,550);
        _player.ExternalRespawn=true;
        // The hurtbox layer does not block travel; attacks explicitly query layer two.
        _player.GetNode<Area2D>("AttackArea").CollisionMask=3;
        _world.AddChild(_player);
        _player.AddChild(new TrainingArtwork {Name="HandDrawnArtwork"});
        _player.GetNode<PointLight2D>("OperativeAuraLight").Visible=false;
        _player.GetNode<AudioStreamPlayer>("JumpSound").VolumeDb=-12;
        _player.ActionExecuted+=OnAction;
        _player.AttackLanded+=OnHit;
        _player.PlayerDefeated+=RetryRoom;
        if(Lesson==3) { Target(530,false); Target(760,false); }
        if(Lesson==4) Target(510,false);
        if(Lesson==6) { Target(880,false); Target(1030,true); Target(1160,true); }
        _heading.Text=$"PROTOCOL ZERO  /  {Lesson+1:00} — {Translate("training.room."+Lesson,_titles[Lesson])}";
        _objective.Text="AXIOM-7  /  "+Translate("training.objective."+Lesson,_objectives[Lesson]);
        _menu.Visible=false; _retry.Visible=true; _skip.Visible=true; _continue.Text=Translate("training.menu.Continue","Skip training"); _paused=false; _complete=false;
        _world.ProcessMode=ProcessModeEnum.Inherit;
        _ui.GetViewport().GuiReleaseFocus();
        QueueRedraw();
    }

    /// <summary>Adds one solid platform and records the same rectangle for the visible surface.</summary>
    private void Platform(float x,float y,float width,float height)
    {
        var rect=new Rect2(x,y,width,height); _platforms.Add(rect);
        var body=new StaticBody2D {Position=rect.GetCenter()};
        body.AddChild(new CollisionShape2D {Shape=new RectangleShape2D {Size=rect.Size}});
        _world.AddChild(body);
    }

    /// <summary>Adds either a repeatable dummy or a finite-health assessment drone.</summary>
    private void Target(float x,bool drone)
    {
        var target=new TrainingTarget {Position=new Vector2(x,557),Drone=drone};
        target.Defeated+=()=>_defeated++;
        _world.AddChild(target);
    }

    /// <summary>Records executed actions, never raw key presses; wall slides may report across several ticks.</summary>
    private void OnAction(string action)
    {
        _moves.Add(action);
        if(action=="dash") _dashCount++;
    }

    /// <summary>Records attack identities only after the controller confirms a damageable collision.</summary>
    private void OnHit(string action) => _hits.Add(action);

    /// <summary>Evaluates lesson objectives, pulse timing, safe recovery and extraction at physics cadence.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if(_paused || _complete || _transition || _player==null) return;
        _elapsed+=(float)delta;
        var p=_player.Position;
        if(p.Y>780 || _player.CurrentHealth<=0) { RetryRoom(); return; }
        if(Lesson==5 && !_pulse && _elapsed>2 && p.X>430 && p.X<700)
        {
            _player.TakeDamage(20);
            _pulse=_player.CurrentShield<_player.MaxShield;
        }
        bool grounded=_player.IsOnFloor();
        bool success=Lesson switch {
            0=>grounded && p.X>890 && p.X<1050 && Mathf.Abs(_player.Velocity.X)<5,
            1=>_moves.Contains("jump") && grounded && p.X>730 && p.X<920 && p.Y<320,
            2=>_moves.Contains("wall_slide") && _moves.Contains("wall_jump") && grounded && p.X>720 && p.Y<360,
            3=>_hits.Contains("primary") && _hits.Contains("secondary") && _player.ComboCount>=2,
            4=>_hits.Contains("dash") && _dashCount>=2 && p.X>900 && p.X<1100 && _player.ActiveAction=="dash",
            5=>_pulse && p.X>800 && _player.CurrentShield>=_player.MaxShield,
            6=>_moves.Contains("wall_jump") && _moves.Contains("upper_ledge") && _hits.Contains("dash") && _defeated>=2,
            _=>false
        };
        if(Lesson==6 && grounded && p.X>680 && p.X<820 && p.Y<360) _moves.Add("upper_ledge");
        if(success) DoorOpen=true;
        if(DoorOpen && p.X>1150 && grounded) AdvanceRoom();
    }

    /// <summary>Updates readable controls and telemetry, with expanded help after fifteen seconds.</summary>
    public override void _Process(double delta)
    {
        if(!_paused) _ambient+=(float)delta;
        if(_player==null) return;
        _telemetry.Text=$"{_player.ActiveProfile.DisplayName}    HP {_player.CurrentHealth:0}/{_player.MaxHealth:0}    SHIELD {_player.CurrentShield:0}/{_player.MaxShield:0}    DASH {_player.DashCooldownTimer:0.0}s";
        string controls=_device=="pad"?"MOVE  Stick / D-pad    JUMP  A    MELEE  X / Y    DASH  RB    PAUSE  Start":
            $"MOVE  {KeyboardBinding("left")} / {KeyboardBinding("right")}    JUMP  {KeyboardBinding("jump")}    MELEE  {KeyboardBinding("attack")} / Q    DASH  Shift / C    PAUSE  Esc";
        _hint.Text=DoorOpen?Translate("training.exit","Lesson complete. Proceed through the cyan exit on the right."):
            (_elapsed>15?Translate("training.hint."+Lesson,_hints[Lesson])+"\n":"")+controls;
        QueueRedraw();
    }

    /// <summary>Reads configured keyboard mappings so remapped movement prompts stay accurate.</summary>
    private string KeyboardBinding(string action)
    {
        var names=new List<string>();
        foreach(var input in InputMap.ActionGetEvents(action))
            if(input is InputEventKey key)
                names.Add(OS.GetKeycodeString(key.PhysicalKeycode!=Key.None?key.PhysicalKeycode:key.Keycode));
        return string.Join("/",names);
    }

    /// <summary>Switches control hints by device and toggles pause without consuming combat buttons.</summary>
    public override void _Input(InputEvent input)
    {
        if(input is InputEventJoypadButton or InputEventJoypadMotion) _device="pad";
        else if(input is InputEventKey) _device="keyboard";
        bool pause=input is InputEventKey key && key.Pressed && !key.Echo && key.Keycode==Key.Escape
            || input is InputEventJoypadButton button && button.Pressed && button.ButtonIndex==JoyButton.Start;
        if(pause && !_complete) { SetPaused(!_paused); GetViewport().SetInputAsHandled(); }
    }

    /// <summary>Schedules checkpoint reconstruction outside collision callbacks.</summary>
    public void RetryRoom()
    {
        if(_transition) return;
        _transition=true;
        Callable.From(BuildRoom).CallDeferred();
    }

    /// <summary>Moves through the course once, or shows the final non-scoring completion choices.</summary>
    private void AdvanceRoom()
    {
        if(Lesson<6) { Lesson++; RetryRoom(); }
        else
        {
            _complete=true; _paused=true; _world.ProcessMode=ProcessModeEnum.Disabled;
            _menu.Visible=true; _resume.Visible=false; _retry.Visible=false; _skip.Visible=false;
            _continue.Text=Translate("training.begin","Begin Mission 1");
            _heading.Text=Translate("training.complete","PROTOCOL ZERO COMPLETE");
            _objective.Text=Translate("training.complete.body","Calibration complete. You are ready for your first mission.");
            _menu.GetNode<VBoxContainer>("Options").GetNode<Button>("Continue").GrabFocus();
        }
    }

    /// <summary>Pauses the room only; focus stays in the keyboard/controller accessible menu.</summary>
    private void SetPaused(bool paused)
    {
        _paused=paused; _world.ProcessMode=paused?ProcessModeEnum.Disabled:ProcessModeEnum.Inherit;
        _menu.Visible=paused; _resume.Visible=true; _skip.Visible=!paused;
        if(paused) _resume.GrabFocus(); else GetViewport().GuiReleaseFocus();
    }

    /// <summary>Builds anchored readable HUD elements and explicit vertical menu focus order.</summary>
    private void BuildUi()
    {
        _ui=new Control {Name="TrainingUI",Size=new Vector2(1280,720),MouseFilter=Control.MouseFilterEnum.Ignore}; AddChild(_ui);
        _heading=LabelAt(new Vector2(40,26),new Vector2(1200,40),27);
        _objective=LabelAt(new Vector2(40,75),new Vector2(1200,70),21);
        _telemetry=LabelAt(new Vector2(40,157),new Vector2(1200,32),17);
        _hint=LabelAt(new Vector2(40,630),new Vector2(1200,75),18);
        _menu=new PanelContainer {Name="PauseOptions",Position=new Vector2(430,210),Size=new Vector2(420,360)};
        var style=new StyleBoxFlat {BgColor=new Color(.025f,.05f,.1f),BorderColor=new Color(.2f,.8f,1f)};
        style.SetBorderWidthAll(2); style.ContentMarginLeft=20; style.ContentMarginRight=20;
        style.ContentMarginTop=16; style.ContentMarginBottom=16;
        _menu.AddThemeStyleboxOverride("panel",style); _ui.AddChild(_menu);
        var options=new VBoxContainer {Name="Options"}; options.AddThemeConstantOverride("separation",12); _menu.AddChild(options);
        _resume=MenuButton(options,"Resume","Resume",()=>SetPaused(false));
        _retry=MenuButton(options,"Retry","Retry room",RetryRoom);
		_continue=MenuButton(options,"Continue","Skip training",()=>Leave("res://scenes/gameplay/level1.tscn"));
        MenuButton(options,"Repeat","Repeat training",()=>{Lesson=0; RetryRoom();});
        MenuButton(options,"MainMenu","Main menu",()=>Leave("res://scenes/main_menu/main_menu.tscn"));
        _skip=new Button {Name="SkipTraining",Text=Translate("training.skip","Skip training"),Position=new Vector2(1060,200),Size=new Vector2(180,38)};
		_skip.Pressed+=()=>Leave("res://scenes/gameplay/level1.tscn"); _ui.AddChild(_skip);
    }

    /// <summary>Creates a wrapping label with contrast appropriate for the dark facility walls.</summary>
    private Label LabelAt(Vector2 position,Vector2 size,int fontSize)
    {
        var label=new Label {Position=position,Size=size,AutowrapMode=TextServer.AutowrapMode.WordSmart,MouseFilter=Control.MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",fontSize); label.AddThemeColorOverride("font_color",new Color(.8f,.93f,1));
        _ui.AddChild(label); return label;
    }

    /// <summary>Adds a focusable menu action with localizable text and sufficient target size.</summary>
    private Button MenuButton(VBoxContainer options,string name,string text,Action action)
    {
        var button=new Button {Name=name,Text=Translate("training.menu."+name,text),CustomMinimumSize=new Vector2(380,46)};
        button.Pressed+=action; options.AddChild(button); return button;
    }

    /// <summary>Returns English until a translation for the stable training key has been supplied.</summary>
    private string Translate(string key,string fallback)
    {
        string translated=Tr(key); return translated==key?fallback:translated;
    }

    /// <summary>Uses the existing scene loader, releasing local pause and never saving training scores.</summary>
    private void Leave(string path)
    {
        if(_transition) return;
        _transition=true;
        GetNodeOrNull<Node>("/root/GameManager")?.Call("set_game_state",path.Contains("main_menu.tscn")?0:1);
        LoadingScreen.LoadScene(GetTree(),path);
    }

    /// <summary>Draws layered flat facility architecture, animated scanners, legible platforms and objective markers.</summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(-2000,-2000,5280,4720),new Color(.012f,.024f,.05f));
        DrawRect(new Rect2(0,0,1280,720),new Color(.025f,.045f,.085f));
        for(int i=0;i<9;i++)
        {
            float x=i*160;
            DrawRect(new Rect2(x+12,215,128,360),new Color(.035f,.07f,.115f));
            DrawLine(new Vector2(x+20,230),new Vector2(x+20,570),new Color(.08f,.16f,.23f),2);
            float scan=260+Mathf.PosMod(_ambient*25+i*33,270);
            DrawLine(new Vector2(x+28,scan),new Vector2(x+112,scan),new Color(.1f,.35f,.45f,.4f),2);
        }
        foreach(var rect in _platforms)
        {
            DrawRect(rect,new Color(.075f,.12f,.18f));
            DrawLine(rect.Position,rect.Position+new Vector2(rect.Size.X,0),new Color(.3f,.8f,.9f),3);
            for(float x=rect.Position.X+8;x<rect.End.X;x+=32)
                DrawLine(new Vector2(x,rect.Position.Y+8),new Vector2(x+12,rect.Position.Y+8),new Color(.13f,.24f,.3f),2);
        }
        var amber=new Color(1,.65f,.22f);
        if(Lesson==0 || Lesson==4) DrawRect(new Rect2(900,594,150,6),amber);
        if(Lesson==1) DrawRect(new Rect2(730,304,190,6),amber);
        if(Lesson==5)
        {
            DrawRect(new Rect2(430,245,270,350),new Color(amber,_pulse?.06f:.16f));
            DrawRect(new Rect2(800,594,250,6),new Color(.2f,1,.8f));
        }
        DrawRect(new Rect2(1215,Lesson==2?245:495,35,105),new Color(DoorOpen?new Color(.2f,1,.8f):amber,.25f));
        DrawLine(new Vector2(1230,Lesson==2?265:515),new Vector2(1230,Lesson==2?330:580),DoorOpen?new Color(.2f,1,.8f):amber,4);
        for(int i=0;i<7;i++) DrawRect(new Rect2(40+i*32,140,24,4),i<=Lesson?new Color(.2f,1,.8f):new Color(.15f,.2f,.3f));
    }

    /// <summary>Disconnects viewport callbacks before this scene is released.</summary>
    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= FitCanvas;
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible",true);
    }
}
