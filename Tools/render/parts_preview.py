"""FULL shot preview of a held gun (2026-11-15): the trigger pull and the muzzle flash, the gun's own kick and climb (HeldPoseMath.Frame via `dotnet /tmp/test.dll heldframes`, the "shot" frames), the moving part (pump fore-end, slide: WeaponArt.PartPhase)
sliding with the off-hand following a pump, and the spent case leaving the port (WeaponArt.Ejects, then HeldPoseMath.Fall). A Python port of the pose maths, not a Unity capture.
Usage: python3 Tools/render/parts_preview.py <heldframes.json> <Key> <out_prefix>   -> <out_prefix>.gif (+ a strip .png)"""
import json, math, os, re, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as H
import held_hires_preview as P
S = H.S; SPR = H.SPR
def phase(style, t):
    ss = lambda x: x * x * (3 - 2 * x)
    if t < 0: return 0.0
    if style == "hammer": return t / 0.06 if t < 0.06 else 1 - (t - 0.06) / 0.08 if t < 0.14 else 0.0
    if style == "lever": return 0.0 if t < 0.12 else ss((t - 0.12) / 0.14) if t < 0.26 else 1.0 if t < 0.31 else 1 - ss((t - 0.31) / 0.14) if t < 0.45 else 0.0
    if style == "bolt": return 0.0 if t < 0.36 else ss((t - 0.36) / 0.18) if t < 0.54 else 1.0 if t < 0.58 else 1 - ss((t - 0.58) / 0.14) if t < 0.72 else 0.0
    if style == "slide": return t / 0.03 if t < 0.03 else 1 - (t - 0.03) / 0.11 if t < 0.14 else 0.0
    if t < 0.14: return 0.0
    if t < 0.32: return ss((t - 0.14) / 0.18)
    if t < 0.38: return 1.0
    if t < 0.56: return 1 - ss((t - 0.38) / 0.18)
    return 0.0
def boltlift(t): return 0.0 if t < 0.30 else (t - 0.30) / 0.06 if t < 0.36 else 1.0 if t < 0.72 else 1 - (t - 0.72) / 0.06 if t < 0.78 else 0.0
def rot(dx, dy, deg):
    """a vector in the gun's picture (x along the barrel, y down) turned by the gun's angle (CCW), image y down"""
    d = math.radians(deg); return dx * math.cos(d) + dy * math.sin(d), -dx * math.sin(d) + dy * math.cos(d)
