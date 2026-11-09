# Putting the mutant animations into the game (plan, 2026-11-09 - nothing of this is done; needs Yan's go)

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
