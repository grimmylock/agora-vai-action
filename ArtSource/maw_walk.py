# Blind Maw walk cycle, second drawing (2026-11-11). Yan on the first (flat polygons): "the new animations look good and expressive but the quality of the sprite is a lot lower ... the old
# sprite was full of realistic curves and contour, the new one is basically all triangles" and "does he need to be that big, can we reduce his size (not sprite count or quality) by about 50%?"
# So: the same walk (explicit foot paths, two-bone legs, diagonal gait, jaw and head sway) but every part is a smooth VOLUME built with pxart (supersampled masks -> a dome height field ->
# a 5-step ramp lit from the top left with a dithered band edge, a rim light, a fleshy mottle), the sprite is drawn with the old sprite's palette, and it is drawn at 88x60 with a
# body ~68 px long: SHOWN AT HALF SIZE (see SHOW_SCALE: one sprite pixel = a quarter map pixel instead of half), so the pixel detail is kept and he is about 1.1 tiles long, not 2.2.
#   python3 maw_walk.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
import pxart as P

class KMask(P.Mask):
    """a pxart mask whose shapes are given in design units"""
    def __init__(self, size=None): super().__init__((W, H))
    def ell(self, cx, cy, rx, ry, rot=0.0): return super().ell(cx * K, cy * K, rx * K, ry * K, rot)
    def poly(self, pts): return super().poly([(x * K, y * K) for x, y in pts])
    def tube(self, pts, widths): return super().tube([(x * K, y * K) for x, y in pts], [w * K for w in widths])

def kpx(im, pts, col): P.px(im, [(int(round(x * K)), int(round(y * K))) for x, y in pts], col)
def kline(im, x0, y0, x1, y1, col): P.line(im, round(x0 * K), round(y0 * K), round(x1 * K), round(y1 * K), col)
def kshade(mask, ramp, **kw):
    if 'depth' in kw: kw['depth'] = max(1.2, kw['depth'] * K)
    return P.shade(mask, ramp, **kw)

K = 1.0                                    # drawn at the full 92x62 (a 0.74 try at 68x46 lost the detail: teeth became a white blob, legs blobs); the size reduction is in how he is SHOWN, see SHOW_DENSITY
DW, DH, DGROUND = 92, 62, 56               # the design grid; every coordinate below is in design units and is scaled by K when it is drawn
W, H, GROUND = int(round(DW * K)), int(round(DH * K)), int(round(DGROUND * K))
SHOW_DENSITY = 5.4                         # sprite pixels per MAP pixel in game (the HD mutants are 2.0): his ~89 px body is then ~16.5 map px = 1.0 tile long, half of the first version (33 map px = 2.1 tiles), with every pixel kept
N = 8
BODY = P.ramp("#150a0e", "#3a111a", "#5e2229", "#8d3a3b", "#bd6559")             # the old sprite's maroons and flesh, dark -> light
FAR = P.ramp("#0c0507", "#220c11", "#3a141b", "#59222a", "#7a3438")
LEG = P.ramp("#150a0e", "#40141d", "#6a2a2e", "#a04a4a", "#d0786f")
GUM = P.ramp("#0a0204", "#2a060d", "#5a1019", "#8f2a30", "#c4565a")
TEETH, TEETH_D, INK, CLAW = P.rgb("#e8d9c2"), P.rgb("#a8947c"), P.rgb("#0a0204"), P.rgb("#cbbba3")
LEGS = {"far_hind": (26, 36, 0.50, True, False), "near_hind": (18, 38, 0.00, False, False), "far_fore": (50, 36, 0.00, True, True), "near_fore": (42, 38, 0.50, False, True)}
L1, L2 = 11.0, 11.0
STRIDE, LIFT = 8.0, 6.0

def foot(ph):
    if ph < 0.55: return (STRIDE - 2 * STRIDE * ph / 0.55, 0.0)
    u = (ph - 0.55) / 0.45
    return (-STRIDE + 2 * STRIDE * (0.5 - 0.5 * math.cos(math.pi * u)), LIFT * math.sin(math.pi * u))

