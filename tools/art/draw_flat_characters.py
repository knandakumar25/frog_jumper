"""Original 2D cutout animation. Run with Blender --background --python this_file.

No source artwork, model, or project resource is read. Every mark is a planar
polygon in an orthographic drawing; emission materials provide solid ink colors.
"""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / "assets/characters/rendered"
OUT.mkdir(parents=True, exist_ok=True)
INK = "#11182b"
PAPER = "#fff3d9"
DESIGNS = {
    "cyber_shogun": ("#e85945", "#f8c868", "#28364c", 1.0),
    "surya_prime": ("#edaa32", "#fff0a2", "#754053", 1.22),
    "oceanus": ("#30bdc0", "#b4f6e4", "#24487c", .92),
    "mirage_blade": ("#a886e6", "#f0d7ff", "#393456", .87),
    "apex_titan": ("#6cbd77", "#eced91", "#364c58", 1.35),
    "phoenicia": ("#ed73ae", "#ffe3a0", "#6556a6", .94),
    "axiom7": ("#e3e9db", "#6ae1cb", "#3c5971", .82),
    "vanguard": ("#e69d60", "#ffe3ae", "#484b67", .95),
    "heavy": ("#ae6778", "#ffcb8c", "#43465f", 1.45),
}
MATS = {}
depth = 0


def mat(color):
    if color not in MATS:
        # Convert sRGB authored swatches to scene-linear emission values.
        rgb = [int(color[i:i+2], 16) / 255 for i in (1, 3, 5)]
        rgb = [v / 12.92 if v <= .04045 else ((v + .055) / 1.055)**2.4 for v in rgb]
        m = bpy.data.materials.new(color)
        m.use_nodes = True
        nodes = m.node_tree.nodes
        nodes.clear()
        out = nodes.new("ShaderNodeOutputMaterial")
        shader = nodes.new("ShaderNodeEmission")
        shader.inputs["Color"].default_value = (*rgb, 1)
        m.node_tree.links.new(shader.outputs[0], out.inputs["Surface"])
        MATS[color] = m
    return MATS[color]


def face(points, color):
    global depth
    depth += .0001
    mesh = bpy.data.meshes.new("flat_ink")
    mesh.from_pydata([(x, y, depth) for x, y in points], [], [list(range(len(points)))])
    mesh.materials.append(mat(color))
    obj = bpy.data.objects.new("2D polygon", mesh)
    bpy.context.collection.objects.link(obj)


def poly(points, color, outline=2):
    if outline:
        cx = sum(p[0] for p in points) / len(points)
        cy = sum(p[1] for p in points) / len(points)
        outer = []
        for x, y in points:
            dx, dy = x-cx, y-cy
            length = max(1, math.hypot(dx, dy))
            outer.append((x + outline*dx/length, y + outline*dy/length))
        face(outer, INK)
    face(points, color)


def disc(x, y, radius, color, outline=1.5, sides=12):
    poly([(x + radius*math.cos(i*math.tau/sides),
           y + radius*math.sin(i*math.tau/sides)) for i in range(sides)], color, outline)


def stroke(a, b, width, color, outline=1.4):
    dx, dy = b[0]-a[0], b[1]-a[1]
    length = max(1, math.hypot(dx, dy))
    nx, ny = -dy/length*width/2, dx/length*width/2
    poly([(a[0]+nx,a[1]+ny), (b[0]+nx,b[1]+ny),
          (b[0]-nx,b[1]-ny), (a[0]-nx,a[1]-ny)], color, outline)


