# Mimics AtmosphereView (wash, fog, rain, light texture at 2 texels/tile, bilinear) over concept A,
# for six times of day / weathers. Numbers copied from Atmosphere.Look().
import sys, math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, "C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/concept")
import concept
W, H = concept.W, concept.H
base = np.asarray(concept.scene("A", raw=True)).astype(np.float32) / 255
PX, PY = 150 / 16, 108 / 16 - 0.5        # player tile (world px / 16)

def C(r, g, b): return np.array([r, g, b]) / 255
KEYS = [(0, C(12, 16, 32), .30, .80), (4.5, C(12, 16, 32), .30, .78), (5.5, C(70, 66, 92), .30, .40), (7, C(214, 196, 168), .12, 0),
        (9.5, C(150, 152, 142), .08, 0), (16, C(150, 152, 142), .08, 0), (18, C(226, 172, 112), .11, 0), (19.5, C(96, 58, 92), .28, .35),
        (21, C(12, 16, 32), .30, .78), (24, C(12, 16, 32), .30, .80)]
def look(hour, w):
    i = 0
    while i < len(KEYS) - 2 and hour >= KEYS[i + 1][0]: i += 1
    a, b = KEYS[i], KEYS[i + 1]; t = min(1, max(0, (hour - a[0]) / (b[0] - a[0])))
    wash = a[1] + (b[1] - a[1]) * t; wa = a[2] + (b[2] - a[2]) * t; night = a[3] + (b[3] - a[3]) * t
    vision = 13 + (5 - 13) * night / .8; cone = 11 if night > .25 else 0; fog = rain = 0; light = False
    if w == "Overcast": wash = wash + (C(120, 126, 126) - wash) * .5; wa += .06
    if w == "Fog": wash = wash + (C(150, 156, 150) - wash) * .7; wa += .22; fog = 1; vision = min(vision, 7); cone = min(cone, 7)
    if w == "Rain": wash = wash + (C(70, 82, 96) - wash) * .6; wa += .14; rain = .5; vision -= 2
    if w == "Storm": wash = wash + (C(38, 44, 60) - wash) * .7; wa += .26; night = max(night, .25); rain = 1; light = True; vision -= 4
    return wash, wa, night, max(4, vision), cone, fog, rain, light

# Batch 35: the colour grade (Atmosphere.Look -> ColorGrade's URP Volume). GRADE=0 shows the old wash-only look.
GRADE = os.environ.get("GRADE", "1") != "0"
NIGHT_G = (.68, (.88, .92, 1.04), 0, .32)
GKEYS = [(0,) + NIGHT_G, (4.5,) + NIGHT_G, (5.5, .72, (.96, .93, 1.02), .03, .28), (7, .84, (1.04, 1, .94), .04, .22),
         (9.5, .82, (1, 1, .98), .04, .22), (16, .82, (1, 1, .98), .04, .22), (18, .95, (1.1, 1.02, .88), .06, .20),
         (19.5, .76, (.94, .9, 1), .02, .30), (21,) + NIGHT_G, (24,) + NIGHT_G]
def grade(hour, w, underground=False):
    if underground: return .7, np.array([.92, .98, 1]), 0, .34
    i = 0
    while i < len(GKEYS) - 2 and hour >= GKEYS[i + 1][0]: i += 1
    a, b = GKEYS[i], GKEYS[i + 1]; t = min(1, max(0, (hour - a[0]) / (b[0] - a[0])))
    sat = a[1] + (b[1] - a[1]) * t; filt = np.array(a[2]) + (np.array(b[2]) - np.array(a[2])) * t
    haze = a[3] + (b[3] - a[3]) * t; vig = a[4] + (b[4] - a[4]) * t
    if w == "Overcast": sat *= .92; filt = filt + (np.array([.97, 1, 1]) - filt) * .5
    if w == "Fog": sat *= .86; haze += .03; filt = filt + (1 - filt) * .6
    if w == "Rain": sat *= .86; filt = filt * np.array([.86, .89, .96])
    if w == "Storm": sat *= .82; filt = filt * np.array([.8, .83, .92]); vig += .06
    return max(.65, sat), filt, haze, vig

