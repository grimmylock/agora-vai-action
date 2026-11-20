# The cathedral exterior as one set-piece prop (2026-11-20; Yan: "it is still doesn't have that gothic cathedral style building, the walls and tiles are fine but we need shape", with a photo of Cologne cathedral).
# The roof system only draws boxes, so the whole outside is painted here in the game's front-oblique view (only south faces and roofs show): twin west towers with tall spires, a gabled front with a rose window and a
# pointed portal, a steep slate nave roof with lower aisle roofs, a transept, a slender crossing spire, an apse with conical roofs, buttress piers with pinnacles and flying arches. Double density (scale 2): a tile is 32 px.
# Footprint on the Tier 6 map: 34 x 29 tiles (x 114..147, y 121..149), front (south) at the bottom, the door at tiles 16-17 of the front row. Look "cathedral" on the anchor tile (131, 150).
import math
import numpy as np
from PIL import Image, ImageDraw
import props
from props import Prop
from worldlib import *

T = 32; M = 40; WT = 34; DEPTH = 24; HMAX = 640; BOT = 36
Wpx = WT * T + 2 * M; Hpx = DEPTH * T + HMAX + BOT; BASE = Hpx - BOT
def P(xt, dt, h): return (M + xt * T, BASE - dt * T - h)

GOTH = P_gothic = ramp("#201d1c", "#2e2a29", "#403b39", "#58524e", "#78716b")
SLATE = ramp("#131d1b", "#1e2d29", "#2b4038", "#3c5a4e", "#548070")
BAYER = np.array([[0, 2], [3, 1]]) / 4.0

def _h(a, b, k=0): return ((a * 73856093) ^ (b * 19349663) ^ (k * 83492791)) % 1000 / 1000.0
def fbm_(w, h, seed, cell=24): return value_noise(w, h, cell, seed, wrap=False)

class Canvas:
    def __init__(self): self.im = Image.new("RGBA", (Wpx, Hpx), (0, 0, 0, 0)); self.noise = fbm_(Wpx, Hpx, 7, 28); self.noise2 = fbm_(Wpx, Hpx, 11, 9)
    def mask(self, pts):
        m = Image.new("L", self.im.size, 0); ImageDraw.Draw(m).polygon(pts, fill=255); return m
    def fill(self, pts, fn):
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]; x0, x1, y0, y1 = max(0, int(min(xs))), min(Wpx, int(max(xs)) + 2), max(0, int(min(ys))), min(Hpx, int(max(ys)) + 2)
        if x1 <= x0 or y1 <= y0: return
        m = np.array(self.mask(pts).crop((x0, y0, x1, y1))) > 0
        yy, xx = np.mgrid[y0:y1, x0:x1]; rgb = fn(xx, yy)                                        # (h, w, 4)
        a = np.array(self.im); sub = a[y0:y1, x0:x1]; sub[m] = rgb[m]; self.im = Image.fromarray(a, "RGBA")

def ramp_idx(v, pal, xx, yy):
    th = (BAYER[yy % 2, xx % 2] - 0.375) * 0.8; k = len(pal) - 1
    idx = np.clip(np.floor(np.clip(v, 0, 1) * k + 0.5 + th), 0, k).astype(int)
    return np.array(pal, np.uint8)[idx]

def stone(light, seed=1, bw=30, bh=14, pal=GOTH, stain=0.0):
    def fn(xx, yy, cv=None):
        row = yy // bh; off = (row % 2) * (bw // 2); col = (xx + off) // bw
        tone = np.vectorize(lambda a, b: _h(int(a), int(b), seed))(col, row) - 0.5
        n = cv.noise[yy, xx] if cv is not None else 0.5; n2 = cv.noise2[yy, xx] if cv is not None else 0.5
        v = 0.42 * light + tone * 0.25 + (n - 0.5) * 0.3 + (n2 - 0.5) * 0.12 - stain * (n > 0.62) * 0.15
        out = ramp_idx(v, pal, xx, yy)
        mortar = ((yy % bh) == bh - 1) | (((xx + off) % bw) == bw - 1)
        out[mortar] = np.array(pal[0], np.uint8); lit = ((yy % bh) == 0) & ~mortar; out[lit] = np.array(pal[min(4, 3)], np.uint8) if light > 0.7 else out[lit]
        return out
    return fn

