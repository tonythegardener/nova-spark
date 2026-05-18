# Nova Spark — Claude Code starting prompt

You are working on **Nova Spark**, a Unity 6 LTS 2D game.

**Project root:** `C:\Users\Tony\Documents\workspaces\nova_spark`

The full design is in `docs/nova_spark_design.md` (relative to project root). Read it before starting any work — it is the source of truth for all design decisions, and the roadmap in section 15 defines the phases you'll execute.

## Your task

Take the project from its current state (a freshly created Unity 6 LTS Universal 2D project, just opened successfully with no console errors) through **Phase 0a** and **Phase 0b** as defined in the roadmap. Your goal is a playable single-level prototype that proves the core marble shooter loop feels good. Stop after Phase 0b and let me playtest before any further work.

## Preflight — do this first, before anything else

1. Confirm the working directory is the project root by listing the top level. You should see `Assets/`, `Packages/`, `ProjectSettings/`, and `docs/` (or similar Unity project layout). If not, stop and ask.
2. Read `docs/DESIGN.md` in full.
3. Check `ProjectSettings/ProjectVersion.txt` to confirm Unity 6 LTS (6000.x). If it's a different version, stop and ask.
4. Check `Packages/manifest.json` to confirm URP is present (`com.unity.render-pipelines.universal`) and that `com.unity.burst` resolved correctly. If URP isn't present or Burst is missing/broken, stop and ask before proceeding — the project setup had an earlier package import error that we believe is resolved, but verify.
5. Confirm whether a Git repo exists at the project root (`.git/` directory). If it doesn't, you'll be initializing it in Phase 0a. If it does, note its current state.
6. Report back with a short preflight summary before starting Phase 0a tasks.

## Working principles

1. **Design doc is canon.** If something in the doc is ambiguous, ask before guessing. If you find a contradiction, flag it — don't paper over it.
2. **Stay in scope.** Phase 0a is project scaffolding. Phase 0b is a single hardcoded level proving the core loop. Do not start Phase 1 content (multiple levels, drafted power-ups, towers, node map) under any circumstances. Resist the urge to add "while I'm here."
3. **Data-driven from day one.** Even with only one power-up or one ship type in Phase 0, define it as a ScriptableObject with a stable content ID and load it through the content registry. The moddability architecture in section 12 is non-negotiable.
4. **Conventions in section 14 are mandatory.** `Assets/_Project/` prefix, assembly definitions per module, `NovaSpark.<Module>` namespaces, ScriptableObjects for content, one MonoBehaviour per file unless tightly coupled.
5. **Commit in meaningful chunks.** Use clear commit messages. I want to be able to read git log later and follow what happened.
6. **Stop and ask when it matters.** Especially around: file paths and structure, anything that's hard to undo later, anything that touches the moddability foundation. Don't ask permission for routine work.
7. **Windows-friendly paths.** No spaces in paths you create. Forward or backslashes both fine, but be consistent within a command.

## Phase 0a — Project Skeleton

Goal: clean Unity project that builds, with all architectural patterns in place.