def apply_grade(img, g):
    """What URP does with ColorGrade's settings, in linear: saturation, colour filter, LDR lift, vignette."""
    sat, filt, haze, vig = g
    lin = np.clip(img, 0, 1) ** 2.2
    luma = (lin @ np.array([.2126, .7152, .0722]))[..., None]
    lin = (luma + (lin - luma) * sat) * filt ** 2.2
    lift = haze ** 2.2                                   # ColorGrade sends haze^2.2/0.2, URP scales it by 0.2
    lin = np.clip(lin, 0, 1); lin = lin + lift * (1 - lin)
    h, w_ = img.shape[:2]; yy, xx = np.mgrid[0:h, 0:w_]
    d2 = ((xx / w_ - .5) * vig * 3) ** 2 + ((yy / h - .5) * vig * 3) ** 2
    lin = lin * (np.clip(1 - d2, 0, 1) ** (.3 * 5))[..., None]
    return lin ** (1 / 2.2)

def lightmap(vision, cone, aim, night):
    tw, th = W // 8 + 2, H // 8 + 2                     # 2 texels per 16 px tile
    a = np.zeros((th, tw))
    for j in range(th):
        for i in range(tw):
            gx, gy = (i + .5) / 2 - .5, (j + .5) / 2 - .5
            dx, dy = gx - PX, gy - PY; d = math.hypot(dx, dy)
            near = min(1, max(0, (vision - d) / 1.5 + .3))
            lit = near
            if cone > 0 and d > .5:
                da = abs(math.remainder(math.atan2(-dy, dx) - aim, 2 * math.pi))
                lit = max(lit, min(1, max(0, (.6 - da) / .25)) * min(1, max(0, (cone - d) / 3)))
            unseen = .6 * (1 - lit); dark = night * (1 - lit)
            a[j, i] = 1 - (1 - unseen) * (1 - dark)
    im = Image.fromarray((a * 255).astype(np.uint8)).resize((tw * 8, th * 8), Image.BILINEAR)
    return np.asarray(im).astype(np.float32)[:H, :W] / 255

def render(hour, w, flash=False):
    wash, wa, night, vision, cone, fog, rain, lightning = look(hour, w)
    img = base.copy()
    if GRADE: wa *= .5                                  # AtmosphereView halves the wash while the grade runs
    img = img * (1 - wa) + wash * wa
    if fog:
        n = concept.noise(W, H, 40, 11) * .7 + concept.noise(W, H, 13, 12) * .3
        f = np.clip((n - .35) / .45, 0, 1) ** 2 * .55
        img = img * (1 - f[..., None]) + np.array([.72, .74, .7]) * f[..., None]
    if rain:
        r = np.zeros((H, W)); rnd = np.random.RandomState(3)
        for k in range(int(500 * rain)):
            x, y, ln = rnd.randint(W), rnd.randint(H), rnd.randint(4, 9)
            for s in range(ln):
                xx, yy = x - s // 4, y + s
                if 0 <= xx < W and 0 <= yy < H: r[yy, xx] = .35 + .3 * s / ln
        img = img * (1 - r[..., None]) + np.array([.75, .8, .9]) * r[..., None]
    aim = math.atan2(-(98 / 16 - 0.5 - PY), 262 / 16 - PX)          # toward the raider by the ruin
    lm = lightmap(vision, cone, aim, night)[..., None]
    img = img * (1 - lm) + np.array([5, 7, 13]) / 255 * lm
    if flash: img = img * .5 + np.array([.85, .88, 1]) * .5
    if GRADE: img = apply_grade(img, grade(hour, w))
    return Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8))

CASES = [(7.0, "Fog", "07:00  MORNING FOG"), (12.0, "Overcast", "12:00  OVERCAST"), (18.0, "Clear", "18:00  GOLDEN DUSK"),
         (14.0, "Rain", "14:00  RAIN"), (1.0, "Clear", "01:00  NIGHT, FLASHLIGHT"), (22.0, "Storm", "22:00  STORM")]
if __name__ == "__main__":
    f = ImageFont.truetype("C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/fonts/Silkscreen-Regular.ttf", 20)
    tiles = []
    for hour, w, label in CASES:
        im = render(hour, w).resize((W * 3, H * 3), Image.NEAREST)
        d = ImageDraw.Draw(im); d.rectangle([0, 0, 360, 30], fill=(15, 17, 19)); d.text((8, 4), label, font=f, fill=(230, 220, 190))
        tiles.append(im)
        im.save(f"C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/_out/weather_{label.split()[0].replace(':', '')}.png")
    tw, th = tiles[0].size
    sheet = Image.new("RGB", (tw * 2 + 10, th * 3 + 20), (15, 17, 19))
    for i, t in enumerate(tiles): sheet.paste(t, ((i % 2) * (tw + 10), (i // 2) * (th + 10)))
    sheet.save("C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/_out/weather_sheet.png"); print(sheet.size)
