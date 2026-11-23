"""2026-11-23: the Visor's moves, before and after the pose rework (Yan: clearer, more expressive poses): for each move the WIND-UP (the frame the glint fires on) and the STRIKE (mid-blow), from the game's own pose code (`dotnet /tmp/test.dll visormoves f.json`
before and after) with his real sprites. Usage: python3 Tools/render/visor_compare.py before.json after.json out.png"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, S, BG, big_glove, glove, paste
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
hw = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
def tab(n, k):
    m = re.search(r'\["%s"\] = ([0-9.]+)f' % k, hw.split(n + " = new")[1].split("};")[0]); return float(m.group(1)) if m else 1.0
PKEYS = ["crouch", "crouch_h", "kneel", "kneel_h", "lean_h_l", "lean_h_r", "lean_l", "lean_r", "lunge_h_l", "lunge_h_r", "lunge_l", "lunge_r", "lying", "sit_h_l", "sit_h_r", "sit_l", "sit_r", "stagger_h_l", "stagger_h_r", "stagger_l", "stagger_r"]
def cells(d, wpn):
    cuff, gl = "#1f2226", "#2a2c30"
    body = Image.open(os.path.join(SPR, "armor_unique_black_visor_suit.png")).convert("RGBA").crop((0, 0, 16, 16)).resize((16 * S, 16 * S), Image.NEAREST)
    poses = Image.open(os.path.join(SPR, "armor_unique_black_visor_suit_poses.png")).convert("RGBA")
    def posed(k): return body if not k else poses.crop((PKEYS.index(k) * 16, 0, PKEYS.index(k) * 16 + 16, 16)).resize((16 * S, 16 * S), Image.NEAREST)
    kd, hs = tab("Dense", wpn["key"]), tab("HandScale", wpn["key"])
    if kd > 1: im = Image.open(os.path.join(SPR, f"Weapons/weapon_{wpn['key']}_hd.png")).convert("RGBA"); img = im.resize((round(im.width / kd * hs), round(im.height / kd * hs)), Image.LANCZOS)
    else: img = Image.open(os.path.join(SPR, f"Weapons/weapon_{wpn['key']}.png")).convert("RGBA")
    grip = [wpn["grip"][0] * hs, wpn["grip"][1] * hs]
    fist = big_glove(cuff, gl).resize((5 * S, 4 * S), Image.NEAREST); other = glove(cuff, gl).resize((4 * S, 3 * S), Image.NEAREST)
    W = 16 * S * 3; ub = 16 * S; out = []
    for mi, name in enumerate(wpn["moves"]):
        idx = [i for i, f in enumerate(wpn["frames"]) if f[9] == mi and f[10]]
        if not idx: out.append(None); continue
        gl_i = next((i for i in idx if len(wpn["frames"][i]) > 13 and not isinstance(wpn["frames"][i][13], str) and wpn["frames"][i][13]), idx[0] + 12)       # the frame the glint fires on: the wind-up held, the blow about to start
        pair = []
        for i in (gl_i, min(idx[-1], gl_i + 10)):
            f = wpn["frames"][i]; gx, gy, deg, hx, hy, sup, sx, sy, lean, move, swinging, length, hdeg = f[:13]; bkey = f[14] if len(f) > 14 else ""
            u = Image.new("RGBA", (W, W)); u.alpha_composite(posed(bkey), (ub, ub))
            wi = img if length > 0.99 else img.resize((max(1, round(img.width * length)), img.height), Image.NEAREST)
            paste(u, wi, (grip[0] * length, grip[1]), (ub + gx * S, ub + gy * S), deg)
            fz = fist.rotate(hdeg, expand=True, resample=Image.NEAREST); u.alpha_composite(fz, (round(ub + hx * S - fz.width / 2), round(ub + hy * S - fz.height / 2)))
            if sup: u.alpha_composite(other, (round(ub + sx * S - 2 * S), round(ub + sy * S - 1.5 * S)))
            u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST); c = Image.new("RGBA", (W, W), BG); c.alpha_composite(u); pair.append(c.crop((60, 30, W - 60, W - 40)))
        out.append((name, pair))
    return out
b = json.load(open(sys.argv[1])); a = json.load(open(sys.argv[2]))
cb, ca = cells(b, b["weapons"][0]), cells(a, a["weapons"][0]); font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16)
rows = [(x, y) for x, y in zip(cb, ca) if x and y]; cw, ch = rows[0][0][1][0].size
sheet = Image.new("RGB", (cw * 4 + 150, (ch + 26) * len(rows)), BG[:3]); d = ImageDraw.Draw(sheet)
for r, ((n, pb), (_, pa)) in enumerate(rows):
    y = r * (ch + 26); d.text((8, y + ch // 2), n, font=font, fill=(240, 200, 90))
    for k, im in enumerate(pb + pa): sheet.paste(im.convert("RGB"), (150 + k * cw, y + 22))
    d.text((150, y + 2), "BEFORE: wind-up (glint)", font=font, fill=(200, 200, 200)); d.text((150 + cw, y + 2), "BEFORE: strike", font=font, fill=(200, 200, 200)); d.text((150 + 2 * cw, y + 2), "AFTER: wind-up (glint)", font=font, fill=(255, 220, 140)); d.text((150 + 3 * cw, y + 2), "AFTER: strike", font=font, fill=(255, 220, 140))
sheet.save(sys.argv[3]); print(sheet.size)
