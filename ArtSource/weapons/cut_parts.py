#!/usr/bin/env python3
"""Cuts a moving part out of a double-size gun picture so the game can move it in code (2026-11-15, Yan: "test it your way first, no spritecook"): the pump fore-end, a slide, a bolt, a lever.
   python3 ArtSource/weapons/cut_parts.py [Key ...]      (all keys of gun_parts.json when none given)
Reads gun_parts.json: {"<Key>": {"style": "pump|slide", "rect": [x0, y0, x1, y1] (or "rects": [[...], ...]) in texture px (end exclusive), "fill_from": x (optional), "fills": [{"rect": [...], "color": [r, g, b]}] (optional), "travel": texture px}}.
Writes weapon_<Key>_part.png (the pixels inside the rect) and weapon_<Key>_body.png (the rest, with the hole filled by smearing the column just left of the rect across it, so the barrel and the magazine tube show when the part slides away), BOTH the size of
weapon_<Key>_hd.png so the grip pivot is the same. The menu and the inventory keep using the whole _hd picture. The style and travel go into HeldWeapon.cs (WeaponArt.Parts)."""
import os, sys, json
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); WP = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'Sprites', 'Weapons')
def main(keys):
    cfg = json.load(open(os.path.join(HERE, 'gun_parts.json')))
    for k in keys or cfg:
        c = cfg[k]; a = np.array(Image.open(os.path.join(WP, f'weapon_{k}_hd.png')).convert('RGBA')); rects = c.get('rects') or ([c['rect']] if 'rect' in c else [])
        part = np.zeros_like(a); body = a.copy()
        if 'column_cut' in c:                                                   # a slide: per column from y0 down to (and including) the first black outline pixel at or below scan_from = the slide's lower edge
            cc = c['column_cut']
            for x in range(cc['x'][0], cc['x'][1]):
                ye = next((y for y in range(cc['scan_from'], a.shape[0]) if a[y, x, 3] > 0 and a[y, x, :3].astype(int).sum() < cc.get('dark', 24)), None)
                if ye is None: continue
                part[cc['y0']:ye + 1, x] = a[cc['y0']:ye + 1, x]; body[cc['y0']:ye + 1, x] = 0
        for x0, y0, x1, y1 in rects: part[y0:y1, x0:x1] = a[y0:y1, x0:x1]; body[y0:y1, x0:x1] = 0
        if 'fill_from' in c:                                                    # smear the column next to the first rect across it (a pump: the tube and barrel behind the fore-end)
            x0, y0, x1, y1 = rects[0]; fx = c['fill_from']
            for y in range(y0, y1):
                if a[y, fx, 3] > 0: body[y, x0:x1] = a[y, fx]
        for f in c.get('fills', []):                                            # hand-placed fills (what shows when the part is away): {"rect": [x0, y0, x1, y1], "color": [r, g, b]}
            x0, y0, x1, y1 = f['rect']; body[y0:y1, x0:x1] = [*f['color'], 255]
        Image.fromarray(part, 'RGBA').save(os.path.join(WP, f'weapon_{k}_part.png')); Image.fromarray(body, 'RGBA').save(os.path.join(WP, f'weapon_{k}_body.png'))
        print(f'{k}: ["{k}"] = ("{c["style"]}", {c["travel"]}f),')
if __name__ == '__main__': main(sys.argv[1:])
