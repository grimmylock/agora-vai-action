# The Verger's beast: Pixel Lab clips (2026-11-20)
Source: `../boss_concepts/ai/poses/stand.png` (Yan's generated 192x190 stand), on a 256x232 canvas (the helper `ArtSource/pixellab_anim.py`, bottom-centre, 6 px margin). Pixel Lab `animate-pixminimax`, `enhance_prompt` on. **frame_count must be a multiple of 4** (the API returns that many + the first frame); **one job at a time** (429 otherwise); a clip costs about 2 of the 40 trial generations (6.5 left after these).
- `slam/` 13 frames, seed 11: rears back, both arms up, crashes down in a ground slam (frame 4 has a red shockwave ring), holds crouched (5-9), heaves back up (10-12). The Smash. GOOD.
- `walk/` 9 frames (8 + the first repeated), seed 83, loop (last_frame = first_frame): a heavy lumbering step cycle. Made by accident with a placeholder prompt ("walk test") and it came out fine. GOOD.
- `roar/` 13 frames, seed 9: rears to full height, head thrown back, jaw wide, arms flung out, holds, settles. The CHANGE (the transformation window). GOOD.
- `charge/` 9 frames, seed 47: crouches, explodes forward with arms reaching, coat streaming (frames 5-7). Some tan blotches on frames 2, 3 and 6 (artifacts). OK.
- `rake/` 13 frames, seed 31: wind-up, a slash smear on frame 5, then it turns to face the camera for the second swipe. Direction is off (it should keep facing right). WEAK.
- `sweep/` 13 frames, seed 59: wind-up, a wide smear on frame 5 swinging left, turns toward the camera. Direction is off. WEAK.
Not generated (budget): idle (the first walk frame stands in), hurt, death (the corpse is the man again).