def character(kind, action="idle", frame=0, offset=(0, 0), scale=1):
    """Draw articulated flat shapes. All poses computed in 2D, never from a rig."""
    primary, accent, dark, bulk = DESIGNS[kind]
    phase = frame / 8 * math.tau
    running = action == "running"
    jumping = action == "jumping"
    dash = action == "blade_dash"
    sword = action == "sword_attack"
    kick = action == "karate_attack"
    bob = 1.4*math.sin(phase) if action == "idle" else 0
    if running:
        bob = 2*abs(math.cos(phase))
    lean = 13 if running else 21+2*math.sin(phase) if dash else 0
    ox, oy = offset
    # Local wrappers give every sprite an identical ground anchor.
    def p(x, y):
        return (ox + (x-128)*scale, oy + (y-32+bob)*scale)
    def P(points, color, outline=2):
        poly([p(x,y) for x,y in points], color, outline*scale)
    def D(x,y,r,c,outline=1.5):
        disc(*p(x,y),r*scale,c,outline*scale)
    def L(a,b,w,c,outline=1.4):
        stroke(p(*a),p(*b),w*scale,c,outline*scale)
    hx, hy = 128+lean, 192
    hip = (128,112)
    shoulder = (128+lean,163)
    # Different rear silhouettes: split coat, solar mantle, water ribbons,
    # assassin scarf, tank fins, and harmonic feather fan.
    flutter = math.sin(phase)*5
    if kind == "cyber_shogun":
        P([(111,143),(99-flutter,77),(120,88),(132,116),(151,76),(158,92),(145,147)],dark)
        L((108,100),(118,134),3,primary,0)
    elif kind == "surya_prime":
        P([(103,172),(81,78),(111,86),(125,132),(147,81),(176,93),(152,173)],dark)
        P([(103,170),(94,109),(104,114),(119,169)],accent,0)
    elif kind in ("oceanus","mirage_blade"):
        P([(hx-10,181),(84,177),(62,151+flutter),(80,154),(93,168),(hx,172)],primary)
        if kind == "oceanus":
            P([(142,128),(172,102+flutter),(184,74),(165,91),(138,112)],accent)
    elif kind == "phoenicia":
        for j in range(3):
            P([(116,162),(67+j*11,170-j*18+flutter),(88+j*9,122-j*13),(125,140)],
              [dark,primary,accent][j])
    elif kind in ("apex_titan","heavy"):
        P([(100,169),(83,176),(85,142),(104,133)],dark)
        P([(146,168),(172,178),(170,139),(148,130)],dark)
    # Legs have genuine joint motion; boots stay clear of frame edges.
    for side in (-1,1):
        step = math.sin(phase)*side if running else .12*side
        knee = (128+side*12+step*19, 78 + max(0,-step)*13)
        foot = (128+side*22+step*24, 36+max(0,-step)*21)
        if jumping:
            knee=(112+side*24+3*math.sin(phase),90+side*10)
            foot=(109+side*33+5*math.sin(phase),64+side*10+3*math.cos(phase))
        if dash:
            knee=(104+side*24,85)
            foot=(80+side*30,43+side*5)
        if kick and side == 1:
            extension=math.sin(frame/7*math.pi)
            knee=(146+extension*22,84+extension*25)
            foot=(154+extension*56,38+extension*79)
        L((hip[0]+side*9,hip[1]),knee,17*bulk,dark)
        D(*knee,8*bulk,primary)
        L(knee,foot,14*bulk,primary)
        P([(foot[0]-9*bulk,foot[1]+7),(foot[0]+8*bulk,foot[1]+7),
           (foot[0]+15*bulk,foot[1]-3),(foot[0]-10*bulk,foot[1]-3)],dark)
        L((knee[0]-2,knee[1]-5),(foot[0]-2,foot[1]+9),3,accent,0)
    # Chest silhouette, waist, center emblem.
    w=21*bulk
    P([(shoulder[0]-w,170),(shoulder[0]+w,170),(144,119),(112,119)],primary)
    P([(shoulder[0]-w+5,164),(shoulder[0]-2,155),(124,126),(115,132)],dark,0)
    P([(110,123),(146,123),(149,111),(108,111)],dark)
    D(129,117,4,accent,0)
    if kind == "surya_prime":
        D(shoulder[0]+3,150,11,accent)
        for a in range(8):
            angle=a*math.tau/8
            L((shoulder[0]+3+13*math.cos(angle),150+13*math.sin(angle)),
              (shoulder[0]+3+17*math.cos(angle),150+17*math.sin(angle)),2,accent,0)
    elif kind in ("apex_titan","heavy"):
        for j in range(3):
            L((shoulder[0]-9,153-j*8),(shoulder[0]+13,153-j*8),4,accent,0)
    else:
        P([(shoulder[0]+5,162),(shoulder[0]+13,150),(shoulder[0]+3,136),
           (shoulder[0]-2,149)],accent,0)
    # Rear arm, then expressive head and front arm.
    rear_elbow=(shoulder[0]-26,139+math.sin(phase)*8 if running else 139)
    rear_hand=(shoulder[0]-23,116)
    L((shoulder[0]-w,160),rear_elbow,13*bulk,dark)
    L(rear_elbow,rear_hand,12*bulk,primary)
    D(*rear_hand,7,dark)
    L((hx,170),(hx,182),12,dark)
    P([(hx-15,205),(hx+12,207),(hx+20,193),(hx+13,178),(hx-12,178),(hx-18,191)],dark)
    if kind == "cyber_shogun":
        P([(hx-19,198),(hx-13,214),(hx+11,214),(hx+21,197)],primary)
        P([(hx-15,207),(hx-23,219),(hx-22,198),(hx-9,201)],accent)
        P([(hx+12,209),(hx+24,220),(hx+20,199),(hx+9,201)],accent)
        L((hx-10,194),(hx+13,194),4,accent,0)
        P([(hx-11,185),(hx+12,186),(hx+7,179),(hx-6,179)],primary,0)
    elif kind == "surya_prime":
        P([(hx-16,199),(hx-12,217),(hx+1,211),(hx+13,217),(hx+18,199)],accent)
        P([(hx-10,198),(hx+12,198),(hx+11,184),(hx-6,181)],"#b96943")
        L((hx-7,192),(hx+1,192),2,INK,0)
        L((hx+6,192),(hx+11,192),2,INK,0)
    elif kind == "oceanus":
        P([(hx-18,192),(hx-20,212),(hx+1,220),(hx+19,207),(hx+12,201),(hx-7,200)],accent)
        P([(hx-7,199),(hx+13,201),(hx+16,189),(hx+7,180),(hx-8,185)],"#ac715b")
        L((hx+3,194),(hx+11,195),2,INK,0)
        D(hx-11,190,4,primary)
    elif kind == "mirage_blade":
        P([(hx-20,178),(hx-21,201),(hx-3,219),(hx+17,204),(hx+20,178),
           (hx+8,184),(hx+13,198),(hx-5,202),(hx-13,189)],primary)
        L((hx-7,194),(hx+10,194),3,accent,0)
    elif kind == "phoenicia":
        P([(hx-16,183),(hx-23,206),(hx-11,217),(hx+8,215),(hx+23,200),
           (hx+10,199),(hx-5,207),(hx-8,188)],primary)
        P([(hx-7,199),(hx+12,199),(hx+14,186),(hx+3,179),(hx-9,187)],"#ce956d")
        L((hx+1,193),(hx+9,193),2,INK,0)
        P([(hx-16,208),(hx-6,225),(hx-1,211)],accent)
    else:
        P([(hx-16,208),(hx+13,208),(hx+20,199),(hx+17,184),(hx-17,184)],primary)
        P([(hx-13,200),(hx+15,200),(hx+12,190),(hx-10,190)],dark)
        if kind == "axiom7":
            D(hx-4,196,3,accent,0)
            D(hx+8,196,3,accent,0)
            L((hx,210),(hx+4,219),2,primary)
            D(hx+4,220,3,accent)
        else:
            L((hx-9,195),(hx+12,195),4,accent,0)
    elbow=(shoulder[0]+28,139)
    hand=(shoulder[0]+32,120)
    if running:
        elbow=(shoulder[0]+23-math.sin(phase)*14,145)
        hand=(shoulder[0]+32-math.sin(phase)*22,151)
    if sword:
        angle=1.7-frame/7*2.5
        elbow=(shoulder[0]+21*math.cos(angle),134+21*math.sin(angle))
        hand=(elbow[0]+25*math.cos(angle),elbow[1]+25*math.sin(angle))
    if dash:
        elbow=(shoulder[0]+20,151)
        hand=(shoulder[0]+34,155)
    L((shoulder[0]+w-3,160),elbow,15*bulk,primary)
    D(shoulder[0]+w-1,165,10*bulk,primary)
    L(elbow,hand,13*bulk,dark)
    D(*hand,7*bulk,primary)
    # Equipment is part of the silhouette, not a recolor of a shared sprite.
    x,y=hand
    if kind in ("cyber_shogun","mirage_blade"):
        angle = (1.7-frame/7*2.5) if sword else .45 if dash else 1.1
        length=47 if kind=="cyber_shogun" else 29
        dx,dy=math.cos(angle),math.sin(angle)
        L((x-7*dx,y-7*dy),(x+5*dx,y+5*dy),5,dark)
        L((x-5*dy,y+5*dx),(x+5*dy,y-5*dx),3,accent)
        P([(x+4*dx-2*dy,y+4*dy+2*dx),(x+length*dx,y+length*dy),
           (x+(length-7)*dx+3*dy,y+(length-7)*dy-3*dx),
           (x+4*dx+3*dy,y+4*dy-3*dx)],PAPER,1)
        if kind=="mirage_blade":
            L(rear_hand,(rear_hand[0]-19,rear_hand[1]+23),5,accent)
    elif kind == "surya_prime":
        P([(x-14,y+20),(x+10,y+23),(x+19,y+4),(x+7,y-23),(x-15,y-10)],accent)
        P([(x-8,y+13),(x+5,y+15),(x+11,y+3),(x+3,y-13),(x-9,y-6)],primary)
    elif kind == "oceanus":
        L((x-8,y-25),(x+19,y+47),4,dark)
        P([(x+16,y+41),(x+10,y+53),(x+19,y+50),(x+22,y+62),
           (x+27,y+48),(x+35,y+52),(x+27,y+36)],accent)
    elif kind in ("apex_titan","heavy"):
        P([(x-10,y+10),(x+16,y+10),(x+20,y-9),(x+13,y-18),(x-10,y-17)],dark)
        for j in range(3):
            L((x-5+j*7,y+5),(x-5+j*7,y-8),4,accent,0)
    elif kind == "phoenicia":
        P([(x+2,y+8),(x+16,y+28),(x+29,y+8),(x+15,y-9)],accent)
        P([(x+9,y+8),(x+16,y+19),(x+22,y+8),(x+15,y-1)],primary,0)
    elif kind == "vanguard":
        L((x,y),(x+34,y+4),12,dark)
        L((x+24,y+4),(x+38,y+6),5,accent)
    # Flat graphic slash trails, restricted to attack's middle frames.
    if sword and frame in (2,3,4,5):
        P([(184,208),(215,187),(231,146),(216,162),(201,185)],accent,0)
    if dash:
        for j in range(3):
            L((42+j*5,128-j*13),(80+j*5,128-j*13),2,accent,0)


