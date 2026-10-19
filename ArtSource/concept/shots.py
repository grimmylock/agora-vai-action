# Game-zoom screenshots of the real (harness-rendered) zones under different times of day and
# weather. The atmosphere is the same math as AtmosphereView / Atmosphere.Look / Vision, including
# line of sight against the zone's walls (from DUMP_ZONE).
import sys, math
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, "/home/claude/art/world"); sys.path.insert(0, "/home/claude/art/concept"); sys.path.insert(0, "/home/claude/push/ArtSource")
import weather_mock as wm
import mock_frame as mf
import concept

VW, VH = 24, 13.5                          # tiles in view
FONT = ImageFont.truetype("/home/claude/art/fonts/Silkscreen-Regular.ttf", 22)

def load(zone):
    grid = [l for l in open(f"/tmp/claude-0/dump_{zone}.txt").read().split("\n") if l]
    return Image.open(f"/tmp/claude-0/z_{zone}.png").convert("RGBA"), grid

def blocks(grid, x, y): return y < 0 or x < 0 or y >= len(grid) or x >= len(grid[0]) or grid[y][x] in "#D"
def walk(grid, x, y): return 0 <= y < len(grid) and 0 <= x < len(grid[0]) and grid[y][x] in ".dL0123456789"

def los(grid, x0, y0, x1, y1):
    n = max(abs(x1 - x0), abs(y1 - y0))
    for i in range(1, n):
        x = round(x0 + (x1 - x0) * i / n); y = round(y0 + (y1 - y0) * i / n)
        if blocks(grid, x, y): return False
    return True

def pick(grid, seed=0):
    """the busiest window: some walls, some cover, mostly walkable"""
    H, W = len(grid), len(grid[0]); cands = []
    for cy in range(10, H - 10, 4):
        for cx in range(14, W - 14, 4):
            win = [grid[y][cx - 12:cx + 12] for y in range(cy - 7, cy + 7)]
            s = "".join(win); n = len(s)
            wall = s.count("#") / n; cover = (s.count("c") + s.count("=")) / n; open_ = s.count(".") / n
            if open_ < 0.45 or not walk(grid, cx, cy): continue
            score = min(wall, 0.25) * 3 + min(cover, 0.08) * 6 + (1.0 if "D" in s else 0) + min(s.count("~"), 10) * 0.02
            cands.append((score, cx, cy))
    cands.sort(reverse=True)
    _, cx, cy = cands[min(seed, len(cands) - 1)]
    return cx, cy

def lightmap(grid, px, py, x0, y0, vision, cone, aim, night):
    """texel grid (2 per tile) over the view, as AtmosphereView.DrawLight"""
    tw, th = int(VW * 2) + 2, int(VH * 2) + 2; a = np.zeros((th, tw))
    seen = {}
    for j in range(th):
        for i in range(tw):
            gx = x0 + (i + .5) / 2 - .5; gy = y0 + (j + .5) / 2 - .5
            cx, cy = int(math.floor(gx + .5)), int(math.floor(gy + .5))
            if (cx, cy) not in seen: seen[(cx, cy)] = math.hypot(cx - px, cy - py) <= 13.5 and los(grid, px, py, cx, cy)
            dx, dy = gx - px, gy - py; d = math.hypot(dx, dy)
            lit = min(1, max(0, (vision - d) / 1.5 + .3))
            if cone > 0 and d > .5:
                da = abs(math.remainder(math.atan2(-dy, dx) - aim, 2 * math.pi))
                lit = max(lit, min(1, max(0, (.6 - da) / .25)) * min(1, max(0, (cone - d) / 3)))
            if not seen[(cx, cy)]: lit = 0
            a[j, i] = 1 - (1 - .6 * (1 - lit)) * (1 - night * (1 - lit))
    return a