def solve(hip, ft, fwd):
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), L1 + L2 - 0.4); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (L1 * L1 + d * d - L2 * L2) / (2 * L1 * d)))); k = a + (ang if fwd else -ang)
    return (hip[0] + L1 * math.cos(k), hip[1] + L1 * math.sin(k))

def leg_img(name, i, bob):
    hx, hy, ph0, far, fwd = LEGS[name]; ph = (i / N + ph0) % 1.0; fo, fl = foot(ph)
    hip = (hx, hy + bob); ft = (hx + fo + (3 if fwd else -2), DGROUND - fl); kn = solve(hip, ft, fwd)
    m = KMask(); m.tube([hip, kn, ft], [13, 9.5, 7.5]); m.ell(kn[0], kn[1], 5.2, 5.2)                      # a fat thigh, a knuckle, a tapering shin
    m.ell(ft[0] + 1.5, ft[1] - 0.5, 5.5, 2.6)                                                                    # the pad
    im = kshade(m, FAR if far else LEG, depth=3.6, tex=0.16, seed=hash(name) % 97, gain=0.8 if far else 1.0, bias=-0.2 if far else -0.05)
    for dx in (0, 2.4, 4.6):                                                                                     # three claws reaching forward from the pad
        kline(im, ft[0] + 3 + dx * .5, ft[1] + 1, ft[0] + 6 + dx, ft[1] + 2, CLAW if not far else TEETH_D)
    return P.outline(im, INK if not far else (4, 1, 2, 255))                                                      # every limb gets its own dark contour, so a leg parts from the body behind it

def body_img(i, bob, gape, sway):
    b = bob; layers = []
    def part(mask, ramp, **kw): kw.setdefault('gain', 0.82); kw.setdefault('bias', -0.2); layers.append(kshade(mask, ramp, **kw))
    rump = KMask().ell(10, 30 + b, 12, 12, -20); part(rump, BODY, depth=5, tex=0.2, seed=3)                # haunch: a heavy dome on the left
    back = KMask().ell(30, 25 + b, 25, 15.5, -4); part(back, BODY, depth=7, tex=0.22, seed=5)               # the hunched back
    for k, (x, y, r) in enumerate(((16, 12, 3.2), (23, 10, 3.6), (31, 9.6, 3.9), (39, 11, 3.4), (46, 14, 2.8))):   # a spine of knobs
        part(KMask().ell(x, y + b, r, r * 0.9), BODY, depth=2.4, tex=0.1, seed=11 + k)
    sh = KMask().ell(46, 29 + b, 11, 12, 10); part(sh, BODY, depth=5, tex=0.18, seed=7)                    # the shoulder muscle: lit
    for k in range(3):                                                                                           # ribs: long soft grooves under the skin
        im = layers[1]; kline(im, 20 + k * 6, 22 + b, 23 + k * 6, 34 + b, BODY[1]); kline(im, 21 + k * 6, 22 + b, 24 + k * 6, 34 + b, BODY[3])
    hx, hy = 54 + sway, 17 + b
    skull = KMask().ell(hx + 8, hy + 6, 14, 9.5, -6).poly([(hx + 10, hy - 2), (hx + 28, hy + 2), (hx + 31, hy + 9), (hx + 16, hy + 14)])      # a heavy skull running out into a blunt snout
    part(skull, BODY, depth=5.5, tex=0.2, seed=13)
    brow = KMask().ell(hx + 15, hy + 3.5, 6, 2.6, -8); part(brow, BODY, depth=2.2)                       # a swollen brow ridge...
    seam = layers[-2]
    for (dx, dy) in ((11, 6), (12, 7), (13, 7), (14, 7), (15, 7), (16, 6)): kpx(seam, [(hx + dx, hy + dy)], INK)   # ...over a blind, sewn-shut eye: a dark curved seam
    for (dx, dy) in ((12, 8), (14, 8)): kpx(seam, [(hx + dx, hy + dy)], BODY[3])
    # the maw: the mouth line runs along the upper jaw; the jaw hinges at the back of it; between them, black, a tongue
    lip = [(hx + 11, hy + 12), (hx + 31, hy + 10)]; hinge = (hx + 11, hy + 14)
    a = math.radians(16 + 9 * (1 + gape)); Lj = 21
    tip = (hinge[0] + Lj * math.cos(a), hinge[1] + Lj * math.sin(a))
    inner = KMask().poly([lip[0], lip[1], (tip[0] - 1, tip[1] - 3), hinge]); layers.append(kshade(inner, GUM, depth=2.2, rim=False, bias=-0.7))
    tongue = KMask().ell((lip[0][0] + tip[0]) / 2 - 1, (lip[0][1] + tip[1]) / 2 + 3, 7, 2.6, 8); part(tongue, GUM, depth=1.8)
    jaw = KMask().tube([hinge, tip], [7.5, 5.5]); part(jaw, BODY, depth=3.2, tex=0.16, seed=17)           # the lower jaw: a real jaw, round in section
    teeth = P.paint((W, H), [])
    def tpoly(pts, col): ImageDraw.Draw(teeth).polygon([(px_ * K, py_ * K) for px_, py_ in pts], fill=col)
    for k in range(5):                                                                                           # upper fangs: hanging along the lip, spaced so the black shows between, the middle ones longest
        t = k / 4.0; x = lip[0][0] + 3 + t * 17; y = lip[0][1] + (lip[1][1] - lip[0][1]) * t; ln = 3.0 + 4.2 * math.sin(math.pi * (0.12 + 0.76 * t)); w = 1.5
        tpoly([(x - w, y), (x + w, y - 0.4), (x + 0.6, y + ln)], TEETH); kpx(teeth, [(x - w + 0.8, y + 0.4)], TEETH_D)
    for k in range(4):                                                                                           # lower teeth: rising from the jaw, shorter, set between the upper ones
        t = (k + 0.5) / 4.0; x = hinge[0] + (tip[0] - hinge[0]) * (0.12 + 0.78 * t); y = hinge[1] + (tip[1] - hinge[1]) * (0.12 + 0.78 * t) - 2.6; ln = 2.4 + 2.2 * math.sin(math.pi * (0.1 + 0.8 * t)); w = 1.4
        tpoly([(x - w, y), (x + w, y - 0.3), (x + 0.4, y - ln)], TEETH)
    layers.append(teeth)
    return P.paint((W, H), layers)

