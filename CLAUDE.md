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
  EnvironmentBuilder ends by calling `MoodBuilder` (night-rain look); `MoodBuilder.Apply` can also run alone.
- Look target = founder's reference images in `Assets/_Project/Art/image refernce/` (git-ignored, third-party).
  Runtime mood (fog, ambient, moon, grading, rain, lightning, player fill light) lives in `Core/AtmosphereController`
  + `Core/WeatherRain`, NOT in the scene — GameManager spawns it and it overrides scene RenderSettings.
- Renderer must keep `postProcessData` assigned (it was null = no post at all until Build #7) and Forward+
  (the floor is one 120 m mesh; plain Forward's per-object light limit hid most lamp pools).
- PlaySim facing check uses the 3-sample sustained angle; raw single-frame spikes to ~180° are normal turns.
- NEVER edit project files while a batchmode Unity run is going: it recompiles mid-play, statics reset
  (WaveManager/PlayerRevive Instances = null) and the sim "stalls" for no game reason.
- Portrait is the shipping orientation (Build #8+, locked in BuildScript). HUD panels use `UI/UIScale`
  (480-unit short side, safe area); raw-pixel OnGUI (touch sticks, world labels) resets GUI.matrix itself.
  Touch layout: `MobileControlsManager.RecalculatePortraitLayout`. Camera: TopDownCamera portrait framing.
- Weapon pickup is manual (PICK UP / E); sniper = hold to aim (PrecisionAimLine), release to fire.
  PlaySim runs a scripted mechanics test for both. Scripted InputCommand must set selectSlot = -1
  (0 means "select slot 1" every frame).
- Check the real screen (IMGUI HUD isn't in camera renders): `BuildScript.BuildWindowsCapture`, then
  `Builds/WindowsCapture/Deadzone.exe -screen-width 540 -screen-height 1200 -screen-fullscreen 0 -dzshots <dir>`.
