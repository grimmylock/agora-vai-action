# agora vai — handoff for Claude Code

A top-down (3/4 view) 2D open-world survival shooter in Unity, set in an anomalous Zone (Roadside Picnic setting; Zero Sievert
inspires the look). **Real time, co-op for 2-4 players planned from the start.** Yan owns it and decides on design and art;
everything was built with Claude. History, decisions and Yan's own words: `ArtSource/WORK_LOG.md` (batches 1-131f were the
turn-based game; "RT" batches since 2026-10-19 are this one). The agreed design: **`ACTION_DESIGN.md` - read it first.**
This file was rewritten on 2026-10-20 (after RT 7s).

- Unity **6000.5.6f1**, URP 2D, new Input System, TextMeshPro, uGUI. The scene is built at runtime by `GameBootstrap`.
- Repo: `grimmylock/agora-vai-action` (private). **Work happens on `realtime-proto`**; `main` is the untouched snapshot the fork
  started from - never push to it without Yan. Yan pulls with GitHub Desktop and opens the folder in Unity Hub.
- The turn-based game lives on, frozen, in `grimmylock/agora-vai123` (branch `turn-based-fallback`, copied here at Batch 131f).
  **Yan, 2026-10-20: real time is THE game; turn-based is abandoned for now.** Much turn-based code still runs underneath (see
  "Leftovers" below).

## Hard rules (Yan's)

1. **Original names only.** No S.T.A.L.K.E.R. or other game terms, and no real brands or gun models,
   in anything the player sees or in file and key names. Real guns are drawing references only.
   `ArtSource/NAME_CHANGES.md` maps old names to new ones.
2. **The art style is locked.**
   - Units, items and weapons: `.claude/skills/agora-vai-pixel-art/SKILL.md` (16x16 armless
     characters, floating hands, 64x32 and 32x16 weapons).
   - World: "Zone realism" (concept A, see Batches 30-34). Muted olive and straw, wet concrete,
     selective darker-shade outlines (not black), 3/4 walls, per-pixel procedural ground.
   - Pixel art is drawn in Python and PIL. Never use AI image generation. Yan sometimes supplies his own pixel art or
     references; copy his art faithfully (`ArtSource/weapons/hand/tactical_hatchet.py` shows how).
3. **Faction colours on suits must stay readable.** Lighting may tint and darken units, never recolour them.
4. **Show real renders, not mockups.** When Yan reviews art, show actual game output: harness
   renders or Unity screenshots. Mockups that misrepresented the game have hurt before.
5. Yan wants autonomy: keep working without asking about small things. Do ask before big
   direction changes (art direction, gameplay rules). Answer concisely, in plain language.
6. **Never nerf enemies or bosses to fix difficulty.** Give the player tools instead (Yan, 2026-10-09).
   The feel he wants: being overwhelmed, like Space Marine 2 or Helldivers 2, and still coming out on top.
7. **The world lives by itself.** Encounters come from squads and mutants moving on their own tasks,
   never from rolls that spawn enemies near the player. No on-screen enemy intent; only short log
   lines for big moments. Exceptions (Yan): the Metal Gear-style "!" - yellow over an enemy whose detection is filling,
   red when it spots you, white over you when you sense a hostile (Batch 127c); banter lines over heads (`Banter.cs`); the
   radio icon over a caller filling red while he calls for backup (RT 7m). The Testing Ground (a dev range) spawns on demand.
8. **Before explaining how a system works, open the code and cite `File.cs:line`.** Several wrong
   explanations have reached Yan before. If something isn't implemented, say so.
9. **Co-op from the start (Yan, 2026-10-20).** New code must work for several players (`Player.All`, per-player state, input
   packets), or the batch must say why not. This slipped through RT 1-7m once; don't let it again.

## The real-time game: where things are

