"""Prepare a CC0 rigged creature / animation pack for DEADZONE (Blender 5.x, headless).

    blender --background --python scripts/blender_creature_import.py -- \
        --in <src.fbx> --out <dst.fbx> [--target-tris 2000] [--keep Idle,Walk,Run] [--strip-mesh]

- Decimates all meshes together to ~target tris (mobile budget, ASSET_CONTRACT_GATE1.md).
- Keeps only the listed actions (substring match, case-insensitive) and renames them to the bare
  clip name (e.g. 'CharacterArmature|CharacterArmature|Walk' -> 'Walk').
- --strip-mesh exports the armature + animations only (for animation libraries).
Prints one 'CREATURE {...json}' line.
"""
import argparse, json, sys
import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
ap = argparse.ArgumentParser()
ap.add_argument("--in", dest="src", required=True)
ap.add_argument("--out", required=True)
ap.add_argument("--target-tris", type=int, default=2000)
ap.add_argument("--keep", default="")
ap.add_argument("--strip-mesh", action="store_true")
a = ap.parse_args(argv)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=a.src)

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]


def tris(objs):
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)


before = tris(meshes)
if a.strip_mesh:
    for o in meshes:
        bpy.data.objects.remove(o, do_unlink=True)
    meshes = []
elif before > a.target_tris:
    ratio = max(0.05, a.target_tris / before)
    for o in meshes:
        bpy.context.view_layer.objects.active = o
        m = o.modifiers.new("dec", "DECIMATE")
        m.ratio = ratio
        m.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=m.name)

keep = [k.strip().lower() for k in a.keep.split(",") if k.strip()]
kept = []
for act in list(bpy.data.actions):
    bare = act.name.split("|")[-1]
    if keep and not any(k == bare.lower() or k in bare.lower() for k in keep):
        bpy.data.actions.remove(act)
        continue
    act.name = bare
    act.use_fake_user = True
    kept.append(bare)

# Make sure every kept action is exported as its own take
if arms:
    arm = arms[0]
    if arm.animation_data is None:
        arm.animation_data_create()
    if kept:
        arm.animation_data.action = bpy.data.actions[kept[0]]

bpy.ops.export_scene.fbx(
    filepath=a.out, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z", axis_up="Y", add_leaf_bones=False, bake_anim=bool(kept),
    bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
    use_armature_deform_only=False, path_mode="STRIP", mesh_smooth_type="FACE",
)
print("CREATURE " + json.dumps({"out": a.out, "tris_before": before, "tris_after": tris(meshes),
                                "bones": len(arms[0].data.bones) if arms else 0, "actions": kept}))
