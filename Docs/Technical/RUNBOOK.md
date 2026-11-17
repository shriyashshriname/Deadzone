# DEADZONE Production Runbook

This document defines standard operating procedures, build commands, test runners, logging destinations, and sandbox execution parameters for Project DEADZONE.

---

## 1. Fast Reference Commands

All commands are run from the project root: `E:\gamedev\Project Deadzone\Deadzone_Game`.

### A. Run Headless Playtest & Real Behavior Test Suite
```bat
.\run_tests.bat
```
- **Underlying Command:**
  `"E:\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "E:\gamedev\Project Deadzone\Deadzone_Game" -executeMethod Deadzone.Editor.AutonomousPlaytestRunner.RunHeadlessSimulation -logFile "E:\gamedev\Project Deadzone\Deadzone_Game\playtest_honesty.log"`
- **Log Destination:** `playtest_honesty.log`
- **What It Runs:**
  - Automated Gate 0–3 smoke test suite (14 checks)
  - Real Behavior Test Suite (10 genuine behavior tests including noise alert, friendly-fire immunity, DBNO revive/bleedout, power grid extraction locks, v1-to-v2 stash migration with backup, real physics raycast AI probe for 35 active enemies, hybrid 2-slot loadout, muzzle flash light pooling, HitFlash URP `_BaseColor` test, and obstacle circumnavigation around a 4m container).

### B. Build Android Development APK
```bat
.\build_android.bat
```
- **Underlying Command:**
  `"E:\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "E:\gamedev\Project Deadzone\Deadzone_Game" -executeMethod Deadzone.Editor.BuildScript.BuildAndroid -logFile "E:\gamedev\Project Deadzone\Deadzone_Game\build_android.log"`
- **Log Destination:** `build_android.log`
- **Output Target:** `Builds/Android/Deadzone.apk`

### C. Build Windows Standalone (x64)
```bat
"E:\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "E:\gamedev\Project Deadzone\Deadzone_Game" -executeMethod Deadzone.Editor.BuildScript.BuildWindows -logFile "E:\gamedev\Project Deadzone\Deadzone_Game\build_windows.log"
```
- **Log Destination:** `build_windows.log`
- **Output Target:** `Builds/Windows/Deadzone.exe`

---

## 2. Log Destinations & Diagnostics

| Log File | Purpose | Location |
|---|---|---|
| `playtest_honesty.log` | Headless playtest results, real behavior test outputs, pipeline metrics, and assertion passes/failures | Project root |
| `build_android.log` | Android script compilation, asset packaging, Gradle build details | Project root |
| `build_windows.log` | Windows Standalone build logs and linking details | Project root |
| `Editor.log` | Unity internal editor diagnostic log | `AppData/Local/Unity/Editor/Editor.log` |
| `run_metrics.json` | Telemetry output generated per run (kill count, damage taken, peak noise, extraction status) | Project root |

---

## 3. Sandbox Bypass Security Protocol

### Why Unity Requires `BypassSandbox: true`
In agentic AI environments, terminal commands run inside an isolated OS container by default. However, Unity Editor batchmode operations require:
1. **Child Process Spawning:** Unity orchestrates separate background worker processes (`VBCSCompiler.exe`, `dotnet.exe`, `Tundra.exe`, `UnityShaderCompiler.exe`, `Unity.Licensing.Client.exe`).
2. **Local Named Pipes & IPC:** Unity communicates with its licensing daemon and package manager via local IPC streams (`Upm-*`, `LicenseClient-*`).
3. **Hardware & Driver Access:** Graphics initialization and Direct3D/Vulkan runtime probing.
4. **Drive Access:** Reading and writing assets, libraries, and temporary caches on secondary storage drives (`E:\`).

Under strict sandbox isolation, child process creation and named pipe connections are blocked, causing batchmode to immediately abort with compiler or worker initialization failures.

### Security Guardrail
- **Restricted Execution:** Unsandboxed execution (`BypassSandbox: true`) is strictly restricted to:
  1. The named repository batch scripts (`run_tests.bat`, `build_android.bat`)
  2. The local Git CLI commands (`git status`, `git add`, `git commit`, `git checkout`, `git merge`)
- **Prohibition:** Under no circumstances should arbitrary, untrusted, or third-party scripts, remote executables, or curl-to-shell payloads ever be run with sandbox bypass.
- **Skills Deferral:** Agent skill automation (Part E) remains deferred until post-phone testing.
