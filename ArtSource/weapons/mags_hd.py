"""Magazines for the double-size guns (2026-11-16, Yan: "go" on the reload animations: the magazine must come out, drop, and a new one go in).
Visible magazines (SMGs, rifles ...): the magazine is cut out of weapon_<Key>_hd.png -> weapon_<Key>_hdmag.png, the rest -> weapon_<Key>_hdbody.png (same canvas, same pivot), the well closed with outline.
Hidden magazines (pistols): the magazine is the grip's lower part redrawn in magazine steel, drawn BEHIND the gun and slid out (no body file: the gun is drawn whole).
Writes the pngs and patches the guns' rows in Assets/Scripts/GameCore/WeaponMags.cs (well x, well y, centre x, centre y in virtual px, the units of the Grips rows).
Run: python3 ArtSource/weapons/mags_hd.py [keys...]     Review: python3 Tools/render/mag_sheet.py"""
import os, re, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); W = ROOT + '/Assets/Resources/Sprites/Weapons/'
CS = ROOT + '/Assets/Scripts/GameCore/HeldWeapon.cs'; src = open(CS).read()
D = {k: float(v) for k, v in re.findall(r'\["(\w+)"\] = ([0-9.]+)f', re.search(r"Dense = new Dictionary<string, float>\s*\{(.*?)\};", src, re.S).group(1))}
G = {}
for k, row in re.findall(r'\["(\w+)"\] = new\[\] \{([^}]*)\}', re.search(r"Dictionary<string, float\[\]> Grips = new Dictionary<string, float\[\]>\s*\{(.*?)\n        \};", src, re.S).group(1)):
    G[k] = [float('nan') if 'NaN' in v else float(v.strip().rstrip('f')) for v in row.split(',')]
K = (14, 14, 18, 255)
HIDDEN = ['PSh18Pchela', 'WaldenP9', 'Halden45Service', 'SR21Gadyuka', 'BlackHarrier', 'Corvan57', 'SteppeEagle']
VISIBLE = ['CorvanHornet45', 'CorvanWasp9', 'CorvanGnatPDW', 'VosaVz62', 'PPZZubr']
# per-gun overrides for the visible cut: (x0, x1, y0) texture px - the columns of the magazine and the row it starts at (None = automatic)
OVR = {'CorvanGnatPDW': (111, 134, 60),     # the magazine is the lower part of the pistol-grip column (an MP7-style gun): only that part comes out
       'PPZZubr': (121, 216, 31)}          # the helical magazine under the barrel
