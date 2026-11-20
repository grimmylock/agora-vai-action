# Mother Hollow, redone on the Gaunt's method (2026-11-20; Yan: the boss concepts "didn't have much care put into them, they look bad"): the real Pallid Prowler's bone-grey hide and anatomy (lean barrel, a humped shoulder, thin folded legs, a long wedge head with a green-lit mouth),
# grown huge and swollen with her brood: a belly of translucent sacs with pups inside, their mouths already lit. Hand-built with pxart (tubes and ellipses shaded as volume, details by hand), faces right. Concept, NOT in the game.
# python3 ArtSource/make_mother_hollow_v2.py -> mutants_anim/boss_concepts/mother_hollow_v2.png (+ _4x)
import os, sys, math, random
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts'); os.makedirs(OUT, exist_ok=True)
HIDE = ramp("#1b1915", "#38352f", "#625f54", "#979381", "#c2beac"); HIDE_F = ramp("#141310", "#28261f", "#46433a", "#6e6a5c", "#928e7e")       # the Prowler's bone-grey, and the far side in shadow
BONE = ramp("#3a362c", "#6c6654", "#a39b82", "#d4ccb0", "#f0ead2")
SAC = ramp("#0a130c", "#15251a", "#264330", "#3f6a43", "#78a867"); GLOW = ramp("#16391b", "#2a7a30", "#52c94e", "#a6f27c", "#e4ffc8")
FLESH = ramp("#1a0a0c", "#3a1418", "#5e2328", "#8a3a3c", "#b46060"); VEIN = ramp("#1c0e16", "#3a1a2c", "#5a2a44")
def tube(m, pts, w): return m.tube(pts, w)
def mottle(im, seed, n, cols):
    """blotches and pale scars on the hide: darker patches, a few lighter ones; only where there is hide"""
    rnd = random.Random(seed); a = np.array(im); H, W = a.shape[:2]
    for _ in range(n):
        x, y = rnd.randrange(W), rnd.randrange(H)
        if a[y, x, 3] == 0: continue
        r = rnd.randint(2, 6); c = cols[rnd.randrange(len(cols))]
        for yy in range(max(0, y - r), min(H, y + r + 1)):
            for xx in range(max(0, x - r), min(W, x + r + 1)):
                if a[yy, xx, 3] > 0 and (xx - x) ** 2 / (r * 1.6) ** 2 + (yy - y) ** 2 / r ** 2 < 1 and rnd.random() < 0.8: a[yy, xx, :3] = (a[yy, xx, :3] * 0.55 + np.array(c[:3]) * 0.45).astype(np.uint8)
    return Image.fromarray(a, "RGBA")
