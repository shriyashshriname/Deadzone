# DEADZONE — Next Steps & Roadmap (Claude review, 2026-11-20)

Companion to `ROADMAP_GATES.md` (the gates stay the source of truth). This file is the
ordered plan for getting from today's state to a real Gate 4 vertical slice.

## 1. Where the project actually is

| Area | State | Evidence |
|---|---|---|
| Engine | Unity 6000.6.9f1, URP 17.6, Input System, AI Navigation | `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json` |
| Code | ~9,000 lines C#, 60+ scripts, clean namespaces, ≤250-line rule mostly held | `Assets/_Project/Scripts/**` |
| Systems built | Noise/swarm AI, 4 weapons, 4 enemy types + 2 bosses, waves, DBNO/revive, extraction, vault, substations, airdrops, perks, upgrade terminal, AI squadmate + rival bots, mobile twin-stick, save v2 + migration, juice (hit-stop, shake), URP post (bloom/vignette/fog) | `Docs/Team/BOARD.md`, git log |
| Tests | 11 real-behavior tests pass; the separate 14-check suite is honestly 6 real / 8 smoke | `playtest_honesty.log` |
| Builds | Windows x64 (99.9 MB) and Android APK (58.4 MB) build headless | `Builds/` |
| Art | **Programmer art.** 6 OBJ models built from axis-aligned boxes (operator = 252 tris, no rig, no textures, no animation). One graybox scene | `Art/Models/**`, `scripts/generate_3d_assets.py` |
| Audio | Procedural synth only | D-021 |
| Validation | **Zero human playtests.** Phone smoke test and 5-player Gate 4 test both still open | `Docs/Team/HUMAN_TODO.md` |

## 2. Problems to fix first

1. **Scope drift past the gates (risk R-01).** BOARD says "Gate 5 in progress" while every Gate 2–4
   checkbox needing a human is unchecked. Rival bots, AI squadmate, airdrops, perks, turrets, daily
   rewards and a 4th weapon were added beyond the agreed first slice. Freeze features until a human
   has played.
2. **The game has never been played by a person.** Automated tests prove systems run, not that
   Noise is fun. This is the single most important gap.
3. **Characters can't be animated.** OBJ files carry no skeleton. Every character needs to move to
   a rigged FBX/GLB pipeline before any animation work.
4. **Engine is on a staging Tech Stream build** (D-023). Fine for prototyping; pick an LTS before Gate 6.
5. **Housekeeping:** 8 uncommitted Unity API-updater edits (`FindFirstObjectByType` → `FindAnyObjectByType`);
   multi-MB logs in the project root (gitignored, but clutter); a `[STASH] JSON parse error` line in the
   honesty log worth checking against a real save file.

## 3. Roadmap

### Phase A — Stabilise & play (now, ~1 week)
- [x] Commit the API-updater changes (2026-11-20). Still to do: move root `*.log` into `Logs/`.
- [ ] Founder plays 3 full runs on the phone (HUMAN_TODO §2). Write notes in `Docs/QA/`.
- [ ] Decide what to **cut** for the slice: keep the original slice (1 map, 1 character, AR/shotgun/SMG,
      Crawler/Brute/Spitter, Crawler King, extraction). Park squadmate, rivals, turrets, daily rewards behind flags.
- [ ] Tune Noise until shots visibly pull enemies (Gate 2 box).

### Phase B — Art foundation (parallel with A, ~1–2 weeks)
- [x] Art bible approved; `Scripts/Core/Palette.cs` added (2026-11-20). Follow-up: migrate 24 scripts with literal colours.
- [x] Blender 5.3 installed and verified headless (2026-11-20).
- [x] Character pipeline v1 (2026-11-20): Kenney CC0 rig → graded skins → Humanoid avatar → idle/run blend tree,
      on player, squadmate, rival, walker, runner, spitter, tank, brute. **Still missing clips:** aim-run, shoot,
      reload, downed, death, attack (source from Mixamo/Quaternius, retarget via Humanoid). Crawler King still a capsule.
