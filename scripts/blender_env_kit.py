"""DEADZONE environment kit generator (Blender 5.x, headless). Low-poly, art-bible palette.

    blender --background --python scripts/blender_env_kit.py -- <out_dir> [--preview <png>]

Writes one FBX per prop (pivot at ground centre, +Y up / +Z forward in Unity, metres).
Material names are shared across props (M_Env_*) so Unity can remap them to a handful of
shared URP materials and batch everything.
"""
import bpy, bmesh, math, random, sys, os
from mathutils import Vector, Euler

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
PREVIEW = argv[argv.index("--preview") + 1] if "--preview" in argv else None
os.makedirs(OUT, exist_ok=True)

PAL = {  # art bible derived (Docs/ROADMAP_NEXT.md §4); mirrored in EnvironmentBuilder.cs
    "Concrete": "6B6E6A", "ConcreteDark": "4A4D4A", "Rust": "7A4A2E", "Metal": "55595A",
    "MetalDark": "2F3336", "Wood": "5C4632", "WoodDark": "3E3024", "Grass": "6F8240",
    "GrassDry": "A79C57", "GrassDark": "4A5A2E", "Dirt": "54473A", "Hazard": "C9A227",
    "Rubber": "1F2124", "Paint": "5E6B5C", "Glass": "22303A", "Sandbag": "9C8A62",
    "Bulb": "FFB15C", "Cone": "D9662B", "White": "D8D4C8", "Leaf": "4C5A2C",
}


def lin(h):
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c] + [1]


def M(name):
    key = "M_Env_" + name
    m = bpy.data.materials.get(key)
    if m is None:
        m = bpy.data.materials.new(key)
        m.diffuse_color = lin(PAL[name])
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = lin(PAL[name])
            bsdf.inputs["Roughness"].default_value = 0.9
    return m


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


PARTS = []


def _finish(obj, mat, loc, rot, scale):
    obj.location = loc
    obj.rotation_euler = Euler([math.radians(a) for a in rot])
    obj.scale = scale
    obj.data.materials.clear()
    obj.data.materials.append(M(mat))
    PARTS.append(obj)
    return obj


def box(size, loc=(0, 0, 0), rot=(0, 0, 0), mat="Concrete", bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.object
    if bevel > 0:
        b = o.modifiers.new("b", "BEVEL"); b.width = bevel; b.segments = 1
        b.use_clamp_overlap = True
    return _finish(o, mat, loc, rot, size)


def cyl(r, h, loc=(0, 0, 0), rot=(0, 0, 0), mat="Metal", seg=8, r2=None):
    bpy.ops.mesh.primitive_cone_add(vertices=seg, radius1=r, radius2=r if r2 is None else r2, depth=h)
    return _finish(bpy.context.object, mat, loc, rot, (1, 1, 1))


def ico(r, loc=(0, 0, 0), mat="Concrete", squash=(1, 1, 0.6), jitter=0.25, seed=0, sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=r)
    o = bpy.context.object
    rnd = random.Random(seed)
    for v in o.data.vertices:
        v.co *= 1 + rnd.uniform(-jitter, jitter)
    return _finish(o, mat, loc, (rnd.uniform(0, 30), rnd.uniform(0, 30), rnd.uniform(0, 360)), squash)


def torus(R, r, loc=(0, 0, 0), rot=(0, 0, 0), mat="Rubber", seg=10, mseg=5):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, major_segments=seg, minor_segments=mseg)
    return _finish(bpy.context.object, mat, loc, rot, (1, 1, 1))


def blade(base, h, lean, yaw, width, mat):
    """Tapered 3-sided grass blade as its own small mesh."""
    me = bpy.data.meshes.new("blade")
    bm = bmesh.new()
    w = width
    a = bm.verts.new((-w, 0, 0)); b = bm.verts.new((w, 0, 0)); c = bm.verts.new((0, w * 0.8, 0))
    tip = bm.verts.new((lean * h, lean * 0.3 * h, h))
    for f in ((a, b, tip), (b, c, tip), (c, a, tip)):
        bm.faces.new(f)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new("blade", me)
    bpy.context.collection.objects.link(o)
    return _finish(o, mat, base, (0, 0, yaw), (1, 1, 1))


