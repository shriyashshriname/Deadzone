# DEADZONE — notes for Claude

Top-down twin-stick outbreak shooter, Unity 6000.6.9f1 + URP, Android first. Solo founder, $0 budget, CC0 assets only.
Plan and decisions: `Docs/ROADMAP_NEXT.md` (milestones M1→M5), `Docs/DECISION_LOG.md`, art bible in ROADMAP §4.

## Founder's standing rules
- End every reply about the game with the build number (e.g. **Build #6 · v0.4.6**); every build bumps it automatically.
- Never remove features or mechanics to save space; APK size is not a constraint the founder wants traded for features.
- Polish movement, characters and enemies before adding breadth. Small details matter.
- Grounded-stylised look; robots + dark monsters + 1–2 zombie types; Duo/Squad = AI teammates until M5.

## Verify before every build (no exceptions)
1. Unit/behaviour suite: `Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod Deadzone.Editor.AutonomousPlaytestRunner.RunHeadlessSimulation -logFile Logs/playtest.log`
2. **Play simulation** (real game in play mode, auto-player, revive test, stall/facing/IK checks, in-play screenshots):
   `DZ_PLAYSIM=1 DZ_PLAYSIM_WAVES=6 Unity.exe -batchmode -projectPath . -executeMethod Deadzone.Editor.PlaySim.Run -logFile Logs/playsim.log`
   → read `Logs/playsim_report.txt` and LOOK at `Captures/play_*.png`. Edit-mode captures are not enough:
   humanoid facing, IK and animation bugs only show in play mode.
3. Build: `-executeMethod Deadzone.Editor.BuildScript.BuildAndroid` → `Builds/Android/Deadzone.apk`.

## Hard-won lessons
- AI movement goes through `Core/EnemyMotor` (`Step` + `SteerDir` path steering). Never `transform.position +=` for characters.
- Navmesh: only the floor is walkable (props are Not Walkable) and the asset must be saved (`EnvironmentBuilder`).
- Humanoid characters: ModelYaw 0 (retargeting orients them); UAL2 clips need `rotationOffset = 180`.
- Unity fake-null: never use `??` on components or `GetBoneTransform`.
- Builders are idempotent; rerun order after big changes: Environment → AI → CharacterArt → Weapon → Creature.
