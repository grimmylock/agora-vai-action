# 3/4 walls and natural masses as per-cell autotiles (Batch 30).
# A wall cell's sprite is 16 wide x (16 + H) tall; its bottom 16 rows sit on the cell itself.
#   footprint - what the wall covers seen from straight above, inside the 16x16 cell: built walls are a
#               T px band through the centre with arms to the connected sides; masses fill the cell
#   cap       - the footprint lifted up by H (the top of the wall, light)
#   face      - under the cap's bottom edge, H px of front face down to the ground (textured, darker)
# Connected sides run to the canvas edge with no outline, so neighbouring cells join seamlessly; the
# south neighbour's cap (drawn later, it sorts in front) covers this cell's lower rows. Masks are
# NESW bits: N=1 E=2 S=4 W=8.
import numpy as np
from PIL import Image, ImageDraw
from worldlib import *

C = 16

def valid_corners(mask):
    """corner bits (NE=1 SE=2 SW=4 NW=8) that can be filled: both sides next to the corner connect"""
    v = 0
    if mask & 1 and mask & 2: v |= 1
    if mask & 4 and mask & 2: v |= 2
    if mask & 4 and mask & 8: v |= 4
    if mask & 1 and mask & 8: v |= 8
    return v

def footprint(mask, T, seed, organic, corners=0):
    f = np.zeros((C, C), bool)
    if T >= C: f[:, :] = True
    else:
        a, b = (C - T) // 2, (C - T) // 2 + T
        f[a:b, a:b] = True
        if mask & 1: f[:b, a:b] = True
        if mask & 4: f[a:, a:b] = True
        if mask & 8: f[a:b, :b] = True
        if mask & 2: f[a:b, a:] = True
        if corners & 1: f[:a, b:] = True                 # solid blocks: the diagonal is wall too -> fill the corner
        if corners & 2: f[b:, b:] = True
        if corners & 4: f[b:, :a] = True
        if corners & 8: f[:a, :a] = True
    if organic:
        # Batch 127 (Yan: "does everything need to be squared like that? we need some natural shapes"): each open side is a
        # wavy edge (a few random harmonics, pinned to the same depth at the cell's ends so neighbours still join), and a
        # corner between two open sides is rounded off, so a hedge or a cliff reads as one lumpy mass, not a row of squares.
        import math
        r = rng(seed)
        PIN, R = 2, 6
        for side in range(4):
            if mask & (1 << side): continue
            a = [r.uniform(-2.6, 2.6) for _ in range(3)]
            for k in range(C):
                t = k / (C - 1)
                d = PIN + sum(a[i] * math.sin(math.pi * (i + 1) * t) for i in range(3)) + r.uniform(-0.5, 0.5)
                for dd in range(max(0, min(6, int(round(d))))):
                    if side == 0: f[dd, k] = False
                    elif side == 2: f[C - 1 - dd, k] = False
                    elif side == 3: f[k, dd] = False
                    else: f[k, C - 1 - dd] = False
        for bit_a, bit_b, cx, cy in ((1, 2, C - 1, 0), (4, 2, C - 1, C - 1), (4, 8, 0, C - 1), (1, 8, 0, 0)):
            if mask & bit_a or mask & bit_b: continue
            ox, oy = (R if cx == 0 else C - 1 - R), (R if cy == 0 else C - 1 - R)   # the rounding circle's centre
            for y in range(C):
                for x in range(C):
                    if (x - ox) * (cx - ox) > 0 and (y - oy) * (cy - oy) > 0 and (x - ox) ** 2 + (y - oy) ** 2 > R * R: f[y, x] = False
    return f

# ---------------- surface painters: (x, y) cell-local / face-local -> RGBA
def dk(c, k=0.45):
    """Batch 31 outline: a darker shade of the pixel's own colour instead of flat black"""
    return (int(c[0] * k), int(c[1] * k), int(c[2] * k), 255)

def weather(face):
    """Batch 31: wet, dirty walls - darker damp band at the foot, moss, rain streaks down the face"""
    def f(x, k, H, r):
        c = face(x, k, H, r)
        if k >= H - 5 and r.random() < (k - H + 6) / 6: c = tuple(int(v * .72) for v in c[:3]) + (255,)
        if k >= H - 3 and r.random() < .28: c = P["moss"][r.randrange(2)] if "moss" in P else c
        if (x * 13 + 5) % 17 == 0 and k < H - 4 and r.random() < .8: c = tuple(int(v * .8) for v in c[:3]) + (255,)
        return c
    return f

