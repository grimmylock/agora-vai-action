# Blind Maw walk cycle, drawn fresh (2026-11-11). Yan: "try it yourself one more time, only the blind maw ... he's probably the harder one because of his stubby legs and sprite angle.
# Change whatever you need to change about him to make it work (angle, size, whatever)". So this is a REDRAW, not a re-posing of his sprite: the old one hides its legs under a black mass.
# Same animal and palette (sampled from the original), bigger (64x44 instead of 48x32), seen from the side with the camera a little above so all four stubby legs show:
# a low hunched body, a huge blind head with a great open maw, four thick two-segment legs. Each foot follows an explicit path (planted and sliding back at a constant speed for
# half the cycle, lifted and swung forward for the other half) and a two-bone solve puts the knee; diagonal gait (near fore + far hind, then the other pair).
#   python3 maw_walk.py <out_dir>
import math, os, sys, json
from PIL import Image, ImageDraw
import numpy as np

W, H, GROUND = 72, 46, 41
PAL = dict(
    k=(4, 1, 2), d=(24, 15, 18), m=(63, 18, 27), r=(98, 44, 44), rr=(115, 31, 43), f=(167, 80, 81), fl=(190, 104, 100), t=(212, 194, 178), td=(150, 128, 112), mo=(36, 6, 12), hl=(133, 86, 78))
N = 8                                   # frames per cycle
LEGS = {                                # name: (hip x, hip y, phase, is_far, forward-knee?)
    "far_hind": (21, 27, 0.50, True, False), "near_hind": (14, 28, 0.00, False, False),
    "far_fore": (40, 27, 0.00, True, True), "near_fore": (34, 28, 0.50, False, True)}
L1, L2 = 7.0, 7.0                       # thigh, shin
STRIDE, LIFT = 6.0, 4.0                 # half-stride (px each way from under the hip), foot lift

def foot(ph):
    """foot offset from under the hip for cycle phase ph in 0..1: stance 0..0.55 slides back linearly, swing 0.55..1 lifts and returns."""
    if ph < 0.55: return (STRIDE - 2 * STRIDE * ph / 0.55, 0.0)
    u = (ph - 0.55) / 0.45
    return (-STRIDE + 2 * STRIDE * (0.5 - 0.5 * math.cos(math.pi * u)), LIFT * math.sin(math.pi * u))

def solve(hip, ft, fwd):
    """two-bone solve: knee for a hip and a foot, bending forward or back."""
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), L1 + L2 - 0.3); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (L1 * L1 + d * d - L2 * L2) / (2 * L1 * d))))
    k = a + (-ang if fwd else ang) if False else a + (ang if fwd else -ang)
    return (hip[0] + L1 * math.cos(k), hip[1] + L1 * math.sin(k))

def thick(d, p, q, w, col):
    d.line([p, q], fill=col, width=w)
    for pt in (p, q): d.ellipse([pt[0] - w / 2 + .3, pt[1] - w / 2 + .3, pt[0] + w / 2 - .3, pt[1] + w / 2 - .3], fill=col)

def draw_leg(im, name, i, bob):
    hx, hy, ph0, far, fwd = LEGS[name]; ph = (i / N + ph0) % 1.0
    fo, fl = foot(ph); hip = (hx, hy + bob); ft = (hx + fo + (2 if fwd else -1), GROUND - fl)
    kn = solve(hip, ft, fwd); d = ImageDraw.Draw(im)
    base, lite, dark = (PAL["m"], PAL["r"], PAL["k"]) if far else (PAL["rr"], PAL["f"], PAL["d"])
    thick(d, hip, kn, 8, PAL["k"]); thick(d, kn, ft, 7, PAL["k"])                                                # a dark rim round the limb so it parts from the body
    thick(d, hip, kn, 6, base); thick(d, kn, ft, 5, base)
    thick(d, (hip[0] - 1, hip[1] - 1), (kn[0] - 1, kn[1] - 1), 2, lite)                                       # the lit edge of the thigh
    d.line([(kn[0] - 1, kn[1] - 1), (ft[0] - 1, ft[1] - 2)], fill=lite)
    d.ellipse([kn[0] - 2, kn[1] - 2, kn[0] + 2, kn[1] + 2], fill=lite if not far else base); d.point((kn[0] - 1, kn[1] - 1), fill=PAL["hl"] if not far else lite)   # the knuckle
    d.rectangle([ft[0] - 3, ft[1] - 1, ft[0] + 3, ft[1] + 1], fill=dark)                                       # the pad, flat on the ground
    for k in (-3, 0, 3):                                                                                        # three claws, pale, reaching forward
        d.line([(ft[0] + k * 0.5 + 2, ft[1]), (ft[0] + k * 0.5 + 4, ft[1] + 1)], fill=PAL["td"] if far else PAL["t"])

