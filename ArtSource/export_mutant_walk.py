# Exports a mutant's walk frames to the game: one PNG strip (8 frames in a row), saved with the .bytes extension (the game decodes it itself, like world_atlas.bytes, so no import
# settings can blur it). Every frame is cropped so the lowest foot of the cycle sits on the last row: the game's HD mutant sprites have their feet on the bottom row and a pivot 16 px above it.
#   python3 ArtSource/export_mutant_walk.py <key> <frames dir> [frame name pattern]     e.g.  pallid_prowler ArtSource/mutants_anim/pallid_online
import sys, os, numpy as np
from PIL import Image
key, src = sys.argv[1], sys.argv[2]; pat = sys.argv[3] if len(sys.argv) > 3 else "frame{}.png"
fr = [Image.open(os.path.join(src, pat.format(i))).convert("RGBA") for i in range(8)]
W, H = fr[0].size
low = max(int(np.where(np.array(f)[..., 3].any(axis=1))[0].max()) for f in fr)
fr = [f.crop((0, 0, W, low + 1)) for f in fr]; H = low + 1
strip = Image.new("RGBA", (W * 8, H), (0, 0, 0, 0))
for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
out = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Sprites", "Mutants", "anim"); os.makedirs(out, exist_ok=True)
tmp = os.path.join(out, f"{key}_walk.png"); strip.save(tmp); os.replace(tmp, os.path.join(out, f"{key}_walk.bytes"))
print(key, "frame", W, "x", H, "strip", strip.size, "->", os.path.join(out, f"{key}_walk.bytes"))