def flat_patch(radius, mat, seed, pts=12, z=0.012):
    me = bpy.data.meshes.new("patch")
    bm = bmesh.new()
    rnd = random.Random(seed)
    ring = []
    for i in range(pts):
        ang = i / pts * math.tau
        rr = radius * rnd.uniform(0.6, 1.0)
        ring.append(bm.verts.new((math.cos(ang) * rr, math.sin(ang) * rr * rnd.uniform(0.7, 1.0), z)))
    bm.faces.new(ring)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new("patch", me)
    bpy.context.collection.objects.link(o)
    return _finish(o, mat, (0, 0, 0), (0, 0, 0), (1, 1, 1))


def export(name):
    global PARTS
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in PARTS:
        o.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    # apply modifiers then join into one mesh object
    for o in PARTS:
        bpy.context.view_layer.objects.active = o
        for m in list(o.modifiers):
            bpy.ops.object.modifier_apply(modifier=m.name)
    for o in PARTS:
        o.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # flat shading for the low-poly look
    for p in obj.data.polygons:
        p.use_smooth = False
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", path_mode="STRIP")
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    dims = tuple(round(d, 2) for d in obj.dimensions)
    print(f"ENVKIT {name} tris={tris} dims={dims}")
    PARTS = []
    return obj


# ------------------------------------------------------------------ props
def grass(name, seed, n, hmin, hmax, mats, spread=0.35, width=0.035):
    reset(); rnd = random.Random(seed)
    for i in range(n):
        ang = rnd.uniform(0, math.tau); d = rnd.uniform(0, spread)
        blade((math.cos(ang) * d, math.sin(ang) * d, 0), rnd.uniform(hmin, hmax),
              rnd.uniform(0.05, 0.35), rnd.uniform(0, 360), width, rnd.choice(mats))
    export(name)


def bush_dead(name, seed):
    reset(); rnd = random.Random(seed)
    for i in range(9):
        yaw = rnd.uniform(0, 360); tilt = rnd.uniform(20, 55); h = rnd.uniform(0.5, 1.0)
        rad = math.radians(tilt); y = math.radians(yaw)
        cx = math.sin(rad) * h / 2 * math.cos(y); cy = math.sin(rad) * h / 2 * math.sin(y)
        cyl(0.025, h, (cx, cy, math.cos(rad) * h / 2), (0, tilt, yaw), "WoodDark", 5, r2=0.008)
    for i in range(5):
        ico(0.12, (rnd.uniform(-.3, .3), rnd.uniform(-.3, .3), rnd.uniform(.25, .6)), "Leaf", (1, 1, .7), .3, seed + i)
    export(name)


def rock(name, seed, r):
    reset(); ico(r, (0, 0, r * 0.35), "ConcreteDark", (1, 0.8, 0.55), 0.3, seed)
    ico(r * 0.45, (r * 0.9, r * 0.2, r * 0.15), "Concrete", (1, 1, .6), .3, seed + 7)
    export(name)


def pole_leaning(name):
    reset(); tilt = 12
    cyl(0.13, 5.0, (math.sin(math.radians(tilt)) * 2.5, 0, 2.45), (0, tilt, 0), "Wood", 8, r2=0.11)
    top = (math.sin(math.radians(tilt)) * 4.6, 0, 4.5)
    box((0.12, 1.8, 0.12), top, (0, tilt, 0), "WoodDark")
    for s in (-0.75, 0.75):
        cyl(0.05, 0.18, (top[0], s, top[2] + 0.14), (0, tilt, 0), "White", 6)
    # snapped wire hanging down
    cyl(0.012, 2.4, (top[0] + 0.3, 0.75, 3.2), (0, 25, 0), "MetalDark", 4)
    box((0.5, 0.5, 0.25), (0, 0, 0.12), (0, 0, 15), "ConcreteDark")  # base chunk
    export(name)


def pole_snapped(name):
    reset()
    cyl(0.13, 1.3, (0, 0, 0.65), (0, 0, 0), "Wood", 8)
    for i in range(5):  # splinters
        a = i / 5 * math.tau
        cyl(0.03, 0.3, (math.cos(a) * .08, math.sin(a) * .08, 1.35), (math.cos(a) * 15, math.sin(a) * 15, 0), "Wood", 4, r2=0.005)
    cyl(0.12, 3.6, (2.0, 0.3, 0.14), (0, 90, 8), "Wood", 8)   # fallen top section
    box((0.1, 1.6, 0.1), (3.5, 0.5, 0.25), (8, 0, 8), "WoodDark")
    export(name)


