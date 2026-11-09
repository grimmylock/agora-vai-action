# Quality check: NPC/armor sprites and world sprites (2026-11-09)
Yan: "a quality check on the npc/armor sprites ... i feel like they fell behind ... most world sprites, the art style is fine but a lot of stuff is still bad, like the trees being kinda bugged". Real renders only (`armor_front_sheet.png` = all 180 front sprites at 6x, 15 per faction row; `armor_back_sheet.png`; `trees_zoom.png` = real Bonfire Village render at 6x). Nothing was changed. The 2026-10-14 prop audit (`maps/renders/AUDIT.md`) is still open too.

## Trees - two real bugs, found in the code
1. **Pinstripe spruces - FIXED (preview).** (I first blamed the old `concept.spruce`; that was wrong, it is smooth.) The cause is in `zone_style.big_spruce`: its centre `cx = w / 2` is half a pixel when the width is odd, and `_px` rounds half to even, so every other column of the branches landed on a different row: perfect alternating vertical stripes on the tall spruces with an odd width (2nd panel of `trees_zoom.png`). Fix: `cx = w // 2` (`zone_style.py`, commit 0135894). `trees_before_after.png`: upper row = before, lower row = after, same real Bonfire Village render.
2. **Birch crowns - FIXED (preview).** Every leaf clump was cut by the same straight diagonal light/shade line, and the cuts lined up across the crown into long diagonal streaks. Now each clump has its own light angle (about top-left, jittered) and a rounded cap of light (`zone_style._mass`). A first attempt at a whole new birch (smooth shaded blobs) looked worse than the old speckled foliage and was dropped. The trunk is unchanged: a thin white pole is how a birch looks, so I did not redraw it.
3. Minor: spruces and birches overlap in odd blue/green mixes (the old spruce's colours differ from `big_spruce`).

## NPC / armor sprites (180 fronts, 180 backs, walk frames, 16 pose sets)
1. **Two body widths only (corrected after reading Yan's skill and measuring all 180).** The style is locked as blocky, armless, front view, 16x16 (`agora-vai-pixel-art`): light bodies (jacket, coat, suit, vest) are 6 wide, heavy and exo bodies 8 wide with shoulder pads and one more leg row ("option C"). Measured on `armor_atlas.png`: every sprite is 16 px tall; 83 are 8 px wide (with the outline) and 97 are 10 px wide. So the heavy step exists but is only +2 px, and it is spread unevenly through each faction's 15 tiers (e.g. the Raiders' tier 4 is already wide, while some of their tier 12 are not). Tiers otherwise differ by colour, headgear and a few pixels, so danger is hard to read from the shape.
2. **Fifteen near-twins per faction.** Within a row (e.g. the Liberators in green, the Sentinels in blue) most of the 15 look the same at a glance; the tier ladder is hard to read. Faction colours themselves are readable (rule 3 holds).
3. **No faces, hair or builds.** The visor eyes are identical across hundreds of sprites; heads are a hat colour on a skin dot.
4. Back views are consistent with the fronts and fine in the same sense; the pose sets are built from the same body.
5. **Names (rule 1, needs your call):** several suit names are real gear designations: `Garrison 6B2 Flak Vest`, `6B5 Armor`, `6B23 Assault`, `Gorka Coat`/`Gorka-M`/`Spetsnaz Gorka`, `Ratnik-Z`, `Altyn Assault`, `OZK Chem Suit`, `Strelets Exo`, `NBC Suit`, `HZ-90..100`. `ArtSource/NAME_CHANGES.md` renames only some (Granit-9, Kokon). Rule 1 says no real brands or gun models in anything the player sees.

## Other world sprites (see AUDIT.md of 2026-10-14, still true)
Props that read as nothing (brush, tires, jersey, block, pillar, hatch, pipe, trash, scrap, debris, culvert), wrong scale (container at 48 px, birches/cars/bus about half the concept's size), and props placed where they make no sense (armchairs in fields, sandbags in meadows).

## What I would do, in order
1. Trees: fix the pinstripe spruces (small), then redraw the birches (medium). Real render before/after.
2. Armor: give each tier a silhouette step (4 body sizes: light, medium, heavy, exo) instead of 15 same-size sprites, keeping the faction palettes; needs your art skill (`agora-vai-pixel-art`, on your account, not in the cloud) or references from you.
3. Names: you decide which of the real designations to rename.
4. Then the 2026-10-14 prop list, starting with scale and the 'reads as nothing' group.

## Prop list: what I checked, and a correction (props_flagged_x5.png, 5x, a man at the same scale)
At 5x most of the props flagged in October are readable and were drawn from references (Batches 93-99). Two of Claude's October flags were wrong or are Yan's call: `pipe`/`pipepost` is an OVERHEAD district-heating line you walk under (`props.py` heatpipe), so a bar "floating above the ground" is by design; `container` at 48 px is a placement/footprint matter (resizing it to 107 px would break its footprint and every map that uses it), so scale needs a decision, not a quick edit.
Candidates I would redraw if Yan agrees (they read weakest at game scale): `tires` (dark rubber on dark ground: low contrast), `trash` (black blob), `jersey` (grey lump), `brush` (a tiny tuft), `debris`. Nothing changed yet; Yan picks keep / redraw / remove.
