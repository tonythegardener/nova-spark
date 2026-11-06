# Nova Spark — Game Design Document
*Working title.*

> **Status:** Early prototype. This document describes the intended game, and most of it isn't built yet; see section 15 for current progress. It is the source of truth for design decisions. Update as decisions firm up; tag open questions with `[OPEN]`.

---

## 1. Vision

A modern, real-time marble shooter with a Space Invaders themed skin, a Zuma-style advancing chain of colored spheres to be matched by shooter, and a roguelike build-crafting layer inspired by Balatro and Slay the Spire. Tower defense elements *support* the shooter — they don't replace it. The marble shooter is the hero mechanic.

**Pitch:** "Zuma meets Space Invaders, with a roguelike draft layer." 
**Story (loose):** You are a lone defender outpost on the edge of your faction's space, defending against an oncoming alien invasion. You need to hold the line while reinforcements arrive. You have a weapon printer, but it was poorly maintained, so it randomly generates the weapons you need. Each weapon uniquely counters a specific ship class (marble color) when clustered but doesn't affect the other ship types directly (the reason color matching removes marbles). When those ships are destroyed, they can cause chain reactions in other ships if surrounded by the same class, which is the loose reasoning for chained eliminations when colors match after an internal set is eliminated. The tone can borrow from "The Last Starfighter": cheesy, and not taking itself seriously. "All your base are belong to us" fits.

**Target platform:** PC first (Windows primary, Linux/SteamOS as a follow-on). Distribution via Steam.

---

## 2. Design Pillars

1. **Shoot first, build second.** The shooter is the primary skill expression. Towers are augmentation.
2. **Synergy over power.** Power-ups feel best when they combine. Engine-building is the long-term hook.
3. **Run variety, balanced floor.** Meta-unlocks expand the draft pool. They don't raise the power floor — every run should be winnable from a clean start.
4. **Tactile and juicy.** Hits feel good. Physics are clean. Audio/visual feedback rewards skill. Flashy visuals are a goal, not a bonus.
5. **Moddable by design.** Content is data, not code. Community content extends the game's life.

---

## 3. Theme & Narrative Frame

**Setting:** Deep-space defense outpost(s). The player commands a planetary/orbital defense platform repelling waves of an alien armada.

**Marbles as ships.** Each "marble" is an alien craft sliding along an attack vector toward your Bastion (the goal). Colors = ship classes/factions. These are countered by a similarly colored shooter "marble" that is a weapons type for that specific ship class. 

**Why this works:** Space Invaders aesthetics give a natural reason for advancing waves, modular ship variety (shielded, armed, etc.), and escalating "fleet evolution" that fits roguelike progression beautifully.

**Run framing:** Each run is a campaign across multiple sectors of contested space. You push deeper, encountering an evolving enemy doctrine.

---

## 4. Core Loop

### Moment-to-moment (per level)
- An advancing chain of colored alien craft moves along a designer-defined path toward the Shooter Core (Bastion).
- The player aims a defense platform (cannon) and fires color-matched shots.
- 3+ matches detonate; combos cascade.
- Static tower slots along the path host drafted defensive structures.
- Level clears when the chain is fully eliminated. Level fails when the chain reaches the Bastion.

### Per run
- ~12–20 levels arranged into 3 acts (sectors).
- Between levels, choose a node: power-up draft, shop, event, elite, boss.
- Bosses introduce sector-specific "fleet doctrines" (run modifiers).

### Meta
- Run results unlock new draftable power-ups, new defense platforms (starter decks), new sectors.

---

## 5. Locked Design Decisions

| Decision | Choice | Notes |
|---|---|---|
| Chain style | Zuma-style path | Not Puzzle Bobble. Path gives towers something to defend. |
| Pace | Real-time | No tick/beat gating. Skill expression is primary. |
| Theme | Space Invaders / alien fleet | Marbles are ships. |
| Tower placement | Fixed slots | Designer-placed. Player picks which slot gets which tower, but not where slots go. |
| Tower role | Support, not primary | Tower defense "lite." Shooter remains hero. |
| Power-up source | Drafted between levels | Roguelike loop. |
| Target platform | PC (Windows first), Steam | Linux/SteamOS depending on engine path. |
| Engine | Unity 6 LTS with URP (2D) | See section 14. |
| Language | C# | Unity standard; integrates with MoonSharp for future Lua modding. |
| Input | New Input System | Not legacy Input Manager. |

