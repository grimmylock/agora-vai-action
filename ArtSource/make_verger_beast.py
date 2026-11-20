# The Verger's second form (BOSS_DESIGNS.md "two-form fight"; Yan, 2026-11-20): a werewolf-like beast, still in his torn coat, scarf and vial belt. Concept, NOT in the game.
# Rebuilt on the Gaunt's own method and anatomy (make_gaunt.py: tall hunched frame, long digitigrade legs, thin long arms, volume-shaded parts, hand-placed details), scaled up 1.3x,
# fur instead of bare hide, a canine head with ears, ribs showing through a torn flank. Faces right like every mutant. python3 ArtSource/make_verger_beast.py
import os, sys, math, random
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts'); os.makedirs(OUT, exist_ok=True)
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
FUR = ramp("#141012", "#272021", "#3d3430", "#594c44", "#7a6a5e"); FURD = ramp("#0b090a", "#171213", "#241d1d", "#352c2a", "#483c38")
BONE = ramp("#4a4332", "#7a7058", "#a89e82", "#cfc6a8", "#ece4cb"); DARK = ramp("#0a0506", "#1a0b0c", "#2d1214", "#431a1c", "#5e2527")
COAT = ramp("#09090b", "#151519", "#222228", "#34343c", "#4a4a54"); COATD = ramp("#050506", "#0c0c0e", "#141418", "#1e1e24", "#2a2a32")
LEATHER = ramp("#120c08", "#241810", "#3a2618", "#553824", "#74502f"); TAN = ramp("#2a2016", "#4a3a2a", "#6e5a40", "#8c7656", "#b09a78")
RED = ramp("#1e0508", "#44090e", "#6e1018", "#9a1a22", "#c2323a"); BRASS = ramp("#2a1c08", "#5a3e10", "#946a1c", "#c8a034", "#f4d878")
K = 1.3; OY = 14; S = (150, 170)
def Y(pts): return [(x * K, (y + OY) * K) for x, y in pts]
def wd(w): return [v * K for v in w]
def T(m, pts, w): return m.tube(Y(pts), wd(w))
def PL(m, pts): return m.poly(Y(pts))
def E(m, cx, cy, rx, ry, rot=0): return m.ell(cx * K, (cy + OY) * K, rx * K, ry * K, rot)
def strands(im, seed, n):
    """fur: short diagonal strands, lighter and darker, over the shaded part"""
    rnd = random.Random(seed); a = np.array(im); H, W = a.shape[:2]
    for _ in range(n):
        x, y = rnd.randrange(W), rnd.randrange(H)
        if a[y, x, 3] == 0: continue
        f = 1.3 if rnd.random() < 0.5 else 0.72; c = a[y, x, :3].astype(float)
        for k in range(rnd.randint(2, 4)):
            xx, yy = x - k, y + k
            if 0 <= xx < W and 0 <= yy < H and a[yy, xx, 3] > 0: a[yy, xx, :3] = np.clip(c * f, 0, 255).astype(np.uint8)
    return Image.fromarray(a, "RGBA")
