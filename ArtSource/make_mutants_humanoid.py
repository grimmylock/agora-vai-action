# The mutants that never got a good double-resolution sprite (Rot Swarm, Alpha Prowler, Mind Weaver, Void Weaver) as NEW humanoid-ish concepts (Yan, 2026-11-09:
# "check the other mutants that do not have 'good' updated sprites and do them, these too will need a new enemy concept, humanoid kinda thing").
# Hand-drawn in Python/PIL (never generated): shapes are built from segments and ellipses per body part, each part shaded from the top-left (light on its upper-left edge,
# dark on its lower-right), details placed pixel by pixel, then the game's outline pass. FIRST PASS for Yan's review: concepts in ArtSource/MUTANT_CONCEPTS.md.
#   python3 make_mutants_humanoid.py <out_dir>
import math, os, sys
from PIL import Image, ImageDraw
import mutant_from_ref as m

def rgb(h): h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)

class Part:
    """one body part: a mask painted with shapes, shaded as light / mid / dark from its own edges"""
    def __init__(self, size, pal, tex=0, seed=1):
        self.m = Image.new("L", size, 0); self.d = ImageDraw.Draw(self.m); self.pal = pal; self.tex = tex; self.seed = seed; self.size = size
    def ell(self, cx, cy, rx, ry): self.d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=255); return self
    def poly(self, pts): self.d.polygon(pts, fill=255); return self
    def limb(self, pts, widths):
        for (x0, y0), (x1, y1), w in zip(pts, pts[1:], widths):
            n = max(abs(x1 - x0), abs(y1 - y0), 1) * 2
            for i in range(int(n) + 1):
                x, y = x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n
                self.d.ellipse([x - w / 2, y - w / 2, x + w / 2, y + w / 2], fill=255)
        return self
    def erase(self, fn):
        p = self.m.load()
        for y in range(self.size[1]):
            for x in range(self.size[0]):
                if p[x, y] and fn(x, y): p[x, y] = 0
        return self
    def render(self):
        light, mid, dark = self.pal; out = Image.new("RGBA", self.size, (0, 0, 0, 0)); o = out.load(); p = self.m.load(); W, H = self.size
        def on(x, y): return 0 <= x < W and 0 <= y < H and p[x, y] > 0
        for y in range(H):
            for x in range(W):
                if not p[x, y]: continue
                c = mid
                up, lf, dn, rt = on(x, y - 1), on(x - 1, y), on(x, y + 1), on(x + 1, y)
                if not up or not lf or not on(x - 1, y - 1) and (x + y) % 2 == 0: c = light
                if (not dn or not rt) and (up and lf): c = dark
                if not dn and not rt: c = dark
                if self.tex:                                                  # a rough fixed texture so flat areas are not dead
                    h = (x * 73856093 ^ y * 19349663 ^ self.seed * 83492791) % 100
                    if h < self.tex: c = light if h % 2 else dark
                o[x, y] = c
        return out

def compose(size, parts, extra=None):
    im = Image.new("RGBA", size, (0, 0, 0, 0))
    for p in parts: im.alpha_composite(p.render() if isinstance(p, Part) else p)
    if extra: extra(im)
    return im

def px(im, pts, col):
    for x, y in pts:
        if 0 <= x < im.width and 0 <= y < im.height: im.putpixel((x, y), col)

def pal(l, mi, d): return (rgb(l), rgb(mi), rgb(d))

