# Orderly walk, rig 2 (2026-11-21; Yan: the first rig "breaks the sprite a lot", the Pixel Lab and ChatGPT walks "lost 2 of its legs" or "just move the entire body"): two-bone legs with real foot planting.
# Every pixel is still a pixel of Yan's drawing (orderly_native.png) moved by nearest-neighbour; each leg is cut at its knee into a thigh and a shin; a foot target runs along the ground (stance: it slides back
# under the body at the body's speed, the foot stays put on the floor) and lifts in an arc to step forward (swing); 2-bone IK turns the thigh about the hip and the shin about the knee to reach it. Opposite pairs alternate.
# python3 orderly_walk_rig2.py [preview.gif]  -> bosses_v3/orderly/walk/f_00..f_08
import math, os, sys
import numpy as np
from PIL import Image
H = os.path.dirname(os.path.abspath(__file__)); SRC = os.path.join(H, "mutants_anim", "boss_concepts", "gpt", "orderly_native.png"); OUT = os.path.join(H, "mutants_anim", "bosses_v3", "orderly", "walk")
CW, CH = 224, 176; N = 8
# hip, knee, foot (native px), radius, pair, stride (px the foot travels), lift (px)
LEGS = {
    "A": ((62, 91), (51, 126), (37, 134), 4.6, 0, 16, 7),
    "C": ((143, 101), (161, 97), (185, 137), 4.4, 1, 14, 7),
    "D": ((133, 108), (134, 122), (138, 131), 3.4, 0, 8, 5),
    "B": ((105, 110), (104, 124), (98, 137), 3.8, 1, 10, 5),
}
def dist_seg(x, y, a, b):
    vx, vy = b[0] - a[0], b[1] - a[1]; u = max(0.0, min(1.0, ((x - a[0]) * vx + (y - a[1]) * vy) / max(1e-6, vx * vx + vy * vy)))
    return math.hypot(x - a[0] - u * vx, y - a[1] - u * vy)
def ik(hip, L1, L2, tx, ty, bend):
    dx, dy = tx - hip[0], ty - hip[1]; d = math.hypot(dx, dy); d = max(abs(L1 - L2) + 0.5, min(L1 + L2 - 0.01, d))
    a = math.atan2(dy, dx); cosb = (L1 * L1 + d * d - L2 * L2) / (2 * L1 * d); b = math.acos(max(-1, min(1, cosb)))
    th = a + bend * b; return (hip[0] + L1 * math.cos(th), hip[1] + L1 * math.sin(th))
