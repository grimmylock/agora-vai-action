# Reference library (Batch 104)

Photos to draw from, one folder per prop or building part. **References only**: every sprite is still drawn by hand in
Python/PIL. Each folder has `sheet.jpg` (numbered contact sheet), `credits.txt` (title, author, licence, source link per
photo, all openly licensed via Openverse) and `dropped.txt` (photos removed as off-topic; never fetched again).

- Add photos: `python3 ArtSource/refs/fetch.py <folder> "query" ["query" ...]` (keeps what's there).
- Remove photos: `python3 ArtSource/refs/fetch.py <folder> --drop 3,7` (numbers from the sheet).
- The queries used are in `queries.tsv` and `topup.tsv`.
- Scale: a 16 px person is 1.8 m, so a pixel is about 11 cm.

| Folder | Photos | What to take from it |
|-|-|-|
| `antenna` | 9 | lattice masts and radio towers |
| `armchair` | 11 | dumped armchairs and sofas, torn upholstery |
| `barrel` | 3 | red drums (few good ones) |
| `barrierpole` | 4 | boom barriers: red-white striped arm, counterweight |
| `block` | 8 | from Batch 93, used for the Batch 99 redraws |
| `brush` | 4 | tall dry weeds, uncut grass round buildings |
| `bus` | 7 | abandoned buses: rounded fronts, rust, broken windows |
| `bush` | 3 | hazel and elder: rounded, many thin stems from the ground |
| `busstop` | 4 | Soviet concrete bus stops (sculptural shelters), plain shelters |
| `campfire` | 7 | fires in rings of stones, night light falloff |
| `container` | 6 | rusty shipping containers: ribbed sides, rust runs from the top |
| `crate` | 3 | wooden ammunition-style crates, pallets |
| `culvert` | 3 | from Batch 93, used for the Batch 99 redraws |
| `deadtree` | 4 | bare snags, dead birches standing in swamp water |
| `debris` | 2 | from Batch 93, used for the Batch 99 redraws |
| `desk` | 4 | abandoned office desks under dust and papers |
| `desk_flipped_game` | 16 | Enter the Gungeon's flipped tables (how a top-down game shows a table on each side) |
| `dumpster` | 6 | rusty steel bins, garbage containers by walls |
| `engine` | 8 | rusty engine blocks, tractor engines |
| `fence` | 9 | board fences, picket fences, chain-link |
| `flatcar` | 3 | railway flatcars and old wooden wagons |
| `floorpipe` | 7 | basement and tunnel pipes along walls and floors, steam |
| `fountain` | 12 | from Batch 93, used for the Batch 99 redraws |
| `front_brick` | 5 | brick buildings left to rot, arched doors, ruined walls |
| `front_concrete` | 5 | stained concrete walls and bunker walls |
| `front_door` | 5 | old wooden and rusty steel doors |
| `front_metal` | 11 | corrugated iron sheds: rust, patched panels |
| `front_panel` | 7 | Soviet prefab apartment blocks: panel seams, balconies, rows of windows |
| `front_plank` | 7 | old wooden village houses and log houses, sagging roofs |
| `front_plaster` | 8 | peeling and cracked plaster showing brick and lath |
| `front_window` | 4 | broken and boarded-up windows |
| `hatch` | 4 | from Batch 93, used for the Batch 99 redraws |
| `jersey` | 8 | from Batch 93, used for the Batch 99 redraws |
| `lamp` | 7 | street lamps: concrete and steel posts, old lanterns, night pools of light |
| `lockers` | 8 | rusty steel lockers, rows in changing rooms |
| `logs` | 4 | firewood stacks: round ends, bark on the sides |
| `machine` | 6 | abandoned factory halls, lathes, big flywheels |
| `pillar` | 2 | from Batch 93, used for the Batch 99 redraws |
| `pine` | 4 | spruce: dark, dense, drooping boughs; a broken-off spruce stump |
| `pipe` | 4 | from Batch 93, used for the Batch 99 redraws |
| `pipes` | 5 | factory pipework, rusty large-bore pipes on the ground |
| `pole` | 9 | wooden and concrete utility poles, crossarms, insulators |
| `power_lines` | 7 | rural poles with wires, rows of poles along roads |
| `power_pylon` | 5 | lattice high-voltage towers |
| `redbush` | 6 | red autumn shrubs, barberry |
| `redtree` | 4 | birch and maple in autumn: yellow-orange crowns, leaves on the ground |
| `reeds` | 5 | cattail and bulrush beds, straw-coloured in autumn and winter |
| `rock` | 6 | mossy boulders in birch woods, glacial erratics |
| `roots` | 3 | hanging roots |
| `rubble` | 3 |  |
| `rustbarrel` | 6 | rusty oil drums: two ribs, flaking paint, piles of them |
| `sandbags` | 9 | sandbag walls and bunkers, sagging bags, tarp covers |
| `scrap` | 4 | from Batch 93, used for the Batch 99 redraws |
| `stones` | 6 | stacked stone piles and cairns |
| `sunktruck` | 5 | trucks and cars half under water, rust and green paint |
| `tank` | 3 | storage tanks: rusty cylinders indoors and out |
| `tires` | 4 | from Batch 93, used for the Batch 99 redraws |
| `trash` | 8 | from Batch 93, used for the Batch 99 redraws |
| `tree` | 5 | birches: white bark with black bands, thin crowns; lone birches in fields |
| `watchtower` | 7 | wooden guard towers on legs, enclosed cabins, camp towers |
| `wreck` | 7 | rusting cars and trucks overgrown with grass and trees (a derelict military truck) |

## Buildings by kind (Batch 121, `buildings.tsv`)
| folder | photos | what to take from it |
|-|-|-|
| `bld_farmhouse` | 5 | village houses: painted plank or log walls, carved window frames with shutters, pitched tin/tile roof |
| `bld_barn` | 7 | kolkhoz barns and sheds: stone or plank walls, wide double doors, a brick chimney stack, the main kolkhoz office |
| `bld_cowshed` | 7 | long low cowsheds: rows of posts, open sides, mud-and-pine walls caving in, a corrugated roof |
| `bld_garages` | 5 | garage rows: one door each, flat roofs, mismatched steel doors |
| `bld_warehouse` | 6 | brick warehouses: tall blind walls, rows of small windows, long column-lined halls, barrels in a shed |
| `bld_factory` | 6 | factory halls: saw-tooth and gabled roof lines, brick with tall arched windows, a flour mill |
| `bld_school` | 11 | interiors (gyms, classrooms, kindergarten): for the upper floors |
| `bld_shop` | 6 | small-town shops and two-storey plank houses with a porch and a tin roof |
| `bld_apartment` | 5 | panel blocks: balconies in rows, a flat roof, a porch on the ground floor |
| `bld_hospital` | 12 | hospital and sanatorium interiors: tiled walls, round halls, stairwells |
| `bld_station` | 8 | small railway stations: plaster with a hip roof, a long platform canopy, a rail shed |
| `bld_pumphouse` | 8 | pumping stations and boiler houses: a plain concrete front, long flooded halls, firing gear |
| `bld_church` | 7 | ruined village churches: red brick, a bell tower over the door, domes, chapels inside |
| `bld_hut` | 9 | huts and fishing huts: rusty tin walls, logs, plank shacks on stilts, a lone hut on the shore |
| `bld_bunker` | 9 | bunkers and pillboxes: stained concrete, angled faces, a blue riveted steel door |
| `bld_tower` | 7 | water towers: a steel tank on legs, a round brick tower with a steel tank on top |
