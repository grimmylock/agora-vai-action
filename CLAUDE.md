# agora vai (real-time fork) — handoff for Claude Code

**This repo is the REAL-TIME fork** (`grimmylock/agora-vai-action`, created 2026-10-19). It was copied from the
turn-based game at Batch 131f (`grimmylock/agora-vai123`, branch `turn-based-fallback`, commit af02b98), which stays
frozen and continues separately on `tier-maps`. The plan agreed with Yan is **`ACTION_DESIGN.md`**: read it first.
`main` here is the untouched starting snapshot; work goes on branches (now `realtime-proto`). Everything below the
real-time section describes the inherited turn-based game, most of which still runs underneath.

## Real-time fork: where things are (RT batches, logged in `ArtSource/WORK_LOG.md`)

- `Rt.On` (set in `PlayerController.Awake`) switches the game to real time; the offline harness leaves it off, so the
  old turn-based probes and checks still run. `RtChecks()` in `Tools/compile_check/test/Program.cs` tests the new parts.
- `Rt.cs`: your movement (free within and across tiles; crossing a tile is a real step via `PlayerController.StepFree`),
  stamina, aiming, firing, reload timers, camera look-ahead, ESC pause, the dive/bullet time/Focus (RT3); `Rt.WorldTick` runs
  enemies in a fight: humans re-think every ~3 s through the old `EnemyAI.HumanTurn` (roles, cover/flank tiles, grenades,
  morale - with `Rt.On` it only sets a goal) and act every frame in `RtAi.Tick` (walk, bursts, reloads, suppression,
  call-outs); mutants still take a whole old turn every ~3 s. A 6 s upkeep round covers bleeding, radio, fight end.
- `Body.cs` (RT2): six parts per human, Yan's 60% wound rule, fractures/bleeding/concussion, downed and bleed-out.
- `Ballistics.cs`: a round is a line over the ground; bodies are footprints; the crosshair's height on the sprite picks the
  part (head/torso/arm/legs), spread scatters sideways and up/down; low cover blocks by height, walls stop rounds.
- `Gunplay.cs`: per-class handling (spread, steadying, kick, recovery, rate, reach, reload), `Shoot` / `Resolve` (damage
  through the old armor math), `NpcShoot` (NPC gunfire from `Weapon.Fire` becomes real rounds when `Rt.On`).
- Views: `UnitMotionView.Place` draws the player where it really is (`Rt.Free`), `TacticalGridView.StandOffset` reads
  `Rt.Offset`, `TacticalGridView.Reticle` draws the spread crosshair, `CombatFxView` plays `FxEvent.Now` rounds at once.

## Hard rules (Yan's)

1. **Original names only.** No S.T.A.L.K.E.R. or other game terms, and no real brands or gun models,
   in anything the player sees or in file and key names. Real guns are drawing references only.
   `ArtSource/NAME_CHANGES.md` maps old names to new ones.
2. **The art style is locked.**
   - Units, items and weapons: `.claude/skills/agora-vai-pixel-art/SKILL.md` (16x16 armless
     characters, floating hands, 64x32 and 32x16 weapons).
   - World: "Zone realism" (concept A, see Batches 30-34). Muted olive and straw, wet concrete,
     selective darker-shade outlines (not black), 3/4 walls, per-pixel procedural ground.
   - Pixel art is drawn in Python and PIL. Never use AI image generation.
3. **Faction colours on suits must stay readable.** Lighting may tint and darken units, never recolour them.
4. **Show real renders, not mockups.** When Yan reviews art, show actual game output: harness
   renders or Unity screenshots. Mockups that misrepresented the game have hurt before.
5. Yan wants autonomy: keep working without asking about small things. Do ask before big
   direction changes (art direction, gameplay rules). Answer concisely, in plain language.
6. **Never nerf enemies or bosses to fix difficulty.** Give the player tools instead (Yan, 2026-10-09).
   The feel he wants: being overwhelmed, like Space Marine 2 or Helldivers 2, and still coming out on top.
7. **The world lives by itself.** Encounters come from squads and mutants moving on their own tasks,
   never from rolls that spawn enemies near the player. No on-screen enemy intent; only short log
   lines for big moments. Exception (Yan, 2026-10-15): the Metal Gear-style "!" - yellow over an enemy
   whose detection is filling, red when it spots you, white over you when you sense a hostile (Batch 127c);
   and banter lines over heads (`Banter.cs`, Batch 129: chatter, greetings, "Huh?", the spotting shout).
