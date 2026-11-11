# Veil Prowler walk (2026-11-11), the second one after the Pallid, "based on" the Pallid walk Yan recovered from an online animator (mutants_anim/pallid_online). Yan: "can't you use this
# one as a reference and try animating another one based on it? the black dog that disappears could be next".
# What is taken from the reference: 8 pictures a cycle at 125 ms, a diagonal gait (near hind + far fore, then the other pair), the body dipping twice a cycle (the reference's back drops
# about one sprite pixel at two points of the cycle), the head riding the body, the planted feet sliding back at one speed, the legs lifted and swung forward in between, only two feet on
# the ground in the two passing frames. What is kept from Yan's own Veil sprite: every pixel of the body, head, jaw and tail (the cut-out rig of canine_cutout.py, legs removed; moved only
# by whole pixels, never resampled). What is new: the four legs, drawn fresh each frame in the sprite's own colours (thin tubes with a lit edge and a flat clawed foot, positioned by a two-bone solve)
# because rotating or shearing a 3 px leg shreds it (the second and third tries).
#   python3 veil_walk.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
import canine_cutout as C
import mutant_from_ref as m

ox, oy = 8, 4                           # the sprite's place inside the frame
N = 8
rig = C.Rig("veil_prowler", C.SPECS["veil_prowler"])
W, H = rig.W0 + 2 * ox, rig.H0 + 2 * oy
GROUND = oy + 31                        # the original's feet end on its row 31
LEGS = {"far_hind": ((15, 19), 0.50, True, False), "near_hind": ((10, 19), 0.00, False, False), "far_fore": ((31, 19), 0.00, True, True), "near_fore": ((25, 19), 0.50, False, True)}
L1, L2 = 6.5, 6.5
STRIDE, LIFT = 5.5, 3.5
BODY = Image.new("RGBA", rig.base.size, (0, 0, 0, 0))
for n in ("hind", "fore"): BODY.alpha_composite(rig.parts[n])
HEAD = Image.new("RGBA", rig.base.size, (0, 0, 0, 0)); HEAD.alpha_composite(rig.parts["head"]); HEAD.alpha_composite(rig.parts["jaw"])
# the legs' own colours (the original's leg pieces: black contour, violet body, a pale lit edge)
DARK, MID, LIT, LIT2 = (26, 24, 36), (34, 32, 45), (87, 88, 103), (55, 54, 64)
OUT = (4, 1, 2, 255)

def foot(ph):
    if ph < 0.55: return (STRIDE - 2 * STRIDE * ph / 0.55, 0.0)
    u = (ph - 0.55) / 0.45
    return (-STRIDE + 2 * STRIDE * (0.5 - 0.5 * math.cos(math.pi * u)), LIFT * math.sin(math.pi * u))

def solve(hip, ft, fwd):
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), L1 + L2 - 0.2); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (L1 * L1 + d * d - L2 * L2) / (2 * L1 * d)))); k = a + (ang if fwd else -ang)
    return (hip[0] + L1 * math.cos(k), hip[1] + L1 * math.sin(k))

def seg(d, p, q, w, col):
    d.line([p, q], fill=col, width=w)

