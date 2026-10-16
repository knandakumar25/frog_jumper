extends Node3D

var model: Node3D
var player: AnimationPlayer
var camera: Camera3D
var title: Label
var category: Label
var elapsed := 0.0
var current_shot := -1
var review := false
var still := false
var autoplay := true
var review_step := 0
var review_clock := 0.0
var active := ""
var browse_index := -1
var browse_time := 0.0
var side_review := false
var in_place := false
var names: PackedStringArray
var shots = [
    ["067", "POWER KICK", "SPORTS", 0.48, 3.0],
    ["001", "HEAVY SWORD SLASH", "RPG / SWORD & MAGIC", 0.15, 3.0],
    ["068", "RIFLE RECOIL", "MILITARY SHOOTER", 0.0, 2.5],
    ["011", "POWER SERVE", "SPORTS", 0.3, 2.5],
    ["054", "SWORD COMBAT", "RPG / SWORD & MAGIC", 0.0, 2.5],
    ["093", "LEAPING KICK", "SPORTS / COMBAT", 0.0, 2.5],
    ["086", "DRIVING FRONT KICK", "SPORTS / COMBAT", 0.0, 2.5],
    ["002", "FIREBALL CAST", "RPG / SWORD & MAGIC", 0.0, 3.0],
    ["075", "TACTICAL SPRINT", "MILITARY SHOOTER", 0.0, 2.5],
    ["003", "DASH TELEPORT", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["004", "SHIELD PARRY", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["005", "TACTICAL RELOAD", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["006", "CROUCH TO FIRE", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["007", "LOW CRAWL", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["008", "GRENADE THROW", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["009", "WINDMILL DUNK", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["010", "BICYCLE KICK", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["083", "LOW LUNGE / TACKLE", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["087", "LEG-CATCH THROW", "FREE DEMO / 20 CLIPS", 0.0, 3.0],
    ["100", "SABER SLASH", "FREE DEMO / 20 CLIPS", 0.0, 3.0]
]
var candidates = ["001", "002", "003", "004", "005", "006", "007", "008", "009", "010", "011", "054", "067", "068", "075", "083", "086", "087", "093", "100"]
var colors = {"dark":Color("182524"),"floor":Color("607365"),"lime":Color("d8ff3e"),"coral":Color("ec6c58"),"green":Color("365541"),"stone":Color("658072")}

func material(col: Color, metallic:=0.0, roughness:=0.65) -> StandardMaterial3D:
    var m=StandardMaterial3D.new()
    m.albedo_color=col
    m.metallic=metallic
    m.roughness=roughness
    return m

func box(pos: Vector3, size: Vector3, col: Color) -> MeshInstance3D:
    var node=MeshInstance3D.new()
    var mesh=BoxMesh.new()
    mesh.size=size
    node.mesh=mesh
    node.material_override=material(col)
    node.position=pos
    add_child(node)
    return node

func cylinder(pos: Vector3, radius: float, height: float, col: Color, sides:=32) -> MeshInstance3D:
    var node=MeshInstance3D.new()
    var mesh=CylinderMesh.new()
    mesh.top_radius=radius
    mesh.bottom_radius=radius
    mesh.height=height
    mesh.radial_segments=sides
    node.mesh=mesh
    node.position=pos
    node.material_override=material(col)
    add_child(node)
    return node

func tree(pos: Vector3, height: float):
    cylinder(pos+Vector3(0,height*0.32,0),0.10,height*0.64,colors.dark,7)
    for i in range(3):
        var node=MeshInstance3D.new()
        var mesh=SphereMesh.new()
        mesh.radius=height*(0.25-i*0.025)
        mesh.height=mesh.radius*1.7
        mesh.radial_segments=7
        mesh.rings=4
        node.mesh=mesh
        node.material_override=material(colors.green.lightened(i*0.05))
        node.position=pos+Vector3(0,height*(0.55+i*0.14),0)
        add_child(node)

func label(text: String, size: int, pos: Vector2, col: Color) -> Label:
    var l=Label.new()
    l.text=text
    l.position=pos
    l.add_theme_font_size_override("font_size",size)
    l.add_theme_color_override("font_color",col)
    var font=SystemFont.new()
    font.font_names=PackedStringArray(["Bahnschrift","Arial"])
    font.font_weight=700
    l.add_theme_font_override("font",font)
    return l

func _ready():
    var args=OS.get_cmdline_user_args()
    review=args.has("--review")
    still=args.has("--still")
    var env=Environment.new()
    env.background_mode=Environment.BG_COLOR
    var sky=Sky.new()
    var sky_material=ProceduralSkyMaterial.new()
    sky_material.sky_top_color=Color("253f43")
    sky_material.sky_horizon_color=Color("90a59c")
    sky_material.ground_bottom_color=Color("243e30")
    sky_material.ground_horizon_color=Color("90a59c")
    sky.sky_material=sky_material
    env.background_mode=Environment.BG_SKY
    env.sky=sky
    env.ambient_light_source=Environment.AMBIENT_SOURCE_COLOR
    env.ambient_light_color=Color("e2f2de")
    env.ambient_light_energy=0.28
    env.tonemap_mode=Environment.TONE_MAPPER_FILMIC
    var world=WorldEnvironment.new()
    world.environment=env
    add_child(world)
    var sun=DirectionalLight3D.new()
    sun.rotation_degrees=Vector3(-48,-28,0)
    sun.light_color=Color("fff5df")
    sun.light_energy=0.72
    sun.shadow_enabled=true
    sun.directional_shadow_max_distance=45
    add_child(sun)
    box(Vector3(0,-0.24,0),Vector3(80,0.3,80),colors.green)
    cylinder(Vector3(0,-0.08,0),4.0,0.16,colors.dark,64)
    cylinder(Vector3(0,0.006,0),3.85,0.01,colors.lime,64)
    cylinder(Vector3(0,0.015,0),3.77,0.012,colors.floor,64)
    for x in range(-3,4):
        box(Vector3(x,0.025,0),Vector3(0.008,0.008,6.8),Color("b6c3aa"))
    for z in range(-3,4):
        box(Vector3(0,0.025,z),Vector3(6.8,0.008,0.008),Color("b6c3aa"))
    for i in range(4):
        box(Vector3(0,0.16+i*0.23,-5.8-i*0.55),Vector3(12,0.32,0.48),colors.dark.lightened(i*0.045))
        box(Vector3(0,0.335+i*0.23,-5.8-i*0.55),Vector3(12,0.035,0.16),colors.lime)
    for x in [-5.3,5.3]:
        box(Vector3(x,1.55,-3.8),Vector3(0.5,3.1,0.6),colors.dark)
        box(Vector3(x,2.9,-3.45),Vector3(0.56,0.14,0.03),colors.lime)
        tree(Vector3(x*1.5,0,-3),3.9)
        tree(Vector3(x*1.7,0,-6),4.8)
    box(Vector3(0,3.28,-3.8),Vector3(11.2,0.28,0.6),colors.dark)
    box(Vector3(0,3.25,-3.47),Vector3(10.3,0.055,0.015),colors.lime)
    for i in range(16):
        var angle=i*TAU/16
        var pos=Vector3(sin(angle)*9,0,cos(angle)*9)
        if pos.z<2:
            cylinder(pos+Vector3(0,0.15,0),0.6,0.3,colors.stone,6)
            tree(pos,2.8+0.5*sin(i*7))
    for x in [-4.6,4.6]:
        box(Vector3(x,0.25,-1.8),Vector3(1.0,0.5,1.0),colors.dark)
        box(Vector3(x,0.51,-1.8),Vector3(0.92,0.03,0.92),colors.coral)
    in_place=args.has("--in-place")
    var source="res://assets/white_in_place.glb" if in_place else "res://assets/white.glb"
    model=load(source).instantiate()
    add_child(model)
    player=model.find_child("AnimationPlayer",true,false) as AnimationPlayer
    assert(player!=null,"Imported rig has no AnimationPlayer")
    names=player.get_animation_list()
    print("GODOT_IMPORTED_ANIMATIONS ",names.size()," SOURCE ",source)
    var report=[]
    for n in names:
        report.append({"name":n,"duration":player.get_animation(n).length})
    DirAccess.make_dir_recursive_absolute("res://captures")
    var f=FileAccess.open("res://captures/imported_animations.json",FileAccess.WRITE)
    f.store_string(JSON.stringify(report,"  "))
    camera=Camera3D.new()
    camera.fov=38
    add_child(camera)
    side_review=args.has("--side")
    if side_review:candidates=["067","086","093"]
    camera.position=Vector3(4.0,1.8,0.2) if side_review else Vector3(1.9,2.0,4.0)
    camera.look_at(Vector3(0,1.0,0))
    camera.current=true
    var canvas=CanvasLayer.new()
    add_child(canvas)
    var heading=label("ULTIMATE / ANIMATION LIBRARY",30,Vector2(76,55),colors.lime)
    canvas.add_child(heading)
    canvas.add_child(label("R: ROOT / IN-PLACE",22,Vector2(1550,70),colors.lime))
    category=label("SPORTS",22,Vector2(80,850),colors.lime)
    canvas.add_child(category)
    title=label("POWER KICK",62,Vector2(76,880),colors.lime)
    canvas.add_child(title)
    canvas.add_child(label("20 FREE CLIPS     /     SPORTS · SHOOTERS · RPGs",23,Vector2(80,997),colors.lime))
    if args.has("--clean"):canvas.visible=false
    if args.has("--probe"):
        assert(names.size()==20,"Demo must contain exactly 20 animations")
        for n in names:
            player.play(n)
            player.seek(player.get_animation(n).length*0.5,true)
        print("DEMO_ALL_20_PLAYBACK_PASS")
        get_tree().quit()

func pick(prefix: String) -> String:
    for n in names:
        if n.begins_with(prefix+"_"):return n
    return ""

func _process(delta):
    if player==null:return
    if not autoplay:return
    if browse_index>=0:
        browse_time+=delta
        player.seek(fmod(browse_time,player.get_animation(active).length),true)
        return
    if review:
        review_clock+=delta
        var index=review_step/3
        if index>=candidates.size():
            get_tree().quit()
            return
        if review_clock<0.2:return
        var n=pick(candidates[index])
        player.play(n)
        player.seek(player.get_animation(n).length*[0.25,0.5,0.75][review_step%3],true)
        title.text=n.get_slice("_",0)+" / "+n.replace("_"," ").substr(4)
        title.add_theme_font_size_override("font_size",28)
        category.text="ACTUAL GODOT FRAME / CLIP REVIEW"
        await RenderingServer.frame_post_draw
        get_viewport().get_texture().get_image().save_png("res://captures/%s_%s_%d.png"%["side" if side_review else "review",candidates[index],review_step%3])
        review_clock=0
        review_step+=1
        return
    elapsed+=delta
    var t=0.0
    var shot=0
    for s in shots:
        if elapsed<t+s[4]:break
        t+=s[4]
        shot+=1
    if shot>=shots.size():
        if OS.has_feature("movie"):get_tree().quit()
        else:elapsed=0;current_shot=-1
        return
    if current_shot!=shot:
        current_shot=shot
        active=pick(shots[shot][0])
        title.text=shots[shot][1]
        category.text=shots[shot][2]
        player.play(active)
        player.seek(shots[shot][3],true)
        print("SHOWCASE_SHOT ",shot," ",active)
    var local=elapsed-t
    var angle=0.43+0.055*sin(local*1.15)+0.08*sin(shot*1.8)
    var distance=4.7-0.22*local/shots[shot][4]
    camera.position=Vector3(sin(angle)*distance,2.0,cos(angle)*distance)
    camera.look_at(Vector3(0,1.0,0))
    player.seek(fmod(local+shots[shot][3],player.get_animation(active).length),true)
    if still and elapsed>1:
        await RenderingServer.frame_post_draw
        get_viewport().get_texture().get_image().save_png("res://captures/hero.png")
        get_tree().quit()

func _unhandled_key_input(event):
    if not event is InputEventKey or not event.pressed:return
    if event.keycode==KEY_R:
        in_place=not in_place
        var old=model
        model=load("res://assets/white_in_place.glb" if in_place else "res://assets/white.glb").instantiate()
        add_child(model)
        player=model.find_child("AnimationPlayer",true,false) as AnimationPlayer
        names=player.get_animation_list()
        old.queue_free()
        if browse_index>=0:
            player.play(active)
            player.seek(browse_time,true)
        else:current_shot=-1
        category.text="IN-PLACE" if in_place else "ROOT MOTION"
    elif event.keycode==KEY_SPACE:
        autoplay=not autoplay
        player.pause()
    elif event.keycode==KEY_RIGHT or event.keycode==KEY_LEFT:
        browse_index=posmod(browse_index+(1 if event.keycode==KEY_RIGHT else -1),names.size())
        active=names[browse_index]
        browse_time=0.0
        autoplay=true
        player.play(active)
        title.text=active.replace("_"," ")
        title.add_theme_font_size_override("font_size",28)
        category.text="CLIP BROWSER / LEFT & RIGHT / SPACE TO PAUSE"
    elif event.keycode==KEY_HOME:
        browse_index=-1
        elapsed=0.0
        current_shot=-1
        autoplay=true