8. **Before explaining how a system works, open the code and cite `File.cs:line`.** Several wrong
   explanations have reached Yan before. If something isn't implemented, say so.

## Layout

| Path | What |
|-|-|
| `Assets/Scripts/GameCore/` | All game code (about 120 files, 23k lines). The scene is built at runtime by `GameBootstrap`. |
| `Assets/Scripts/GameData/` | Static databases: weapons, armor, mutants, zones (`MacroMapDatabase`), layouts. |
| `Assets/Scripts/Editor/IncomingSync.cs` | Unzips any `Incoming/*.zip` over the project root (the old cloud delivery path). |
| `Assets/Resources/Sprites/world_atlas.bytes` + `world_atlas_meta.txt` | The 3/4 world atlas (PNG bytes plus a sprite index), made by `ArtSource/world/pack.py`. |
| `Assets/Resources/{Sprites/Weapons,Audio,Fonts}` | Hand-pixelled weapons, 65 procedural WAVs (`ArtSource/audio/sfx.py`), OFL pixel fonts. |
| `ArtSource/` | The art and audio pipeline in Python (numpy and PIL). It writes to `ArtSource/_out/`. |
| `ArtSource/concept/` | Concept-frame generators: `concept.py` (styles A/B/C), `biomes.py` (swamp/city/industrial/lab), `world_shots.py`. |
| `Docs/00..10_*.md` | The design docs: rules, weapons, armor, artifacts, mutants, gear, zone map, loot, economy, quests, living world. The code cites them as "file 04" and so on. |
| `Tools/compile_check/` | The offline harness. See below. |

## Key systems (where to look)

- **Tier maps (Batch 70).** Tiers 1-6 are **one ~200x200 map each** (`TierMaps.cs`), authored at full
  size; the old zones are **regions** of it. `ZoneDef.MapId` names the zone that owns the map
  (`perimeter`, `scrapyard`, `scavengers_hub`, `vanguard_barracks`, `quarry_basin`, `abandoned_city`);
  `MacroMapDatabase.MapOf/RegionsOf/SameMap`; `MapLayout.Regions` is the region per tile.
  `TerrainMap.ZoneId` = the loaded map, `MacroState.CurrentZoneId` = the region you stand in
  (`ZoneTravel.UpdateRegion` every step: free, no time). Exits, landmarks, loot, anomalies are per map;
  squads belong to the region their leader is in (`SquadManager.OnMap`, `UpdateLiveRegions`,
  `MayWalkInto`). Dungeons are small full-size maps (`TierDungeons.cs`, `ZoneDef.IsDungeon`);
  `ZoneDef.RequiresKey` + `MacroState.Keys` lock one (the flooded bunker). Bonfire Village
  (`bonfire_village`) is Tier 1's Safe Zone. Canvas helpers for authoring: `Road`, `Fence`, `Grove`,
  `Hut`, `Block` (the carver makes rooms), `Ruin`, `Hedge`, `Stash`, `Keep` (dresser leaves it alone),
  `Regions`. Tier 7 (`reactor_core`) and the old hidden nodes still go through the upscaler.
- **Map pipeline.** `ZoneLayoutRecipes` (authored zones via `ZoneLayoutBuilder`/`TerrainCanvas`)
  feeds `MapUpscaler` (3x), then `BuildingCarver` (hollows solid blocks into rooms, doors and
  windows), then `MapDresser` (per-biome composers `Marsh`/`City`/`Industrial`, then generic
  vegetation, shacks, camps and wrecks). The result is `MapLayoutDatabase` and then `TerrainMap`
  (tiles: `Type`, `Look`, `Ground`, `Height`). Gameplay reads only `TileType`; `Look` is cosmetic.
- **Rendering.** `WorldArt` (static) composes the ground texture per pixel and gives the standing
  sprite per cell: y-sorted `sortingOrder = SortBase + foot pixel row`.
  - It also bakes the sun shadows (`CastShadows`, re-baked on a background `Task` when `SunKey()` changes).
  - `TacticalGridView` owns the scene objects, units, reach outline and markers.
  - `RoofView` does roofs: rooms seen into lose their roof, and the player's own building fades out.
  - `AtmosphereView` draws the time-of-day wash, fog, rain, lightning and the darkness/flashlight light texture.