def main(src, key, out):
    cs = open(os.path.join(H.ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
    dense = float(re.search(r'\["%s"\] = ([0-9.]+)f,' % key, cs).group(1)); gr = [float(v.strip().rstrip("f")) for v in re.search(r'\["%s"\] = new\[\] \{([^}]*)\}' % key, cs).group(1).replace("float.NaN", "nan").split(",")]
    pm = re.search(r'\["%s"\] = \("(\w+)", ([0-9.]+)f\)' % key, cs); style, travel = (pm.group(1), float(pm.group(2))) if pm else (None, 0.0)
    hv = re.search(r'HammerPivot.*?\["%s"\] = \(([0-9.]+)f, ([0-9.]+)f\)' % key, cs, re.S); pivot = (float(hv.group(1)), float(hv.group(2))) if hv else None
    em = re.search(r'\["%s"\] = \(([0-9.]+)f, ([0-9.]+)f, ([0-9.]+)f, (true|false)\)' % key, cs); port = (float(em.group(1)), float(em.group(2)), float(em.group(3)), em.group(4) == "true") if em else None
    shell = key in ("RangerP8", "BayouPump", "Hound13", "SP14Warden", "HaldenLeverShotgun")
    allan = json.load(open(src)); shot = [x for x in allan if x["key"] == key and x["style"] == "shot"][0]; base = [x for x in allan if x["key"] == key and x["style"] not in ("jam", "shot")][0]
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); i = base["armor"]
    body = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST); gl = H.glove(base["cuff"], base["glove"])
    wp = os.path.join(SPR, "Weapons"); full = Image.open(f"{wp}/weapon_{key}_hd.png").convert("RGBA")
    if style: gb = Image.open(f"{wp}/weapon_{key}_body.png").convert("RGBA"); gp = Image.open(f"{wp}/weapon_{key}_part.png").convert("RGBA")
    grip = (gr[0] * dense, gr[1] * dense); frames = []; dt = 1 / 30.0
    # the case: starts at the port at its delay (the port's place in THAT frame), then falls (gravity 14 tiles/s^2, 16 char px a tile)
    case = None
    if port:
        k = min(len(shot["frames"]) - 1, int(round(port[2] / dt))); f = shot["frames"][k]; dx, dy = (port[0] - grip[0]) / dense, (port[1] - grip[1]) / dense
        if port[3] and style: dx -= phase(style, port[2]) * travel / dense
        ox, oy = rot(dx, dy, f[2]); x, y = f[0] + ox, f[1] + oy; a = math.radians(f[2]); vx = (-math.sin(a) * 0.7 - math.cos(a) * 0.25) * 16; vy = -(1.3 + math.cos(a) * 0.2) * 16; t = port[2]; track = []
        while t < 2.0:
            vy += 14 * 16 / 60.0; x += vx / 60.0; y += vy / 60.0; t += 1 / 60.0
            if y >= 14.8 and vy > 0:
                y = 14.8
                if vy > 1.2 * 16: vy = -vy * 0.3; vx *= 0.5
                else: track.append((t, x, y)); break
            track.append((t, x, y))
        case = track
    for n, f in enumerate(shot["frames"]):
        t = n * dt; gx_, gy_, deg, hx, hy, sx, sy, has_sup, left = f[:9]; sh = phase(style, t) * travel if style else 0.0
        c = Image.new("RGBA", (H.W * S, H.H * S), H.BG); c.alpha_composite(body, (H.BX * S, H.BY * S))
        if style:
            if style in ("hammer", "lever"): part = gp.rotate(sh, center=pivot, resample=Image.BICUBIC) if abs(sh) > 0.5 else gp
            elif style == "bolt":
                part = Image.new("RGBA", gp.size, (0, 0, 0, 0)); lift = int(round(boltlift(t) * 0.22 * travel)); part.alpha_composite(gp.crop((int(round(sh)), 0, gp.width, gp.height)) if sh >= 0.5 else gp, (0, -lift))
            else: part = Image.new("RGBA", gp.size, (0, 0, 0, 0)); part.alpha_composite(gp.crop((int(round(sh)), 0, gp.width, gp.height)) if sh >= 0.5 else gp, (0, 0))
            P.paste2(c, gb, grip[0], grip[1], gx_, gy_, deg, left, dense); P.paste2(c, part, grip[0], grip[1], gx_, gy_, deg, left, dense)
        else: P.paste2(c, full, grip[0], grip[1], gx_, gy_, deg, left, dense)
        if style == "pump":                                                                               # the off-hand goes back with the fore-end
            o = sh / dense; sx, sy = sx - o * math.cos(math.radians(deg)), sy + o * math.sin(math.radians(deg))
        gg = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
        for (x, y, on) in ((sx, sy, has_sup), (hx, hy, 1)):
            if on: c.alpha_composite(gg, (round((H.BX + x) * S - 2 * S), round((H.BY + y) * S - 1.5 * S)))
        d = ImageDraw.Draw(c)
        if t < 0.09:                                                                                      # the muzzle flash, at the picture's muzzle point
            mx, my = rot(gr[4] - gr[0], gr[5] - gr[1], deg); X, Y = (H.BX + gx_ + mx) * S, (H.BY + gy_ + my) * S; r = (4 if t < 0.05 else 2.5) * S
            col = (255, 240, 170, 255) if t < 0.05 else (255, 160, 60, 255)
            d.polygon([(X - r, Y), (X, Y - r * 0.5), (X + r * 1.6, Y), (X, Y + r * 0.5)], fill=col); d.rectangle((X - S, Y - S, X + S, Y + S), fill=(255, 255, 255, 255))
        if case:
            pos = None
            for tt, x, y in case:
                if tt <= t: pos = (x, y)
            if pos and t >= port[2]:
                X, Y = (H.BX + pos[0]) * S, (H.BY + pos[1]) * S; w, h = (7, 3) if shell else (5, 2)
                d.rectangle((X, Y, X + w * S / 2.5, Y + h * S / 2.5), fill=(190, 40, 40) if shell else (222, 184, 74))
        frames.append(c)
    # crop to the gun's area and double it
    cw, chh = frames[0].size; box = (int(cw * 0.2), int(chh * 0.05), cw, int(chh * 0.85)); big = [f.crop(box).resize(((box[2] - box[0]) * 2 // 2, (box[3] - box[1]) * 2 // 2), Image.NEAREST).convert("RGB") for f in frames]
    big[0].save(out + ".gif", save_all=True, append_images=big[1:], duration=[33] * len(big), loop=0)
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14); sel = [0, 2, 5, 8, 12, 16, 21, 28]; sel = [j for j in sel if j < len(big)]
    tw, th = big[0].size; strip = Image.new("RGB", (len(sel) * (tw // 2 + 4), th // 2 + 22), (27, 29, 31)); dr = ImageDraw.Draw(strip)
    for q, j in enumerate(sel): strip.paste(big[j].resize((tw // 2, th // 2), Image.NEAREST), (q * (tw // 2 + 4), 0)); dr.text((q * (tw // 2 + 4) + 4, th // 2 + 3), f"{j * dt:.2f} s", font=font, fill=(220, 220, 220))
    strip.save(out + ".png"); print("saved", out, len(big), "frames")
if __name__ == "__main__": main(*sys.argv[1:4])