- [x] Enemy variants via skin swaps (same rig). Next: shamble/attack clips so infected don't use the human run.
- [ ] Environment kit: modular walls, floor tiles, doors, crates, barrels, barriers, lights (≤ contract budgets).

### Phase C — Visual pass (~2 weeks)
- [ ] Lighting: one key directional + baked ambient + emissive emergency strobes; palette-tinted fog.
- [ ] Rebuild the research-facility map from the kit (replace graybox).
- [ ] VFX: muzzle flash, tracers, blood/acid hits, shell casings, dust — pooled.
- [ ] Readability check from the 55°/14 m camera: player, enemies, pickups distinct at a glance.
- [ ] Mobile perf budget: ≥30 fps on the founder's phone with 35 enemies.

### Phase D — Gate 4 close (~1–2 weeks)
- [ ] Audio pass (CC0 packs replacing synth where it matters: guns, hits, zombie barks).
- [ ] Tutorial + HUD polish; TMP fonts styled to the bible.
- [ ] 5 outside players; ≥3 say they'd play another run. **Only then** move to Gate 5.

### Later (unchanged from ROADMAP_GATES)
Gate 5 content alpha → Gate 6 beta (LTS migration here) → Gate 7 RC → Gate 8 launch → co-op.

## 4. Art bible (draft — founder to approve)

Derived from the founder's existing choices in `ASSET_CONTRACT_GATE1.md` and `CLAUDE_REVIEW_NOTES.md`.

1. **Style:** Grounded low-poly outbreak — chunky, readable silhouettes, flat/lightly-textured
   materials, desaturated industrial world broken by saturated gameplay cues.
2. **Palette:**
   | Role | Hex | Name |
   |---|---|---|
   | Floor | `#3B3D3A` | stained concrete |
   | Walls / structure | `#55595A` | gunmetal |
   | Operator primary | `#4F5A3C` | olive drab |
   | Operator secondary | `#B8A47A` | coyote tan |
   | Infected skin | `#8A9479` | sickly grey-green |
   | Props | `#C9A227` | hazard yellow |
   | Shadow | `#1D2228` | cold blue-black |
   | Accent — health | `#2ECC71` | emerald |
   | Accent — ammo | `#F5A623` | amber |
   | Accent — objective | `#22D3EE` | cyan |
   | Accent — danger/acid | `#D7263D` | crimson |
3. **Mood:** night-time facility on emergency power — cool moonlight key, warm sodium lamps
   pooling on floors, red strobes near objectives, light fog swallowing the far edges.

## 5. 3D character plan

**Budgets (from the asset contract):** operator ≤2,500 tris, infected ≤2,000, bosses ≤4,000; one
1024² texture/atlas; pivot at feet; 1 unit = 1 m; +Y up, +Z forward.

**Rig standard:** Unity Humanoid-compatible skeleton (hips → spine → chest → neck → head, arms,
legs) so every animation retargets to every character.

**Recommended route (zero cost):**
1. **Base:** CC0 rigged low-poly humans/zombies (e.g. Quaternius, Kenney) — already rigged and
   animated, licence-clean. Log each in `ASSET_LICENSES.md`.
2. **Make them DEADZONE:** recolour to the palette, add kit pieces in Blender (plate carrier,
   helmet, backpack, gas mask; torn clothing and growths for infected). Blender Python scripts
   make this repeatable.
3. **Animations:** pack animations + Mixamo (free, needs Adobe account) for gaps; retarget via Humanoid.
4. **Unity hookup:** Animator with a speed/aim blend tree driven by `PlayerController` velocity;
   upper-body aim layer for shooting; replace the box prefabs.

**Alternative:** fully custom modelling in Blender (more unique, much slower; better left to the
hero character after the slice proves fun).

## 6. Character pipeline — how to rerun

1. `python scripts/grade_skins.py` — regrade skins from `Art_Source/CC0/` (edit `SKINS` to change who wears what).
2. Unity menu **Deadzone → Art → Build Rigged Characters** (or headless:
   `Unity.exe -batchmode -quit -projectPath . -executeMethod Deadzone.Editor.CharacterArtBuilder.BuildAll`).
   Idempotent; rerun it after **Deadzone → Setup Wave Horde Prefabs and Scene**, which recreates capsule prefabs.
