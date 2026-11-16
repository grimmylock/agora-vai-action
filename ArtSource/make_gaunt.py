# The Gaunt (2026-11-16; Yan: "keep the wendigo-style concept, elongated limbs, canine features, maybe some exposed bones, a real horror feel ... a kind of wendigo that controls other mutants").
# Hand-built with pxart.py (masks shaded as volume, details by hand, pixel by pixel); NOT generated. Facing right like every mutant. Writes ArtSource/mutants_anim/gaunt/gaunt_idle.png and a 6x preview.
#   python3 ArtSource/make_gaunt.py
import os, sys, math
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
HIDE = ramp("#17161a", "#2a2a2c", "#403f3a", "#5d5a4f", "#807b6a")           # ash-grey skin shrunk over the frame
HIDE_FAR = ramp("#0e0e10", "#18181a", "#252522", "#363530", "#4a483f")
BONE = ramp("#4a4332", "#7a7058", "#a89e82", "#cfc6a8", "#ece4cb")
DARK = ramp("#0a0506", "#1a0b0c", "#2d1214", "#431a1c", "#5e2527")          # the opened chest and the mouth: dried blood, almost black
S = (112, 116)
OY = 14                                                                       # everything is drawn at y + OY so the antlers have room above the skull
def Y(pts): return [(x, y + OY) for x, y in pts]
def gaunt(pose='idle'):
    OPEN = 7 if pose == 'scream' else 0                                       # a scream unhinges the jaw further
    L = []; HL = []; cur = [L]                                                          # body layers, head layers (the head is drawn apart so a scream can throw it back)
    def part(build, cols, **kw):
        m = Mask(S); build(m); cur[0].append(shade(m, cols, **kw)); return m
    T = lambda m, pts, w: m.tube(Y(pts), w)
    # far leg (digitigrade, long shin) and far arm (hangs to the ground)
    part(lambda m: T(m, [(34, 60), (41, 74), (34, 88), (37, 96)], [8, 5.4, 3.8, 3]).poly(Y([(33, 95), (47, 95), (47, 97.5), (33, 97.5)])), HIDE_FAR, tex=0.1, seed=1)
    part(lambda m: T(m, [(52, 32), (58, 52), (55, 74), (59, 92)], [6, 4.4, 3.2, 2.6]), HIDE_FAR, tex=0.1, seed=2)
    # the trunk: a narrow pelvis, a barrel of ribs, a hump of shoulder, a long neck
    part(lambda m: m.ell(34, 58 + OY, 7.5, 6.5).ell(40, 44 + OY, 10.5, 15, 18).ell(46, 31 + OY, 8, 7, -20).tube(Y([(48, 29), (58, 21), (66, 19)]), [7, 5, 4.2]), HIDE, tex=0.14, seed=3, depth=3.6, bias=-0.1)
    # the chest laid open: a dark cavity with the ribs standing out of it
    cav = Mask(S); cav.ell(42, 44 + OY, 7.2, 12, 18); L.append(shade(cav, DARK, depth=2.4, tex=0.08, seed=4))
    rib = Mask(S)
    for k in range(6):
        y0 = 31 + k * 4.1; x0 = 36 + k * 0.8; w = 7 + (3 if 1 < k < 5 else 0); rib.tube(Y([(x0, y0), (x0 + w * 0.55, y0 + 1.6), (x0 + w, y0 + 5)]), [1.5, 1.6, 1.0])
    rib.tube(Y([(43, 30), (45, 36), (47, 42), (47, 47)]), [1.2, 1.2, 1.0, 0.8])                   # a breastbone hanging off the front
    L.append(shade(rib, BONE, depth=1.3, rim=True))
    # the spine: knuckles of bone along the back
    sp = Mask(S)
    for i, (x, y) in enumerate([(52, 24), (46, 28), (41, 32), (37, 38), (34, 45), (32, 52), (31, 59)]): sp.ell(x, y + OY, 2.5 - i * 0.12, 2.1)
    L.append(shade(sp, BONE, depth=1.5))
    # near leg and near arm: elbows high, forearms very long, the knuckles almost on the ground
    part(lambda m: T(m, [(37, 58), (45, 72), (38, 86), (42, 96)], [9, 6, 4.2, 3.4]).poly(Y([(40, 95), (56, 95), (56, 97.5), (40, 97.5)])), HIDE, tex=0.12, seed=5, bias=-0.1)
    if pose == 'point':                                                         # the long arm swung out level, one finger straight at the target
        part(lambda m: T(m, [(50, 32), (60, 46), (74, 52), (86, 50)], [6.4, 5, 4, 3.2]), HIDE, tex=0.12, seed=6, bias=-0.1)
        cl = Mask(S); cl.tube(Y([(86, 50), (102, 47)]), [2.0, 0.8]); cl.tube(Y([(86, 51), (94, 56)]), [1.6, 0.8]); cl.tube(Y([(85, 49), (92, 44)]), [1.6, 0.7]); cl.tube(Y([(85, 52), (91, 58)]), [1.6, 0.7])
        L.append(shade(cl, BONE, depth=1.1))
    else:
        part(lambda m: T(m, [(50, 30), (60, 42), (56, 60), (64, 80), (62, 94)], [6.4, 5.2, 4, 3.4, 3]), HIDE, tex=0.12, seed=6, bias=-0.1)
        cl = Mask(S)
        for i, dx in enumerate((-3.4, -1.2, 1.2, 3.4)): cl.tube(Y([(62 + dx * 0.5, 92), (63 + dx * 1.3, 99 + (1.5 if i in (1, 2) else 0))]), [1.8, 0.7])
        L.append(shade(cl, BONE, depth=1.1))
    cl = Mask(S)
    for dx in (-3, 0, 3): cl.tube(Y([(54 + dx, 96.5), (57 + dx * 1.2, 99.5)]), [1.7, 0.7])
    L.append(shade(cl, BONE, depth=1.1))
    cur[0] = HL
    # the skull: a long canine muzzle on a small cranium, a lower jaw hung open
    part(lambda m: m.ell(70, 18 + OY, 6.4, 6, 8).poly(Y([(70, 12), (80, 16), (90, 21), (92, 24), (90, 26), (73, 26), (68, 22)])), BONE, tex=0.1, seed=7, depth=2.8, bias=-0.15)
    jaw = Mask(S); jaw.poly(Y([(71, 26), (92, 27.5), (88, 42 + OPEN), (80, 44 + OPEN), (73, 33)])); HL.append(shade(jaw, ramp("#1c1710", "#352d20", "#5a4e38", "#8a7a5a", "#b3a27a"), depth=1.6, tex=0.1, seed=8))
    mouth = Mask(S); mouth.poly(Y([(72, 26), (91, 26.8), (87, 38 + OPEN), (79, 40 + OPEN), (74, 31)])); HL.append(shade(mouth, DARK, depth=1.0))
    # antlers: two crooked beams of bone with tines, one broken
    ant = Mask(S)
    ant.tube(Y([(66, 13), (62, 5), (58, -3), (57, -8)]), [3.2, 2.6, 2, 1.4]); ant.tube(Y([(62, 5), (55, 3), (50, -1)]), [2, 1.5, 1]); ant.tube(Y([(58, -3), (64, -6), (67, -12)]), [1.8, 1.4, 0.9])
    ant.tube(Y([(70, 11), (70, 2), (76, -5), (74, -10)]), [3, 2.4, 1.8, 1.2]); ant.tube(Y([(70, 2), (78, 1), (82, -3)]), [1.8, 1.3, 0.8]); ant.tube(Y([(76, -5), (70, -8)]), [1.5, 0.8])
    HL.append(shade(ant, BONE, depth=1.3, tex=0.08, seed=9))
    im = paint(S, L); hm = paint(S, HL)
    # the teeth: upper row along the muzzle, long fangs, the lower row and a long dark tongue
    tooth, tooth_d = rgb("#efe7cf"), rgb("#bdb396")
    for x, ln in ((74, 1), (76, 2), (78, 1), (79, 3), (82, 1), (84, 2), (86, 1), (88, 4), (90, 3)): px(hm, [(x, 26 + OY + k) for k in range(ln)], tooth)       # ragged, unequal teeth, a few snapped
    for x, ln in ((77, 2), (80, 1), (83, 3), (85, 1), (87, 2)): px(hm, [(x, 38 + OY - k - (3 if x < 80 else 0)) for k in range(ln)], tooth_d)
    line(hm, 80, 34 + OY, 83, 41 + OY, rgb("#5e1d22")); line(hm, 81, 34 + OY, 84, 40 + OY, rgb("#7b2a30"))      # a long dark tongue hanging out of the jaw
    # the eye: a hollow socket with a dim cold point (pale in the dark, so it reads against the red alarm)
    socket = [(x, y + OY) for x in range(69, 74) for y in range(14, 19)]; px(hm, socket, rgb("#050304")); px(hm, [(70, 14 + OY), (73, 18 + OY), (69, 18 + OY)], BONE[1])
    px(hm, [(71, 16 + OY)], rgb("#e8f2ff")); px(hm, [(72, 16 + OY)], rgb("#9fc4e8"))                       # one cold point deep in the hollow
    px(hm, [(86, 20 + OY), (87, 20 + OY)], rgb("#050304"))                                    # nostril slits
    # hanging strips of hide on the neck and flank, a broken rib (body); a long scar over the muzzle (head)
    line(hm, 66, 26 + OY, 64, 33 + OY, HIDE[2]); line(hm, 68, 27 + OY, 67, 35 + OY, HIDE[1]); line(im, 38, 62 + OY, 36, 70 + OY, HIDE[2])
    line(im, 41, 36 + OY, 50, 33 + OY, BONE[4]); px(im, [(51, 33 + OY)], BONE[4])
    line(hm, 74, 20 + OY, 80, 22 + OY, DARK[1])
    if pose == 'scream': hm = hm.rotate(24, resample=Image.NEAREST, center=(64, 20 + OY))        # the head thrown back, muzzle to the roof
    im.alpha_composite(hm)
    return outline(im)
