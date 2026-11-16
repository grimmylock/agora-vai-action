# Elder Gaunt claw key poses (SpriteCook edit mode, 2026-11-16)
Base: `redesign_v1_arms.png` on a 192x192 transparent canvas at offset (20,62) = SpriteCook asset `1802ce14-5ea7-4255-a430-4a9b4f05381e` (`elder_base_pad192.png`).
Model gemini-nano-banana-2.1, 12 credits per edit, smart_crop off.
| pose | asset | result |
|-|-|-|
| strike, try 1 | `48a2f6ee-03bf-4fb1-8e80-0f0f8d9b6892` (`strike_donor_48a2f6ee.png`) | body kept, but a THIRD arm was added and the two hanging arms were straightened (zigzag lost). The added arm is good: used as the donor |
| strike, try 2 | `d6ed4551-3811-4367-b14b-0f64491b3647` | same failure with a firmer "move, exactly two arms" prompt |
Lesson: edit mode adds limbs, it does not move them, and it re-draws untouched arms straighter. So: ask for an ADDED arm, keep only that arm (`gaunt_donor_arm.py`), erase the base's far arm in code. Credits 126 -> 102 after the two strike edits.

## Elder v3 (Yan, 2026-11-16: "version 2 with the 3 arms looks kinda cool, remove the zigzag elbows and use that one")
Version 2 = strike try 1 above. Its body with the thrown arm erased is `elder3_base_b3.png` (192x192, uploaded as SpriteCook asset `ee3314dc-f20a-4c70-a069-32edd0b400a8`). Third-arm donors, each an ADD-an-arm edit of that body (12 credits, nano-banana-2.1):
| pose | asset | file |
|-|-|-|
| rest: a mantis fold under the jaw | `e1e7f873-b96e-459a-b372-d30ab979afec` | `fold_donor_e1e7f873.png` (shift -1,0 onto the body) |
| wind-up: cocked up over the back like a scorpion's tail | `cd799152-533a-4dbc-8c33-83fc30f39460` | `wind_donor_cd799152.png` (shift -3,+1) |
| strike: thrown forward, hand wide open | `48a2f6ee-03bf-4fb1-8e80-0f0f8d9b6892` | `strike_donor_48a2f6ee.png` (shift 0,0) |
Walk: job `d5701d27` (asset `0e0f188a`, animated the three-armed rest pose: debris and a mangled third arm in half the frames, DISCARDED) and job `f19ad1e9` (asset `13fce2c0`, animated the two-armed body `47cf40fc`: clean; the folded arm is pasted on in code). Uploaded sources: `c8449d66` (rest pose, tight), `47cf40fc` (body, tight).
Credits: 126 at the start of the session, 38 after the walk (edits 12 x 5, walk jobs 20 x 2).
