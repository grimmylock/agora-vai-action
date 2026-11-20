# Verger beast, PART 5: the coat (Yan, 2026-11-20: "continue with the coat"). His coat torn to ribbons but still on him: a ragged collar, a tail-coat hanging off the waist, shoulder rags, a sleeve cuff on the near wrist, the vial belt with its brass clasp, the red scarf flung back.
# Order: far limbs, trunk, tail-coat, shoulder rags, belt, near limbs, cuff, collar, scarf, head. python3 ArtSource/make_verger_beast_coat.py -> mutants_anim/boss_concepts/verger_beast_coat(_5x).png
import os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
from make_verger_beast import FUR, FURD, BONE, DARK, COAT, COATD, LEATHER, TAN, RED, BRASS, strands
import make_verger_beast_body as bd
import make_verger_beast_limbs as lm
from make_verger_beast_body import K, OY, S, Y, wd, T, PL, E
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts')
COATL = ramp("#0c0c0f", "#1a1a20", "#2a2a33", "#3e3e49", "#58586a")          # a lighter cloth for the lit folds
def cloth(build, cols, seed, **kw):
    m = Mask(S); build(m); return shade(m, cols, tex=kw.pop('tex', 0.16), seed=seed, **kw)
X = lambda x: int(round(x * K)); YY = lambda y: int(round((y + OY) * K))
SCARF = None
def coat_layers(wrist=(62, 77)):
    back, front, cuff = [], [], []
    # the tail-coat: two layers of ragged strips hanging off the waist and dragging behind
    back.append(cloth(lambda m: PL(m, [(26, 48), (38, 56), (39, 74), (35, 90), (32, 82), (29, 99), (24, 86), (20, 96), (16, 82), (18, 66), (22, 56)]), COATD, 31, depth=3.0))
    back.append(cloth(lambda m: PL(m, [(28, 50), (38, 56), (37, 72), (33, 84), (30, 76), (27, 90), (23, 78), (21, 66), (24, 58)]), COAT, 32, depth=2.6))
    back.append(cloth(lambda m: PL(m, [(33, 56), (37, 58), (36, 72), (34, 66)]), COATL, 33, depth=1.2, gain=0.9))                                      # a lit fold
    # shoulder rags: what is left of the coat's shoulders and lapel
    cuff.append(cloth(lambda m: PL(m, [(36, 26), (49, 20), (59, 28), (56, 37), (52, 33), (50, 38), (46, 33), (43, 37), (40, 33), (37, 35), (34, 33)]), COATL, 34, depth=2.6, gain=1.05))
    cuff.append(cloth(lambda m: PL(m, [(40, 27), (49, 23), (54, 28), (47, 31), (42, 32)]), ramp("#18181e", "#2a2a33", "#42424f", "#5e5e70", "#7e7e92"), 35, depth=1.2, gain=0.95))
    # the belt: leather across the narrow waist
    belt = Mask(S); T(belt, [(27, 52), (34, 56), (45, 59)], [3.6, 3.6, 3.6]); cuff.append(shade(belt, LEATHER, depth=1.2, tex=0.2, seed=36))
    # the cuff on the near wrist: a ragged sleeve end with a button
    bd.LIMB = True; wx, wy = wrist[0] - 62, wrist[1] - 77                                    # the cuff follows the near wrist, not the trunk
    cuff.append(cloth(lambda m: PL(m, [(56 + wx, 72 + wy), (66 + wx, 70 + wy), (68 + wx, 78 + wy), (65 + wx, 83 + wy), (63 + wx, 78 + wy), (60 + wx, 84 + wy), (57 + wx, 78 + wy)]), COATD, 37, depth=1.8))
    cuff.append(cloth(lambda m: PL(m, [(57 + wx, 72 + wy), (65 + wx, 71 + wy), (66 + wx, 74 + wy), (58 + wx, 75 + wy)]), COATL, 38, depth=1.0, gain=0.9)); bd.LIMB = False
    # the collar: high and ragged, round the base of the neck
    cuff.append(cloth(lambda m: PL(m, [(47, 25), (53, 17), (60, 15), (68, 18), (66, 27), (62, 24), (58, 30), (54, 25), (51, 31)]), COATD, 39, depth=2.0))
    # the scarf: flung back off the neck, two torn tails
    import math
    def tail(x0, y0, x1, wid, sag, phase, seed, drop):
        n = 14; pts = [(x0 + (x1 - x0) * i / n, y0 + sag * (i / n) ** 1.5 * 10 + 3.2 * math.sin(i * 0.9 + phase) + drop * (i / n)) for i in range(n + 1)]
        ws = [wid * (1 - 0.65 * (i / n)) + 1 for i in range(n + 1)]
        a = math.radians(bd.POSE['scarf_rot']); sx, sy = (bd.tf(58, 24, True)[0] - bd.tf(58, 24, False)[0], bd.tf(58, 24, True)[1] - bd.tf(58, 24, False)[1])   # the scarf hangs from the tilted neck but flies by the wind, not with the trunk
        pts = [(x0 + (x - x0) * math.cos(a) - (y - y0) * math.sin(a) + sx, y0 + (x - x0) * math.sin(a) + (y - y0) * math.cos(a) + sy) for x, y in pts]
        bd.LIMB = True; m = Mask(S); T(m, pts, ws); bd.LIMB = False; return shade(m, RED, depth=2.0, tex=0.2, seed=seed), pts
    t1, p1 = tail(57, 23, 16, 6.2, 0.2, 0.0, 40, 3); cuff.append(t1)
    t2, p2 = tail(56, 22, 30, 3.8, 0.0, 2.4, 41, -3); cuff.append(t2)
    global SCARF; SCARF = (p1, p2)
    return back, front, cuff
