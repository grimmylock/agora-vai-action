# The Verger (BOSS_ROSTER #14, MISSION_CHOIR.md; Yan: "a gothic beast hunter using an axe"): concept, hand-built with pxart.py (NOT in the game). Two poses: guard (the axe held low) and the overhead chop. python3 ArtSource/make_verger_concept.py
import os, sys, math, random
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts'); os.makedirs(OUT, exist_ok=True)
COAT = ramp("#0a0a0c", "#17171b", "#25252b", "#38383f", "#4e4e57"); COAT_F = ramp("#060607", "#0e0e10", "#16161a", "#222228", "#30303a")
LEATHER = ramp("#120c08", "#241810", "#3a2618", "#553824", "#74502f"); STEEL = ramp("#0c0e10", "#1c2024", "#343b42", "#566068", "#8a969e")
RED = ramp("#1e0508", "#44090e", "#6e1018", "#9a1a22", "#c2323a"); BRASS = ramp("#2a1c08", "#5a3e10", "#946a1c", "#c8a034", "#f4d878")
SKIN = ramp("#2a2220", "#4a3a34", "#76605a", "#a08a82", "#c8b4aa"); WOOD = ramp("#140d08", "#26190f", "#3e2918", "#5a3d24", "#7a5632")
BLOOD = ramp("#1a0406", "#3a0a0e", "#5e1016", "#7e1a20", "#9e2a30")
S = (150, 170)
def axe_layer(size=(150, 170)):
    """the greataxe drawn upright (haft vertical at x=75, butt at the bottom, grip points at y=112 and y=84), as an RGBA layer; rotated by the pose"""
    L = []; m = Mask(size); m.tube([(75, 168), (75, 70)], [4.2, 3.6]); L.append(shade(m, WOOD, depth=1.6, tex=0.14, seed=3))
    wrap = Mask(size); wrap.tube([(75, 120), (75, 80)], [5.0, 5.0]); L.append(shade(wrap, LEATHER, depth=1.4, tex=0.2, seed=4))                    # the leather-wrapped grip
    cap = Mask(size); cap.ell(75, 168, 3.4, 3); L.append(shade(cap, STEEL, depth=1.2))
    for y in (74, 76): pass
    blade = Mask(size); blade.poly([(80, 50), (86, 38), (108, 34), (124, 40), (122, 58), (112, 74), (104, 86), (98, 80), (100, 66), (88, 62), (80, 66)])     # a great bearded axe head: a wide cheek, a long hooked beard
    spike = Mask(size); spike.poly([(72, 54), (58, 46), (48, 52), (60, 62), (72, 66)])                                                    # the back spike
    L.append(shade(blade, STEEL, depth=2.4, tex=0.1, seed=5)); L.append(shade(spike, STEEL, depth=1.8, tex=0.1, seed=6))
    sock = Mask(size); sock.poly([(70, 52), (80, 52), (82, 74), (68, 74)]); L.append(shade(sock, STEEL, depth=1.2))
    im = paint(size, L)
    for k in range(10): px(im, [(124 - k * 1.6, 40 + k * 3.3 + (k % 2))], STEEL[4])                                                       # a lit cutting edge
    for (x, y) in ((123, 44), (124, 50), (122, 56), (119, 64), (114, 71), (109, 78), (105, 84)): px(im, [(x, y), (x - 1, y + 1)], STEEL[0])        # serrations
    line(im, 90, 46, 108, 40, STEEL[4]); line(im, 92, 52, 112, 52, STEEL[1]); line(im, 90, 58, 104, 72, STEEL[1])                          # engraved lines on the cheek
    for (x, y, n) in ((112, 56, 5), (118, 62, 4), (106, 46, 4), (100, 70, 3)):
        for k in range(n): px(im, [(x + (k % 2), y + k)], BLOOD[2 + (k % 3)])                                                           # blood on the steel
    for k in range(4): px(im, [(58 + k, 54 + k * 2)], STEEL[0])
    ring = Mask(size); ring.ell(75, 96, 3.6, 2); L2 = shade(ring, BRASS, depth=1.0); im.alpha_composite(L2)
    return im