def slate(light, seed=2, sw=14, sh=9):
    def fn(xx, yy, cv=None):
        row = yy // sh; off = (row % 2) * (sw // 2); col = (xx + off) // sw
        tone = np.vectorize(lambda a, b: _h(int(a), int(b), seed))(col, row) - 0.5
        n = cv.noise[yy, xx] if cv is not None else 0.5
        v = 0.5 * light + tone * 0.28 + (n - 0.5) * 0.28
        out = ramp_idx(v, SLATE, xx, yy)
        edge = ((yy % sh) >= sh - 2); out[edge] = (out[edge] * 0.6).astype(np.uint8)
        side = ((xx + off) % sw) == sw - 1; out[side] = (out[side] * 0.7).astype(np.uint8)
        return out
    return fn

def paint(cv, pts, shader): cv.fill(pts, lambda xx, yy: shader(xx, yy, cv))
def lit(light, kind="stone", **kw): return stone(light, **kw) if kind == "stone" else slate(light, **kw)

def line_(cv, pts, col, w=1):
    ImageDraw.Draw(cv.im).line(pts, fill=col, width=w)

def lancet(cv, cx, base, w, h, glass=True, seed=0, lead=True):
    """a pointed window: straight sides to the springing, two arcs meeting at the apex. cx, base in px; w half-width."""
    pts = [(cx - w, base), (cx - w, base - h * 0.62)]
    for k in range(1, 9): a = k / 8; pts.append((cx - w + 2 * w * 0.0 + (cx - (cx - w)) * a - 0 , base - h * 0.62 - h * 0.38 * math.sin(a * math.pi / 2) * (1 - 0.0 * a)))
    pts = [(cx - w, base), (cx - w, base - h * 0.6)] + [(cx - w * math.cos(a * math.pi / 2) * 1.0 + w * 0 * a, base - h * 0.6 - h * 0.4 * math.sin(a * math.pi / 2)) for a in np.linspace(0.12, 1, 8)]
    # simpler and symmetric: build the left half then mirror
    left = [(cx - w, base), (cx - w, base - h * 0.58)] + [(cx - w + w * (1 - math.cos(a * math.pi / 2)) * 1.0, base - h * 0.58 - h * 0.42 * math.sin(a * math.pi / 2) ** 1.0) for a in np.linspace(0, 1, 9)[1:]]
    right = [(2 * cx - x, y) for x, y in reversed(left)]
    pts = left + right
    if glass:
        GL = [hx("#2a56a8"), hx("#b02a32"), hx("#d8a032"), hx("#2a8a4c"), hx("#36a0ac"), hx("#8a3a9c")]
        def fn(xx, yy, cv=None):
            cell_a = (xx + yy) // 7; cell_b = (xx - yy) // 7
            col = np.vectorize(lambda a, b: _h(int(a), int(b), seed + 5))(cell_a, cell_b)
            idx = (col * 6).astype(int) % 6; out = np.array(GL, np.uint8)[idx]
            leadm = (((xx + yy) % 7) == 0) | (((xx - yy) % 7) == 0) | (np.abs(xx - cx) < 1.2)
            out[leadm] = np.array(hx("#121317"), np.uint8); glow = (xx + yy) % 5 == 0; out[glow] = np.minimum(255, out[glow].astype(int) * 1.18).astype(np.uint8)
            return out
        cv.fill(pts, lambda xx, yy: fn(xx, yy))
    else: cv.fill(pts, lambda xx, yy: np.tile(np.array(hx("#08090b"), np.uint8), (yy.shape[0], yy.shape[1], 1)))
    ImageDraw.Draw(cv.im).line(pts + [pts[0]], fill=GOTH[4], width=2)                               # the lit stone reveal

