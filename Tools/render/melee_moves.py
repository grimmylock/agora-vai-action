"""Melee 2: each kind of melee weapon in its stance and its string of swings (`dotnet /tmp/test.dll meleemoves moves.json`: the real
Melee.PlayerInput clicks and HeldPoseMath.Frame), drawn with the game's sprites the way HeldWeaponView draws them: the body (leaning
with the swing), the weapon turned round its grip, the weapon-hand fist and the other hand (on the handle of a two-handed weapon,
up in a guard otherwise).
Usage: python3 Tools/render/melee_moves.py moves.json out_dir  -> melee_moves.gif, melee_moves_sheet.png"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, S, BG, big_glove, glove, paste

NAMES = {"SteppeMachete": ("Machete", ["vertical chop", "horizontal sweep"]), "HuntingKnife": ("Knife", ["stab", "slash down", "slash up"]), "TacticalHatchet": ("Hatchet", ["vertical chop", "horizontal sweep"]),
         "FireAxe": ("Two-handed (fire axe)", ["overhead slam", "wide sweep", "rising swing"])}

def main(src, out):
    d = json.load(open(src)); os.makedirs(out, exist_ok=True)
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA")
    ai, acuff, aglove = d["armor"]
    body = atlas.crop(((ai % 16) * 16, (ai // 16) * 16, (ai % 16) * 16 + 16, (ai // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST)
    fist = big_glove(acuff, aglove).resize((5 * S, 4 * S), Image.NEAREST)
    other = glove(acuff, aglove).resize((4 * S, 3 * S), Image.NEAREST)
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
    W = 16 * S * 3
    cols = []
    for wpn in d["weapons"]:
        img = Image.open(os.path.join(SPR, f"Weapons/weapon_{wpn['key']}.png")).convert("RGBA")
        title, moves = NAMES[wpn["key"]]
        fr = []
        for gx, gy, deg, hx, hy, sup, sx, sy, lean, move, swinging, length, hdeg in wpn["frames"]:
            c = Image.new("RGBA", (W, W + 40), BG); ub = 16 * S
            u = Image.new("RGBA", (W, W)); u.alpha_composite(body, (ub, ub))
            wi = img if length > 0.99 else img.resize((max(1, round(img.width * length)), img.height), Image.NEAREST)   # a sweep foreshortens
            paste(u, wi, (wpn["grip"][0] * length, wpn["grip"][1]), (ub + gx * S, ub + gy * S), deg)
            fz = fist.rotate(hdeg, expand=True, resample=Image.NEAREST)            # the fist turns with the swing
            u.alpha_composite(fz, (round(ub + hx * S - fz.width / 2), round(ub + hy * S - fz.height / 2)))
            if sup: u.alpha_composite(other, (round(ub + sx * S - 2 * S), round(ub + sy * S - 1.5 * S)))
            u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST)
            c.alpha_composite(u, (0, 40))
            dr = ImageDraw.Draw(c)
            dr.text((8, 4), title, font=font, fill=(235, 235, 235))
            dr.text((8, 22), moves[move % len(moves)] if swinging else "stance", font=font, fill=(240, 200, 90) if swinging else (180, 180, 180))
            fr.append(c)
        cols.append(fr)
    n = max(len(f) for f in cols)
    frames = []
    for i in range(n):
        c = Image.new("RGBA", (W * len(cols), W + 40), (27, 29, 31, 255))
        for k, fr in enumerate(cols): c.alpha_composite(fr[min(i, len(fr) - 1)], (k * W, 0))
        frames.append(c.convert("RGB"))
    frames[0].save(os.path.join(out, "melee_moves.gif"), save_all=True, append_images=frames[1:], duration=33, loop=0)
    # the sheet: per weapon, the stance and each move's wound-up and end-of-strike frames
    rows = []
    for wpn, fr in zip(d["weapons"], cols):
        picks, last = [fr[5]], None
        for i, f in enumerate(wpn["frames"]):
            if f[10] and f[9] != last: last = f[9]; picks += [fr[i + 3], fr[i + 6], fr[min(i + 9, len(fr) - 1)]]   # wound up, strike, follow-through
        rows.append(picks)
    m = max(len(r) for r in rows)
    sheet = Image.new("RGB", (m * W, len(rows) * (W + 40)), (27, 29, 31))
    for r, row in enumerate(rows):
        for k, f in enumerate(row): sheet.paste(f.convert("RGB"), (k * W, r * (W + 40)))
    sheet.save(os.path.join(out, "melee_moves_sheet.png")); print("melee moves ->", out)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
