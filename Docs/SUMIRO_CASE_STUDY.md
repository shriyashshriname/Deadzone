# Sumiro Case Study
## What the developer did, why the workflow worked, and what DEADZONE should adopt

**Research date:** 16 November 2026  
**Subject:** Sumiro, a browser-based 3D adventure game by Shikhar (@xikhar)  
**Research confidence:** Medium for workflow description; low for independently verifying the 24-hour claim or commercial outcomes.

---

# 1. Executive summary

Sumiro is a 3D ink-wash samurai adventure that runs in a web browser. Its creator said the game was made in approximately 24 hours using AI-assisted tools. Public breakdowns describe a workflow involving:

- Claude Code / Claude Opus 5.5 for coding and agent coordination.
- Flux 3 Max for a visual reference video.
- GPT Image for character and level concept images.
- Tripo for converting character reference views into a 3D model.
- Blender for asset reconstruction and scene preparation.
- Three.js as the browser runtime.

The creator reportedly used several coding agents in parallel to build and check parts of the world, manually tested a playable prototype, then repeated a build/play/fix loop.

The most important lesson is NOT "AI can make a complete commercial game in 24 hours."

The lesson is:

**A human creative director defines the target, specialist AI tools produce bounded outputs, agents work in parallel where tasks are separable, and the human repeatedly tests the actual game.**

The 24-hour figure is a creator-reported claim and has not been independently audited. Sumiro was not a commercial mobile multiplayer shooter. Its browser technology, art pipeline and production constraints differ substantially from DEADZONE.

---

# 2. What Sumiro is

Reported public details:
- Developer: Shikhar, known as @xikhar.
- Release: 4 November 2026.
- Genre: 3D adventure / platformer.
- Platform: Browser.
- Runtime: Three.js.
- Visual identity: Ink-wash / sumi-e inspired Japanese mountain and temple environments.
- Reported playtime: approximately 30–40 minutes.
- Reported build time: 24 hours (creator claim).
- Launch visibility: public reporting recorded millions of views; views are not equivalent to installs, active users or revenue.

Sumiro's gameplay features a traveler moving across cliffs, stairs, temple grounds and platforms, climbing ledges and using lifts across chapters.

---

# 3. Tools used and how they were used

| Tool | Reported role | DEADZONE adaptation |
|---|---|---|
| Claude Code / Opus 5.5 | Main coding and parallel agent workflows | Antigravity + Gemini 3.8 Flash agents, within current free/partner quota |
| Flux 3 Max | Generated the initial reference video | Free/available reference boards, Blender blockouts, or existing entitled image/video tools; no paid dependency |
| GPT Image | Character views and level concept art | Use available free image tools only when available; otherwise draw/assemble references in Krita/GIMP |
| Tripo | Turned character views into a 3D model | Blender manual blockout/modeling; optional free tools only after license/limits are checked |
| Blender | Rebuilt and prepared 3D assets | Use Blender as the main free 3D asset tool |
| Three.js | Browser runtime | Do NOT copy. DEADZONE uses Unity 6 URP for Android/iOS |

The pipeline was a chain: reference → assets → scene → playable prototype → level concepts → playable levels → manual test → fixes.

---

# 4. Sumiro's reported production workflow

## Step 1 — Make a reference video before coding

The creator reportedly generated a short visual reference video and treated it as the art bible.

The video established:
- World appearance.
- Character design.
- Palette.
- Camera.
- Atmosphere.
- Environmental language.

Why it matters:
- Prevents visual drift.
- Gives artists and agents a shared target.
- Reduces ambiguous descriptions.
- Makes it easier to judge whether generated assets belong in the same game.

DEADZONE application:
- Create a visual target board and a short camera/gameplay reference.
- Lock top-down/isometric readability, grounded outbreak setting, lighting, palette and silhouette rules.
- Use graybox scenes before producing final art.
- Do not generate many unrelated art styles.

## Step 2 — Break the reference into scenes and assets

