# The Verger's second form (BOSS_DESIGNS.md "two-form fight"; Yan, 2026-11-20): a werewolf-like beast in the Gaunt's style, still wearing his torn coat, scarf and vial belt. Concept, hand-built with pxart (NOT in the game). python3 ArtSource/make_verger_beast.py
import os, sys, math
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts'); os.makedirs(OUT, exist_ok=True)
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
FUR = ramp("#0e0b0a", "#201a18", "#352c28", "#4e423b", "#72635a"); FURD = ramp("#080606", "#150f0e", "#231b19", "#352b27", "#493c36")
SKIN = ramp("#1a1214", "#33262a", "#52403f", "#76605a", "#9a8680")
COAT = ramp("#0a0a0c", "#17171b", "#25252b", "#38383f", "#52525c"); COATD = ramp("#060607", "#0e0e10", "#16161a", "#222228", "#30303a")
LEATHER = ramp("#120c08", "#241810", "#3a2618", "#553824", "#74502f"); TAN = ramp("#2a2016", "#4a3a2a", "#6e5a40", "#8c7656", "#b09a78")
RED = ramp("#1e0508", "#44090e", "#6e1018", "#9a1a22", "#c2323a"); BRASS = ramp("#2a1c08", "#5a3e10", "#946a1c", "#c8a034", "#f4d878")
BONE = ramp("#2a2620", "#524a3e", "#8a7e6a", "#c4b8a0", "#ece2cc"); MOUTH = ramp("#12040a", "#2a0810", "#4a0e18", "#6e1624", "#8a2030")
S = (170, 160)
def L(m, col, **k): return shade(m, col, **k)
import random
def strands(im, seed, n, up=1.28, down=0.74):
    """fur: short diagonal strands, lighter and darker, drawn over the shaded part"""
    rnd = random.Random(seed); a = np.array(im); H, W = a.shape[:2]
    for _ in range(n):
        x, y = rnd.randrange(W), rnd.randrange(H)
        if a[y, x, 3] == 0: continue
        f = up if rnd.random() < 0.55 else down; c = a[y, x, :3].astype(float)
        for k in range(rnd.randint(2, 4)):
            xx, yy = x + k, y + k * 2 // 1 if False else y + k
            if 0 <= xx < W and 0 <= yy < H and a[yy, xx, 3] > 0: a[yy, xx, :3] = np.clip(c * f, 0, 255).astype(np.uint8)
    return Image.fromarray(a, "RGBA")
