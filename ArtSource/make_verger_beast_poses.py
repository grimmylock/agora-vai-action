# Verger beast: the run and attack poses (Yan, 2026-11-20: "Draw the run and attack poses"), built from the same parts as the standing pose (make_verger_beast_coat.py) with a new pose: the trunk, head, coat and scarf lean round the pelvis, the limbs are placed joint by joint.
# python3 ArtSource/make_verger_beast_poses.py -> mutants_anim/boss_concepts/verger_beast_poses_4x.png (+ verger_beast_run.png, verger_beast_attack.png)
import os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
import make_verger_beast_body as bd
import make_verger_beast_limbs as lm
import make_verger_beast_coat as ct
from make_verger_beast import BONE
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts')
def H(h, w, **kw): return dict(haunch=h, **kw)
POSES = {
  # galloping, all fours, mid-stride, airborne: the body nearly level, the head up and forward, the hind legs thrown back, the forelegs reaching, the coat and scarf streaming behind
  'run': dict(pose=dict(tilt=58, pivot=(34, 58), ox=22, oy=-4, head_rot=-34, jaw=20, scarf_rot=6),
    J=dict(far=dict(haunch=(34, 62, 8.5, 8.5), leg=[(34, 60), (24, 72), (14, 78), (5, 90)], paw=(7, 92), arm=[(60, 52), (72, 60), (84, 68), (94, 80)], hand=(94, 82)),
           near=dict(haunch=(36, 62, 10.5, 10), leg=[(37, 58), (51, 68), (41, 80), (49, 93)], paw=(52, 94), arm=[(62, 54), (64, 68), (58, 82), (62, 94)], hand=(62, 95)))),
  # the change, half done: arched back, the head thrown back in a howl with the jaw split wide, the arms flung out with a man's hands still on them, the legs buckling
  'change': dict(pose=dict(tilt=-6, pivot=(34, 58), ox=16, oy=0, head_rot=-46, jaw=40, scarf_rot=-26),
    J=dict(far=dict(haunch=(36, 62, 8.5, 8.5), leg=[(34, 60), (27, 74), (33, 88), (27, 97)], paw=(30, 98), arm=[(52, 32), (40, 40), (30, 58), (32, 76)], hand=(32, 78)),
           near=dict(haunch=(38, 62, 10.5, 10), leg=[(37, 58), (51, 74), (43, 88), (52, 98)], paw=(55, 99), arm=[(50, 30), (70, 36), (82, 54), (78, 74)], hand=(78, 76)))),
  # the swipe: reared up and leaning in, the head thrust forward with the jaw wide, the near arm swung high and across, the far arm cocked back, the legs braced
  'attack': dict(pose=dict(tilt=22, pivot=(34, 58), ox=4, oy=0, head_rot=12, jaw=34, scarf_rot=-6),
    J=dict(far=dict(haunch=(36, 62, 8.5, 8.5), leg=[(34, 60), (44, 74), (34, 88), (36, 97)], paw=(39, 98), arm=[(50, 34), (62, 52), (74, 66), (84, 76)], hand=(84, 78)),
           near=dict(haunch=(38, 62, 10.5, 10), leg=[(37, 58), (54, 70), (46, 86), (56, 97)], paw=(59, 98), arm=[(58, 36), (74, 56), (90, 66), (102, 64), (110, 62)], hand=(110, 64)))),
}
SKINH = ramp("#2e211e", "#523d36", "#7e6054", "#a88676", "#cca898")      # a man's skin under the fur
def skin_hand(arm, hand, seed, near=True):
    """the change caught half-done: the wrist and hand are still a man's, pale, the fingers splitting at the tips where black claws burst out through the nails"""
    bd.LIMB = True; L = []
    hx, hy = hand; wx, wy = arm[-2]; mx, my = (arm[-3][0] + wx) / 2, (arm[-3][1] + wy) / 2
    m = Mask(bd.S); bd.T(m, [(mx, my), (wx, wy), (hx, hy - 1)], [4.6, 4.2, 3.8]); bd.E(m, hx, hy, 4.3, 3.4)
    for i, dx in enumerate((-3.6, -1.2, 1.2, 3.6)): bd.T(m, [(hx + dx * 0.4, hy + 1), (hx + dx * 1.3, hy + 8 + (1.5 if i in (1, 2) else 0))], [2.4, 1.5])
    L.append(shade(m, SKINH, depth=1.8, tex=0.12, seed=seed))
    cl = Mask(bd.S)
    for i, dx in enumerate((-3.6, -1.2, 1.2, 3.6)): bd.T(cl, [(hx + dx * 1.3, hy + 8 + (1.5 if i in (1, 2) else 0)), (hx + dx * 1.8, hy + 13.5 + (1.5 if i in (1, 2) else 0))], [2.0, 0.7])
    L.append(shade(cl, ramp("#050405", "#0e0c0e", "#1c181a", "#2e282c", "#46404a"), depth=1.0))
    im = paint(bd.S, L)
    for i, dx in enumerate((-3.6, -1.2, 1.2, 3.6)):                                             # blood where each claw splits the finger
        x0, y0 = bd.XY(hx + dx * 1.3, hy + 8 + (1.5 if i in (1, 2) else 0)); px(im, [(x0, y0), (x0 + 1, y0), (x0, y0 - 1)], rgb("#9a1a22"))
    for k in range(5): x0, y0 = bd.XY(mx + k * 0.8, my + k * 1.5); px(im, [(x0, y0)], FURC)       # fur creeping up from the forearm
    bd.LIMB = False; return outline(im)