def lamp_bent(name):
    reset()
    cyl(0.08, 2.6, (0, 0, 1.3), (0, 0, 0), "Metal", 8)
    cyl(0.07, 2.2, (0.45, 0, 3.4), (0, 24, 0), "Metal", 8)       # bent upper section
    box((1.2, 0.08, 0.08), (1.4, 0, 4.35), (0, -8, 0), "Metal")  # arm
    box((0.5, 0.26, 0.14), (2.0, 0, 4.25), (0, -8, 0), "MetalDark")
    box((0.36, 0.18, 0.03), (2.0, 0, 4.17), (0, -8, 0), "Bulb")
    box((0.4, 0.4, 0.15), (0, 0, 0.07), (0, 0, 0), "ConcreteDark")
    export(name)


def rebar_chunk(name, seed):
    reset(); rnd = random.Random(seed)
    box((1.6, 1.1, 0.35), (0, 0, 0.17), (rnd.uniform(-4, 4), rnd.uniform(-6, 6), 12), "Concrete", 0.04)
    box((0.7, 0.6, 0.3), (0.9, 0.5, 0.12), (0, 18, 40), "ConcreteDark", 0.03)
    for i in range(7):
        x = rnd.uniform(-0.6, 0.6); y = rnd.uniform(-0.4, 0.4); h = rnd.uniform(0.6, 1.4)
        tx, ty = rnd.uniform(-35, 35), rnd.uniform(-35, 35)
        cyl(0.018, h, (x, y, 0.3 + h / 2 * 0.9), (tx, ty, 0), "Rust", 5)
        cyl(0.018, h * 0.5, (x + math.sin(math.radians(ty)) * h * .9, y - math.sin(math.radians(tx)) * h * .9, 0.3 + h * 0.95),
            (tx + rnd.uniform(40, 80), ty, 0), "Rust", 5)  # bent end
    export(name)


def rubble(name, seed, n=9, r=1.4):
    reset(); rnd = random.Random(seed)
    for i in range(n):
        s = rnd.uniform(0.2, 0.55)
        box((s * rnd.uniform(1, 1.8), s, s * rnd.uniform(.4, .9)),
            (rnd.uniform(-r, r) * .6, rnd.uniform(-r, r) * .6, s * .3), (rnd.uniform(-20, 20), rnd.uniform(-20, 20), rnd.uniform(0, 360)),
            rnd.choice(["Concrete", "ConcreteDark"]), 0.02)
    for i in range(3):
        cyl(0.015, rnd.uniform(.5, 1), (rnd.uniform(-.6, .6), rnd.uniform(-.6, .6), 0.2), (rnd.uniform(50, 80), 0, rnd.uniform(0, 360)), "Rust", 4)
    export(name)


def fence(name, broken=False, seed=3):
    reset(); rnd = random.Random(seed); L, H = 3.0, 2.0
    for x in (-L / 2, L / 2):
        tilt = rnd.uniform(10, 18) if (broken and x > 0) else 0
        cyl(0.045, H + 0.2, (x + math.sin(math.radians(tilt)) * H / 2, 0, (H + .2) / 2), (0, tilt, 0), "Metal", 6)
    box((L, 0.04, 0.04), (0, 0, H), (0, -6 if broken else 0, 0), "Metal")
    step = 0.22
    for i in range(int(L / step)):
        x = -L / 2 + step / 2 + i * step
        if broken and 0.2 < x < 1.1:
            continue  # torn hole
        sag = (rnd.uniform(0, .25) if broken and x > 1.1 else 0)
        box((0.012, 0.012, H - sag), (x, 0, (H - sag) / 2), (0, 0, 0), "MetalDark")
    for z in (0.5, 1.0, 1.5):
        box((L, 0.012, 0.012), (0, 0, z), (0, 0, 0), "MetalDark")
    if broken:
        box((0.9, 0.012, 0.6), (0.65, 0.35, 0.05), (80, 0, 10), "MetalDark")  # torn mesh on ground
    export(name)