---

## 6. Marble (Enemy Ship) Types

The standard marble (basic colored ship) is the baseline. Special types appear progressively as runs deepen and as fleet doctrine evolves.

| Ship Type | Behavior | Counterplay |
|---|---|---|
| **Fighter** (basic) | Standard colored marble | Match 3+ |
| **Shielded Cruiser** | Requires 2 matches to destroy, or one explosive | Stack hits, use AOE |
| **Gunner** | Periodically fires at towers/shooter, briefly disabling them | Prioritize; some towers can intercept |
| **Stealth Scout** | Briefly invisible at intervals; can't be color-matched while cloaked | Time shots, reveal effects |
| **Carrier** | On destruction, spawns 2 small fighters at its position | Decide whether to chain through it |
| **Commander** | Buffs adjacent ships (faster, tougher) until killed | Priority target |
| **Kamikaze** | When the chain shortens past a threshold, accelerates toward Bastion | Don't leave them last |
| **Phase Drifter** | Color shifts every few seconds | Predict cycles or use wild shots |

`[OPEN]` Final balance and which types appear in which sectors. Pace introduction carefully — never more than 1 new type per sector.

---

## 7. Towers (Defensive Installations)

Towers occupy **fixed slots** along the path. Each run, drafted towers are assigned to slots between levels. All towers support the shooter; none replace it.

### Confirmed Towers

| Tower | Effect | Design Note |
|---|---|---|
| **Gravity Well** | Marbles passing within range move slower | Makes hard angles easier; high value on twisty paths |
| **Warp Pad** | Active ability: instantly relocates shooter to a paired position | Player-triggered, cooldown-gated; great for U-turn paths |
| **Echo Turret** | Fires a mirror shot from its position whenever the shooter fires | Enables hitting path segments the shooter can't reach |
| **Magnetic Lock** | Briefly halts a section of chain when a detonation occurs nearby | Defensive panic button |
| **Beacon** | Reveals stealth ships in range, marks priority targets | Counter to Stealth Scouts |
| **Flak Battery** | Auto-fires low-damage shots at Gunners and Kamikazes | Counter to threat-class ships |

### Experimental / `[OPEN]`

| Tower | Effect | Risk |
|---|---|---|
| **Switchyard** | Reroutes path at a junction when activated | Could confuse level reading; only works on levels designed with junctions. **Lean: cut for v1, revisit as expansion content.** |

`[OPEN]` Tower persistence model: per-level vs per-sector vs full-run. **Leaning: persistent within a sector, reassignable between sectors.**

---

## 8. Shooter Power-Ups

Shooter mods alter what comes out of the cannon, how it aims, or what the player sees. These are the bulk of the draft pool.

This is the target list, not what exists today. Built so far: Laser Sight, Long Scope, Explosive Round, Chrono Field (currently triggered by destroying a marked chain marble rather than as an active ability), plus Mimic Shot, Vaporizer, Knockout Round and Auto Shot, which came out of prototyping and aren't listed below. Rapid Reload was cut for now: base fire rate is already limited by click speed, so a shorter cooldown wasn't noticeable.

### Aim & Fire

| Power-Up | Effect |
|---|---|
| **Laser Sight** | Shows shot trajectory line with bounces |
| **Accuracy Calibrator** | Tightens spread, slight homing on color match |
| **Rapid Reload** | Faster fire rate |
| **Multi-Load** | Hold 2 chambered shots, fire either |

### Shot Effects

| Power-Up | Effect |
|---|---|
| **Explosive Round** | Detonates on impact, destroys nearby marbles regardless of color |
| **Piercing Shot** | Passes through one marble before settling |
| **Freeze Ball** | Destroys one marble, freezes that chain segment briefly |
| **Wild Shot** | Counts as any color (limited charges) |

### Queue Control