FURC = rgb("#3d3430")
def shard(cx, cy, w, h, rot, seed):
    """a rag of coat torn off by the change: a jagged triangle with a ripped edge"""
    a = np.radians(rot); pts = [(-w, -h * 0.4), (-w * 0.3, -h * 1.1), (w * 0.5, -h * 0.6), (w, h * 0.2), (w * 0.2, h * 1.2), (-w * 0.6, h * 0.6)]
    pts = [(cx + x * np.cos(a) - y * np.sin(a), cy + x * np.sin(a) + y * np.cos(a)) for x, y in pts]
    bd.LIMB = True; m = Mask(bd.S); bd.PL(m, pts); im = shade(m, ct.COAT, depth=1.4, tex=0.2, seed=seed); bd.LIMB = False; return outline(im)
def hat(cx, cy, rot):
    """his hat, knocked off by the swelling skull: the wide brim, the crown, a band, tumbling above and behind him"""
    bd.LIMB = True; L = []
    brim = Mask(bd.S); bd.E(brim, cx, cy, 15, 4.2, rot); L.append(shade(brim, ct.COAT, depth=1.6, tex=0.14, seed=70))
    crown = Mask(bd.S); bd.E(crown, cx + 1.5 * np.sin(np.radians(rot)), cy - 5.5 * np.cos(np.radians(rot)), 7.5, 6.5, rot); L.append(shade(crown, ct.COAT, depth=2.2, tex=0.14, seed=71))
    band = Mask(bd.S); bd.E(band, cx + 0.7 * np.sin(np.radians(rot)), cy - 2.0 * np.cos(np.radians(rot)), 7.6, 1.6, rot); L.append(shade(band, ct.LEATHER, depth=1.0))
    bd.LIMB = False; return outline(paint(bd.S, L))
def draw(name):
    P = POSES[name]; bd.POSE.update(dict(tilt=0.0, pivot=(34, 58), ox=0.0, oy=0.0, head_rot=0.0, jaw=16)); bd.POSE.update(P['pose'])
    im = ct.beast(P['J'])
    if name == 'attack':                                                                    # the swing: three crescent slashes, thin at the start and thick and white at the claws, each in three bands of brightness
        bd.LIMB = True
        for r0, w0, sweep in ((30, 4.2, 78), (25, 3.4, 70), (20, 2.6, 62)):
            n = 28; ang = lambda t: np.radians(-80 + sweep * t)
            for lo, hi, col in ((0.0, 0.45, BONE[1]), (0.4, 0.75, BONE[3]), (0.7, 1.0, rgb("#fffaf0"))):
                outer = [(82 + (r0 + w0 * t) * np.cos(ang(t)), 90 + (r0 + w0 * t) * np.sin(ang(t))) for t in np.linspace(lo, hi, n)]
                inner = [(82 + (r0 - 0.4) * np.cos(ang(t)), 90 + (r0 - 0.4) * np.sin(ang(t))) for t in np.linspace(hi, lo, n)]
                m = Mask(bd.S); bd.PL(m, outer + inner); im.alpha_composite(shade(m, [col] * 5, depth=0.5, rim=False))
        bd.LIMB = False
    if name == 'change':
        J = P['J']
        for (cx, cy, w, h, rot, sd) in ((20, 30, 7, 3.2, 30, 72), (10, 44, 8, 3.4, -20, 73), (22, 58, 6, 2.6, 60, 74), (4, 64, 7, 3.0, 10, 75), (46, 8, 5, 2.4, -40, 76)):
            im.alpha_composite(shard(cx + bd.POSE['ox'] - 16, cy, w, h, rot, sd))
        im.alpha_composite(skin_hand(J['near']['arm'], J['near']['hand'], 80)); im.alpha_composite(skin_hand(J['far']['arm'], J['far']['hand'], 81, False))
        im.alpha_composite(hat(30 + bd.POSE['ox'] - 16 - 6, -2, -28))
    bd.POSE.update(dict(tilt=0.0, ox=0.0, oy=0.0, head_rot=0.0, jaw=16)); return im
if __name__ == "__main__":
    cells = {}
    for n in POSES: cells[n] = crop(draw(n), 2); cells[n].save(os.path.join(OUT, "verger_beast_%s.png" % n))
    cells = {'stand': crop(ct.beast(), 2), **cells}; cells = {k: cells[k] for k in ('stand', 'change', 'run', 'attack')}
    W = sum(c.width for c in cells.values()) * 4 + 80; Hh = max(c.height for c in cells.values()) * 4 + 40
    sheet = Image.new("RGBA", (W, Hh), (58, 58, 64, 255)); x = 20
    for n, c in cells.items(): sheet.alpha_composite(c.resize((c.width * 4, c.height * 4), Image.NEAREST), (x, Hh - c.height * 4 - 20)); x += c.width * 4 + 20
    sheet.save(os.path.join(OUT, "verger_beast_poses_4x.png")); print({n: c.size for n, c in cells.items()})
