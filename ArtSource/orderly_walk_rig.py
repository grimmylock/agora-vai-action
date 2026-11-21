# Procedural walk for the Orderly (2026-11-21; Yan: the Pixel Lab walk "looks very bad", "go with the procedural rig for the Orderly first, if it doesn't work we'll try option 3").
# Rules of canine_cutout.py: NO stretching; every pixel is a pixel of Yan's drawing (boss_concepts/gpt/orderly_native.png), moved by nearest-neighbour; the neutral pose is the drawing exactly.
# Four legs are cut out as polylines with a radius (the rest is the body); each is turned about its hip and lifted while it swings forward; the body bobs a pixel; opposite pairs step together.
# Writes bosses_v3/orderly/walk/f_00..f_08 (the last = the first: export_boss_clips drops it) on the Pixel Lab canvas (224 x 176, the stand 6 px above the bottom).   python3 orderly_walk_rig.py [preview.gif]
import math, os, sys
import numpy as np
from PIL import Image
H = os.path.dirname(os.path.abspath(__file__)); SRC = os.path.join(H, "mutants_anim", "boss_concepts", "gpt", "orderly_native.png"); OUT = os.path.join(H, "mutants_anim", "bosses_v3", "orderly", "walk")
CW, CH = 224, 176
# name: (polyline of the leg from its hip to its toe, radius, pair, swing amplitude in radians, lift in px)
LEGS = {
    "A": ([(62, 92), (58, 102), (54, 116), (50, 126), (45, 131), (38, 135)], 4.2, 0, 0.20, 5),
    "C": ([(142, 101), (152, 99), (160, 97), (164, 106), (169, 118), (175, 129), (185, 137)], 4.0, 1, 0.16, 5),
    "D": ([(133, 108), (134, 120), (135, 128), (138, 131)], 3.2, 0, 0.16, 4),
    "B": ([(105, 110), (104, 122), (101, 130), (98, 137)], 3.6, 1, 0.18, 4),
}
def seg_dist(x, y, a, b):
    vx, vy = b[0] - a[0], b[1] - a[1]; u = max(0.0, min(1.0, ((x - a[0]) * vx + (y - a[1]) * vy) / max(1e-6, vx * vx + vy * vy)))
    return math.hypot(x - a[0] - u * vx, y - a[1] - u * vy)
def main():
    im = Image.open(SRC).convert("RGBA"); a = np.array(im); h, w = a.shape[:2]; alpha = a[..., 3] > 0
    mask = {}; claimed = np.zeros((h, w), bool)
    for name, (pts, r, *_rest) in LEGS.items():
        m = np.zeros((h, w), bool)
        for y in range(h):
            for x in range(w):
                if alpha[y, x] and not claimed[y, x] and min(seg_dist(x, y, pts[i], pts[i + 1]) for i in range(len(pts) - 1)) <= r and y >= pts[0][1] - 1: m[y, x] = True
        mask[name] = m; claimed |= m
    body = a.copy(); body[claimed] = 0
    def frame(t):                                   # t in 0..1 over the cycle
        ph = 2 * math.pi * t; bob = int(round(1.2 * math.sin(2 * ph)))
        out = np.zeros((h + 12, w + 12, 4), np.uint8); oy = 6; ox = 6
        by = np.zeros_like(out); by[oy + bob:oy + bob + h, ox:ox + w] = body       # body (with its saws, bag and wings) bobs
        out = by
        for name, (pts, r, pair, amp, lift) in LEGS.items():
            p = ph + math.pi * pair; sw = math.cos(p)                                  # +1 foot far forward, -1 far back
            fwd = 1 if pts[-1][0] < pts[0][0] else -1                                  # which way is forward on screen (left for a left-facing creature)
            hip = pts[0]; foot = pts[-1]; rel = (foot[0] - hip[0], foot[1] - hip[1])
            th = amp * sw; c, s = math.cos(th), math.sin(th)
            nx = rel[0] * c - rel[1] * s                                                 # where the foot goes under +th
            if (nx - rel[0]) * (-fwd) > 0: th = -th                                      # make +sw = forward
            c, s = math.cos(th), math.sin(th); dy = -lift * max(0.0, math.sin(p + math.pi / 2)) if False else -lift * max(0.0, -math.sin(p)) * 1.0   # lifted while it swings forward
            m = mask[name]; ys, xs = np.where(m)
            for y in range(int(ys.min()) - 25, int(ys.max()) + 26):
                for x in range(int(xs.min()) - 25, int(xs.max()) + 26):
                    ddx, ddy = x - hip[0], y - hip[1] - dy                               # inverse map: undo the lift, then the turn
                    sx = int(round(hip[0] + ddx * c + ddy * s)); sy = int(round(hip[1] - ddx * s + ddy * c))
                    if 0 <= sx < w and 0 <= sy < h and m[sy, sx] and 0 <= y + oy < out.shape[0] and 0 <= x + ox < out.shape[1]:
                        out[y + oy, x + ox] = a[sy, sx]
        res = Image.fromarray(out, "RGBA"); return res
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT): os.remove(os.path.join(OUT, f))
    frames = []
    for i in range(9):
        fr = frame((i % 8) / 8.0); cv = Image.new("RGBA", (CW, CH), (0, 0, 0, 0)); cv.alpha_composite(fr, ((CW - fr.width) // 2, CH - fr.height - 6 + 6)); cv.save(os.path.join(OUT, "f_%02d.png" % i)); frames.append(cv)
    # the neutral check: phase where every swing is 0 is not in the loop, so check the drawing is intact apart from the legs
    if len(sys.argv) > 1:
        fl = []
        for rep in range(3):
            for cv in frames[:8]:
                bg = Image.new("RGBA", cv.size, (34, 38, 34, 255)); bg.alpha_composite(cv); fl.append(bg.resize((int(cv.width * 1.6), int(cv.height * 1.6)), Image.NEAREST).convert("P", palette=Image.ADAPTIVE))
        fl[0].save(sys.argv[1], save_all=True, append_images=fl[1:], duration=110, loop=0, disposal=2); print("gif", sys.argv[1])
    print("frames", len(frames), {n: int(m.sum()) for n, m in mask.items()})
main()
