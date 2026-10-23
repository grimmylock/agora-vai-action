"""RT 7z-d: the dive in four directions (Yan: "we need an animation for diving upward and downward as well"), from the harness's
real numbers (`dotnet /tmp/test.dll diveframes dive_<name>.json dx dy`), drawn as TacticalGridView draws a posed body: squashed,
turned, lifted, its shadow on the ground. A strip of moments per direction.
  python3 Tools/render/dive_dirs.py out.png right:dive_right.json up:dive_up.json ..."""
import json, math, os, sys
from PIL import Image, ImageDraw, ImageFont
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
world = Image.open(os.path.join(R, "Tools/render/_render/perimeter.png")).convert("RGBA")
atlas = Image.open(os.path.join(R, "Assets/Resources/Sprites/armor_atlas.png")).convert("RGBA")
body = atlas.crop((30 % 16 * 16, 30 // 16 * 16, 30 % 16 * 16 + 16, 30 // 16 * 16 + 16))
TX, TY, S = 70, 170, 5
BG = world.crop(((TX - 3) * 16, (TY - 4) * 16, (TX + 4) * 16, (TY + 4) * 16))    # 7 x 8 tiles, the dive starts at (3.5, 4.5)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 18)
def draw(dx, dy, lift, tilt, sx, sy, shadow):
    img = BG.copy(); gx, gy = (3.5 + dx) * 16, (4.5 + dy) * 16
    feet = 7 * (1 - sy) * math.cos(math.radians(tilt)); cy = gy - lift * 16 + feet
    sw, sh = max(1, round(12 * shadow)), max(1, round(4 * shadow)); shy = gy + 7 * math.cos(math.radians(tilt))
    ImageDraw.Draw(img).ellipse((gx - sw / 2, shy - sh / 2, gx + sw / 2, shy + sh / 2), fill=(50, 33, 37, 140))
    b = body.resize((max(1, round(64 * sx)), max(1, round(64 * sy))), Image.NEAREST).rotate(tilt, resample=Image.NEAREST, expand=True)
    b = b.resize((max(1, round(b.width / 4)), max(1, round(b.height / 4))), Image.NEAREST)
    img.alpha_composite(b, (round(gx - b.width / 2), round(cy - b.height / 2)))
    return img.resize((img.width * S, img.height * S), Image.NEAREST)
keys = [0.07, 0.18, 0.28, 0.4, 0.5, 0.85, 1.12]
rows = [(a.split(":")[0], json.load(open(a.split(":")[1]))) for a in sys.argv[2:]]
cw, ch = BG.width * S, BG.height * S
sheet = Image.new("RGBA", (130 + len(keys) * (cw + 6), len(rows) * (ch + 6) + 34), (24, 26, 28, 255)); d = ImageDraw.Draw(sheet)
for i, k in enumerate(keys): d.text((130 + i * (cw + 6) + 6, 8), f"{k:.2f} s", font=font, fill=(220, 220, 210))
for r, (name, fr) in enumerate(rows):
    d.text((8, 34 + r * (ch + 6) + ch // 2 - 10), name, font=font, fill=(235, 220, 140))
    for i, k in enumerate(keys):
        f = min(fr, key=lambda f: abs(f[0] - k))
        sheet.alpha_composite(draw(*f[1:8]), (130 + i * (cw + 6), 34 + r * (ch + 6)))
sheet.save(sys.argv[1]); print(sheet.size)
