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
print("walk", walk[0].size, "slam", slam[0].size, "lowest", low, "contact ms", sum(ms[:6]), "total", sum(ms))
