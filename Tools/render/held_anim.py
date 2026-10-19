"""RT 6f: the reload and jam-clearing animations as the game draws them. Positions come from the game's own pose code
(HeldPoseMath.Frame, dumped by the harness: `dotnet /tmp/test.dll heldframes frames.json`); this puts the real sprites
on them the way HeldWeaponView does (body 16 px a unit, guns 32 px a unit turned round their grip, 4x3 gloves).
Usage: python3 Tools/render/held_anim.py frames.json out_dir   ->  <key>_<style>.gif per animation + held_anim_sheet.png"""
import json, math, os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(ROOT, "Assets/Resources/Sprites")
S = 10                                   # screen pixels per character pixel
W, H = 34, 22                            # canvas, character pixels
BX, BY = 6, 4                            # where the 16x16 body sits on it
BG = (46, 42, 34, 255)
HINGES = {"Obrez66": (9, 4), "Sever34": (16, 4)}              # = WeaponArt.Hinges

def hexc(h): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, 255)

def glove(cuff, gl):
    img = Image.new("RGBA", (4, 3))
    col = {"K": hexc("#141617"), "c": hexc(cuff), "g": hexc(gl)}
    for y, row in enumerate([".KK.", "KcgK", ".KK."]):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img.resize((4 * S, 3 * S), Image.NEAREST)

def frame(anim, f, body, gun, gl, parts=None):
    gx_, gy_, deg, hx, hy, sx, sy, has_sup, left, mag_off, brk = f
    if parts:                                                  # RT 6g: the magazine under the gun, mag_off half-px out (-1: gone)
        gbody, gmag = parts; layer = Image.new("RGBA", (gun.width, gun.height + 24))
        if mag_off >= 0: layer.alpha_composite(gmag, (0, round(mag_off)))
        layer.alpha_composite(gbody or gun); gun = layer
    if anim.get("barrels"):                                    # RT 6i: barrels swung down on the hinge (clockwise in the gun's own picture)
        gbody, gbar = anim["barrels"]; layer = Image.new("RGBA", gun.size); layer.alpha_composite(gbody)
        layer.alpha_composite(gbar.rotate(-brk, center=HINGES[anim["key"]], resample=Image.NEAREST)); gun = layer
    c = Image.new("RGBA", (W * S, H * S), BG)
    c.alpha_composite(body, (BX * S, BY * S))
    # the gun: half-size pixels, pivot on the grip, flipped vertically when aiming left, turned by deg (counter-clockwise)
    g = gun.transpose(Image.FLIP_TOP_BOTTOM) if left else gun
    py = (g.height - anim["gy"]) if left else anim["gy"]       # flipping the (taller) layer moves the grip row with it
    g = g.resize((g.width * S // 2, g.height * S // 2), Image.NEAREST)
    pad = max(g.size) * 2
    big = Image.new("RGBA", (pad * 2, pad * 2)); big.alpha_composite(g, (pad - round(anim["gx"] * S / 2), pad - round(py * S / 2)))
    big = big.rotate(deg, center=(pad, pad), resample=Image.NEAREST)
    c.alpha_composite(big, (round((BX + gx_) * S) - pad, round((BY + gy_) * S) - pad))
    gg = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
    for (x, y, on) in ((sx, sy, has_sup), (hx, hy, 1)):       # support under the grip hand, as the sorting orders do
        if on: c.alpha_composite(gg, (round((BX + x) * S - 2 * S), round((BY + y) * S - 1.5 * S)))
    return c

def main(src, out):
    os.makedirs(out, exist_ok=True)
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA")
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 14)
    bold = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16)
    anims = json.load(open(src))
    rows = []
    for a in anims:
        i = a["armor"]; body = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST)
        gun = Image.open(os.path.join(SPR, "Weapons", f"weapon_{a['key']}.png")).convert("RGBA")
        gl = glove(a["cuff"], a["glove"])
        mp = os.path.join(SPR, "Weapons", f"weapon_{a['key']}_mag.png"); bp = mp.replace("_mag.png", "_body.png")
        rp = os.path.join(SPR, "Weapons", f"weapon_{a['key']}_barrels.png")
        if os.path.exists(rp): a["barrels"] = (Image.open(bp).convert("RGBA"), Image.open(rp).convert("RGBA"))
        parts = None if os.path.exists(rp) else (Image.open(bp).convert("RGBA") if os.path.exists(bp) else None, Image.open(mp).convert("RGBA")) if os.path.exists(mp) else None
        frames = [frame(a, f, body, gun, gl, parts) for f in a["frames"]]
        hold = [frames[-1]] * 12                                   # a beat at rest before it loops
        frames[0].save(os.path.join(out, f"{a['key']}_{a['style']}.gif"), save_all=True, append_images=frames[1:] + hold,
                       duration=33, loop=0, disposal=2)
        rows.append((a, frames))
    # contact sheet: 8 evenly spaced frames per animation
    n, cw, ch = 8, W * S * 3 // 4, H * S * 3 // 4
    sheet = Image.new("RGBA", (240 + n * (cw + 6), 40 + len(rows) * (ch + 24)), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    d.text((10, 10), "Reload and jam clearing - the game's pose code (HeldPoseMath.Frame) with the real sprites", font=bold, fill=(230, 230, 230))
    for r, (a, frames) in enumerate(rows):
        y = 40 + r * (ch + 24)
        what = "clear a jam" if a["style"] == "jam" else f"reload ({a['style']})"
        d.text((10, y + ch // 2 - 16), a["name"], font=bold, fill=(220, 222, 224))
        d.text((10, y + ch // 2 + 4), f"{what}, {a['secs']:.2f} s", font=font, fill=(190, 194, 198))
        last = len(a["frames"]) - 5
        for j in range(n):
            k = round(j * last / (n - 1)); t = k / 30
            sheet.alpha_composite(frames[k].resize((cw, ch), Image.NEAREST), (240 + j * (cw + 6), y))
            d.text((240 + j * (cw + 6) + 4, y + ch + 2), f"{t:.2f} s", font=font, fill=(160, 164, 168))
    sheet.convert("RGB").save(os.path.join(out, "held_anim_sheet.png"))
    print(f"{len(rows)} animations -> {out}")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
