# DEADZONE Risk Register

| ID | Risk | Probability | Impact | Mitigation | Owner |
|---|---|---:|---:|---|---|
| R-01 | Scope expands before core combat is fun | High | Critical | Freeze first-slice scope; NOVA enforces gates | NOVA |
| R-02 | AI agents produce inconsistent architecture | High | High | ARCH reviews contracts; tests; scoped branches | ARCH |
| R-03 | Parallel agents edit same files | Medium | High | One owner per subsystem; worktrees | NOVA |
| R-04 | Controls feel poor on phones | Medium | Critical | Early real-device playtests | UX/GAME |
| R-05 | Performance too heavy for low-end Android | Medium | High | URP budgets; profiling; target device matrix | PERF |
| R-06 | Multiplayer costs and complexity overwhelm project | High | Critical | Defer networking until single-player validates | NOVA |
| R-07 | Asset licenses unclear | Medium | High | Asset register and license review | ART/SEC |
| R-08 | Publishing fees unavailable | High | High | Delay store enrollment until release-ready | SHIP |
| R-09 | No Mac available for iOS signing | High | High | Borrow university/friend Mac; keep iOS compatibility | SHIP |
| R-10 | Ads harm retention | Medium | High | Optional rewarded ads only; test with users | BIZ |
| R-11 | IAP policy violations | Low/Medium | High | Store IAP compliance review | SHIP/SEC |
| R-12 | Jio/Antigravity quota or feature changes | Medium | Medium | Small tasks; use fallback custom agents | NOVA |
| R-13 | Save data corrupts across updates | Medium | High | Versioned save schema and migration tests | ARCH/QA |
| R-14 | No real player validation | Medium | Critical | Human testing at every milestone | Founder/QA |
| R-15 | The Noise hook is never built or tuned, leaving a generic shooter | Medium | Critical | Gate 2 requires visible Noise behavior; Gate 4 tests it with players | DESIGN/ENEMY |
| R-16 | Sandbox policy blocks Unity headless commands | Medium | High | Gate 0 TEST B; relax policy for GAME/QA/SHIP only | NOVA |
| R-17 | Agent tools list empty or misnamed (agent cannot edit files or hangs) | Medium | High | Gate 0 TEST A; copy tool names exactly from docs | NOVA |
| R-18 | Repeated error loops burn the Flash quota | High | Medium | Loop breaker in STUDIO_RULES.md; 250-line cap; max 3 agents | NOVA |
| R-19 | 12-tester closed test is not ready when release is near | Medium | High | Recruit from Gate 4 onward; track in HUMAN_TODO | SHIP/Founder |
