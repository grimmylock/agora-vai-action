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
SMGS = ['CorvanHornet45', 'CorvanWasp9', 'CorvanGnatPDW', 'VosaVz62', 'PPZZubr']
RIFLES = ['VK74', 'VKM', 'VKS74U', 'VK74Obelisk', 'VK103', 'NA94Yenisei', 'CorvanBattleRifle', 'CorvanBullpup', 'TS9Tishina', 'HaldenHR36K', 'TR30', 'BR86Bulldog', 'Smerch3']
SNIPERS = ['VSNShepot', 'CorvanMarksman', 'SV2Strela', 'Sever98', 'VK127Grom', 'HaldenAM50Sledge']
VISIBLE = SMGS + RIFLES + SNIPERS
# per-gun overrides for the visible cut: (x0, x1, y0) texture px - the columns of the magazine and the row it starts at (None = automatic)
# guns whose magazine sits behind the grip or beside a bipod/foregrip the automatic search would take: key -> (centre x, top y, half width) in VIRTUAL px (the old in-hand art's magazine place)
NEAR = {'CorvanMarksman': (18, 4.0, 3.5), 'VK127Grom': (10.2, 4.4, 3.0), 'HaldenAM50Sledge': (19, 6.0, 4.0), 'BR86Bulldog': (11.5, 6.5, 3.5), 'CorvanBullpup': (10.5, 7.4, 2.4)}
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
    if k in NEAR: cx, ty, hw = NEAR[k]; x0, x1, y0 = int((cx - hw) * d), int((cx + hw) * d), int(ty * d)
    elif k in OVR: x0, x1, y0 = OVR[k]
    else:
        if not runs: raise SystemExit(k + ': no hanging magazine found')
        x0, x1 = max(runs, key=lambda r: (bot[r[0]:r[1] + 1].max() - body_bot) * (r[1] - r[0] + 1) ** 0.5); y0 = body_bot - 1
    # flood fill from the lowest pixel of that run, inside the columns +- 2, below y0
    ys = np.where(a[:, x0:x1 + 1].any(1))[0]; ylow = ys.max(); xs = np.where(a[ylow, x0:x1 + 1])[0]; seed = (ylow, x0 + int(xs[len(xs) // 2]))
    allow = np.zeros_like(a); allow[y0:, max(0, x0 - 2):x1 + 3] = True
    lab, _ = ndi.label(a & allow, structure=np.ones((3, 3))); comp = lab == lab[seed]
    op = ndi.binary_opening(comp, structure=np.ones((6, 6)))                                # thin protrusions (a strip of the handguard's lip, a trigger-guard tab) are not the magazine
    if op.sum() > 0.6 * comp.sum(): lab2, _ = ndi.label(op, structure=np.ones((3, 3))); comp = lab2 == lab2[seed] if lab2[seed] else comp
    ys2, xs2 = np.where(comp)                                                                # the top rows: clip to the width of the magazine's body a little lower (the handguard's lip beside the top is not the magazine)
    top = ys2.min(); body_rows = (ys2 >= top + 16) & (ys2 <= top + 26)
    if body_rows.sum() > 20:
        L, R = xs2[body_rows].min() - 2, xs2[body_rows].max() + 2
        for y in range(top, top + 16):
            row = np.where(comp[y])[0]
            for x in row:
                if x < L or x > R: comp[y, x] = False
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
# revolvers: the cylinder's interior box (x0, x1, y0, y1, inclusive, texture px). The cylinder becomes weapon_<Key>_hdcyl.png (outlined), the frame weapon_<Key>_hdbody.png keeps a dark empty window; the game slides the cylinder out along the underside for the whole reload.
CYL = {'HaldenServiceRevolver': (50, 72, 9, 28), 'RinaldiR6': (51, 76, 17, 39), 'RK12Kuvalda': (45, 81, 13, 36),
       'Tambour12': (104, 130, 14, 42)}                       # the drum shotgun's drum, the part below the receiver
# break actions: the hinge (virtual px, WeaponArt.Hinges): everything from the hinge's x forward (barrels and fore-end) swings down on it -> _hdbarrels.png, the rest _hdbody.png
BRK = {'Sever34': (19.3, 3.4), 'Obrez66': (9.8, 3.6)}
def breaker(k):
    im = load(k); d = D[k]; hx = int(round(BRK[k][0] * d)); body = im.copy(); bar = np.zeros_like(im); bar[:, hx:] = im[:, hx:]; body[:, hx:] = 0
    save(f'weapon_{k}_hdbody.png', body); save(f'weapon_{k}_hdbarrels.png', bar); return int((bar[..., 3] > 0).sum())
def cylinder(k):
    im = load(k); x0, x1, y0, y1 = CYL[k]; box = np.zeros(im.shape[:2], bool); box[y0:y1 + 1, x0:x1 + 1] = True; a = im[..., 3] > 0; comp = box & a
    body = im.copy(); body[comp] = (24, 26, 30, 255)                                           # the empty window: the dark inside of the frame
    cyl = np.zeros_like(im); cyl[comp] = im[comp]
    ring = ndi.binary_dilation(comp, structure=np.ones((3, 3))) & ~comp; cyl[ring] = K            # an outline of its own, since it leaves the frame
    save(f'weapon_{k}_hdbody.png', body); save(f'weapon_{k}_hdcyl.png', cyl)
    return int(comp.sum())
# belt-fed guns whose picture draws no box (Yan: "draw the belt boxes for the four heavies"): (box centre x, receiver underside y) in virtual px. The box hangs under the feed tray with three rounds of belt going up into it;
# it is drawn as the gun's magazine (always seated at rest, drops and is replaced on a reload); no body file, the gun is drawn whole.
BOXES = {'PL62Kobza': (23.0, 5.9), 'PL74': (24.5, 5.4), 'CorvanLMG': (22.0, 8.0)}
# a magazine that sits ON TOP (the Zulu's pan, the round thing on its receiver, Yan: "this one has the ammo on that round thing on top"): the opaque pixels in the box (x0, x1, y0, y1 texture px) come off; the reload moves it up (WeaponArt.TopMags)
TOP = {'PL74PPolovetsZulu': (82, 114, 14, 27)}
JACKET = {'PL74PPolovetsZulu': (77, 20)}                          # (the column behind the pan whose pixels continue the jacket, the row of its top outline)
def top_mag(k):
    im = load(k); d = D[k]; x0, x1, y0, y1 = TOP[k]; box = np.zeros(im.shape[:2], bool); box[y0:y1 + 1, x0:x1 + 1] = True; comp = box & (im[..., 3] > 0)
    body = im.copy(); body[comp] = 0; mag = np.zeros_like(im); mag[comp] = im[comp]
    # the pan covered part of the receiver jacket: put the jacket back where the pan was, copying the column just behind it (its top outline included), so only the round thing leaves (Yan: "removes the entire piece")
    if k in JACKET:
        src, ytop = JACKET[k]
        for y in range(ytop, y1 + 1):
            for x in range(x0, x1 + 1):
                if comp[y, x] and im[y, src, 3] > 0: body[y, x] = im[y, src]
    save(f'weapon_{k}_hdbody.png', body); save(f'weapon_{k}_hdmag.png', mag)
    ys, xs = np.where(comp); cx = (xs.min() + xs.max() + 1) / 2 / d; return (cx, (ys.max() + 1) / d, cx, (ys.min() + ys.max() + 1) / 2 / d), int(comp.sum())
def ammo_box(k):
    im = load(k); d = D[k]; xc, yb = BOXES[k]; bw, bh = int(round(4.0 * d)), int(round(5.5 * d)); x0 = int(round(xc * d - bw / 2)); y0 = int(round((yb - 0.5) * d)); x1 = x0 + bw - 1; y1 = y0 + bh - 1
    H, Wd = im.shape[:2]; canvas = np.zeros((max(H, y1 + 4), Wd, 4), np.uint8); box = np.zeros(canvas.shape[:2], bool); box[y0:y1 + 1, x0:x1 + 1] = True
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            tt = (x - x0) / max(1, bw - 1); lid = y - y0 < int(0.6 * d)
            col = (118, 126, 90) if y == y0 else (38, 44, 30) if lid else (22, 26, 18) if y == y1 else (98, 108, 74) if tt < 0.25 else (72, 80, 54) if tt < 0.7 else (50, 56, 38)
            canvas[y, x] = (*col, 255)
    hh = int(round(0.5 * d))                                                                      # the carry handle on top
    for y in range(y0 - hh, y0):
        for x in range(x0 + int(bw * 0.28), x0 + int(bw * 0.72)): canvas[y, x] = (60, 66, 72, 255) if y == y0 - hh else (34, 38, 42, 255); box[y, x] = True
    for x in range(x1 - int(0.5 * d), x1 + 1):                                                      # the latch on the front edge
        for y in range(y0 + int(2.0 * d), y0 + int(3.2 * d)): canvas[y, x] = (126, 132, 140, 255)
    for (rx, ry) in ((x0 + int(0.7 * d), y0 + int(0.25 * d)), (x1 - int(0.9 * d), y0 + int(0.25 * d))):   # rivets
        canvas[ry:ry + 2, rx:rx + 2] = (150, 156, 120, 255)
    for j in range(3):                                                                              # the belt going up into the feed tray: three brass rounds, copper tips
        bx = x0 + int(bw * (0.36 + 0.14 * j)); rw = max(2, int(0.45 * d)); top = y0 - hh - int(1.3 * d)
        for y in range(top, y0 - hh):
            for x in range(bx, bx + rw): canvas[y, x] = (166, 108, 60, 255) if y < top + int(0.4 * d) else (206, 166, 70, 255)
        box[top:y0 - hh, bx:bx + rw] = True
    ring = ndi.binary_dilation(box, structure=np.ones((3, 3))) & ~box; canvas[ring] = K
    save(f'weapon_{k}_hdmag.png', canvas)
    ys, xs = np.where(canvas[..., 3] > 0); return ((x0 + x1 + 1) / 2 / d, y0 / d, (x0 + x1 + 1) / 2 / d, (y0 + y1 + 1) / 2 / d), int(box.sum())
def hidden(k):
    im = load(k); a = im[..., 3] > 0; H, Wd = a.shape; d = D[k]; g = G[k]; gx, gy = g[0] * d, g[1] * d
    mag = np.zeros_like(im); rows = []
    for y in range(int(gy) - 2, H):
        if not a[y, int(gx)]: continue
        x0 = x1 = int(gx)
        while x0 > 0 and a[y, x0 - 1] and gx - x0 < 3.6 * d: x0 -= 1
        while x1 < Wd - 1 and a[y, x1 + 1] and x1 - gx < 3.6 * d: x1 += 1
        rows.append((y, x0, x1))
    # a real magazine is long: extend it up inside the frame along the grip's own centre line (the part hidden by the gun until it slides out or drops)
    ysr = np.array([r[0] for r in rows], float); cxr = np.array([(r[1] + r[2]) / 2 for r in rows]); w0 = int(np.median([r[2] - r[1] + 1 for r in rows]))
    slope, icpt = np.polyfit(ysr, cxr, 1) if len(rows) > 2 else (0.0, cxr.mean()); ytop = int(gy - 5.2 * d)
    ext = []
    for y in range(max(0, ytop), rows[0][0]):
        cxx = slope * y + icpt; x0e = int(round(cxx - w0 / 2)); ext.append((y, x0e, x0e + w0 - 1))
    rows = [(y, int(round(slope * y + icpt - w0 / 2)), int(round(slope * y + icpt - w0 / 2)) + w0 - 1) for y in range(max(0, ytop), rows[-1][0] + 1)]   # straight edges along the grip's centre line (the silhouette's own ragged edge is not a magazine's)
    ymin, ymax = rows[0][0], rows[-1][0]; plate = max(5, int(round(0.95 * d)))
    # a pistol magazine as in Yan's references (2026-11-16): a blued steel box a little narrower than the grip, a column of round witness holes down the middle, a wider base plate with a lip
    cxs = [(r[1] + r[2]) / 2 for r in rows]; hole_r = max(1, int(round(0.20 * d)))
    for n, (y, x0, x1) in enumerate(rows):
        pl = y > ymax - plate; inset = -1 if pl else 2                                       # the base plate is a little wider than the body
        xa, xb = x0 + inset, x1 - inset; w = xb - xa + 1
        for x in range(xa, xb + 1):
            tt = (x - xa) / max(1, w - 1)
            if x == xa or x == xb or y == ymax: col = (10, 10, 13)
            elif y == ymin: col = (110, 116, 126)                                                    # the feed lips' lighter edge
            elif pl: col = (84, 88, 98) if y == ymax - plate + 1 else (52, 55, 62) if tt < 0.2 else (30, 32, 37)
            elif tt < 0.22: col = (82, 88, 98)
            elif tt < 0.6: col = (46, 50, 58)
            else: col = (27, 30, 36)
            mag[y, x] = (*col, 255)
    gap = int(round(1.5 * d)); hy = ymax - plate - int(round(1.3 * d))
    while hy > ymin + int(round(0.9 * d)):                                                                 # witness holes
        i = min(len(rows) - 1, max(0, hy - ymin)); cx = int(round(cxs[i]))
        for yy in range(hy - hole_r, hy + hole_r + 1):
            for xx in range(cx - hole_r, cx + hole_r + 1):
                if (xx - cx) ** 2 + (yy - hy) ** 2 <= hole_r * hole_r + 1 and 0 <= yy < mag.shape[0] and mag[yy, xx, 3]: mag[yy, xx] = (6, 6, 8, 255)
        hy -= gap
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
            if k in CYL: print(k, 'cylinder', cylinder(k), 'px'); continue
            if k in BRK: print(k, 'barrels', breaker(k), 'px'); continue
            if k in TOP: r = top_mag(k); pts[k] = r[0]; print(k, 'pan on top', r[1], 'px', [round(float(v), 1) for v in r[0]]); continue
            if k in BOXES: r = ammo_box(k); pts[k] = r[0]; print(k, 'drawn box', r[1], 'px'); continue
            if k in HIDDEN: r = hidden(k); pts[k] = r[0]; print(k, 'hidden mag', r[1], 'px', [round(float(v), 1) for v in r[0]])
            else: r = finish_visible(k); pts[k] = r[0]; print(k, 'cut', r[2], 'px', 'box', r[1], [round(float(v), 1) for v in r[0]])
        except SystemExit as e: print('SKIPPED', e)
    patch_points(pts)
if __name__ == '__main__': main(sys.argv[1:] or HIDDEN + VISIBLE)
