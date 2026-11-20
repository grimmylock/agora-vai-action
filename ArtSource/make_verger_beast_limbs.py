# Verger beast, PART 3 + 4: the legs and the arms (Yan, 2026-11-20: "continue with the legs and arms"). Gaunt frame at K=1.5: haunch and long digitigrade shin, a paw with three toes and bone claws; a long arm with a heavy shoulder, a thin forearm, a fur-backed hand with long fingers, knuckles near the ground.
# Order: far leg and arm, the trunk (part 2), near leg and arm, the head (part 1). Coat, scarf and belt are part 5. python3 ArtSource/make_verger_beast_limbs.py
import os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
from make_verger_beast import FUR, FURD, BONE, DARK, strands
import make_verger_beast_body as bd
from make_verger_beast_body import K, OY, S, Y, wd, T, PL, E, MUSC
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts')
CLAW = ramp("#2a2620", "#524a3e", "#8a7e6a", "#c4b8a0", "#ece2cc")
def limb(cols, haunch, leg, lw, paw, arm, aw, hand, near, seed):
    L = []
    def part(build, c, fur=0, **kw):
        m = Mask(S); build(m); im = shade(m, c, **kw); L.append(strands(im, kw.get('seed', 1), fur) if fur else im); return m
    s = seed
    part(lambda m: (E(m, *haunch), T(m, leg, lw)), cols, fur=150 if near else 90, tex=0.14, seed=s, depth=4, bias=-0.05)                     # the haunch and the leg: thigh, knee, a thin shin, a long foot
    if near: part(lambda m: E(m, haunch[0] + 3, haunch[1] + 4, 9.5, 5, 58), MUSC, fur=40, depth=2.6, tex=0.16, seed=s + 1, gain=0.8)   # the lit thigh muscle
    px_, py_ = paw                                                                                                                              # the paw: a pad and three toes, each ending in a bone claw
    part(lambda m: (E(m, px_, py_, 5.5, 2.6), [T(m, [(px_ + 2, py_), (px_ + 9 + i, py_ + 1.8 + i * 0.8)], [3.0, 2.2]) for i in range(3)]), cols, fur=30, depth=1.6, tex=0.1, seed=s + 2)
    cl = Mask(S)
    for i in range(3): T(cl, [(px_ + 9 + i, py_ + 1.8 + i * 0.8), (px_ + 12.5 + i, py_ + 3 + i * 0.8)], [1.7, 0.6])
    L.append(shade(cl, CLAW, depth=1.0))
    part(lambda m: T(m, arm, aw), cols, fur=150 if near else 80, tex=0.14, seed=s + 3, depth=3.4, bias=-0.05)                                      # the arm
    if near: part(lambda m: T(m, arm[:3], [aw[0] * 0.7, aw[1] * 0.7, aw[2] * 0.7]), MUSC, fur=20, depth=1.8, tex=0.1, seed=s + 4, gain=0.82)          # the lit muscle on the upper arm
    hx, hy = hand                                                                                                                               # the hand: a fur-backed palm, four long fingers, bone claws
    part(lambda m: (E(m, hx, hy, 4.8, 3.6), [T(m, [(hx + dx * 0.4, hy + 1), (hx + dx * 1.3, hy + 8 + (1.5 if i in (1, 2) else 0))], [2.8, 1.6]) for i, dx in enumerate((-3.6, -1.2, 1.2, 3.6))]), cols, fur=26, depth=1.6, tex=0.1, seed=s + 5)
    cl = Mask(S)
    for i, dx in enumerate((-3.6, -1.2, 1.2, 3.6)): T(cl, [(hx + dx * 1.3, hy + 8 + (1.5 if i in (1, 2) else 0)), (hx + dx * 1.5, hy + 11.5 + (1.5 if i in (1, 2) else 0))], [1.7, 0.6])
    L.append(shade(cl, CLAW, depth=1.0))
    im = paint(S, L)
    return outline(im, DARK[0]) if near else im                                                          # a dark edge on the near limbs so they stand off the body
def beast_bare():
    far = limb(FURD, (36, 62, 8.5, 8.5), [(34, 60), (41, 74), (34, 88), (38, 96)], [12, 8, 5, 4], (41, 97), [(52, 32), (58, 52), (55, 74), (59, 92)], [9, 6.4, 4.6, 3.8], (59, 94), False, 40)
    near = limb(FUR, (38, 62, 10.5, 10), [(37, 58), (45, 72), (38, 86), (42, 96)], [15, 10, 6, 4.8], (45, 97.5), [(50, 30), (60, 42), (56, 60), (64, 80), (62, 94)], [11, 9, 6.4, 5.4, 4.6], (62, 95), True, 60)
    im = Image.new("RGBA", S, (0, 0, 0, 0)); im.alpha_composite(far); im.alpha_composite(bd.trunk()); im.alpha_composite(near)
    return bd.with_head(im)
if __name__ == "__main__":
    f = crop(beast_bare(), 2); f.save(os.path.join(OUT, "verger_beast_limbs.png"))
    W = Image.new("RGBA", (f.width * 5 + 20, f.height * 5 + 20), (58, 58, 64, 255)); W.alpha_composite(f.resize((f.width * 5, f.height * 5), Image.NEAREST), (10, 10)); W.save(os.path.join(OUT, "verger_beast_limbs_5x.png")); print(f.size)