- **Time and weather.** `Atmosphere` holds `MacroState.Hour` and `.Weather` (saved), and `Look()`
  gives the per-hour keyframes; the shown look eases over 2.5 s (Batch 67). A combat round is 1
  minute, a zone crossing is 8 h, and sleeping wakes you at 06:30.
  - **Exploring runs on its own clock** (Batch 67, Yan chose it): out of combat a game minute passes
    every 1/4.2 s (`PlayerController.TickClock`), and each minute runs `TurnLoop.TickAwareness`, so
    NPCs move while you stand still. Click routes are walked one tile per step. A game day is about
    6 real minutes: worth revisiting once maps are bigger.
- **Vision.** `Vision` does line of sight within 20 (enemies see 12; Batch 65) and light (radius plus flashlight cone).
  `Sees()` is the strict gameplay check. `InSight()` is the display version, which also counts
  leaning one step.
- **Combat and AI.** `CombatRules`, `CombatResolver`, `TurnLoop`, `EnemyAI`, `SquadManager`, `NpcFights` (NPC vs NPC).
  - Attacks roll 2d10 with hit tiers (graze x0.85, clean, vital x1.2, double-10 headshot x1.5); skill
    checks stay d20; jams are a separate d100 (1% +2% per wear step).
  - Aimed shots (L), sneaking (N) with enemy facing cones, ambush shots one tier higher, breaking
    contact, radio backup calls, retreats, rank growth from kills.
  - Batch 69 player tools: surprise round (`EncounterState.Surprised`), sneak takedown
    (`Player.TryTakedown`), resting heals (`WorldDays.Rest`), hireable Wanderer (`Companion.cs`,
    hire row in `ShopUI` at Wanderers traders).
  - Batch 72: Hunker Down (K, `CoverUtil.Hunker`: cover from every side, doubled), Overwatch (O, fires on the first
    hostile step in view, `Perks.OnStep` from `TacticalGridSystem.AttemptMove`), the Tier 1 Raider shakedown
    (`Shakedown.cs`: pay, hand over your gun, refuse, or shoot first), wrapping cover (`CoverWrap.cs`, Ls after the dresser).
- **A-Life.** `SquadManager` ticks hourly on the clock; factions draw squads from fixed rank decks at
  their outposts (`FactionDeck`); squads walk in and out through exits, fight off-screen with dice
  and on your map for real (`NpcFights`), retreat when beaten, carry quests, caravans and migrations.
  On your map a squad moves as one: the leader walks 8-20 tiles at half your pace and rests 20-90
  min, the rest keep a ring round it (`TurnLoop.CheckVisionAndWander`, Batch 67).
- **Animated props.** Cells with the `campfire` look play `Resources/Sprites/bonfire_strip.png`
  (6 frames, from `ArtSource/make_bonfire.py`) in `TacticalGridView`, like the anomaly strips.
- **UI.** `HudView` (built in code, Silkscreen and Pixelify fonts) plus the older panels (I toggles them).
  The XCOM-style action bar (Batch 72): `CombatActions` lists every action (icon, key, AP, why it's greyed, what it
  does), `HudView.ActionBar.cs` draws it; icons from `ArtSource/make_action_icons.py` (`icon_act_<name>.png`).
  Batch 77: clicking an enemy opens the body-part targeting panel (`HudView.Targeting.cs`; odds from `Weapon.PreviewHit`,
  the same numbers `Weapon.Fire` rolls with) and Tab the examine panel (`Examine.cs`).
- **Balance knobs and probes (Batches 74-77).** Player AP 5, 6 from Veteran; NPCs 4, bosses (`Named`) like the player
  (`CombatRules.PlayerRankAp/NpcAp`). Range: normal rolls inside a gun's range, -2 out to twice it (`Weapon.MaxReach`).
  Snipers 3 AP a shot, +1 to hit. Squads settle in their leader's tier (`SquadManager.Settle`). Enemies only spot you
  when you can see them (`TurnLoop.CheckVisionAndWander`). Probe-only knobs: `CombatRules.Lethality`, `ArmorScaleByTier`,
  `PlayerApBonus`. Fight simulations live in `Tools/probes/` (results next to them).
  - Yan's targets per tier and build: open fight vs 1 80%+, ambush on a typical squad ~60%, open fight vs a squad low.
    `BuildsProbe targets` runs all 9 builds; `BuildsProbe elite <n> <tier> <foe> <ranks>` a fixed squad; `TierDiag.cs`
    breaks one fight down (hit chances and damage both ways).