def leg_img(name, i, bob):
    (hx, hy), ph0, far, fwd = LEGS[name]; ph = (i / N + ph0) % 1.0; fo, fl = foot(ph)
    hip = (hx + ox, hy + oy + bob); ft = (hx + ox + fo + (2 if fwd else -1), GROUND - fl); kn = solve(hip, ft, fwd)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    k = 0.72 if far else 1.0
    dark = tuple(int(c * k) for c in DARK) + (255,); mid = tuple(int(c * k) for c in MID) + (255,); lit = tuple(int(c * (0.7 if far else 1.0)) for c in LIT2) + (255,); hi = tuple(int(c * (0.7 if far else 1.0)) for c in LIT) + (255,)
    d.line([hip, kn], fill=mid, width=2); d.line([kn, ft], fill=mid, width=1)                            # the body of the leg is thin: the black contour added round it (m.outline) makes it the 3-4 px of the original
    d.line([(hip[0] - 0.7, hip[1]), (kn[0] - 0.7, kn[1])], fill=lit, width=1); d.line([(kn[0] - 0.5, kn[1]), (ft[0] - 0.5, ft[1])], fill=lit, width=1)   # the lit edge on the side the light comes from
    fx, fy = int(round(ft[0])), int(round(ft[1]))
    d.rectangle([fx - 1, fy, fx + 3, fy], fill=mid); d.point((fx - 1, fy), fill=lit); d.point((fx + 3, fy), fill=hi)                      # the flat foot, the toes reaching forward
    d.point((int(round(kn[0] - 1)), int(round(kn[1]))), fill=hi)                                          # a bright speck at the knee
    a = np.array(im); a[..., 3] = np.where(a[..., 3] > 120, 255, 0); return Image.fromarray(a.astype(np.uint8))

def frame(i, only=None):
    bob = int(round(0.9 * math.sin(2 * math.pi * (i / N * 2) + 0.6))); sway = int(round(0.9 * math.sin(2 * math.pi * i / N + 2.0)))
    far = [n for n in LEGS if LEGS[n][2]]; near = [n for n in LEGS if not LEGS[n][2]]
    if only: return leg_img(only, i, bob)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for n in far: im.alpha_composite(leg_img(n, i, bob))
    im.alpha_composite(BODY, (ox, oy + bob)); im.alpha_composite(HEAD, (ox, oy + bob + sway))
    for n in near: im.alpha_composite(leg_img(n, i, bob))
    return m.outline(im)

def leg_check():
    res = {}
    for n in LEGS:
        fr = [np.array(frame(i, only=n))[..., 3] > 0 for i in range(N)]
        xs = [np.where(f.any(axis=0))[0] for f in fr]
        res[n] = dict(distinct_states=len({f.tobytes() for f in fr}), min_pixels_changed_in_a_step=min(int((fr[i] ^ fr[(i + 1) % N]).sum()) for i in range(N)), foot_reach_px=int(max(x.max() for x in xs) - min(x.max() for x in xs)))
    return res

def save(out):
    os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"frame{i}.png"))
    strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
    for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(out, "veil_walk_strip.png"))
    json.dump({"veil_prowler": {"walk": {"frame": [W, H], "ms": [125] * N, "loop": True, "travel": [0] * N, "ground_row": GROUND}}}, open(os.path.join(out, "veil_walk.json"), "w"), indent=1)
    sc = 12; sheet = Image.new("RGBA", (W * 4 * sc, H * 2 * sc), (105, 115, 90, 255))
    for i, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((i % 4) * W * sc, (i // 4) * H * sc))
    sheet.convert("RGB").save(os.path.join(out, "sheet.png")); return fr

def gif(path, fr, ms, px=2.27, n=48, sc=10):
    fs = []; ds = []
    for k in range(n):
        im = Image.new("RGBA", (W + 12, H + 4), (54, 49, 41, 255)); d = ImageDraw.Draw(im); d.rectangle([0, GROUND + 1, im.width, im.height], fill=(60, 55, 46, 255)); off = int(k * px) % 12
        for gx in range(-12, im.width, 12): d.rectangle([gx - off + 12, GROUND + 3, gx - off + 14, GROUND + 3], fill=(80, 74, 62, 255))
        im.alpha_composite(fr[k % N], (6, 0)); fs.append(im.resize((im.width * sc, im.height * sc), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE)); ds.append(ms)
    fs[0].save(path, save_all=True, append_images=fs[1:], duration=ds, loop=0)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "_out/veil_walk"; fr = save(out); gif(os.path.join(out, "loop_125ms.gif"), fr, 125); gif(os.path.join(out, "loop_220ms.gif"), fr, 220)
    chk = leg_check()
    for k, v in chk.items(): print(k, v)
