# HUMAN_TODO (Founder-Only Action Items)

## 1. Android Module & Development APK Build
- [ ] **Install Android Build Support in Unity Hub:**
  1. Open Unity Hub on this PC.
  2. Navigate to **Installs** -> Locate `Unity 6000.6.9f1`.
  3. Click the settings gear icon -> Select **Add modules**.
  4. Ensure **Android Build Support** is checked (specifically the Playback Engine / Editor extensions; NDK/SDK/OpenJDK are already present).
  5. Wait for Unity Hub to complete installation.
- [ ] **Build Development APK:**
  - Execute `run_command` in batchmode or open Unity Editor and click menu item: `Deadzone -> Build -> Android APK`.
  - Target output: `Builds/Android/Deadzone.apk`.

## 2. Founder Device Smoke Test (10 Minutes)
- [ ] **Sideload to Test Device:**
  - Connect Android phone via USB with USB Debugging enabled.
  - Sideload via command: `adb install -r "E:\gamedev\Project Deadzone\Deadzone_Game\Builds\Android\Deadzone.apk"`
- [ ] **Verify Core Mobile Experience:**
  - Play 1 full deployment (8-10 minutes).
  - Note frame rate feel (smooth 30-60 fps vs stutter during large swarm spawns).
  - Test touch Mode A (push-to-fire on right stick) vs Mode B (aim stick + fire button).
  - Test context interaction button hold (DBNO self-stabilize, substation reboot, airdrop claim, extraction).
  - Note device heat / thermal throttling and any unexpected crashes.

## 3. Gate 4 External Validation (5 Outside Players)
- [ ] **Recruit 5 Independent Outside Players:**
  - Provide them with either the Standalone Windows build (`Builds/Windows/Deadzone.exe`) or the sideloaded Android APK.
  - Have each player run an unguided 8-10 minute session.
  - Complete the rubric in `Docs/QA/PLAYTEST_SHEET.md`.
  - **Gate 4 Threshold:** At least 3 out of 5 players must answer **YES** to "Would you play another run of DEADZONE tomorrow?".

## 4. Install Blender (for 3D Asset Generation Pipeline)
- [ ] **Install Blender:**
  1. Download the latest stable official Blender release (free & open source) from https://www.blender.org/download/ (or run `winget install BlenderFoundation.Blender` in Windows PowerShell).
  2. Run the installer and finish standard setup (default path: `C:\Program Files\Blender Foundation\Blender 4.x\blender.exe`).
  3. Ensure `blender.exe` is added to your Windows user/system `PATH` so headless execution (`blender --background ...`) works from the terminal.
  4. Once installed, ART will run the automated headless audit test for modular crate/barrel prop generation using `blender-3d-asset-generation`.