def clear():
    global depth
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    depth = 0


def render(path,width,height):
    scene=bpy.context.scene
    camera_data=bpy.data.cameras.new("Orthographic drawing camera")
    camera=bpy.data.objects.new("Orthographic drawing camera",camera_data)
    scene.collection.objects.link(camera)
    camera.location=(width/2,height/2,100)
    camera_data.type="ORTHO"
    camera_data.ortho_scale=max(width,height)
    camera_data.clip_end=200
    scene.camera=camera
    scene.render.engine="CYCLES"
    scene.cycles.samples=16
    scene.cycles.use_denoising=False
    scene.render.resolution_x=width
    scene.render.resolution_y=height
    scene.render.resolution_percentage=100
    scene.render.film_transparent=True
    scene.render.image_settings.file_format="PNG"
    scene.render.image_settings.color_mode="RGBA"
    scene.view_settings.view_transform="Standard"
    scene.render.filepath=str(path)
    bpy.ops.render.render(write_still=True)
    print("FLAT_2D_RENDER",path.name,flush=True)


for kind in DESIGNS:
    clear()
    portrait_height=1024 if kind not in ("axiom7","vanguard","heavy") else 768
    character(kind, offset=(384,90),scale=3.6 if portrait_height==1024 else 2.9)
    render(OUT/f"{kind}_portrait.png",768,portrait_height)
    clear()
    actions=("idle","running","jumping","blade_dash","sword_attack","karate_attack") if portrait_height==1024 else ("idle",)
    height=len(actions)*256
    for row,action in enumerate(actions):
        for frame in range(8):
            character(kind,action,frame,offset=(frame*256+128,height-(row+1)*256+32))
    render(OUT/f"{kind}_atlas.png",2048,height)
print("FLAT_2D_RENDERING_COMPLETE",flush=True)