def rose(cv, cx, cy, r, seed=3):
    GL = [hx("#2a56a8"), hx("#b02a32"), hx("#d8a032"), hx("#2a8a4c"), hx("#8a3a9c")]
    d = ImageDraw.Draw(cv.im)
    d.ellipse([cx - r - 3, cy - r - 3, cx + r + 3, cy + r + 3], fill=GOTH[4])
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=hx("#121317"))
    for ring, rr in ((0, r - 3), (1, r * 0.66), (2, r * 0.34)):                                         # concentric rings of petals
        n = 12 if ring == 0 else 8 if ring == 1 else 6
        for k in range(n):
            a = 2 * math.pi * k / n + ring * 0.3; px_, py_ = cx + math.cos(a) * rr * (0.78 if ring < 2 else 0.55), cy + math.sin(a) * rr * (0.78 if ring < 2 else 0.55)
            pr = rr * (0.22 if ring == 0 else 0.3 if ring == 1 else 0.45)
            d.ellipse([px_ - pr, py_ - pr, px_ + pr, py_ + pr], fill=GL[(k + ring * 2) % 5])
    d.ellipse([cx - r * 0.14, cy - r * 0.14, cx + r * 0.14, cy + r * 0.14], fill=hx("#e8c8a0"))
    for k in range(12): a = math.pi * k / 6; d.line([(cx, cy), (cx + math.cos(a) * r, cy + math.sin(a) * r)], fill=hx("#121317"), width=1)

def spire(cv, cx, base_y, base_half, tip_y, depth_px, seed=4, crockets=True):
    """a tall four-sided spire seen from the front-oblique: a lit west facet, the south facet, a dark east facet; crockets up the edges, a finial cross at the tip"""
    sw, se = cx - base_half, cx + base_half; fy = base_y; by = base_y - depth_px
    tip = (cx, tip_y)
    paint(cv, [(sw, fy), (cx, fy + 0), tip], lit(1.0, "slate", seed=seed, sw=10, sh=8))                           # (the west half of the south facet)
    paint(cv, [(cx, fy), (se, fy), tip], lit(0.62, "slate", seed=seed + 1, sw=10, sh=8))
    paint(cv, [(sw, fy), (sw + 0, by), tip], lit(1.25, "slate", seed=seed + 2, sw=10, sh=8)) if False else None
    d = ImageDraw.Draw(cv.im)
    d.line([(sw, fy), tip], fill=SLATE[4], width=2); d.line([(se, fy), tip], fill=SLATE[0], width=2); d.line([(cx, fy), tip], fill=SLATE[4], width=1)   # edges and the centre ridge
    if crockets:
        for k in range(1, 9):
            u = k / 9; yy_ = fy + (tip_y - fy) * u
            for sx_, sd in ((sw + (cx - sw) * u, -1), (se + (cx - se) * u, 1)):
                d.polygon([(sx_, yy_), (sx_ + sd * 5, yy_ - 1), (sx_ + sd * 1, yy_ - 7)], fill=GOTH[4] if sd < 0 else GOTH[1])
    d.line([(cx, tip_y), (cx, tip_y - 16)], fill=GOTH[4], width=2); d.line([(cx - 5, tip_y - 11), (cx + 5, tip_y - 11)], fill=GOTH[4], width=2)    # the finial cross