def sandbags(name, seed):
    reset(); rnd = random.Random(seed)
    for row in range(3):
        n = 5 - row
        for i in range(n):
            x = (i - (n - 1) / 2) * 0.62 + rnd.uniform(-.03, .03)
            box((0.6, 0.34, 0.2), (x, 0, 0.1 + row * 0.19), (0, rnd.uniform(-3, 3), rnd.uniform(-6, 6)), "Sandbag", 0.07)
    export(name)


def tires(name, seed):
    reset(); rnd = random.Random(seed)
    for i in range(3):
        torus(0.36, 0.13, (rnd.uniform(-.05, .05), rnd.uniform(-.05, .05), 0.13 + i * 0.25), (0, 0, rnd.uniform(0, 360)))
    torus(0.36, 0.13, (0.85, 0.2, 0.36), (75, 0, 30))
    export(name)


def car_wreck(name, seed):
    reset(); rnd = random.Random(seed)
    box((4.1, 1.75, 0.75), (0, 0, 0.62), (0, 1.5, 0), "Paint", 0.06)
    box((2.1, 1.6, 0.6), (-0.3, 0, 1.25), (0, 0, 0), "Paint", 0.06)
    box((2.0, 1.62, 0.42), (-0.3, 0, 1.25), (0, 0, 0), "Glass")
    box((1.3, 1.5, 0.35), (1.25, 0, 1.08), (0, -18, 0), "Rust", 0.03)   # crumpled hood
    for (x, y, miss) in ((1.3, .82, False), (1.3, -.82, False), (-1.35, .82, True), (-1.35, -.82, False)):
        if miss:
            box((0.25, 0.25, 0.25), (x, y, 0.2), (0, 0, 0), "MetalDark")  # axle on a block
            continue
        cyl(0.36, 0.24, (x, y, 0.36), (90, 0, 0), "Rubber", 10)
    for i in range(4):
        box((rnd.uniform(.3, .8), rnd.uniform(.2, .5), 0.03), (rnd.uniform(-1.8, 1.8), rnd.uniform(-.7, .7), 1.03), (0, 0, rnd.uniform(0, 90)), "Rust")
    export(name)


def drums(name, toppled, seed):
    reset(); rnd = random.Random(seed)
    if toppled:
        cyl(0.3, 0.88, (0, 0, 0.3), (90, 0, 15), "Rust", 10)
        flat_patch(0.9, "Rubber", seed); PARTS[-1].location = (0.7, 0.4, 0)  # spill
        cyl(0.3, 0.88, (-0.7, 0.3, 0.44), (0, 0, 0), "Hazard", 10)
    else:
        for i, (x, y) in enumerate(((0, 0), (0.62, 0.1), (0.3, 0.55))):
            cyl(0.3, 0.88, (x, y, 0.44), (rnd.uniform(-3, 3), 0, 0), rnd.choice(["Rust", "Hazard", "Metal"]), 10)
    export(name)


def pallet(name, seed):
    reset(); rnd = random.Random(seed)
    for i in range(5):
        box((1.2, 0.14, 0.03), (0, -0.4 + i * 0.2, 0.13), (0, 0, rnd.uniform(-2, 2)), "Wood")
    for y in (-0.4, 0, 0.4):
        box((1.2, 0.1, 0.1), (0, y, 0.05), (0, 0, 0), "WoodDark")
    for i in range(3):
        box((rnd.uniform(1, 1.8), 0.15, 0.03), (rnd.uniform(.5, 1.2), rnd.uniform(.4, 1), 0.04 + i * .03), (0, 0, rnd.uniform(0, 180)), "Wood")
    export(name)


def cone(name):
    reset()
    box((0.38, 0.38, 0.04), (0, 0, 0.02), (0, 0, 0), "Cone")
    cyl(0.16, 0.6, (0, 0, 0.34), (0, 0, 0), "Cone", 10, r2=0.03)
    cyl(0.125, 0.09, (0, 0, 0.38), (0, 0, 0), "White", 10, r2=0.1)
    export(name)


def sign_bent(name):
    reset()
    cyl(0.035, 1.6, (0, 0, 0.8), (0, 0, 0), "Metal", 6)
    cyl(0.035, 0.9, (0.18, 0, 1.95), (0, 24, 0), "Metal", 6)
    box((0.75, 0.04, 0.75), (0.42, 0.03, 2.35), (0, 24 + 45, 0), "Hazard")
    box((0.12, 0.05, 0.4), (0.42, 0.04, 2.35), (0, 24, 0), "MetalDark")  # '!' glyph
    export(name)