def brick_face(pal):
    def f(x, k, H, r):
        row = k // 4; off = 4 if row % 2 else 0
        if k % 4 == 3: return pal[1]                                    # mortar line
        if (x + off) % 8 == 7: return pal[1]
        v = pal[3] if (x + off) % 8 in (0, 1) and k % 4 == 0 else pal[2]
        return pal[1] if r.random() < .05 else v
    return f

def plaster_face(pal, under):
    def f(x, k, H, r):
        if k == 0: return pal[2]
        if k >= H - 3: return pal[0] if (x + k) % 3 else pal[1]                 # damp/dirt band at the foot
        return pal[1] if (x * 7 + k * 3) % 29 else pal[2]
    return f

def panel_face(pal):
    def f(x, k, H, r):
        if x % 8 == 7: return pal[0]
        if k == 0: return pal[3]
        if k % 9 == 8: return pal[1]
        return pal[2] if r.random() > .06 else pal[1]
    return f

def plank_face(pal):
    """Batch 33: marsh huts - vertical weathered boards, dark gaps, a few boards a shade off"""
    def f(x, k, H, r):
        if x % 4 == 3: return pal[0]                                    # gap between boards
        board = x // 4
        v = pal[2] if (board * 5) % 3 else pal[3] if x % 4 == 0 else pal[2]
        if (board * 7) % 5 == 0: v = pal[1]                             # an older, darker board
        if k == 0: return pal[4]
        return pal[1] if r.random() < .06 else v
    return f

def corrugated_face(pal):
    def f(x, k, H, r):
        v = (pal[3], pal[2], pal[2], pal[1])[x % 4]
        if r.random() < .05: return P["rust"][2]
        return v
    return f

def container_face(pal):
    """Batch 101: a shipping container's side - ribs every 3 px, top and bottom rails, rust runs"""
    def f(x, k, H, r):
        if k <= 1: return pal[3] if k == 0 else pal[1]                    # top rail
        if k >= H - 2: return pal[1] if k == H - 2 else pal[0]            # bottom rail
        v = (pal[3], pal[2], pal[1])[x % 3]
        if (x * 7) % 23 == 0 and r.random() < .7: v = P["rust"][1]        # a rust run down the ribs
        return v
    return f

