# Gemini prompts for building layouts (Yan pastes one, sends me the image, I rebuild it in the game)

How it works: you paste a prompt into Gemini, send me the picture and say which building and which map spot it is for. I read the walls, doors, windows, rooms and furniture, redraw it as a plan with our tiles, floors and props, run the reachability check and show you the render. The picture is only a blueprint; its colours and furniture style are not used.

## Base prompt (fill in the part in [brackets])
```
A flat top-down architectural floor plan of [BUILDING, e.g. "a small abandoned roadside pharmacy"], seen straight from above with no perspective, no 3D, no shadows.
Draw it on a visible square grid where every square is 1 metre, and write the width and height in squares under the plan (about [W] x [H] squares).
Thick dark walls, gaps for doors (draw each door as a short arc or a break in the wall), small rectangles for windows.
Label every room in plain text (for example: Waiting room, Counter, Storage, Toilet).
Show furniture as simple labelled top-down shapes (bed, table, chair, counter, shelf, sofa, stove, sink, toilet, bath, desk, locker, crate), not decorated.
Rooms of different sizes, a believable circulation: every room reachable from the entrance, one main entrance and one back door. Plain white background, no text except the labels.
```

## What the game actually has (audit 2026-11-27, `BUILDINGLIST=<file>` in the harness lists every enterable building: map, size, wall, nearest landmark)
142 enterable buildings on six maps. Sizes are the outer wall box in tiles (a tile is about 1 m in the prompt's grid: ask Gemini for the INNER size, outer minus 2). "Plan exists" = I already wrote a furnished plan for it; the rest are bare huts or procedural blocks with no rooms.

| # | Type (what it is in game) | Where, how many, outer size | Plan exists? | Fill for [BUILDING] and size to ask for |
|-|-|-|-|-|
| 1 | One-room cabin (camp, hut, shack) | Lakeside Camp x8 and Lifeguard Tower x2 (6x5, plank); Old Summer Camp x10 (7x6, plank); Logging Camp x3 (4-5 sq); Hunter's Hide, Hunter's Shack, Hermit's Shack, Boathouse, Forester's Hut, Fisherman's Shack (5-6 x 4-6) | no | "a one-room forest cabin with a bunk bed, a wood stove, a table with two stools and shelves, one door and two windows" (4x3 to 5x4 inside) |
| 2 | Two-room cabin | the bigger camp cabins and Track Workers' Hut, Signal Box, Radio Mast, Loading Office, Rail Depot (6-7 x 5-6, concrete/wall) | no | "a small two-room hut: a front room with a desk and a stove, a back room with a bunk, and a lean-to storage corner" (6x4 inside) |
| 3 | Row of shops (Main Street, 2 blocks, each 69x6 outer, carved into rooms by the game) | Ghost Town Main Street, ~8 shopfronts a block | procedural | "one side of a high street: eight narrow shopfronts side by side, each with a shop floor, a back room and a door onto the street; vary them: grocer, tailor, barber, cafe, bakery, bookshop, pawnbroker, empty shop" (66x4 inside; or ask for 4 shops at a time, 33x4) |
| 4 | Row houses | Ghost Town Row Houses | procedural | "a terrace of six narrow two-storey houses, ground floor only: a front room, a kitchen at the back, a staircase, a yard door" (7x8 inside each) |
| 5 | Department store | Abandoned City, 12x18 and 12x15 blocks (+ 8x6, 5x4 entrance blocks) | no | "a ruined department store ground floor: a wide sales floor with counters in departments, escalators, a customer service desk, stock rooms and a loading dock at the back" (24x16) |
| 6 | Hospital: ward block and field hospital | Siege Ward Hospital 22x7 + 10x4, Field Hospital Ruins 24x16 (Abandoned City) | no | "a hospital ward block: a long corridor with two six-bed wards, a nurses' station, an operating room and a pharmacy" (20x6 and 22x14 for the field hospital) |
| 7 | Garrison / armoury compound | Garrison Armory 23x5, 16x7, 7x8, 6x4 (Abandoned City); Garrison Barricade huts 12x4 / 12x5 x3 | no | "a small army armoury: a front guard room, a weapons cage with racks, an ammunition store, a briefing room and two barracks rooms" (21x5 and 14x5) |
| 8 | Tram / parking structures | Burned Tram 12x5, Parking Garage 8x10 + 4x6, Evacuation Buses 5x4 | no | "a tram depot hall: two tracks with a tram on each, an inspection pit, a workshop and a small office" (22x10); "a multi-storey car park ground ramp with an attendant booth" (6x8) |
| 9 | Rows of small blocks | Abandoned City 7x5 x4 and 12x5 x2 at y91-101; unnamed 7x7, 10x11, 14x10, 12x5 blocks | no | "a ground floor of a small block of flats: a hall, four one-room flats off a corridor, each with a kitchen nook and a bathroom" (10x4 inside each, 12x5 outer) |
| 10 | Factory halls (4 big blocks 26x12 / 27x12 / 26x13 / 27x13) | Central Plant (Industrial Basin, Scrapyard map) | no (open yard with props) | "a factory hall: a production floor with long machine lines, a loading bay, a foreman's glass office on a mezzanine, a tool crib and a staff canteen" (24x10) |
| 11 | Relay / sub-station buildings | Relay Station 7x5 x2 and 12x5 x2 (Quarry map); Sub-Station House 12x4 + 8x4; Breaker Room 5x4 | no | "an electrical relay station: a switch room with cabinets, a control room with a desk and panels, a battery room and a store" (10x3 and 5x3) |
| 12 | Ship cabins, cannery, shipyard huts | Captain's Row 4-8 cabins 8x6 and a 12x7 hull; Fish Cannery 8x4; Dry Dock 4x7; The Hulks 8x6/9x6 x4 | no | "a captain's cabin on a stranded ship: a bunk, a chart table, a stove, lockers"; "a fish cannery: a gutting table, a canning line, an ice store, a packing room" (6x4 / 6x2 and 36x28 for a whole hulk) |
| 13 | Laundromat, Pharmacy, Petrol station, Truck Stop diner, Church, Town Hall | Ghost Town / Scrapyard | **yes** (batch 9fq, 9fv) | done |
| 14 | School (U with yard, 2 floors) and Flats (T, 3 floors), farmhouse | Ghost Town, Perimeter | authored plans (rooms exist; furniture is thin) | optional later |
| 15 | Bunkhouse, clinic, cabins, bar, workshop, barracks, mess, command post, armoury, station, barn, cowshed, camp huts, fishing houses | village, hub, barracks, perimeter, fishing village | **yes** | done |

Most valuable first (they are the "warehouse" buildings the player meets most): **3 Main Street shops, 1 cabins, 5 department store, 9 small flats, 6 hospital, 2 huts**, then the rest.

## Ready-made fills by type
Use the "Fill" column above. For a combined prompt, paste the base prompt and replace `[BUILDING]` and `[W] x [H]` with the fill and the inner size.

## Tips
- Ask Gemini for several versions and send me the one with the clearest walls; a plan with the grid visible is the easiest to copy exactly.
- Tell me which row of the table it is for and, for rows 1-2, how many variants you want (I can mirror and re-furnish one plan into several).
- Tell me if you want it ruined (collapsed wall, missing roof section).
