"""RT 7a: the melee scenes from the harness (`dotnet /tmp/test.dll meleeframes melee.json`: the real Melee.Swing / Throw / Tick and
HeldPoseMath.Frame), drawn with the game's sprites and blood effects the way the view draws them: the characters, your hand and the
hatchet (turned round its grip, as HeldWeaponView), the Raider flashing red and knocked back a pixel when hit (CombatFxView.Hurt),
the blood puff (fx_hit_blood) and the splatter left on the ground (fx_blood_splat), the hatchet spinning, stuck, wrenched, dropped.
Usage: python3 Tools/render/melee_scene.py melee.json out_dir  -> melee_swing.gif, melee_throw.gif, melee_sheet.png"""
import json, math, os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(ROOT, "Assets/Resources/Sprites")
S = 6                                     # screen px per character px
TILE = 16                                 # character px per tile
BG = (58, 54, 42, 255)

def hexc(h): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, 255)
def big_glove(cuff, gl):
    """The 5x4 glove (HeldWeaponView.GloveBig): a fist round a melee handle (RT 7c)."""
    g = hexc(gl); lit = tuple(min(255, int(v * 1.45)) for v in g[:3]) + (255,); dark = tuple(int(v * 0.65) for v in g[:3]) + (255,)
    img = Image.new("RGBA", (5, 4)); col = {"K": hexc("#141617"), "c": hexc(cuff), "l": lit, "g": g, "d": dark}
    for y, row in enumerate([".KKK.", "KcllK", "KcgdK", ".KKK."]):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img

def glove(cuff, gl):
    img = Image.new("RGBA", (4, 3)); col = {"K": hexc("#141617"), "c": hexc(cuff), "g": hexc(gl)}
    for y, row in enumerate([".KK.", "KcgK", ".KK."]):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img

def paste(c, img, pivot, at, deg, flip=False, ppu_half=True):
    """img (weapon pixels, half a character px each) turned `deg` CCW round `pivot`, landing on canvas px `at`."""
    g = img.transpose(Image.FLIP_TOP_BOTTOM) if flip else img
    px, py = pivot
    if flip: py = g.height - py
    k = S / 2 if ppu_half else S
    g = g.resize((max(1, round(g.width * k)), max(1, round(g.height * k))), Image.NEAREST)
    pad = max(g.size) * 2
    big = Image.new("RGBA", (pad * 2, pad * 2)); big.alpha_composite(g, (pad - round(px * k), pad - round(py * k)))
    big = big.rotate(deg, center=(pad, pad), resample=Image.NEAREST)
    c.alpha_composite(big, (round(at[0]) - pad, round(at[1]) - pad))