def F(m, col, seed, n=260, **k): return strands(shade(m, col, tex=0.3, seed=seed, **k), seed, n)
def beast():
    layers = []
    # far side: leg and arm
    m = Mask(S); m.tube([(106, 78), (98, 100), (112, 118), (104, 144)], [13, 9, 6, 5]); layers.append(F(m, FURD, 1, 120, depth=3))
    m = Mask(S); m.poly([(98, 142), (110, 142), (118, 148), (108, 152), (94, 150)]); layers.append(F(m, FURD, 2, 40, depth=1.5))
    m = Mask(S); m.tube([(70, 54), (62, 84), (54, 108), (56, 130)], [10, 7, 5, 4.5]); layers.append(F(m, FURD, 3, 100, depth=3))
    m = Mask(S); m.poly([(50, 124), (62, 124), (66, 134), (58, 140), (48, 136)]); layers.append(F(m, FURD, 4, 30, depth=1.5))
    # torso: a deep chest, a narrow waist, the haunch
    m = Mask(S); m.ell(94, 70, 20, 10, rot=14); layers.append(F(m, FUR, 5, 120, depth=5))                                    # the waist
    m = Mask(S); m.ell(70, 58, 24, 21, rot=-12); layers.append(F(m, FUR, 6, 320, depth=7))                                   # the chest and the shoulder hump
    m = Mask(S); m.ell(108, 76, 13, 13); layers.append(F(m, FUR, 7, 160, depth=5))                                           # the hip
    # shaggy tufts along the spine
    m = Mask(S); m.poly([(58, 38), (60, 30), (64, 37), (68, 28), (72, 36), (78, 29), (80, 38), (88, 32), (90, 42), (98, 38), (98, 50), (62, 50)]); layers.append(F(m, FURD, 8, 80, depth=1.6))
    # near hind leg: digitigrade
    m = Mask(S); m.tube([(98, 82), (86, 102), (106, 120), (94, 144)], [24, 15, 8, 6]); layers.append(F(m, FUR, 9, 220, depth=4))
    m = Mask(S); m.poly([(88, 143), (98, 143), (106, 149), (98, 152), (84, 151)]); layers.append(F(m, FUR, 10, 50, depth=2))
    # the coat: a high collar, the back torn into a cape, long ragged tails
    m = Mask(S); m.poly([(54, 36), (66, 30), (82, 34), (100, 46), (116, 62), (126, 84), (134, 108), (128, 106), (124, 118), (118, 100), (112, 114), (108, 94), (100, 104), (98, 84), (84, 80), (72, 70), (58, 62), (50, 48)]); layers.append(F(m, COAT, 11, 60, depth=3.5))
    m = Mask(S); m.poly([(56, 38), (66, 30), (72, 36), (66, 48), (56, 52)]); layers.append(shade(m, COATD, depth=2, tex=0.1))                    # the collar
    m = Mask(S); m.poly([(96, 54), (110, 62), (118, 80), (108, 78), (100, 68)]); layers.append(shade(m, COATD, depth=2, tex=0.12, seed=12))     # a fold
    # head: lean wolf skull, long muzzle, open jaws, tall ears
    m = Mask(S); m.ell(44, 46, 15, 11, rot=-14); layers.append(F(m, FUR, 13, 120, depth=5))
    m = Mask(S); m.poly([(42, 42), (18, 50), (8, 58), (12, 62), (44, 60)]); layers.append(F(m, FUR, 14, 80, depth=3))                          # the muzzle
    m = Mask(S); m.poly([(42, 58), (16, 64), (8, 74), (18, 78), (46, 68)]); layers.append(F(m, FURD, 15, 60, depth=2.5))                         # the lower jaw, dropped
    m = Mask(S); m.poly([(42, 60), (14, 62), (14, 68), (44, 65)]); layers.append(shade(m, MOUTH, depth=1.2))
    im0 = layers[-1]
    for (x, y) in ((14, 62), (19, 61), (25, 60), (31, 59), (37, 59)): px(im0, [(x, y), (x, y + 1), (x, y + 2), (x, y + 3)], BONE[4])          # upper fangs
    for (x, y) in ((15, 70), (21, 69), (28, 67), (35, 65)): px(im0, [(x, y), (x, y - 1), (x, y - 2)], BONE[3])                               # lower fangs
    m = Mask(S); m.poly([(36, 38), (33, 18), (47, 31)]); layers.append(F(m, FURD, 16, 20, depth=1.6))
    m = Mask(S); m.poly([(50, 34), (57, 14), (63, 36)]); layers.append(F(m, FUR, 17, 30, depth=1.8))
    m = Mask(S); m.poly([(54, 31), (57, 19), (60, 33)]); layers.append(shade(m, SKIN, depth=1.0))
    # near arm: very long, knuckle-walking
    m = Mask(S); m.tube([(62, 54), (48, 86), (36, 108), (38, 130)], [19, 12, 8, 6.5]); layers.append(F(m, FUR, 18, 180, depth=3.5))
    m = Mask(S); m.poly([(32, 126), (44, 126), (48, 134), (42, 140), (32, 139), (28, 134)]); layers.append(F(m, FUR, 19, 50, depth=2))
    # the torn coat sleeve hanging from the shoulder and the wrist
    m = Mask(S); m.poly([(56, 46), (68, 50), (60, 70), (50, 84), (44, 78), (50, 62)]); layers.append(F(m, COAT, 20, 30, depth=2.2))
    m = Mask(S); m.poly([(30, 100), (44, 98), (46, 108), (40, 114), (36, 108), (32, 112)]); layers.append(F(m, COATD, 21, 20, depth=1.6))
    # the mask torn off, hanging from one strap under the jaw, a brass buckle
    m = Mask(S); m.poly([(30, 70), (44, 68), (50, 74), (46, 86), (38, 90), (32, 80)]); layers.append(shade(m, TAN, depth=2.2, tex=0.2, seed=22))
    m = Mask(S); m.tube([(46, 56), (52, 46), (56, 38)], [2.2, 2.2, 2.2]); layers.append(shade(m, LEATHER, depth=1.0))
    # the scarf, long and torn
    m = Mask(S); m.poly([(52, 54), (68, 56), (82, 78), (86, 98), (80, 102), (78, 88), (70, 92), (66, 72), (56, 64)]); layers.append(shade(m, RED, depth=2.2, tex=0.22, seed=23))
    # the vial belt across the waist
    m = Mask(S); m.tube([(74, 86), (92, 88), (110, 88)], [5, 5, 5]); layers.append(shade(m, LEATHER, depth=1.4, tex=0.2, seed=24))
    im = paint(S, layers)
    for (x, y, c) in ((78, 89, RED[3]), (84, 90, RED[2]), (91, 90, RED[3]), (99, 90, RED[2]), (106, 90, RED[3])): px(im, [(x, y), (x, y + 1), (x, y + 2)], c); px(im, [(x, y - 1)], BRASS[3])
    px(im, [(41, 78), (42, 78), (41, 79), (42, 79)], BRASS[4])
    px(im, [(36, 44), (37, 44), (36, 45), (37, 45), (45, 41), (46, 41)], BONE[4]); px(im, [(35, 43), (44, 40)], BRASS[4])                          # eyes
    for (x0, y0) in ((28, 138), (34, 141), (40, 142), (46, 140)): line(im, x0, y0, x0 - 3, y0 + 8, BONE[3]); px(im, [(x0 - 3, y0 + 8)], BONE[4])
    for (x0, y0) in ((52, 134), (56, 137), (60, 134)): line(im, x0, y0, x0 - 2, y0 + 6, BONE[2])
    for (x, y) in ((10, 60), (12, 64), (11, 70), (22, 79), (48, 82)): px(im, [(x, y)], RED[2]); px(im, [(x, y + 1)], RED[1])
    return outline(im)
if __name__ == "__main__":
    b = crop(beast(), 2); b.save(os.path.join(OUT, "verger_beast_concept.png"))
    g = Image.open(os.path.join(ROOT, "Assets", "Resources", "Sprites", "Mutants", "mutant_hd_elder_gaunt.png")).convert("RGBA")
    W = Image.new("RGBA", (b.width * 4 + g.width * 4 + 40, max(b.height, g.height) * 4), (58, 58, 64, 255))
    W.alpha_composite(b.resize((b.width * 4, b.height * 4), Image.NEAREST), (0, 0)); W.alpha_composite(g.resize((g.width * 4, g.height * 4), Image.NEAREST), (b.width * 4 + 40, 0))
    W.save(os.path.join(OUT, "verger_beast_vs_gaunt.png")); print(b.size)
