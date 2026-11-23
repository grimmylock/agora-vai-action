"""2026-11-23: the hand swing test (HeldWeaponView.Show wrapper + HeldPoseMath.FreeHands), redrawn with the same numbers, real sprites (Raider Jacket cell, its walk frames, a Corvan battle rifle), WITHOUT Unity.
Rows: empty hands walking / running, rifle in hand walking / running. The rifle's grip positions are approximate (the real pose comes from HeldPoseMath.Frame); the bob and swing numbers are the code's.
Usage: python3 Tools/render/hands_swing.py out_dir -> hands_swing.gif, hands_swing_sheet.png"""
import math, os, sys
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))); SPR = os.path.join(ROOT, "Assets/Resources/Sprites")
Z = 8; BG = (58, 54, 42, 255); FPS = 50; STRIDE = 1.2
def clamp01(v): return max(0.0, min(1.0, v))
def af(speed): return 0.5 + (0.59 - 0.5) * clamp01(speed - 3.0)                 # UnitMotionView.AnimFactor: a walk takes half the steps per tile, a run the old walking cadence
def cycle(speed): return STRIDE * 2 + (2.03 - STRIDE * 2) * clamp01(speed - 3.0)   # HeldWeaponView.CycleTiles: tiles per full arm swing
def cell(n, x=0): return Image.open(os.path.join(SPR, n)).convert("RGBA").crop((x * 16, 0, x * 16 + 16, 16))
stand, wa, wb = cell("armor_atlas.png"), cell("armor_walk_a.png"), cell("armor_walk_b.png")
rifle = Image.open(os.path.join(SPR, "Weapons/weapon_CorvanBattleRifle.png")).convert("RGBA")
def glove():
    img = Image.new("RGBA", (5, 4)); col = {"K": (20, 22, 23, 255), "c": (42, 44, 48, 255), "l": (80, 84, 92, 255), "g": (54, 58, 64, 255), "d": (36, 38, 42, 255)}
    for y, row in enumerate([".KKK.", "KcllK", "KcgdK", ".KKK."]):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img
G = glove(); font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 13)
def free_hands(phase, swing, run):
    a = math.sin(phase) * swing; dx = round((1.2 + 2.2 * run) * a); lift = round(math.sin(phase) ** 2 * 2.2 * run)     # lowest in the middle, lifted at both ends: an arc
    return (14 + dx, 11.5 - lift, 2 + dx, 11.5 - lift)
