# Verger beast, PART 2: the trunk (Yan, 2026-11-20: "build it part by part"): shoulder hump, deep chest, ribs through a torn flank, spine, narrow waist and hip, thick neck, with the part-1 head placed on it. Gaunt frame (make_gaunt.py) at K=1.5.
# Limbs, coat and scarf come in the next parts. python3 ArtSource/make_verger_beast_body.py -> mutants_anim/boss_concepts/verger_beast_body.png (+ _5x)
import os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
import make_verger_beast as mb
from make_verger_beast import FUR, FURD, BONE, DARK, strands
import make_verger_beast_head as hd
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts')
K = 1.5; OY = 14; S = (250, 210)
import math
POSE = dict(tilt=0.0, pivot=(34, 58), ox=0.0, oy=0.0, head_rot=0.0, jaw=16, scarf_rot=0.0)       # the pose: the trunk, head, coat and scarf lean by `tilt` round the pelvis, `ox/oy` slide everything, the head turns and the jaw opens
LIMB = False                                                                          # True while the limbs are drawn: they are placed by hand and do not follow the tilt
def tf(x, y, tilt=True):
    p = POSE
    if tilt and p['tilt']:
        a = math.radians(p['tilt']); dx, dy = x - p['pivot'][0], y - p['pivot'][1]
        x, y = p['pivot'][0] + dx * math.cos(a) - dy * math.sin(a), p['pivot'][1] + dx * math.sin(a) + dy * math.cos(a)
    return x + p['ox'], y + p['oy']
def XY(x, y, tilt=True):
    a, c = tf(x, y, tilt and not LIMB); return int(round(a * K)), int(round((c + OY) * K))
def Y(pts): return [((lambda q: (q[0] * K, (q[1] + OY) * K))(tf(x, y, not LIMB))) for x, y in pts]
def wd(w): return [v * K for v in w]
def T(m, pts, w): return m.tube(Y(pts), wd(w))
def PL(m, pts): return m.poly(Y(pts))
def E(m, cx, cy, rx, ry, rot=0):
    x, y = tf(cx, cy, not LIMB); return m.ell(x * K, (y + OY) * K, rx * K, ry * K, rot + (0 if LIMB else POSE['tilt']))
MUSC = ramp("#1a1413", "#2f2624", "#4a3d38", "#695850", "#8a7668")        # lit muscle under thin fur
def trunk():
    L = []
    def part(build, cols, fur=0, **kw):
        m = Mask(S); build(m); im = shade(m, cols, **kw); L.append(strands(im, kw.get('seed', 1), fur) if fur else im); return m
    # the mass: pelvis, a deep barrel chest, a heavy shoulder hump, a thick neck
    part(lambda m: (E(m, 34, 58, 8.5, 7.5), E(m, 40, 44, 12.5, 16, 18), E(m, 46, 31, 11, 9, -20), T(m, [(48, 29), (58, 21), (68, 22)], [11, 9, 8])), FUR, fur=460, tex=0.16, seed=3, depth=5, bias=-0.05)
    # muscle: the shoulder blade, the pectoral and the long muscle of the back, lit from the top left
    part(lambda m: E(m, 47, 31, 7, 5.5, -25), MUSC, fur=40, depth=2.4, tex=0.12, seed=21, gain=0.9)
    part(lambda m: E(m, 49, 46, 5.5, 8, 25), MUSC, fur=40, depth=2.2, tex=0.12, seed=22, gain=0.85)
    part(lambda m: T(m, [(50, 27), (43, 31), (38, 38), (35, 46), (33, 54)], [3.4, 3.4, 3.2, 3, 2.6]), MUSC, depth=1.4, tex=0.1, seed=23, gain=0.8)
    # the torn flank: fur gone, a dark wound, five ribs standing out of it, loose strips of hide
    cav = Mask(S); E(cav, 43, 46, 6.2, 10, 18); L.append(shade(cav, DARK, depth=2.4, tex=0.08, seed=4))
    rib = Mask(S)
    for k in range(5): T(rib, [(37.5, 39.5 + k * 3.4), (41.5, 40.5 + k * 3.4), (47, 43.5 + k * 3.4)], [1.4, 1.5, 0.9])
    T(rib, [(46.5, 39), (47.5, 46), (47.5, 53)], [1, 1, 0.8])
    L.append(shade(rib, BONE, depth=1.2))
    hide = Mask(S); PL(hide, [(36, 36), (42, 36), (38, 41)]); PL(hide, [(44, 54), (49, 51), (47, 59)]); L.append(strands(shade(hide, FURD, depth=1.0, tex=0.2, seed=24), 24, 20))
    # the spine: a ridge of shaggy fur with knuckles of bone through it
    tuft = Mask(S)
    for (x, y) in ((54, 22), (49, 25), (44, 28), (39, 33), (35, 40), (32, 48), (31, 56)):
        PL(tuft, [(x - 2.5, y + 1), (x - 3.5, y - 5), (x + 1, y - 1.5), (x + 2, y - 6), (x + 3.5, y)])
    L.append(strands(shade(tuft, FURD, depth=1.4, tex=0.2, seed=5), 5, 80))
    sp = Mask(S)
    for (x, y) in ((50, 24), (45, 27.5), (40, 32), (36, 38.5), (33, 46)): E(sp, x, y, 1.25, 1.1)
    L.append(shade(sp, BONE, depth=1.2))
    # the neck: a thick ruff of fur flowing from the skull into the shoulders
    part(lambda m: PL(m, [(52, 24), (60, 18), (70, 20), (68, 30), (60, 34), (54, 32)]), FUR, fur=70, depth=2, tex=0.2, seed=25)
    return paint(S, L)
def with_head(body):
    h = hd.head(POSE['jaw']); ax, ay = 14, 40                                # the head's neck anchor
    pad = 30; big = Image.new("RGBA", (h.width + 2 * pad, h.height + 2 * pad), (0, 0, 0, 0)); big.alpha_composite(h, (pad, pad))
    if POSE['head_rot']: big = big.rotate(-POSE['head_rot'], resample=Image.NEAREST, center=(ax + pad, ay + pad))      # clockwise positive
    nx, ny = XY(66, 21); nx += 6                                             # where the neck ends on the trunk
    im = body.copy(); im.alpha_composite(big, (nx - ax - pad, ny - ay - pad)); return outline(im)
if __name__ == "__main__":
    b = trunk(); f = crop(with_head(b), 2); f.save(os.path.join(OUT, "verger_beast_body.png"))
    W = Image.new("RGBA", (f.width * 5 + 20, f.height * 5 + 20), (58, 58, 64, 255)); W.alpha_composite(f.resize((f.width * 5, f.height * 5), Image.NEAREST), (10, 10)); W.save(os.path.join(OUT, "verger_beast_body_5x.png")); print(f.size)