| Power-Up | Effect |
|---|---|
| **Long Scope** | Reveal next 5 queued shots instead of 2 |
| **Queue Cycler** | Skip current shot; reshuffle queue |
| **Color Bank** | Store one shot for later use |

### Time & Field

| Power-Up | Effect |
|---|---|
| **Chrono Field** | Active ability: slow all marbles for ~3s, cooldown-gated |
| **Pulse Wave** | Active ability: destroy all marbles in a small radius around shooter |

### Passives

| Power-Up | Effect |
|---|---|
| **Combo Conditioning** | +X% damage per combo step |
| **Last Stand** | While chain is within 10% of Bastion, +50% combo strength |
| **Salvage Protocol** | Big combos refund a power-up charge |

---

## 9. Synergy Design Targets

The fun lives in combinations. Specific synergies to design and tune toward:

- **Explosive Round + Echo Turret** — two AOE detonations per shot.
- **Freeze Ball + Gravity Well** — long-lasting choke points.
- **Long Scope + Color Bank** — precise planning over chaos.
- **Chrono Field + Rapid Reload** — burst-damage windows.
- **Warp Pad + Echo Turret** — full path coverage from anywhere.

`[OPEN]` Anti-synergies / trap cards (Balatro has these on purpose) — include or keep the pool clean?

---

## 10. Run Structure

### Acts (Sectors)
- **Sector 1: Frontier** — Tutorial fleet. Basic fighters, occasional Shielded Cruisers. Establish core loop.
- **Sector 2: Contested Space** — Gunners and Stealth Scouts introduced. Path complexity rises.
- **Sector 3: Hive Core** — All ship types, multi-path levels, accelerating chains. Final boss.

### Node Types Between Levels
- **Draft** — Pick 1 of 3 power-ups.
- **Shop** — Spend currency on power-ups, rerolls, removals.
- **Event** — Narrative choice with mechanical outcome (e.g., "scan a derelict": gain a random tower, or take chain damage).
- **Elite** — Harder fleet; better rewards.
- **Boss** — Sector-ender with a "fleet doctrine" modifier.

### Boss Doctrines (examples)
- **Adaptive Shielding** — Chain segments gain shields after each match.
- **Reinforced Wave** — Every 10 seconds, new marbles spawn at chain head.
- **Phase Lock** — Color matching requires 4 instead of 3.

`[OPEN]` Economy. Leaning single currency ("Salvage") used for shop and rerolls. Possible secondary "Energy" for active abilities. Decide in prototype.

---

## 11. Meta-Progression

### Unlock Categories

1. **Power-up pool expansion** — beat levels/runs to unlock new draft cards.
2. **Defense platforms (starter decks)** — alternate cannons with biased starting pools:
   - *Sentinel* — Balanced starter.
   - *Artillery* — Shot-effect heavy pool.
   - *Engineer* — Tower-biased pool.
   - *Tactician* — Queue control / planning pool.
3. **Sector unlocks** — beating the run on a platform unlocks harder sector variants.
4. **Ascension tiers** — per-platform difficulty escalators (Balatro stakes / StS ascensions).
5. **Achievements** — long-tail goals that unlock cosmetic skins, alternate music, gameplay seeds.

### Base Upgrades (no draft required)
Mild permanent improvements available from run 1, unlocked via achievements:
- Slightly tighter default aim
- Default queue preview of 3 instead of 2
- One free reroll per shop
- Visual: bullet trail, custom reticles

`[OPEN]` How "deep" should meta-progression go? **Lean: light-to-medium.** Avoid making early runs feel handicapped.

---

## 12. Moddability

Community modding is a Phase 5+ deliverable, but the architectural decisions to support it are Phase 0. Build content as data, not code, from day one and most of the work is done.

### Tiers of Mod Support

1. **Asset reskins** (easy) — sprites, audio, music, themes. Load all visual/audio assets through a manifest, not hard-coded references.
2. **Data-defined content** (easy if architected right) — new power-ups, ship types, towers, levels that compose existing behaviors with new stats, art, and parameters. All content as ScriptableObjects authored in editor, or as external JSON loaded at runtime for mods.
3. **Scripted behaviors** (moderate) — new power-up or tower *logic*, not just stats. Via MoonSharp (Lua) embedded in the C# runtime. Hold until tiers 1 and 2 are proven and there's a community.
4. **Steam Workshop distribution** (separate layer) — wraps whatever modding model exists. Steamworks.NET or Facepunch.Steamworks integration when ready.