def frame(i, only=None):
    bob = int(round(1.0 * math.sin(2 * math.pi * (i / N * 2) + 0.6)))
    gape = math.sin(2 * math.pi * i / N + 1.2); sway = int(round(1.0 * math.sin(2 * math.pi * i / N + 2.0)))
    far = [n for n in LEGS if LEGS[n][3]]; near = [n for n in LEGS if not LEGS[n][3]]
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    if only: im.alpha_composite(leg_img(only, i, bob)); return im
    for n in far: im.alpha_composite(leg_img(n, i, bob))
    im.alpha_composite(body_img(i, bob, gape, sway))
    for n in near: im.alpha_composite(leg_img(n, i, bob))
    return P.outline(im, INK)

def leg_check():
    res = {}
    for n in LEGS:
        fr = [np.array(frame(i, only=n))[..., 3] > 0 for i in range(N)]
        xs = [np.where(f.any(axis=0))[0] for f in fr]; ys = [np.where(f.any(axis=1))[0] for f in fr]
        res[n] = dict(distinct_states=len({f.tobytes() for f in fr}), min_pixels_changed_in_a_step=min(int((fr[i] ^ fr[(i + 1) % N]).sum()) for i in range(N)),
                      foot_reach_px=int(max(x.max() for x in xs) - min(x.max() for x in xs)), foot_lift_px=int(max(y.max() for y in ys) - min(y.max() for y in ys)))
    return res

def save(out):
    os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"blind_maw_walk_{i}.png"))
    strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
    for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(out, "blind_maw_walk_strip.png"))
    sc = 5; sheet = Image.new("RGBA", (W * 4 * sc, H * 2 * sc), (105, 115, 90, 255))
    for i, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((i % 4) * W * sc, (i // 4) * H * sc))
    sheet.convert("RGB").save(os.path.join(out, "blind_maw_walk_sheet.png")); return fr

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "_out/maw_walk"; save(out); print(json.dumps(leg_check(), indent=1))
