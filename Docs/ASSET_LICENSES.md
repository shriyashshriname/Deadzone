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
| **Code & Architecture** | 51+ modular production C# scripts | Authored by DEADZONE Studio Team | Proprietary Studio IP | Written strictly to studio standards |

---

## 2. External Dependencies Audit

- **Third-Party Store Assets:** NONE.
- **Paid Assets / Subscriptions:** NONE.
- **Tracking / Telemetry SDKs:** NONE (All tracking is offline, local JSON in `run_metrics.json`).
- **External Network APIs:** NONE.

All game assets are fully internal, procedural, or provided directly by the official Unity engine.
