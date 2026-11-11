# Lock-and-key redo of every dungeon: the program (started 2026-11-10)

Yan: apply the Secret Lab Alpha method (WORK_LOG "Secret Lab Alpha redesigned") to ALL dungeons and separate levels. Method per dungeon, in this order:
1. **Graph** (`<id>.mmd` in this folder): rooms, locks, keys, shortcuts, goal. Checked by `graph_check.py` of the level-design skill (`python3 -I scripts/graph_check.py <file>`): goal reachable, every node reachable, no softlock, loops present, shortcuts only loops. **DONE for all 19 below (this batch), all PASS.**
2. **Plan**: hand-plan the floor as a text grid (a `<id>_plan()` in `lab_plans.py` style, `to_cs.py` pastes it into `TierDungeons.LabPlans.cs`), with the plan letters of `LabLocks.cs` (1 badge door, 2 power door, 3 locker, 4 crate, 5 switch, 6 pump, W deep water) and a `<id>_check` that floods the grid with each key set.
3. **Wire** it: `TierDungeons.<Name>()` becomes `FloorPlan(...)`, the key objects go in `LabLocks.Things` (per-dungeon key prefix), the harness gets a flood check on the REAL built map (like `AlphaChecks`).
4. **Render** before/after for Yan, one dungeon per batch, then the next.

## Rules (from Alpha)
Start in the open and show the locks before they can be used; one job per room; a chain, not a pile; boss/goal last; alternate routes (a hole, a shortcut door) are loops, never requirements; keys are never spent; one lock type = one look, a locked door is a wall to everyone until the key is held, and bumping it says what it wants.

## Lock kinds
Existing in code: **badge** (blue card-reader door, plan '1'), **power** (dead hazard door, '2'; the switch '5' needs a fuse '4'), **drain** (deep water until a pump '6' runs). **New, to build once and reuse** (a look + a nag line + a key kind in `LabLocks`): **gate** (iron gate, an iron key), **seal** (a sealed slab / keypad: a code from a note), **rockfall** (a collapse a pry bar lifts), **coolant** (steam-filled door until a valve wheel is turned), **lever** (gates that a lever lifts), **sluice** (a wheel handle).

