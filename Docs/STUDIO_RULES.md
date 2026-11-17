# DEADZONE Studio Rules

## 1. Principle
The repository is the studio's memory. Subagents do NOT inherit conversation history, so anything that matters is written to a file.

## 2. Roster
Active at the start: NOVA (lead), DESIGN, ARCH, GAME, QA.
Brought in when relevant: ENEMY (Gate 2+), UX (Gate 2+), ART (Gate 1+), BIZ (design from Gate 3, SDKs from Gate 5), SHIP (Gate 0 build, Gate 6+ release).
Agent files live in .agents/agents/. Max 3 agents working at once.

## 3. Task contract (NOVA writes it)
ID | Owner | Objective | Files to read | Allowed write scope | Depends on | Acceptance (2-4 lines) | Tests | Deliverables
Each task must fit one agent context. If it is bigger, split it.

## 4. Report format (every agent returns this)
TASK / OWNER / STATUS / SUMMARY / FILES CHANGED / TESTS RUN / RESULTS / RISKS / DEPENDENCIES / NEXT RECOMMENDATION

## 5. Communication
- Use Antigravity features if they work on this account: /agents to see status, messages between agents, /swarm shared board. Test them in Gate 0; do not assume.
- Fallback that always works: reports and messages as files in Docs/Team/Inbox/<TO>_<ID>.md. NOVA routes them.
- Any interface change is announced to affected agents.
- Files: Docs/Team/BOARD.md (tasks), STATUS.md (under 40 lines), HUMAN_TODO.md (founder-only), BACKLOG.md (out-of-gate ideas), LOOP_LOG.md (failures). HALT.md existing means: stop after the current task.

## 6. Loop breaker (protects the quota)
- Same error twice in a row: STOP. Do not try a third identical fix.
- Write to Docs/Team/LOOP_LOG.md: task, error, what was tried. Tell NOVA.
- NOVA splits the task into smaller pieces, simplifies the interface, or replaces the logic with a plain state table.
- Three failed approaches on one task: mark it BLOCKED, move on.

## 7. Size and quota discipline
- Scripts about 250 lines max (split, do not grow).
- Never read the whole repo. Read your role file, the task, and the files in scope.
- Update STATUS.md before long tasks so work can resume after a quota stop.

## 8. Branches
Main always builds. One branch per task (feature/..., fix/...). One owner per subsystem. Parallel code agents use isolated worktrees when supported. Review diffs before merge. Tag milestone builds. Never force-push main.

## 9. Definition of done
Acceptance lines pass, compiles, relevant tests pass, manual playtest when relevant, no unrelated files changed, docs updated, reviewer (QA/ARCH) approves, evidence attached.

## 10. Guardrails (never without the founder)
Spending money, paid accounts, payment details, accepting legal terms, publishing or submitting a build, secrets or keys in the repo or chat, deleting the repo or rewriting history, changing a CONFIRMED decision, adding an SDK that collects data. To challenge a confirmed decision: add a FOUNDER-REVIEW entry to DECISION_LOG.md with evidence and keep working with the current decision.

## 11. Honest limits
Agents run only when invoked, then go idle. They cannot play the game for you, pay, verify identity, or judge fun. Human playtests decide the gates.