The creator reportedly captured frames from the reference and rebuilt environment elements in Blender before placing them into Three.js.

Why it matters:
- A scene becomes a list of manageable assets.
- Environment work can be divided among agents.
- Reusable modular assets reduce repeated work.

DEADZONE application:
- Break the first facility into modular pieces: floor tiles, walls, doors, hallways, pillars, crates, research equipment, lights and props.
- Define dimensions and pivots.
- Use a consistent material and texture budget.
- Assign agents separate asset families, not overlapping scene ownership.

## Step 3 — Build the character from multiple views

The reported pipeline used four character views, converted them to a 3D model through Tripo, then added physics and animation.

Why it matters:
- A front-only concept image is not enough for a usable 3D character.
- Consistent multi-view references reduce model ambiguity.
- Character silhouette and proportions must remain stable.

DEADZONE application:
- Define front, side, back and top/three-quarter references.
- Start with a placeholder capsule or low-poly character.
- Validate scale and movement before investing in detailed models.
- Rigging, animation and mobile optimization require human review.

## Step 4 — Play the prototype before adding content

This was one of the most important steps.

The creator reportedly played the first prototype before building additional levels.

Why it matters:
- A generated scene is not automatically a game.
- Controls, collision, camera, navigation and interaction must work.
- Visual quality can hide weak gameplay.

DEADZONE application:
- Build a graybox arena first.
- Implement movement and both selected shooting modes.
- Test aiming, recoil, hit feedback and enemy readability on a real Android device.
- Do not create multiple maps until the first arena is fun.

## Step 5 — Plan each level in the established visual style

The creator reportedly generated concept art for each level and used the existing style as the guide.

Why it matters:
- Level layouts can be planned before detailed asset production.
- A consistent art bible helps multiple agents create related content.

DEADZONE application:
- Use one approved facility art direction.
- Create a top-down level layout and gameplay flow before decoration.
- Plan enemy entrances, cover, choke points, pickup placement and boss arena.
- Validate line-of-sight and movement with graybox geometry.

## Step 6 — Build levels and repeat the play/fix loop

The reported agents built playable levels from concept art. The creator played, reported issues and agents fixed them.

Why it matters:
- The human's role is not merely prompt writing.
- Testing reveals problems that generated code cannot reliably predict.
- Feedback must be concrete and reproducible.

DEADZONE application:
- Human plays every milestone build.
- QA records steps, expected result, actual result, severity and device.
- Gameplay agent fixes one bounded issue at a time.
- QA reruns regression tests.
- NOVA accepts or rejects the change based on evidence.

---

# 5. Multi-agent system: what Sumiro did and what we should improve

Public breakdowns describe multiple agents building and checking parts in parallel.

Parallel agents are useful when tasks are separable:
- One agent researches architecture.
- One designs enemies.
- One prepares environment props.
- One builds UI screens.
- One writes tests or reviews changes.

However, parallel agents can also create:
- Conflicting code.
- Incompatible interfaces.
- Duplicate systems.
- Different naming conventions.
- Merge conflicts.
- Inconsistent art direction.
- Unverified assumptions.

DEADZONE's improvement:
1. NOVA owns the task board and interface contracts.
2. Every agent gets one task ID and a defined file scope.
3. Agents use isolated Git branches/worktrees for parallel code changes when possible.
4. Agents report files changed, tests run and unresolved dependencies.
5. QA reviews behavior.
6. ARCH reviews architecture.
7. NOVA integrates.
8. Human founder accepts milestone builds.

Never allow multiple agents to edit the same core gameplay files simultaneously without coordination.

---

# 6. What Sumiro's story does NOT prove

## 6.1 The 24-hour claim

It is a creator-reported time figure, not an independently audited end-to-end production study.

The case analysis notes that the creator had already built previous game prototypes and had learned lessons about:
- Blender-to-runtime asset alignment.
- Parallel agent coordination.
- Cloth physics and clipping.
- Building and debugging interactive scenes.

