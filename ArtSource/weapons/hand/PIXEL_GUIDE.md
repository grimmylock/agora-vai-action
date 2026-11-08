# Weapon sprites by hand: how (RT 7v, the melee set Yan approved)

Use this for every weapon redraw, guns included (ACTION_DESIGN "Later list": redraw every gun the melee way).
Worked example: `melee_px.py` (11 melee weapons, approved: "they all look great").

## Why not shrink the reference

Yan's references are pixel art 140-240 px long. A menu sprite has ~60 px for the whole weapon and an in-hand sprite 15-30, so each
pixel you keep stands for 4-15 of theirs. A computer can't decide what survives:
- Shrinking (`melee_set.py`, deleted, in git history) turned everything to mush.
- Building weapons from guessed polygons (`melee_drawn.py`, deleted) gave wrong proportions, jagged edges, gaps, and menu and
  in-hand versions that didn't match.

Yan rejected both. A person has to choose which details survive at each size.

## The method

1. **Measure, don't guess.** Turn the reference to the game's way round (handle or stock left, muzzle or head right, edge down)
   and trace it at the target length:
   - `python3 pixel_tools/grid.py <ref> <length> <flip 0/1> out.png`: the reference on a pixel grid with numbered rows and
     columns, to read off coordinates.
   - `python3 pixel_tools/tmat.py <ref> <length> <flip> "k=#outline,w=#wood,s=#steel,..."`: which material each pixel is (the
     most common one under it), printed as a letter grid. It gives silhouette and proportions; it's noisy at the edges, so clean
     it by eye.
2. **Draw the menu sprite first** (64x32 canvas, the weapon ~58-60 px long, or as tall as fits). Shade per material with a 4-5
   colour ramp picked from the reference: light from the top-left; top edge light, bottom edge darkest, flat middle tone.
   Long round parts (handles, barrels): rows light -> highlight -> mid -> dark -> darkest. Then add the details that make it
   *that* weapon by hand: rivets, wraps, teeth, glints, bevel lines, holes.
3. **Make the in-hand sprite from the menu one** (32x16 canvas). Same features, fewer pixels: keep each distinctive part (red
   handle, knuckle rings, curve, step in the spine) even if it's 1-2 px. Yan checks that the two read as the same weapon.
4. **Store each sprite as a typed grid** (one letter per pixel, `.` empty, `_` a see-through hole with no outline) plus a palette,
   in the weapon's script. `paint()` adds the `#141617` outline round it and centres it on the canvas. Drafting helpers
   (`pixel_tools/bench.py`: `G.col/span/row/set`, `show()` previews into `ArtSource/_out/draft_<name>.png`) may write the first grid;
   the typed grid in the script is the source after that. `pixel_tools/example_*.py` are two real drafts.
5. **Review sheet for Yan**: reference | menu x6 | in hand x6 | size check next to an in-game weapon and a character. Say that
   the size check is a side-by-side, not a game render. Send two weapons first, then the rest once he likes the look.
6. **Install only after approval** (`--install`), then fix the grip points (`WeaponArt.Grips` in `HeldWeapon.cs`; y counts from
   the top; the old sprites were cropped, the new ones are centred on the full canvas) and run the harness.

## What Yan caught (avoid these)

- **Proportions from the reference, not from memory.** The first hammer's head was twice too big. The sledge head should be a
  bit smaller than the reference's ("too big", "shaped like a brick").
- **Symmetric round parts.** A taper must grow on both sides at once: the bat that grew on top first was "one side bigger".
- **Don't stretch the thin part.** Bat handles too long "look like that other bat from that other sport".
- **Keep the signature shape.** The machete's curve, the kris waves, the trench knife's clip point. At in-hand size give a curve
  enough length to read: too short and it looks "crooked".
- **Same colours in both sizes.** The switchblade's in-hand handle lost its red and "looks nothing like" the menu one.
- **No bumps that aren't on the reference.** The hatchet's handle swell near the head read as "a weird protrusion".
- **Thin features need room.** Holes under 3 px become solid outline; use `_` holes and shade the ring round them.

## Guns (not done yet): extra parts the animations need

- Grip and support-hand points, plus the muzzle (`WeaponArt.Grips`).
- Magazine cuts (`ArtSource/weapons/mags.py` -> `WeaponMags.cs`; the `_mag` and `_body` sprites).
- Break-action barrels and revolver cylinders (`_barrels`).

Draw the gun whole, then cut those parts from the typed grid, so they always line up.
