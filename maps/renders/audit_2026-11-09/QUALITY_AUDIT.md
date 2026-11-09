# Quality check: NPC/armor sprites and world sprites (2026-11-09)
Yan: "a quality check on the npc/armor sprites ... i feel like they fell behind ... most world sprites, the art style is fine but a lot of stuff is still bad, like the trees being kinda bugged". Real renders only (`armor_front_sheet.png` = all 180 front sprites at 6x, 15 per faction row; `armor_back_sheet.png`; `trees_zoom.png` = real Bonfire Village render at 6x). Nothing was changed. The 2026-10-14 prop audit (`maps/renders/AUDIT.md`) is still open too.

## Trees - two real bugs, found in the code
1. **Pinstripe spruces.** 2 of the 8 spruce variants are the OLD generator `concept.spruce` (`ArtSource/concept/concept.py:164`), the other 6 are `zone_style.big_spruce`; `props.py:1136` picks by seed (`(s - 1000) // 77 < 6`). The old one draws each branch as a 1 px stroke per row, so the same x repeats down the rows: the tree is covered in vertical hairlines (2nd and 4th panels of `trees_zoom.png`: the narrow dark spruces), unlike the others. Fix: drop the old two (make all 8 use `big_spruce` with other seeds), or redraw them.
2. **Lollipop birches.** The canopy floats on a thin white 3 px pole, with a gap-like join and diagonal hatch streaks across the foliage (3rd/4th panels). The pole is much thinner than the crown is wide, so it reads as a stick under a bush. Needs a trunk that widens at the base, branches into the crown, and a crown shaded as clumps, not diagonal strokes.
3. Minor: spruces and birches overlap in odd blue/green mixes (the old spruce's colours differ from `big_spruce`).

## NPC / armor sprites (180 fronts, 180 backs, walk frames, 16 pose sets)
1. **One body for everyone.** Every suit is the same 16x16 template (same head, shoulders, legs); tiers differ by colour, a hat or mask and a few pixels. A juggernaut or an exoskeleton has the same silhouette as a windbreaker, so you cannot read how dangerous someone is from the shape.
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
