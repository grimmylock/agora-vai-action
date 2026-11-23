"""2026-11-23: an ANIMATED sheet of every melee weapon in the hands: the real HeldPoseMath poses (`dotnet /tmp/test.dll meleemoves mm.json`), the inventory art at its in-hand scale, the weapon spring
(WeaponSpring numbers by weight) with the fist turning and the two-handed grip following it, the Verger's flip on the rising cut / backhand. Not Unity: same maths, a Python draw.
Usage: python3 Tools/render/melee_anim.py mm.json out.gif"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, S, BG, big_glove, glove, paste
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
src = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read(); db = open(os.path.join(ROOT, "Assets/Scripts/GameData/WeaponDatabase.cs")).read()
def tab(n, k):
    m = re.search(r'\["%s"\] = ([0-9.]+)f' % k, src.split(n + " = new")[1].split("};")[0]); return float(m.group(1)) if m else 1.0
def spring_for(key):
    if key == "VergerScythe": return (18.0, 0.32, 75.0)
    if key == "Sledgehammer": return (14.0, 0.36, 65.0)
    m = re.search(r'\["%s"\] = new WeaponDef \{[^}]*WeightKg = ([0-9.]+)f' % key, db)
    t = max(0.0, min(1.0, (float(m.group(1)) - 0.5) / 3.5)) if m else 0.0; return (26 + (15 - 26) * t, 0.46 + (0.37 - 0.46) * t, 32 + (68 - 32) * t)
class Spring:
    def __init__(s, p): s.p = p; s.pos = None; s.vel = 0.0
    def step(s, target, dt=1 / 30.0):
        w, z, mx = s.p
        if s.pos is None or abs(target - s.pos) > 300: s.pos, s.vel = target, 0.0; return target
        n = max(1, int(-(-dt // (1 / 120.0)))); h = dt / n
        for _ in range(n): s.vel += (w * w * (target - s.pos) - 2 * z * w * s.vel) * h; s.pos += s.vel * h
        s.pos = max(target - mx, min(target + mx, s.pos)); return s.pos
two = ("FireAxe", "Sledgehammer", "VergerScythe")
d = json.load(open(sys.argv[1])); atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); ai, cuff, glv = d["armor"]
body = atlas.crop(((ai % 16) * 16, (ai // 16) * 16, (ai % 16) * 16 + 16, (ai // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST)
fist = big_glove(cuff, glv).resize((5 * S, 4 * S), Image.NEAREST); other = glove(cuff, glv).resize((4 * S, 3 * S), Image.NEAREST)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15); W = 16 * S * 3; ub = 16 * S
ws = []
for wpn in d["weapons"]:
    key = wpn["key"]; k = tab("Dense", key); hs = tab("HandScale", key)
    if k > 1.0: hd = Image.open(os.path.join(SPR, f"Weapons/weapon_{key}_hd.png")).convert("RGBA"); img = hd.resize((max(1, round(hd.width / k * hs)), max(1, round(hd.height / k * hs))), Image.LANCZOS)
    else: mini = Image.open(os.path.join(SPR, f"Weapons/weapon_{key}.png")).convert("RGBA"); img = mini.resize((max(1, round(mini.width * hs)), max(1, round(mini.height * hs))), Image.NEAREST)
    ws.append((wpn, key, hs, img, Spring(spring_for(key))))
def panel(wpn, key, hs, img, sp, i, label):
    f = wpn["frames"][i]; gx, gy, deg, hx, hy, sup, sx, sy, lean, move, swinging, length, hdeg = f[:13]; flip = bool(f[15]) if len(f) > 15 else False
    d0 = deg; deg = sp.step(deg); hdeg += 0.6 * (deg - d0)
    if sup and key in two: r = __import__("math").radians(deg); sx = round(gx + 4 * __import__("math").cos(r)); sy = round(gy - 4 * __import__("math").sin(r) - 0.5) + 0.5
    c = Image.new("RGBA", (W, W + 26), BG); u = Image.new("RGBA", (W, W)); u.alpha_composite(body, (ub, ub))
    wi = img if length > 0.99 else img.resize((max(1, round(img.width * length)), img.height), Image.NEAREST)
    paste(u, wi, (wpn["grip"][0] * length * hs, wpn["grip"][1] * hs), (ub + gx * S, ub + gy * S), deg, flip)
    fz = fist.rotate(hdeg, expand=True, resample=Image.NEAREST); u.alpha_composite(fz, (round(ub + hx * S - fz.width / 2), round(ub + hy * S - fz.height / 2)))
    if sup: u.alpha_composite(other, (round(ub + sx * S - 2 * S), round(ub + sy * S - 1.5 * S)))
    u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST); c.alpha_composite(u, (0, 26))
    ImageDraw.Draw(c).text((8, 4), label, font=font, fill=(240, 200, 90)); return c
n = min(len(w[0]["frames"]) for w in ws); firsts = [[i for i, f in enumerate(w[0]["frames"]) if f[10]][0] for w in ws]
cols = 3; rows = (len(ws) + cols - 1) // cols; frames = []; lo, hi = 4, min(n, 64)
for t in range(lo, hi):
    sheet = Image.new("RGB", (W * cols, (W + 26) * rows), BG[:3])
    for j, (wpn, key, hs, img, sp) in enumerate(ws):
        i = max(0, min(len(wpn["frames"]) - 1, firsts[j] - 14 + (t - lo)))
        sheet.paste(panel(wpn, key, hs, img, sp, i, f"{key} (x{hs:.2f})").convert("RGB"), ((j % cols) * W, (j // cols) * (W + 26)))
    frames.append(sheet.resize((sheet.width * 2 // 3, sheet.height * 2 // 3), Image.NEAREST))
frames[0].save(sys.argv[2], save_all=True, append_images=frames[1:], duration=40, loop=0); print("ok", len(frames), frames[0].size)
