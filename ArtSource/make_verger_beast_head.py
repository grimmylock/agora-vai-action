# Verger beast, PART 1: the head (Yan, 2026-11-20: "build it part by part"). Faces right; local canvas 72x64, neck anchor at (14, 40). Hand-placed like the Gaunt's skull (make_gaunt.py head_image).
# python3 ArtSource/make_verger_beast_head.py -> mutants_anim/boss_concepts/verger_beast_head.png (+ _8x)
import os, sys, math, random
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
from make_verger_beast import FUR, FURD, BONE, DARK, TAN, LEATHER, RED, BRASS, strands
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts')
S = (72, 64)
SKINN = ramp("#050304", "#0d0809", "#18100f", "#271b19", "#3a2a27")      # the nose, the lips
def rot(pts, ang, c): 
    a = math.radians(ang); return [(c[0] + (x - c[0]) * math.cos(a) - (y - c[1]) * math.sin(a), c[1] + (x - c[0]) * math.sin(a) + (y - c[1]) * math.cos(a)) for x, y in pts]
def head(open_deg=16):
    L = []
    def part(build, cols, fur=0, **kw):
        m = Mask(S); build(m); im = shade(m, cols, **kw); L.append(strands(im, kw.get('seed', 1), fur) if fur else im); return m
    hinge = (26, 36)
    # far ear and far cheek ruff, behind everything
    part(lambda m: m.poly([(14, 16), (9, -2), (24, 9)]), FURD, fur=14, depth=1.4, seed=1)
    part(lambda m: m.poly([(10, 30), (2, 38), (8, 40), (4, 48), (14, 44), (20, 50), (22, 38)]), FURD, fur=30, depth=1.6, seed=2)
    # the lower jaw, dropped open: a deep chin, a bony underside, fur at the throat
    jaw = [(27, 34), (52, 33), (56, 36), (51, 43), (42, 47), (31, 45), (24, 41)]
    part(lambda m: m.poly(rot(jaw, open_deg, hinge)), FURD, fur=40, depth=2.6, tex=0.12, seed=3)
    # the gum and the inside of the mouth
    part(lambda m: m.poly(rot([(30, 34), (53, 33), (50, 38), (40, 41), (32, 40)], open_deg, hinge)), DARK, depth=1.2, tex=0.08, seed=4)
    part(lambda m: m.poly(rot([(31, 38), (44, 38), (47, 41), (38, 44)], open_deg, hinge)), ramp("#2a0a10", "#4a1018", "#6e1c26", "#8a2a34", "#a83c46"), depth=1.0)       # the tongue
    # the skull: a domed cranium, a heavy brow, a long arched muzzle, a cheek
    part(lambda m: (m.ell(21, 24, 11, 10.5, 8), m.poly([(24, 16), (33, 14), (45, 19), (57, 25), (59, 29), (56, 33), (30, 33), (22, 30)]), m.ell(30, 28, 6, 6)), FUR, fur=170, depth=4.4, tex=0.16, seed=5, bias=-0.05)
    part(lambda m: m.poly([(23, 15), (37, 14), (38, 20), (28, 21), (22, 19)]), FURD, fur=24, depth=1.6, seed=6)                                    # the brow ridge, a shadow over the eye
    part(lambda m: m.poly([(15, 13), (12, -6), (25, -3), (31, 14)]), FUR, fur=30, depth=2.0, seed=7)                                           # the near ear, tall and pointed
    part(lambda m: m.poly([(17, 10), (15, -1), (23, 0), (26, 11)]), ramp("#1a0a0c", "#34141a", "#4e2028", "#6a3038", "#823c46"), depth=1.2)       # the inside of it
    # the ruff: shaggy fur at the jaw hinge and the throat, falling toward the chest
    part(lambda m: m.poly([(6, 24), (0, 34), (7, 33), (3, 44), (11, 40), (10, 52), (17, 44), (22, 54), (25, 42), (26, 30), (16, 22)]), FUR, fur=90, depth=2, tex=0.2, seed=8)
    im = paint(S, L); X = lambda v: int(round(v))
    def P(x, y, c): px(im, [(X(x), X(y))], c)
    # the nose: a wet dark pad with a highlight and two slits
    px(im, [(X(x), X(y)) for x in range(55, 61) for y in range(24, 31) if (x - 57.5) ** 2 / 9 + (y - 27) ** 2 / 14 < 1], SKINN[1]); P(57, 25, SKINN[4]); P(58, 25, SKINN[3]); px(im, [(58, 28), (59, 28)], SKINN[0]); px(im, [(56, 29), (57, 29)], SKINN[0])
    # the lip: a dark line along the upper jaw, a curled-back snarl above the teeth, a few wrinkles
    line(im, 33, 33, 57, 32, SKINN[0]); line(im, 33, 32, 56, 31, SKINN[2])
    for k in range(3): line(im, 44 + k * 3, 27 + k, 47 + k * 3, 29 + k, FURD[0])
    # teeth: the upper row with the long canine and the carnassials behind it, the lower row with its canine; the jaw is open so the lower row is rotated with it
    tooth, tooth_d, tooth_s = rgb("#efe7cf"), rgb("#bdb396"), rgb("#8c8268")
    up = [(35, 3), (38, 3), (41, 4), (44, 3), (47, 5), (50, 3), (53, 7), (55, 3)]                           # (x, length)
    for x, ln in up:
        for k in range(ln): P(x, 33 + k, tooth if k < ln - 1 else tooth_d)
        P(x + 1, 33, tooth_s)
    lowx = [(34, 3), (38, 2), (42, 3), (46, 2), (49, 6), (52, 2)]
    for x, ln in lowx:
        pts = rot([(x, 34 - k) for k in range(ln)], open_deg, hinge)
        for i, (a, b) in enumerate(pts): P(a, b, tooth_d if i == ln - 1 else tooth)
    # the eye: a hollow under the brow, a burning gold point, a lower lid
    px(im, [(X(x), X(y)) for x in range(26, 33) for y in range(21, 26)], SKINN[0]); px(im, [(27, 21), (31, 25), (26, 25)], FURD[2])
    P(29, 23, rgb("#ffe07a")); P(30, 23, rgb("#ffd05a")); P(30, 24, rgb("#c8902a")); P(29, 22, rgb("#b07a20"))
    # scars: three old claw lines across the bridge and a notch torn from the ear
    for k in range(3): line(im, 38 + k * 3, 17 + k, 42 + k * 3, 24 + k, BONE[2]); line(im, 39 + k * 3, 17 + k, 43 + k * 3, 24 + k, BONE[3])
    px(im, [(24, 3), (25, 4), (25, 3)], (0, 0, 0, 0)); px(im, [(24, 4)], FURD[0])
    # the leather mask, still on: a stitched cup over the upper muzzle with a torn lower edge (the fangs burst through), a strap round the head under the eye, a brass buckle, a seam down the nose
    strap = Mask(S); strap.tube([(38, 27), (30, 29), (20, 28), (10, 25)], [3.2, 3.2, 3.2, 3.0]); im.alpha_composite(shade(strap, LEATHER, depth=1.0, tex=0.2, seed=9))
    cup = Mask(S); cup.poly([(36, 17), (46, 18), (55, 23), (57, 27), (56, 31), (52, 30), (50, 32), (46, 30), (43, 32), (39, 30), (36, 31), (34, 25)]); im.alpha_composite(shade(cup, TAN, depth=2.2, tex=0.24, seed=10))
    seam = [(x, 21 + (x - 38) * 0.45) for x in range(38, 56)]; px(im, [(X(x), X(y)) for x, y in seam[::2]], LEATHER[1])             # the stitched seam along the top
    for k in range(5): px(im, [(40 + k * 3, 19 + k), (40 + k * 3, 18 + k)], LEATHER[0])                                                    # stitches
    line(im, 45, 20, 45, 30, LEATHER[1]); line(im, 46, 20, 46, 30, TAN[4])                                                                   # a fold where it strains over the muzzle
    px(im, [(40, 26), (41, 26), (40, 27), (41, 27)], BRASS[4]); px(im, [(40, 27), (41, 27)], BRASS[2])                                   # the buckle where the strap meets the cup
    px(im, [(52, 31), (53, 31), (53, 32)], FURD[1])                                                                                         # a torn gap
    for (x, y) in ((8, 26), (7, 29), (9, 31)): line(im, x, y, x - 4, y + 4, LEATHER[2])                                                      # the strap's loose ends flapping behind the ear
    # blood on the muzzle, the teeth and the flap
    for (x, y) in ((53, 34), (49, 36), (35, 42), (56, 33), (41, 47)): P(x, y, RED[2]); P(x, y + 1, RED[1])
    return outline(im)
if __name__ == "__main__":
    h = head(); h.save(os.path.join(OUT, "verger_beast_head.png"))
    W = Image.new("RGBA", (h.width * 9 + 20, h.height * 9 + 20), (58, 58, 64, 255)); W.alpha_composite(h.resize((h.width * 9, h.height * 9), Image.NEAREST), (10, 10)); W.save(os.path.join(OUT, "verger_beast_head_9x.png"))
