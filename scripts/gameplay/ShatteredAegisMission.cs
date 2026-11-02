using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>Act I fortress assault: finite defense, six operative roles, checkpoint-safe objectives and a playable liberated crossing.</summary>
public partial class ShatteredAegisMission : CodeLevel, IFiniteCombatHost
{
    /// <summary>Fresh scene-local ledger; completed objectives and kill rewards survive only checkpoint recovery.</summary>
    public AegisProgress Progress { get; } = new();
    /// <summary>Compatibility property used by arrival tests and HUD consumers.</summary>
    public bool BossDefeated => Progress.Has("boss:voss");
    /// <summary>Existing mech scene; null before the arena and after an abandoned attempt.</summary>
    public CyberBossMech2D? Boss { get; private set; }
    /// <summary>Opt-in encounter state, exposed for runtime acceptance.</summary>
    public AegisBossController? Fortress { get; private set; }
    /// <inheritdoc/>
    public CyberShogun CombatPlayer => Player;
    /// <inheritdoc/>
    public bool GameplaySuspended => ArrivalActive || _recoveringLocal || MissionComplete || !IsInsideTree() || GetTree().Paused;
    private readonly List<WildfireEncounter> _encounters = new();
    private readonly List<(PalacePlatform Barrier, string Event)> _seals = new();
    private readonly List<(Node Node, ProcessModeEnum Mode)> _recoveryModes = new();
    private readonly List<CyberLaserGrid2D> _curtains = new();
    private readonly List<Node2D> _gunArt = new();
    private readonly List<Node2D> _civilians = new();
    private RecruitBeat _surya = null!;
    private AegisDialogue _dialogue = null!;
    private PalaceObjective _exit = null!;
    private PalacePlatform _arenaDoor = null!;
    private Vector2 _previousPosition;
    private bool _recoveringLocal, _handoff, _practiceArmed, _liveArmed;
    private bool _gunsOffline, _curtainOffline;
    private readonly List<Node2D> _thermalArt = new();
    private readonly List<AbilityGate> _gunTargets = new();
    /// <inheritdoc/>
    protected override int MissionNumber => 4;
    /// <inheritdoc/>
    protected override Rect2I WorldBounds => new(0, -1300, 28000, 2120);
    /// <inheritdoc/>
    protected override Vector2 StartPosition => new(200, 600 - FeetOffset);
    /// <inheritdoc/>
    protected override SkylinePalette SkylinePalette => SkylinePalette.Citadel;
    /// <inheritdoc/>
    protected override Color PlatformTint => new(.86f, .82f, 1);
    /// <inheritdoc/>
    protected override string CompletionLine => "ACT I COMPLETE / CROSSING OPEN. BRING THEM HOME.";
    /// <inheritdoc/>
    protected override string IntroLine() => "AXIOM-7: Reach Surya. Secure the heat works, disable the gun deck, and breach the inner curtain.";
    /// <inheritdoc/>
    protected override string TitleCardObjective(string narrativeObjective) => "Secure the fortress systems. Open the crossing before extraction.";
    /// <inheritdoc/>
    protected override IEnumerable<(string Name, Vector2 Position)> Shrines => new[] {
        ("ShrineApproach", new Vector2(3950,600)), ("ShrineSurya", new Vector2(7850,600)),
        ("ShrineThermal", new Vector2(11900,600)), ("ShrineGunDeck", new Vector2(16850,0)),
        ("ShrineCurtain", new Vector2(20750,600)), ("ShrineArena", new Vector2(21100,600)),
        ("ShrineCrossing", new Vector2(25100,600)) };
    /// <inheritdoc/>
    protected override IEnumerable<Vector2> FragmentPositions => new[] { new Vector2(2900,400), new Vector2(14650,-200), new Vector2(19600,400) };
    /// <inheritdoc/>
    protected override string FragmentLog(int found) => found switch {
        1 => "ARCHIVE: Every gun watches a road inside the border.",
        2 => "ARCHIVE: The coolant map serves homes as well as weapons. Spare the district.",
        _ => "ARCHIVE: Crossing permits expired before their holders reached the wall." };
    /// <summary>Six broad 100px ascents and six descents stay below Titan's weakest jump; all landings are stationary.</summary>
    public static float RouteFloor(float x) => x < 12000 ? 600 : x < 15000 ? 500 - (int)((x - 12000) / 500) * 100 :
        x < 17000 ? 0 : x < 20000 ? 100 + (int)((x - 17000) / 500) * 100 : 600;
    /// <summary>Builds collision before presentation: low cover, vertical gun decks, local service routes and safe ending road.</summary>
    protected override void BuildGeometry()
    {
        AddPlatform(-160,-1300,160,2600,PalaceSurface.Wall,false,"WestWall");
        AddPlatform(0,600,12000,800,PalaceSurface.Concrete,false,"ServiceRoad");
        for (int i=0;i<6;i++) AddPlatform(12000+i*500,500-i*100,500,1500,PalaceSurface.Steel,false,$"GunStep{i}");
        AddPlatform(15000,0,2000,1400,PalaceSurface.Steel,false,"GunDeck");
        for (int i=0;i<6;i++) AddPlatform(17000+i*500,100+i*100,500,1500,PalaceSurface.Concrete,false,$"CurtainStep{i}");
        AddPlatform(20000,600,8000,800,PalaceSurface.Concrete,false,"CommandRoad");
        AddPlatform(28000,-1300,100,2700,PalaceSurface.Wall,false,"EastWall");
        foreach(float x in new[] { 1000f,2200,3200,4600,5600,8400,9800,10600,12700,13700,17800,18800,20200 })
            AddPlatform(x,RouteFloor(x)-80,120,80,PalaceSurface.Concrete,false,$"Cover{x}");
        foreach(float x in new[] { 2700f,14450,19400 })
        {
            AddPlatform(x,RouteFloor(x)-100,150,100,PalaceSurface.Concrete);
            AddPlatform(x+100,RouteFloor(x)-200,330,22,PalaceSurface.Steel,true,$"ArchiveDeck{x}");
        }
        // Both gun approaches reconnect on the same deck; the upper coolant route has broad two-way stairs.
        for(int i=0;i<3;i++) AddPlatform(15000+i*180,-100-i*100,180,22,PalaceSurface.Steel,true,$"CoolantStair{i}");
        AddPlatform(15540,-300,750,22,PalaceSurface.Steel,true,"CoolantDeck");
        for(int i=0;i<3;i++) AddPlatform(16290+i*180,-200+i*100,180,22,PalaceSurface.Steel,true,$"CoolantReturn{i}");
        Seal("ApproachInterlock",3900,"interlock:approach");
        Seal("SuryaLessonInterlock",7800,"crossing:bulwark");
        Seal("ThermalInterlock",11800,"coolant:intake");
        Seal("GunInterlock",16750,"guns:western");
        Seal("BlinkPracticeInterlock",17600,"lesson:blink");
        Seal("CurtainInterlock",20500,"curtain:inner");
        _arenaDoor=AddPlatform(21400,-1300,30,1900,PalaceSurface.Gate,false,"ArenaDoor");
        _arenaDoor.Open();
        Seal("BorderWall",24800,"crossing:open");
        BuildFortressArtwork();
    }
    /// <summary>Dresses the validated blockout with original fortress silhouettes, independently powered guns and visible safe-route landmarks.</summary>
    private void BuildFortressArtwork()
    {
        for(int i=0;i<8;i++)
        {
            float x=1400+i*3400;
            var backdrop=new Node2D { Name=$"FortressBackdrop{i}",Position=new Vector2(x,RouteFloor(x)),ZIndex=-5,Modulate=new Color(.65f,.7f,.85f,.8f) };
            World.AddChild(backdrop);AegisVisual.Add(backdrop,"fortress",new Vector2(1900,1000),new Vector2(0,-460));
        }
        foreach(float x in new[]{1800f,5300,12600,14800,16400})
        {
            var gun=new Node2D { Name=$"GunEmplacement{x}",Position=new Vector2(x,RouteFloor(x)),ZIndex=-2 };
            World.AddChild(gun);_gunArt.Add(gun);AegisVisual.Add(gun,"gun",new Vector2(600,550),new Vector2(0,-280));
        }
        foreach(float x in new[]{9000f,10100,11200})
        {
            var heat=new Node2D { Name=$"ThermalPlant{x}",Position=new Vector2(x,600),ZIndex=-2 };
            World.AddChild(heat);_thermalArt.Add(heat);AegisVisual.Add(heat,"coolant",new Vector2(400,400),new Vector2(0,-210));
        }
        foreach(var marker in new[]{(6400f,"SURYA / SHELTER"),(6900f,"BULWARK / PRACTICE"),(7450f,"BULWARK / LIVE"),
            (11400f,"OCEANUS / COOLANT"),(15500f,"COOLANT ABOVE / BREACH BELOW"),(17400f,"MIRAGE / PRACTICE"),
            (20300f,"AURA / LOCAL CONTROL"),(21100f,"COMMAND ARENA"),(24550f,"CROSSING RELEASE")})
        {
            var label=new Label { Text=marker.Item2,Position=new Vector2(marker.Item1-150,RouteFloor(marker.Item1)-240),
                Modulate=new Color("#9ddde5"),MouseFilter=Control.MouseFilterEnum.Ignore };
            World.AddChild(label);
        }
        var crossing=new Node2D { Name="OpenCrossingArt",Position=new Vector2(25300,600),ZIndex=-2 };
        World.AddChild(crossing);AegisVisual.Add(crossing,"crossing",new Vector2(800,800),new Vector2(0,-380));
    }