def pup(im, x, y, flip=1):
    """a pup curled in its sac: a small hound seen from the side, pale head, a mouth already lit"""
    body = [(-6, 1), (-5, 0), (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (-5, 1), (-4, 1), (-3, 1), (-2, 1), (-1, 1), (0, 1), (-4, 2), (-2, 2), (-1, 2)]
    for dx, dy in body: px(im, [(x + dx * flip, y + dy)], HIDE_F[1] if dy else HIDE_F[2])
    for dx, dy in ((1, -1), (2, -1), (3, -1), (1, 0), (2, 0), (3, 0)): px(im, [(x + dx * flip, y + dy)], HIDE[2])        # the head
    px(im, [(x + 4 * flip, y), (x + 4 * flip, y + 1)], GLOW[3]); px(im, [(x + 3 * flip, y + 1)], GLOW[4])                 # the lit mouth
    px(im, [(x - 6 * flip, y - 1)], HIDE_F[2])                                                                             # a stub of tail
K = 4.6; PAD = 8
S = (int(50 * K) + 2 * PAD, int(28 * K) + 2 * PAD)
def X(x): return x * K + PAD
def Yc(y): return y * K + PAD
def T(m, pts, w): return m.tube([(X(x), Yc(y)) for x, y in pts], [v * K for v in w])
def PL(m, pts): return m.poly([(X(x), Yc(y)) for x, y in pts])
def E(m, cx, cy, rx, ry, rot=0): return m.ell(X(cx), Yc(cy), rx * K, ry * K, rot)
def mother():
    """Laid out on the real Pallid Prowler's own sprite (47 x 23: ArtSource's mutant_hd_pallid_prowler.png read joint by joint): a long low barrel with a flat back and a small hump, a short wedge head carried level, thin legs with the hind ones folded; everything here is those joints x 4.6, swollen below."""
    L = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
    # far legs (shadow side)
    part(lambda m: (T(m, [(15, 9), (12, 14.5), (9, 16.5), (10, 20.5)], [3.2, 2.0, 1.4, 1.1]), PL(m, [(8, 20.2), (13.5, 20.2), (14.5, 21.4), (8, 21.6)])), HIDE_F, tex=0.1, seed=1)
    part(lambda m: (T(m, [(34, 10), (35, 14), (37, 18), (38, 20.6)], [2.0, 1.5, 1.2, 1.0]), PL(m, [(36.5, 20.2), (41, 20.2), (42, 21.4), (36.5, 21.6)])), HIDE_F, tex=0.1, seed=2)
    # the body: the long barrel, the small hump, the deep chest, the haunch, the neck
    part(lambda m: (E(m, 23, 7.2, 14, 4.2), E(m, 25, 4.6, 5.5, 2.6, -4), E(m, 31.5, 8, 5.5, 4.6), E(m, 12.6, 9, 4.6, 5), T(m, [(35, 7.4), (37.6, 8), (39.5, 8.2)], [4.4, 4, 3.4])), HIDE, tex=0.16, seed=3, depth=7, bias=-0.04)
    part(lambda m: E(m, 22, 11.6, 11.5, 3.4, 1), HIDE, tex=0.16, seed=4, depth=4, gain=0.9)                                                          # the belly slung low with the brood
    # ribs over the chest and fore barrel: tight skin, dark gaps
    gap = Mask(S); E(gap, 29, 8.4, 4.6, 3.6, 0); L.append(shade(gap, ramp("#1a1812", "#2a2820", "#3a372c", "#4a4739", "#5a5646"), depth=2.0, tex=0.1, seed=5))
    rib = Mask(S)
    for k in range(7): T(rib, [(25.5 + k * 1.0, 5.6 + k * 0.05), (26.8 + k * 1.0, 8.2), (26.0 + k * 1.0, 10.6)], [0.5, 0.55, 0.4])
    L.append(shade(rib, BONE, depth=1.3, tex=0.05, seed=6, gain=0.9))
    # the spine: knuckles of bone along the flat back
    sp = Mask(S)
    for i, x in enumerate(range(10, 34, 2)):
        y = 3.2 if 20 <= x <= 29 else 3.7; PL(sp, [(x - 0.8, y + 1.2), (x - 0.2, y - 0.5 - (i % 2) * 0.3), (x + 0.6, y + 1.2)])
    L.append(shade(sp, BONE, depth=1.5, tex=0.05, seed=7))
    vn = Mask(S)
    for pts in ([(14, 6), (17, 8), (19, 10.5)], [(20, 4.5), (22, 6.5), (24, 9)], [(11, 7), (12, 10), (11.5, 13)], [(16, 9), (19, 11.5), (22, 12.5)]): T(vn, pts, [0.32, 0.25, 0.2])
    L.append(shade(vn, VEIN, depth=0.9))
    # near legs: the haunch and a folded hind; the fore, thin and straight
    part(lambda m: (T(m, [(12.5, 9), (9.2, 14.5), (6.2, 16.5), (7, 20.4)], [4.2, 2.6, 1.7, 1.2]), PL(m, [(4.6, 20.0), (10, 20.0), (11, 21.4), (4.6, 21.6)])), HIDE, tex=0.16, seed=8, depth=4)
    part(lambda m: (T(m, [(26.6, 10), (24.4, 14.6), (25.4, 18.4), (26, 20.6)], [2.8, 2.0, 1.5, 1.1]), PL(m, [(24.6, 20.2), (29.6, 20.2), (30.8, 21.4), (24.6, 21.6)])), HIDE, tex=0.16, seed=9, depth=4)
    jn = Mask(S)
    for (x, y, r) in ((9.2, 14.5, 1.1), (6.2, 16.5, 0.9), (24.4, 14.6, 1.0), (25.4, 18.4, 0.8)): E(jn, x, y, r, r)
    L.append(shade(jn, BONE, depth=1.5, tex=0.05, seed=10, gain=0.9))
    cl = Mask(S)
    for dx in (0, 1.8, 3.6): T(cl, [(5 + dx, 21.4), (5.9 + dx, 22.8)], [0.6, 0.18])
    for dx in (0, 1.8, 3.6): T(cl, [(25 + dx, 21.4), (25.9 + dx, 22.8)], [0.6, 0.18])
    L.append(shade(cl, BONE, depth=1.1))
    # the brood: sacs bulging out of the belly between the legs, a fold of flesh round each root, a pup in each
    sacs = [(15.5, 13.2, 2.0, 1.8), (19, 14.6, 2.5, 2.3), (23.2, 15, 2.6, 2.4), (27.4, 13.4, 2.0, 1.8), (30.2, 11.8, 1.5, 1.4)]
    for i, (x, y, rx, ry) in enumerate(sacs):
        fold = Mask(S); E(fold, x, y - ry + 0.5, rx * 0.8, 1.1); L.append(shade(fold, FLESH, depth=1.6, tex=0.1, seed=40 + i))
        sm = Mask(S); E(sm, x, y, rx, ry); L.append(shade(sm, SAC, depth=5.0, tex=0.05, seed=20 + i, bias=0.12, gain=0.9))
    im = paint(S, L)
    im = mottle(im, 11, 130, [HIDE[1], HIDE[1], HIDE[0], (112, 84, 72, 255), (78, 80, 64, 255)])
    for i, (x, y, rx, ry) in enumerate(sacs):
        pup(im, int(X(x)) - 1, int(Yc(y)), 1 if i % 2 == 0 else -1)
        for (a, b) in ((-rx * .55, -ry * .55), (-rx * .25, -ry * .75), (rx * .45, -ry * .5)): px(im, [(int(X(x + a)), int(Yc(y + b)))], SAC[4])
    for (x, y, ln) in ((15.5, 15.2, 20), (23.2, 17.2, 14), (19, 16.8, 9), (27.4, 15.2, 8)):
        for k in range(ln): px(im, [(int(X(x)) + (k // 7) % 2, int(Yc(y)) + k)], GLOW[1 + (k % 3 == 0)])
    # the head, as the Prowler's: a short wedge carried level, the jaw dropped on the green glow, crooked teeth, a hollow where the eye was
    hm = []
    def hpart(build, cols, **kw):
        m = Mask(S); build(m); hm.append(shade(m, cols, **kw)); return m
    hpart(lambda m: (E(m, 40.4, 7.8, 3.8, 3.2, 4), PL(m, [(40, 5.4), (43.5, 6.4), (47, 8), (47.8, 9.2), (46.4, 10.2), (41, 10.2), (37.5, 9)])), HIDE, tex=0.14, seed=30, depth=5)
    hpart(lambda m: PL(m, [(40.4, 10.2), (47, 10.2), (45.2, 14.4), (42, 15.2), (40, 12.6)]), HIDE_F, tex=0.1, seed=31, depth=3)
    hpart(lambda m: PL(m, [(41, 10.2), (46.6, 10.2), (44.8, 13.8), (42, 14.4), (40.8, 12.4)]), GLOW, depth=2.6, tex=0.05, seed=32)
    himg = paint(S, hm)
    for x, ln in ((41.2, 3), (42.6, 4), (44, 3), (45.4, 4), (46.6, 3)): px(himg, [(int(X(x)), int(Yc(10.2)) + k) for k in range(ln + 1)], BONE[4])
    for x, ln in ((42, 2), (43.4, 3), (44.8, 2)): px(himg, [(int(X(x)), int(Yc(13.6)) - k) for k in range(ln + 1)], BONE[3])
    for k in range(10): px(himg, [(int(X(42.6)) + k // 3, int(Yc(14.6)) + k)], GLOW[2]); px(himg, [(int(X(44.2)), int(Yc(14)) + k)], GLOW[1])
    px(himg, [(x, y) for x in range(int(X(38.4)), int(X(38.4)) + 6) for y in range(int(Yc(6.9)), int(Yc(6.9)) + 5)], (8, 8, 8, 255)); px(himg, [(int(X(38.4)) + 2, int(Yc(6.9)) + 2)], GLOW[1])
    for (x, y) in ((40.4, 5.7), (42, 6.4), (44, 7.2)): px(himg, [(int(X(x)), int(Yc(y)))], HIDE[4])
    im.alpha_composite(himg)
    for (x0, y0, x1, y1) in ((20, 4.5, 22, 8), (15, 4.2, 16, 7)): line(im, int(X(x0)), int(Yc(y0)), int(X(x1)), int(Yc(y1)), HIDE[0]); line(im, int(X(x0)) + 1, int(Yc(y0)), int(X(x1)) + 1, int(Yc(y1)), FLESH[3])
    return outline(im)
if __name__ == "__main__":
    m = crop(mother(), 2); m.save(os.path.join(OUT, "mother_hollow_v2.png"))
    p = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites', 'Mutants', 'mutant_hd_pallid_prowler.png')).convert("RGBA")
    W = Image.new("RGBA", (m.width * 4 + p.width * 4 + 40, m.height * 4 + 20), (46, 46, 52, 255))
    W.alpha_composite(m.resize((m.width * 4, m.height * 4), Image.NEAREST), (10, 10)); W.alpha_composite(p.resize((p.width * 4, p.height * 4), Image.NEAREST), (m.width * 4 + 30, m.height * 4 - p.height * 4 - 6))
    W.save(os.path.join(OUT, "mother_hollow_v2_4x.png")); print(m.size)