- **Batches 80-85.**
  - Two weapon slots: Y swaps; a pistol or blade is free, anything else 1 AP; a sniper rifle pairs only with a pistol or blade
    (`Player.SecondaryWeapon`). Gunslinger needs Point-Blank Execution (`PerkDef.Requires`).
  - Tiered ammo per class (`Ammo.cs`, pools like "rifle3|HollowPoint"); traders sell only their own tier.
  - Snipers: +1 beyond half range, -2 within 5 tiles. RK-12: +1 AP a shot (`CombatRules.Kick`).
  - Faction decks (`FactionDeck`): a south deck (Rookie-Veteran, x3 roster) and a north deck (Professional-Legend, x4).
    Squads made for Tier 4+ draw from the north deck with an Expert+ leader first. South squads draw whole, and the best
    card leads. Members are at most 2 tiers under the leader.
  - Squads keep to their band (Tiers 1-3 or 4+, `SquadManager.InBand`).
  - Human bosses by tier (`SquadManager.Arm`/`MakeBoss`), AP rounds for bosses (50%) and Tier 4+ members (5-15% by rank).
  - Enemies are kitted by rank, not by map tier: Yan decided to keep that, and to leave Tier 5 as it is (Batch 85).
- **Batches 86-92.** Boss perks; HP/AP ammo always sold (`ShopSystem.AmmoBoxPrice`); the Garrison raid (`GarrisonRaid.cs`);
  the G-7 lab (`G7Lab.cs`, Lattice Foundry, two floors). **Floor plans** (`TierDungeons.Plans.cs`, `FloorPlan`/`StampPlan`,
  one char a tile, legend in the summary): the Foundry (`ArtSource/plans/foundry_plans.py`), the six concrete hidden labs and
  bunkers (`lab_plans.py` -> `to_cs.py` -> `TierDungeons.LabPlans.cs`) and the Siege Ward Hospital (`hospital_plan.py`), all messy
  on purpose (Yan: "less neat ... dark, gritty, dirty, bloody"). `Grit.cs` dirties every other map's floors and lights dungeons.
  **Light** (`LightMap.cs`): lamps/campfires/ceiling and red emergency lights; `Vision.Light` counts them; flashlight toggle (U)
  for stealth in the dark. Renders: `LIT=1` (darkness + lamps), `NOROOF=1` (no roofs) for `Tools/render/render.sh`.
- **Batches 93-96 (art pass).** Audit `maps/renders/AUDIT.md`; reference photos `ArtSource/refs/` (`fetch.py`, Openverse;
  references only, sprites stay hand-drawn); 7 props redrawn at real size (16 px person = 1.8 m). Sun shadows soft and graded,
  overcast shadows, 10-minute sun steps, more clear days (`WorldArt.SunStrength`). North-south and corner sprites for runs of
  cover (`WorldArt.RunLook`). Power lines (`WorldArt.DrawWires`). Render options: `GRADE=1`, `RENDER_WEATHER`, `PROP_SHEET`,
  `LOOK_AUDIT=1`, `TIME_BAKE=1`.
  - Agreed plan, in order: reference library for every prop, building fronts and power lines; prop redraws with a **material**
    each (impact effect, sound, toughness; bullets go through wood/tin, not concrete); building fronts and true scale; then
    **physical props** (Gungeon-style: flip tables for 1 AP into one-sided cover, a standing table is no cover; kick barrels to
    roll them, they explode only when shot; breakables; blasts shove props; enemies use all of it). Breakable walls: later.
