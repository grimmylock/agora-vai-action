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
  # the swipe: reared up and leaning in, the head thrust forward with the jaw wide, the near arm swung high and across, the far arm cocked back, the legs braced
  'attack': dict(pose=dict(tilt=22, pivot=(34, 58), ox=4, oy=0, head_rot=12, jaw=34, scarf_rot=-6),
    J=dict(far=dict(haunch=(36, 62, 8.5, 8.5), leg=[(34, 60), (44, 74), (34, 88), (36, 97)], paw=(39, 98), arm=[(50, 34), (62, 52), (74, 66), (84, 76)], hand=(84, 78)),
           near=dict(haunch=(38, 62, 10.5, 10), leg=[(37, 58), (54, 70), (46, 86), (56, 97)], paw=(59, 98), arm=[(58, 36), (74, 56), (90, 66), (102, 64), (110, 62)], hand=(110, 64)))),
}
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
    bd.POSE.update(dict(tilt=0.0, ox=0.0, oy=0.0, head_rot=0.0, jaw=16)); return im
if __name__ == "__main__":
    cells = {}
    for n in POSES: cells[n] = crop(draw(n), 2); cells[n].save(os.path.join(OUT, "verger_beast_%s.png" % n))
    cells = {'stand': crop(ct.beast(), 2), **cells}
    W = sum(c.width for c in cells.values()) * 4 + 80; Hh = max(c.height for c in cells.values()) * 4 + 40
    sheet = Image.new("RGBA", (W, Hh), (58, 58, 64, 255)); x = 20
    for n, c in cells.items(): sheet.alpha_composite(c.resize((c.width * 4, c.height * 4), Image.NEAREST), (x, Hh - c.height * 4 - 20)); x += c.width * 4 + 20
    sheet.save(os.path.join(OUT, "verger_beast_poses_4x.png")); print({n: c.size for n, c in cells.items()})