def hunter(pose="guard"):
    L = []
    def part(build, cols, **kw): m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
    sh = 0 if pose == "guard" else -4
    # boots and legs
    part(lambda m: m.tube([(62, 100), (58, 130), (56, 152)], [9, 7, 6]), COAT_F, tex=0.1, seed=1); part(lambda m: m.tube([(82, 100), (86, 130), (90, 152)], [9, 7, 6]), COAT, tex=0.1, seed=2)
    part(lambda m: m.poly([(50, 140), (62, 140), (66, 156), (70, 162), (44, 162), (48, 152)]), LEATHER, tex=0.18, seed=3, depth=2.0); part(lambda m: m.poly([(84, 140), (96, 140), (100, 156), (112, 162), (82, 162), (84, 152)]), LEATHER, tex=0.18, seed=4, depth=2.0)
    # the long coat: shoulders, a nipped waist, a skirt that flares and is cut into ragged tails
    part(lambda m: m.poly([(54, 52), (94, 50), (98, 92), (106, 138), (96, 152), (88, 140), (80, 154), (72, 142), (64, 154), (56, 140), (48, 150), (42, 138), (50, 94)]), COAT, tex=0.16, seed=5, depth=4.4)
    part(lambda m: m.poly([(72, 62), (96, 60), (102, 100), (108, 142), (94, 148), (86, 100)]), COAT_F, tex=0.14, seed=6, depth=2.4)           # the far side of the coat in shadow
    # belts, pouches and vials across the waist; a red scarf
    belt = Mask(S); belt.poly([(50, 92), (100, 92), (100, 97), (50, 97)]); L.append(shade(belt, LEATHER, depth=1.6, tex=0.2, seed=7))
    cross = Mask(S); cross.tube([(52, 52), (74, 80), (102, 96)], [4, 4, 4]); L.append(shade(cross, LEATHER, depth=1.4, tex=0.2, seed=8))
    for (x, y) in ((54, 100), (62, 101), (88, 101)): v = Mask(S); v.ell(x, y, 3, 5); L.append(shade(v, RED, depth=1.8))                         # vials of blood
    pch = Mask(S); pch.poly([(70, 98), (82, 98), (82, 110), (70, 110)]); L.append(shade(pch, LEATHER, depth=1.4, tex=0.2, seed=9))
    # the high collar and the scarf tails
    col = Mask(S); col.poly([(52, 44), (64, 34), (88, 34), (100, 44), (96, 52), (56, 52)]); L.append(shade(col, COAT, depth=2.0, tex=0.1, seed=10))
    sc = Mask(S); sc.tube([(60, 48), (56, 64), (60, 82)], [6, 5, 3]); L.append(shade(sc, RED, depth=1.6, tex=0.14, seed=11))
    # arms
    if pose == "guard":
        part(lambda m: m.tube([(98, 54), (110, 76), (90, 100)], [10, 8, 7]), COAT, tex=0.14, seed=12)                                          # the near arm
        part(lambda m: m.tube([(52, 56), (50, 80), (68, 108)], [9, 7, 6]), COAT_F, tex=0.14, seed=13)
        hands = [(90, 100), (68, 108)]
    else:
        part(lambda m: m.tube([(98, 54), (104, 36), (84, 20)], [10, 8, 7]), COAT, tex=0.14, seed=12)
        part(lambda m: m.tube([(52, 56), (54, 34), (74, 24)], [9, 7, 6]), COAT_F, tex=0.14, seed=13)
        hands = [(84, 20), (74, 24)]
    for (hx_, hy_) in hands: g = Mask(S); g.ell(hx_, hy_, 5, 5); L.append(shade(g, LEATHER, depth=1.8, tex=0.1, seed=hx_))
    # the head: a wide-brimmed hat, a leather half-mask, pale eyes under the brim
    part(lambda m: m.ell(74, 30, 30, 6), COAT, tex=0.12, seed=14, depth=2.4)                                                                  # the brim
    part(lambda m: m.poly([(58, 28), (62, 8), (72, 0), (76, 3), (86, 8), (90, 28)]), COAT, tex=0.12, seed=15, depth=3.0)                                # the crown
    band = Mask(S); band.poly([(58, 22), (90, 22), (91, 27), (57, 27)]); L.append(shade(band, LEATHER, depth=1.2, tex=0.1, seed=16))
    face = Mask(S); face.ell(74, 37, 10, 10); L.append(shade(face, ramp("#0a0707", "#150e0c", "#221814", "#33241e", "#4a3a32"), depth=2.4, tex=0.08, seed=17))
    maskm = Mask(S); maskm.poly([(66, 40), (82, 40), (80, 50), (74, 56), (68, 50)]); L.append(shade(maskm, LEATHER, depth=1.8, tex=0.2, seed=18))   # a leather mask over the nose and mouth
    hm = paint(S, L)
    for (x, y) in ((68, 35), (80, 35)): px(hm, [(x, y), (x + 1, y)], rgb("#dfe6ea")); px(hm, [(x, y - 1), (x + 1, y - 1)], rgb("#0a0a0c"))      # pale eyes under the brim
    for k in range(5): line(hm, 66 + k * 4, 40, 66 + k * 4, 50, LEATHER[1])                                                                  # stitching down the mask
    px(hm, [(73, 46), (74, 46), (75, 46)], BRASS[3])                                                                                          # a brass clasp
    # tattered hem strips and blood on the coat
    rnd = random.Random(5)
    for _ in range(40): px(hm, [(rnd.randint(40, 110), rnd.randint(100, 148))], BLOOD[rnd.choice((0, 1, 2))])
    ax = axe_layer(S)
    if pose == "guard":
        a = ax.rotate(-34, resample=Image.NEAREST, center=(75, 96)); hm.alpha_composite(a, (8, 0))
        out = hm
    else:
        out = Image.new("RGBA", (S[0], S[1] + 90), (0, 0, 0, 0)); out.alpha_composite(hm, (0, 90))                                     # a taller canvas: the axe is raised high over the head
        a = ax.rotate(-62, resample=Image.NEAREST, center=(75, 96)); out.alpha_composite(a, (4, 90 + 22 - 96 + 6))
    return outline(out)
if __name__ == "__main__":
    A = hunter("guard"); B = hunter("chop")
    K = 4; W = (A.width + B.width) * K + 120; H = max(A.height, B.height) * K + 80
    sheet = Image.new("RGBA", (W, H), (26, 22, 26, 255)); d = ImageDraw.Draw(sheet); x = 40
    for lab, im in (("THE VERGER - guard", A), ("THE VERGER - overhead chop", B)):
        sheet.alpha_composite(im.resize((im.width * K, im.height * K), Image.NEAREST), (x, 40)); d.text((x, 10), lab, fill=(230, 225, 215, 255)); x += im.width * K + 40
    A.save(os.path.join(OUT, "verger_guard_concept.png")); B.save(os.path.join(OUT, "verger_chop_concept.png")); sheet.convert("RGB").save(os.path.join(OUT, "verger_concept_sheet.png")); print(A.size, B.size)