def frame(t, speed, armed, label):
    W = 16 * Z + 40; H = 16 * Z + 50; c = Image.new("RGBA", (W, H), BG); d = ImageDraw.Draw(c)
    phase = speed * t * 2 * math.pi / cycle(speed); run = max(0.0, min(1.0, (speed - 3.4) / 1.0)); walk = 1.0
    q = int(speed * t * af(speed) * 4) % 4          # the game: HeldPoseMath.WalkFrame(stride) = int(stride * 4) % 4, stride in tiles walked x UnitMotionView.AnimFactor
    f = stand if q in (1, 3) else (wa if q == 0 else wb); step_body = q in (0, 2)
    ox, oy = 20, 30 - (1 * Z if step_body else 0)
    d.ellipse((20 + 3 * Z, 30 + 14 * Z, 20 + 13 * Z, 30 + 16 * Z), fill=(30, 28, 22, 255))
    body = f.resize((16 * Z, 16 * Z), Image.NEAREST); tilt = -2.0
    layer = Image.new("RGBA", (W, H)); layer.alpha_composite(body, (ox, oy)); layer = layer.rotate(tilt, resample=Image.NEAREST, center=(ox + 8 * Z, oy + 16 * Z))
    if not armed:
        rx, ry, lx, ly = free_hands(phase, walk, run)
        for (x, y, deg) in ((rx, ry, -90), (lx, ly, 90)):
            g = G.rotate(deg, expand=True).resize((G.height * Z, G.width * Z), Image.NEAREST)
            layer.alpha_composite(g, (ox + round(x * Z - g.width / 2), oy + round(y * Z - g.height / 2)))
    else:
        step = 1.0 if math.sin(phase * 2) > 0.2 else 0.0; s1 = math.sin(phase)
        walkbob = round(step) * (1 - run); swing = round(s1 * 2 * run); lift = round(s1 * s1 * 2 * run)     # the code's pendulum: fore-aft along the aim (here +x), lifted at the ends
        gx, gy = 10 + swing, 8.5 + walkbob + lift
        gun = rifle.resize((rifle.width * Z // 2, rifle.height * Z // 2), Image.NEAREST)
        gxp = ox + round((gx - 3) * Z); gyp = oy + round((16 - gy - 6) * Z)                       # the rifle's top-left; the hands are laid ON it (grip under the receiver, support under the handguard)
        layer.alpha_composite(gun, (gxp, gyp))
        for fx in (0.22, 0.62):
            g = G.resize((G.width * Z, G.height * Z), Image.NEAREST); layer.alpha_composite(g, (gxp + round(gun.width * fx - g.width / 2), gyp + round(gun.height * 0.62 - g.height / 2)))
    c.alpha_composite(layer); d.text((6, 4), label, font=font, fill=(240, 200, 90)); return c
# the rifle columns use the game's own stance (HeldPoseMath.Frame at the aim: Program.cs `restframes`), drawn with the held_anim.py helpers; the swing is the code's: a pendulum across the body on SCREEN axes
import json, subprocess
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as ha
subprocess.run(["dotnet", "/tmp/test.dll", "restframes", "/tmp/rest.json"], check=True, capture_output=True)
rest = {int(r["angle"]): r for r in json.load(open("/tmp/rest.json"))}
gun = Image.open(os.path.join(SPR, "Weapons", "weapon_CorvanBattleRifle.png")).convert("RGBA"); gl = ha.glove("#5e2424", "#c8916a")
bstand, bwa, bwb = cell("armor_back_atlas.png"), cell("armor_back_walk_a.png"), cell("armor_back_walk_b.png")
def rifle_col(t, speed, aim, label):
    r = rest[aim]; back = aim == 90
    phase = speed * t * 2 * math.pi / cycle(speed); run = max(0.0, min(1.0, (speed - 3.4) / 1.0)); walk = 1.0
    q = int(speed * t * af(speed) * 4) % 4          # the game: HeldPoseMath.WalkFrame(stride) = int(stride * 4) % 4, stride in tiles walked x UnitMotionView.AnimFactor
    fr = (bstand if q in (1, 3) else (bwa if q == 0 else bwb)) if back else (stand if q in (1, 3) else (wa if q == 0 else wb))
    body = fr.resize((16 * ha.S, 16 * ha.S), Image.NEAREST)
    s1 = math.sin(phase); amp = 1 + 2 * run; up = 1 + run
    dx = round(s1 * amp * walk); lift = round(s1 * s1 * up * walk); th = -s1 * 5 * run          # the code: Swing() + the gun's lean
    c = Image.new("RGBA", (ha.W * ha.S, ha.H * ha.S), ha.BG)
    def put_gun(): ha.paste(c, gun, r["grip"][0], r["grip"][1], r["gx"] + dx, r["gy"] - lift, r["deg"] + th, bool(r["left"]))
    def put_hands():
        g2 = gl.transpose(Image.FLIP_LEFT_RIGHT) if r["left"] else gl
        for (x, y, on) in ((r["sx"], r["sy"], r["sup"]), (r["hx"], r["hy"], 1)):
            if on: c.alpha_composite(g2, (round((ha.BX + x + dx) * ha.S - 2 * ha.S), round((ha.BY + y - lift) * ha.S - 1.5 * ha.S)))
    if back: put_gun(); put_hands(); c.alpha_composite(body, (ha.BX * ha.S, ha.BY * ha.S))          # from behind: the rifle and the hands are behind the body (sorting orders -3..-2)
    else: c.alpha_composite(body, (ha.BX * ha.S, ha.BY * ha.S)); put_gun(); put_hands()
    c = c.convert("RGB"); c = c.resize((c.width * 2 // 3, c.height * 2 // 3), Image.NEAREST)
    d = ImageDraw.Draw(c); d.text((6, 4), label, font=font, fill=(240, 200, 90)); return c
rows = [("empty: walk", 2.6), ("empty: run", 4.4)]
n = int(2.0 * FPS); frames = []
for i in range(n):
    t = i / FPS; cs = [frame(t, sp, False, lb).convert("RGB") for lb, sp in rows] + [rifle_col(t, 2.6, 0, "rifle: walk"), rifle_col(t, 4.4, 0, "rifle: run"), rifle_col(t, 2.6, 90, "from behind: walk"), rifle_col(t, 4.4, 90, "from behind: run")]
    h = max(x.height for x in cs); s = Image.new("RGB", (sum(x.width for x in cs), h), BG[:3]); xx = 0
    for x in cs: s.paste(x, (xx, 0)); xx += x.width
    frames.append(s)
os.makedirs(sys.argv[1], exist_ok=True)
frames[0].save(os.path.join(sys.argv[1], "hands_swing.gif"), save_all=True, append_images=frames[1:], duration=1000 // FPS, loop=0)
pick = [frames[k] for k in (0, 3, 6, 9)]; sh = Image.new("RGB", (pick[0].width, pick[0].height * 4)); [sh.paste(x, (0, k * x.height)) for k, x in enumerate(pick)]
sh.save(os.path.join(sys.argv[1], "hands_swing_sheet.png")); print("ok")

# ---- running in the four directions (rifle pointing the way he runs; the swing is on screen axes, as in the code) ----
ha.H += 20; ha.BY += 8; ha.W += 12; ha.BX += 9                                   # room for the muzzle above the head and to the sides
dirs = [("up", 90), ("down", 270), ("left", 180), ("right", 0)]
fr2 = []
for i in range(int(2.0 * FPS)):
    t = i / FPS; cs = [rifle_col(t, 4.4, aim, f"run {nm}") for nm, aim in dirs]
    s = Image.new("RGB", (sum(x.width for x in cs), cs[0].height), BG[:3]); xx = 0
    for x in cs: s.paste(x, (xx, 0)); xx += x.width
    fr2.append(s)
fr2[0].save(os.path.join(sys.argv[1], "run_directions.gif"), save_all=True, append_images=fr2[1:], duration=1000 // FPS, loop=0)
pick = [fr2[k] for k in (0, 7, 14, 21, 28, 35)]; sh = Image.new("RGB", (pick[0].width, pick[0].height * 3 // 1 // 3 * len(pick) // 1)); 
sh = Image.new("RGB", (pick[0].width, pick[0].height * len(pick))); [sh.paste(x, (0, k * x.height)) for k, x in enumerate(pick)]
sh.save(os.path.join(sys.argv[1], "run_directions_sheet.png")); print("dirs ok")