def main():
    a = np.array(Image.open(SRC).convert("RGBA")); h, w = a.shape[:2]; alpha = a[..., 3] > 0
    up = {}; lo = {}; claimed = np.zeros((h, w), bool)
    for n, (hip, knee, foot, r, *_r) in LEGS.items():
        mu = np.zeros((h, w), bool); ml = np.zeros((h, w), bool)
        for y in range(h):
            for x in range(w):
                if not alpha[y, x] or claimed[y, x] or y < hip[1] - 1: continue
                du = dist_seg(x, y, hip, knee); dl = dist_seg(x, y, knee, foot)
                if min(du, dl) > r: continue
                (mu if du <= dl else ml)[y, x] = True
        up[n] = mu; lo[n] = ml; claimed |= mu | ml
    body = a.copy(); body[claimed] = 0
    def piece(out, m, pivot, ang, newpivot, oy, ox):
        ys, xs = np.where(m); c, s = math.cos(ang), math.sin(ang); R = int(max(ys.max() - ys.min(), xs.max() - xs.min())) + 6
        cx, cy = newpivot
        for y in range(int(cy) - R, int(cy) + R + 1):
            for x in range(int(cx) - R, int(cx) + R + 1):
                dx, dy = x - cx, y - cy                                   # inverse map into the source piece
                sx = int(round(pivot[0] + dx * c + dy * s)); sy = int(round(pivot[1] - dx * s + dy * c))
                if 0 <= sx < w and 0 <= sy < h and m[sy, sx] and 0 <= y + oy < out.shape[0] and 0 <= x + ox < out.shape[1]: out[y + oy, x + ox] = a[sy, sx]
    def frame(k):
        t = k / N; out = np.zeros((h + 16, w + 16, 4), np.uint8); oy, ox = 8, 8; bob = int(round(1.4 * abs(math.sin(2 * math.pi * t))) - 1)
        out[oy + bob:oy + bob + h, ox:ox + w] = body
        for n, (hip, knee, foot, r, pair, stride, lift) in LEGS.items():
            ph = (t + 0.5 * pair) % 1.0; fwd = -1 if foot[0] <= hip[0] else 1       # forward on screen is left (-x) for this creature
            hipb = (hip[0], hip[1] + bob)
            if ph < 0.5:  u = ph / 0.5; fx = -(-stride / 2 + stride * u) * 1.0 * -1; ty_l = 0.0      # stance: the foot slides back along the floor
            else:         u = (ph - 0.5) / 0.5; fx = 0; ty_l = 0.0
            # foot offset along the creature's forward axis: stance goes +stride/2 (forward) -> -stride/2 (back); swing goes back -> forward with an arc
            if ph < 0.5: off = stride / 2 - stride * (ph / 0.5); lf = 0.0
            else: u = (ph - 0.5) / 0.5; off = -stride / 2 + stride * u; lf = lift * math.sin(math.pi * u)
            tx = foot[0] + (-off if False else (off * (-1 if fwd < 0 else 1) * 1.0)) - 0; ty = foot[1] - lf
            L1 = math.hypot(knee[0] - hip[0], knee[1] - hip[1]); L2 = math.hypot(foot[0] - knee[0], foot[1] - knee[1])
            # rest bend side: which side the knee sits of the hip->foot line
            side = 1 if ((foot[0] - hip[0]) * (knee[1] - hip[1]) - (foot[1] - hip[1]) * (knee[0] - hip[0])) > 0 else -1
            k2 = ik(hipb, L1, L2, tx, ty, -side)
            if math.hypot(k2[0] - knee[0], k2[1] - (knee[1] + bob)) > 0.01 and False: pass
            a1 = math.atan2(k2[1] - hipb[1], k2[0] - hipb[0]) - math.atan2(knee[1] - hip[1], knee[0] - hip[0])
            a2 = math.atan2(ty - k2[1], tx - k2[0]) - math.atan2(foot[1] - knee[1], foot[0] - knee[0])
            piece(out, up[n], hip, a1, hipb, oy, ox); piece(out, lo[n], knee, a2, k2, oy, ox)
        return Image.fromarray(out, "RGBA")
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT): os.remove(os.path.join(OUT, f))
    fr = []
    for i in range(N + 1):
        f = frame(i % N); cv = Image.new("RGBA", (CW, CH), (0, 0, 0, 0)); cv.alpha_composite(f, ((CW - f.width) // 2, CH - f.height - 6 + 8)); cv.save(os.path.join(OUT, "f_%02d.png" % i)); fr.append(cv)
    if len(sys.argv) > 1:
        g = []
        for rep in range(3):
            for cv in fr[:N]:
                bg = Image.new("RGBA", cv.size, (34, 38, 34, 255)); bg.alpha_composite(cv); g.append(bg.resize((int(cv.width * 1.6), int(cv.height * 1.6)), Image.NEAREST).convert("P", palette=Image.ADAPTIVE))
        g[0].save(sys.argv[1], save_all=True, append_images=g[1:], duration=120, loop=0, disposal=2)
        sh = Image.new("RGBA", (CW * 4, CH * 2), (40, 44, 40, 255))
        for i, cv in enumerate(fr[:N]): sh.alpha_composite(cv, ((i % 4) * CW, (i // 4) * CH))
        sh.resize((sh.width // 2, sh.height // 2), Image.NEAREST).save("/tmp/rig2_sheet.png")
    print("ok", {n: (int(up[n].sum()), int(lo[n].sum())) for n in LEGS})
main()