### Day-One Architectural Decisions

- **Content as ScriptableObjects.** Power-ups, towers, ships, themes — all SOs. Editor-inspectable, serializable, naturally moddable. The mod layer later adds runtime JSON loading that produces the same SO instances.
- **Stable content IDs.** Reference content by string ID (`powerup.explosive_round`, `ship.cruiser`, `tower.gravity_well`). Never by reference or class name. Mods reference these IDs.
- **Content registry pattern.** Central registry where base game and mods both register content into a common pool at startup. Game systems only know about IDs, not sources.
- **Asset path indirection.** Sprites/audio referenced by logical name, resolved through a manifest. Allows mods to override base assets.
- **Versioned schemas.** Every content definition declares a schema version. Loader validates and provides clear errors when mods are out of date.

### Things to Decide Later

- `[OPEN]` Mod sandboxing — file system, network access for scripted mods.
- `[OPEN]` Mod load order and dependency declarations.
- `[OPEN]` Steam Workshop content rules and curation.

---

## 13. Open Questions Tracker

- `[OPEN]` Tower persistence model: per-level vs per-sector vs full-run.
- `[OPEN]` Switchyard tower: include or cut for v1.
- `[OPEN]` Economy: single vs dual currency.
- `[OPEN]` Anti-synergy / trap power-ups.
- `[OPEN]` Marble ship type pacing per sector.
- `[OPEN]` Multi-path / branching levels: introduce in Sector 2 or hold for Sector 3?
- `[OPEN]` Active abilities — bind to individual keys, or shared cooldown pool?
- `[OPEN]` Save/run resume — full save anywhere or run-only saves?
- `[OPEN]` Mod scripting sandboxing model.
- `[OPEN]` Steamworks library — Steamworks.NET vs Facepunch.Steamworks.

---

## 14. Technical Notes

### Engine & Stack

- **Unity 6 LTS** (6000.x). Current LTS line.
- **Universal Render Pipeline (URP)**, 2D configuration. Required for Shader Graph and VFX Graph, which are how we hit the "flashy" bar without writing rendering code.
- **C#**, with **MoonSharp** planned for Lua-based mod scripting (Phase 6).
- **New Input System** package. Not the legacy Input Manager.
- **Steamworks** integration TBD between Steamworks.NET and Facepunch.Steamworks. Not needed until Phase 4–5.

### Project Conventions

- `Assets/_Project/` for all first-party code and assets. Keeps separation from third-party packages.
- Assembly definitions (`.asmdef`) per module to enforce boundaries and reduce compile times.
- ScriptableObjects for all content definitions (power-ups, towers, ships, themes, levels). Editor-authored; mod-loadable from JSON later.
- One MonoBehaviour per file unless they're tightly coupled.
- Namespaces: `NovaSpark.<Module>` (e.g., `NovaSpark.Game.Chain`, `NovaSpark.Powerups`).

### Source Control

- Git repo from project init.
- Unity-flavored `.gitignore` (GitHub template).
- Force-text serialization in Editor Project Settings (diffable scenes/prefabs).
- Git LFS configured at repo init for `*.png`, `*.psd`, `*.wav`, `*.ogg`, `*.mp3`, `*.fbx`, etc.

### Folder Structure
```
Assets/
  _Project/
    Code/
      Game/
        Chain/        # marble chain logic, advancement, matching
        Shooter/      # cannon, aiming, queue, firing
        Physics/      # marble collision, settling
      Entities/
        Marbles/
        Ships/        # specialized ship type behaviors
      Towers/
      Powerups/
      Run/            # run state, node map, rewards
      Meta/           # save, unlocks, achievements
      Mods/           # registry, loader, manifest
      Core/           # utilities, events, services
    Content/          # ScriptableObject instances
      Powerups/
      Towers/
      Ships/
      Levels/
      Themes/
    Art/
    Audio/
    Scenes/
    Prefabs/
  Plugins/            # third-party packages outside of UPM
  Settings/           # URP assets, input actions, etc.
```