def beast(J=None):
    J = J or lm.STAND; far, near = lm.limbs_pair(J); wrist = J['near']['arm'][-2]; back, front, cuff = coat_layers(wrist)
    im = Image.new("RGBA", S, (0, 0, 0, 0)); im.alpha_composite(far); im.alpha_composite(lm.tone(bd.trunk()))
    for l in back: im.alpha_composite(l)
    for l in front: im.alpha_composite(l)
    im.alpha_composite(near)
    for l in cuff: im.alpha_composite(l)
    for pts in SCARF:                                                                                                                           # folds: dark creases across the cloth and lit edges
        for i in range(3, len(pts) - 1, 3):
            bd.LIMB = True; x0, y0 = bd.XY(*pts[i]); bd.LIMB = False; px(im, [(x0, y0 - 2 + k) for k in range(0, 5)], RED[1]); px(im, [(x0 + 1, y0 - 2 + k) for k in range(0, 3)], RED[4])
    # hand-placed detail: the clasp and vials on the belt, buttons, seams down the tail, blood in the hem
    for (x, col) in ((30, RED[3]), (35, RED[2]), (40, RED[3]), (45, RED[2])):
        yy0 = 52 + (x - 27) * 0.39 + 2.2; x0, y0 = bd.XY(x, yy0); px(im, [(x0, y0 + k) for k in range(4)], col); px(im, [(x0, y0 - 1)], BRASS[3])
    x0, y0 = bd.XY(36, 56.4); px(im, [(x0 + dx, y0 + dy) for dx in range(2) for dy in range(3)], BRASS[4]); px(im, [(x0, y0 + 1)], BRASS[2])
    for (x, y) in ((58, 40), (55, 46)): x0, y0 = bd.XY(x, y); px(im, [(x0, y0), (x0 + 1, y0)], BRASS[3])
    bd.LIMB = True; x0, y0 = bd.XY(wrist[0] + 1, wrist[1] - 3); bd.LIMB = False; px(im, [(x0, y0), (x0 + 1, y0), (x0, y0 + 1)], BRASS[3])
    for (xa, ya, xb, yb) in ((24, 60, 22, 80), (30, 64, 29, 84), (36, 62, 35, 76)): p0 = bd.XY(xa, ya); p1 = bd.XY(xb, yb); line(im, p0[0], p0[1], p1[0], p1[1], COATD[0])
    for (x, y) in ((22, 94), (30, 96), (17, 90)): x0, y0 = bd.XY(x, y); px(im, [(x0, y0), (x0 + 1, y0), (x0, y0 + 1)], RED[1])
    return bd.with_head(im)
if __name__ == "__main__":
    f = crop(beast(), 2); f.save(os.path.join(OUT, "verger_beast_coat.png"))
    g = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites', 'Mutants', 'mutant_hd_elder_gaunt.png')).convert("RGBA")
    W = Image.new("RGBA", (f.width * 5 + 20, f.height * 5 + 20), (58, 58, 64, 255)); W.alpha_composite(f.resize((f.width * 5, f.height * 5), Image.NEAREST), (10, 10)); W.save(os.path.join(OUT, "verger_beast_coat_5x.png")); print(f.size)