    /// <summary>Adds a full-height physical interlock controlled by one exact durable objective.</summary>
    private void Seal(string name,float x,string id) => _seals.Add((AddPlatform(x,-1300,24,FullHeightFrom(RouteFloor(x)),PalaceSurface.Gate,false,name),id));
    /// <summary>Creates an ordinary-attack device; defender clearance and prerequisites are checked before operation.</summary>
    private AegisDevice Device(string name,float x,string id)
    {
        var device=new AegisDevice { Name=name,Position=new Vector2(x,RouteFloor(x)-60),
            Acceptance=()=>!GameplaySuspended && Progress.CanComplete(id), Operated=()=>Progress.Complete(id) };
        Actors.AddChild(device);
        return device;
    }
    /// <summary>Creates an operative target; both western gun approaches commit the same controller objective.</summary>
    private AbilityGate Target(string name,GateKind kind,Vector2 position,string id)
    {
        var target=AddGate(name,kind,position,new Vector2(80,130));
        target.Acceptance=()=>!GameplaySuspended && Progress.CanComplete(id);
        if(kind==GateKind.SonicLock)AegisVisual.Add(target,"controller",new Vector2(150,180),new Vector2(0,-85));
        target.GateCleared+=()=> { if(Progress.Complete(id) && kind==GateKind.FireVent) target.SecureForEvacuation(); };
        return target;
    }
    /// <inheritdoc/>
    protected override void BuildObjectives()
    {
        Device("ApproachControl",3750,"interlock:approach");
        Target("CoolantIntake",GateKind.FireVent,new Vector2(11400,600),"coolant:intake");
        _gunTargets.Add(Target("GunCoolant",GateKind.FireVent,new Vector2(15900,-300),"guns:western"));
        _gunTargets.Add(Target("MaintenanceBreach",GateKind.RubbleWall,new Vector2(16100,0),"guns:western"));
        Target("InnerController",GateKind.SonicLock,new Vector2(20300,600),"curtain:inner");
        var crossing=Device("CrossingControl",24550,"crossing:open");
        crossing.Operated=()=> { if(!Progress.Complete("crossing:open"))return; QueueBeat("crossing"); MakeCrossingSafe(); };
        _exit=new PalaceObjective { Name="ExtractionGate",Kind="Exit",Position=new Vector2(27700,600) };
        Actors.AddChild(_exit);
    }
    /// <summary>Harmless Bulwark/Blink practice precedes live uses; beams have separated broad landing platforms.</summary>
    protected override void BuildHazards()
    {
        Vent("BulwarkPractice",6900,true,false);
        Vent("BulwarkLive",7450,false,false);
        Vent("MaintenanceHeat",8650,false,true);
        Vent("ThermalPulseA",9450,false,true);
        Vent("ThermalPulseB",10250,false,true,2);
        Vent("GunCoolantHeat",15700,false,true);
        foreach(float x in new[] { 17400f,18300,19300,19950 })
        {
            var beam=AddLaserField(new Vector2(x,-1300),RouteFloor(x)+1300,x==17400?0:999);
            beam.Name=x==17400?"BlinkPractice":"InnerBeam"+x;
            _curtains.Add(beam);
        }
    }
    /// <summary>Seats a compact vent on the actual floor; Surya's full window is roughly three times the protected span.</summary>
    private void Vent(string name,float x,bool practice,bool thermal,float offset=0) => Actors.AddChild(new AegisVent {
        Name=name,Position=new Vector2(x,RouteFloor(x)),Mission=this,Practice=practice,Thermal=thermal,Offset=offset });
    /// <summary>Fourteen finite three-member formations, at most four live enemies; remote bypassed groups release their budget.</summary>
    protected override void SpawnEnemies()
    {
        float[][] positions={new[] {900f,2100,3250},new[] {4700f,5800},new[] {8400f,9700,10850},
            new[] {12600f,14000,15200},new[] {17700f,18700,20100}};
        for(int s=0;s<positions.Length;s++) for(int g=0;g<positions[s].Length;g++)
        {
            float x=positions[s][g],floor=RouteFloor(x);
            var e=new WildfireEncounter { Name=$"Encounter_s{s+1}_{g+1}",Id=$"s{s+1}-{g+1}",Mission=this,
                Activation=new Rect2(x-450,floor-700,1100,1000),Members=new[] {
                    (new Vector2(x+160,RouteFloor(x+160)),true),
                    (new Vector2(x+330,RouteFloor(x+330)-140),false),
                    (new Vector2(x-100,RouteFloor(x-100)-160),false) } };
            _encounters.Add(e); Actors.AddChild(e);
        }
    }
    /// <summary>Preserves recruitment words and arrival snapshot; scheduler is frozen with the rest of the mission.</summary>
    protected override void OnLevelReady()
    {
        _dialogue=new AegisDialogue { Name="AegisDialogue",Mission=this };
        AddChild(_dialogue);
        _surya=new RecruitBeat { Name="SuryaJoins",Level=this,Operative="Surya Prime",Position=new Vector2(6400,600),
            RecruitmentCondition=()=>!GameplaySuspended && Progress.CanComplete("recruit:surya"),
            LessonCondition=()=>Progress.Has("lesson:bulwark"),Lines=System.Array.Empty<string>() };
        Actors.AddChild(_surya);
        Player.AbilityUsed+=OnAbilityUsed;
        _previousPosition=Player.Position;
        BeginArrivalPresentation(new ShatteredAegisIntro { Name="ShatteredAegisIntro",Layer=40 });
    }
    /// <summary>Accepts only an actual forward Blink crossing of the harmless beam, including phase protection and landing height.</summary>
    private void OnAbilityUsed(string tag)
    {
        if(GameplaySuspended)return;
        if(tag==LevelCatalog.Phase && Player.ActiveProfile.DisplayName=="Mirage Blade" && Player.IsPhased &&
            _previousPosition.X>=17220 && _previousPosition.X<17400 && Player.Position.X>17430 &&
            Player.Position.X-_previousPosition.X>=96 && Mathf.Abs(Player.Position.Y-(RouteFloor(Player.Position.X)-46))<90)
            Progress.Complete("lesson:blink");
    }
    /// <summary>Checks the entire vent overlap under real Bulwark protection, rejecting unprotected or airborne bypasses.</summary>
    private void CheckProtectedCrossing(float x,string id,ref bool armed)
    {
        bool protectedNow=Player.IsBulwarked && Player.ActiveProfile.DisplayName=="Surya Prime" &&
            Mathf.Abs(Player.Position.Y-(RouteFloor(x)-46))<55;
        float previous=_previousPosition.X,current=Player.Position.X;
        if(previous<x-100 && current>=x-100) armed=protectedNow;
        if(current>=x-100 && current<=x+100 && !protectedNow)armed=false;
        if(previous<=x+100 && current>x+100)
        { if(armed && protectedNow)Progress.Complete(id); armed=false; }
    }
    /// <summary>Runs durable objectives, resets remote incomplete groups, starts the sealed arena and accepts extraction once.</summary>
    protected override void UpdateMission()
    {
        if(GameplaySuspended)return;
        if(!_handoff){_handoff=true;QueueBeat("handoff");}
        if(_surya.Triggered && Progress.Complete("recruit:surya")) { QueueBeat("surya");QueueBeat("recruit");QueueBeat("practice"); }
        CheckProtectedCrossing(6900,"lesson:bulwark",ref _practiceArmed);
        CheckProtectedCrossing(7450,"crossing:bulwark",ref _liveArmed);
        if(Progress.Has("lesson:bulwark") && !_surya.Completed)_surya.CheckLessonSuccess();
        foreach(var seal in _seals)if(Progress.Has(seal.Event) && !seal.Barrier.IsOpen)seal.Barrier.Open();
        foreach(var encounter in _encounters)
            if(encounter.Active && Mathf.Abs(Player.Position.X-encounter.Activation.GetCenter().X)>1500)encounter.ResetIncomplete();
        if(Player.Position.X>14900 && !Progress.Has("guns:western")){QueueBeat("ocean_route");QueueBeat("titan_route");}
        if(Player.Position.X>19000 && !Progress.Has("curtain:inner"))QueueBeat("aura");
        if(Progress.Has("coolant:intake"))foreach(var art in _thermalArt)art.Modulate=new Color(.35f,.8f,1);
        if(Progress.Has("guns:western") && !_gunsOffline)
        { _gunsOffline=true;foreach(var art in _gunArt)art.Modulate=new Color(.28f,.38f,.4f);
            foreach(var target in _gunTargets){target.ProcessMode=ProcessModeEnum.Disabled;target.Visible=false;target.QueueFree();} }
        if(Progress.Has("curtain:inner") && !_curtainOffline)
        { _curtainOffline=true;foreach(var beam in _curtains){beam.ProcessMode=ProcessModeEnum.Disabled;beam.Visible=false;beam.QueueFree();} }
        if(!BossDefeated && Boss==null && Player.Position.X>21500 && Progress.CanComplete("boss:voss"))SpawnBoss();

        if(Progress.CanExtract)
        {

            if(Player.Position.X>26200)QueueBeat("home");
            if(Player.GlobalPosition.DistanceTo(_exit.GlobalPosition+new Vector2(0,-80))<130){_dialogue.Cancel();CompleteMission();}
        }
        _previousPosition=Player.Position;
    }
    /// <summary>Starts a fresh attempt only after every fortress objective; the pre-arena checkpoint remains outside its sealed door.</summary>
    private void SpawnBoss()
    {
        _arenaDoor.Close();
        Boss=GD.Load<PackedScene>("res://scenes/gameplay/cyber_boss_mech_2d.tscn").Instantiate<CyberBossMech2D>();
        Boss.CollisionLayer=6;Boss.Name="CyberBossMech2D";Boss.Position=new Vector2(23200,310);
        Fortress=new AegisBossController { Name="FortressEncounter",Mission=this,Boss=Boss };
        Actors.AddChild(Fortress);Boss.FortressController=Fortress;Actors.AddChild(Boss);
        Boss.BossDefeated+=OnBossDefeated;
        GetNode<CyberCombatHud>("CyberCombatHud").BindBoss(Boss);
        QueueBeat("arena");
    }
    /// <summary>Defeat shuts down the arena but leaves the crossing release as the player's final deliberate action.</summary>
    private void OnBossDefeated()
    {
        if(!Progress.Complete("boss:voss"))return;
        Fortress?.CancelAttempt();_arenaDoor.Open();
        if(Progress.Reward("boss:voss"))for(int i=0;i<50;i++)Score.AddPoint();
        QueueBeat("defeat");QueueBeat("kaito");Boss=null;
    }
    /// <summary>Silences all remaining ordinary threats before invulnerable, nonblocking civilians appear.</summary>
    private void MakeCrossingSafe()
    {
        foreach(var e in _encounters)e.ResetIncomplete();
        ClearRounds();
        for(int i=0;i<8;i++)
        {
            var actor=new AegisCivilian { Name=$"CrossingCivilian{i}",Position=new Vector2(25100+i*140,600),ZIndex=1 };
            Actors.AddChild(actor);_civilians.Add(actor);
            AegisVisual.Add(actor,"civilians",new Vector2(100,100),new Vector2(0,-48));
        }
    }
    /// <summary>Connects the controller's existing projectile scene to the mission-owned actor lifetime.</summary>
    public void AddBossProjectile(CyberBossMissile2D missile,Vector2 at){Actors.AddChild(missile);missile.GlobalPosition=at;}
    /// <summary>Pylons are local attack targets and are removed with the attempt, never saved as mission objectives.</summary>
    public void AddArenaDevice(AegisDevice device)=>Actors.AddChild(device);
    /// <summary>Queues one authored speech event; the scheduler owns priority, expiry and fallback subtitles.</summary>
    public void QueueBeat(string id)=>_dialogue.Enqueue(id);
    /// <inheritdoc/>
    protected override string ComputeObjective()
    {
        string id=Progress.CurrentObjective;
        string text=id switch {
            "interlock:approach"=>"Secure approach defenders, then attack the interlock.",
            "recruit:surya"=>"Defeat both road formations. Reach Surya at the shelter.",
            "lesson:bulwark"=>"SURYA: Bulwark across the practice vent; stay protected until the far platform.",
            "crossing:bulwark"=>"SURYA: Recover, then Bulwark across the live vent.",
            "coolant:intake"=>"SURYA: Cross maintenance heat. OCEANUS: Clear intake defenders and douse the coolant intake.",
            "guns:western"=>"Clear gun defenders. OCEANUS: Upper coolant line; TITAN: Lower maintenance breach.",
            "lesson:blink"=>"MIRAGE: Stand beside the blue practice curtain, face right and Blink through.",
            "curtain:inner"=>"MIRAGE: Blink each separated curtain. AURA: Clear defenders and pulse the far controller.",
            "boss:voss"=>Fortress?.Shielded==true?"Destroy both power pylons; avoid marked defense lanes.":"Defeat Voss: parry, absorb or dodge missiles; attack the lowered core during cyan recovery.",
            "crossing:open"=>"KAITO: Open the crossing first. Attack its amber control.",
            _=>"Crossing secured. Walk with the civilians to extraction." };
        return WildfireText.Get("AEGIS_OBJECTIVE_"+id.Replace(':','_').ToUpperInvariant(),text);
    }
    /// <inheritdoc/>
    public bool HasCombatEvent(string id)=>Progress.Has(id);
    /// <inheritdoc/>
    public void CompleteCombatEvent(string id)=>Progress.Complete(id);
    /// <inheritdoc/>
    public bool CanActivateEncounter(int members)=>!BossDefeated && !Progress.Has("crossing:open") && Boss==null && _encounters.Sum(e=>e.LivingMembers)+members<=4;
    /// <inheritdoc/>
    public void RewardMember(string id){if(Progress.Reward(id))Score.AddPoint();}
    /// <inheritdoc/>
    public void LaunchRound(Vector2 origin,Vector2 heading,WildfireGunner source)
    {if(GameplaySuspended)return;var round=new WildfireRound{Heading=heading,Source=source};Actors.AddChild(round);round.GlobalPosition=origin;}
    /// <summary>Removes transient ordinary and boss rounds synchronously before teleporting or ending an attempt.</summary>
    private void ClearRounds()
    {
        foreach(string group in new[]{"wildfire_rounds","aegis_projectiles"})foreach(Node n in GetTree().GetNodesInGroup(group))
            if(IsAncestorOf(n)){n.ProcessMode=ProcessModeEnum.Disabled;n.QueueFree();}
    }
    /// <inheritdoc/>
    protected override void OnRecoveryStarting()
    {
        _recoveringLocal=true;_dialogue.Cancel();ClearRounds();_practiceArmed=_liveArmed=false;
        foreach(var e in _encounters)e.ResetIncomplete();
        if(!BossDefeated && Boss!=null)
        {
            _dialogue.ResetBossAttempt();
            if(Fortress!=null){Fortress.ProcessMode=ProcessModeEnum.Disabled;Fortress.CancelAttempt();Fortress.QueueFree();}
            Boss.ProcessMode=ProcessModeEnum.Disabled;Boss.RemoveFromGroup("enemies");Boss.QueueFree();
            Boss=null;Fortress=null;_arenaDoor.Open();
        }
        foreach(Node n in Actors.FindChildren("*","",true,false))
        {if(n.IsQueuedForDeletion())continue;_recoveryModes.Add((n,n.ProcessMode));n.ProcessMode=ProcessModeEnum.Disabled;}
    }
    /// <inheritdoc/>
    protected override void OnRecoveryCompleted()
    {
        foreach(var state in _recoveryModes)if(IsInstanceValid(state.Node)&&!state.Node.IsQueuedForDeletion())state.Node.ProcessMode=state.Mode;
        _recoveryModes.Clear();_previousPosition=Player.Position;_recoveringLocal=false;
    }
    /// <summary>Checkpoints require their section's objective, not unrelated bypassable enemies; arena attempt cannot move the checkpoint inside.</summary>
    protected override bool CanActivateCheckpoint(PalaceObjective shrine)=>!GameplaySuspended && (shrine.Name.ToString() switch {
        "ShrineApproach"=>Progress.Has("interlock:approach"),"ShrineSurya"=>Progress.Has("crossing:bulwark"),
        "ShrineThermal"=>Progress.Has("coolant:intake"),"ShrineGunDeck"=>Progress.Has("guns:western"),
        "ShrineCurtain" or "ShrineArena"=>Progress.Has("curtain:inner") && Boss==null,
        "ShrineCrossing"=>BossDefeated,_=>false });
}