### Data-Driven Content
All content is data, not code. Power-ups, towers, ship types, levels, themes all live as ScriptableObjects in `Assets/_Project/Content/`. This is the foundation for both balance iteration and modding.

---

## 15. Development Roadmap

**Current progress:** Phases 0a and 0b are done. Phase 1 is in progress: three levels and eight power-ups are playable, the draft screen exists but doesn't trigger yet, and towers and the node map haven't been started.

### Phase 0a — Project Skeleton
Mechanical setup. No gameplay yet.
- Unity 6 LTS project created with URP 2D template.
- Folder structure, namespace conventions, assembly definitions.
- `.gitignore`, Git LFS, force-text serialization.
- New Input System package installed and a basic input action map stubbed.
- Content registry stub with at least one ScriptableObject category (e.g., `PowerupDefinition`) wired through it.
- Empty scene loads, project builds successfully to Windows standalone.
- **Exit criteria:** clean project that builds, with patterns in place for future content.

### Phase 0b — Playable Prototype
Prove the core loop is fun.
- Single hardcoded level with one path (waypoints as a ScriptableObject).
- Shooter: aim and fire one color of marble.
- Marble chain: spawns, advances along path, accepts inserts, detects 3+ matches, cascades.
- Win/lose detection (chain cleared / chain reaches Bastion).
- Minimal placeholder art and audio. Juice optional but encouraged.
- **Exit criteria:** the level is winnable and the shooter feels good to play. Gut check before investing further.

### Phase 1 — Core Game
- 3–5 handcrafted levels.
- 4–5 power-ups, drafted between levels (basic draft UI).
- 2–3 towers with fixed slots.
- Basic node map between levels.
- All content fully ScriptableObject-driven.
- **Exit criteria:** a full mini-run is playable end-to-end.

### Phase 2 — Roguelike Loop
- Full Sector 1 content.
- Shop, event, elite, boss nodes.
- Currency (Salvage).
- Run save/load.
- **Exit criteria:** runs feel varied, build-crafting is happening.

### Phase 3 — Content Breadth
- Sectors 2 and 3.
- Full power-up pool (target ~40).
- Full tower set.
- Special ship types.
- **Exit criteria:** content scope met.

### Phase 4 — Meta-Progression
- Defense platforms (multiple starters).
- Unlock system.
- Achievements.
- Ascension tiers.
- Steamworks integration begins.
- **Exit criteria:** retention loop closes.

### Phase 5 — Polish & Moddability
- Audio pass.
- VFX / juice pass (Shader Graph and VFX Graph leveraged).
- Onboarding and tutorial.
- Difficulty tuning.
- Accessibility (colorblind palettes, input rebinding, motion settings).
- Mod loader (tiers 1 and 2: asset reskins + JSON content).
- Steam Workshop integration.
- **Exit criteria:** ship-ready.

### Phase 6 — Post-Launch
- Scripted mod tier (MoonSharp / Lua) if community demand warrants.
- Expansion content.
- Switchyard / branching paths revisit.

---

## 16. Glossary

- **Bastion** — The goal point. If the chain reaches it, the level is lost.
- **Chain** — The advancing line of alien craft (marbles).
- **Sector** — One act of a run; ~5 levels.
- **Doctrine** — A boss-introduced gameplay modifier.
- **Platform** — A starter loadout / character / deck.
- **Slot** — A fixed position along the path where a tower can be placed.
- **Salvage** — In-run currency (working name).
- **Content ID** — Stable string identifier for moddable content (e.g., `powerup.explosive_round`).

---

## 17. References / Inspiration

- **Zuma / Zuma's Revenge** — core marble shooter, chain advancement.
- **Puzzle Bobble** — color matching feel.
- **Balatro** — draft pool, synergy depth, run variety, stakes.
- **Slay the Spire** — node map, act structure, ascension. *(BaseMod community modding precedent.)*
- **Plants vs. Zombies** — tower defense pacing in a lighter shell.
- **Space Invaders** — thematic and visual inspiration.

---

*End of document.*
