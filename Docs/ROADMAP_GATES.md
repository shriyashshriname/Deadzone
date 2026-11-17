# DEADZONE Roadmap and Gates (no deadlines; move on only when every box is true)

## Gate 0 — Setup and agent test
- [ ] Git repo created, Unity 6 version pinned, Android modules installed
- [ ] .agents/agents/ discovered (/agents lists them)
- [ ] TEST A: a subagent can read a file, create a file and edit a file. If not, add a tools list from the Antigravity docs (exact names; a wrong name can hang the agent)
- [ ] TEST B: with commandExecutionPolicy sandbox, Unity headless compile and EditMode tests run. If blocked, change the policy for GAME/QA/SHIP only
- [ ] TEST C: check which of /swarm and /teamwork this account has; otherwise use the Inbox fallback
- [ ] Baseline scene opens, Android development build installs on the founder's phone
- [ ] Headless commands written in Docs/Technical/RUNBOOK.md

## Gate 1 — Discovery
- [ ] Core fantasy, audience hypothesis, core loop diagram, first mission outline
- [ ] Noise rules table (actions, radius, decay, thresholds)
- [ ] Art bible and reference board; asset license register started
- [ ] Scope boundaries approved by the founder

## Gate 2 — Graybox combat (the Fun Gate begins)
- [ ] Movement, camera, both shooting modes, weapon switching, reload, health/damage
- [ ] Three placeholder weapons, one enemy, Noise system working (shots visibly attract enemies)
- [ ] Runs on the founder's phone at 30 fps or better
- [ ] Founder playtest notes written

## Gate 3 — Core gameplay
- [ ] Three enemy types, spawn/encounter system, pickups, inventory
- [ ] Crawler King boss, vault loot window, extraction, run summary
- [ ] Local save (versioned), difficulty tuning
- [ ] A full 6-8 minute run completes without a crash

## Gate 4 — Vertical slice (the Fun Gate ends)
- [ ] One polished environment, one character, three polished weapons, boss phases, HUD, audio, tutorial
- [ ] Device performance test
- [ ] 5 outside players tried it; at least 3 would play another run (founder collects)
- [ ] Noise visibly changes how people play (QA playtest notes)

## Gate 5 — Content alpha
- [ ] Three zones, three bosses, 2+ characters, rarity tiers 1-3, progression, cosmetic inventory
- [ ] Settings and accessibility, analytics plan, save migration plan
- [ ] Economy sheet shows no pay-to-win path; ad/IAP SDK chosen and audited (BIZ + ARCH)

## Gate 6 — Beta
- [ ] Device matrix, crash and stability tests (target 99% crash-free sessions), profiling
- [ ] Privacy policy, data inventory, SDK audit, store assets
- [ ] Founder pays the Play fee only now (approval) and recruits 12 testers for the 14-day closed test
- [ ] Mac access plan for iOS written (borrowed Mac, lab, or paid)

## Gate 7 — Release candidate
- [ ] Final Android build (signed AAB), content rating, privacy and ads declarations, IAP and ads test flows
- [ ] iOS build path validated on a Mac with Xcode
- [ ] No P0/P1 defects; founder approval

## Gate 8 — Launch
- [ ] Closed test completed, production access granted, Android released (India first, staged)
- [ ] iOS submitted when the account and Mac are available
- [ ] Monitoring, review replies and first patch plan active
- [ ] Targets (not promises): D1 retention 35%+, D7 10%+

## Later (after a successful single-player launch)
Co-op (4 players), then PvPvE. Server-authoritative design, costs covered by revenue only.
