# Code and Repository Standards

## General
- Use readable, maintainable C#.
- Prefer composition over deep inheritance.
- Keep scripts focused on one responsibility.
- Avoid unnecessary abstractions.
- Do not add a package without ARCH approval.
- Do not commit generated Library/Temp/Obj folders.
- Do not commit secrets, signing keys or local machine paths.

## Naming
- Namespaces: `Deadzone.<Feature>`
- Classes and public methods: PascalCase
- Private fields: `_camelCase`
- ScriptableObject definitions: clear singular names, e.g. `WeaponDefinition`
- Scenes and assets: consistent prefixes only where useful.

## Data
- Weapon/enemy/item/character definitions should be data-driven.
- Use ScriptableObjects for authoring configuration.
- Keep player save data versioned and serializable.
- Do not store transient scene object references in persistent save data.

## Architecture
- Separate input, simulation, UI and persistence.
- UI reads game state; game state does not depend on UI.
- Centralize damage and health rules.
- Use explicit interfaces for cross-system interaction.
- Avoid circular dependencies.
- Document events and their owners.
- Do not add speculative multiplayer code.

## Testing
- Add EditMode tests for pure calculations and rules.
- Add PlayMode tests for scene/gameplay behavior where useful.
- Every bug fix should include a regression test when practical.
- Test on real Android hardware regularly.
- Keep a build identifier and test notes.

## Git
- Main branch should build.
- Use feature branches.
- Commit focused changes.
- Review diffs before merge.
- Never force-push shared main.
- Tag milestone builds.

## Added in the merged kit
- Scripts stay under about 250 lines. Split instead of growing.
- All control goes through an InputCommand (move vector, aim vector, action flags) so the player, the QA bot and a future network player share one path.
- Rules logic (damage, ammo, cooldowns, noise) is plain C# with no UI dependency, so it can move to a server later.
- Seeded randomness for loot and spawns; stable IDs for items, weapons, characters, missions.
- Object pooling for bullets, enemies and effects. No per-frame garbage in Update.
- Same error twice: stop and log it (see Docs/STUDIO_RULES.md, loop breaker).