def rock_face(pal):
    def f(x, k, H, r):
        n = ((x * 5 + (k // 3) * 11) % 7)
        v = pal[2] if n > 2 else pal[1]
        if k % 5 == 4 and n > 1: v = pal[0]
        if x % 6 == 0 and r.random() < .5: v = pal[3]
        return v
    return f

def leaf_face(pal, seed=11):
    """Batch 127: the canopy's front - the same leaf clusters as the top, lit at the rim and darker down into the shade,
    with trunks showing under it (was flat speckle, which read as a clipped box)."""
    tex = leaf_tex(pal, seed)
    def f(x, k, H, r):
        c = tex[(k + 5) % C, x]
        if not c[3]: c = pal[1]
        s = 1.08 if k < 2 else 1.0 - 0.38 * k / H                     # lit rim, then into the shade
        v = tuple(min(255, int(ch * s)) for ch in c[:3]) + (255,)
        if k > H - 6 and (x * 5) % 11 in (2, 3) and r.random() < .85: v = P["bark"][1]   # trunks under the canopy
        return v
    return f

def junk_face(pal):
    def f(x, k, H, r):
        return r.choice((P["rust"][1], P["rust"][2], P["tin"][1], P["tin"][2], P["steel"][1], P["wood"][2]))
    return f

def plain_cap(pal, speck=None):
    def f(x, y, r):
        v = pal[0]
        if speck and r.random() < .08: v = speck
        return v
    return f

def tile16(draw, seed):
    """a 16x16 tileable texture: draw(img3x3, rng) paints on a 48x48 canvas, the centre is kept
    after folding the 8 surrounding copies back in (so shapes wrap across cell edges)"""
    big = blank(48, 48); draw(big, rng(seed))
    out = blank(16, 16)
    for oy in (0, 16, 32):
        for ox in (0, 16, 32): out.alpha_composite(big.crop((ox, oy, ox + 16, oy + 16)))
    return np.array(out)

def leaf_tex(pal, seed):
    def draw(im, r):
        for y in range(16, 32):
            for x in range(16, 32): px(im, x, y, pal[0])
        for _ in range(60):                                     # Batch 31: scrubby small leaf clusters, not round puffs
            cx, cy, rad = r.uniform(4, 44), r.uniform(4, 44), r.uniform(1.4, 2.6)
            for yy in range(int(cy - rad), int(cy + rad) + 1):
                for xx in range(int(cx - rad), int(cx + rad) + 1):
                    if ((xx - cx) / rad) ** 2 + ((yy - cy) / rad) ** 2 <= 1 and r.random() > .2:
                        px(im, xx, yy, pal[3 if (xx - cx) + (yy - cy) < -0.5 else 2 if r.random() > .4 else 1])
    return tile16(draw, seed)

def rock_tex(pal, seed):
    def draw(im, r):
        for y in range(16, 32):
            for x in range(16, 32): px(im, x, y, pal[2])
        for _ in range(12): px(im, r.randrange(48), r.randrange(48), pal[3])
        for _ in range(8): px(im, r.randrange(48), r.randrange(48), pal[1])
        for _ in range(4): px(im, r.randrange(48), r.randrange(48), P["grass"][2])
    return tile16(draw, seed)

def junk_tex(seed):
    def draw(im, r):
        d = ImageDraw.Draw(im)
        d.rectangle([16, 16, 31, 31], fill=P["rust"][1])
        for _ in range(26):
            pal = r.choice((P["rust"], P["tin"], P["steel"], P["wood"], P["olive"]))
            x, y = r.uniform(0, 44), r.uniform(0, 44); w, h = r.randint(3, 8), r.randint(2, 5)
            d.rectangle([x, y, x + w, y + h], fill=pal[2]); d.line([(x, y), (x + w, y)], fill=pal[3])
            d.line([(x, y + h), (x + w, y + h)], fill=pal[0])
    return tile16(draw, seed)

def tex_cap(tex):
    return lambda x, y, r: tuple(tex[y, x]) if tex[y, x][3] else K

def rock_cap(pal): return tex_cap(rock_tex(pal, 5))
def leaf_cap(pal): return tex_cap(leaf_tex(pal, 7))

# name: (H, T, organic, cap(x,y,r)->c, face(x,k,H,r)->c, cap_edge_light)
STOREY2 = 40  # Batch 119 (Yan: buildings "go with the concept"): brick, plaster and concrete buildings are two storeys, 20 px a floor
STOREY = 28   # Batch 106: a built wall is one real storey (3 m; a 16 px person is 1.8 m). Was 18 (2 m). RoofView.Lift matches.
MATERIALS = {
    "wall":       (STOREY2, 10, False, plain_cap([P["concrete"][3]], P["concrete"][2]), weather(brick_face(P["brick"])), P["concrete"][4]),
    "plaster":    (STOREY2, 10, False, plain_cap([P["plaster"][3]], P["plaster"][2]), weather(plaster_face(P["plaster"], P["brick"])), P["plaster"][4]),
    "concrete":   (STOREY2, 10, False, plain_cap([P["concrete"][3]], P["concrete"][2]), weather(panel_face(P["concrete"])), P["concrete"][4]),
    "hull":       (STOREY, 10, False, plain_cap([P["steel"][3]], P["rust"][2]), corrugated_face(P["steel"]), P["steel"][4]),
    "metal":      (STOREY, 10, False, plain_cap([P["tin"][2]], P["rust"][2]), corrugated_face(P["tin"]), P["tin"][3]),
    "plank":      (STOREY, 10, False, plain_cap([P["wood"][3]], P["wood"][2]), weather(plank_face(P["wood"])), P["wood"][4]),
    "cliff":      (16, 16, True, rock_cap(P["rock"]), rock_face(P["rock"]), P["rock"][4]),
    "foliage":    (20, 16, True, leaf_cap(P["foliage"]), leaf_face(P["foliage"]), None),
    "redfoliage": (20, 16, True, leaf_cap(P["redleaf"]), leaf_face(P["redleaf"]), None),
    "junk":       (12, 16, True, tex_cap(junk_tex(9)), junk_face(None), None),
    "container":  (20, 10, False, plain_cap([hx("#7f4a35")], hx("#6a3a29")), weather(container_face(ramp("#4a2a20", "#6b3a2a", "#874a33", "#9c5c40"))), hx("#9c5c40")),   # Batch 101
}
WALL_H = {k: v[0] for k, v in MATERIALS.items()}

def roof_mask(f, mask, corners, edge=5):
    """footprint pixels at least `edge` px from any open side: a roof, with a parapet around it"""
    E = edge; big = np.zeros((C + 2 * E, C + 2 * E), bool)
    big[E:E + C, E:E + C] = f
    if mask & 1: big[:E, E:E + C] = f[0][None, :]
    if mask & 4: big[E + C:, E:E + C] = f[-1][None, :]
    if mask & 8: big[E:E + C, :E] = f[:, 0][:, None]
    if mask & 2: big[E:E + C, E + C:] = f[:, -1][:, None]
    if corners & 1: big[:E, E + C:] = True
    if corners & 2: big[E + C:, E + C:] = True
    if corners & 4: big[E + C:, :E] = True
    if corners & 8: big[:E, :E] = True
    roof = np.zeros((C, C), bool)
    for y in range(C):
        for x in range(C):
            roof[y, x] = big[y:y + 2 * E + 1, x:x + 2 * E + 1].all()
    return roof

TAR = ramp("#2f2c2a", "#3a3634", "#45403c", "#57524b")
def roof_px(mat, x, y, r):
    if mat in ("hull", "metal", "plank"):
        return (P["tin"][1], P["tin"][2], P["tin"][2], P["tin"][3])[x % 4] if r.random() > .04 else P["rust"][2]
    if mat == "concrete":
        return P["concrete"][1] if x % 16 == 15 or y % 16 == 15 else P["concrete"][2] if r.random() > .08 else P["concrete"][1]
    v = r.random()
    return TAR[3] if v < .05 else TAR[1] if v < .3 else TAR[2]

def build(mat, mask, variant=0, extra=None, corners=0):
    """extra: None | 'window' | 'door' | 'metaldoor' | 'crack'"""
    import zlib
    H, T, organic, cap, face, lip = MATERIALS[mat]
    r = rng(zlib.crc32(f"{mat}{mask}{variant}{extra}{corners}".encode()))
    f = footprint(mask, T, 1000 * variant + mask * 7 + len(mat), organic, corners)
    roof = roof_mask(f, mask, corners) if not organic else np.zeros((C, C), bool)
    im = np.zeros((C + H, C, 4), np.uint8)
    if extra == "door" and (mask & 5) == 5 and not (mask & 10):      # door in a N-S wall: planks across the cap
        for y in range(C):
            for x in range(C):
                if f[y, x]: im[y, x] = P["wood"][3] if (y % 4) else P["wood"][1]
    else:
        for y in range(C):
            for x in range(C):
                if roof[y, x]:
                    shade = (x > 0 and f[y, x - 1] and not roof[y, x - 1]) or (y > 0 and f[y - 1, x] and not roof[y - 1, x])
                    im[y, x] = TAR[0] if shade else roof_px(mat, x, y, r)       # parapet throws a shadow onto the roof
                elif f[y, x]:
                    im[y, x] = cap(x, y, r)
    # face below every column's bottom edge (unless the footprint runs on into the south cell)
    face_top, cuts = {}, {}
    for x in range(C):
        ys = np.where(f[:, x])[0]
        if len(ys) == 0: continue
        yb = ys.max()
        if yb == C - 1 and mask & 4: continue
        face_top[x] = yb + 1
        if organic:   # Batch 127: the front rounds off at an open east/west side too (no box-like vertical sides)
            e = min(x if not mask & 8 else C, C - 1 - x if not mask & 2 else C)
            cut = int(round(7 - (49 - (7 - e) ** 2) ** 0.5)) if e < 7 else 0
            cuts[x] = cut
        for k in range(H - cuts.get(x, 0)):
            im[yb + 1 + k, x] = face(x, k, H, r)
        if H >= 36 and mat in ("wall", "plaster", "concrete"):          # Batch 119: the floor band between the two storeys
            for k in (H // 2 - 1, H // 2):
                c = im[yb + 1 + k, x]; im[yb + 1 + k, x] = tuple(min(255, int(v * (1.18 if k == H // 2 - 1 else 0.72))) for v in c[:3]) + (255,)
        if lip is not None: im[yb, x] = lip                         # sunlit top edge of the wall
    # openings on a horizontal stretch of built wall
    fx = [x for x in face_top]
    if extra in ("window", "uwin", "door", "metaldoor") and fx and T < C and not ((mask & 5) == 5 and not (mask & 10)):
        ft = min(face_top[x] for x in fx)
        two = H >= 36                                                    # Batch 119: two storeys - the top half is the upper floor
        if extra in ("window", "uwin"):
            for k0 in ([3] if extra == "uwin" else [3, H // 2 + 3]) if two else [2]:
                boarded = r.random() < .5
                for k in range(k0, k0 + 11):
                    for x in range(3, 13):
                        edge = k in (k0, k0 + 10) or x in (3, 12)
                        if edge: c = P["wood"][1]
                        elif boarded: c = P["wood"][3] if (k - k0 - 2) % 3 else P["wood"][1]
                        else: c = hx("#1d2629") if (x - k) % 6 else hx("#4f6468")
                        if edge and k == k0 + 10: c = P["wood"][3]           # sill
                        im[ft + k, x] = c
        else:
            pal = P["wood"] if extra == "door" else P["steel"]
            for k in range(H - 19 if two else 1, H):                     # a door is one floor tall
                for x in range(3, 13):
                    edge = x in (3, 12) or k == (H - 19 if two else 1)
                    c = pal[0] if edge else pal[2] if (x % 3) else pal[1]
                    if extra == "metaldoor" and not edge: c = pal[3] if k in (5, 12) else pal[2]
                    im[ft + k, x] = c
            im[ft + (H - 9 if two else H // 2 + 1), 10] = P["steel"][4] if extra == "door" else P["rust"][3]
    if extra == "crack" and fx:
        x = r.randrange(3, 13); ft = face_top.get(x, None)
        if ft is not None:
            for k in range(2, H - 2):
                im[ft + k, x] = P["rock"][0]; x = max(1, min(14, x + r.choice((-1, 0, 0, 1))))
    # inner outline: opaque pixels touching transparency (canvas edges count as "continues"),
    # plus the ground line at the bottom of each face
    al = im[..., 3] > 0; out = im.copy()
    for y in range(C + H):
        for x in range(C):
            if not al[y, x]: continue
            nb = [(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)]
            if y == C - 1 and mask & 4 and f[y, x]: nb = nb[:3]                  # cap runs on into the south cell
            if any(0 <= nx < C and 0 <= ny < C + H and not al[ny, nx] for nx, ny in nb) or y == C + H - 1 and not (mask & 4 and T >= C):
                out[y, x] = dk(im[y, x])
    for x, ft in face_top.items():
        fb = ft + H - 1 - cuts.get(x, 0)
        if fb < C + H: out[fb, x] = dk(im[fb, x])
        if ft - 1 >= 0 and lip is None and al[ft - 1, x]: out[ft - 1, x] = dk(im[ft - 1, x]) if T < C else out[ft - 1, x]
    # cap/face seam: one dark row so the top reads as a separate plane
    for x, ft in face_top.items():
        if ft < C + H and out[ft, x][3]: out[ft, x] = tuple(int(v * .7) for v in out[ft, x][:3]) + (255,)
    return Image.fromarray(out, "RGBA")

def ruinify(im, seed, maxcut=11):
    """Batch 97 (Yan: "walls look too neat for a decaying building"): a roofless ruin's wall - each column sunk by a ragged
    amount so the top is broken and uneven, the broken edge rough and dark, bricks knocked out of the face, moss at the foot."""
    a = np.array(im); h, w = a.shape[:2]; r = rng(seed)
    cut, c = [], r.randint(2, 7)
    for x in range(w):
        c = max(0, min(maxcut, c + r.choice((-2, -1, 0, 0, 1, 2)) + (3 if r.random() < 0.08 else 0)))
        cut.append(c)
    out = np.zeros_like(a)
    for x in range(w):
        ys = np.where(a[:, x, 3] > 0)[0]
        if len(ys) == 0: continue
        top, bot = ys.min(), ys.max(); k = min(cut[x], max(0, bot - top - 4))
        out[top + k:bot + 1, x] = a[top:bot + 1 - k, x]                  # the column sunk by k: a lower, ragged top
        t = top + k
        out[t, x, :3] = (out[t, x, :3] * 0.55).astype(np.uint8)           # broken edge
        if r.random() < 0.3 and t + 1 <= bot: out[t + 1, x, :3] = (out[t + 1, x, :3] * 0.7).astype(np.uint8)
    for _ in range(r.randint(2, 5)):                                       # knocked-out bricks
        x, y = r.randrange(1, w - 2), r.randrange(h // 2, h - 3)
        for dx in range(2):
            if out[y, x + dx, 3]: out[y, x + dx, :3] = (out[y, x + dx, :3] * 0.45).astype(np.uint8)
    moss = P["moss"]
    for x in range(w):                                                     # moss creeping up from the foot
        ys = np.where(out[:, x, 3] > 0)[0]
        if len(ys) == 0: continue
        for y in range(ys.max() - r.randint(0, 3), ys.max()):
            if r.random() < 0.5: out[y, x, :3] = moss[r.randrange(2)][:3]
    return Image.fromarray(out, "RGBA")

def wear(im, seed, mat):
    """Batch 98 (Yan: "walls look too neat for a decaying building"): an intact wall that has stood thirty years - chips out of
    the top edge, water stains running down from it, plaster fallen off in patches showing the brick, a crack."""
    a = np.array(im); h, w = a.shape[:2]; r = rng(seed); H = MATERIALS[mat][0]
    face0 = h - H
    for _ in range(r.randint(2, 4)):                                       # chips out of the top edge
        x = r.randrange(1, w - 3)
        for dx in range(r.randint(1, 3)):
            ys = np.where(a[:, x + dx, 3] > 0)[0]
            if len(ys): a[ys.min():ys.min() + r.randint(1, 2), x + dx, 3] = 0
    for _ in range(r.randint(2, 4)):                                       # rain stains down the face
        x = r.randrange(w - 1); y0 = face0 + r.randint(0, 2); sw = r.randint(1, 2)
        for y in range(y0, min(h - 1, y0 + r.randint(6, 13))):
            for dx in range(sw):
                if a[y, x + dx, 3]: a[y, x + dx, :3] = (a[y, x + dx, :3] * 0.66).astype(np.uint8)
    if mat == "plaster":                                                   # plaster fallen off: brick underneath
        for _ in range(r.randint(2, 3)):
            x0, y0 = r.randrange(0, w - 6), r.randrange(face0 + 1, h - 6)
            pw, ph = r.randint(4, 6), r.randint(3, 5)
            for y in range(y0, y0 + ph):
                for x in range(x0 + (y - y0) % 2, x0 + pw - (y - y0) % 2):
                    if a[y, x, 3]: a[y, x, :3] = P["brick"][2 if (x + y) % 4 else 1][:3]
    if r.random() < 0.6:                                                   # a crack
        x = r.randrange(2, w - 2)
        for y in range(face0 + 1, min(h - 2, face0 + r.randint(5, H - 3))):
            if a[y, x, 3]: a[y, x, :3] = (a[y, x, :3] * 0.5).astype(np.uint8)
            x = max(1, min(w - 2, x + r.choice((-1, 0, 0, 1))))
    return Image.fromarray(a, "RGBA")

RUBBLE_TOP = {"wall": P["brick"], "plaster": P["brick"], "plank": P["wood"]}
def ruin_wall(mat, mask, cs, key):
    """the cap of a broken wall is exposed core (brick under the plaster, splintered boards), not a clean concrete top"""
    import zlib
    pal = RUBBLE_TOP[mat]
    def rubble_cap(x, y, r):
        v = r.random()
        return pal[1] if v < .3 else pal[2] if v < .7 else pal[3] if v < .9 else P["concrete"][2]
    H, T, organic, cap, face, lip = MATERIALS[mat]
    MATERIALS[mat] = (H, T, organic, rubble_cap, face, None)
    try: im = build(mat, mask, 0, None, cs)
    finally: MATERIALS[mat] = (H, T, organic, cap, face, lip)
    return ruinify(im, zlib.crc32(f"{mat}{key}".encode()), 1 if mask & 1 else 11)   # a run from the north only loses a little, or it breaks apart

def all_tiles():
    """name -> image. w_<mat>_<mask>_<v>; built walls also get _window/_door/_metaldoor/_crack faces"""
    t = {}
    for mat, (H, T, organic, *_r) in MATERIALS.items():
        nv = 3 if organic else 1
        for mask in range(16):
            for v in range(nv): t[f"w_{mat}_{mask}_{v}"] = build(mat, mask, v)
            if organic: continue
            vc = valid_corners(mask)
            for cs in range(16):
                if cs & ~vc: continue
                key = f"{mask}c{cs}" if cs else f"{mask}"
                if cs: t[f"w_{mat}_{key}_0"] = build(mat, mask, 0, None, cs)
                for ex in ("window", "door", "metaldoor", "crack"): t[f"w_{mat}_{key}_{ex}"] = build(mat, mask, 0, ex, cs)
                if H >= 36: t[f"w_{mat}_{key}_uwin"] = build(mat, mask, 0, "uwin", cs)   # Batch 119: an upper-floor window on a plain wall
                if mat in ("wall", "plaster", "plank"): t[f"w_{mat}_{key}_ruin"] = ruin_wall(mat, mask, cs, key)   # Batch 97
                if mat in ("wall", "plaster", "plank", "concrete"):   # Batch 98: worn, for intact walls
                    t[f"w_{mat}_{key}_worn"] = wear(build(mat, mask, 0, None, cs), __import__("zlib").crc32(f"worn{mat}{key}".encode()), mat)
    return t

if __name__ == "__main__":
    # preview: a small compound - a room with a door and window, a cliff, a hedge, a junk pile
    import sys
    sys.path.insert(0, "/home/claude/push/ArtSource")
    import ground
    lay = [
        "..........................",
        ".WWWWWWWW.....CCCCC.......",
        ".W......W....CCCCCCC..FF..",
        ".W......n....CCCCCC..FFFF.",
        ".W......W.....CCCC....FF..",
        ".WWWDWWWW.................",
        "..........PPPPPP..JJ......",
        "..........P....P.JJJ......",
        "..KKKK....P....D..J.......",
        "..K..K....PPwPPP..........",
        "..KKdK....................",
        "..........................",
    ]
    mats = {"W": "wall", "C": "cliff", "F": "foliage", "P": "plaster", "J": "junk", "K": "concrete"}
    h, w = len(lay), len(lay[0])
    def cellmat(x, y):
        if not (0 <= x < w and 0 <= y < h): return None
        ch = lay[y][x]
        if ch in mats: return mats[ch]
        if ch in "Dnwd": return "door"
        return None
    def wallish(x, y): return cellmat(x, y) is not None
    g = ground.GROUNDS["grass"](3); fl = ground.GROUNDS["floor"](4)
    can = blank(w * C, h * C + 40)
    for ty in range(0, can.height, 64):
        for tx in range(0, can.width, 64): can.paste(g, (tx, ty))
    for (x0, y0, x1, y1) in ((2, 2, 8, 5), (11, 7, 15, 9), (3, 9, 5, 10)):
        for y in range(y0, y1):
            for x in range(x0, x1): can.paste(fl.crop((0, 0, 16, 16)), (x * C, y * C + 40))
    import random
    for y in range(h):
        for x in range(w):
            m = cellmat(x, y)
            if not m: continue
            mask = sum(b for b, (dx, dy) in ((1, (0, -1)), (2, (1, 0)), (4, (0, 1)), (8, (-1, 0))) if wallish(x + dx, y + dy))
            ch = lay[y][x]
            if m == "door":
                # door: material of a neighbouring wall
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    mm = cellmat(x + dx, y + dy)
                    if mm and mm != "door": base = mm; break
                ex = {"D": "door", "n": "door", "w": "window", "d": "metaldoor"}[ch]
                tile = build(base, mask, 0, ex)
            else:
                organic = MATERIALS[m][2]
                tile = build(m, mask, (x * 7 + y * 3) % 3 if organic else 0)
            Hh = tile.height - C
            can.alpha_composite(tile, (x * C, y * C + 40 - Hh))
    can = can.resize((can.width * 3, can.height * 3), Image.NEAREST)
    can.save("/tmp/claude-0/walls_test.png"); print(can.size, len(all_tiles()))