## The 19 recipes (graph file = `<id>.mmd`; Alpha and Sub-Station Depths are already done)
| Dungeon | Tier | Theme | Locks (key chain) | Loops |
|-|-|-|-|-|
| secret_cave | 1 | cave the smugglers closed | rockfall (pry bar from the camp) | dug tunnel, hole |
| chapel_crypt | 1 | family crypt | gate (priest's cell) + seal (code in the ossuary) | collapsed crypt hole |
| pumping_basement | 1 | flooded pump hall | fuse -> power -> pump -> drain (culvert) | hole |
| flooded_bunker | 1 | story shelter | badge (duty locker) -> fuse -> power; command level half flooded | hatch ladder |
| smugglers_tunnel | 1 | dug tunnel | gate (guard post) | crews' bypass hole |
| crusher_pit | 2 | hydraulic rams | badge (foreman's locker) -> fuse -> power | hole |
| valley_mine | 2 | mine | rockfall (pry from the tool shed) | crawl, vent shaft |
| stranded_ferry | 3 | beached ship | badge (purser) -> fuse -> power (bridge) | hull plate hole |
| camp_shelter | 4 | Cold War shelter | badge -> fuse (filter room) -> power | vent crawl |
| drydock_tunnels | 5 | dock culverts | fuse -> pump -> drain -> sluice handle | service ladder |
| hospital_morgue | 6 | hospital basement | badge -> fuse -> power (records safe) | service hole |
| secret_lab_beta | boss 1 | security lab, the Visor | gate -> seal -> fuse -> power | vent hole, shortcut door |
| secret_lab_gamma | story | vault lab | badge -> fuse -> power -> story key (the eel's vault key) | cable duct |
| lattice_foundry_lower | 5 | foundry | badge -> coolant -> fuse -> power | gantry shortcut |
| siege_catacombs | - | war crypt | gate x2 + rockfall (pry) | sappers' hole, tomb stair |
| maintenance_tunnels | - | utility tunnels | badge -> fuse -> power -> pump -> drain | ladder shaft |
| underground_bunker | - | command bunker | badge -> fuse -> power | duct |
| underground_labyrinth | - | maze of gates | lever 1 -> 2 -> 3 | many |
| deep_caverns | - | natural caves | rockfall (pry) + dam lever (drain) | crawl, chimney |
Not dungeons, so no locks planned: farmhouse_upper, school_upper, flats_2, flats_3 (a building's upper floors; optional locked rooms could come later).

## Status: pumping_basement, secret_cave, smugglers_tunnel, chapel_crypt, valley_mine, crusher_pit flooded_bunker (2026-11-10) stranded_ferry, camp_shelter, drydock_tunnels and hospital_morgue, maintenance_tunnels and underground_bunker, deep_caverns, underground_labyrinth siege_catacombs and lattice_foundry_lower (locks set into the old floor) and the story labs secret_lab_gamma and secret_lab_beta (2026-11-11) BUILT. ALL 19 are built; left: Yan's review of each, his scare-corner marks, a denser labyrinth if he wants one. The three lock kinds that had no art were built on 2026-11-11: keypad door `{` (key kind `code`), steam door `}` (`coolant`), lever gates `<` `-` `/` (NOT digits: the Depths' plans use `7` and `8` as the eel's marks, which the first try broke; portcullis a/b/c, `lever1..3`, levers `lever_a/b/c`); all lock looks and their key kinds are one table, `LabLocks.LockKinds`. Quick check of all of them on the real maps: `bash Tools/compile_check/compile_only.sh && (cd Tools/compile_check && dotnet /tmp/test.dll dungeons)`. Scare corners (DungeonLairs.cs) for the later dungeons: Yan will say where, after the base layouts.

## Order proposed (small and self-contained first, boss labs last)
pumping_basement, secret_cave, smugglers_tunnel, chapel_crypt, valley_mine, crusher_pit, flooded_bunker (story: check Tier1Story), stranded_ferry, camp_shelter, drydock_tunnels, hospital_morgue, then the plan-based ones (maintenance_tunnels, underground_bunker, underground_labyrinth, deep_caverns, siege_catacombs, lattice_foundry_lower), the story labs last (Beta with Boss 1, Gamma with the Sealed Vault).
Caveat on the graphs: a "hole" or "shortcut" edge that touches a room behind a lock makes that lock avoidable on that route by design (as Alpha's hole is); the graph checker only proves it is never *required*.

## Plan-drawing rule
New plans use plain rectangular rooms (`Plan.room`), never `Plan.jag`: bitten corners and alcoves make one-tile staircase walls the wall atlas draws as odd dark blocks (found on the Pumping Basement, 2026-11-10). Walls between rooms may be any thickness: built walls (concrete, metal, hull, plaster) always draw as a plain slab top.

## Plan characters
Capital letters are landmark marks ONLY if the dungeon's `marks` names them, and `N`, `W`, `Z`, `D` and `S`... are taken by StampPlan (`N` = deep water: the bunker's Generator Room mark was silently a deep-water tile until 2026-11-11). Every lowercase letter is a prop. New prop tiles: `?` wreck, `>` armchair, `[` engine block, `]` tank. New lock tiles use SYMBOLS (`@` rockfall, `$` iron gate), never letters: StampPlan reads a letter as a landmark mark in older plans (the foundry uses R and F).

## Direction: free (Yan, 2026-11-10)
Yan first suggested top to bottom instead of left to right, then took it back: "not all need to be top to bottom, forget that". So there is NO rule on direction; each dungeon runs the way its theme wants. The one lesson kept: a lock (gate, slab, door) is drawn from the FRONT, so put it across a north-south leg of the tunnel (a gate across an east-west tunnel is seen edge-on and looks wrong). The Pumping Basement and the Secret Cave stay as built.
