# The mutants that never got a good double-resolution sprite (Rot Swarm, Alpha Prowler, Mind Weaver, Void Weaver) as NEW humanoid-ish concepts (Yan, 2026-11-09).
# Second try: the first pass was flat blobs ("do not hold up to the standards of the hydra, brain and the first gallop"). Now every body part is a mask of shapes shaded as VOLUME
# (pxart.py: height field -> normal -> 5-colour ramp, dithered band borders, rim light) and drawn back to front; details are placed by hand, pixel by pixel.
# Concepts: ArtSource/MUTANT_CONCEPTS.md.   python3 make_mutants_humanoid.py <out_dir>
import math, os, sys
import numpy as np
from PIL import Image
from pxart import *

SKIN = ramp("#1c1b24", "#2b2a37", "#42404f", "#5f5c70", "#85819a")
SKIN_FAR = ramp("#121118", "#1b1a23", "#282732", "#383646", "#4d4a5e")
BONE = ramp("#6e6753", "#9a9179", "#bdb59b", "#d9d2bc", "#efe9d6")
GUM = ramp("#2a0f14", "#4a1a22", "#6e2a34", "#9a4350", "#c06570")
PALE = ramp("#5d6370", "#7c8390", "#9ba3b1", "#bcc3cf", "#dde2ea")
PALE_FAR = ramp("#474c58", "#5d6370", "#757c8a", "#8e95a3", "#a7aebb")

