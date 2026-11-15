#!/usr/bin/env python3
"""Cuts a moving part out of a double-size gun picture so the game can move it in code (2026-11-15, Yan: "test it your way first, no spritecook"): the pump fore-end, a slide, a bolt, a lever.
   python3 ArtSource/weapons/cut_parts.py [Key ...]      (all keys of gun_parts.json when none given)
Reads gun_parts.json: {"<Key>": {"style": "pump|slide", "rect": [x0, y0, x1, y1] in texture px (end exclusive), "fill_from": x, "travel": texture px}}.
Writes weapon_<Key>_part.png (the pixels inside the rect) and weapon_<Key>_body.png (the rest, with the hole filled by smearing the column just left of the rect across it, so the barrel and the magazine tube show when the part slides away), BOTH the size of
weapon_<Key>_hd.png so the grip pivot is the same. The menu and the inventory keep using the whole _hd picture. The style and travel go into HeldWeapon.cs (WeaponArt.Parts)."""
import os, sys, json
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); WP = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'Sprites', 'Weapons')
def main(keys):
    cfg = json.load(open(os.path.join(HERE, 'gun_parts.json')))
    for k in keys or cfg:
        c = cfg[k]; a = np.array(Image.open(os.path.join(WP, f'weapon_{k}_hd.png')).convert('RGBA')); x0, y0, x1, y1 = c['rect']
        part = np.zeros_like(a); part[y0:y1, x0:x1] = a[y0:y1, x0:x1]
        body = a.copy(); body[y0:y1, x0:x1] = 0
        fx = c['fill_from']
        for y in range(y0, y1):
            if a[y, fx, 3] > 0: body[y, x0:x1] = a[y, fx]
        Image.fromarray(part, 'RGBA').save(os.path.join(WP, f'weapon_{k}_part.png')); Image.fromarray(body, 'RGBA').save(os.path.join(WP, f'weapon_{k}_body.png'))
        print(f'{k}: ["{k}"] = ("{c["style"]}", {c["travel"]}f),')
if __name__ == '__main__': main(sys.argv[1:])
