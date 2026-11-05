using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>Six-sector infiltration and identity recovery. Records remain outside combat; finite defenders and exact prerequisites drive every recovery stage.</summary>
public partial class NamesWithoutNumbersMission : CodeLevel, IFiniteCombatHost
{
    /// <summary>Checkpoint-persistent scene ledger; replay never shares this instance.</summary>
    public NamesProgress Progress { get; } = new();
    /// <inheritdoc/>
    public CyberShogun CombatPlayer => Player;
    /// <inheritdoc/>
    public bool GameplaySuspended => _recovering || MissionComplete || !IsInsideTree() || GetTree().Paused;
    private readonly List<WildfireEncounter> _encounters = new();
    private readonly List<RecoveryTerminal> _terminals = new();
    private readonly List<(PalacePlatform Wall, string Event)> _seals = new();
    private readonly List<(Node Node, ProcessModeEnum Mode)> _recoveryModes = new();
    private readonly List<Sprite2D> _civilianGates = new();
    private NamesDialogue _dialogue = null!;
    private AudioStreamPlayer _accepted = null!;
    private bool _recovering;
    private int _sector = -1;
    private Vector2 _previous;
    /// <inheritdoc/>
    protected override int MissionNumber => 5;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0,-900,28000,1900);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200,554);
    /// <inheritdoc/>
    protected override Color PlatformTint => new(.65f,.74f,.83f);
    /// <inheritdoc/>
    protected override string MusicPath => "res://assets/audio/names/atmosphere.ogg";
    /// <inheritdoc/>
    protected override string TitleCardTitle(string narrativeTitle) => WildfireText.Get("NAMES_TITLE", LevelCatalog.Get(5).Title);
    /// <inheritdoc/>
    protected override string TitleCardObjective(string narrativeObjective) => WildfireText.Get("NAMES_BRIEFING", "Recover original identity records. Stop exclusion. Restore access.");
    /// <inheritdoc/>
    protected override string IntroLine() => WildfireText.Get("NAMES_INTRO", "MIRAGE: Service passage below the processing hall. White scanner light burns; wait in the dark recesses.");
    /// <summary>Boundary checkpoints require the completed preceding sector. Core recovery restarts at its entrance.</summary>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[] {
        ("Service",new Vector2(500,600)),("Hall",new Vector2(4300,600)),("Utilities",new Vector2(8800,600)),
        ("Records",new Vector2(12800,600)),("Core",new Vector2(19800,600)),("District",new Vector2(24500,600)) };
    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[] { new Vector2(2650,400),new Vector2(10700,400),new Vector2(17600,300) };
    /// <inheritdoc/>
    protected override string FragmentLog(int found)
    {
        string text=WildfireText.Get("NAMES_FRAGMENT_"+found,found switch {
        1 => "CIVILIAN TESTIMONY: My daughter wrote our names inside her coat. The scanner called us a number that no longer existed.",
        2 => "CIVILIAN TESTIMONY: I repaired these water pipes for thirty years. Yesterday the district said I had never lived here.",
        _ => "CIVILIAN TESTIMONY: Keep the original. That was what my mother said. A name is more than permission to pass." });
        return text;
    }
    /// <summary>Wide stationary ground, 100px archive stairs and optional galleries use conservative weakest-operative limits.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-100,-900,100,2000,PalaceSurface.Wall);
        AddPlatform(0,600,28000,900,PalaceSurface.Concrete,false,"DistrictFloor");
        AddPlatform(28000,-900,100,2000,PalaceSurface.Wall);

        // Architectural wall columns for acrobatic wall-sliding and wall-jumping
        foreach(float wx in new[]{1200f, 1380f, 4800f, 7400f, 9600f, 11400f, 21000f, 23000f})
            AddPlatform(wx, 260, 24, 340, PalaceSurface.Wall, false);

        // Elevated one-way catwalks and service gantries across all 6 sectors
        // Sector 1: Service Duct overhead conduits
        AddPlatform(800, 430, 360, 22, PalaceSurface.Steel, true);
        AddPlatform(1600, 410, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(3200, 420, 380, 22, PalaceSurface.Steel, true);

        // Sector 2: Processing Hall inspection walkways
        AddPlatform(4600, 430, 440, 22, PalaceSurface.Steel, true);
        AddPlatform(5800, 410, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(7100, 430, 460, 22, PalaceSurface.Steel, true);

        // Sector 3: Utilities & Coolant infrastructure conduits
        AddPlatform(9100, 430, 380, 22, PalaceSurface.Steel, true);
        AddPlatform(9800, 340, 360, 22, PalaceSurface.Steel, true);
        AddPlatform(11200, 420, 420, 22, PalaceSurface.Steel, true);

        // Sector 4: Archive mezzanine balconies
        AddPlatform(14100, 330, 420, 22, PalaceSurface.Steel, true);
        AddPlatform(16600, 250, 400, 22, PalaceSurface.Steel, true);
        AddPlatform(18500, 380, 420, 22, PalaceSurface.Steel, true);

        // Sector 5: Core Vault fortified vantage platforms
        AddPlatform(20700, 440, 400, 22, PalaceSurface.Steel, true);
        AddPlatform(21800, 370, 460, 22, PalaceSurface.Steel, true);
        AddPlatform(22900, 440, 420, 22, PalaceSurface.Steel, true);

        // Sector 6: Restored District liberation bridges
        AddPlatform(25200, 430, 400, 22, PalaceSurface.Steel, true);
        AddPlatform(26300, 430, 400, 22, PalaceSurface.Steel, true);

        // Primary progression steps and platforms
        foreach(float x in new[]{2400f,10450,17350})
        {
            AddPlatform(x,500,180,100,PalaceSurface.Steel);
            AddPlatform(x+180,400,400,22,PalaceSurface.Steel,true);
        }

        // Each required archive room approach: low cover, raised gallery, then descending utility stairs
        foreach(var route in new[]{(13200f,2),(15300f,4),(17600f,1)})
        {
            for(int i=0;i<route.Item2;i++) AddPlatform(route.Item1+i*180,500-i*100,180,100+i*100,PalaceSurface.Steel,false);
            AddPlatform(route.Item1+route.Item2*180,600-route.Item2*100,650,route.Item2*100,PalaceSurface.Steel,false);
            for(int i=0;i<route.Item2;i++) AddPlatform(route.Item1+route.Item2*180+650+i*180,600-(route.Item2-i-1)*100,180,System.Math.Max(22,(route.Item2-i-1)*100),PalaceSurface.Steel,false);
        }

        // Ground tactical cover blocks
        foreach(float x in new[]{2200f,3100,5100,6500,7600,9400,10100,11500,13400,14500,16300,18200,20700,22500,23500})
            AddPlatform(x,520,110,80,PalaceSurface.Concrete);

        Seal(4150,"phase");Seal(8600,"signal");Seal(12500,"maintenance");
        Seal(15100,"record:1");Seal(17400,"record:2");Seal(19500,"record:3");Seal(24200,"defense:3");Seal(27400,"commit");
    }
    /// <summary>Full-height barriers make the objective chain mandatory while leaving record equipment untouched.</summary>
    private void Seal(float x,string id) => _seals.Add((AddPlatform(x,-900,24,1500,PalaceSurface.Gate,false,"Seal"+id.Replace(':','_')),id));
    /// <summary>Only proximity and the existing context action can operate a terminal; combat cannot destroy originals.</summary>
    private void Terminal(string id,string caption,Vector2 position)
    {
        var terminal=new RecoveryTerminal { Name="Terminal"+id.Replace(':','_'),Objective=id,Caption=caption,Position=position,Mission=this };
        _terminals.Add(terminal);Actors.AddChild(terminal);
    }
    /// <summary>Ability devices check exact defender prerequisites before accepting an operative signature.</summary>
    private void Device(string id,GateKind kind,Vector2 position)
    {
        var device=AddGate("Device"+id,kind,position,new Vector2(80,140));
        device.Acceptance=()=>!GameplaySuspended && Progress.CanComplete(id);
        device.GateCleared+=()=> { if(CommitObjective(id) && kind==GateKind.FireVent)device.SecureForEvacuation(); };
    }
    /// <inheritdoc/>
    protected override void BuildObjectives()
    {
        Device("signal",GateKind.SonicLock,new Vector2(8200,600));
        Device("coolant",GateKind.FireVent,new Vector2(10900,600));
        Device("maintenance",GateKind.RubbleWall,new Vector2(12100,600));
        Terminal("record:1","Authenticate family register",new Vector2(13900,400));
        Terminal("record:2","Authenticate residency register",new Vector2(16350,200));
        Terminal("record:3","Authenticate civic register",new Vector2(18300,500));
        Terminal("isolate","Isolate the exclusion command",new Vector2(20300,600));
        Terminal("commit","Commit recovered identities",new Vector2(25000,600));
        Terminal("extract","Extract through accepted access",new Vector2(27700,600));
    }
    /// <summary>The first scanner is isolated from combat. Later scanners combine the same readable timing with formations.</summary>
    protected override void BuildHazards()
    {
        foreach(float x in new[]{1100f,1800,5400,6900,7900})
            Actors.AddChild(new RegistryScanner { Name="Scanner"+x,Position=new Vector2(x,600),Mission=this });
    }
    /// <summary>Standable main-route height at an enemy slot, including solid archive stairs and cover. Keeps defenders out of structural collision.</summary>
    private static float EnemyFloor(float x)
    {
        float floor=600;
        foreach(var route in new[]{(13200f,2),(15300f,4),(17600f,1)})
        {
            float start=route.Item1,deck=start+route.Item2*180,end=deck+650;
            if(x>=start && x<deck)floor=500-(int)((x-start)/180)*100;
            else if(x>=deck && x<end)floor=600-route.Item2*100;
            else if(x>=end && x<end+route.Item2*180)floor=600-(route.Item2-(int)((x-end)/180)-1)*100;
        }
        foreach(float cover in new[]{2200f,3100,5100,6500,7600,9400,10100,11500,13400,14500,16300,18200,20700,22500,23500})
            if(x>=cover && x<cover+110)floor=System.Math.Min(floor,520);
        foreach(float step in new[]{2400f,10450,17350})
            if(x>=step && x<step+180)floor=System.Math.Min(floor,500);
        return floor;
    }
    /// <summary>Seventeen three-member formations, with three sequential manually activated core waves and a six-actor ceiling.</summary>
    protected override void SpawnEnemies()
    {
        // Keep paired required groups just beyond their 1,250px activation span so one
        // approach does not wake two full formations together. Objective order and count stay fixed.
        float[] xs={2500,3800,5000,6500,8000,9400,10700,12000,13300,14600,15700,17000,17900,19200,21400,22500,23400};
        for(int i=0;i<xs.Length;i++)
        {
            string id=NamesProgress.Formations[i];float x=xs[i];
            // Contextual encounter timings and composition variety
            bool isCore = id.StartsWith("core");
            bool isService = id.StartsWith("service");
            bool isArchive = id.StartsWith("archive");

            float gunnerWarn = isCore ? 1.25f : isService ? 1.35f : 1.45f;
            float gunnerRec = isCore ? 2.2f : isService ? 2.4f : 2.6f;
            float sentryWarn = isCore ? 0.85f : isService ? 0.9f : 1.0f;

            // Varied tactical member composition while strictly maintaining exactly 3 members
            (Vector2 Pos, bool Sentry)[] members;
            if (isService)
            {
                // Service passage: fast ambush sentries
                members = new[] {
                    (new Vector2(x+80,EnemyFloor(x+80)), true),
                    (new Vector2(x+240,EnemyFloor(x+240)), true),
                    (new Vector2(x-120,EnemyFloor(x-120)-170), false)
                };
            }
            else if (isCore && id == "core3")
            {
                // Final core defense wave: heavy vanguard
                members = new[] {
                    (new Vector2(x+60,EnemyFloor(x+60)), true),
                    (new Vector2(x+240,EnemyFloor(x+240)), true),
                    (new Vector2(x-140,EnemyFloor(x-140)-170), false)
                };
            }
            else
            {
                // Standard mixed formation with flanking positions
                members = new[] {
                    (new Vector2(x+100,EnemyFloor(x+100)), true),
                    (new Vector2(x+270,EnemyFloor(x+270)-170), false),
                    (new Vector2(x-120,EnemyFloor(x-120)-170), false)
                };
            }

            var e=new WildfireEncounter { Name="Formation"+id,Id=id,Mission=this,ManualActivation=isCore,
                GunnerWarningDuration=gunnerWarn,GunnerRecoveryDuration=gunnerRec,SentryWarningDuration=sentryWarn,
                Activation=new Rect2(x-500,-200,1250,1000),Acceptance=()=>Progress.CanStartFormation(id),
                Members=members };
            _encounters.Add(e);Actors.AddChild(e);
        }
    }
    /// <summary>Attaches a single owned dialogue lane and ability observation after construction; presentation is dressed after blockout validation.</summary>
    protected override void OnLevelReady()
    {
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible",false);
        _dialogue=new NamesDialogue { Name="NamesDialogue",Mission=this };AddChild(_dialogue);
        Player.AbilityUsed+=OnAbility;
        _previous=Player.Position;
        _accepted=new AudioStreamPlayer { Name="AccessAccepted",Bus="SFX",VolumeDb=-14,ProcessMode=ProcessModeEnum.Pausable };
        AddChild(_accepted);
        var music=GetNodeOrNull<AudioStreamPlayer>("AudioStreamPlayer");
        if(music!=null){music.Bus="Music";music.VolumeDb=-21;music.ProcessMode=ProcessModeEnum.Pausable;if(music.Stream is AudioStreamOggVorbis loop)loop.Loop=true;}
        BuildArtwork();
    }
    /// <summary>Mirage must actually phase across the service access lane; an unrelated ability press cannot satisfy entry.</summary>
    private void OnAbility(string tag)
    {
        if(!GameplaySuspended && tag==LevelCatalog.Phase && Player.IsPhased && _previous.X>=3700 && _previous.X<3950 &&
            Player.Position.X>3950 && Player.Position.X-_previous.X>=96 && Mathf.Abs(Player.Position.Y-554)<90) CommitObjective("phase");
    }
    /// <summary>Commits a legal interaction once, then emits its voiced story event without making speech a prerequisite.</summary>
    public bool CommitObjective(string id)
    {
        if(GameplaySuspended || !Progress.Complete(id))return false;
        if(id=="extract") { _dialogue.Cancel();ClearRounds();CompleteMission();return true; }
        _dialogue.Enqueue(id.Replace(':','_'));
        if(id=="commit")
        {
            string sound="res://assets/audio/names/accepted.ogg";
            if(ResourceLoader.Exists(sound)){_accepted.Stream=GD.Load<AudioStream>(sound);_accepted.Play();}
            var status=World.GetNode<Label>("AccessStatus");status.Text=WildfireText.Get("NAMES_ACCESS_ACCEPTED","ACCEPTED / CIVILIAN ACCESS RESTORED");status.Modulate=new Color("85dacb");
        }
        if(id=="commit") foreach(var gate in _civilianGates)
        { var texture=GD.Load<Texture2D>("res://assets/environment/names/access_restored.png");if(texture!=null)gate.Texture=texture; }
        return true;
    }
    /// <summary>Updates exact interlocks, context prompts and sequential defenses; no countdown or endless spawn advances recovery.</summary>
    protected override void UpdateMission()
    {
        if(GameplaySuspended)return;
        int sector=Player.Position.X<4300?0:Player.Position.X<8800?1:Player.Position.X<12800?2:Player.Position.X<19800?3:Player.Position.X<24500?4:5;
        if(sector!=_sector){_sector=sector;_dialogue.Enqueue("sector"+sector);}
        foreach(var seal in _seals)if(Progress.Has(seal.Event)&&!seal.Wall.IsOpen)seal.Wall.Open();
        // Core stages can be fought from either side of cover; all spawn in the same defended recovery room.
        foreach(var encounter in _encounters.Where(e=>e.ManualActivation))
            if(Player.Position.X>=19800 && Player.Position.X<24200 && Progress.CanStartFormation(encounter.Id)) encounter.Activate();
        for(int stage=1;stage<=3;stage++) if(Progress.CanComplete("defense:"+stage))CommitObjective("defense:"+stage);
        var terminal=_terminals.FirstOrDefault(t=>t.InRange&&!Progress.Has(t.Objective));
        if(terminal!=null)
        {
            ShowPrompt(Progress.CanComplete(terminal.Objective)?ContextBinding("attack")+" / "+WildfireText.Get("NAMES_TAP_CONSOLE","tap console")+" / "+WildfireText.Get("NAMES_TERMINAL_"+terminal.Objective.Replace(':','_').ToUpperInvariant(),terminal.Caption):WildfireText.Get("NAMES_TERMINAL_LOCKED","TERMINAL LOCKED")+" / "+ComputeObjective());
            if(!Progress.CanComplete(terminal.Objective))_dialogue.Enqueue("hint");
            if(Input.IsActionJustPressed("attack"))terminal.TryInteract();
        }
        else if(Player.Position.X>3650 && !Progress.Has("phase"))ShowPrompt(WildfireText.Get("NAMES_PHASE_PROMPT","MIRAGE / PHASE THROUGH THE WHITE SERVICE MARKER")+" / "+ContextBinding("karate"));
        else HidePrompt();
        _previous=Player.Position;
    }
    /// <summary>Concise step-specific guidance with stable localization keys and English fallbacks.</summary>
    protected override string ComputeObjective()
    {
        string text=Progress.Current switch {
            "phase"=>"Clear service defenders. Mirage: phase through the white service marker.",
            "signal"=>"Clear processing security. Aura: resonate at the eastern scanner control.",
            "coolant"=>"Clear utility defenders. Oceanus: stabilize the archive coolant intake.",
            "maintenance"=>"Titan: open the damaged maintenance wall. Protect the originals.",
            "record:1"=>"Clear family archive defenders. Recover original set 1 on the low gallery.",
            "record:2"=>"Clear residency defenders. Recover set 2 on the upper archive gallery.",
            "record:3"=>"Clear civic defenders. Recover set 3 above the maintenance return.",
            "isolate"=>"Use the registry entrance console to isolate the exclusion command.",
            "defense:1"=>"Recovery 1/3: defeat the first finite security formation.",
            "defense:2"=>"Recovery 2/3: defeat the second finite security formation.",
            "defense:3"=>"Recovery 3/3: defeat the final security formation.",
            "commit"=>"Commit all recovered identities at the district access console.",
            _=>"Civilian access accepted. Extract at the eastern gate." };
        return WildfireText.Get("NAMES_OBJECTIVE_"+Progress.Current.Replace(':','_').ToUpperInvariant(),text);
    }
    /// <summary>Exposes exact objective state to runtime acceptance and presentation consumers.</summary>
    public bool HasObjective(string id)=>Progress.Has(id);
    /// <inheritdoc/>
    public bool HasCombatEvent(string id)=>Progress.Has(id);
    /// <inheritdoc/>
    public void CompleteCombatEvent(string id)=>Progress.Complete(id);
    /// <inheritdoc/>
    public bool CanActivateEncounter(int members)=>!GameplaySuspended&&_encounters.Sum(e=>e.LivingMembers)+members<=6;
    /// <inheritdoc/>
    public void RewardMember(string id){if(Progress.Reward(id))Score.AddPoint();}
    /// <inheritdoc/>
    public void LaunchRound(Vector2 origin,Vector2 heading,WildfireGunner source)
    {if(GameplaySuspended||heading.LengthSquared()<=0)return;var round=new WildfireRound { Heading=heading,Source=source };Actors.AddChild(round);round.GlobalPosition=origin;}
    /// <summary>Clears mission-owned rounds before teleport, results or scene teardown.</summary>
    private void ClearRounds()
    {
        foreach(Node n in GetTree().GetNodesInGroup("wildfire_rounds"))if(IsAncestorOf(n)){n.ProcessMode=ProcessModeEnum.Disabled;n.QueueFree();}
    }
    /// <inheritdoc/>
    protected override void OnRecoveryStarting()
    {
        _recovering=true;_dialogue.Cancel();_accepted.Stop();ClearRounds();
        foreach(var e in _encounters)e.ResetIncomplete();
        foreach(Node n in Actors.FindChildren("*","",true,false))
        {if(n.IsQueuedForDeletion())continue;_recoveryModes.Add((n,n.ProcessMode));n.ProcessMode=ProcessModeEnum.Disabled;}
    }
    /// <inheritdoc/>
    protected override void OnRecoveryCompleted()
    {
        foreach(var state in _recoveryModes)if(IsInstanceValid(state.Node)&&!state.Node.IsQueuedForDeletion())state.Node.ProcessMode=state.Mode;
        _recoveryModes.Clear();_previous=Player.Position;_recovering=false;
    }
    /// <inheritdoc/>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine)=>!GameplaySuspended&&(shrine.Name.ToString() switch {
        "Service"=>true,"Hall"=>Progress.Has("phase"),"Utilities"=>Progress.Has("signal"),"Records"=>Progress.Has("maintenance"),
        "Core"=>Progress.Has("record:3"),"District"=>Progress.Has("defense:3"),_=>false });
    /// <summary>Loads original Docker/Blender props independently of authored collision; absent decoration never blocks play.</summary>
    private void Art(string name,Vector2 position,Vector2 size,int z=-2)
    {
        string path="res://assets/environment/names/"+name+".png";
        if(!ResourceLoader.Exists(path))return;
        var texture=GD.Load<Texture2D>(path);
        var sprite=new Sprite2D { Texture=texture,Position=position,Scale=size/texture.GetSize(),ZIndex=z,Name="Art"+name };
        World.AddChild(sprite);if(name=="access_denied")_civilianGates.Add(sprite);
    }
    /// <summary>Dark glass and white scanners frame amber denial; the final playable district keeps restrained civilian warmth.</summary>
    private void BuildArtwork()
    {
        for(int i=0;i<10;i++)Art("district",new Vector2(1000+i*2800,150),new Vector2(2600,1500),-8);
        foreach(float x in new[]{1100f,1800,5400,6900,7900})Art("scanner",new Vector2(x,330),new Vector2(280,600));
        foreach(float x in new[]{10600f,13700,16000,18100,22200})Art("archive",new Vector2(x,280),new Vector2(520,600));
        foreach(var terminal in _terminals)Art("terminal",terminal.Position+new Vector2(0,-80),new Vector2(140,170),-1);
        foreach(float x in new[]{25600f,26500,27100})
        {Art("access_denied",new Vector2(x,330),new Vector2(460,600));Art("civilians",new Vector2(x-200,530),new Vector2(270,180),-1);}
        // Sector Landmark Atmospheric Signs
        string[] sectors = {
            "SEC.01 // SUBTERRANEAN SERVICE DUCT",
            "SEC.02 // BIOMETRIC PROCESSING HALL",
            "SEC.03 // UTILITIES & ARCHIVE COOLING",
            "SEC.04 // CITIZEN IDENTITY ARCHIVES",
            "SEC.05 // REGISTRY CENTRAL CORE VAULT",
            "SEC.06 // RESTORED DISTRICT PERIMETER"
        };
        float[] sectorXs = { 400f, 4400f, 8900f, 12900f, 19900f, 24600f };
        for (int s = 0; s < sectors.Length; s++)
        {
            var secLabel = new Label {
                Text = sectors[s],
                Position = new Vector2(sectorXs[s], 280),
                Modulate = new Color(0, 0.94f, 1f, 0.55f)
            };
            secLabel.AddThemeFontSizeOverride("font_size", 14);
            World.AddChild(secLabel);
        }

        var marker=new Label { Text=WildfireText.Get("NAMES_SERVICE_ACCESS","MIRAGE / SERVICE ACCESS"),Position=new Vector2(3750,340),Modulate=Colors.White };
        World.AddChild(marker);
        World.AddChild(new Label { Name="AccessStatus",Text=WildfireText.Get("NAMES_ACCESS_DENIED","AMBER / IDENTITY DENIED"),Position=new Vector2(24900,340),Modulate=new Color("e6a650") });
    }
    /// <summary>Unsubscribes the ability listener and silences the mission speech before base teardown.</summary>
    public override void _ExitTree()
    {
        if(IsInstanceValid(Player))Player.AbilityUsed-=OnAbility;
        _dialogue?.Cancel();
        GetNodeOrNull<Node>("/root/Global")?.Call("set_control_prompt_visible",true);
        base._ExitTree();
    }
}
