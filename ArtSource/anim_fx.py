# Hand-built little effects for the mutants' attack previews (dust, motion streaks, the bite's hit on the target). Pixel clumps drawn per frame from a fixed seed, in the sprites' palette
# (dust: the world's straw-grey; blood: the game's dark red). They are previews here; in the game the same shapes would go through CombatFx.PxAt-style events.
import math, random
from PIL import Image, ImageDraw

DUST = [(176, 166, 140), (150, 141, 118), (120, 112, 94)]
BLOOD = [(196, 54, 52), (140, 30, 34), (92, 18, 24)]

def dust_puff(im, x, y, k, n=7, seed=1, big=1.0):
    """k = 0..n-1: a few clumps rise and spread along the ground, thinning out; (x, y) = the ground under it"""
    if k < 0 or k >= n: return
    rnd = random.Random(seed); u = (k + 1) / n
    for i in range(7):
        a = math.pi * (0.1 + 0.8 * rnd.random()); d = (4 + 9 * rnd.random()) * u * big; r = (2.2 + 2.2 * rnd.random()) * (0.55 + 0.7 * math.sin(math.pi * min(1, u * 1.1))) * big
        cx, cy = x + math.cos(a) * d * (1 if i % 2 else -1), y - math.sin(a) * d * 0.7
        col = DUST[min(2, int(u * 3))] if u < 0.85 else DUST[2]
        for dy in range(-int(r) - 1, int(r) + 2):
            for dx in range(-int(r) - 1, int(r) + 2):
                if dx * dx + dy * dy <= r * r and (u < 0.6 or (dx + dy + i) % 2 == 0) and 0 <= int(cx + dx) < im.width and 0 <= int(cy + dy) < im.height:
                    im.putpixel((int(cx + dx), int(cy + dy)), col + (255,))

def speed_lines(im, x, y, w, k, seed=2):
    rnd = random.Random(seed + k)
    for i in range(3):
        yy = y - 6 + i * 6 + rnd.randint(-1, 1); l = 6 + rnd.randint(0, 6); xx = x - (k % 3) * 2
        for j in range(l):
            if 0 <= xx - j < im.width and 0 <= yy < im.height: im.putpixel((xx - j, yy), (214, 208, 190, 255 - j * 18 if j < 13 else 40))

def bite_hit(im, x, y, k, n=7):
    """at the target: a white flash, claw/tooth slashes and drops of blood flying off"""
    if k < 0 or k >= n: return
    d = ImageDraw.Draw(im)
    if k <= 1:
        r = 3 + 2 * (1 - k)
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)) + tuple((dx * i, dy * i) for i in range(2, int(r) + 1) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
            if 0 <= x + dx < im.width and 0 <= y + dy < im.height: im.putpixel((x + dx, y + dy), (255, 250, 232, 255))
    if 0 <= k <= 3:                                                              # three slashes
        for s in range(3):
            x0, y0 = x - 5 + s * 3, y - 6 + s; x1, y1 = x0 + 5, y0 + 9
            n_px = int(max(abs(x1 - x0), abs(y1 - y0)) * (k + 1) / 4)
            for i in range(n_px + 1):
                px_, py_ = x0 + (x1 - x0) * i / 9, y0 + (y1 - y0) * i / 9
                if 0 <= int(px_) < im.width and 0 <= int(py_) < im.height: im.putpixel((int(px_), int(py_)), (240, 236, 224, 255) if k < 3 else (170, 160, 150, 255))
    rnd = random.Random(7)
    for i in range(7):
        a = math.radians(-70 + 140 * rnd.random()); v = 5 + 9 * rnd.random(); t = (k + 0.5) / n
        bx = x + math.cos(a) * v * t * 1.2; by = y - math.sin(a) * v * t + 14 * t * t
        if 0 <= int(bx) < im.width and 0 <= int(by) < im.height: im.putpixel((int(bx), int(by)), BLOOD[min(2, int(t * 3))] + (255,))
