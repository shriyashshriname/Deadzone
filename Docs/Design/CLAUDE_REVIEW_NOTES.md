# DEADZONE — Claude Outside Review Design Directives
**Origin:** Outside Architecture & Game Feel Review (Claude)  
**Status:** Saved for Post-Gate 4 Implementation

---

## 1. Pacing & Level Rhythms
- **Target Run Length:** 8 to 10 minutes total per extraction deployment.
- **Rhythm Structure:** 
  1. *Calm Scavenging:* Teaches player acoustic noise consequences with sparse walker zombies.
  2. *Optional Risk Event:* Golden Supply Airdrop (high risk channel, greedy reward).
  3. *Objective Escalation:* Power Substations Alpha & Beta defense under screaming siren alarms.
  4. *Climax:* Sector Boss (Crawler King / Brute) and Security Vault breach.
  5. *Peak Final Defense:* Helipad extraction zone hold under heavy horde assault.
- **Anti-Fatigue Rule:** Never place two high-intensity peaks back-to-back without a breathing room intermission.
- **Acoustic Driver:** Let the dynamic Noise Level drive spawn intensity rather than rigid stopwatch countdowns.
- **Spatial Geometry Rule:** At 7 m/s movement speed, a 120m arena is traversable in ~17 seconds. Sightlines, barricades, spawn pacing, and empty negative space matter far more than raw physical square meterage.

---

## 2. Visual Atmosphere (Grounded Industrial, Not Sci-Fi)
- **Palette Discipline:** Dim, desaturated environment surfaces contrasted against punchy, saturated gameplay cues (emerald medkits, amber ammo, cyan objective beacons, crimson blood/acid).
- **Free Fidelity Techniques:**
  - Depth fog, subtle vignette, and high-contrast color grading (via Post-Processing / URP Volume when active).
  - Baked ambient lighting supplemented by a handful of real-time key lights with emissive textures.
  - Flickering emergency warning strobes and floor decals for intuitive pathfinding.
  - Pooled dust, steam, and embers particles (zero per-frame allocations).
  - High-quality CC0 modular props (Kenney, Quaternius) and ProBuilder grayboxing.
  - Mandatory audit of every imported 3D mesh and texture in `Docs/ASSET_LICENSES.md`.

---

## 3. Metagame & Retention Design (Fair Free-to-Play)
- **Priority Sequence:**
  1. *Daily Bounties & Contracts:* Simple daily milestones driving day-to-day retention.
  2. *Weapon Mastery & Operator Cosmetics:* Rewarding player skill and aesthetic personalization.
  3. *Gunsmith Workbench:* Side-grade attachments (e.g. extended mag with slower ads, suppressor with reduced range) — strictly zero raw power creep.
  4. *Black Market Trader:* Introduced only once a healthy Credit sink is established in the economy.
- **Monetization Inviolability:** Never sell stat boosts, health, damage, or pay-to-win advantages for Gems or real currency.

---

## 4. Multiplayer Architecture Road (Post-Gate 4 Co-Op)
- **Authoritative Architecture:** The server/host must own health, damage resolution, and loot drop RNG.
- **Input Seam Decoupling:** Human players, AI companion squadmates (`FriendlySquadmate`), and AI rival bots (`RivalOperator`) must all communicate with simulation entities via the unified `InputCommand` struct.
- **Team Matrix:** `TeamMember.TeamId` must remain strictly server-validated.
- **Zero Premature Networking:** Do not introduce Netcode for GameObjects or Photon Fusion dependencies until single-player vertical slice is validated by outside players.
