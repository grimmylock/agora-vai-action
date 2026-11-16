# Elder Gaunt claw key poses (SpriteCook edit mode, 2026-11-16)
Base: `redesign_v1_arms.png` on a 192x192 transparent canvas at offset (20,62) = SpriteCook asset `1802ce14-5ea7-4255-a430-4a9b4f05381e` (`elder_base_pad192.png`).
Model gemini-nano-banana-2.1, 12 credits per edit, smart_crop off.
| pose | asset | result |
|-|-|-|
| strike, try 1 | `48a2f6ee-03bf-4fb1-8e80-0f0f8d9b6892` (`strike_donor_48a2f6ee.png`) | body kept, but a THIRD arm was added and the two hanging arms were straightened (zigzag lost). The added arm is good: used as the donor |
| strike, try 2 | `d6ed4551-3811-4367-b14b-0f64491b3647` | same failure with a firmer "move, exactly two arms" prompt |
Lesson: edit mode adds limbs, it does not move them, and it re-draws untouched arms straighter. So: ask for an ADDED arm, keep only that arm (`gaunt_donor_arm.py`), erase the base's far arm in code. Credits 126 -> 102 after the two strike edits.