def shot(zone, hour, weather, label, seed=0, flash=False, aim_at=None):
    world, grid = load(zone)
    px, py = pick(grid, seed)
    x0, y0 = px - VW / 2 + .5, py - VH / 2 + .5
    view = world.crop((int(x0 * 16), int(y0 * 16), int(x0 * 16) + 384, int(y0 * 16) + 216)).resize((768, 432), Image.NEAREST)
    # enemies: walkable tiles 5-9 away in sight
    r = np.random.RandomState(seed + 3); foes = []
    for _ in range(400):
        ex, ey = px + r.randint(-9, 10), py + r.randint(-5, 6)
        if 5 <= math.hypot(ex - px, ey - py) <= 9 and walk(grid, ex, ey) and los(grid, px, py, ex, ey) and len(foes) < 2: foes.append((ex, ey))
    wash, wa, night, vision, cone, fog, rain, light = wm.look(hour, weather)
    if aim_at is None and foes: aim_at = foes[0]
    aim = math.atan2(-(aim_at[1] - py), aim_at[0] - px) if aim_at else 0
    lm = lightmap(grid, px, py, x0, y0, vision, cone, aim, night)
    def vis(x, y):                                   # Vision.Sees: in sight and lit enough
        d = math.hypot(x - px, y - py); lit = min(1, max(0, (vision - d) / 1.5 + .3))
        if cone > 0:
            da = abs(math.remainder(math.atan2(-(y - py), x - px) - aim, 2 * math.pi))
            lit = max(lit, min(1, max(0, (.6 - da) / .25)) * min(1, max(0, (cone - d) / 3)))
        return lit > .35 and los(grid, px, py, x, y) and lm[int((y - y0 + .5) * 2), int((x - x0 + .5) * 2)] < 0.45
    vk = [13.5, 7.5, 27.5, 5]
    def at(x, y): return (int((x - x0) * 16 + 8), int((y - y0) * 16 + 15))
    units = [(py, "Rookie Field Suit", "VK74", px, py, False)] + [(fy, ["Raider Jacket", "Raider Stitched Coat"][i], "VK74", fx, fy, fx < px) for i, (fx, fy) in enumerate(foes) if vis(fx, fy)]
    for _, armor, gun, ux, uy, flip in sorted(units):
        mf.unit(view, armor, gun, vk, at(ux, uy), flip=not flip if ux != px else (aim_at is not None and aim_at[0] < px))
    img = np.asarray(view.convert("RGB")).astype(np.float32) / 255
    img = img * (1 - wa) + wash * wa
    H2, W2 = img.shape[:2]
    if fog:
        n = concept.noise(W2, H2, 80, 11 + seed) * .7 + concept.noise(W2, H2, 26, 12 + seed) * .3
        f = np.clip((n - .35) / .45, 0, 1) ** 2 * .5
        img = img * (1 - f[..., None]) + np.array([.72, .74, .7]) * f[..., None]
    if rain:
        rr = np.zeros((H2, W2)); rnd = np.random.RandomState(3 + seed)
        for k in range(int(1600 * rain)):
            x, y, ln = rnd.randint(W2), rnd.randint(H2), rnd.randint(8, 18)
            for s in range(ln):
                xx, yy = x - s // 4, y + s
                if 0 <= xx < W2 and 0 <= yy < H2: rr[yy, xx] = .3 + .3 * s / ln
        img = img * (1 - rr[..., None]) + np.array([.75, .8, .9]) * rr[..., None]
    lmi = Image.fromarray((lm * 255).astype(np.uint8)).resize((lm.shape[1] * 16, lm.shape[0] * 16), Image.BILINEAR)   # a texel = half a tile = 16 px at 2x
    lmA = np.asarray(lmi).astype(np.float32)[:H2, :W2] / 255
    img = img * (1 - lmA[..., None]) + np.array([5, 7, 13]) / 255 * lmA[..., None]
    if flash: img = img * .45 + np.array([.85, .88, 1]) * .55
    out = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).resize((1920, 1080), Image.NEAREST)
    d = ImageDraw.Draw(out)
    name = zone.replace("_", " ").upper()
    d.rectangle([20, 20, 560, 92], fill=(21, 23, 26)); d.rectangle([20, 20, 560, 92], outline=(58, 62, 65), width=2)
    d.text((36, 30), name, font=FONT, fill=(230, 220, 192)); d.text((36, 62), label, font=ImageFont.truetype("/home/claude/art/fonts/Silkscreen-Regular.ttf", 14), fill=(125, 122, 112))
    return out

SHOTS = [("perimeter", 7.2, "Fog", "07:12  FOG", 0), ("perimeter", 12.5, "Overcast", "12:30  OVERCAST", 1),
         ("crimson_woods", 18.0, "Clear", "18:00  CLEAR", 0), ("abandoned_city", 14.0, "Rain", "14:00  RAIN", 0),
         ("rail_yards", 1.0, "Clear", "01:00  CLEAR, NIGHT", 0), ("marshes", 5.8, "Fog", "05:48  FOG, DAWN", 0),
         ("ghost_town", 22.0, "Storm", "22:00  STORM", 0), ("dried_lake", 16.0, "Clear", "16:00  CLEAR", 0),
         ("scrapyard", 19.6, "Overcast", "19:36  OVERCAST, DUSK", 0), ("black_valley", 10.0, "Rain", "10:00  RAIN", 0)]

if __name__ == "__main__":
    which = sys.argv[1:] or [str(i) for i in range(len(SHOTS))]
    for i in map(int, which):
        z, h, w, lab, sd = SHOTS[i]
        shot(z, h, w, lab, sd).save(f"/tmp/claude-0/shot_{i:02d}_{z}.png"); print(i, z)