def main(src, out):
    d = json.load(open(src)); os.makedirs(out, exist_ok=True)
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA")
    def body(i): return atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16))
    ai, acuff, aglove, ri, rcuff, rglove = d["armor"]
    you_b, foe_b = body(ai), body(ri)
    hatchet = Image.open(os.path.join(SPR, "Weapons/weapon_TacticalHatchet.png")).convert("RGBA")
    pistol = Image.open(os.path.join(SPR, "Weapons/weapon_PSh18Pchela.png")).convert("RGBA")
    splat = Image.open(os.path.join(SPR, "Fx/fx_blood_splat.png")).convert("RGBA")
    puff = Image.open(os.path.join(SPR, "Fx/fx_hit_blood.png")).convert("RGBA")
    def tint(im, col):
        im = im.copy(); px = im.load()
        for y in range(im.height):
            for x in range(im.width):
                r, g, b, a = px[x, y]
                if a: px[x, y] = (r * col[0] // 255, g * col[1] // 255, b * col[2] // 255, a)
        return im
    splats = [tint(splat.crop((i * 10, 0, i * 10 + 10, 8)), (140, 20, 26)) for i in range(4)]
    puffs = [tint(puff.crop((i * 5, 0, i * 5 + 5, 5)), (180, 33, 41)) for i in range(3)]
    gy, gp = glove(acuff, aglove), glove(rcuff, rglove)
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
    sheet_rows = []
    for clip in ("swing", "throw"):
        c0 = d[clip]; yx, yy = c0["you"]
        ox, oy = yx - 1.5, yy - 1.6                                   # scene origin, tiles
        Wt, Ht = 8.5, 2.6
        frames = []
        for i, f in enumerate(c0["frames"]):
            c = Image.new("RGBA", (round(Wt * TILE * S), round(Ht * TILE * S)), BG)
            def P(x, y): return ((x - ox) * TILE * S, (y - oy) * TILE * S)
            # blood already on the ground
            for k, (bt, bx, by) in enumerate(c0["blood"]):
                if i / 30 >= bt + 0.1:
                    sp = splats[k % 4].resize((10 * S, 8 * S), Image.NEAREST); x, y = P(bx + 0.15, by + 0.35); c.alpha_composite(sp, (round(x), round(y - 4 * S)))
            item = f["item"]
            if item and item[0] == "floor": paste(c, hatchet, (16, 8), P(item[1], item[2]), item[3])
            # you: body, the hatchet held upright in your fist, the fist on top; the whole unit leans with the swing (RT 7c)
            gx, gyy, gdeg, hx, hy, inhand, lean, left = f["you"]
            u = Image.new("RGBA", (16 * S * 3, 16 * S * 3)); ub = 16 * S        # the unit drawn on its own, then turned by the lean
            u.alpha_composite(you_b.resize((16 * S, 16 * S), Image.NEAREST), (ub, ub))
            if inhand: paste(u, hatchet, tuple(d["grip"]), (ub + gx * S, ub + gyy * S), gdeg, flip=bool(left))
            gl = big_glove(acuff, aglove)
            if left: gl = gl.transpose(Image.FLIP_LEFT_RIGHT)
            gl = gl.resize((5 * S, 4 * S), Image.NEAREST)
            u.alpha_composite(gl, (round(ub + hx * S - 2.5 * S), round(ub + hy * S - 2 * S)))
            u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST)
            bx, by = P(yx, yy); c.alpha_composite(u, (round(bx - 8 * S - ub), round(by - 8 * S - ub)))
            # the Raider, aiming his pistol at you; red and knocked back a pixel for 0.15 s after a hit
            fx_, fy_, hp, since, busy = f["foe"]
            if hp > 0:
                hurt = 0 <= since < 0.15
                fb = foe_b.transpose(Image.FLIP_LEFT_RIGHT)
                if hurt: fb = tint(fb, (255, 130, 115))
                rx, ry = P(fx_, fy_); rx -= 8 * S - (S if hurt else 0); ry -= 8 * S
                c.alpha_composite(fb.resize((16 * S, 16 * S), Image.NEAREST), (round(rx), round(ry)))
                if not busy: paste(c, pistol, tuple(d["pistol"]), (rx + 3 * S, ry + 10.5 * S), 180, flip=True)
                pg = gp.transpose(Image.FLIP_LEFT_RIGHT).resize((4 * S, 3 * S), Image.NEAREST)
                c.alpha_composite(pg, (round(rx + (1 if not busy else 5) * S), round(ry + 9.5 * S)))
                bar = ImageDraw.Draw(c); bw = 14 * S
                bar.rectangle([rx + S, ry - 2 * S, rx + S + bw, ry - S], fill=(30, 30, 30)); bar.rectangle([rx + S, ry - 2 * S, rx + S + bw * hp / c0["hp"], ry - S], fill=(190, 50, 45))
            else:                                                       # down: lying on the floor
                rx, ry = P(fx_, fy_)
                lb = foe_b.rotate(90, expand=True).resize((16 * S, 16 * S), Image.NEAREST)
                c.alpha_composite(lb, (round(rx - 8 * S), round(ry - 4 * S)))
            if item and item[0] in ("air", "stuck"): paste(c, hatchet, (16, 8), P(item[1], item[2]), item[3])
            # the blood puff, 0.2 s
            for bt, bx2, by2 in c0["blood"]:
                if 0 <= i / 30 - bt < 0.2:
                    k = min(2, int((i / 30 - bt) / 0.2 * 3)); x, y = P(bx2, by2 - 0.25)
                    c.alpha_composite(puffs[k].resize((5 * S, 5 * S), Image.NEAREST), (round(x - 2.5 * S), round(y - 2.5 * S)))
            dr = ImageDraw.Draw(c)
            label = {"swing": "swing: body, then head", "throw": "aim + click: throw"}[clip]
            if clip == "throw" and busy: label = "he wrenches it out (2.5 s)"
            if clip == "throw" and item and item[0] == "floor": label = "dropped: walk over, E to pick it up"
            dr.text((10, 8), f"{label}   {i / 30:4.2f} s", font=font, fill=(235, 235, 235))
            frames.append(c)
        frames[0].save(os.path.join(out, f"melee_{clip}.gif"), save_all=True, append_images=frames[1:], duration=33, loop=0, disposal=2)
        picks = [frames[k] for k in ([0, 10, 13, 30, 34, 37, 45, 59] if clip == "swing" else [8, 11, 15, 19, 100, 150, 175, 205])]
        sheet_rows.append(picks)
    w, h = sheet_rows[0][0].size; sw, sh = w // 2, h // 2
    sheet = Image.new("RGBA", (4 * sw + 30, 4 * sh + 50), (27, 29, 31, 255))
    for r, row in enumerate(sheet_rows):
        for k, fr in enumerate(row):
            sheet.alpha_composite(fr.resize((sw, sh), Image.NEAREST), (10 + (k % 4) * sw, 10 + (r * 2 + k // 4) * (sh + 8)))
    sheet.convert("RGB").save(os.path.join(out, "melee_sheet.png")); print("melee scenes ->", out)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
