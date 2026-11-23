"""2026-11-23: an ANIMATED preview of the Visor's moves (Yan: "give me animated preview of how they look"): each move from the game's own pose code (`dotnet /tmp/test.dll visormoves f.json`: the real Boss.Begin/Tick, HeldPoseMath.Frame and Melee charge,
30 frames a second of game time) with his real sprites, the glint dot where the game fires it (white = parry, red = dive), before (left) and after (right) the rework. Usage: python3 Tools/render/visor_anim.py before.json after.json out.gif"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, S, BG, big_glove, glove, paste
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
hw = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
def tab(n, k):
    m = re.search(r'\["%s"\] = ([0-9.]+)f' % k, hw.split(n + " = new")[1].split("};")[0]); return float(m.group(1)) if m else 1.0
PKEYS = ["crouch", "crouch_h", "kneel", "kneel_h", "lean_h_l", "lean_h_r", "lean_l", "lean_r", "lunge_h_l", "lunge_h_r", "lunge_l", "lunge_r", "lying", "sit_h_l", "sit_h_r", "sit_l", "sit_r", "stagger_h_l", "stagger_h_r", "stagger_l", "stagger_r"]
body0 = Image.open(os.path.join(SPR, "armor_unique_black_visor_suit.png")).convert("RGBA").crop((0, 0, 16, 16)).resize((16 * S, 16 * S), Image.NEAREST)
poses = Image.open(os.path.join(SPR, "armor_unique_black_visor_suit_poses.png")).convert("RGBA")
def posed(k): return body0 if not k else poses.crop((PKEYS.index(k) * 16, 0, PKEYS.index(k) * 16 + 16, 16)).resize((16 * S, 16 * S), Image.NEAREST)
fist = big_glove("#1f2226", "#2a2c30").resize((5 * S, 4 * S), Image.NEAREST); other = glove("#1f2226", "#2a2c30").resize((4 * S, 3 * S), Image.NEAREST)
W = 16 * S * 3; ub = 16 * S; font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
def weapon(wpn):
    kd, hs = tab("Dense", wpn["key"]), tab("HandScale", wpn["key"])
    if kd > 1: im = Image.open(os.path.join(SPR, f"Weapons/weapon_{wpn['key']}_hd.png")).convert("RGBA"); img = im.resize((round(im.width / kd * hs), round(im.height / kd * hs)), Image.LANCZOS)
    else: img = Image.open(os.path.join(SPR, f"Weapons/weapon_{wpn['key']}.png")).convert("RGBA")
    return img, [wpn["grip"][0] * hs, wpn["grip"][1] * hs]
def draw(img, grip, f):
    gx, gy, deg, hx, hy, sup, sx, sy, lean, move, doing, length, hdeg = f[:13]; bkey = f[14] if len(f) > 14 else ""
    glint = f[13] if len(f) > 13 and not isinstance(f[13], str) else 0
    u = Image.new("RGBA", (W, W)); u.alpha_composite(posed(bkey), (ub, ub))
    wi = img if length > 0.99 else img.resize((max(1, round(img.width * length)), img.height), Image.NEAREST)
    paste(u, wi, (grip[0] * length, grip[1]), (ub + gx * S, ub + gy * S), deg)
    fz = fist.rotate(hdeg, expand=True, resample=Image.NEAREST); u.alpha_composite(fz, (round(ub + hx * S - fz.width / 2), round(ub + hy * S - fz.height / 2)))
    if sup: u.alpha_composite(other, (round(ub + sx * S - 2 * S), round(ub + sy * S - 1.5 * S)))
    u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST); c = Image.new("RGBA", (W, W), BG); c.alpha_composite(u); c = c.crop((50, 20, W - 50, W - 30))
    if glint:
        d = ImageDraw.Draw(c); r = 14; x, y = c.width // 2 + 60, 40; col = (255, 60, 40) if glint == 2 else (255, 255, 230); d.ellipse((x - r, y - r, x + r, y + r), fill=col)
    return c
b = json.load(open(sys.argv[1]))["weapons"][0]; a = json.load(open(sys.argv[2]))["weapons"][0]; ib, gb = weapon(b); ia, ga = weapon(a); frames = []
for mi, name in enumerate(a["moves"]):
    def span(w):
        idx = [i for i, f in enumerate(w["frames"]) if f[9] == mi and f[10]]
        return (idx[0] - 4, min(idx[-1], idx[0] + 80)) if idx else None
    sb, sa = span(b), span(a)
    if not sb or not sa: continue
    n = max(sb[1] - sb[0], sa[1] - sa[0]) + 8
    for k in range(n):
        fb = b["frames"][min(b["frames"].__len__() - 1, sb[0] + min(k, sb[1] - sb[0]))]; fa = a["frames"][min(a["frames"].__len__() - 1, sa[0] + min(k, sa[1] - sa[0]))]
        cb, ca = draw(ib, gb, fb), draw(ia, ga, fa); cw, ch = cb.size
        s = Image.new("RGB", (cw * 2 + 6, ch + 30), BG[:3]); dd = ImageDraw.Draw(s)
        dd.text((6, 5), f"{name}   BEFORE", font=font, fill=(200, 200, 200)); dd.text((cw + 12, 5), "AFTER", font=font, fill=(255, 220, 140))
        s.paste(cb.convert("RGB"), (0, 30)); s.paste(ca.convert("RGB"), (cw + 6, 30)); frames.append(s)
    for _ in range(8): frames.append(frames[-1])
frames[0].save(sys.argv[3], save_all=True, append_images=frames[1:], duration=1000 // 30, loop=0); print(len(frames), "frames", frames[0].size)