def load(k): return np.array(Image.open(W + f'weapon_{k}_hd.png').convert('RGBA'))
def save(name, arr): Image.fromarray(arr).save(W + name)
def visible(k):
    im = load(k); a = im[..., 3] > 0; H, Wd = a.shape; d = D[k]; g = G[k]
    bot = np.array([Wd and (H - 1 - np.argmax(a[::-1, x])) if a[:, x].any() else -1 for x in range(Wd)])
    gx, sx = int(g[0] * d), int((g[2] if g[2] == g[2] else g[0] + 10) * d)
    body_bot = int(np.median(bot[int(sx - 3 * d):int(sx + 3 * d)]))                       # the handguard's underside, as a level to measure the hanging magazine against
    lo = gx + int(2.5 * d); hi = sx + int(1.0 * d)
    cand = [bot[x] - body_bot for x in range(lo, min(hi + 1, Wd))]; thr = max(6, 0.45 * max(cand))                  # columns that hang clearly lower than the handguard (the trigger guard hangs less than the magazine)
    hang = np.array([x if (lo <= x <= hi and bot[x] > body_bot + thr) else -1 for x in range(Wd)])
    runs = []; start = None
    for x in range(Wd):
        if hang[x] >= 0 and start is None: start = x
        if (hang[x] < 0 or x == Wd - 1) and start is not None: runs.append((start, x - 1)); start = None
    if k in OVR: x0, x1, y0 = OVR[k]
    else:
        if not runs: raise SystemExit(k + ': no hanging magazine found')
        x0, x1 = max(runs, key=lambda r: (bot[r[0]:r[1] + 1].max() - body_bot) * (r[1] - r[0] + 1) ** 0.5); y0 = body_bot - 1
    # flood fill from the lowest pixel of that run, inside the columns +- 2, below y0
    ys = np.where(a[:, x0:x1 + 1].any(1))[0]; ylow = ys.max(); xs = np.where(a[ylow, x0:x1 + 1])[0]; seed = (ylow, x0 + int(xs[len(xs) // 2]))
    allow = np.zeros_like(a); allow[y0:, max(0, x0 - 2):x1 + 3] = True
    lab, _ = ndi.label(a & allow, structure=np.ones((3, 3))); comp = lab == lab[seed]
    return im, comp, (x0, x1, y0)
def finish_visible(k):
    im, comp, box = visible(k); d = D[k]
    body = im.copy(); body[comp] = 0
    # close the emptied well: outline pixels of the body that now face the hole keep their black; add outline where the body's pixels border the removed magazine
    grown = ndi.binary_dilation(comp, structure=np.ones((3, 3))) & ~comp & (im[..., 3] > 0); body[grown & (body[..., 3] > 0) & (body[..., :3].sum(-1) > 90)] = K
    mag = np.zeros_like(im); mag[comp] = im[comp]
    ys, xs = np.where(comp); top, left, right, bottom = ys.min(), xs.min(), xs.max(), ys.max()
    save(f'weapon_{k}_hdbody.png', body); save(f'weapon_{k}_hdmag.png', mag)
    return ((left + right + 1) / 2 / d, top / d, (left + right + 1) / 2 / d, (top + bottom + 1) / 2 / d), box, int(comp.sum())
def hidden(k):
    im = load(k); a = im[..., 3] > 0; H, Wd = a.shape; d = D[k]; g = G[k]; gx, gy = g[0] * d, g[1] * d
    mag = np.zeros_like(im); rows = []
    for y in range(int(gy) - 2, H):
        if not a[y, int(gx)]: continue
        x0 = x1 = int(gx)
        while x0 > 0 and a[y, x0 - 1] and gx - x0 < 3.6 * d: x0 -= 1
        while x1 < Wd - 1 and a[y, x1 + 1] and x1 - gx < 3.6 * d: x1 += 1
        rows.append((y, x0, x1))
    ymin, ymax = rows[0][0], rows[-1][0]
    for y, x0, x1 in rows:
        w = x1 - x0 + 1; plate = y > ymax - 4
        for x in range(x0, x1 + 1):
            t = (x - x0) / max(1, w - 1)
            if x == x0 or x == x1 or y == ymax: col = (14, 14, 18)
            elif plate: col = (70, 74, 82) if y == ymax - 3 else (36, 38, 44)
            elif t < 0.28: col = (92, 98, 108)
            elif t < 0.62: col = (60, 64, 72)
            else: col = (36, 39, 45)
            mag[y, x] = (*col, 255)
    save(f'weapon_{k}_hdmag.png', mag)
    xs = [r[1] for r in rows] + [r[2] for r in rows]; cx = (min(xs) + max(xs) + 1) / 2
    return (cx / d, ymin / d, cx / d, (ymin + ymax + 1) / 2 / d), int(sum(r[2] - r[1] + 1 for r in rows))
def patch_points(pts):
    p = ROOT + '/Assets/Scripts/GameCore/WeaponMags.cs'; t = open(p).read()
    for k, (wx, wy, cx, cy) in pts.items():
        row = f'            ["{k}"] = new[] {{ {wx:.1f}f, {wy:.1f}f, {cx:.1f}f, {cy:.1f}f }},'
        if re.search(r'\["%s"\] = new\[\][^\n]*\n' % k, t): t = re.sub(r'            \["%s"\] = new\[\][^\n]*' % k, row, t)
        else: t = t.replace('        };\n    }\n}', row + '\n        };\n    }\n}', 1)
    open(p, 'w').write(t)
def main(keys):
    pts = {}
    for k in keys:
        try:
            if k in HIDDEN: r = hidden(k); pts[k] = r[0]; print(k, 'hidden mag', r[1], 'px', [round(float(v), 1) for v in r[0]])
            else: r = finish_visible(k); pts[k] = r[0]; print(k, 'cut', r[2], 'px', 'box', r[1], [round(float(v), 1) for v in r[0]])
        except SystemExit as e: print('SKIPPED', e)
    patch_points(pts)
if __name__ == '__main__': main(sys.argv[1:] or HIDDEN + VISIBLE)