def alpha_prowler(cloaked=False):
    S = (64, 68)
    sk, skf = (PALE, PALE_FAR) if cloaked else (SKIN, SKIN_FAR)
    layers = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); layers.append(shade(m, cols, **kw)); return m
    # far side first
    part(lambda m: m.tube([(26, 37), (31, 46), (25, 54), (26, 61)], [7, 5, 3.6, 3]).poly([(24, 60), (33, 60), (33, 62), (24, 62)]), skf, tex=0.1, seed=1)
    part(lambda m: m.tube([(33, 19), (29, 31), (32, 44), (35, 55)], [5, 4.4, 3.6, 3]), skf, tex=0.1, seed=2)
    # torso: pelvis, a narrow waist, the chest and a hump of shoulder, a long neck
    part(lambda m: m.ell(26, 38, 7, 6).tube([(27, 36), (31, 28), (35, 22)], [8, 9, 11]).ell(36, 21, 9, 7, -15).ell(32, 16, 6, 5).tube([(40, 17), (45, 14)], [6, 5]), sk, tex=0.14, seed=3, depth=3.4, bias=-0.12)
    # skull and muzzle (one red eye), lower jaw
    part(lambda m: m.ell(49, 13, 6, 5.2).poly([(50, 9), (58, 13), (62, 16), (61, 18), (50, 18)]), sk, tex=0.08, seed=4, depth=2.6, bias=-0.12)
    jaw = part(lambda m: m.poly([(49, 17), (61, 18.5), (58, 21.5), (48, 20)]), skf if not cloaked else PALE_FAR, depth=1.6)
    # near leg (digitigrade) and near arm (long, knuckles down)
    part(lambda m: m.tube([(28, 38), (35, 46), (28, 54), (31, 61)], [8, 5.5, 4, 3.4]).poly([(29, 60), (39, 60), (39, 62.5), (29, 62.5)]), sk, tex=0.12, seed=5, bias=-0.12)
    part(lambda m: m.tube([(39, 20), (45, 31), (44, 43), (48, 54)], [5.6, 4.8, 4, 3.4]), sk, tex=0.12, seed=6, bias=-0.12)
    # claws (hands, feet)
    cl = Mask(S)
    for dx, l in ((-2.2, 7), (0, 8), (2.2, 7)): cl.tube([(48 + dx * 0.4, 54), (50 + dx, 54 + l)], [1.8, 0.8])
    for dx, l in ((-2, 3), (0.5, 3.4), (3, 3)): cl.tube([(38 + dx, 61), (40 + dx * 1.5, 62 + l * 0.5)], [1.6, 0.8])
    layers.append(shade(cl, BONE, depth=1.2))
    for dx, l in ((-2, 6), (0, 7), (2, 6)):
        m2 = Mask(S); m2.tube([(35 + dx * 0.3, 55), (36 + dx, 55 + l)], [1.6, 0.7]); layers.append(shade(m2, [c for c in (BONE if not cloaked else PALE)], depth=1.0))
    im = paint(S, layers)
    # dorsal spines and ribs
    sp = Mask(S)
    for (x, y, l) in ((33, 12, 4), (29, 15, 4.4), (26, 19, 4.6), (24, 25, 4.4), (23, 31, 4)): sp.tube([(x, y), (x - l * 0.6, y - l * 0.6)], [1.8, 0.5])
    im.alpha_composite(shade(sp, BONE if not cloaked else PALE, depth=1.2))
    bone = BONE[3] if not cloaked else PALE[4]; bone_d = BONE[1] if not cloaked else PALE[2]
    for k in range(4):                                                       # ribs across the narrow body: a thin lit line over a shadow line
        y = 23 + k * 3; x0 = 30 + (k if k < 3 else 2); x1 = x0 + 6 - (k // 2)
        line(im, x0, y, x1, y - 1, bone_d)
    # head details: a red eye in a dark socket, a brow, nostril, the mouth line with teeth
    eye, eye2 = rgb("#ff3b2b"), rgb("#ffb0a0")
    px(im, [(51, 11), (52, 11), (53, 11), (51, 13), (52, 13)], SKIN[0] if not cloaked else PALE[0]); px(im, [(52, 12), (53, 12)], eye); px(im, [(52, 12)], eye2)
    px(im, [(60, 14)], SKIN[0] if not cloaked else PALE[0])
    for x in range(50, 61): px(im, [(x, 18)], GUM[0])                      # the mouth line
    for x in (52, 54, 56, 58, 60): px(im, [(x, 19)], rgb("#efe9d6"))       # teeth showing under the lip
    for x in (53, 57): px(im, [(x, 17)], rgb("#d9d2bc"))
    # scars, a pale line along the cheek and shoulder
    line(im, 42, 21, 38, 26, bone_d); line(im, 47, 14, 45, 17, bone_d)
    return outline(im)

FLESH = ramp("#24231a", "#3b3724", "#564f31", "#766d43", "#9d9260")
FLESH_FAR = ramp("#16160f", "#232016", "#332f20", "#454029", "#5a5334")
FUNGUS = ramp("#33401a", "#506624", "#7a9a34", "#a8c853", "#d8ee86")
PUS = ramp("#6a5a1c", "#9a8628", "#c9b43c", "#e8d460", "#f6ea9a")

def rot_crawler():
    """a bloated, fungus-eaten man crawling on all fours, facing right"""
    S = (60, 44); layers = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); layers.append(shade(m, cols, **kw)); return m
    part(lambda m: m.tube([(33, 21), (31, 30), (34, 36), (37, 38)], [4.6, 3.8, 3.2, 2.8]).tube([(12, 25), (17, 34), (9, 37), (4, 36)], [6, 4.6, 3.4, 3]), FLESH_FAR, tex=0.1, seed=1, bias=-0.1)
    part(lambda m: m.ell(13, 24, 6.5, 6).tube([(15, 22), (24, 21), (33, 18)], [9, 10, 11]).ell(24, 27, 8.5, 5.5).ell(34, 18, 7.5, 6.5), FLESH, tex=0.2, seed=2, depth=4, bias=-0.08)   # pelvis, ribcage, a sagging bloat, shoulders
    part(lambda m: m.tube([(38, 18), (42, 22)], [6, 5.4]).ell(44, 23, 5.4, 5.2).poly([(42, 26), (49, 27), (48, 32), (43, 30)]), FLESH, tex=0.12, seed=3, bias=-0.05)   # neck, head hanging low, a slack jaw
    part(lambda m: m.tube([(14, 25), (21, 34), (13, 37), (6, 36)], [7.4, 5.2, 3.8, 3.2]), FLESH, tex=0.12, seed=4, bias=0.06)
    part(lambda m: m.tube([(36, 19), (39, 29), (42, 35), (45, 38)], [5.8, 4.4, 3.6, 3.2]), FLESH, tex=0.12, seed=5, bias=0.06)
    fm = Mask(S)
    for dy in (-1.8, 0, 1.8): fm.tube([(45, 38 + dy * 0.3), (50, 39 + dy)], [1.7, 0.8])
    for dy in (-1.4, 0.2, 1.8): fm.tube([(5, 36 + dy * 0.3), (1, 37 + dy)], [1.6, 0.8])
    layers.append(shade(fm, FLESH, depth=1.0, bias=0.1))
    # fungus: a few caps down the spine on thick stalks, moss on the shoulder
    for (cx, cy, r) in ((11, 15, 2.8), (19, 11, 3.6), (27, 10, 3.2), (34, 9, 2.6)):
        st = Mask(S); st.tube([(cx, cy + r), (cx + 0.3, cy + r + 4)], [2, 2.4]); layers.append(shade(st, FUNGUS, depth=1.0, bias=-0.2))
        cap = Mask(S); cap.ell(cx, cy, r, r * 0.7); layers.append(shade(cap, FUNGUS, depth=r, tex=0.12, seed=int(cx)))
    pat = Mask(S); pat.ell(36, 15, 4, 2.4).ell(22, 29, 3.4, 2.2); layers.append(shade(pat, FUNGUS, depth=1.6, tex=0.3, seed=9, bias=-0.15))
    im = paint(S, layers)
    # the face: sunken sockets with a pale point of an eye, a slack mouth of broken teeth, wet hair
    dk = FLESH[0]
    px(im, [(44, 21), (45, 21), (46, 21), (44, 22), (46, 22), (45, 22)], dk); px(im, [(45, 22)], rgb("#e8e0a0"))
    px(im, [(48, 22), (49, 22)], dk); px(im, [(49, 22)], rgb("#e8e0a0"))
    for x in range(44, 50): px(im, [(x, 27)], rgb("#2a0f10"))
    for x in (45, 47, 49): px(im, [(x, 28)], rgb("#e9e2c4"))
    for k in range(4): line(im, 41 - k, 18 + k, 39 - k, 22 + k, FLESH[0])
    # rotting sores: dark pits with a yellow rim, and a rib line
    for (x, y) in ((20, 24), (28, 25), (31, 18), (13, 22)):
        px(im, [(x, y), (x + 1, y)], dk); px(im, [(x - 1, y), (x + 2, y), (x, y - 1), (x + 1, y - 1)], PUS[2])
    for k in range(4): line(im, 24 + k, 17 + k * 2, 31 + k, 16 + k * 2, FLESH[1]); line(im, 24 + k, 18 + k * 2, 30 + k, 17 + k * 2, FLESH[4])
    return outline(im)