def barbed(name):
    reset()
    for i in range(7):
        torus(0.4, 0.012, (-1.5 + i * 0.5, 0, 0.42), (90, 0, 20 if i % 2 else -20), "MetalDark", 10, 3)
    for x in (-1.6, 1.6):
        box((0.06, 0.06, 1.0), (x, 0, 0.5), (0, 0, 0), "WoodDark")
    export(name)


def dirt(name, seed, r):
    reset(); flat_patch(r, "Dirt", seed, 14)
    flat_patch(r * 0.55, "GrassDark", seed + 3, 10, 0.016); PARTS[-1].location = (r * .3, r * .2, 0)
    export(name)


def barrier_broken(name):
    reset()
    box((1.1, 0.7, 0.9), (-0.75, 0, 0.45), (0, 0, 0), "Concrete", 0.05)
    box((0.9, 0.45, 0.6), (0.4, 0.05, 0.3), (0, 0, 8), "Concrete", 0.05)
    box((0.5, 0.4, 0.35), (1.2, 0.3, 0.17), (12, 20, 35), "ConcreteDark", 0.04)
    for i in range(3):
        cyl(0.015, 0.6, (-0.15 + i * .08, 0, 0.75), (0, 25 + i * 10, 0), "Rust", 4)
    box((2.6, 0.72, 0.08), (0, 0, 0.04), (0, 0, 0), "Hazard")  # stripe base
    export(name)


def planks(name, seed):
    reset(); rnd = random.Random(seed)
    for i in range(6):
        box((rnd.uniform(1.2, 2.2), 0.18, 0.04), (rnd.uniform(-.6, .6), rnd.uniform(-.6, .6), 0.02 + i * 0.03),
            (0, rnd.uniform(-6, 6), rnd.uniform(0, 180)), rnd.choice(["Wood", "WoodDark"]))
    export(name)


grass("Grass_A", 1, 12, 0.22, 0.45, ["Grass", "GrassDark"])
grass("Grass_B", 2, 16, 0.25, 0.55, ["Grass", "GrassDry", "GrassDark"], spread=0.45)
grass("Grass_C", 3, 10, 0.18, 0.35, ["GrassDry", "Grass"], spread=0.3)
grass("Weeds_Tall", 4, 14, 0.7, 1.25, ["GrassDry", "GrassDark", "Grass"], spread=0.4, width=0.045)
bush_dead("Bush_Dead", 5)
rock("Rock_A", 6, 0.6); rock("Rock_B", 7, 0.35)
pole_leaning("Pole_Leaning"); pole_snapped("Pole_Snapped"); lamp_bent("Lamp_Bent")
rebar_chunk("Rebar_Chunk_A", 8); rebar_chunk("Rebar_Chunk_B", 9)
rubble("Rubble_Pile", 10)
fence("Fence_Chainlink"); fence("Fence_Broken", broken=True)
sandbags("Sandbags", 11); tires("Tire_Stack", 12); car_wreck("Car_Wreck", 13)
drums("Drums_Cluster", False, 14); drums("Drum_Toppled", True, 15)
pallet("Pallet_Planks", 16); cone("Traffic_Cone"); sign_bent("Sign_Bent"); barbed("Barbed_Wire")
dirt("Dirt_Patch_A", 17, 2.4); dirt("Dirt_Patch_B", 18, 1.6)
barrier_broken("Barrier_Broken"); planks("Debris_Planks", 19)


def ground(name, seed, r, outer, inner):
    reset(); flat_patch(r, outer, seed, 18, 0.006)
    flat_patch(r * 0.5, inner, seed + 5, 12, 0.01); PARTS[-1].location = (r * .25, -r * .15, 0)
    flat_patch(r * 0.3, inner, seed + 9, 10, 0.01); PARTS[-1].location = (-r * .4, r * .3, 0)
    export(name)


ground("Ground_Overgrowth", 21, 5.5, "GrassDark", "Grass")
ground("Ground_Dirt_Large", 22, 6.0, "Dirt", "GrassDark")
print("ENVKIT_DONE")