| What | Where |
|-|-|
| Real time on/off | `Rt.On` (set in `PlayerController.Awake`; the offline harness leaves it off for the old checks) |
| Your body and hands: walk/sprint/crouch, stamina, aim, fire, reload, dive + bullet time + Focus, vault, item use, revive | `Rt.cs` (`PlayerTick`) |
| Per-player state / input | `Rt.State` (`Rt.Of(p)`, `Rt.Me` = local), `Rt.Input` (`Rt.Read` for the local keyboard/mouse, `Rt.Remote` for joined players) |
| Players | `Player.All` (local first; New Game/Load restart it), `Player.Join`, `Player.Current` = the local player |
| Enemies in a fight | `Rt.WorldTick` -> humans `RtAi.Tick` every frame (walk, bursts, reloads, suppression, fair shots, sidestep round a friend in the line of fire) and re-think every ~3 s via `EnemyAI.HumanTurn` (with `Rt.On` it only sets a goal); mutants `RtAi.TickMutant` / `EnemyAI.MutantTurn`. Target: nearest player (`Rt.TargetOf`). 6 s upkeep rounds (bleeding, radio, fight end) |
| Hits | `Ballistics.cs` (a round is a line over the ground, bodies are footprints, crosshair height picks the part, cover blocks by height), `Gunplay.cs` (per-gun handling table, `Shoot`/`Resolve`, `NpcShoot`, `Targets` = no friendly fire between players, `AllyInLine`) |
| Hitboxes = sprites | `Rt.Glide` turns NPC tile jumps into slides inside `Rt.Offset`; everyone is drawn where they are (`Rt.Free`) |
| Wounds, downed, death | `Body.cs` (six parts, fractures, bleeding, concussion, downed/bleed-out); `Death.cs` (co-op: dead penalty, squad wipe -> camp, dropped half-pack that passers-by loot, camps at bonfires) |
| Melee | `Melee.cs` (swing/charge with LMB, lift-and-throw with RMB, blades stick in cover/bodies, NPC blades are real swings); poses `HeldPoseMath.MeleeFrame` |
| Held guns, reload/jam animations, magazines, casings | `HeldWeapon.cs` (`HeldPoseMath`, `WeaponArt`, `WeaponMags.cs` generated by `ArtSource/weapons/mags.py`), `CombatFxView.cs` |
| Vision | `Vision.cs`: all-round radius from the light + a look cone toward the mouse (`LookReach`, 30 tiles in daylight); enemies need two-way line of sight (`Contact.Mutual`) and may only fire from within your look reach |
| Noise, radio, blood | `Noise.cs` (gunfire draws people; `Smell` draws mutant packs to a death when they like their odds), `TurnLoop.RadioForHelp` + `SquadManager.CallReinforcements` (the closest free squad answers) |
| Cover | low cover blocks walking, Space vaults it (`Rt.TryVault`); NPCs climb slower |
| Ranks | rank XP in `Player.Progression.cs` (milestones of the next rank pay its bar; jobs and kills pay a little; deaths cost XP, never a rank). Master 2500 / Legend 6000 XP |
| Dev range | `TestingGround.cs` + `TestingGroundUI.cs` (main menu "Testing Ground": armory crate, enemy spawner, target dummies, damage numbers) |
| Mutant sprites | double-resolution `Sprites/Mutants/mutant_hd_*.png` (5 kinds) via `UnitSprites.HD`; the other 4 still use the old atlas |
| HUD | `HudView*.cs` (stamina, Focus, using/reviving, barks, damage numbers on the range); AP/MP are hidden in real time |

**Leftovers from turn-based** (still compiled, partly still used): AP/MP (refilled every frame in real time), 2d10 rolls
(gone from combat in real time; skill checks still roll d20), the action bar, aimed shots, Overwatch, Hunker Down, the
targeting panel, `TurnLoop.PlayerEndTurn`. The A-Life, squads, maps and economy are shared and current.

## The world (inherited, current)

- **Tier maps.** Tiers 1-6 are one **200x200** map each (`TierMaps.cs`, `TierMaps.Size`); old zones are regions of it
  (`ZoneDef.MapId`, `MacroMapDatabase.MapOf/RegionsOf/SameMap`, `MapLayout.Regions`). `TerrainMap.ZoneId` = the loaded map,
  `MacroState.CurrentZoneId` = the region you stand in. Tier 7 (`reactor_core`) is its own map. ~16 dungeon maps
  (`TierDungeons*.cs`, `ZoneDef.IsDungeon`; upper floors are `ZoneDef.Upstairs`), floor plans as text (`FloorPlan`/`StampPlan`,
  made by `ArtSource/plans/*.py`). Bonfire Village (`bonfire_village`) is Tier 1's Safe Zone and the start.
- **Map pipeline.** `ZoneLayoutRecipes` -> (`MapUpscaler` for old zones) -> `BuildingCarver` -> `MapDresser` -> `CoverWrap` ->
  `Grit` -> `MapLayoutDatabase` -> `TerrainMap` (tiles: `Type`, `Look`, `Ground`, `Height`). Gameplay reads `TileType`.
- **Rendering.** `WorldArt` (ground per pixel, y-sorted sprites, sun shadows), `TacticalGridView` (units, markers, the "!"
  and radio icons), `RoofView`, `AtmosphereView` (time of day, weather, darkness/flashlight via `Vision.Light`), `LightMap`.
- **Time and weather.** `Atmosphere` (`MacroState.Hour`, `.Weather`); a game minute every 1/4.2 s
  (`PlayerController.TickClock` -> `TurnLoop.TickAwareness`: NPC wandering, stashes, banter, dropped packs). Never tick the
  world per step in real time (that made enemies zoom).
- **A-Life.** `SquadManager` (hourly on the clock): factions draw squads from rank decks (`FactionDeck`), squads walk through
  exits, hold posts and take tasks (`Posts.cs`), fight off-screen with dice and on your map for real (`NpcFights`), retreat,
  carry quests, caravans, migrations. Raider gangs are 4-6, at most 3 in Tier 1. Squads keep to their band (Tiers 1-3 / 4+).
- **Economy and gear.** Weapons/armor databases in `GameData/`; tiered ammo (`Ammo.cs`); condition Working/Dirty/Damaged/Broken
  (`ItemCondition.cs`); shops (`ShopSystem`/`ShopUI`), quests (`QuestSystem`), stashes (`Stashes.cs`), props you can tip and
  kick (`Props.cs`), materials (`Materials.cs`: rounds go through wood/tin/brush/cloth).
