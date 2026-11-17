# DEADZONE — Gate 1 3D Asset Contract

**Author:** ART (Art & Audio Lead)  
**Reviewed by:** ARCH & QA  
**Date:** 2026-11-17  
**Status:** APPROVED FOR PRODUCTION  

---

## 1. Technical Budget & Constraints

| Asset Class | Max Triangles | Draw Calls | Material Slots | Collision Type | Texture Target |
|---|---|---|---|---|---|
| **Small Props** (Crates, Barrels, Gas Cans) | $\le 500$ tris | 1 | 1 | Simple Box / Capsule Collider | 512x512 PBR / Texture Atlas |
| **Medium Props** (Barricades, Cover Barriers) | $\le 800$ tris | 1 | 1 | Box / Compound Box Collider | 512x512 PBR |
| **Structural** (Modular Walls, Gates, Pillars) | $\le 1,200$ tris | 1 | 1 | Mesh / Box Collider | 1024x1024 PBR |
| **Characters** (Operator, AI Rivals, Squadmate) | $\le 2,500$ tris | 1-2 | 1 | Capsule Collider ($r=0.45\text{m}, h=1.8\text{m}$) | 1024x1024 PBR |
| **Infected** (Walkers, Runners, Spitters) | $\le 2,000$ tris | 1 | 1 | Capsule Collider ($r=0.45\text{m}, h=1.75\text{m}$) | 1024x1024 PBR |
| **Bosses** (Subject Zero, Crawler King) | $\le 4,000$ tris | 1-2 | 1-2 | Compound Box / Sphere Colliders | 1024x1024 PBR |

---

## 2. Coordinate System & Standards
- **Engine Target:** Unity 6 (Universal Render Pipeline).
- **Scale:** Real-world metric ($1.0\text{ unit} = 1.0\text{ meter}$).
- **Axes:** $+Y$ Up, $+Z$ Forward, $+X$ Right.
- **Pivot Origin:** Permanently at the ground base ($y = 0.0\text{m}$) centered on $x=0, z=0$ so placement snaps directly onto floor planes.
- **Topology:** Manifold geometry only, zero loose vertices, zero unapplied scaling (`(1, 1, 1)` scale).
- **Format:** Wavefront `.obj` and glTF/GLB with embedded normals and planar/box unwrapped UV coordinates.

---

## 3. Visual Style Guidelines (Grounded Outbreak)
- **Palette:** Desaturated industrial military tones (olive drab, gunmetal gray, tarnished concrete, hazard yellow, warning orange).
- **Silhouette:** Bold, blocky, highly legible from top-down isometric camera angle ($55^\circ$ pitch at $14\text{m}$ distance).
- **Readability:** High contrast between interactive gameplay elements (amber ammo, emerald medical, golden airdrops) and background industrial concrete.
