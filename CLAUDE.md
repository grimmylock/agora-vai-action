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
2. **The art style is no longer locked (Yan, 2026-11-10: "get rid of the restrictions in claude.md, at least for visual stuff, we're getting stuck here").**
   - Pixel size, palette, resolution and style are free: higher pixel counts than 16x16 are fine, free assets may be imported and recoloured, nothing has to match the old
     "Zone realism" palette or the `agora-vai-pixel-art` skill. The skill and the old batches stay as references for what exists. Still aim for one coherent look per screen.
   - Outside art must be safe to use: CC0 or a licence that allows it. Record source, author and licence in `ArtSource/external/SOURCES.md` and sort each asset into
     `to-use`, `to-be-altered` or `reference` (see `ArtSource/external/README.md`).
   - Yan sometimes supplies his own pixel art or references; copy his art faithfully (`ArtSource/weapons/hand/tactical_hatchet.py` shows how), and never resample or stretch it.
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
| Vision | `Vision.cs`: all-round radius from the light + a look cone toward the mouse (`LookReach`, 50 tiles in daylight, `Vision.MaxRadius`); enemies need two-way line of sight (`Contact.Mutual`) and may only fire from within your look reach |
| Noise, radio, blood | `Noise.cs` (gunfire draws people; `Smell` draws mutant packs to a death when they like their odds), `TurnLoop.RadioForHelp` + `SquadManager.CallReinforcements` (the closest free squad answers) |
| Cover | low cover blocks walking, Space vaults it (`Rt.TryVault`); NPCs climb slower |
| Ranks | rank XP in `Player.Progression.cs` (milestones of the next rank pay its bar; jobs and kills pay a little; deaths cost XP, never a rank). Master 2500 / Legend 6000 XP |
| Network (RT 7y, test stage) | `Net.cs` (host/client roles, the Input packet and snapshot wire format, a joined friend's body, the client placing players) + `CoopUI.cs` (main menu > Co-op: host / join by code, the corner line); `Assets/Scripts/Net/NgoLink.cs` = the Unity side (Multiplayer Services session with a join code over the relay, Netcode named messages), skipped by the harness. The snapshot (<= 1280 bytes, unreliable) carries the players, the eel boss and its cutscene (`EelSump.WriteNet/ApplyNet`) and the rest of the world (`NetWorld.cs`: enemies as mirrors built from database indices, shots and other effects, damage numbers, log lines, bullet time and hit-stop; since 2026-11-21 the duel bosses too: which boss (`Boss.Registry`), the clip it plays and its state, as a mirror `Boss.Fight` the client never ticks). Not synced: what an enemy is doing, the brain boss, items, quests, saves; never tried over a real connection |
| Saves (RT 7x) | `SaveSystem.cs`: a character file + a world file per slot (`saves/characters`, `saves/worlds`), New Game names the character and saves at once, Continue / Load list, the old `savegame.json` migrates |
| Dev range | `TestingGround.cs` + `TestingGroundUI.cs` (main menu "Testing Ground": armory crate, enemy spawner, target dummies, damage numbers) |
| Mutant animation art (2026-11-09, previews + strips, NOT in the game yet) | `ArtSource/canine_cutout.py` + `make_canine_anims.py` (Pallid/Veil/Dread Prowler, Blind Maw: run, bite, pounce on a rigid cut-out rig, the neutral pose is the original sprite pixel for pixel), `pxart.py` + `make_mutants_humanoid.py` + `humanoid_anims.py` (Alpha Prowler, Rot Crawler, Mind Weaver, Void Weaver: new volume-shaded sprites drawn from joints), the Brute in `make_mutant_frames.py`; outputs, strips, GIFs and `*_anims.json` (ms, travel) in `ArtSource/mutants_anim/`. **Never resample or stretch Yan's sprites** (his words: it "distorts the original"). Concepts: `ArtSource/MUTANT_CONCEPTS.md` |
| Armor/NPC sprites doubled (2026-11-09) | drawn at 16x16 (`Resources/Sprites/armor_*.png`, the generators and Python tools still read these); `ArtSource/double_armor.py` writes `armor_*_x2.png` (32x32 cells, 2x2 nearest) and `UnitSprites.LoadX2` loads those when present (cell size from the texture); the ragdoll (`UnitMotionView.Kill`, pieces still in 16 px units) and the floating gloves (`HeldWeaponView.HandSprite`) handle both. Re-run `double_armor.py` after any change to the 16 px files |
| Creature bosses (2026-11-21) | `BossArt.cs` (a boss `Def.Art` key -> Pixel Lab clips `Sprites/Mutants/anim/boss_<key>_<clip>.bytes`, table written by `ArtSource/export_boss_clips.py` from `ArtSource/mutants_anim/bosses_v3/`), `MotherHollow.cs`, `SingerBoss.cs`, `FerrymanBoss.cs`, `SpecimenGrown.cs`, `ExcavatorBoss.cs`, `OrderlyBoss.cs` (fights on `Boss.cs`; effects in `Boss.Land`); concepts, briefs and the Pixel Lab budget per boss (max 50, Yan) in `BOSS_CONCEPTS_V3.md`. The Verger is the scythe man only; his beast is parked (`Boss.VergerBeast`) |
| Mutant sprites | double-resolution `Sprites/Mutants/mutant_hd_*.png` (5 kinds) via `UnitSprites.HD`; the other 4 still use the old atlas |
| Language (2026-11-18) | `Loc.cs` (rules: exact lines, `{}` pattern lines, `Loc.Text` for UI text between rich-text tags / line breaks / 3+ spaces), table `Assets/Resources/Lang/pt-BR.txt` (`english<TAB>português`, `{}` in the English side = a captured piece, `{1}` `{2}` on the Portuguese side), Unity side `Unity2D/LocHook.cs` (every TextMeshPro label is translated in LateUpdate, `GameLog.Write` translates the log, the Options button switches language, the Editor writes `Logs/untranslated_pt-BR.txt`). New English text needs no code change; a missing line just stays English |
| UI, Resident Evil 4 style (2026-11-18, never run in Unity) | HUD: `HudView.RE4.cs` (condition ring, ammo, quick-slot diamond, fades when calm, danger vignette, pick-up toasts; F4 or pause menu "HUD STYLE" switches to the old cards; any exception falls back to the old HUD), pause menu `HudView.Pause.cs`, the pack as an attaché case `HudView.Case.cs` + `HudView.Pack.cs` (tabs, sort, descriptions, right-click context menu, body grid beside it), the trader `ShopUI.cs` (tabs, list, preview, one button), `UIBuildUtil.Dress` (leather/brass look and the CASE / CHARACTER / QUESTS / MAP tab row on the quest, Zone and character panels), palette in `HudView.Fill/Edge/Lip` |
| Story Tiers 1-3 (2026-11-18) | `Story.cs` (steps/flags/talks), `Tier1Story*.cs` (village, Pavel, the Culvert, the night defence), `Tier2Story.cs` (Taken) + `Tier2Jobs.cs` (King, Bogdan, Runner, Liberators) + `Tier2Escort.cs` (trail, escort north), `Tier3Story.cs` (Lev, the lake, Sub-Station, the leak), `Tier4Story.cs` + `Preacher.cs` (the Barrier, the Apex, the Summit), `Husk.cs` (+ `Husk.Roam` in the A-Life), `GreyRobes.cs` ("those"), `StoryNpcs.cs` (the people as units), `StoryBarks.cs`; status and gaps in `STORY_STATUS.md`, plan in `STORY_OUTLINE_T4_T7.md`; art `ArtSource/make_husks.py`, `make_robes.py` |
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
- **Economy and gear.** Weapons/armor databases in `GameData/`; ammo by caliber and load (`Ammo.cs`, `GameData/AmmoDatabase.cs`, table in `ArtSource/weapons/AMMO_RT.md`); condition Working/Dirty/Damaged/Broken
  (`ItemCondition.cs`); shops (`ShopSystem`/`ShopUI`), quests (`QuestSystem`), stashes (`Stashes.cs`), props you can tip and
  kick (`Props.cs`), materials (`Materials.cs`: rounds go through wood/tin/brush/cloth).
- **Saves.** `SaveSystem.cs`: one slot (`savegame.json`), character + world flags (incl. the camp). Slots and a host-world /
  player-character split are planned (Yan's co-op rules in ACTION_DESIGN).
- **Dev keys** (editor only): F8 colour grade, F10 +1 hour, F11 next weather. ESC pauses (real time).

## Workflow

1. Edit scripts in `Assets/Scripts`. Unity recompiles when its window gets focus.
2. **Run the full harness only before pushing to live (`main`)** (Yan, 2026-11-10: "you don't need to run full harness every time, that's wasting too much time and tokens"). For commits to `realtime-proto` run only what the change needs (a plan's Python check, a render, the one affected check you can reason about) and say plainly that the harness was not run. Before any push to `main`: **run the harness** (needs the .NET 8 SDK; in a cloud container
   `apt-get install -y dotnet-sdk-8.0`):
   ```
   cd Tools/compile_check
   bash runtest.sh ../../Assets/Scripts       # every check (test/Program.cs); must end with "ALL CHECKS PASSED"
   ```
   On Windows (needs the .NET 8 SDK: `winget install Microsoft.DotNet.SDK.8`): `powershell -ExecutionPolicy Bypass -File Tools\compile_check\runtest.ps1`
   (builds in `%TEMP%\agora_harness`; extra harness commands via `-HarnessArgs eelsump,frames.json`).
   It compiles all scripts except `Editor/` and `Net/` (Unity packages) against `stubs.cs`, a fake UnityEngine (add a minimal stub when you use a Unity
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
   Item icons (trial, one icon so far): `ArtSource/pixel_draw/README.md` (sprite-gen `pixel-draw`, locked palette, hand-edited grid).
   Weapons: **guns are redone at DOUBLE size from real reference photos through SpriteCook, rules and pipeline in `ArtSource/weapons/WEAPON_SPRITE_RULES.md`** (Yan, 2026-11-13; read it first, it says what is decided; done/old list: `ArtSource/weapons/WEAPON_STATUS.md`, generated; `make_gun_x2.py` does the picture). Melee weapons keep **`ArtSource/weapons/hand/PIXEL_GUIDE.md`** (RT 7v melee set); `mags.py` cuts magazines (not yet for the double-size guns). Mutants: `ArtSource/make_mutants_hd.py`,
   `make_mutant_frames.py`. Audio: `ArtSource/audio/sfx.py` -> `Assets/Resources/Audio/`.
5. **Test in Unity (Yan):** Play -> New Game (or Testing Ground). Check the Console for red.
6. **Log every batch** at the end of `ArtSource/WORK_LOG.md`: what changed, why, Yan's words when he decided something, what
   wasn't checked. Keep `ACTION_DESIGN.md` in step with his decisions. Commit and push to `realtime-proto`.

Paths in older Python files point to `C:/Users/yan-s/Desktop/agora vai/agora vai/...`; search and replace if they break.

**This Windows PC (set up 2026-11-06):** Python 3.12 (`%LOCALAPPDATA%\Programs\Python\Python312\python.exe`, with numpy, pillow, scipy; `python` on PATH is only the Store stub until a new shell), Node.js LTS (winget, `npx`), the .NET 8 SDK, git only through GitHub Desktop's copy (`%LOCALAPPDATA%\GitHubDesktop\app-*\resources\app\git\cmd`; put it on PATH for `npx skills add`). Account-level skills installed: `level-design`, `handoff`, `unity-2d-lighting`, `gamedev-2d-art-lighting-art`, `unity-netcode`, `game-feel`, `unity-npc-behavior`; `agora-vai-pixel-art` is Yan's own zip and still has to be copied to `~/.claude/skills/agora-vai-pixel-art/SKILL.md`.

## State (2026-11-08)

**Checked in Unity by Yan:** the first real-time playtest (RT 7), the melee/Visor work (Visor 4: heads clipping, parry too hard ->
wider windows), the brain boss once (Boss 2 fixes: it walked off, triple HP, two moves at once past 50%). The fixes after each were
not re-checked. **Not played in Unity yet:** everything else since RT 7 - hitboxes, look cone, cover/vault, co-op groundwork and the
networking test, death states, save slots, gun feel, dives and bullet time on V (RT 9), poses, the pack, survival mode, sounds,
pixelfx effects, the brain boss after its fixes, Secret Lab Alpha's locks, Sub-Station Depths' locks, lighting v2 (F7).

**Done lately (WORK_LOG for detail):** story Tiers 1-3 and Tier 1 missions; Boss 1 (the Visor, parry duels); Boss 2 (the brain in the
vat, Secret Lab Alpha); the backpack; survival mode; recorded sounds; pixelfx; in-game lighting v2; lock-and-key dungeons (Alpha,
then Sub-Station Depths with the new deep-water lock; per-dungeon keys in `LabLocks.cs`); the Depths eel boss designed and its
three flooded levels built as maps (`sub_station_depths_1..3`; levels 1-2 are mazes built from their mission graphs, with the eel's
strike spots marked in the plan), the eel's art is Yan's own drawing recovered to game pixels and bent along paths (`eel_native.py`,
`eel_poses.py`, `eel_anim.py`: animation strips for rear-up, lunge, snap, slam, sweep, hurt, reveal (swallows a body), vomit and retreat (its 'death'), the wrap and the swim, each with damage overlays at 75/50/25 %, waiting on his look), the Sump fight built (2026-11-05, see
`ACTION_DESIGN.md` "Dodge bosses"): four deep pools, authored spine moves in `EelSump.cs` (combo, arch, dash; phase 2: snare launch/hurl, flood), a targetable tail, always up and down a pool's centre; the platform form (wrap, shock, strike) is `EelBoss.cs` + `EelRig.cs`; a FIRST-PASS Unity view (`CombatFxView.Eel.cs`: the baked body strips `boss_eel_body_s0..3.png` laid along the spine, the head, shadows, telegraphs, impacts; `HudView.Boss.cs`: its bars) and the Testing Ground entry (main menu > Testing Ground > BOSS The Drowner, `TestingGround.EelFight`) exist but have NEVER been run in Unity; clips of the real fight come from the harness dump (`eelsump`, `Tools/render/eel_real_clip.py`); ten ready-made pixelfx effects in the game (`CombatFx.PxAt`, RT fx 5).

**Bosses, 2026-11-21 (WORK_LOG 9ag/9ah, `BOSS_CONCEPTS_V3.md`, `PLAYTEST_BOSSES_V3.md`, review page https://claude.ai/artifact/T7s9cNMZh117QESP6TtfqG):** the Verger is the scythe man only (eight own poses, two phases, hat off in phase 2; his werewolf beast is parked as `Boss.VergerBeast`, Yan: "looks very ai generated"); six creature bosses concepted, animated with Pixel Lab (Yan bought a plan: 2000 generations, max 50 a boss; about 70 used) and built as fights with Testing Ground entries, harness checks, co-op mirror sync, procedural sounds and pt-BR lines: Mother Hollow (T1), the Singer (T2), the Ferryman (T3), the Specimen grown (T4), the Excavator (T5), the Orderly (T6). `dotnet /tmp/test.dll bossprobe` runs a bot through every duel boss. NONE of it run in Unity. Waiting on Yan: his picks of the concepts, a Unity run per `PLAYTEST_BOSSES_V3.md`, the weak points listed as NOT built per boss, whether floor markers are wanted (he turned the eel's off).

**Waiting on Yan:**
- **The eel boss, never run in Unity:** a Unity play session of Testing Ground > BOSS The Drowner (Console first, then: do the four pools show, does it come up out of a pool's centre, telegraphs, impacts, bars, lifted/thrown players). Then his tuning notes (all damage/timing/radius numbers are guesses) and: what the body's story key opens (Gamma's Sealed Vault is the proposal), the final boss name ("The Drowner" is a placeholder).
- His review of Sub-Station Depths (`maps/renders/dungeon/sub_station_depths_before_after.jpg`) before any other dungeon is redone.
- A Unity play session: lighting (night too bright or flat? does F7 show a difference? cost per frame unmeasured), locks, brain boss.
- Pose picks (`maps/renders/poses/poses_preview.png`); his map and quest spots for the starting area; the birch redraw.
- Should solo death use the camp respawn too? Merge `realtime-proto` into `main`?

**Done since (2026-11-07/08, WORK_LOG for detail):** the eel's three-phase Sump fight (flood at 75%, the storm and the bite at 50%), the storm's hand-built VFX (coiling lightning, charging water, shock ring), the introduction CUTSCENE (examine the body, west pool, slither, swallow, east pool, middle pool; cinematic camera, ESC skips), co-op sync of the cutscene and the fight (`EelSump.WriteNet/ApplyNet`), the story key (the body carries the Sealed Vault key; Gamma's vault needs it) and the eel parts, the platform form for Levels 1-2 (proxy spine, lamp tell with the silence, false alarms, wounds carry down, the last pump waits for the eel), shorter shot VFX, and the merge of Yan's PC session (whole-body hitbox, sounds, brain vat sprites). NONE of it has been run in Unity yet.

**Next:** the eel - **TO-DO (Yan, 2026-11-06): a FRONT-FACING version of the eel's head and neck rising out of the water (he sent a reference in chat: head and neck seen from the front, spines round the head, two blind white eyes, open mouth dripping, the ribbed belly down the neck, a splash ring at the water line) - for the moments it looks at the camera; `ArtSource/refs/eel/` only has the side-view drawing, the front-facing file still has to be sent and saved; his own drawing, never generated.** Then what Yan's Unity run finds (the first-pass view's seams, draw order, damage numbers, the lump on the body, camera feel, lamp visibility), tuning `EelSump.cs`, the other dungeons the same way (after Yan's go on Depths), lighting follow-ups, the co-op test of enemies (clients can't see most enemies yet) and over a real connection. Pending tasks: #19 (building fronts, scale, power lines), #36 (why Yan loses to humans as Ex-Military).
Longer list: `ACTION_DESIGN.md` "Later list".

## Working from a cloud session

- The session needs `grimmylock/agora-vai-action` attached; work on `realtime-proto`, push it; Yan pulls in GitHub Desktop.
- `apt-get update && apt-get install -y dotnet-sdk-8.0` for the harness and renders; python3 with numpy and pillow is there.
- Send Yan the review images (renders, GIFs) directly, and say plainly what wasn't checked.
- **Skills used** (install at account level only, never into this repo; read each SKILL.md and its scripts first; a cloud container
  loses them, so reinstall per session): `level-design` https://github.com/Scouten2/LevelDesignSkill (dungeon mission graphs,
  `graph_check.py`) - `agora-vai-pixel-art` (Yan's own, on the account). `pixelfx` (https://github.com/patch-gg/pixelfx, effects) is
  already in the repo at `.claude/skills/pixelfx`: nothing to install.

## Handoff, 2026-11-24 (what a fresh session should know first)

- Branch `realtime-proto` is current; `main` was last fast-forwarded at the Sledgehammer spring batch (9dw). Batches 9dv-9eh in `ArtSource/WORK_LOG.md` are everything since (Verger scythe flip, melee weapon sheet, item-use fix, walk/run/lean fixes, mutants no longer flee at a health share, squads barred from empty dungeons and Safe Zones, Tier 1 band, village + hub + barracks crews and services, medic/bar dialogue). **None of it has been run in Unity** - Yan tests next; his reports come back as screenshots/videos.
- Pixel Lab works (2026-11-24): the key is a network secret on the environment (`api.pixellab.ai`, type Bearer - saved without the Bearer type it gives 401); the REST scripts `ArtSource/pixellab_*.py` and the MCP tools both work in the session. First check: `curl https://api.pixellab.ai/v2/balance`. Props for the settled places: `ArtSource/world/settle_pl/` (Pro Flash, `view=low top-down`, style anchor = the atlas desk; canvas must be at least the anchor's 38x42).
- Next work Yan asked for: real props for settled places (beds, bar counter, bottle shelf, medical cabinet - currently stand-ins from `desk/armchair/lockers`), lines for the other crew members (guards, villagers, traders), more world detail outside the safe zones, the stutter hunt (`[PERF]` Console lines from `Perf.cs`; waiting on Yan's log), Yan's description of the boss counter-timing mismatch.

- **Parked until Yan is at the Unity PC (2026-11-24):** drive a live Unity Editor through the CoplayDev "MCP for Unity" package (`unity-mcp`; read the Console, enter Play mode, screenshots, run tests) - needs the package added to `Packages/manifest.json`, the bridge server on his PC and a session that can reach his machine (a Claude Code session on that PC, or an attached folder). It can run arbitrary C# in the Editor: only on a branch/copy he is happy for Claude to change. Other ideas from the skills Yan reviewed: assembly definitions to cut recompiles (one assembly today; analysed 2026-11-24: only `GameData`, 24 files / 11k of 59k lines, can be split off, after moving 10 references to `GameCore.Economy/Optics/Story/ZoneLayoutRecipes` out of `ShopDatabase`, `QuartermasterDatabase`, `MapLayoutDatabase`; `GameCore` itself is 48k lines of mutually-referencing statics and cannot be split - so the gain is about 18 % of a recompile and a wrong package reference in an asmdef would stop the project compiling: do it only with the Unity MCP or Yan at the PC), an audit of static state that a second Play would keep, an emptiness audit of the tier maps (distance from every walkable tile to a landmark/prop cluster), a worst-viewpoint tour for `Perf`.