def head_image(OPEN=0):
    """The skull, jaw, antlers, teeth and eye as one transparent image on the base canvas S (neck anchor at (66, 19 + OY)); the rig (make_gaunt_anims.py) rotates and places it."""
    HL = []
    def lay(build, cols, **kw):
        m = Mask(S); build(m); HL.append(shade(m, cols, **kw))
    lay(lambda m: m.ell(70, 18 + OY, 6.4, 6, 8).poly(Y([(70, 12), (80, 16), (90, 21), (92, 24), (90, 26), (73, 26), (68, 22)])), BONE, tex=0.1, seed=7, depth=2.8, bias=-0.15)
    lay(lambda m: m.poly(Y([(71, 26), (92, 27.5), (88, 42 + OPEN), (80, 44 + OPEN), (73, 33)])), ramp("#1c1710", "#352d20", "#5a4e38", "#8a7a5a", "#b3a27a"), depth=1.6, tex=0.1, seed=8)
    lay(lambda m: m.poly(Y([(72, 26), (91, 26.8), (87, 38 + OPEN), (79, 40 + OPEN), (74, 31)])), DARK, depth=1.0)
    def ant(m):
        m.tube(Y([(66, 13), (62, 5), (58, -3), (57, -8)]), [3.2, 2.6, 2, 1.4]); m.tube(Y([(62, 5), (55, 3), (50, -1)]), [2, 1.5, 1]); m.tube(Y([(58, -3), (64, -6), (67, -12)]), [1.8, 1.4, 0.9])
        m.tube(Y([(70, 11), (70, 2), (76, -5), (74, -10)]), [3, 2.4, 1.8, 1.2]); m.tube(Y([(70, 2), (78, 1), (82, -3)]), [1.8, 1.3, 0.8]); m.tube(Y([(76, -5), (70, -8)]), [1.5, 0.8])
    lay(ant, BONE, depth=1.3, tex=0.08, seed=9)
    hm = paint(S, HL); tooth, tooth_d = rgb("#efe7cf"), rgb("#bdb396")
    for x, ln in ((74, 1), (76, 2), (78, 1), (79, 3), (82, 1), (84, 2), (86, 1), (88, 4), (90, 3)): px(hm, [(x, 26 + OY + k) for k in range(ln)], tooth)
    for x, ln in ((77, 2), (80, 1), (83, 3), (85, 1), (87, 2)): px(hm, [(x, 38 + OY + OPEN - k - (3 if x < 80 else 0)) for k in range(ln)], tooth_d)
    line(hm, 80, 34 + OY, 83, 41 + OY + OPEN, rgb("#5e1d22")); line(hm, 81, 34 + OY, 84, 40 + OY + OPEN, rgb("#7b2a30"))
    px(hm, [(x, y + OY) for x in range(69, 74) for y in range(14, 19)], rgb("#050304")); px(hm, [(70, 14 + OY), (73, 18 + OY), (69, 18 + OY)], BONE[1])
    px(hm, [(71, 16 + OY)], rgb("#e8f2ff")); px(hm, [(72, 16 + OY)], rgb("#9fc4e8")); px(hm, [(86, 20 + OY), (87, 20 + OY)], rgb("#050304"))
    line(hm, 66, 26 + OY, 64, 33 + OY, HIDE[2]); line(hm, 68, 27 + OY, 67, 35 + OY, HIDE[1]); line(hm, 74, 20 + OY, 80, 22 + OY, DARK[1])
    return hm