MIND_SKIN = ramp("#1f1830", "#33284d", "#4e3d75", "#705c9c", "#9b86c8")
BRAIN = ramp("#3d1640", "#6a2870", "#9a42a2", "#c66cc9", "#eaa4ec")
SHROUD = ramp("#0f0b1a", "#181229", "#251b40", "#372a5d", "#4d3d80")
SHROUD_FAR = ramp("#0a0712", "#100b1b", "#181229", "#221a3a", "#30254f")
GLOW = ramp("#6a3fb0", "#9a66e0", "#c79bff", "#e6d0ff", "#fbf3ff")

def mind_weaver():
    """a gaunt floating seer: exposed folds of brain over a huge skull, three eyes, a shroud for a body, long fingers weaving threads of light; faces right, a little turned to us"""
    S = (68, 80); layers = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); layers.append(shade(m, cols, **kw)); return m
    # the shroud: a tapering body of torn cloth with long tatters
    part(lambda m: m.poly([(24, 34), (44, 34), (49, 52), (45, 66), (40, 62), (37, 74), (33, 64), (28, 76), (25, 63), (20, 70), (20, 52)]), SHROUD, tex=0.22, seed=2, depth=5, bias=-0.1)
    # far arm and near arm: thin, jointed, long hands; the thread is woven between them
    part(lambda m: m.tube([(26, 38), (17, 44), (11, 38), (9, 30)], [4, 3, 2.6, 2.4]), SHROUD_FAR, tex=0.1, seed=3, bias=-0.05)
    part(lambda m: m.tube([(43, 37), (52, 44), (58, 37), (60, 29)], [4.4, 3.2, 2.8, 2.6]), MIND_SKIN, tex=0.1, seed=4, bias=-0.1)
    # chest and neck
    part(lambda m: m.ell(34, 40, 7.5, 7).tube([(34, 33), (35, 27)], [5, 4.4]), MIND_SKIN, tex=0.14, seed=5, bias=-0.12, depth=3.5)
    # the skull: a huge dome; the top is open brain, in folds
    part(lambda m: m.ell(35, 17, 14, 13).ell(40, 23, 9, 8), MIND_SKIN, tex=0.1, seed=6, depth=6, bias=-0.1)
    br = Mask(S); br.ell(35, 11, 12.5, 8.5).ell(30, 14, 6.5, 5); layers.append(shade(br, BRAIN, tex=0.22, seed=7, depth=4, bias=-0.04))
    im = paint(S, layers)
    # the brain's folds: dark winding grooves with a lit lip
    import random; rnd = random.Random(5)
    for (x0, y0, x1, y1) in ((26, 8, 32, 12), (31, 6, 38, 9), (37, 7, 44, 10), (25, 13, 31, 15), (33, 11, 41, 13), (30, 15, 36, 17)):
        n = int(max(abs(x1 - x0), 1) * 2)
        for i in range(n + 1):
            u = i / n; x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u + 1.2 * math.sin(u * 9 + rnd.random())
            px(im, [(round(x), round(y))], BRAIN[0]); px(im, [(round(x), round(y) - 1)], BRAIN[4])
    # three eyes (two big, one on the brow) glowing pale violet, a thin dark mouth
    for (ex, ey, s) in ((38, 21, 3), (44, 23, 3)):
        for dx in range(s):
            for dy in range(s): px(im, [(ex + dx, ey + dy)], rgb("#fbf3ff"))
        px(im, [(ex + 1, ey + 1)], rgb("#8a3cff")); px(im, [(ex - 1, ey), (ex - 1, ey + 1), (ex + s, ey + 1), (ex + s, ey + 2)], MIND_SKIN[0])
    px(im, [(41, 14), (42, 14), (41, 15), (42, 15)], rgb("#e6d0ff")); px(im, [(41, 14)], rgb("#8a3cff"))
    for x in range(40, 47): px(im, [(x, 29)], MIND_SKIN[0])
    # fingers: long, thin, light-edged
    fm = Mask(S)
    for dx, l in ((-2.6, 7), (-0.9, 9), (0.9, 9), (2.6, 7)): fm.tube([(60 + dx * 0.3, 29), (60 + dx * 1.2, 29 - l)], [1.5, 0.6])
    for dx, l in ((-2.6, 7), (-0.9, 9), (0.9, 9), (2.6, 7)): fm.tube([(9 + dx * 0.3, 30), (9 + dx * 1.2, 30 - l)], [1.5, 0.6])
    layers2 = shade(fm, MIND_SKIN, depth=1.0, bias=0.15); im.alpha_composite(layers2)
    # the thread: a bright strand between the hands, and little motes
    for t in range(0, 52):
        x = 9 + t; y = 22 - 9 * math.sin(math.pi * t / 51) + 1.6 * math.sin(t * 0.7); px(im, [(round(x), round(y))], GLOW[3] if t % 3 else GLOW[4])
        px(im, [(round(x), round(y) + 1)], GLOW[1])
    for (x, y) in ((24, 8), (50, 12), (16, 14), (56, 20), (30, 3)): px(im, [(x, y)], GLOW[3]); px(im, [(x + 1, y)], GLOW[1])
    return outline(im)