- **Batches 97-113.**
  - Ruins and worn walls (`_ruin`/`_worn` wall sprites), caves as messy plans (`cave_plans.py`), the G-7 unique, saved toggles.
  - **Free movement, step 1 (Batch 102, Yan chose "option 2": the grid stays underneath, movement stops looking like a grid):**
    `ICombatant.Mp` is a float; a diagonal step costs 1.5 (`TacticalGridSystem.Diagonal`, `IGridService.StepCost`), never past
    a wall corner; A* searches 8 ways; `Pathfinding.Smooth` pulls routes into straight lines; walkers round corners
    (`UnitMotionView.CornerCut`). Next steps: units standing anywhere inside a tile, ranges in real distances.
  - **Physical props (103, 107, `Props.cs`):** click a desk/barrel/crate next to you (or the Shove button) - a desk tips into
    one-sided light cover (`desk_n/s/e/w`, `Props.Shields`), a standing desk is no cover; a barrel rolls up to 6 tiles and knocks
    down whoever it hits (explosive ones blow only when shot); crates and tipped desks slide a tile; blasts shove props; enemies
    tip desks for cover and kick barrels up to you to shoot them. Desk sprites follow Enter the Gungeon's tables (`ArtSource/refs/desk_flipped_game/`).
  - **Reference library (104):** `ArtSource/refs/INDEX.md`, 61 folders; `fetch.py` adds, `--drop` prunes.
  - **Materials (105, `Materials.cs`):** toughness per material; a round the cover stopped goes through wood/tin/brush/cloth as
    a half-damage graze; impact chips and sounds per material (`hit_wood/tin/concrete/dirt`).
  - **Walls one storey tall (106):** `walls.py STOREY = 28` (3 m), `RoofView.Lift` matches.
  - A-Life posts (110, `Posts.cs`): the map's landmarks are posts; squads get a home post and a task (hold, patrol, scavenge,
    attack) for 3-6 h. Buildings messier (112). PL-62 Kobza sprite (113). Walking every landmark of a tier map takes 4-6 real
    minutes at the exploring pace (111, `Tools/probes/ExploreTime.cs`): the pace is Yan's call. (HP/AP ammo stays always
    on sale: Yan, Batch 86.)
  - Batches 114-121: units stand anywhere in a tile (`StandOffset`); path preview dots, green within MP and red beyond
    (`TacticalGridView.UpdatePathPreview`); big trees in many shapes (`zone_style.py big_*`); L/U building shapes (`BuildingCarver`
    shape cut); two-storey fronts (`walls.py STOREY2`, `RoofView.TwoStorey`); **upper floors as their own maps** (farmhouse,
    school, flats: `house_plans.py`, `ZoneDef.Upstairs`, `TierDungeons.StampHouse/Upstairs`); building references by kind
    (`refs/bld_*`). Walking speed: on hold until Yan has tested it.
  - Probes after 102 and 105 (`BuildsProbe targets`): averages unchanged (open fight vs 1 ~94%, ambush on a squad ~64%,
    open fight vs a squad ~12%); blades lost some ground after 102.
- **Dev keys** (editor only): **F8** = colour grade on/off, **F10** = +1 hour, **F11** = next weather.

## Workflow

1. Edit scripts in `Assets/Scripts`. Unity recompiles when its window gets focus. There's no
   need for `Incoming/` any more now that you work in the project directly.
2. **Before handing anything to Yan, run the harness** (it needs the .NET 8 SDK; in a cloud
   container `apt-get install -y dotnet-sdk-8.0` gives exactly the version the scripts expect):
   ```
   cd Tools/compile_check
   bash runtest.sh ../../Assets/Scripts       # every check (test/Program.cs); must end with "ALL CHECKS PASSED"
   ```
   `test/Program.cs` is the real harness. The top-level `Program.cs` is stale (doesn't compile since
   Batch 63). It writes `offline_squads.json` into the folder it runs from (gitignored).
   It compiles all scripts except `Editor/` against `stubs.cs`, a fake UnityEngine. If you use a
   Unity API the stubs don't have, add a minimal stub to `stubs.cs`; the fake behaviour is enough.
   The harness uses C# 9; Unity accepts it too.
3. **World renders without Unity** - the quick way since Batch 70:
   `Tools/render/render.sh perimeter scrapyard ...` (PNG + landmark labels in `Tools/render/_render/`),
   `python3 Tools/render/overview.py` (labelled overviews and dungeon sheets into `maps/renders/`),
   `DUMPAT=x,y,r Tools/render/render.sh <zone>` prints tile types and heights round a spot. The older way:
   ```
   python ArtSource/world/pack.py Tools/compile_check/_render     # atlas png/txt/raw (needs numpy, pillow)
   $env:ART_DIR="..\..\Tools\compile_check\_render"; $env:RENDER_ONLY="marshes,abandoned_city"
   $env:RENDER_HOUR="17.5"; $env:RENDER_WEATHER="Clear"; dotnet run -c Release
   python raw.py from _render/render_marshes.raw marsh.png
   ```
   Other environment variables:
   - `PREVIEW_PLAYER=x,y`: roofs cut away as that player would see them.
   - `LOOK_STATS=1`: look counts per zone.
   - `LOOK_WHERE=door`: coordinates of a look.
   - `TIME_RENDER=1`: render timings.
   - `DUMP_ZONE=id`: ASCII tile dump to the temp folder.

   `ArtSource/concept/world_shots.py` turns a render into a 1080p in-game-style shot, with the
   atmosphere overlays, units and a label.
