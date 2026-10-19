# Same scene (concept A art) through a day: the target looks for the Atmosphere system.
import math, numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import concept as C

W, H = C.W, C.H
PX, PY = 150, 108                                     # player (flashlight origin)

def arr(im): return np.asarray(im).astype(np.float32) / 255
def img(a): return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
yy, xx = np.mgrid[0:H, 0:W]
dist = np.sqrt((xx - PX) ** 2 + (yy - PY) ** 2)

def grade(a, mul, add=(0, 0, 0), sat=1.0, gamma=1.0):
    lum = a.mean(-1, keepdims=True); a = lum + (a - lum) * sat
    return np.clip(a ** gamma * np.array(mul) + np.array(add), 0, 1)

def vignette(a, k, cx=.5, cy=.5):
    v = np.sqrt(((xx - W * cx) / (W / 2)) ** 2 + ((yy - H * cy) / (H / 2)) ** 2)
    return a * (1 - k * np.clip(v - .55, 0, 1))[..., None]

def fog(a, density, color, seed=3, drift=0):
    n = C.noise(W + 64, H + 32, 40, seed) * .6 + C.noise(W + 64, H + 32, 14, seed + 1) * .4
    n = n[16:16 + H, drift:drift + W]
    f = np.clip(density * (0.55 + n), 0, 0.9)[..., None]
    return a * (1 - f) + np.array(color) * f

def rain(a, n, length, alpha, seed=5, splashes=0):
    im = img(a).convert("RGBA"); lay = Image.new("RGBA", (W, H)); d = ImageDraw.Draw(lay); r = np.random.RandomState(seed)
    for _ in range(n):
        x, y = r.randint(-20, W), r.randint(-20, H)
        d.line([(x, y), (x + length * .25, y + length)], fill=(200, 210, 220, int(255 * alpha * r.uniform(.5, 1))))
    for _ in range(splashes):
        x, y = r.randint(0, W), r.randint(0, H)
        d.point([(x - 1, y), (x + 1, y)], fill=(210, 220, 230, int(160 * alpha)))
    im.alpha_composite(lay); return arr(im.convert("RGB"))

def flashlight(a, ambient, aim_deg, color=(1, .95, .8), reach=210, width=.5):
    ang = np.arctan2(yy - PY, xx - PX); da = np.abs((ang - math.radians(aim_deg) + np.pi) % (2 * np.pi) - np.pi)
    cone = np.clip(1 - da / width, 0, 1) ** .6 * np.clip(1 - dist / reach, 0, 1) ** .8
    near = np.clip(1 - dist / 30, 0, 1)
    l = np.clip(np.maximum(cone, near), 0, 1)[..., None]
    return a * (np.array(ambient) + l * np.array(color) * 1.05)

def grain(a, s, seed=1): return a + np.random.RandomState(seed).normal(0, s / 255, (H, W, 1))

def frame(name):
    if name == "DAWN  06:00  MIST":
        a = arr(C.scene("A", shadow="cast", sun=(-1.4, .25), raw=True))
        a = grade(a, (.86, .88, .98), (.03, .03, .05), sat=.7, gamma=1.05)
        a = fog(a, .55, (.72, .74, .80)); a = vignette(a, .35); return grain(a, 2)
    if name == "MORNING  09:00":
        a = arr(C.scene("A", shadow="cast", sun=(-1.2, .35), raw=True))
        a = grade(a, (1.08, 1.0, .86), (.03, .02, 0), sat=.95)
        hi = np.clip(a.mean(-1, keepdims=True) - .4, 0, 1); a = a + hi * np.array([.22, .15, .04])
        return vignette(grain(a, 2), .3, .35, .25)
    if name == "MIDDAY  13:00  OVERCAST":
        a = arr(C.scene("A", shadow="blob", raw=True))
        a = grade(a, (.97, .98, .97), (.035, .035, .03), sat=.8)
        return vignette(grain(a, 2.5), .35)
    if name == "EVENING  18:30":
        a = arr(C.scene("A", shadow="cast", sun=(1.8, .3), raw=True))
        a = grade(a, (1.12, .9, .72), (.04, .015, 0), sat=1.0, gamma=1.08)
        hi = np.clip(a.mean(-1, keepdims=True) - .38, 0, 1); a = a + hi * np.array([.3, .12, 0])
        return vignette(grain(a, 2), .5, .75, .2)
    if name == "RAIN  15:00":
        a = arr(C.scene("A", shadow="none", raw=True))
        a = grade(a, (.78, .82, .86), (.02, .025, .035), sat=.6, gamma=1.1)
        a = fog(a, .22, (.55, .58, .62), seed=8)
        a = rain(a, 520, 7, .32, splashes=160); return vignette(grain(a, 3), .45)
    if name == "NIGHT  23:00  FLASHLIGHT":
        a = arr(C.scene("A", shadow="none", raw=True))
        a = grade(a, (.62, .72, .95), (0, 0, .01), sat=.5)
        a = flashlight(a, (.24, .28, .38), -5); return vignette(grain(a, 5), .6)
    if name == "STORM  NIGHT  LIGHTNING":
        a = arr(C.scene("A", shadow="none", raw=True))
        a = grade(a, (.75, .82, 1.0), (.1, .11, .15), sat=.3)        # lightning frame: flat blue-white flash
        a = rain(a, 900, 10, .45, seed=9, splashes=260); return vignette(grain(a, 5), .5)
    raise KeyError(name)

NAMES = ["DAWN  06:00  MIST", "MORNING  09:00", "MIDDAY  13:00  OVERCAST", "EVENING  18:30",
         "RAIN  15:00", "NIGHT  23:00  FLASHLIGHT", "STORM  NIGHT  LIGHTNING"]
if __name__ == "__main__":
    f = ImageFont.truetype("/home/claude/art/fonts/Silkscreen-Regular.ttf", 22)
    tiles = []
    for i, n in enumerate(NAMES):
        im = img(frame(n)).resize((W * C.SC, H * C.SC), Image.NEAREST)
        im.save(f"/tmp/claude-0/tod_{i}.png"); tiles.append((n, im.resize((768, 432), Image.LANCZOS))); print(n)
    cols = 2; rows = (len(tiles) + 1) // 2
    sheet = Image.new("RGB", (cols * 788, rows * 470), (20, 22, 23)); d = ImageDraw.Draw(sheet)
    for i, (n, t) in enumerate(tiles):
        x, y = (i % cols) * 788, (i // cols) * 470
        sheet.paste(t, (x + 10, y + 32)); d.text((x + 12, y + 6), n, font=f, fill=(230, 220, 190))
    sheet.save("/tmp/claude-0/tod_sheet.png")