VOID_CLOTH = ramp("#07060d", "#100e1c", "#1b1830", "#2b2748", "#3f3a68")
VOID_CLOTH_PALE = ramp("#2a2740", "#3a3658", "#4d4872", "#655f8f", "#8179ae")
HAND = ramp("#2a2540", "#433b6a", "#6a5e9a", "#8d82bd", "#b6acdc")
RIFT = ramp("#4b2e9a", "#7a52e0", "#a98cff", "#d8c8ff", "#ffffff")

def void_weaver(cloaked=False):
    """a tall hooded figure; its face is a hole with one pale point; the robe is cracked with violet light and comes apart into motes at the hem"""
    S = (72, 92); layers = []
    cl = VOID_CLOTH_PALE if cloaked else VOID_CLOTH
    def part(build, cols, **kw):
        m = Mask(S); build(m); layers.append(shade(m, cols, **kw)); return m
    part(lambda m: m.tube([(24, 36), (14, 48), (10, 62)], [7, 6, 4]), cl, tex=0.2, seed=2, bias=-0.15)                                                      # far sleeve
    part(lambda m: m.poly([(26, 28), (46, 28), (52, 48), (56, 70), (46, 76), (38, 70), (30, 77), (20, 72), (16, 70), (22, 48)]), cl, tex=0.25, seed=3, depth=7, bias=-0.1)   # robe
    hood = Mask(S); hood.ell(37, 20, 11, 14).poly([(27, 24), (47, 24), (50, 36), (24, 36)]); layers.append(shade(hood, cl, tex=0.25, seed=4, depth=6, bias=-0.08))
    part(lambda m: m.tube([(48, 34), (58, 46), (62, 62)], [8, 6.4, 4.4]), cl, tex=0.2, seed=5, bias=-0.1)                                                     # near sleeve
    im = paint(S, layers)
    # the face: a hole with a ring of shadow, a single pale point and a few drifting specks
    fm = Mask(S); fm.ell(38, 21, 6.4, 9); face = fm.bits()
    for y in range(S[1]):
        for x in range(S[0]):
            if face[y, x]: im.putpixel((x, y), rgb("#020106"))
    px(im, [(39, 20), (39, 21)], rgb("#f4eeff")); px(im, [(40, 20)], RIFT[1]); px(im, [(36, 26), (41, 15), (35, 17)], RIFT[0])
    # folds in the cloth: long darker and lighter strokes, following the fall of the robe
    for (x0, y0, x1, y1, dark) in ((30, 36, 28, 70, True), (36, 38, 36, 74, False), (42, 36, 45, 72, True), (33, 40, 32, 68, False), (39, 42, 41, 70, False)):
        n = int(max(abs(y1 - y0), 1))
        for i in range(n + 1):
            u = i / n; x = round(x0 + (x1 - x0) * u + math.sin(u * 5 + x0) * 0.8); y = round(y0 + (y1 - y0) * u)
            if y < S[1] and im.getpixel((x, y))[3]: px(im, [(x, y)], cl[0] if dark else cl[3])
    # the cracks: a lattice of violet light through the robe, bright where they cross
    for pts in (((37, 38), (39, 46), (36, 54), (38, 62), (36, 70)), ((44, 40), (46, 48), (43, 56), (45, 64)), ((30, 42), (29, 50), (31, 58)), ((38, 50), (43, 52), (46, 48))):
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            n = int(max(abs(x1 - x0), abs(y1 - y0), 1))
            for i in range(n + 1):
                x = round(x0 + (x1 - x0) * i / n); y = round(y0 + (y1 - y0) * i / n)
                if im.getpixel((x, y))[3]:
                    px(im, [(x, y)], RIFT[2]); px(im, [(x + 1, y)], RIFT[0])
                    if i % 4 == 0: px(im, [(x, y)], RIFT[3])
    # hands: long fingers at the end of each sleeve, lit from above
    hm = Mask(S)
    for dx, l in ((-3, 9), (-1, 11), (1, 11), (3, 9)): hm.tube([(62 + dx * 0.4, 62), (62 + dx * 1.3, 62 + l)], [1.8, 0.7])
    for dx, l in ((-3, 9), (-1, 11), (1, 11), (3, 9)): hm.tube([(10 + dx * 0.4, 62), (10 + dx * 1.3, 62 + l)], [1.8, 0.7])
    im.alpha_composite(shade(hm, HAND, depth=1.2, bias=0.05))
    # the hem comes apart: motes and shards drift up from the floor of the robe
    import random; rnd = random.Random(11)
    for i in range(34):
        x = 16 + rnd.randint(0, 40); y = 76 + rnd.randint(0, 14)
        if rnd.random() < 0.55: px(im, [(x, y)], RIFT[0] if i % 3 else RIFT[2]); 
        if i % 5 == 0: px(im, [(x + 1, y), (x, y + 1)], RIFT[1])
    return outline(im)

def save(out):
    os.makedirs(out, exist_ok=True)
    ims = {"alpha_prowler": alpha_prowler(), "alpha_prowler_cloaked": alpha_prowler(True), "rot_crawler": rot_crawler(), "mind_weaver": mind_weaver(), "void_weaver": void_weaver(), "void_weaver_cloaked": void_weaver(True)}
    for k, im in ims.items(): crop(im).save(os.path.join(out, f"mutant_hd_{k}.png"))
    sc = 7; cs = [crop(im) for im in ims.values()]
    sheet = Image.new("RGBA", (sum(c.width for c in cs) * sc + 30 * (len(cs) + 1), max(c.height for c in cs) * sc + 40), (42, 38, 32, 255)); x = 30
    for c in cs: sheet.alpha_composite(c.resize((c.width * sc, c.height * sc), Image.NEAREST), (x, 20 + (max(cc.height for cc in cs) - c.height) * sc)); x += c.width * sc + 30
    sheet.save(os.path.join(out, "humanoid_mutants_sheet.png")); return ims

if __name__ == "__main__":
    save(sys.argv[1] if len(sys.argv) > 1 else "_out/humanoid")
