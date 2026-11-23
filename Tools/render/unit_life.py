"""2026-11-23: the procedural life test on the Raider Jacket (UnitLife.cs), redrawn with the same numbers and the real sprites (armor_atlas / armor_walk_a / armor_walk_b, cell 0), WITHOUT Unity:
breathing (1 px up on the in-breath, 0.8 Hz), a weight shift to one side, walking (1 px hop on foot-down frames + 2 degree lean), a flinch (3 px back + 4 degrees, 0.18 s). Left = with life, right = as it was.
Usage: python3 Tools/render/unit_life.py out_dir -> unit_life.gif, unit_life_sheet.png"""
import math, os, sys
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))); SPR = os.path.join(ROOT, "Assets/Resources/Sprites")
Z = 8; PX = 1.0; BG = (58, 54, 42, 255)
def cell(n): return Image.open(os.path.join(SPR, n)).convert("RGBA").crop((0, 0, 16, 16))
stand, wa, wb = cell("armor_atlas.png"), cell("armor_walk_a.png"), cell("armor_walk_b.png")
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14)
FPS = 30
def draw(frame, ox, oy, tilt, label, life):
    W, H = 16 * Z + 40, 16 * Z + 40; c = Image.new("RGBA", (W, H), BG); d = ImageDraw.Draw(c)
    d.ellipse((20 + 3 * Z, 20 + 14 * Z, 20 + 13 * Z, 20 + 16 * Z), fill=(30, 28, 22, 255))                       # the shadow stays put
    im = frame.resize((16 * Z, 16 * Z), Image.NEAREST)
    if tilt: im = im.rotate(tilt, resample=Image.NEAREST, center=(8 * Z, 16 * Z))
    c.alpha_composite(im, (20 + int(ox * Z), 20 - int(oy * Z)))
    d.text((6, 3), label, font=font, fill=(240, 200, 90)); return c
def life(t, mode, hit_t=None, shift=(1.4, 2.4)):
    ox = oy = tilt = 0.0
    if mode == "idle":
        oy = 1.0 if math.sin(t * 0.8 * 2 * math.pi + 0.6) > 0.25 else 0.0
        if shift[0] <= t < shift[1]: ox = 1.0
    if mode == "walk": tilt = -2.0
    if hit_t is not None and 0 <= t - hit_t < 0.18:
        k = 1 - (t - hit_t) / 0.18; ox += round(-3 * k); tilt += 4 * k * (1 if True else -1)
    return ox, oy, tilt
frames = []; T = 0.0
script = [("idle", 3.2, None), ("walk", 2.0, None), ("idle", 1.4, 0.3)]
for mode, dur, hit in script:
    n = int(dur * FPS)
    for i in range(n):
        t = i / FPS
        if mode == "walk":
            q = int(t * 1.0 * 4 / 1.0 * 1.0) % 4          # stride 1 tile/s... one cycle per second here
            f = stand if q in (1, 3) else (wa if q == 0 else wb); step = q in (0, 2)
        else: f, step = stand, False
        ox, oy, tilt = life(t, mode, hit)
        if mode == "walk" and step: oy = 1.0
        a = draw(f, ox, oy, tilt, {"idle": "breathing, shifting weight", "walk": "walking: hop + lean", "hit": ""}[mode] if not (hit is not None and t >= hit) else "flinch", True)
        b = draw(f, 0, 0, 0, "as it was", False)
        s = Image.new("RGB", (a.width * 2, a.height)); s.paste(a.convert("RGB"), (0, 0)); s.paste(b.convert("RGB"), (a.width, 0)); frames.append(s)
os.makedirs(sys.argv[1], exist_ok=True)
frames[0].save(os.path.join(sys.argv[1], "unit_life.gif"), save_all=True, append_images=frames[1:], duration=1000 // FPS, loop=0)
pick = [frames[int(x * FPS)] for x in (0.2, 0.7, 1.7, 3.4, 4.0, 5.3, 5.38, 5.5)]
sheet = Image.new("RGB", (pick[0].width * 4, pick[0].height * 2))
for k, f in enumerate(pick): sheet.paste(f, ((k % 4) * f.width, (k // 4) * f.height))
sheet.save(os.path.join(sys.argv[1], "unit_life_sheet.png")); print("ok", len(frames))
