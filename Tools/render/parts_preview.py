"""Preview of a gun's moving part (2026-11-15): the character aiming with the gun's body, the part slid by the game's own phase curve (WeaponArt.PartPhase, HeldWeapon.cs), the gloves on top.
Usage: python3 Tools/render/parts_preview.py <heldframes.json> <Key> <out_prefix>   -> <out_prefix>.png (a strip of moments after the shot) and <out_prefix>.gif"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as H
import held_hires_preview as P
S = H.S; SPR = H.SPR
def phase(style, t):
    ss = lambda x: x * x * (3 - 2 * x)
    if t < 0: return 0.0
    if style == "slide": return t / 0.03 if t < 0.03 else 1 - (t - 0.03) / 0.11 if t < 0.14 else 0.0
    if t < 0.14: return 0.0
    if t < 0.32: return ss((t - 0.14) / 0.18)
    if t < 0.38: return 1.0
    if t < 0.56: return 1 - ss((t - 0.38) / 0.18)
    return 0.0
import math
def glove_xy(sx, sy, deg, shift, dense, style):
    """the off-hand of a pump goes back with the fore-end (the same offset the game adds to _support)"""
    if style != "pump": return sx, sy
    d = math.radians(deg); o = shift / dense; return sx - o * math.cos(d), sy + o * math.sin(d)
def main(src, key, out):
    cs = open(os.path.join(H.ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
    dense = float(re.search(r'\["%s"\] = ([0-9.]+)f,' % key, cs).group(1)); style, travel = re.search(r'\["%s"\] = \("(\w+)", ([0-9.]+)f\)' % key, cs).groups(); travel = float(travel)
    a = [x for x in json.load(open(src)) if x["key"] == key and x["style"] != "jam"][0]; f = a["frames"][-1]
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); i = a["armor"]
    body = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST); gl = H.glove(a["cuff"], a["glove"])
    gb = Image.open(os.path.join(SPR, "Weapons", f"weapon_{key}_body.png")).convert("RGBA"); gp = Image.open(os.path.join(SPR, "Weapons", f"weapon_{key}_part.png")).convert("RGBA")
    grip = (a["gx"] * dense, a["gy"] * dense); gx_, gy_, deg, hx, hy, sx, sy, has_sup, left = f[:9]
    frames = []; ts = [0.0, 0.10, 0.20, 0.26, 0.32, 0.38, 0.46, 0.56]
    for t in ts:
        sh = round(phase(style, t) * travel); part = Image.new("RGBA", gp.size, (0, 0, 0, 0)); part.alpha_composite(gp.crop((sh, 0, gp.width, gp.height)) if sh else gp, (0, 0))
        c = Image.new("RGBA", (H.W * S, H.H * S), H.BG); c.alpha_composite(body, (H.BX * S, H.BY * S)); P.paste2(c, gb, grip[0], grip[1], gx_, gy_, deg, left, dense); P.paste2(c, part, grip[0], grip[1], gx_, gy_, deg, left, dense)
        gg = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
        sx2, sy2 = glove_xy(sx, sy, deg, sh, dense, style)
        for (x, y, on) in ((sx2, sy2, has_sup), (hx, hy, 1)):
            if on: c.alpha_composite(gg, (round((H.BX + x) * S - 2 * S), round((H.BY + y) * S - 1.5 * S)))
        frames.append(c)
    cw, chh = frames[0].size; font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14)
    sel = [0, 2, 4, 6]; frames = [frames[j].crop((int(cw * 0.2), int(chh * 0.12), cw, int(chh * 0.8))) for j in sel]; ts = [ts[j] for j in sel]; cw, chh = frames[0].size; cw, chh = cw * 2, chh * 2
    strip = Image.new("RGBA", (len(frames) * (cw // 2 + 4), chh // 2 + 24), (27, 29, 31, 255)); d = ImageDraw.Draw(strip)
    for j, (fr, t) in enumerate(zip(frames, ts)): strip.alpha_composite(fr.resize((cw // 2, chh // 2), Image.NEAREST), (j * (cw // 2 + 4), 0)); d.text((j * (cw // 2 + 4) + 4, chh // 2 + 3), f"{t:.2f} s", font=font, fill=(220, 220, 220))
    strip.convert("RGB").save(out + ".png")
    fine = [phase(style, t / 100.0) for t in range(0, 70)]; gif = []
    for t in range(0, 70, 2):
        sh = round(phase(style, t / 100.0) * travel); part = Image.new("RGBA", gp.size, (0, 0, 0, 0)); part.alpha_composite(gp.crop((sh, 0, gp.width, gp.height)) if sh else gp, (0, 0))
        c = Image.new("RGBA", (H.W * S, H.H * S), H.BG); c.alpha_composite(body, (H.BX * S, H.BY * S)); P.paste2(c, gb, grip[0], grip[1], gx_, gy_, deg, left, dense); P.paste2(c, part, grip[0], grip[1], gx_, gy_, deg, left, dense)
        gg = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
        sx2, sy2 = glove_xy(sx, sy, deg, sh, dense, style)
        for (x, y, on) in ((sx2, sy2, has_sup), (hx, hy, 1)):
            if on: c.alpha_composite(gg, (round((H.BX + x) * S - 2 * S), round((H.BY + y) * S - 1.5 * S)))
        gif.append(c.convert("RGB"))
    gif[0].save(out + ".gif", save_all=True, append_images=gif[1:], duration=40, loop=0); print("saved", out)
if __name__ == "__main__": main(*sys.argv[1:4])
