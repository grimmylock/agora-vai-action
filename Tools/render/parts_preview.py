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
import math, random
def casing_track(cfs, key, dense, grip, f, shell, t_shot=0.0):
    """the spent case's path in the preview (the game's HeldPoseMath.Fall: gravity 14 tiles/s^2, 16 char px a tile), from the ejection port (HeldWeapon.cs WeaponArt.Ejects) at its delay: [(t, x, y)] in char px"""
    m = re.search(r'\["%s"\] = \(([0-9.]+)f, ([0-9.]+)f, ([0-9.]+)f\)' % key, cfs)
    if not m: return []
    tx, ty, delay = map(float, m.groups()); gx_, gy_, deg = f[0], f[1], f[2]
    dx, dy = (tx - grip[0]) / dense, (ty - grip[1]) / dense; d = math.radians(deg)
    px, py = gx_ + dx * math.cos(d) + dy * math.sin(d), gy_ - dx * math.sin(d) + dy * math.cos(d)
    a = d; k = 1.0; vx = (-math.sin(a) * 0.7 - math.cos(a) * 0.25) * k * 16; vy_up = (1.3 + math.cos(a) * 0.2) * 16; floor = 14.8
    track = []; t = delay; y = py; x = px; vy = -vy_up                                                  # image y runs down: up is negative
    while t < 1.2:
        vy += 14 * 16 * (1 / 60.0); x += vx / 60.0; y += vy / 60.0; t += 1 / 60.0
        if y >= floor and vy > 0:
            y = floor
            if vy > 1.2 * 16: vy = -vy * 0.3; vx *= 0.5
            else: track.append((t, x, y)); break
        track.append((t, x, y))
    return track
def glove_xy(sx, sy, deg, shift, dense, style):
    """the off-hand of a pump goes back with the fore-end (the same offset the game adds to _support)"""
    if style != "pump": return sx, sy
    d = math.radians(deg); o = shift / dense; return sx - o * math.cos(d), sy + o * math.sin(d)
def draw_case(c, track, t, shell):
    pos = None
    for tt, x, y in track:
        if tt <= t: pos = (x, y)
    if pos is None: return
    d = ImageDraw.Draw(c); X, Y = (H.BX + pos[0]) * S, (H.BY + pos[1]) * S; d.rectangle((X, Y, X + (2 if shell else 1.5) * S, Y + S), fill=(176, 40, 40) if shell else (201, 166, 61))
def main(src, key, out):
    cs = open(os.path.join(H.ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
    dense = float(re.search(r'\["%s"\] = ([0-9.]+)f,' % key, cs).group(1)); style, travel = re.search(r'\["%s"\] = \("(\w+)", ([0-9.]+)f\)' % key, cs).groups(); travel = float(travel)
    a = [x for x in json.load(open(src)) if x["key"] == key and x["style"] != "jam"][0]; f = a["frames"][-1]
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); i = a["armor"]
    body = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST); gl = H.glove(a["cuff"], a["glove"])
    gb = Image.open(os.path.join(SPR, "Weapons", f"weapon_{key}_body.png")).convert("RGBA"); gp = Image.open(os.path.join(SPR, "Weapons", f"weapon_{key}_part.png")).convert("RGBA")
    grip = (a["gx"] * dense, a["gy"] * dense); gx_, gy_, deg, hx, hy, sx, sy, has_sup, left = f[:9]
    frames = []; ts = [0.0, 0.10, 0.20, 0.26, 0.32, 0.38, 0.46, 0.56] if style == 'pump' else [0.0, 0.03, 0.06, 0.09, 0.12, 0.2, 0.3, 0.45]; shell = key == 'RangerP8'; cas = casing_track(cs, key, dense, grip, f, shell)
    for t in ts:
        sh = round(phase(style, t) * travel); part = Image.new("RGBA", gp.size, (0, 0, 0, 0)); part.alpha_composite(gp.crop((sh, 0, gp.width, gp.height)) if sh else gp, (0, 0))
        c = Image.new("RGBA", (H.W * S, H.H * S), H.BG); c.alpha_composite(body, (H.BX * S, H.BY * S)); P.paste2(c, gb, grip[0], grip[1], gx_, gy_, deg, left, dense); P.paste2(c, part, grip[0], grip[1], gx_, gy_, deg, left, dense)
        gg = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
        sx2, sy2 = glove_xy(sx, sy, deg, sh, dense, style)
        for (x, y, on) in ((sx2, sy2, has_sup), (hx, hy, 1)):
            if on: c.alpha_composite(gg, (round((H.BX + x) * S - 2 * S), round((H.BY + y) * S - 1.5 * S)))
        draw_case(c, cas, ts[len(frames)], shell)
        frames.append(c)
    cw, chh = frames[0].size; font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14)
    sel = [0, 2, 4, 6] if style == 'pump' else [0, 1, 2, 6]; frames = [frames[j].crop((int(cw * 0.2), int(chh * 0.12), cw, int(chh * 0.8))) for j in sel]; ts = [ts[j] for j in sel]; cw, chh = frames[0].size; cw, chh = cw * 2, chh * 2
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
        draw_case(c, cas, t / 100.0, shell)
        gif.append(c.convert("RGB"))
    gif[0].save(out + ".gif", save_all=True, append_images=gif[1:], duration=40, loop=0); print("saved", out)
if __name__ == "__main__": main(*sys.argv[1:4])