# ---------------------------------------------------------------- Alpha Prowler: a lean, long-armed hunter that stalks upright and drops to its knuckles to run
def alpha_prowler(cloak=False):
    S = (56, 64)
    skin = pal("#6b6874", "#454350", "#2a2932"); scar = pal("#b8b3a8", "#9a958a", "#74706a"); bone = pal("#d9d2bc", "#bdb59b", "#8f8870")
    if cloak: skin = pal("#c9ccd2", "#a8acb5", "#7d818c")
    dk = tuple(rgb(c) for c in ("#3a3842", "#2b2a33", "#1d1c23")) if not cloak else (rgb("#9a9ea8"), rgb("#7c808b"), rgb("#5d616c"))
    leg_far = Part(S, dk); leg_far.limb([(26, 34), (22, 41), (27, 49), (23, 59)], [5, 4, 3])
    arm_far = Part(S, dk); arm_far.limb([(27, 21), (24, 32), (28, 44), (31, 57)], [4, 3.5, 3])
    torso = Part(S, skin, tex=10, seed=3); torso.poly([(25, 20), (36, 17), (38, 27), (33, 38), (26, 37), (23, 28)]).ell(31, 22, 7, 5)       # narrow, hunched
    head = Part(S, skin, tex=6, seed=5); head.ell(40, 15, 5, 5).poly([(41, 11), (50, 16), (51, 20), (42, 21)])
    leg_near = Part(S, skin, tex=8, seed=7); leg_near.limb([(31, 34), (37, 41), (32, 49), (36, 59)], [6, 5, 3])
    arm_near = Part(S, skin, tex=8, seed=9); arm_near.limb([(34, 21), (41, 31), (41, 43), (45, 55)], [5, 4.5, 3.5])   # knuckles to the ground
    claws = Part(S, bone)
    for dx in (-2, 0, 2): claws.limb([(45 + dx // 2, 55), (48 + dx, 61)], [1.6, 1])
    for dx in (-2, 0, 2): claws.limb([(31 + dx // 2, 57), (33 + dx, 62)], [1.4, 1])
    ribs = Part(S, scar, seed=11)
    for k in range(4): ribs.limb([(26 + k, 25 + 3 * k), (33 + k // 2, 24 + 3 * k)], [1, 1])
    spine = Part(S, scar); spine.limb([(24, 21), (29, 16), (35, 14)], [2, 2, 2])
    def det(im):
        eye = rgb("#ff4a3a") if not cloak else rgb("#e04030")
        px(im, [(44, 15), (45, 15)], eye); px(im, [(44, 14)], rgb("#ffb0a0"))
        px(im, [(46, 21), (48, 21), (49, 20)], rgb("#ece6d4"))                  # teeth along a shut mouth
        for x in range(42, 50): im.putpixel((x, 20), rgb("#1a1018")) if im.getpixel((x, 20))[3] else None
    return m.outline(compose(S, [leg_far, arm_far, torso, ribs, spine, head, leg_near, arm_near, claws], det))

# ---------------------------------------------------------------- Rot Crawler (Rot Swarm): a bloated, fungus-eaten man that crawls on all fours
def rot_crawler():
    S = (44, 34)
    flesh = pal("#9a8f6e", "#6e6648", "#463f2c"); fung = pal("#c9d88a", "#8fa34e", "#586a2e"); dark = pal("#4a4430", "#38331f", "#262216")
    limb_far = Part(S, dark); limb_far.limb([(14, 20), (9, 26), (12, 31)], [4, 3, 2]); limb_far.limb([(28, 20), (31, 27), (36, 31)], [4, 3, 2])
    belly = Part(S, flesh, tex=14, seed=2); belly.ell(21, 18, 12, 8).ell(14, 17, 7, 6)
    spine = Part(S, fung, tex=30, seed=4); spine.limb([(10, 11), (17, 8), (25, 9), (31, 12)], [4, 4, 4, 3])
    head = Part(S, flesh, tex=8, seed=6); head.ell(34, 15, 5, 5).ell(36, 20, 3, 3)       # a human skull and jaw
    limb_near = Part(S, flesh, tex=10, seed=8); limb_near.limb([(17, 20), (13, 27), (16, 32)], [4, 3, 2]); limb_near.limb([(30, 21), (35, 27), (40, 30), (42, 33)], [4, 3, 2, 2])
    caps = Part(S, fung, tex=0)
    for cx, cy, r in ((12, 12, 2), (19, 9, 2), (26, 10, 3), (9, 17, 2)): caps.ell(cx, cy, r, r - 1)
    def det(im):
        px(im, [(35, 14), (36, 14)], rgb("#e8e0a0")); px(im, [(36, 21), (37, 21), (38, 22), (35, 22)], rgb("#ece6d4")); px(im, [(34, 14)], rgb("#1a1008"))
        for x in range(35, 39): im.putpixel((x, 22), rgb("#2a1410")) if im.getpixel((x, 22))[3] else None
        px(im, [(18, 20), (24, 21), (27, 18)], rgb("#c9d88a"))
    return m.outline(compose(S, [limb_far, belly, spine, caps, head, limb_near], det))

# ---------------------------------------------------------------- Mind Weaver: a gaunt, floating seer; huge cranium, a shroud for a body, long fingers weaving threads
def mind_weaver():
    S = (56, 64)
    skin = pal("#9a86b8", "#6f5a8e", "#473a5e"); shroud = pal("#4d4166", "#382f4c", "#241e33"); thread = pal("#e6cfff", "#c79bff", "#9a66e0"); bone = pal("#ddd3ea", "#bcb0d0", "#8d81a6")
    tail = Part(S, shroud, tex=8, seed=2); tail.poly([(22, 28), (36, 28), (38, 44), (32, 58), (27, 52), (24, 60), (20, 44)])
    tatter = Part(S, shroud, seed=3)
    for x, y in ((21, 52), (26, 58), (31, 56), (35, 52)): tatter.limb([(x, 44), (x + 1, y)], [3, 2])
    body = Part(S, skin, tex=8, seed=4); body.ell(29, 33, 7, 8)
    arm_n = Part(S, skin, tex=6, seed=5); arm_n.limb([(34, 30), (41, 33), (44, 27)], [4, 3, 2])
    arm_f = Part(S, tuple(rgb(c) for c in ("#5c4a78", "#46375e", "#2f2541")), seed=6); arm_f.limb([(24, 30), (18, 34), (15, 28)], [4, 3, 2])
    cranium = Part(S, skin, tex=10, seed=7); cranium.ell(30, 17, 11, 11).ell(34, 21, 7, 6)
    folds = Part(S, pal("#c1a8e0", "#a285c8", "#7a5ca6"), seed=8)
    for a in (0, 1, 2): folds.limb([(22 + a * 2, 12 + a * 3), (28 + a * 2, 9 + a * 3), (36, 11 + a * 3)], [1, 1, 1])
    fingers = Part(S, bone)
    for k in (-2, 0, 2): fingers.limb([(44, 27), (46 + k, 21 + abs(k))], [1.4, 1]); fingers.limb([(15, 28), (13 + k, 22 + abs(k))], [1.4, 1])
    def det(im):
        # three eyes: two big pale ones and a third on the brow
        for ex, ey in ((33, 19), (39, 21)): px(im, [(ex, ey), (ex + 1, ey), (ex, ey + 1), (ex + 1, ey + 1)], rgb("#fff4ff")); px(im, [(ex + 1, ey)], rgb("#b04cff"))
        px(im, [(35, 14), (36, 14)], rgb("#e6cfff")); px(im, [(36, 25), (37, 25), (38, 25)], rgb("#1a1024"))
        # threads of light between the hands
        for t in range(0, 29):
            x = 15 + t; y = int(24 - 7 * math.sin(math.pi * t / 28) + 1.5 * math.sin(t * 0.9))
            if 0 <= y < im.height: im.putpixel((x, y), rgb("#c79bff") if t % 3 else rgb("#f0e0ff"))
    return m.outline(compose(S, [tail, tatter, arm_f, body, arm_n, cranium, folds, fingers], det))

# ---------------------------------------------------------------- Void Weaver: a tall hooded figure whose face is a hole; its robes come apart into motes at the hem
def void_weaver(cloak=False):
    S = (56, 72)
    robe = pal("#2c2840", "#1c1a2b", "#0e0d17"); rim = pal("#6a5aa0", "#4b3f7a", "#322a56"); void = pal("#0a0612", "#05030a", "#000000")
    if cloak: robe = pal("#4a4666", "#363250", "#242238")
    body = Part(S, robe, tex=7, seed=2); body.poly([(24, 20), (36, 20), (41, 40), (44, 58), (36, 62), (30, 58), (22, 62), (14, 58), (19, 40)])
    hood = Part(S, robe, tex=5, seed=3); hood.ell(30, 16, 9, 11).poly([(22, 22), (38, 22), (40, 30), (20, 30)])
    face = Part(S, void); face.ell(32, 17, 5, 7)
    arm_n = Part(S, robe, tex=6, seed=4); arm_n.limb([(38, 28), (45, 36), (47, 46)], [6, 5, 3])
    arm_f = Part(S, tuple(rgb(c) for c in ("#1f1c30", "#15131f", "#0a0910")), seed=5); arm_f.limb([(22, 28), (15, 36), (12, 46)], [6, 5, 3])
    hands = Part(S, pal("#8d82bd", "#6a5e9a", "#463d70"))
    for k in (-2, 0, 2): hands.limb([(47, 46), (48 + k, 52)], [1.4, 1]); hands.limb([(12, 46), (11 + k, 52)], [1.4, 1])
    cracks = Part(S, pal("#d8c8ff", "#a98cff", "#7a5cd6"))
    for pts in (((30, 30), (31, 38), (29, 46), (31, 54)), ((36, 32), (37, 40), (35, 48)), ((25, 34), (24, 42), (26, 50))): cracks.limb(list(pts), [1, 1, 1, 1][:len(pts)])
    def det(im):
        px(im, [(33, 16), (33, 17)], rgb("#e8ddff"))                              # one pale point in the hole where a face should be
        rng = (lambda i: (i * 7919) % 97)
        for i in range(26):                                                       # motes: the hem is coming apart
            x = 16 + rng(i) % 28; y = 58 + rng(i + 31) % 12
            if (x * 3 + y) % 5 < 3: im.putpixel((x, y), rgb("#6a5aa0") if i % 2 else rgb("#a98cff"))
    return m.outline(compose(S, [body, cracks, hood, face, arm_f, arm_n, hands], det))

def crop(im, pad=1):
    bb = im.getbbox(); return im.crop((max(0, bb[0] - pad), max(0, bb[1] - pad), min(im.width, bb[2] + pad), min(im.height, bb[3] + pad)))

ALL = {"alpha_prowler": alpha_prowler, "alpha_prowler_cloaked": lambda: alpha_prowler(True), "rot_crawler": rot_crawler, "mind_weaver": mind_weaver,
       "void_weaver": void_weaver, "void_weaver_cloaked": lambda: void_weaver(True)}

def save(out):
    os.makedirs(out, exist_ok=True); ims = {k: fn() for k, fn in ALL.items()}
    for k, im in ims.items(): im.save(os.path.join(out, f"mutant_hd_{k}.png"))
    sc = 8; W = sum(crop(im).width for im in ims.values()) * sc + 40 * len(ims); H = max(crop(im).height for im in ims.values()) * sc + 40
    sheet = Image.new("RGBA", (W, H), (42, 38, 32, 255)); x = 20
    for k, im in ims.items():
        c = crop(im); sheet.alpha_composite(c.resize((c.width * sc, c.height * sc), Image.NEAREST), (x, H - 20 - c.height * sc)); x += c.width * sc + 40
    sheet.save(os.path.join(out, "humanoid_mutants_sheet.png")); return ims

if __name__ == "__main__":
    save(sys.argv[1] if len(sys.argv) > 1 else "_out/humanoid")
