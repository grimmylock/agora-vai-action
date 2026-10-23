"""RT 7z: the dive, before and after, from the harness's real numbers (`dotnet /tmp/test.dll diveframes dive.json`: Rt.PlayerTick
moving the body, Rt.PoseOf posing it), drawn the way TacticalGridView draws it: the body sprite squashed along/across itself,
turned, lifted, its shadow left on the ground. Background: the map renderer's Bonfire Village. Real time: the dive runs in bullet
time (x0.3), so its 0.55 s last ~1.8 s on screen. No held gun or hands (HeldWeaponView isn't drawn here).
  python3 Tools/render/dive_anim.py dive.json out_dir   -> dive_before_after.gif + dive_strip.png"""
import json, math, os, sys
from PIL import Image, ImageDraw, ImageFont
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
frames = json.load(open(sys.argv[1])); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
world = Image.open(os.path.join(R, "Tools/render/_render/perimeter.png")).convert("RGBA")
atlas = Image.open(os.path.join(R, "Assets/Resources/Sprites/armor_atlas.png")).convert("RGBA")
body = atlas.crop((30 % 16 * 16, 30 // 16 * 16, 30 % 16 * 16 + 16, 30 // 16 * 16 + 16))   # Leather Jacket
TX, TY = 70, 170                                   # the open lane in the village square (as the range render)
BG = world.crop(((TX - 2) * 16, (TY - 3) * 16, (TX + 7) * 16, (TY + 2) * 16))         # 9 x 5 tiles
S = 5
SH = (50, 33, 37, 140)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 20)

def draw(dx, dy, lift, tilt, sx, sy, shadow):
    img = BG.copy()
    gx, gy = (2.5 + dx) * 16, (3.5 + dy) * 16                         # the body's middle on the ground (tile space -> px)
    feet = 7 * (1 - sy) * math.cos(math.radians(tilt))
    cy = gy - lift * 16 + feet                                         # lifted; a crouch keeps the feet down
    sw, sh = max(1, round(12 * shadow)), max(1, round(4 * shadow))     # the shadow stays on the ground
    shy = gy + 7 * math.cos(math.radians(tilt))
    d = ImageDraw.Draw(img); d.ellipse((gx - sw / 2, shy - sh / 2, gx + sw / 2, shy + sh / 2), fill=SH)
    b = body.resize((max(1, round(16 * sx * 4)), max(1, round(16 * sy * 4))), Image.NEAREST)   # squash at 4x so 1 px changes show
    b = b.rotate(tilt, resample=Image.NEAREST, expand=True)
    b = b.resize((max(1, round(b.width / 4)), max(1, round(b.height / 4))), Image.NEAREST)
    img.alpha_composite(b, (round(gx - b.width / 2), round(cy - b.height / 2)))
    return img.resize((img.width * S, img.height * S), Image.NEAREST)

def old(t, dx, dy):                                                    # RT batch 3's look: turned 70 deg in the air, 90 sprawled
    return draw(dx, dy, 0, -70 if t < 0.55 else -90 if t < 1.15 else 0, 1, 1, 1)

# resample to 30 fps of real time: game seconds run at x0.3 in bullet time
real, rows, clock = [], [], 0.0
for i, f in enumerate(frames):
    step = (frames[i + 1][0] - f[0]) if i + 1 < len(frames) else 1 / 60
    real.append((clock, f)); clock += step / (0.3 if f[8] else 1.0)
gif = []
t = 0.0
while t <= clock + 0.4:
    f = max((r for r in real if r[0] <= t), key=lambda r: r[0])[1]
    a, b = old(f[0], f[1], f[2]), draw(*f[1:8])
    pane = Image.new("RGBA", (a.width * 2 + 12, a.height + 40), (24, 26, 28, 255))
    pane.alpha_composite(a, (0, 40)); pane.alpha_composite(b, (a.width + 12, 40))
    dd = ImageDraw.Draw(pane)
    dd.text((10, 8), "before", font=font, fill=(200, 200, 190)); dd.text((a.width + 22, 8), "now: crouch, launch, arc, belly landing, get up", font=font, fill=(235, 220, 140))
    if f[8]: dd.text((a.width * 2 - 150, 8), "BULLET TIME", font=font, fill=(150, 200, 255))
    gif.append(pane.convert("P", palette=Image.ADAPTIVE, colors=128)); t += 1 / 30
gif[0].save(os.path.join(out, "dive_before_after.gif"), save_all=True, append_images=gif[1:], duration=33, loop=0, disposal=2)
# a strip of the key moments (game seconds)
keys = [0.0, 0.07, 0.12, 0.2, 0.28, 0.38, 0.5, 0.8, 1.05, 1.2]
cells = [draw(*min(frames, key=lambda f: abs(f[0] - k))[1:8]) for k in keys]
strip = Image.new("RGBA", (sum(c.width for c in cells[:5]) + 40, cells[0].height * 2 + 80), (24, 26, 28, 255)); x = 0; d = ImageDraw.Draw(strip)
for i, (k, c) in enumerate(zip(keys, cells)):
    col, row = i % 5, i // 5
    strip.alpha_composite(c, (col * (c.width + 10), row * (c.height + 40) + 30)); d.text((col * (c.width + 10) + 6, row * (c.height + 40) + 4), f"{k:.2f} s", font=font, fill=(220, 220, 210))
strip.save(os.path.join(out, "dive_strip.png"))
print(len(gif), "frames,", round(clock, 2), "s real")