def beast():
    L = []; HL = []
    def part(cur, build, cols, fur=0, **kw):
        m = Mask(S); build(m); im = shade(m, cols, **kw)
        cur.append(strands(im, kw.get('seed', 1), fur) if fur else im); return m
    # far leg and far arm
    part(L, lambda m: (T(m, [(34, 60), (41, 74), (34, 88), (37, 96)], [10, 7, 4.6, 3.6]), PL(m, [(33, 95), (49, 95), (51, 98), (33, 98)])), FURD, fur=90, tex=0.12, seed=1)
    part(L, lambda m: T(m, [(52, 32), (58, 52), (55, 74), (59, 92)], [7.5, 5.4, 4, 3.2]), FURD, fur=70, tex=0.12, seed=2)
    # the trunk: a narrow pelvis, a deep chest, a hump of shoulder, a thick neck
    part(L, lambda m: (E(m, 34, 58, 8.5, 7.5), E(m, 40, 44, 12.5, 16, 18), E(m, 46, 31, 10, 8, -20), T(m, [(48, 29), (58, 21), (66, 19)], [9, 7, 5.5])), FUR, fur=380, tex=0.16, seed=3, depth=4, bias=-0.05)
    # the torn flank: a dark wound with three ribs standing out of it
    cav = Mask(S); E(cav, 36, 52, 4.2, 6.4, 10); L.append(shade(cav, DARK, depth=2.2, tex=0.08, seed=4))
    rib = Mask(S)
    for k in range(3): T(rib, [(32.5, 48 + k * 4), (36, 49 + k * 4), (39.5, 52 + k * 4)], [1.3, 1.4, 0.9])
    L.append(shade(rib, BONE, depth=1.2))
    # a ridge of fur along the spine, with knuckles of bone showing through
    tuft = Mask(S)
    for (x, y) in ((52, 23), (46, 26), (41, 30), (36, 36), (33, 43), (31, 51)):
        PL(tuft, [(x - 2, y + 1), (x - 3, y - 4), (x + 1, y - 1), (x + 2, y - 5), (x + 3, y)])
    L.append(strands(shade(tuft, FURD, depth=1.4, tex=0.2, seed=5), 5, 60))
    sp = Mask(S)
    for i, (x, y) in enumerate(((49, 25), (43, 29), (38, 35))): E(sp, x, y, 1.6, 1.4)
    L.append(shade(sp, BONE, depth=1.2))
    # near leg: long shin, a paw with claws
    part(L, lambda m: (T(m, [(37, 58), (45, 72), (38, 86), (42, 96)], [12, 8.5, 5.4, 4.2]), PL(m, [(39, 95), (57, 95), (60, 98), (39, 99)])), FUR, fur=200, tex=0.14, seed=6, bias=-0.05)
    # near arm: the elbow high, the forearm very long, knuckles at the ground
    part(L, lambda m: T(m, [(50, 30), (60, 42), (56, 60), (64, 80), (62, 94)], [8.5, 7, 5.4, 4.6, 4]), FUR, fur=190, tex=0.14, seed=7, bias=-0.05)
    cl = Mask(S)
    for dx in (-3.4, -1.2, 1.2, 3.4): T(cl, [(62 + dx * 0.5, 92), (63 + dx * 1.3, 100.5)], [1.9, 0.7])
    for dx in (-3, 0, 3): T(cl, [(54 + dx, 97), (57 + dx * 1.2, 101.5)], [1.8, 0.7])
    L.append(shade(cl, BONE, depth=1.1))
    # the coat, torn to ribbons: a collar, a tail-coat hanging off the waist behind him, rags at the shoulder and the wrist
    part(L, lambda m: PL(m, [(48, 24), (58, 20), (68, 24), (64, 31), (54, 33)]), COATD, depth=2, tex=0.1, seed=8)                                 # the collar
    part(L, lambda m: PL(m, [(30, 50), (40, 56), (38, 74), (32, 88), (29, 80), (26, 92), (20, 82), (22, 68), (27, 62)]), COAT, fur=40, depth=2.6, tex=0.14, seed=9)   # the tail-coat
    part(L, lambda m: PL(m, [(34, 62), (38, 70), (34, 96), (31, 88), (30, 98), (26, 86), (28, 74)]), COATD, depth=1.6, tex=0.1, seed=10)           # the inner layer, longer
    part(L, lambda m: PL(m, [(44, 28), (54, 30), (58, 38), (52, 44), (50, 38), (44, 40)]), COAT, depth=2, tex=0.12, seed=11)                       # a rag of shoulder
    part(L, lambda m: PL(m, [(58, 56), (66, 55), (67, 62), (64, 66), (62, 61), (59, 64)]), COATD, depth=1.4, tex=0.1, seed=12)                        # the wrist rag
    # the belt across the waist with the vials, a brass clasp
    belt = Mask(S); T(belt, [(27, 56), (35, 59), (43, 61)], [3.6, 3.6, 3.6]); L.append(shade(belt, LEATHER, depth=1.2, tex=0.2, seed=13))
    # the scarf: flung back off the neck by the speed, torn at the end
    part(L, lambda m: PL(m, [(54, 22), (50, 24), (40, 28), (31, 36), (24, 38), (27, 42), (32, 41), (30, 46), (37, 42), (45, 33), (54, 28)]), RED, depth=2, tex=0.22, seed=14)
    # the head: a canine skull under fur, a long muzzle, a dropped jaw, two ears
    part(HL, lambda m: (E(m, 70, 18, 7.5, 7, 8), PL(m, [(70, 12), (80, 15), (91, 20), (94, 23), (92, 26), (73, 27), (66, 23)])), FUR, fur=90, tex=0.14, seed=15, depth=3, bias=-0.05)
    part(HL, lambda m: PL(m, [(71, 26), (93, 27.5), (88, 41), (80, 43), (73, 33)]), FURD, fur=40, depth=1.8, tex=0.1, seed=16)                    # the lower jaw
    part(HL, lambda m: PL(m, [(72, 26), (92, 26.8), (87, 38), (79, 40), (74, 31)]), DARK, depth=1.0)                                             # the mouth
    part(HL, lambda m: PL(m, [(63, 12), (58, -2), (71, 8)]), FURD, fur=14, depth=1.4, seed=17)                                                   # the far ear
    part(HL, lambda m: PL(m, [(69, 9), (71, -5), (79, 11)]), FUR, fur=20, depth=1.6, seed=18)                                                     # the near ear
    part(HL, lambda m: PL(m, [(71, 7), (72, -1), (76, 9)]), DARK, depth=1.0)                                                                       # inside it
    part(HL, lambda m: (E(m, 93, 23.5, 1.9, 1.5)), ramp("#050304", "#0b0708", "#14100f", "#201a19", "#2e2624"), depth=1.0)                       # the nose
    strap = Mask(S); T(strap, [(68, 12), (73, 21), (80, 30), (86, 38)], [1.9, 1.9, 1.9, 1.9]); HL.append(shade(strap, TAN, depth=1.0, tex=0.1))     # the leather mask, torn off: its strap down the cheek, hanging under the jaw
    flap = Mask(S); PL(flap, [(82, 36), (92, 34), (93, 41), (88, 46), (83, 44)]); HL.append(shade(flap, TAN, depth=1.4, tex=0.2, seed=19))
    im = paint(S, L); hm = paint(S, HL)
    X = lambda x: int(round(x * K)); YY = lambda y: int(round((y + OY) * K))
    tooth, tooth_d = rgb("#efe7cf"), rgb("#bdb396")
    for x, ln in ((75, 2), (78, 3), (81, 2), (84, 4), (87, 2), (90, 3), (92, 2)): px(hm, [(X(x), YY(27) + k) for k in range(ln + 1)], tooth)            # ragged teeth, long fangs
    for x, ln in ((77, 2), (80, 3), (83, 2), (86, 3), (88, 1)): px(hm, [(X(x), YY(40) - k - (3 if x < 80 else 0)) for k in range(ln + 1)], tooth_d)
    line(hm, X(80), YY(34), X(84), YY(42), rgb("#5e1d22")); line(hm, X(81), YY(34), X(85), YY(41), rgb("#7b2a30"))                                 # a tongue
    px(hm, [(X(70) + dx, YY(16) + dy) for dx in range(-3, 4) for dy in range(-2, 3)], rgb("#050304"))                                              # the eye socket
    px(hm, [(X(71), YY(16)), (X(71) + 1, YY(16))], rgb("#ffd86a")); px(hm, [(X(71) + 2, YY(16))], rgb("#c8902a"))                                   # a burning gold point
    px(im, [(X(35) + k, YY(60) + k // 2) for k in range(0, 14, 3)], BRASS[3])                                                                       # the clasp and the vials on the belt
    for (x, c) in ((29, RED[3]), (33, RED[2]), (38, RED[3]), (42, RED[2])): px(im, [(X(x), YY(62)), (X(x), YY(62) + 1), (X(x), YY(62) + 2)], c); px(im, [(X(x), YY(62) - 1)], BRASS[3])
    for (x, y) in ((91, 29), (93, 33), (86, 44), (40, 47)): px(hm if x > 60 else im, [(X(x), YY(y)), (X(x), YY(y) + 1)], RED[2])                      # blood
    im.alpha_composite(hm)
    return outline(im)
if __name__ == "__main__":
    b = crop(beast(), 2); b.save(os.path.join(OUT, "verger_beast_concept.png"))
    g = Image.open(os.path.join(ROOT, "Assets", "Resources", "Sprites", "Mutants", "mutant_hd_elder_gaunt.png")).convert("RGBA")
    W = Image.new("RGBA", (b.width * 4 + g.width * 4 + 40, max(b.height, g.height) * 4), (58, 58, 64, 255))
    W.alpha_composite(b.resize((b.width * 4, b.height * 4), Image.NEAREST), (0, 0)); W.alpha_composite(g.resize((g.width * 4, g.height * 4), Image.NEAREST), (b.width * 4 + 40, 0))
    W.save(os.path.join(OUT, "verger_beast_vs_gaunt.png")); print(b.size)