def draw_body(im, i, bob, gape, sway):
    d = ImageDraw.Draw(im); b = bob
    # the hunched back and haunches: a big dark mass, shaded from the top left, sitting HIGH enough that the legs show under it
    d.ellipse([6, 8 + b, 44, 31 + b], fill=PAL["m"])
    d.ellipse([9, 9 + b, 40, 27 + b], fill=PAL["r"])
    d.ellipse([12, 10 + b, 33, 20 + b], fill=PAL["rr"])
    for (x, y) in ((16, 13), (22, 11), (28, 15), (34, 18), (20, 18), (26, 21)): d.point((x, y + b), fill=PAL["f"])   # wet highlights
    for k in range(5): d.polygon([(12 + k * 6, 10 + b + (k % 2)), (15 + k * 6, 7 + b + (k % 2)), (17 + k * 6, 10 + b + (k % 2))], fill=PAL["m"])   # knobbly ridge along the spine
    for k in range(3): d.line([(18 + k * 6, 16 + b), (20 + k * 6, 25 + b)], fill=PAL["m"])                       # ribs under the skin
    d.ellipse([3, 17 + b, 14, 30 + b], fill=PAL["d"])                                                              # the rump, darker
    hx, hy = 38 + sway, 11 + b
    # the head: a heavy skull wedge from the shoulders to a blunt face; the lower jaw hangs below it, opened by `gape`
    d.polygon([(hx - 4, hy + 2), (hx + 10, hy - 3), (hx + 22, hy + 1), (hx + 24, hy + 8), (hx + 6, hy + 10)], fill=PAL["r"])      # skull / upper jaw
    d.polygon([(hx - 2, hy + 2), (hx + 10, hy - 2), (hx + 21, hy + 1), (hx + 8, hy + 4)], fill=PAL["f"])                          # the lit top
    d.polygon([(hx - 4, hy + 3), (hx + 2, hy + 2), (hx + 3, hy + 11), (hx - 5, hy + 9)], fill=PAL["rr"])                          # the neck fold
    d.line([(hx + 4, hy + 2), (hx + 11, hy + 1)], fill=PAL["m"]); d.line([(hx + 5, hy + 3), (hx + 10, hy + 2)], fill=PAL["d"])    # blind: a scar-seam where an eye would be
    mx, my = hx + 4, hy + 9
    gp = 9 + 2 * gape                                                                                            # the mouth's opening in px: a real gape (it is called the Maw)
    d.polygon([(mx, my), (mx + 20, my - 2), (mx + 21, my + gp), (mx + 4, my + gp + 1)], fill=PAL["mo"])         # the dark inside
    d.polygon([(mx + 3, my + 4), (mx + 18, my + 3), (mx + 19, my + gp - 2), (mx + 5, my + gp - 1)], fill=PAL["k"])                  # the throat, black
    d.polygon([(mx + 4, my + gp - 3), (mx + 16, my + gp - 4), (mx + 17, my + gp), (mx + 5, my + gp + 1)], fill=PAL["f"]); d.line([(mx + 6, my + gp - 2), (mx + 15, my + gp - 3)], fill=PAL["fl"])   # the tongue, lit
    d.polygon([(mx + 2, my + gp + 1), (mx + 22, my + gp - 1), (mx + 21, my + gp + 4), (mx + 4, my + gp + 5)], fill=PAL["f"])        # the lower jaw: a slimmer slab
    d.line([(mx + 4, my + gp + 4), (mx + 21, my + gp + 3)], fill=PAL["rr"])
    for k in range(4):                                                                                          # upper teeth: fat fangs, hanging
        x = mx + 2 + k * 5; d.polygon([(x, my - 1), (x + 4, my - 1.5), (x + 2, my + 5 + (k % 2))], fill=PAL["t"])
    for k in range(3):                                                                                          # lower teeth: shorter, rising
        x = mx + 5 + k * 5.5; d.polygon([(x, my + gp + 1), (x + 4, my + gp), (x + 2, my + gp - 4)], fill=PAL["t"])

def frame(i, only=None):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bob = int(round(1.0 * math.sin(2 * math.pi * (i / N * 2) + 0.6)))                       # two dips per cycle, one for each pair of planted feet
    gape = int(round(1.0 * math.sin(2 * math.pi * i / N + 1.2)))
    sway = int(round(1.0 * math.sin(2 * math.pi * i / N + 2.0)))
    far = [n for n in LEGS if LEGS[n][3]]; near = [n for n in LEGS if not LEGS[n][3]]
    for n in far:
        if only in (None, n): draw_leg(im, n, i, bob)
    if only is None: draw_body(im, i, bob, gape, sway)
    for n in near:
        if only in (None, n): draw_leg(im, n, i, bob)
    return outline(im)

def outline(im):
    a = np.array(im); al = a[..., 3] > 0; out = a.copy()
    p = np.pad(al, 1); n = p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:]
    ed = n & ~al; out[ed] = (4, 1, 2, 255)
    hard = out.copy(); hard[..., 3] = np.where(hard[..., 3] > 0, 255, 0)
    # no anti-aliasing: PIL's thick lines and ellipses are crisp, but snap alpha anyway
    return Image.fromarray(hard)

def leg_check():
    """every leg must move: render each leg alone for the whole cycle and count how far its pixels travel and how many distinct states it passes through."""
    res = {}
    for n in LEGS:
        fr = [np.array(frame(i, only=n))[..., 3] > 0 for i in range(N)]
        ch = [int((fr[i] ^ fr[(i + 1) % N]).sum()) for i in range(N)]
        xs = [np.where(f.any(axis=0))[0] for f in fr]
        res[n] = dict(distinct_states=len({f.tobytes() for f in fr}), pixels_changed_per_step=ch, foot_x_range=int(max(x.max() for x in xs) - min(x.max() for x in xs)))
    return res

def save(out):
    os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"blind_maw_walk_{i}.png"))
    strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
    for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(out, "blind_maw_walk_strip.png"))
    sc = 6; sheet = Image.new("RGBA", (W * 4 * sc, H * 2 * sc), (105, 115, 90, 255))
    for i, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((i % 4) * W * sc, (i // 4) * H * sc))
    sheet.convert("RGB").save(os.path.join(out, "blind_maw_walk_sheet.png"))
    return fr

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "_out/maw_walk"; save(out); print(json.dumps(leg_check(), indent=1))