Tasks:
1. **Source control setup (local-only, no remote).**
   - `git init` if not already initialized.
   - `.gitignore` using the GitHub Unity template (https://github.com/github/gitignore/blob/main/Unity.gitignore). Fetch and adapt as needed.
   - Git LFS configured for binary assets (`*.png`, `*.psd`, `*.jpg`, `*.jpeg`, `*.wav`, `*.ogg`, `*.mp3`, `*.fbx`, `*.tga`, `*.blend`). Run `git lfs install` and add a `.gitattributes`. Confirm `git lfs version` works first; if LFS isn't installed, stop and tell me.
   - Force-text serialization enabled in `ProjectSettings/EditorSettings.asset` (set `m_SerializationMode: 2`).
   - Initial commit covering the bare project. Then commit again after the scaffolding work in subsequent steps — group commits logically.
2. **Folder structure** as specified in section 14 of the design doc. Create the full tree under `Assets/_Project/` and `Assets/_Project/Content/`.
3. **Assembly definitions** per module under `Assets/_Project/Code/`. At minimum: `NovaSpark.Core`, `NovaSpark.Game`, `NovaSpark.Powerups`, `NovaSpark.Mods`. Add more if architecturally justified. Set up references between asmdefs as needed (e.g., `NovaSpark.Game` references `NovaSpark.Core` and `NovaSpark.Mods`).
4. **Packages and input.**
   - Add the New Input System package via `Packages/manifest.json`.
   - Set the project's Active Input Handling to "Input System Package (New)" in `ProjectSettings/InputManager.asset` or `ProjectSettings/ProjectSettings.asset` as appropriate. Unity may prompt for an editor restart when this is changed via the editor — since you're editing the file directly, the change takes effect on next editor launch.
   - Create a basic Input Actions asset under `Assets/_Project/Settings/InputActions/` with placeholder actions (Aim, Fire, ActiveAbility1, ActiveAbility2) — bindings can be minimal for now (mouse position for Aim, left mouse for Fire, Q and E for abilities is fine).
5. **Content registry foundation.**
   - `NovaSpark.Mods.ContentRegistry` — central registry keyed by stable content ID strings. Static class or singleton; whichever you justify.
   - `NovaSpark.Mods.IContentDefinition` — base interface for all moddable content with a `ContentId` (string) property and `SchemaVersion` (int) property.
   - `NovaSpark.Powerups.PowerupDefinition` — first concrete ScriptableObject implementing `IContentDefinition`. Fields appropriate for Phase 0 (display name, description, icon Sprite, basic effect parameters placeholder). Use `[CreateAssetMenu]` so I can author instances in the editor.
   - Bootstrap code that scans `Resources` (or uses Addressables — but Resources is simpler for Phase 0; document the choice) for all `PowerupDefinition` SO instances at startup and registers them with `ContentRegistry`. Log registered content IDs to the console at startup for verification.
   - Create one example `PowerupDefinition` instance under `Assets/_Project/Content/Powerups/` (e.g., `LaserSight.asset`) — purely as a schema example, no gameplay wired up yet. Set its ContentId to `powerup.laser_sight`.
6. **Build verification.** The project must build successfully to a Windows standalone executable.
   - Configure build settings (PC Standalone, Windows, x64).
   - Add the empty/bootstrap scene to Build Settings.
   - Produce a build to a `Builds/` directory at the project root (add `Builds/` to `.gitignore`).
   - Confirm the build runs without errors. The window can be empty — we just need a clean build.
7. **Commit and report.**

Phase 0a exit gate: report back to me with a summary of what was done, the folder tree under `Assets/_Project/`, the list of registered content IDs from the bootstrap log, and confirmation that the build succeeded and ran. **Wait for me to give the go-ahead before starting Phase 0b.**

## Phase 0b — Playable Prototype

Goal: one hardcoded level where the core shooter+chain loop is playable end to end and feels good.

Scope (do not exceed):
- One scene with one hardcoded level layout.
- Path defined by a `LevelDefinition` ScriptableObject containing a waypoint list. Path can be a simple curve or zigzag — pick something that lets me feel the shooter without being too easy or too hard. A gentle S-curve is a good default.
- Marble chain that spawns at the path's start, advances along the path at a tunable speed, and renders colored marbles in sequence.
- Shooter (cannon) at a fixed position. Aim by mouse position (read via New Input System). Fire on left-click. Cannon has a current marble and a next marble (queue of 2 visible).
- Shots are physics-driven projectiles that, on hitting any chain marble, insert into the chain at the impact point with the shooter's color.
- After insertion, the chain checks for runs of 3+ same-color marbles touching the insert. Matches are destroyed; remaining chain compacts and continues advancing.
- Cascade detection: if compacting brings same-color groups together, those also resolve. Recurse until stable.
- Win: chain fully cleared. Lose: chain head reaches the end of the path (Bastion).
- Win/lose triggers a simple end-of-level overlay with a Restart button.
- Use placeholder art (Unity primitives, solid colors are fine). Audio optional. A small amount of juice (hit flash, simple particle burst on match, screen shake on combo) is encouraged but don't sink time into it.
- 4 marble colors is the right starting count.

Tunable parameters live on ScriptableObjects or serialized fields, not magic numbers in code:
- Chain advance speed.
- Shot velocity.
- Spawn rate / chain length.
- Match count threshold (default 3).

Out of scope (do not implement, even partially):
- Multiple levels, level selection, or any node map.
- Power-ups affecting gameplay (the registry exists, but no powerup is "equipped").
- Towers or tower slots.
- Currency, shops, drafts, run state.
- Any special ship types beyond the basic Fighter.
- Save/load.
- Menus beyond the end-of-level overlay.

Phase 0b exit gate: a build I can run on Windows that launches into the level. Build to `Builds/NovaSpark_Phase0b/` and tell me the executable path. Then stop and wait for playtest feedback. **Do not start Phase 1.**

## Interaction style

- Ask focused questions when you need a decision. Don't pile up many questions at once unless they're tightly related.
- When you make a meaningful design choice that wasn't explicit in the doc, flag it in your report so I can confirm or course-correct.
- If the doc itself is wrong or incomplete after you dig in, propose an edit to `docs/DESIGN.md` rather than just diverging from it.
- Brevity > verbosity in your status reports. I'd rather see "done X, Y, Z; one open question: ..." than a long narrative.

Begin with the preflight checklist above. Report preflight findings, then proceed to Phase 0a.