- **Saves.** `SaveSystem.cs`: one slot (`savegame.json`), character + world flags (incl. the camp). Slots and a host-world /
  player-character split are planned (Yan's co-op rules in ACTION_DESIGN).
- **Dev keys** (editor only): F8 colour grade, F10 +1 hour, F11 next weather. ESC pauses (real time).

## Workflow

1. Edit scripts in `Assets/Scripts`. Unity recompiles when its window gets focus.
2. **Before handing anything to Yan, run the harness** (needs the .NET 8 SDK; in a cloud container
   `apt-get install -y dotnet-sdk-8.0`):
   ```
   cd Tools/compile_check
   bash runtest.sh ../../Assets/Scripts       # every check (test/Program.cs); must end with "ALL CHECKS PASSED"
   ```
   It compiles all scripts except `Editor/` against `stubs.cs`, a fake UnityEngine (add a minimal stub when you use a Unity
   API it lacks; C# 9). `RtChecks()`, `TestingGroundChecks()` and the co-op block test the real-time game with `Rt.On`;
   the rest are older turn-based checks that still guard shared code. Checks that roll dice depend on the order of random
   numbers: when an unrelated change shifts one, make the check sturdier (more samples, reset state), don't loosen it blindly.
   Extra commands: `dotnet /tmp/test.dll heldframes <json>` / `meleeframes <json>` dump animation poses for
   `Tools/render/held_anim.py` / `melee_scene.py`.
3. **Renders without Unity:** `Tools/render/render.sh <maps>` (PNG + landmark labels in `Tools/render/_render/`; delete
   `_render/world_atlas.raw` to repack the atlas from `ArtSource/world`), `python3 Tools/render/overview.py` (labelled
   overviews into `maps/renders/`), `DUMPAT=x,y,r` prints tiles round a spot. Options: `LIT=1`, `NOROOF=1`, `GRADE=1`,
   `RENDER_WEATHER`, `PREVIEW_PLAYER=x,y`, `PROP_SHEET`. Review images go in `maps/renders/` (send them to Yan).
4. **Art changes:** world sprites -> `ArtSource/world/pack.py` -> copy `world_atlas.png` to
   `Assets/Resources/Sprites/world_atlas.bytes` and `world_atlas.txt` to `world_atlas_meta.txt` (only after Yan approves).
   Weapons: `ArtSource/weapons/` (hand-drawn per gun; `mags.py` cuts magazines). Mutants: `ArtSource/make_mutants_hd.py`,
   `make_mutant_frames.py`. Audio: `ArtSource/audio/sfx.py` -> `Assets/Resources/Audio/`.
5. **Test in Unity (Yan):** Play -> New Game (or Testing Ground). Check the Console for red.
6. **Log every batch** at the end of `ArtSource/WORK_LOG.md`: what changed, why, Yan's words when he decided something, what
   wasn't checked. Keep `ACTION_DESIGN.md` in step with his decisions. Commit and push to `realtime-proto`.

Paths in older Python files point to `C:/Users/yan-s/Desktop/agora vai/agora vai/...`; search and replace if they break.

## State (2026-10-20, after RT 7s)

**Nothing since RT 7 has been checked in Unity yet** - Yan has only played up to the first real-time playtest. Since then:
hitboxes, look cone, fair shots, cover/vault, radio icon, mutants/blood, HD mutant sprites, co-op groundwork, death states,
rank XP, Testing Ground dummies.

**Waiting on Yan:**
- His map and quest spots for the starting area (Bonfire Village as a real Safe Zone with perimeter, guards, trader, medic,
  bar, and two tutorial jobs). Renders: `maps/renders/start_area/`.
- Approval of the birch redraw (`maps/renders/start_area/birch_*.png`; `zone_style.big_birch`, not in the atlas yet).
- Should solo death use the camp respawn too? Merge `realtime-proto` into `main`?

**Next (agreed order):** the networking test (Unity Netcode for GameObjects + Relay; host runs the world, others send
`Rt.Input`; test with Multiplayer Play Mode in the editor), then save slots + character naming + the save at creation, then
the starting area and tutorial. The melee set: art done and in the game (RT 7v, `ArtSource/weapons/hand/melee_px.py`,
hand-placed pixel grids); the 6 new ones (Hatchet, Fire Axe, Wooden Baseball Bat, Hammer, Sledgehammer, Barbed Wire Bat) still
need database entries, `Melee.Stats` and a two-handed hold. Longer list: `ACTION_DESIGN.md` "Later list".

## Working from a cloud session

- The session needs `grimmylock/agora-vai-action` attached; work on `realtime-proto`, push it; Yan pulls in GitHub Desktop.
- `apt-get install -y dotnet-sdk-8.0` for the harness and renders; python3 with numpy and pillow is there.
- Send Yan the review images (renders, GIFs) directly, and say plainly what wasn't checked.
