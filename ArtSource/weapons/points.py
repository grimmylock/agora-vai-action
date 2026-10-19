"""Grip / support / muzzle points (B pixels, continuous coords) per weapon: a heuristic guess,
overridden per weapon in OVERRIDE after checking the overlay sheet."""
import sys; sys.path.insert(0, '/home/claude/art/weapons')
import numpy as np
from weapon_pixel import *
from batch_all import CFG
from PIL import ImageDraw, ImageFont

OVERRIDE = {   # name: dict(grip=(x, y), sup=(x, y) or None, muzzle=(x, y)), B pixels
 "HaldenServiceRevolver": dict(grip=(4.5, 7.5)),
 "PPZZubr": dict(grip=(7.5, 8.5)),
 "VosaVz62": dict(grip=(7.5, 7.5), sup=None),
 "CorvanWasp9": dict(sup=(18.5, 7.5)),
 "CorvanGnatPDW": dict(grip=(10.5, 8.5), sup=(16.5, 7.5)),
 "Obrez66": dict(grip=(8.5, 8.5)),
 "SP14Warden": dict(grip=(5.5, 10.5)),
 "VSNShepot": dict(sup=(25.5, 8.5)),
}

def guess(B, cat):
    g = np.array([[c != "." for c in row] for row in B])
    cols = np.where(g.any(axis=0))[0]; L, R = cols[0], cols[-1]; w = R - L + 1
    top = np.array([np.where(g[:, x])[0].min() if g[:, x].any() else 99 for x in range(32)])
    bot = np.array([np.where(g[:, x])[0].max() if g[:, x].any() else -1 for x in range(32)])
    mrows = np.where(g[:, R])[0]; muzzle = (R + 1.0, (mrows.min() + mrows.max()) / 2 + 0.5)
    if cat == "melee":
        x = L + 0.22 * w; rows = np.where(g[:, int(x)])[0]
        return dict(grip=(x + 0.5, (rows.min() + rows.max()) / 2 + 0.5), sup=None, muzzle=muzzle)
    right = [bot[x] for x in range(L + int(w * 0.6), R + 1)]
    base = int(np.median(right))                                      # bottom of slide / barrel line
    if cat == "sidearm":
        deep = [x for x in range(L, R + 1) if bot[x] >= bot[L:R + 1].max() - 1]
        gx = float(np.mean(deep)) + 0.5
        return dict(grip=(gx, base + 1.5), sup=None, muzzle=muzzle)
    mid = [bot[x] for x in range(L + int(w * 0.25), L + int(w * 0.7))]
    base = int(np.percentile(mid, 30))
    runs, cur = [], []
    for x in range(L + int(w * 0.22), L + int(w * 0.72)):
        if bot[x] > base + 1: cur.append(x)
        elif cur: runs.append(cur); cur = []
    if cur: runs.append(cur)
    gx = (runs[0][0] + runs[0][-1]) / 2 + 0.5 if runs else L + w * 0.4
    grip = (gx, base + 1.5)
    sx = gx + (muzzle[0] - gx) * (0.55 if cat == "shotgun" else 0.45)
    sup = (sx, min(bot[int(sx)], base) + 0.5)
    return dict(grip=grip, sup=sup, muzzle=muzzle)

def all_points():
    out = {}
    for cat, P in CFG.items():
        for n, cfg in P.items():
            A, B = make(n, cfg)
            p = guess(B, cat); p.update(OVERRIDE.get(n, {}))
            out[n] = (cat, A, B, p)
    return out

if __name__ == '__main__':
    pts = all_points()
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 13)
    S = 10; names = list(pts)
    for page in range(0, len(names), 12):
        part = names[page:page + 12]
        sheet = Image.new('RGBA', (3 * (32 * S + 20), 4 * (16 * S + 30)), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
        for i, n in enumerate(part):
            cat, A, B, p = pts[n]
            x0, y0 = (i % 3) * (32 * S + 20) + 10, (i // 3) * (16 * S + 30) + 6
            d.text((x0, y0), n, font=font, fill=(200, 200, 200))
            bg = Image.new('RGBA', (32, 16), (42, 38, 32, 255)); bg.alpha_composite(render(B)); sheet.alpha_composite(bg.resize((32 * S, 16 * S), Image.NEAREST), (x0, y0 + 18))
            for gx in range(0, 33, 4): d.line([(x0 + gx * S, y0 + 18), (x0 + gx * S, y0 + 18 + 16 * S)], fill=(70, 70, 70))
            for key, col in (("grip", (255, 60, 60)), ("sup", (80, 160, 255)), ("muzzle", (255, 230, 60))):
                if p.get(key):
                    cx, cy = x0 + p[key][0] * S, y0 + 18 + p[key][1] * S
                    d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], outline=col, width=3)
        sheet.save(f'/tmp/claude-0/points_{page // 12}.png')
