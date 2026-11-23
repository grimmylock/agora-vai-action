"""2026-11-23: the melee weapons drawn from their INVENTORY pictures in the hands (`Weapons/weapon_<Key>_hd.png` shrunk by Dense[key] to the old on-screen size), with the game's own poses
(`dotnet /tmp/test.dll meleemoves mm.json`: the real Melee.PlayerInput and HeldPoseMath.Frame, grips from WeaponArt.Grips = virtual px). One row per weapon: stance, then the first swing wound up, striking and following through.
Usage: python3 Tools/render/melee_hd.py mm.json out_dir [old]  ('old' draws the previous mini sprites with their previous grips for the before row) -> melee_hd.png"""
import json, os, re, subprocess, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, S, BG, big_glove, glove, paste
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
src = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
def scale(k):
    m = re.search(r'\["%s"\] = ([0-9.]+)f' % k, src.split("HandScale = new")[1].split("};")[0]); return float(m.group(1)) if m else 1.0
def dense(k): m = re.search(r'\["%s"\] = ([0-9.]+)f' % k, src.split("Dense = new")[1].split("};")[0]); return float(m.group(1)) if m else 1.0
def main(mm, out, old=False):
    d = json.load(open(mm)); os.makedirs(out, exist_ok=True)
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); ai, cuff, glv = d["armor"]
    body = atlas.crop(((ai % 16) * 16, (ai // 16) * 16, (ai % 16) * 16 + 16, (ai // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST)
    fist = big_glove(cuff, glv).resize((5 * S, 4 * S), Image.NEAREST); other = glove(cuff, glv).resize((4 * S, 3 * S), Image.NEAREST)
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14); W = 16 * S * 3; ub = 16 * S
    rows = []
    for wpn in d["weapons"]:
        key = wpn["key"]; k = dense(key)
        hs = scale(key)
        if k > 1.0: hd = Image.open(os.path.join(SPR, f"Weapons/weapon_{key}_hd.png")).convert("RGBA"); img = hd.resize((max(1, round(hd.width / k * hs)), max(1, round(hd.height / k * hs))), Image.LANCZOS)   # to the in-hand scale x HandScale (the game: Bilinear at ppu x k / scale)
        else: mini = Image.open(os.path.join(SPR, f"Weapons/weapon_{key}.png")).convert("RGBA"); img = mini.resize((max(1, round(mini.width * hs)), max(1, round(mini.height * hs))), Image.NEAREST)   # the scythe: its mini picture, drawn bigger
        fr = wpn["frames"]; first = [i for i, f in enumerate(fr) if f[10]][0]
        picks = [5, first + 3, first + 6, first + 10]; cells = []
        for i in picks:
            gx, gy, deg, hx, hy, sup, sx, sy, lean, move, swinging, length, hdeg = fr[i][:13]
            c = Image.new("RGBA", (W, W + 24), BG); u = Image.new("RGBA", (W, W)); u.alpha_composite(body, (ub, ub))
            wi = img if length > 0.99 else img.resize((max(1, round(img.width * length)), img.height), Image.NEAREST)
            paste(u, wi, (wpn["grip"][0] * length * hs, wpn["grip"][1] * hs), (ub + gx * S, ub + gy * S), deg)
            fz = fist.rotate(hdeg, expand=True, resample=Image.NEAREST); u.alpha_composite(fz, (round(ub + hx * S - fz.width / 2), round(ub + hy * S - fz.height / 2)))
            if sup: u.alpha_composite(other, (round(ub + sx * S - 2 * S), round(ub + sy * S - 1.5 * S)))
            u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST); c.alpha_composite(u, (0, 24))
            ImageDraw.Draw(c).text((8, 4), f"{key} (k {k:.2f}, x{hs:.2f})" + ("  stance" if i == picks[0] else ""), font=font, fill=(240, 200, 90)); cells.append(c)
        rows.append(cells)
    sheet = Image.new("RGB", (W * 4, (W + 24) * len(rows)), BG[:3])
    for r, cells in enumerate(rows):
        for x, c in enumerate(cells): sheet.paste(c.convert("RGB"), (x * W, r * (W + 24)))
    sheet.save(os.path.join(out, "melee_hd.png")); print("ok", sheet.size)
if __name__ == "__main__": main(sys.argv[1], sys.argv[2])