Therefore, the 24-hour result should not be treated as a realistic first-project schedule.

## 6.2 Views are not commercial validation

Millions of social views do not establish:
- Retention.
- Active players.
- Revenue.
- Store conversion.
- Long-term support costs.
- Commercial viability.

DEADZONE must measure real player behavior and retention, not just social engagement.

## 6.3 AI cannot decide if the game feels good

The case analysis explicitly emphasizes the need for a human to play the prototype.

For DEADZONE, the human must judge:
- Is movement satisfying?
- Can players aim accurately?
- Is the camera readable?
- Are enemies fair?
- Does combat feel responsive?
- Is the game fun after repeated sessions?

## 6.4 Audio is a separate pipeline

The case analysis notes that music was not solved by the main six-tool workflow.

DEADZONE must plan:
- Weapon sounds.
- Enemy sounds.
- Boss cues.
- UI sounds.
- Music.
- Mixing and mobile audio performance.

Use free tools and properly licensed audio only.

---

# 7. What we should copy, adapt and avoid

| Workflow element | Decision | Reason |
|---|---|---|
| Visual reference before production | ADOPT | Locks the art direction |
| Modular scene breakdown | ADOPT | Makes work parallel and reusable |
| Multiple specialized agents | ADOPT | Reduces one-agent bottlenecks |
| Human prototype playtest | MUST ADOPT | Validates actual game feel |
| Repeatable play/fix loop | MUST ADOPT | Prevents unverified content expansion |
| AI-generated 3D assets as final assets | ADAPT | Must pass scale, topology, rig, license and mobile checks |
| Three.js runtime | AVOID | DEADZONE targets Unity mobile |
| 24-hour schedule | AVOID | Not relevant to scope or experience |
| Build many levels before testing | AVOID | High risk of producing unusable content |
| Assume social views mean success | AVOID | Need retention, installs and revenue data |

---

# 8. DEADZONE's adapted production pipeline

```text
PRODUCT BRIEF
    ↓
GAME DESIGN DOCUMENT
    ↓
VISUAL ART BIBLE
    ↓
GRAYBOX LEVEL + PLACEHOLDER CHARACTER
    ↓
PLAYABLE COMBAT PROTOTYPE
    ↓
HUMAN PLAYTEST
    ↓
QA BUG REPORTS
    ↓
AGENT IMPLEMENTATION / FIXES
    ↓
ARCHITECTURE + QA REVIEW
    ↓
INTEGRATED BUILD
    ↓
VERTICAL SLICE
    ↓
CONTENT PRODUCTION
    ↓
BETA + DEVICE TESTING
    ↓
STORE SUBMISSION
    ↓
LAUNCH + LIVE OPERATIONS
```

---

# 9. Case-study conclusion

Sumiro demonstrates a useful AI-assisted workflow: visual direction first, modular asset production, parallel specialist agents, early playable validation and repeated human-directed iteration.

It does not prove that a first-time developer can build a polished commercial multiplayer mobile game in 24 hours.

For DEADZONE, the most valuable adaptation is to create a disciplined AI-assisted studio:
- Human founder as product owner and creative director.
- NOVA as Studio Lead.
- Specialist agents with clear boundaries.
- Git and documentation as shared memory.
- Playable builds as the source of truth.
- QA and architecture reviews before integration.
- Real-device testing before release.

**Our target is not the fastest game. It is a game we can proudly ship, maintain and grow.**

---

# 10. Sources

1. 100mini, Sumiro case study: https://100mini.com/showcase/sumiro
2. AIGamer, Sumiro workflow summary: https://www.aigamer.gg/games/sumiro
3. Sumiro playable build: https://xikhar.github.io/sumiro
4. Antigravity custom subagents documentation: https://antigravity.google/docs/subagents/
5. Antigravity model documentation: https://antigravity.google/docs/models

The 24-hour build time, tool use and production details are attributed to the creator's public posts and third-party summaries; they are not independently audited.