def pinnacle(cv, xp, yb, w=9, h=22, hh=34):
    paint(cv, [(xp - w // 2, yb), (xp + w // 2, yb), (xp + w // 2, yb - h), (xp - w // 2, yb - h)], lit(0.9, "stone", seed=9, bw=14, bh=8))
    paint(cv, [(xp - w // 2 - 1, yb - h), (xp + w // 2 + 1, yb - h), (xp, yb - h - hh // 2 - 4)], lit(0.95, "slate", seed=3, sw=6, sh=5))
    ImageDraw.Draw(cv.im).line([(xp, yb - h - hh // 2 - 4), (xp, yb - h - hh // 2 - 10)], fill=GOTH[4], width=2)

def edge(cv, pts, col=None, w=2):
    ImageDraw.Draw(cv.im).line(list(pts) + [pts[0]], fill=col or GOTH[0], width=w)

def roof(cv, pts, light, seed, sw=16, sh=10):
    paint(cv, pts, lit(light, "slate", seed=seed, sw=sw, sh=sh)); edge(cv, pts, SLATE[0], 2)

def wall(cv, pts, light, seed, **kw):
    paint(cv, pts, lit(light, "stone", seed=seed, **kw)); edge(cv, pts, GOTH[0], 2)

def cathedral(seed=1000):
    cv = Canvas(); d = ImageDraw.Draw(cv.im)
    nx0, nx1, nr = 10, 24, 17; wn, rn = 118, 270                         # nave: eaves, ridge (a steep pitch)
    # 1. the apse (far north end): five faces under conical roofs
    for (x0, x1, dt0, hw, ri, sd) in ((10.5, 14, 20, 96, 130, 1), (14, 20, 21, 98, 150, 2), (20, 23.5, 20, 96, 130, 3)):
        wall(cv, [P(x0, dt0, 0), P(x1, dt0, 0), P(x1, dt0, hw), P(x0, dt0, hw)], 0.65, sd)
        cx_ = (x0 + x1) / 2
        roof(cv, [P(x0 - 0.2, dt0 - 1.2, hw), P(x1 + 0.2, dt0 - 1.2, hw), P(cx_, dt0 + 1.5, hw + ri)], 1.1 if cx_ < 17.5 else 0.5, sd + 5, 10, 8)
        lancet(cv, P(cx_, dt0, 0)[0], P(cx_, dt0, 0)[1] - 18, 9, 52, seed=sd)
    # 2. the nave's north stretch (behind the transept): both slopes
    roof(cv, [P(nx0, 19, wn), P(nr, 19, rn), P(nr, 15, rn), P(nx0, 15, wn)], 1.15, 11)
    roof(cv, [P(nr, 19, rn), P(nx1, 19, wn), P(nx1, 15, wn), P(nr, 15, rn)], 0.42, 12)
    # 3. the transept (full width, ridge E-W): north slope dark, south slope lit, gabled dormers on the lit side, the crossing spire
    tz0, tz1, tw, tr = 9.5, 14.5, 112, 196
    roof(cv, [P(0, tz1, tw), P(34, tz1, tw), P(34, 12, tr), P(0, 12, tr)], 0.4, 32)
    roof(cv, [P(0, 12, tr), P(34, 12, tr), P(34, tz0, tw), P(0, tz0, tw)], 1.15, 31)
    line_(cv, [P(0, 12, tr), P(34, 12, tr)], SLATE[4], 4)
    for xs_ in (3, 8, 26, 31):
        bx, by = P(xs_, 10.2, 150)
        wall(cv, [(bx - 26, by), (bx + 26, by), (bx + 26, by - 48), (bx, by - 80), (bx - 26, by - 48)], 0.95, xs_, bw=14, bh=9)
        lancet(cv, bx, by - 3, 11, 58, seed=xs_)
    cxs, cys = P(17, 12, 214)
    wall(cv, [(cxs - 26, cys), (cxs + 26, cys), (cxs + 26, cys - 70), (cxs - 26, cys - 70)], 0.85, 41, bw=14, bh=9)
    lancet(cv, cxs, cys - 4, 12, 56, glass=False)
    spire(cv, cxs, cys - 70, 34, cys - 70 - 300, 30, seed=41)
    for sx_ in (-30, 30): pinnacle(cv, cxs + sx_, cys - 66, w=15, h=18, hh=46)
    # 4. the nave's south stretch (in front of the transept): both slopes, a crested ridge, clerestory gables on the lit slope
    roof(cv, [P(nx0, 9, wn), P(nr, 9, rn), P(nr, 1, rn), P(nx0, 1, wn)], 1.2, 13)
    roof(cv, [P(nr, 9, rn), P(nx1, 9, wn), P(nx1, 1, wn), P(nr, 1, rn)], 0.45, 14)
    line_(cv, [P(nr, 9, rn), P(nr, 1, rn)], SLATE[4], 4)
    # 5. aisle roofs and the buttress piers with flying arches (both sides of the nave's south stretch)
    roof(cv, [P(6, 9, 56), P(nx0, 9, 100), P(nx0, 1, 100), P(6, 1, 56)], 1.15, 21)
    roof(cv, [P(nx1, 9, 100), P(28, 9, 56), P(28, 1, 56), P(nx1, 1, 100)], 0.5, 22)
    for k in (2, 4.5, 7):
        for (xp, sgn, lg) in ((5.1, 1, 1.05), (28.9, -1, 0.5)):
            bx, by = P(xp, k, 0); top = 120
            wall(cv, [(bx - 13, by), (bx + 13, by), (bx + 13, by - top), (bx - 13, by - top)], lg * 0.9, int(k * 5), bw=16, bh=9)
            pinnacle(cv, bx, by - top, w=21, h=20, hh=62)
            ax0, ay0 = bx + sgn * 10, by - top + 10; ax1, ay1 = P(10 if sgn > 0 else 24, k, 132)
            pts = [(ax0, ay0)] + [(ax0 + (ax1 - ax0) * t, ay0 + (ay1 - ay0) * t - 34 * math.sin(math.pi * t)) for t in np.linspace(0, 1, 14)]
            ImageDraw.Draw(cv.im).line(pts, fill=GOTH[4] if sgn > 0 else GOTH[1], width=8); ImageDraw.Draw(cv.im).line([(x, y + 4) for x, y in pts], fill=GOTH[0], width=2)
    for k in (3, 6):
        for xs, lightv, base_h in ((12.4, 1.2, wn + 56), (21.6, 0.5, wn + 56)):
            bx, by = P(xs, k, base_h)
            wall(cv, [(bx - 17, by), (bx + 17, by), (bx + 17, by - 28), (bx, by - 54), (bx - 17, by - 28)], lightv * 0.9, k * 7, bw=12, bh=8)
            lancet(cv, bx, by - 2, 8, 34, seed=k)
    # 6. the front: a gabled west front between the towers, rose window, a triple portal, gallery of niches
    fx0, fx1 = 8, 26; fh = 178; fd = 0.6
    wall(cv, [P(fx0, fd, 0), P(fx1, fd, 0), P(fx1, fd, fh), P(fx0, fd, fh)], 0.9, 51)
    wall(cv, [P(fx0 - 0.3, fd, fh), P(fx1 + 0.3, fd, fh), P(17, fd, fh + 120)], 1.0, 52, bw=22, bh=11)
    d.line([P(fx0 - 0.3, fd, fh), P(17, fd, fh + 120), P(fx1 + 0.3, fd, fh)], fill=GOTH[4], width=4)
    for k in range(1, 9):
        u = k / 9; ax, ay = P(fx0 - 0.3 + (17 - fx0 + 0.3) * u, fd, fh + 120 * u); bx, by = P(fx1 + 0.3 - (fx1 + 0.3 - 17) * u, fd, fh + 120 * u)
        d.polygon([(ax - 7, ay), (ax, ay - 10), (ax + 1, ay)], fill=GOTH[4]); d.polygon([(bx + 7, by), (bx, by - 10), (bx - 1, by)], fill=GOTH[3])
    gx, gy = P(17, fd, fh + 120); d.line([(gx, gy), (gx, gy - 16)], fill=GOTH[4], width=3); d.line([(gx - 7, gy - 10), (gx + 7, gy - 10)], fill=GOTH[4], width=3)
    rcx, rcy = P(17, fd, 178); rose(cv, rcx, rcy, 50)
    px0, py0 = P(17, fd, 0)
    for (wd, hh) in ((76, 116), (60, 104), (46, 92), (34, 80)): lancet(cv, px0, py0, wd, hh, glass=False)
    paint(cv, [(px0 - 32, py0), (px0 + 32, py0), (px0 + 32, py0 - 62), (px0 - 32, py0 - 62)], lambda xx, yy, cv=None: np.tile(np.array(hx("#33220f"), np.uint8), (yy.shape[0], yy.shape[1], 1)))
    for k in range(-24, 25, 12): d.line([(px0 + k, py0), (px0 + k, py0 - 62)], fill=hx("#1a1208"), width=2)
    for yy_ in (14, 32, 50): d.line([(px0 - 32, py0 - yy_), (px0 + 32, py0 - yy_)], fill=hx("#4a4a52"), width=3)
    d.line([(px0, py0), (px0, py0 - 62)], fill=hx("#0a0806"), width=2)
    for sx_ in (-1, 1):
        cxo = P(17 + sx_ * 6.4, fd, 0)[0]; lancet(cv, cxo, py0, 17, 78, glass=False); lancet(cv, cxo, py0 - 76, 14, 60, seed=sx_ + 3)
    for k in range(9): bx, by = P(fx0 + 1 + k * 2, fd, fh - 4); d.rectangle([bx - 6, by - 14, bx + 6, by], outline=GOTH[0], fill=GOTH[1])
    # 7. the twin towers, nearest of all: three stages, lancets, a belfry, a parapet, tall spires with crockets and corner pinnacles
    for (tx0, tx1, sd, lg) in ((1.5, 8.5, 61, 1.1), (25.5, 32.5, 62, 0.55)):
        th = 240
        wall(cv, [P(tx0, 0, 0), P(tx1, 0, 0), P(tx1, 0, th), P(tx0, 0, th)], lg, sd, stain=0.8)
        for (h0, h1) in ((70, 76), (150, 156)): paint(cv, [P(tx0, 0, h0), P(tx1, 0, h0), P(tx1, 0, h1), P(tx0, 0, h1)], lit(min(1.3, lg + 0.3), "stone", seed=sd + 5, bw=40, bh=6))
        cx_t = P((tx0 + tx1) / 2, 0, 0)[0]; by0 = P(0, 0, 0)[1]
        for off in (-66, 0, 66): lancet(cv, cx_t + off, by0 - 6, 15, 56, glass=False)
        lancet(cv, cx_t, by0 - 88, 16, 60, seed=sd); lancet(cv, cx_t - 66, by0 - 92, 12, 48, glass=False); lancet(cv, cx_t + 66, by0 - 92, 12, 48, glass=False)
        for off in (-50, 0, 50): lancet(cv, cx_t + off, by0 - 164, 12, 62, glass=False)
        for cxo in (tx0, tx1): pinnacle(cv, P(cxo, 0, th)[0], P(cxo, 0, th)[1] + 4, w=21, h=24, hh=70)
        sp_cx = P((tx0 + tx1) / 2, 0, th)[0]; sp_by = P(0, 0, th)[1]
        wall(cv, [(P(tx0, 0, th)[0] - 4, sp_by), (P(tx1, 0, th)[0] + 4, sp_by), (P(tx1, 0, th)[0] + 4, sp_by - 24), (P(tx0, 0, th)[0] - 4, sp_by - 24)], lg + 0.15, sd + 9, bw=40, bh=6)
        half = (P(tx1, 0, 0)[0] - P(tx0, 0, 0)[0]) // 2 - 14
        wall(cv, [(sp_cx - half - 8, sp_by - 24), (sp_cx + half + 8, sp_by - 24), (sp_cx + half + 8, sp_by - 86), (sp_cx - half - 8, sp_by - 86)], lg + 0.05, sd + 11, bw=26, bh=9)       # an octagonal drum under the spire
        for off in (-0.55, 0, 0.55): lancet(cv, sp_cx + off * half, sp_by - 28, 9, 44, glass=False)
        for cxo in (-1, 1): pinnacle(cv, sp_cx + cxo * (half + 6), sp_by - 82, w=17, h=16, hh=46)
        spire(cv, sp_cx, sp_by - 86, half + 6, sp_by - 86 - 470, 30, seed=sd)
    for k in range(3): d.rectangle([px0 - 70 - k * 10, py0 + 1 + k * 4, px0 + 70 + k * 10, py0 + 4 + k * 4], fill=GOTH[3 - (k % 2)])
    return cv.im

def cathedral_prop(seed):
    im = cathedral(seed); return Prop(im, (Wpx // 2 + 16, Hpx - BOT + 22), None, True, scale=2)       # the front's centre falls on the boundary between tiles 130 and 131: the anchor tile's centre is half a tile right of it

NEW = {"cathedral": cathedral_prop}
VARIANTS = {"cathedral": 1}

if __name__ == "__main__":
    im = cathedral(); bg = Image.new("RGBA", im.size, (52, 56, 60, 255)); bg.alpha_composite(im)
    bg.convert("RGB").save("/tmp/cathedral_exterior_sheet.png"); print(im.size)
