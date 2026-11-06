# Nova Spark

A real-time marble shooter with a roguelike layer, built in Unity 6.

> **Work in progress, very early stage.** The core shooting and chain mechanics are playable, but the game doesn't look the way it's meant to yet. All art is placeholder shapes, there is no audio, and the space theme described below isn't visible in the game so far.

An advancing chain of colored marbles moves along a path toward your base. You aim a fixed cannon and fire colored shots into the chain, and three or more of the same color destroy each other. If the chain reaches the end of the path, you lose.

The plan is to turn the marbles into an invading alien fleet, and to add between-level drafting and support towers so that each run plays differently. The full vision is in the [design document](docs/nova_spark_design.md).

## What works today

- **Chain mechanics:** shots insert where they hit, matches of 3+ clear, and gaps close to set off cascades. If closing a gap wouldn't make a match, the front of the chain holds its position until the back catches up.
- **Three levels:** Gentle S, Switchback and Long Sweep, each defined by a path and its own tuning values.
- **Eight power-ups:**

  | Power-up | Effect |
  |---|---|
  | Laser Sight | Draws a trajectory line along your aim |
  | Long Scope | Shows more of the upcoming shot queue |
  | Mimic Shot | Shots take on the color of the marble they hit |
  | Explosive Round | Destroys nearby marbles of any color |
  | Vaporizer | Destroys only the marble it hits |
  | Knockout Round | Knocks the marble it hits out of the chain and sends it flying |
  | Auto Shot | The cannon briefly aims and fires for you, choosing shots that help |
  | Chrono Field | Slows the chain, triggered by destroying a marked marble |

- **Pause menu:** includes a debug panel that spawns any power-up, so you can try them without waiting for a random drop.

## Not built yet

- Art, audio and visual effects
- Alien ship types, towers, the node map, shops, currency and save/load
- Between-level drafting: the screen exists, but it never appears yet because each run already starts with every power-up in its pool

## Controls

| Input | Action |
|---|---|
| Mouse | Aim |
| Left click | Fire |
| Right click or Tab | Swap the chambered shot |
| Esc | Pause |

## Running it

You need [Unity Hub](https://unity.com/download) and Unity **6000.4.7f1**. It has only been tested on Windows 11.

1. Clone the repo. Git LFS is configured for binary assets, so [install it](https://git-lfs.com) first.
2. In Unity Hub, choose **Add > Add project from disk** and select the folder.
3. Open `Assets/_Project/Scenes/Bootstrap.unity` and press Play.

To make a Windows build, use **Nova Spark > Build Windows** in the editor menu. The output goes to `Builds/NovaSpark/`.

Tests run from **Window > General > Test Runner > EditMode**.

## How it's put together

Code lives in `Assets/_Project/Code/`, split into assemblies with explicit dependencies:

| Folder | Purpose |
|---|---|
| `Game/Chain` | Chain movement, insertion, match and cascade rules |
| `Game/Shooter` | Cannon, shot queue and projectiles |
| `Game/Powerups` | Power-up effects, built on hooks the cannon and chain call |
| `Game/Level` | Level loading, path sampling, game states and the temporary UI |
| `Run` | State for the current run |
| `Mods` | Registry that looks up content by stable string ID |

Levels and power-ups are ScriptableObject assets in `Assets/_Project/Content/`, registered under IDs like `powerup.explosive_round`. Keeping content as data makes it easy to tune now, and it's the groundwork for mod support later. There are no imported art assets: the sprites are generated in code by `Core/SpriteFactory.cs`.

## How it was built

Anthony Bundy directed the design and playtested the game. The code was written with [Claude Code](https://claude.com/claude-code).

## License

[MIT](LICENSE)
