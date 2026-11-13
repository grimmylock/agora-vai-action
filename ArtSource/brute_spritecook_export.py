# Blight Brute: put the SpriteCook animations Yan approved into the strips the game loads (2026-11-13). RUN AFTER brute_build.py (which makes the pose-based placeholders this replaces).
#   walk  = brute_spritecook/walk1_frame0..7.png (8 frames, 125 ms each = MutantWalk.FrameSeconds)  -> mutants_anim/brute/frame0..7.png (export_mutant_walk.py reads them)
#   slam  = brute_spritecook/slam_v15_frame0..11.png ("looks great"), custom timings, the fist lands on frame 6 (the first hunched frame) -> blight_brute_slam_strip.png + brute_attacks.json
# Then: python3 ArtSource/export_mutant_walk.py blight_brute ArtSource/mutants_anim/brute ; python3 ArtSource/export_mutant_attacks.py
import os, json
import numpy as np
from PIL import Image, ImageDraw
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
# throw: the SpriteCook pantomime (empty hand). The stone is torn OUT OF THE GROUND (Yan: it "just spawns there"): it rises behind the big fist with everything under the ground line cut away, so it is revealed row by
# row as he pulls; a hole, flung dirt and pebbles at the spot; extra pull frames (base frames 3 and 4 repeated) make it slow and heavy; then it sits on the fist (frame 5) and rides the raised palm (6-9). 24 empty rows on top for the raised stone.
tp = os.path.join(HERE, "mutants_anim", "brute_spritecook")
thr = [Image.open(os.path.join(tp, f"throw_frame{i}.png")).convert("RGBA") for i in range(12)]
PAD = 24; _rs = Image.open(os.path.join(OUT, "fx", "brute_boulder_strip.png")).convert("RGBA"); rock = _rs.crop((0, 0, _rs.width // 6, _rs.height)); RW, RH = rock.size
HAND = {6: (14, 9), 7: (12, 5), 8: (12, 5), 9: (12, 5)}                    # the top of the raised open hand, per base frame (measured)
DIRT = [(96, 74, 50), (70, 56, 42), (120, 104, 80), (62, 58, 54)]
FIST = (11, 60); RX = 17; GY = 63 + PAD                                             # where the big fist grips the ground, and the ground line the stone is cut at (frame rows + PAD)
SEQ = [(0, None), (1, None), (2, None), (3, 0.12), (3, 0.32), (4, 0.55), (4, 0.78), (5, 1.0), (6, None), (7, None), (8, None), (9, None), (10, None), (11, None)]       # (base frame, how far out of the ground)
msT = [90, 90, 90, 80, 80, 80, 80, 100, 90, 110, 140, 170, 80, 220]; REL = (30, 6 + PAD); CONTACT_POS = 12
def debris(g, seed, r):
    rng = np.random.RandomState(60 + seed); n = 5 + int(10 * r)
    for k in range(n):
        up = rng.uniform(0, 4 + 14 * r); x = int(RX + rng.uniform(-11, 11) * (0.5 + r)); y = int(GY - 2 - up); sz = 1 + int(rng.rand() < 0.45); col = DIRT[k % 4]
        for dx in range(sz):
            for dy in range(sz):
                if 0 <= x + dx < g.width and 0 <= y + dy < g.height: g.putpixel((x + dx, y + dy), (*col, 255))
def hole(g):
    d = ImageDraw.Draw(g); d.ellipse([RX - 9, GY - 2, RX + 9, GY + 2], fill=(46, 36, 28, 255)); d.ellipse([RX - 7, GY - 1, RX + 7, GY + 1], fill=(28, 22, 18, 255))
frames = []; muz = []
for pos, (bi, r) in enumerate(SEQ):
    f = thr[bi]; g = Image.new("RGBA", (f.width, f.height + PAD), (0, 0, 0, 0)); g.alpha_composite(f, (0, PAD))
    if r is not None:
        under = Image.new("RGBA", g.size, (0, 0, 0, 0)); hole(under)
        top = int((GY - 1) + (1.0 - r) * (RH + 4) - RH) - (8 if r >= 1.0 else 0)                                   # top row of the stone: from fully under the ground (r 0) up to lifted clear of it (r 1)
        layer = Image.new("RGBA", g.size, (0, 0, 0, 0)); layer.alpha_composite(rock, (RX - RW // 2, top))
        if r < 1.0: la = np.array(layer); la[GY:, :, 3] = 0; layer = Image.fromarray(la)                              # everything under the ground line is cut: the stone is revealed row by row as it rises
        under.alpha_composite(g); under.alpha_composite(layer); g = under                                           # hole under him, the stone in front
        debris(g, pos, r)
    if bi in HAND: hx, hy = HAND[bi]; g.alpha_composite(rock, (hx + 1 - RW // 2, hy + PAD - 7 - RH // 2))
    frames.append(g); muz.append(list((HAND[bi][0] + 1, HAND[bi][1] + PAD - 7)) if bi in HAND else list(REL) if bi >= 10 else [FIST[0], GY])
low2 = max(int(np.where(np.array(f)[..., 3].any(1))[0].max()) for f in frames); frames = [f.crop((0, 0, f.width, low2 + 1)) for f in frames]
strip(frames, os.path.join(OUT, "blight_brute_throw_strip.png"))
meta["blight_brute"]["throw"] = {"frame": [frames[0].width, frames[0].height], "ms": msT, "travel": [0.0] * len(frames), "muzzle": muz}
json.dump(meta, open(os.path.join(OUT, "brute_attacks.json"), "w"), indent=1)
print("throw", frames[0].size, len(frames), "frames, contact ms", sum(msT[:CONTACT_POS]), "total", sum(msT))
print("walk", walk[0].size, "slam", slam[0].size, "lowest", low, "contact ms", sum(ms[:6]), "total", sum(ms))