if __name__ == '__main__':
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'gaunt'); os.makedirs(out, exist_ok=True)
    ims = {p: gaunt(p) for p in ('idle', 'scream', 'point')}
    bb = [im.getbbox() for im in ims.values()]; box = (min(b[0] for b in bb) - 1, min(b[1] for b in bb) - 1, max(b[2] for b in bb) + 1, max(b[3] for b in bb) + 1)    # one canvas for every pose: nothing jumps when it changes
    ims = {p: im.crop(box) for p, im in ims.items()}
    A = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites', 'Mutants')
    os.makedirs(os.path.join(A, 'anim'), exist_ok=True)
    for p, im in ims.items(): im.save(os.path.join(out, 'gaunt_%s.png' % p))
    ims['idle'].save(os.path.join(A, 'mutant_hd_gaunt.png'))                                                # the standing sprite the game loads (UnitSprites.HD)
    for p in ('scream', 'point'):                                                                           # the cutscene poses: PNGs with the .bytes extension, decoded in code (no import setting can blur them)
        tmp = os.path.join(A, 'anim', 'gaunt_%s.png' % p); ims[p].save(tmp); os.replace(tmp, os.path.join(A, 'anim', 'gaunt_%s.bytes' % p))
    W = sum(i.width for i in ims.values()) + 60 * 4; H = max(i.height for i in ims.values()) + 40
    sheet = Image.new('RGBA', (W * 4, H * 4), (26, 14, 14, 255)); x = 40
    for p, im in ims.items():
        big = im.resize((im.width * 4, im.height * 4), Image.NEAREST); sheet.alpha_composite(big, (x, H * 4 - big.height - 40)); x += big.width + 60
    sheet.crop((0, 0, x, H * 4)).save(os.path.join(out, 'gaunt_poses_4x.png')); print('canvas', ims['idle'].size)
