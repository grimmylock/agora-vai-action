"""2026-11-23: every weapon category in its four states (WeaponCarry.cs, HeldWeaponView): REST (stationary), WALK, RUN, AIM-WALK, drawn WITHOUT Unity with the game's own pose numbers (HeldPoseMath.Frame via
`dotnet /tmp/test.dll weaponstates`) and the same swing math as the code. One animated row per category (pistol, SMG, rifle, shotgun, sniper, machine gun), the character facing right, front sprite.
Usage: python3 Tools/render/weapon_states.py out_dir -> weapon_states.gif, weapon_states_sheet.png"""
import json, math, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as ha
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))); SPR = os.path.join(ROOT, "Assets/Resources/Sprites")
subprocess.run(["dotnet", "/tmp/test.dll", "weaponstates", "/tmp/ws.json"], check=True, capture_output=True)
rows = json.load(open("/tmp/ws.json")); FPS = 50
def cell(n): return Image.open(os.path.join(SPR, n)).convert("RGBA").crop((0, 0, 16, 16))
stand, wa, wb = cell("armor_atlas.png"), cell("armor_walk_a.png"), cell("armor_walk_b.png")
gl = ha.glove("#5e2424", "#c8916a"); font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14)
def clamp01(v): return max(0.0, min(1.0, v))
def af(speed): return 0.5 + 0.09 * clamp01(speed - 3.0)
def cycle(speed): return 2.4 + (2.03 - 2.4) * clamp01(speed - 3.0)
ha.H += 6; ha.BY += 3; ha.W += 8; ha.BX += 4
def panel(row, state, t):
    d = row["def"]; speed = {"rest": 0.0, "walk": 2.6, "run": 4.4, "aimwalk": 2.0}[state]
    gun = Image.open(os.path.join(SPR, "Weapons", f"weapon_{row['key']}.png")).convert("RGBA")
    carry = state != "aimwalk"; r = row["carry"] if carry else row["aim"]
    phase = speed * t * 2 * math.pi / cycle(speed); run = clamp01((speed - 3.4) / 1.0); walkK = 1.0 if speed > 0 else 0.0
    q = int(speed * t * af(speed) * 4) % 4
    f = stand if (speed == 0 or q in (1, 3)) else (wa if q == 0 else wb); stepBody = speed > 0 and q in (0, 2)
    s1 = math.sin(phase); amp = d["wa"] + (d["ra"] - d["wa"]) * run; up = d["wl"] + (d["rl"] - d["wl"]) * run
    if carry: dx = round(s1 * amp * walkK) - d["shift"]; lift = round(s1 * s1 * up * walkK) - d["drop"] * -1 * -1; th = -s1 * d["rt"] * run
    else: dx = 0; lift = -(1 if math.sin(phase * 2) > 0.2 and speed > 0 else 0); th = 0.0
    if carry: lift = round(s1 * s1 * up * walkK) - d["drop"]
    hop = 1 if stepBody else 0; lean = -(2 + clamp01(speed - 3.4)) if speed > 0 else 0.0
    body = f.resize((16 * ha.S, 16 * ha.S), Image.NEAREST)
    c = Image.new("RGBA", (ha.W * ha.S, ha.H * ha.S), ha.BG)
    c.alpha_composite(body, (ha.BX * ha.S, (ha.BY - hop) * ha.S))
    oy = -hop                                                                    # the body hops, the held weapon rides on it (the marker moves)
    ha.paste(c, gun, row["grip"][0], row["grip"][1], r["gx"] + dx, r["gy"] - lift + oy, r["deg"] + th, False)
    free = carry and d.get("free")
    for (x, y, on) in ((r["sx"], r["sy"], r["sup"] and not free), (r["hx"], r["hy"], 1)):
        if on: c.alpha_composite(gl, (round((ha.BX + x + dx) * ha.S - 2 * ha.S), round((ha.BY + y - lift + oy) * ha.S - 1.5 * ha.S)))
    if free:                                                                      # the other arm hangs at the side and swings (HeldPoseMath.FreeHands), fingers down
        a_ = math.sin(phase) * walkK; ddx = round((1.2 + 2.2 * run) * a_); lf = round(math.sin(phase) ** 2 * 2.2 * run); fx, fy = 2 + ddx, 11.5 - lf
        g = gl.rotate(90, expand=True); c.alpha_composite(g, (round((ha.BX + fx) * ha.S - g.width / 2), round((ha.BY + fy + oy) * ha.S - g.height / 2)))
    if lean: c = c.rotate(lean, resample=Image.NEAREST, center=((ha.BX + 8) * ha.S, (ha.BY + 16) * ha.S), fillcolor=ha.BG)
    c = c.convert("RGB"); c = c.resize((c.width * 55 // 100, c.height * 55 // 100), Image.NEAREST)
    ImageDraw.Draw(c).text((4, 2), f"{row['cat']}: {state}", font=font, fill=(240, 200, 90)); return c
states = ["rest", "walk", "run", "aimwalk"]; frames = []
for i in range(int(2.0 * FPS)):
    t = i / FPS; grid = [[panel(r, st, t) for st in states] for r in rows]
    pw, ph = grid[0][0].size; sheet = Image.new("RGB", (pw * 4, ph * len(rows)))
    for y, line in enumerate(grid):
        for x, p in enumerate(line): sheet.paste(p, (x * pw, y * ph))
    frames.append(sheet)
os.makedirs(sys.argv[1], exist_ok=True)
frames[0].save(os.path.join(sys.argv[1], "weapon_states.gif"), save_all=True, append_images=frames[1:], duration=1000 // FPS, loop=0)
frames[11].save(os.path.join(sys.argv[1], "weapon_states_sheet.png")); print("ok", frames[0].size)
