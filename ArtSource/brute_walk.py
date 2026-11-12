# Blight Brute WALK (2026-11-12; made heavier on Yan's word: body dips 4 px, feet lift up to 5 rows, 200 ms a frame), a heavy stomp on the Brute that is in the game now (mutant_hd_blight_brute.png, 64x64), whole-pixel moves only like the canines: the body is one piece that dips when a foot lands and
# sways by a column shear, the two legs are cut-out pieces (a lifted foot = deleted rows, top-anchored, so the leg stays joined), the hanging arm swings by a row shear from the shoulder.  python3 brute_walk.py <out_dir>
import os, sys, json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
HERE = os.path.dirname(os.path.abspath(__file__))
SRC = Image.open(os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "mutant_hd_blight_brute.png")).convert("RGBA"); S = np.array(SRC)
ox, oy = 6, 8; W, H = SRC.width + 2 * ox, SRC.height + 2 * oy; N = 8; LAST = SRC.height - 1
PARTS = {"leg_l": ((0, 34), 56), "leg_r": ((37, 53), 54), "arm": ((52, 64), 30)}              # (x range, first row): the left foot, the right foot, the hanging arm
PIECE = {}; BODY = S.copy()
for n, ((x0, x1), top) in PARTS.items():
    p = np.zeros_like(S); p[top:, x0:x1] = S[top:, x0:x1]; PIECE[n] = p; BODY[top:, x0:x1] = 0
SPLIT = 54                                          # the body's rows from here down (the underside between the feet) stay planted, the rest dips
BOB = [4, 2, 0, 0, 4, 2, 0, 0]                    # body down in px: lowest when a foot lands (frames 0 and 4)
LIFT_R = [0, 2, 5, 3, 0, 0, 0, 0]; LIFT_L = [0, 0, 0, 0, 0, 2, 5, 3]                          # rows a foot is raised
ARM = [0, -2, -3, -2, 0, 2, 3, 2]                  # px the hand swings (negative = back), shear from the shoulder
SWAY = [0, 1, 2, 1, 0, -1, -2, -1]                   # whole-body tilt: px the head end is moved sideways-up over the feet (a column shear)
MS = [200] * N

def tilt(a, amount):
    if amount == 0: return a
    src = a.copy(); out = np.zeros_like(a); h, w = a.shape[:2]
    for x in range(w):
        sh = int(round(amount * (x - (ox + 32)) / 32.0))
        for y in range(h):
            ny = y + sh
            if 0 <= ny < h and src[y, x, 3]: out[ny, x] = src[y, x]
    return out

def leg(name, bob, lift, fwd=0):
    """a foot piece: planted it stays where it stands (the body dips onto it), raised it moves up by `lift` rows and a little forward/back (its top overlaps the body's underside)"""
    p = PIECE[name]; top = PARTS[name][1]; out = np.zeros((H, W, 4), np.uint8)
    for y in range(top, S.shape[0]):
        yy = y - lift + oy
        for x in range(S.shape[1]):
            if p[y, x, 3] and 0 <= yy < H and 0 <= x + fwd + ox < W: out[yy, x + fwd + ox] = p[y, x]
    return out

def arm(bob, swing):
    p = PIECE["arm"]; top = PARTS["arm"][1]; out = np.zeros((H, W, 4), np.uint8); span = LAST - top
    for y in range(top, S.shape[0]):
        sh = int(round(swing * (y - top) / span))
        for x in range(S.shape[1]):
            if p[y, x, 3]:
                nx, yy = x + sh + ox, y + bob + oy
                if 0 <= nx < W and 0 <= yy < H: out[yy, nx] = p[y, x]
    return out

def frame(i):
    lower = np.zeros((H, W, 4), np.uint8); upper = np.zeros((H, W, 4), np.uint8)
    for y in range(S.shape[0]):
        if y >= SPLIT: lower[y + oy, ox: ox + S.shape[1]] = BODY[y]
        elif y + BOB[i] + oy < H: upper[y + BOB[i] + oy, ox: ox + S.shape[1]] = BODY[y]
    upper = tilt(upper, SWAY[i]); lower = tilt(lower, SWAY[i] // 2)
    # the arm hangs from the shoulder: it follows half the dip so the hand never goes under the ground
    layers = [lower, upper, leg("leg_l", 0, LIFT_L[i], -1 if LIFT_L[i] > 2 else 0), leg("leg_r", 0, LIFT_R[i], 1 if LIFT_R[i] > 2 else 0), arm(BOB[i] // 2, ARM[i])]
    im = np.zeros((H, W, 4), np.uint8)
    for l in layers:
        m = l[..., 3] > 0; im[m] = l[m]
    lab, k = nd.label(im[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (lab == j).sum() < 6: im[lab == j] = 0
    return Image.fromarray(im)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/brute_walk"; os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"frame{i}.png"))
    strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
    for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(out, "blight_brute_walk_strip.png"))
    sc = 6; sheet = Image.new("RGBA", (W * 4 * sc, H * 2 * sc), (105, 115, 90, 255))
    for k, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((k % 4) * W * sc, (k // 4) * H * sc))
    sheet.convert("RGB").save(os.path.join(out, "sheet.png"))
    fs = []
    for k in range(24):
        im = Image.new("RGBA", (W + 8, H + 4), (54, 49, 41, 255)); d = ImageDraw.Draw(im); g = oy + LAST + 1; d.rectangle([0, g, im.width, im.height], fill=(60, 55, 46, 255)); off = int(k * 1.6) % 10
        for gx in range(-10, im.width, 10): d.point((gx - off + 10, g + 3), fill=(80, 74, 62, 255))
        im.alpha_composite(fr[k % N], (4, 0)); fs.append(im.resize((im.width * 8, im.height * 8), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE))
    fs[0].save(os.path.join(out, "walk_200ms.gif"), save_all=True, append_images=fs[1:], duration=MS[0], loop=0)
    json.dump({"blight_brute": {"walk": {"frame": [W, H], "ms": MS, "ground_row": oy + LAST}}}, open(os.path.join(out, "brute_walk.json"), "w"), indent=1)
    print("components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr], "lowest rows", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", oy + LAST)
