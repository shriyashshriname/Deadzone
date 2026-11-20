# DEADZONE Asset Attribution & Licensing

**Project:** DEADZONE (Top-Down Extraction Shooter)  
**Status:** 100% Procedural / Unity Built-In Primitives  
**Audit Date:** 2026-11-17  

---

## 1. Asset Registry

| Category | Asset Description | Source / Provenance | License | Notes |
|---|---|---|---|---|
| **3D Models** | Player, Zombies (Walkers, Runners, Spitters, Tanks, Bosses), Walls, Pillars, Containers, Barricades, Substations, Extraction Helipad | Procedural Unity Primitives (Cubes, Spheres, Capsules, Cylinders) authored in scene | Built-in Unity Engine / Custom | No external 3D meshes or proprietary assets imported |
| **Shaders** | Universal Render Pipeline Lit (`Universal Render Pipeline/Lit`) | Unity Technologies (`com.unity.render-pipelines.universal`) | Unity Package License (Free / Built-in) | Official URP mobile-compatible shaders |
| **Materials** | Tactical color palettes (`Mat_Player`, `Mat_Zombie`, `Mat_Floor`, `Mat_Wall`, `Mat_Cargo_Blue`, etc.) | Procedurally generated via `MaterialSetupHelper.cs` and `UrpSetupHelper.cs` | Custom Studio IP / MIT Equivalent | Pure RGB albedo and PBR values |
| **Audio** | Weapon gunfire, hit pops, explosions, alerts | Synthesized placeholder audio / Unity audio clips generated via `AudioManager.cs` | Custom Studio IP / MIT Equivalent | Zero external commercial audio assets |
| **UI Sprites & Fonts** | Health bars, radar blips, hit markers, virtual touch joysticks | Procedural Unity UI shapes and Unity standard UI sprites (`com.unity.ugui`) | Unity Technologies | Zero external paid font licenses |
| **Characters (3D)** | Rigged humanoid `characterMedium` model + idle/run animations; skins survivorMaleB, survivorFemaleA, zombieA, zombieC (Survivors), zombieFemaleA, zombieMaleA (Retro), criminalMaleA (Protagonists) — regraded to the DEADZONE palette by `scripts/grade_skins.py` | Kenney — Animated Characters Survivors 1.1 / Retro / Protagonists (www.kenney.nl), originals in `Art_Source/CC0/` with each pack's License.txt | **CC0 1.0** (public domain; credit optional) | Added 2026-11-20. Credit "Kenney (kenney.nl)" in game credits as courtesy |
| **Creatures (3D)** | Demon, Yeti (Ultimate Monsters); Trilobite robot (Sci-Fi Essentials Kit, free Standard tier) — decimated by `scripts/blender_creature_import.py`, monster atlas regraded darker | Quaternius (quaternius.com), originals in `Art_Source/CC0/quaternius_*` (git-ignored) | **CC0 1.0** | Added 2026-11-20 |
| **Weapons (3D)** | Pistol, Revolver, Rifle, Sniper + textures (Sci-Fi Essentials Kit, Standard) | Quaternius | **CC0 1.0** | Added 2026-11-20 |
| **Animations** | Zombie idle / walk / scratch, hit knockback (Universal Animation Library 2, Standard) | Quaternius | **CC0 1.0** | Added 2026-11-20 |
| **Environment kit, helipad, dropship** | 32 procedural low-poly props | `scripts/blender_env_kit.py` (Claude, for DEADZONE) | Studio IP | Added 2026-11-20 |
| **Code & Architecture** | 51+ modular production C# scripts | Authored by DEADZONE Studio Team | Proprietary Studio IP | Written strictly to studio standards |

---

## 2. External Dependencies Audit

- **Third-Party Store Assets:** NONE. CC0 art only (Kenney + Quaternius packs, see table).
- **Paid Assets / Subscriptions:** NONE.
- **Tracking / Telemetry SDKs:** NONE (All tracking is offline, local JSON in `run_metrics.json`).
- **External Network APIs:** NONE.

All game assets are internal, procedural, provided by the Unity engine, or CC0 public-domain (Kenney).
