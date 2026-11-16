#!/usr/bin/env python3
"""Cuts a moving part out of a double-size gun picture so the game can move it in code (2026-11-15, Yan: "test it your way first, no spritecook"): the pump fore-end, a slide, a bolt, a lever.
   python3 ArtSource/weapons/cut_parts.py [Key ...]      (all keys of gun_parts.json when none given)
Reads gun_parts.json: {"<Key>": {"style": "pump|slide", "rect": [x0, y0, x1, y1] (or "rects": [[...], ...]) in texture px (end exclusive), "fill_from": x (optional), "fills": [{"rect": [...], "color": [r, g, b]}] (optional), "travel": texture px (degrees for style "hammer", which also takes "pivot": [x, y] in texture px, the hinge it turns on)}}.
Writes weapon_<Key>_part.png (the pixels inside the rect) and weapon_<Key>_body.png (the rest, with the hole filled by smearing the column just left of the rect across it, so the barrel and the magazine tube show when the part slides away), BOTH the size of
weapon_<Key>_hd.png so the grip pivot is the same. The menu and the inventory keep using the whole _hd picture. The style and travel go into HeldWeapon.cs (WeaponArt.Parts)."""
import os, sys, json
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); WP = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'Sprites', 'Weapons')
def main(keys):
    cfg = json.load(open(os.path.join(HERE, 'gun_parts.json')))
    for k in keys or cfg:
        c = cfg[k]; a = np.array(Image.open(os.path.join(WP, f'weapon_{k}_hdbody.png' if os.path.exists(os.path.join(WP, f'weapon_{k}_hdbody.png')) else f'weapon_{k}_hd.png')).convert('RGBA')); rects = c.get('rects') or ([c['rect']] if 'rect' in c else [])
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
        for f in c.get('fills', []):                                            # hand-placed fills (what shows when the part is away): {"rect": [x0, y0, x1, y1], "color": [r, g, b] | "auto", "shade": 0.8, "outline": true}
            x0, y0, x1, y1 = f['rect']
            if f['color'] == 'auto':                                            # the median colour of the part's own (non-outline) pixels, shaded: the barrel or frame in the slide's own steel
                px = part[part[..., 3] > 0][:, :3].astype(float); px = px[px.mean(1) > 30]; col = np.median(px, axis=0) * f.get('shade', 0.8)
            else: col = f['color']
            body[y0:y1, x0:x1] = [*[int(v) for v in col], 255]
            if f.get('outline'): body[y0:y0 + 1, x0:x1] = [14, 14, 18, 255]; body[y1 - 1:y1, x0:x1] = [14, 14, 18, 255]
        if 'second' in c:                                                       # a SECOND moving part (2026-11-16: the hammer, the trigger, a charging handle): cut from what is left of the body, drawn by the game's second part renderer
            sc = c['second']; part2 = np.zeros_like(a)
            for x0, y0, x1, y1 in (sc.get('rects') or ([sc['rect']] if 'rect' in sc else [])): part2[y0:y1, x0:x1] = body[y0:y1, x0:x1]; body[y0:y1, x0:x1] = 0
            if 'poly' in sc:                                                    # a thin curved part (a trigger blade) that no rectangle isolates: the pixels inside a polygon, texture px
                from PIL import ImageDraw
                pm = Image.new('L', (a.shape[1], a.shape[0]), 0); ImageDraw.Draw(pm).polygon([tuple(p) for p in sc['poly']], fill=255); pm = np.array(pm) > 0
                part2[pm] = body[pm]; body[pm] = 0
            Image.fromarray(part2, 'RGBA').save(os.path.join(WP, f'weapon_{k}_part2.png'))
            print(f'{k}: part2 ("{sc["style"]}", {sc["travel"]}f)   pivot ({sc["pivot"][0]}f, {sc["pivot"][1]}f)')
        if rects or 'column_cut' in c: Image.fromarray(part, 'RGBA').save(os.path.join(WP, f'weapon_{k}_part.png'))
        Image.fromarray(body, 'RGBA').save(os.path.join(WP, f'weapon_{k}_body.png'))
        if 'style' not in c: continue
        print(f'{k}: ["{k}"] = ("{c["style"]}", {c["travel"]}f),' + (f'   pivot ({c["pivot"][0]}f, {c["pivot"][1]}f)' if 'pivot' in c else ''))
if __name__ == '__main__': main(sys.argv[1:])