4. **Art changes:** run `pack.py` to a folder, then copy `world_atlas.png` to
   `Assets/Resources/Sprites/world_atlas.bytes` and `world_atlas.txt` to `world_atlas_meta.txt`.
   For audio, run `ArtSource/audio/sfx.py`, then copy the WAVs to `Assets/Resources/Audio/`.
5. **Test in Unity:** Play, then New Game, pick a perk, Start. WASD explores; clicking moves and
   shoots. Check the Console for red.
6. Log every batch at the end of `ArtSource/WORK_LOG.md`: what changed, why, and Yan's words when
   he decided something.

Paths in the Python files point to `C:/Users/yan-s/Desktop/agora vai/agora vai/...`. If the
project moves, search and replace that prefix.

## State on handoff (Batch 121, 2026-10-15)

Batches up to 69 are in the PC project and compile in Unity. **Batches 70-121 are on the git branch `tier-maps`**; Yan has
opened 70 in Unity; nothing from 72-121 has been checked in Unity yet (checklist: `maps/PLAYTEST.md`). The harness prints ALL CHECKS PASSED.
Still open: a run cycle for the hounds (Batch 131f), free movement step 2, prop redraws from the reference library, two-storey fronts and true-scale trees
(Yan's call: a real birch would be 130-180 px), breakable walls (parked by Yan).
Renders for review: `maps/renders/tierN_overview.jpg` (labelled), `tierN_detail.jpg`, `tierN_dungeons.jpg`.

**Done since Batch 34b** (details and Yan's words in `WORK_LOG.md`):
- 35-42: colour grading; A-Life on the clock; squads walk through exits; off-screen news; factions
  fight on your map; the world seeds and refills itself; no spawn rolls (every enemy belongs to a squad).
- 43-51: the tabletop audit: reinforcements, quest squads, caravans, migrations, faction goals,
  noise, situational morale, spatial anomalies, hidden ways and clues are all real mechanics.
- 52-55: rank decks and outposts, Yan's faction tiers, rank growth from kills, retreats, Raider gangs.
- 56-66: 2d10 hit tiers, aimed shots, radio backup, no turns while exploring, vital wounds, stalking
  mutants close in, breaking contact, jam rates, ambush shots, zoom/sight/range changes, sneak and facing.
- 67: playtest fixes: walk speed, gradual light and weather, sane click paths, the exploring clock,
  squads move as groups at walking pace, the flipped light layer (inverted flashlight, vanishing units).
- 68: animated bonfire (Pyre flame style).
- 69: player tools for early fights (surprise, takedown, resting heals, companion).
- 70: the map overhaul: one 200x200 map per tier (Tiers 1-6) with ~25 spots each, Bonfire Village,
  13 new dungeons (5 in Tier 1), regions, map-wide exits/squads/quests, heap A*, no roofs underground.
- 71: Electronic Lock loot rolls the map's tier.
- 72: XCOM-style action bar with pixel icons, Hunker Down, Overwatch, the Raider shakedown, cover that wraps.

**Measured, not yet acted on (Batch 72 probe):** hunkering behind an L of hard cover makes a fresh Scavenger last 2-3x
longer against a Tier 1 Raider gang, but the win rate stays near 0% for 3+ Raiders: the starting pistol does ~10 a hit
against 100 HP Raiders who also take cover. Waiting on Yan's call on the player's damage output.

**Measured earlier:**
- A fresh Scavenger against a Tier 1 Raider gang of 4 dies about 99% of the time even in hard cover
  (each Raider fires 2 shots a phase). With all Batch 69 tools together: 68% wins, 4% deaths; against 6:
  27% wins, 32% deaths. Any single tool alone barely helps. (The old cloud session's probe files are gone.)

**Not yet checked in Unity:** everything in Batch 70 (see its "For Yan to check" list), and from Batch 69
the companion (hire row at Yefim, following, fighting, crossing zones, save/load), takedowns, the
surprise round, the bonfire animation in game.

## The map overhaul (Batch 70 - built; was approved 2026-10-10)

Yan: the maps were too small ("the whole map can be crossed in like 1 minute"), but huge empty maps
are worse. Agreed plan, now built (Tier 1 was redrawn from this plan - the approved overview image was
lost with the old cloud session):
- **One map per tier**, 200x200 tiles, instead of 3 separate zones (Tier 7 stays the Reactor Core alone).
  Old zones are named regions; their names, outposts, landmarks and quests stay.
- **About 25 spots per tier**: 3 big places (30-50 tiles, many rooms), 7-8 medium places that squads
  hold, ~15 small finds that each have a reason (a note pointing to a stash, a sniper nest...).
- **Target: 1 to 3 hours to fully explore Tier 1**, with 5 dungeons (separate ~40x40 maps): Rust Culvert
  cave, flooded bunker (key in the fishing village), smugglers' tunnel under the depot, pumping station
  basement, crypt under the old cemetery. Not measured yet on the real map (the old `tier1_sim.py` is gone).
- **Bonfire Village** (place A, grown from Old Man Yefim's camp): safe zone, Wanderers may walk in,
  Yefim's trader, medic's house, repair bench, job board, bunkhouse (resting heals).

**A-Life for big maps (proposed to Yan, he agreed it "makes sense") - NOT built yet:**
- Every spot is a **post** with an owner (a faction, mutants or nobody) and room for 1-3 squads.
- Each squad has a **home post** and every few game hours picks a task: hold (guards, fire, sleep at
  night), patrol a loop of 2-4 own posts along roads, scavenge a nearby small find, take a weak or empty
  enemy post (at most one attack per faction per day, so it's not all-out war), answer radio calls, or leave.
- Faction leanings: Raiders patrol roads and wait near the highway; Wanderers hold camps and scavenge;
  mutants stay in nests by day and hunt at night.
- **Faction bases** are special posts: squads are born there from the rank deck, cards return there,
  friendly players trade/rest/repair there; they have a home squad and fixed defences and can't be taken.
  Territory grows outward along roads, so cutting a road starves a faction's far posts.
- **Mutant nests** are mutant bases: they breed slowly up to a cap and stay empty a few days after
  being wiped (this answers the old "mutant cap" question).
- Squads within ~40 tiles of the player move and fight for real; farther ones hop along their route and
  fight with the existing dice. (Today every squad on the loaded map is live; that's fine at 4-10 squads.)
- The landmark anchors in `TierMaps.cs` are the natural post list.

## Next steps (in order)

1. **Playtest Batch 69 and Batch 70** with Yan (merge `tier-maps` once he's happy) and fix what comes up.
   Yan reviews the maps from `maps/renders/` first (his rule: maps are shown as renders).
2. **Posts, home posts and squad tasks** in `SquadManager`/`TurnLoop` as described above (spots = posts),
   then measure the time to explore Tier 1 on the real map with the real code.
3. Per-tier polish from Yan's notes on the renders (layouts, which spots are too far apart or too empty).
4. Older items still open: perk rework (proposal in `perks_and_aimed_shots.md`), ambient sounds,
   missing biome passes (labs, panel blocks, the remaining zone composers), prop weathering, honest
   previews, the crack decal, adjacent doors on carved buildings, roof moss, Vorbis audio import,
   where the Alpha Access Keycard comes from, and rewriting `GAME_SYSTEMS.md` (stale after Batch 37).
   `Docs/` (the design docs the code cites as "file 04" etc.) isn't in the git repo - add it if a cloud
   session needs it.

## Working from a cloud session

- Clone `grimmylock/agora-vai-action` (private; the session needs it attached), work on a branch, push it;
  Yan pulls in GitHub Desktop. Never push to `main` without Yan.
- `apt-get install -y dotnet-sdk-8.0` for the harness and `Tools/render/render.sh`; python3 with numpy and
  pillow is there already. `pack.py` writes `__pycache__` (gitignored).
- Renders for Yan: `Tools/render/render.sh <maps>` then `python3 Tools/render/overview.py`.
