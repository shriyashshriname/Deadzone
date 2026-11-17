#!/usr/bin/env python3
"""Blender headless script for generating low-poly modular crate & barrel prop.
Follows blender-3d-asset-generation skill contract and Docs/Design/ASSET_CONTRACT_GATE1.md.

Usage:
  blender --background --python scripts/blender_prop_generator.py
"""

import os
import sys

try:
    import bpy
    import bmesh
except ImportError:
    print("This script must be run inside Blender: blender --background --python blender_prop_generator.py")
    sys.exit(0)

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def build_modular_crate():
    clear_scene()
    # 1. Base Crate Box (1.2m x 0.8m x 0.7m, pivot at y=0)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.35))
    crate = bpy.context.active_object
    crate.name = "ModularCrate_LOD0"
    crate.scale = (1.2, 0.8, 0.7)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # 2. Beveled Lid
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.73))
    lid = bpy.context.active_object
    lid.name = "Lid"
    lid.scale = (1.24, 0.84, 0.08)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # Join meshes
    crate.select_set(True)
    lid.select_set(True)
    bpy.context.view_layer.objects.active = crate
    bpy.ops.object.join()

    # 3. Add Material
    mat = bpy.data.materials.new(name="M_MilitaryCrate")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.22, 0.28, 0.18, 1.0) # Olive Drab
        bsdf.inputs["Roughness"].default_value = 0.75
    crate.data.materials.append(mat)

    # 4. Smart UV Project
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')

    # Save .blend and export GLB
    out_dir = os.path.abspath("Assets/_Project/Art/Models/Props")
    os.makedirs(out_dir, exist_ok=True)
    blend_path = os.path.join(out_dir, "ModularCrate.blend")
    glb_path = os.path.join(out_dir, "ModularCrate.glb")

    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    bpy.ops.export_scene.gltf(filepath=glb_path, export_format='GLB')
    print(f"Generated: {blend_path} & {glb_path}")

if __name__ == "__main__":
    build_modular_crate()
