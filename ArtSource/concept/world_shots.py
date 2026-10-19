# In-game style shots of the real zone renders (harness) at game zoom, with the AtmosphereView
# overlays mimicked (same numbers as Atmosphere.Look): wash, fog, rain, light texture (2 texels/tile,
# bilinear), lightning. Units with guns drawn like mock_frame. Line of sight is not simulated.
import sys, math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
sys.path.insert(0, "C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/concept"); sys.path.insert(0, "C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/world")
import weather_mock as wm, concept, mock_frame as mf

FW, FH = 384, 216                      # world px in view
FONT = ImageFont.truetype("C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/fonts/Silkscreen-Regular.ttf", 22)

def look(hour, weather, underground=False):
    if underground: return np.array([20, 26, 30]) / 255, .18, .7, 5, 10, 0, 0, False
    return wm.look(hour, weather)

def lightmap(px_, py_, vision, cone, aim, night):
    tw, th = FW // 8 + 2, FH // 8 + 2
    a = np.zeros((th, tw))
    for j in range(th):
        for i in range(tw):
            gx, gy = (i + .5) / 2, (j + .5) / 2
            dx, dy = gx - px_, gy - py_; d = math.hypot(dx, dy)
            lit = min(1, max(0, (vision - d) / 1.5 + .3))
            if cone > 0 and d > .5:
                da = abs(math.remainder(math.atan2(-dy, dx) - aim, 2 * math.pi))
                lit = max(lit, min(1, max(0, (.6 - da) / .25)) * min(1, max(0, (cone - d) / 3)))
            a[j, i] = 1 - (1 - .6 * (1 - lit)) * (1 - night * (1 - lit))
    im = Image.fromarray((a * 255).astype(np.uint8)).resize((tw * 16, th * 16), Image.BILINEAR)
    return np.asarray(im).astype(np.float32)[:FH * 2, :FW * 2] / 255

def shot(zone, x0, y0, hour, weather, label, player, enemies=(), aim_at=None, underground=False, flash=False):
    world = Image.open(f"C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/_out/z_{zone}.png").convert("RGBA")
    view = world.crop((x0, y0, x0 + FW, y0 + FH)).resize((FW * 2, FH * 2), Image.NEAREST)
    vk = [13.5, 7.5, 27.5, 5]
    mf.unit(view, "Rookie Field Suit", "VK74", vk, player)
    for (ex, ey, armor) in enemies: mf.unit(view, armor, "VK74", vk, (ex, ey), flip=ex < player[0])
    img = np.asarray(view.convert("RGB")).astype(np.float32) / 255
    wash, wa, night, vision, cone, fog, rain, lightning = look(hour, weather, underground)
    if wm.GRADE: wa *= .5
    img = img * (1 - wa) + wash * wa
    H2, W2 = img.shape[:2]
    if fog:
        n = concept.noise(W2, H2, 80, 11) * .7 + concept.noise(W2, H2, 26, 12) * .3
        f = (np.clip((n - .35) / .45, 0, 1) ** 2 * .55)[..., None]
        img = img * (1 - f) + np.array([.72, .74, .7]) * f
    if rain:
        r = np.zeros((H2, W2)); rnd = np.random.RandomState(3)
        for k in range(int(1400 * rain)):
            x, y, ln = rnd.randint(W2), rnd.randint(H2), rnd.randint(6, 14)
            for s in range(ln):
                xx, yy = x - s // 4, y + s
                if 0 <= xx < W2 and 0 <= yy < H2: r[yy, xx] = .3 + .3 * s / ln
        img = img * (1 - r[..., None]) + np.array([.75, .8, .9]) * r[..., None]
    ptx, pty = player[0] / 16, player[1] / 16 - .5                   # player tile in frame coords
    tx, ty = aim_at if aim_at else (ptx + 5, pty)
    aim = math.atan2(-(ty - pty), tx - ptx)
    lm = lightmap(ptx, pty, vision, cone, aim, night)[..., None]
    mist = min(1, max(0, fog * (1 - night * 2)))                    # same as AtmosphereView: fog fades to grey, not black
    img = img * (1 - lm) + (np.array([5, 7, 13]) + (np.array([150, 156, 150]) - np.array([5, 7, 13])) * mist) / 255 * lm
    if flash: img = img * .45 + np.array([.85, .88, 1]) * .55
    if wm.GRADE: img = wm.apply_grade(img, wm.grade(hour, weather, underground))
    out = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).resize((1920, 1080), Image.NEAREST)
    d = ImageDraw.Draw(out); d.rectangle([0, 0, 20 + int(FONT.getlength(label)), 40], fill=(15, 17, 19)); d.text((10, 8), label, font=FONT, fill=(230, 220, 190))
    return out

SHOTS = [
    # zone, x0, y0, hour, weather, label, player (frame px at the feet, frame is 384x216), enemies, aim tile
    ("perimeter", 1100, 1180, 7.0, "Fog", "PERIMETER  07:00  MORNING FOG", (150, 120), [(250, 70, "Raider Jacket")], None),
    ("perimeter", 350, 700, 12.0, "Overcast", "PERIMETER  12:00  OVERCAST", (190, 120), [(330, 80, "Raider Jacket")], None),
    ("crimson_woods", 1150, 1900, 18.0, "Clear", "CRIMSON WOODS  18:00  GOLDEN DUSK", (250, 150), [(330, 60, "Raider Stitched Coat")], None),
    ("perimeter", 1500, 1250, 14.0, "Rain", "PERIMETER CAMP  14:00  RAIN", (160, 140), [(240, 200, "Raider Jacket")], None),
    ("rail_yards", 300, 150, 1.0, "Clear", "RAIL YARDS  01:00  NIGHT, FLASHLIGHT", (120, 140), [(300, 110, "Raider Stitched Coat")], (18, 6.5)),
    ("black_valley", 2250, 720, 22.0, "Storm", "BLACK VALLEY  22:00  STORM", (160, 175), [(300, 120, "Raider Jacket")], (19, 7)),
    ("marshes", 450, 1200, 5.6, "Fog", "MARSHES  05:40  DAWN FOG", (80, 80), [], None),
    ("perimeter", 700, 980, 19.4, "Overcast", "PERIMETER  19:25  DUSK", (190, 140), [(330, 100, "Raider Jacket")], None),
]

if __name__ == "__main__":
    which = [int(a) for a in sys.argv[1:]] or range(len(SHOTS))
    for i in which:
        z, x0, y0, h, w, label, p, en, aim = SHOTS[i]
        shot(z, x0, y0, h, w, label, p, en, aim).save(f"C:/Users/yan-s/Desktop/agora vai/agora vai/ArtSource/_out/shot_{i}.png"); print(i, label)
