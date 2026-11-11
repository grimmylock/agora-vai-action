# Putting the mutant animations into the game (plan, 2026-11-09; the WALK of the Pallid and Veil Prowler is IN since 2026-11-11, see the end; the attacks are not)

What exists: strips + `*_anims.json` (frame size, ms per frame, `travel` 0..1 for pounces) for the four canines, the Brute, and the four new humanoid concepts (sprites only drawn after his review).

How mutants are drawn today (cited so the plan is checkable):
- One static sprite per mutant, `UnitSprites.HD(name)` (`UnitSprites.cs:94-104`), pivot (0.5, 16 px / height), 2x pixel density; flipped toward the target in `ApplyUnitArt` (`TacticalGridView.cs:1171-1190`). No frames at all.
- Movement is tile steps (`RtAi.TickMutant`, `RtAi.cs:51-68`) drawn as slides (`Rt.Glide`, `Rt.cs:39`).
- A strike is INSTANT: `m.Attack(p)` every ~1 s (`RtAi.cs:64-67`), the Dread Prowler's leap too (`EnemyAI.LeapAttack`, `EnemyAI.cs:488-626`, which the real-time branch only reaches through `HumanTurn/MutantTurn`).

What it takes:
1. **Load**: copy the strips to `Assets/Resources/Sprites/Mutants/anim/` (point filter, no compression, like the HD sprites' .meta), add `UnitSprites.HDAnim(name, anim, frame)` slicing each strip by `frame` size; pivot y = (feet px from the canvas bottom + 16) / height (the JSON should carry `ground`; add it to `make_canine_anims.py`: the frame has 6 px under the original sprite).
2. **State**: a small per-mutant `MutantAnim` (Mutant -> state, start time): `run` while its tile changes (`Rt.Glide` already knows), `bite` when `Attack` fires within ~1.5 tiles, `pounce` when the leap fires or a mutant with `Leap` closes from >= 3 tiles; idle = the static sprite. `ApplyUnitArt` picks the frame; everything falls back to the static sprite when a strip is missing.
3. **The one gameplay decision**: the strikes land instantly today. The animations have a wind-up and a snap frame (bite: frame 5 at ~0.4 s, pounce: frame 7 at ~0.9 s + the leap's travel). Either delay the damage to the snap frame (a fairness gain for the player: a readable tell; the Brute's swipe and the eel already work this way) or play the animation after the hit. I recommend delaying (never a nerf: the same damage, but dodgeable).
4. **Pounce movement**: the strip's `travel` column says how far across the jump it is; `Rt.SetOffset`/`Slide` can carry the unit between tiles over the 0.6 s of the leap.
5. **Co-op**: the state is host-side; `NetWorld` mirrors would need the current anim name + start time per enemy (a byte + a short), or clients can derive `run`/idle from movement and play attacks from the existing shot/FX events.
6. **Check**: the harness can cover the state machine and the damage timing; the look needs Unity.


## Done 2026-11-11: the walk of the Pallid Prowler and the Veil Prowler
Yan: "the walking animation for those 2 are fine, put them in".
- Strips: `Assets/Resources/Sprites/Mutants/anim/<key>_walk.bytes` (8 frames in a row, a PNG with the .bytes extension, built by `ArtSource/export_mutant_walk.py`; decoded in code so no import setting can blur it), `UnitSprites.HDWalk(name)` slices them (32 px a tile, feet on the bottom row, pivot 16 px above: the same as the stand-still HD sprite).
- State: `MutantWalk.Frame` (new): a mutant walks while `Rt.Sliding` (Rt.Glide is sliding its body to catch up with a tile jump), held for 0.25 s so the gap between two tile steps does not flicker; the picture follows the clock from when the walk began, `FrameSeconds` = 0.125 (the animator's own speed). Standing, dead, or turn-based: the stand-still sprite. `TacticalGridView.ApplyUnitArt` picks the frame; flipping toward the target and the Veil's cloak shimmer are unchanged.
- Co-op: nothing is networked; a client's mirror enemies jump tiles from the snapshot and Glide slides them the same way, so the walk plays there too (not tried over a connection).
- Blind Maw walk added the same day (`blind_maw_walk.bytes`, 60x37 frames, from `ArtSource/maw_hybrid.py`: Yan's body kept, four stubby legs drawn fresh, hooked claws); standing sprite unchanged.
- Not done: bite, pounce (the strikes are still instant), Dread Prowler (scrapped for now, sprite is mid-leap), the Brute, the humanoids. The feet skate: a Pallid crosses the ground at about 2.4 tiles a second (`RtAi.Speed`), the strip was drawn for about a quarter of that.
