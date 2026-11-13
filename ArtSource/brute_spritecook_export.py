# Blight Brute: put the SpriteCook animations Yan approved into the strips the game loads (2026-11-13). RUN AFTER brute_build.py (which makes the pose-based placeholders this replaces).
#   walk  = brute_spritecook/walk1_frame0..7.png (8 frames, 125 ms each = MutantWalk.FrameSeconds)  -> mutants_anim/brute/frame0..7.png (export_mutant_walk.py reads them)
#   slam  = brute_spritecook/slam_v15_frame0..11.png ("looks great"), custom timings, the fist lands on frame 6 (the first hunched frame) -> blight_brute_slam_strip.png + brute_attacks.json
# Then: python3 ArtSource/export_mutant_walk.py blight_brute ArtSource/mutants_anim/brute ; python3 ArtSource/export_mutant_attacks.py
import os, json
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); SC = os.path.join(HERE, "mutants_anim", "brute_spritecook"); OUT = os.path.join(HERE, "mutants_anim", "brute")
def load(prefix, n): return [Image.open(os.path.join(SC, f"{prefix}{i}.png")).convert("RGBA") for i in range(n)]
def lowest(frames): return max(int(np.where(np.array(f)[..., 3].any(1))[0].max()) for f in frames)
def strip(frames, path):
    W, H = frames[0].size; s = Image.new("RGBA", (W * len(frames), H), (0, 0, 0, 0))
    for i, f in enumerate(frames): s.alpha_composite(f, (i * W, 0))
    s.save(path)
walk = load("walk1_frame", 8)
for i, f in enumerate(walk): f.save(os.path.join(OUT, f"frame{i}.png"))
slam = load("slam_v15_frame", 12); low = lowest(slam); slam = [f.crop((0, 0, f.width, low + 1)) for f in slam]       # feet (the lowest row of the cycle) on the last row, like every strip
strip(slam, os.path.join(OUT, "blight_brute_slam_strip.png"))
ms = [120, 110, 110, 120, 130, 170, 60, 130, 170, 150, 130, 110]                                         # wind-up 760 ms, the fist lands on frame 6, held low, heave back
meta = json.load(open(os.path.join(OUT, "brute_attacks.json")))
meta["blight_brute"]["slam"] = {"frame": [slam[0].width, slam[0].height], "ms": ms, "travel": [0.0] * len(slam)}
json.dump(meta, open(os.path.join(OUT, "brute_attacks.json"), "w"), indent=1)
# throw: the SpriteCook pantomime (empty hand) with the rock BAKED into the frames where the big hand is raised (6-9), so it flips with the sprite; 24 empty rows added on top for it. The rock leaves on frame 10.
tp = os.path.join(HERE, "mutants_anim", "brute_spritecook")
thr = [Image.open(os.path.join(tp, f"throw_frame{i}.png")).convert("RGBA") for i in range(12)]
PAD = 24; _rs = Image.open(os.path.join(OUT, "fx", "brute_boulder_strip.png")).convert("RGBA"); rock = _rs.crop((0, 0, _rs.width // 6, _rs.height)); HAND = {6: (14, 9), 7: (12, 5), 8: (12, 5), 9: (12, 5)}     # the top of the raised open hand (measured per frame)
DIRT = [(96, 74, 50), (70, 56, 42), (120, 104, 80), (62, 58, 54)]
def debris(g, i, fx, fy):
    """dirt and pebbles thrown up where the big fist grips the ground (frames 3-5): rising and spreading, in ground colours"""
    rng = np.random.RandomState(40 + i); n = 4 + 3 * (i - 2)
    for k in range(n):
        up = (i - 2) * 3.0 + rng.uniform(0, 5); x = int(fx + rng.uniform(-9, 9) * (0.6 + 0.3 * (i - 2))); y = int(fy - up - rng.uniform(0, 4) + PAD); s_ = 1 + int(rng.rand() < 0.4); col = DIRT[k % 4]
        for dx in range(s_):
            for dy in range(s_):
                if 0 <= x + dx < g.width and 0 <= y + dy < g.height: g.putpixel((x + dx, y + dy), (*col, 255))
FIST = (11, 60)                                                          # where the big fist grips the ground in frames 3-5 (px in the original frame)
frames = []
for i, f in enumerate(thr):
    g = Image.new("RGBA", (f.width, f.height + PAD), (0, 0, 0, 0)); g.alpha_composite(f, (0, PAD))
    if i == 4:                                                           # the stone's top shows behind the fist, half buried
        under = Image.new("RGBA", g.size, (0, 0, 0, 0)); under.alpha_composite(rock, (FIST[0] - rock.width // 2, FIST[1] + PAD - 6 - rock.height // 2)); under.alpha_composite(g); g = under
    if i == 5: g.alpha_composite(rock, (FIST[0] - rock.width // 2 + 1, FIST[1] + PAD - 12 - rock.height // 2))     # lifted on the fist
    if i in (3, 4, 5): debris(g, i, *FIST)
    if i in HAND: hx, hy = HAND[i]; g.alpha_composite(rock, (hx + 1 - rock.width // 2, hy + PAD - 7 - rock.height // 2))
    frames.append(g)
low2 = max(int(np.where(np.array(f)[..., 3].any(1))[0].max()) for f in thr) + PAD; frames = [f.crop((0, 0, f.width, low2 + 1)) for f in frames]
strip(frames, os.path.join(OUT, "blight_brute_throw_strip.png"))
msT = [90, 90, 90, 90, 90, 100, 90, 110, 140, 170, 80, 220]; REL = (30, 6 + PAD)                       # the rock leaves from above the head, in front of the raised arm
muz = [list(HAND.get(i, (32, 20 + PAD)) if i not in HAND else (HAND[i][0] + 1, HAND[i][1] + PAD - 7)) for i in range(12)]; muz[10] = list(REL); muz[11] = list(REL)
meta["blight_brute"]["throw"] = {"frame": [frames[0].width, frames[0].height], "ms": msT, "travel": [0.0] * 12, "muzzle": muz}
json.dump(meta, open(os.path.join(OUT, "brute_attacks.json"), "w"), indent=1)
print("throw", frames[0].size, "contact ms", sum(msT[:10]), "total", sum(msT))
print("walk", walk[0].size, "slam", slam[0].size, "lowest", low, "contact ms", sum(ms[:6]), "total", sum(ms))
