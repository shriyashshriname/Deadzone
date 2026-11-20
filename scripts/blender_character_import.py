"""DEADZONE character import pipeline (Blender 5.x, headless).

Takes a CC0 rigged character (FBX / GLB / GLTF), makes it DEADZONE-ready and exports an FBX
that Unity can configure as a Humanoid rig.

    blender --background --python scripts/blender_character_import.py -- \
        --in  "Art_Source/CC0/Survivor.fbx" \
        --out "Assets/_Project/Art/Models/Characters/Operator.fbx" \
        --role operator            # operator | infected | boss

Steps: import -> apply scale/rotation -> pivot at feet -> scale to target height ->
recolour materials to the art-bible palette by role -> check tri budget -> export FBX
(with armature + animations). Prints a one-line JSON report for the logs.
"""
import argparse, json, sys
import bpy
from mathutils import Vector

# Art bible (Docs/ROADMAP_NEXT.md §4) — keep in sync with Assets/_Project/Scripts/Core/Palette.cs
PALETTE = {
    "operator": ["4F5A3C", "B8A47A", "1D2228", "55595A"],  # olive, coyote, shadow, gunmetal
    "infected": ["8A9479", "55595A", "D7263D", "1D2228"],  # skin, rags, blood, shadow
    "boss":     ["8A9479", "D7263D", "1D2228", "C9A227"],
}
BUDGET = {"operator": 2500, "infected": 2000, "boss": 4000}   # ASSET_CONTRACT_GATE1.md
HEIGHT = {"operator": 1.80, "infected": 1.75, "boss": 2.60}   # metres


def hex_rgba(h):
    srgb = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (*lin, 1.0)


def import_any(path):
    ext = path.lower().rsplit(".", 1)[-1]
    if ext == "fbx":
        bpy.ops.import_scene.fbx(filepath=path, automatic_bone_orientation=True)
    elif ext in ("glb", "gltf"):
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        sys.exit(f"Unsupported format: {ext}")


def world_bounds(meshes):
    pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="src", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--role", choices=PALETTE.keys(), default="operator")
    ap.add_argument("--keep-colors", action="store_true", help="skip palette recolour")
    a = ap.parse_args(argv)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    import_any(a.src)

    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    roots = [o for o in bpy.context.scene.objects if o.parent is None]
    if not meshes:
        sys.exit("No meshes found in source file")

    # Normalise: apply transforms on roots so Unity sees scale (1,1,1)
    bpy.ops.object.select_all(action="DESELECT")
    for o in roots:
        o.select_set(True)
    bpy.context.view_layer.objects.active = roots[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    # Scale to target height, pivot at feet centre (Blender Z-up; FBX export converts to Y-up)
    bpy.context.view_layer.update()
    lo, hi = world_bounds(meshes)
    s = HEIGHT[a.role] / max(hi.z - lo.z, 1e-6)
    offset = Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))
    for o in roots:
        o.location = (o.location + offset) * s
        o.scale = (s, s, s)
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=True)

    # Recolour: map materials, darkest/lightest order preserved, onto the role palette
    if not a.keep_colors:
        mats = sorted({m for o in meshes for m in o.data.materials if m}, key=lambda m: m.name)
        cols = PALETTE[a.role]
        for i, m in enumerate(mats):
            m.use_nodes = True
            bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
            if bsdf:
                for link in list(bsdf.inputs["Base Color"].links):
                    m.node_tree.links.remove(link)
                bsdf.inputs["Base Color"].default_value = hex_rgba(cols[i % len(cols)])
                bsdf.inputs["Roughness"].default_value = 0.85
            m.name = f"M_{a.role}_{i:02d}"

    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    arm = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    actions = [act.name for act in bpy.data.actions]

    bpy.ops.export_scene.fbx(
        filepath=a.out, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z", axis_up="Y", add_leaf_bones=False, bake_anim=bool(actions),
        bake_anim_use_all_actions=True, use_armature_deform_only=True, path_mode="COPY",
        embed_textures=False,
    )
    report = {
        "out": a.out, "role": a.role, "tris": tris, "budget": BUDGET[a.role],
        "within_budget": tris <= BUDGET[a.role], "rigged": bool(arm),
        "bones": len(arm[0].data.bones) if arm else 0, "animations": actions,
    }
    print("DEADZONE_IMPORT " + json.dumps(report))


main()
