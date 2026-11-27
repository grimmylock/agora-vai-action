#!/usr/bin/env python3
"""Manifest + QA for the Pixel Lab props (ArtSource/world/settle_pl/raw_*.png). Records source and licence per asset (the create-game-assets skill's provenance step)
and flags: no transparent border, near-white fringe pixels (a keyed-out background left halos), too many colours."""
import json, os, sys, glob, datetime
import numpy as np
from PIL import Image
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
SRC = os.path.join(ROOT, "ArtSource", "world", "settle_pl")
EDITED = {"raw_wreck_torn_h.png": "generator's white background keyed out by hand (near-white, low-saturation pixels set transparent)",
          "raw_goodsshelf.png": "goods erased from the generated shelf to make it half empty (raw_goodsshelf_full.png is the original)",
          "raw_canopy.png": "stitched from raw_canopy_orig.png: end pieces plus a middle slice repeated 5 times, pixel for pixel"}
items, problems = [], []
for p in sorted(glob.glob(os.path.join(SRC, "raw_*.png"))):
    name = os.path.basename(p); im = Image.open(p).convert("RGBA"); a = np.array(im)
    alpha = a[..., 3]; corner = [alpha[0, 0], alpha[0, -1], alpha[-1, 0], alpha[-1, -1]]
    rgb = a[..., :3].astype(int); vis = alpha > 0
    fringe = int(((rgb.min(axis=2) > 215) & ((rgb.max(axis=2) - rgb.min(axis=2)) < 25) & vis).sum())
    colours = len(set(map(tuple, rgb[vis].tolist()))) if vis.any() else 0
    issues = []
    if max(corner) > 0: issues.append("opaque corner (baked background?)")
    if fringe > 40: issues.append(f"{fringe} near-white pixels (halo?)")
    if colours > 400: issues.append(f"{colours} colours (not pixel-art clean)")
    lid = name[4:-4]
    items.append({"id": lid, "path": "ArtSource/world/settle_pl/" + name, "size": f"{im.width}x{im.height}", "colours": colours, "opaque_corner": bool(max(corner) > 0), "near_white_pixels": fringe,
                  "source": {"kind": "AI-generated", "tool": "Pixel Lab (create_object_pro_flash, view low top-down, style anchor: the atlas desk sprite)" if not name.startswith("raw_wreckv") else "Pixel Lab (create_object_pro_flash 8 directions)",
                             "licence": "Pixel Lab subscription terms (generated output usable commercially under the plan)", "hand_edits": EDITED.get(name, "none; the game then mutes and dirties it in props_settle_pl.py")},
                  "issues": issues})
    if issues: problems.append((name, issues))
out = {"schema_version": 1, "generated": datetime.date.today().isoformat(), "note": "AI-generated art: keep for any store AI-content disclosure. All of these are further changed by ArtSource/world/props_settle_pl.py (muting, dirt, outline).", "assets": items}
json.dump(out, open(os.path.join(ROOT, "ArtSource", "ASSET_MANIFEST.json"), "w"), indent=1)
print(f"{len(items)} assets -> ArtSource/ASSET_MANIFEST.json; {len(problems)} with issues")
for n, i in problems: print("  ", n, "; ".join(i))