3. **Deadzone → Art → Capture Character Lineup / Capture Gameplay Scene** writes PNGs to `Captures/` for review.

Runtime pieces: `CharacterAnimDriver` (speed → Animator, works with any mover), `Grounding` (spawned
transform-driven enemies sit on the floor), `HitFlash` now also flashes emission so textured characters visibly flash.

## 7. Environment kit v1 (2026-11-20)

- `scripts/blender_env_kit.py` generates 30 low-poly props in the palette: 3 grass clumps, tall weeds, dead bush,
  2 rocks, leaning + snapped power poles, bent street lamp, 2 rebar chunks ("broken bars"), rubble, chain-link +
  broken fence, sandbags, tyre stack, car wreck, drums (standing/toppled), pallet, cone, bent sign, barbed wire,
  barrier (broken), debris planks, dirt/overgrowth ground patches.
- **Deadzone → Art → Build Environment** (`EnvironmentBuilder.BuildAll`): shared palette materials (batched),
  prefabs with colliders, seeded scatter of 30 themed clusters (overgrowth, wreck, roadside, construction,
  checkpoint, powerline) + perimeter dressing + ~950 grass + 34 ground patches, 5 sodium lamp lights,
  linear fog, graybox materials recoloured to the bible. Idempotent (rebuilds the `Environment` root).
- **Bug fixed:** the NavMesh was baked but never saved, so phone builds had no navmesh (zombies fell back to
  simple steering). Now saved to `Scenes/Graybox_Sandbox_NavMesh.asset` on every environment build.
- Movement: `CharacterAnimDriver` now twists the body toward strafe direction, plays the run backwards when
  backpedalling, and matches playback to the run cycle's measured ground speed (per character, at build time).

## 8. Founder direction 2026-11-20 and M1 progress

Decisions: grounded-stylised look; Solo/Duo/Squad with AI teammates first (online = M5); milestones
M1 combat+loot+helipad → M2 lobby/profile/inventory/modes → M3 sci-fi Zone 2 → M4 realism/terrain → M5 online;
enemies = 1–2 zombie types + Quaternius robots + dark monsters (no cartoon monsters).

**M1 done (Build #4):**
- Roster: Walker + Brute zombies (UAL2 shamble/scratch/knockback); Runner → Demon, Spitter → Trilobite robot,
  Crawler King → Yeti (each: idle/walk/run/attack/hit/death). Corpses fall, linger, sink. Monsters decimated to budget.
- Horde: walkers ×1.8, spawning 30% faster, lower HP (walker 40, runner 35, spitter 40, tank 140); shamblers slowed to 1.7 m/s.
- Weapons: 5 futuristic guns in `Resources/Weapons` (Scout Carbine C, Pulse Pistol R, Plasma Rifle E, Ion Revolver E,
  Rail Sniper L) with coloured tracers; classic guns now have models; guns visible in the player's hand.
- Gun drops: 7% from normal enemies, 30% from sci-fi enemies, 100% from bosses (rarity-biased); glowing pickups;
  full loadout swaps the held gun and drops the old one. Tests 12–13 cover the pool and the swap.
- Helipad + dropship: insertion at run start; evac fly-in + landing when extraction unlocks; boarding + take-off
  before the debrief.

**M1 backpack (Build #5):** `Backpack` on the player — 4 slots, permanent upgrades to 6/8/10 in the field shop
(150/300/500 cr, saved in the stash). Medkits (x3/slot) and ammo packs (x2) are carried and used from the HUD bar
(tap) or H/J; pickups auto-use only when badly hurt / low on ammo. Salvage (Tech Scrap 25 cr from robots, Bio Sample
15 cr from infected, piles from bosses) is cashed in on extraction and lost on death. Test 14 covers it.

**M1 remaining (small):** per-weapon ammo reserves; pickup prompt UI.
