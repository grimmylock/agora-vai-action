# Preview: v2 in-hand sprites + 4x3 gloves, fist on the handle centre, support glove under the handguard.
import sys, math; sys.path.insert(0, '/home/claude/art/weapons/hand'); sys.path.insert(0, '/home/claude/art/hands')
from PIL import Image, ImageDraw, ImageFont
from ingame_sim import body, hand, pose
import b_v2

CAT = {"PSh18Pchela": "sidearm", "RK12Kuvalda": "sidearm", "VK74": "assault", "BayouPump": "shotgun"}
def glove(armor):
    """4x3: outline ring around cuff + glove (cuff on the body side)"""
    h = hand(armor).load(); out = Image.new("RGBA", (4, 3)); col = {"K": h[0, 1], "c": h[1, 1], "g": h[2, 2]}
    for y, r in enumerate([".KK.", "KcgK", ".KK."]):
        for x, ch in enumerate(r):
            if ch != ".": out.putpixel((x, y), col[ch])
    return out

S = 12
def scene(armor, key, ang):
    W, H = 38, 24; ox, oy = 10, 4
    c = Image.new("RGBA", (W * S, H * S), (42, 38, 32, 255))
    c.alpha_composite(body(armor).resize((16 * S, 16 * S), Image.NEAREST), (ox * S, oy * S))
    gx, gy, sx, sy, mx, my = b_v2.grips(key)
    X, Y, sup, left = pose(CAT[key], ang, gx, gy, None if math.isnan(sx) else sx, None if math.isnan(sy) else sy)
    if left: X -= 1; sup = (sup[0] - 1, sup[1]) if sup else sup          # 4-wide glove mirrors as x -> 12 - x
    gun = b_v2.render(b_v2.ALL[key]["rows"]); gy2 = gy
    if left: gun = gun.transpose(Image.FLIP_TOP_BOTTOM); gy2 = gun.height - gy
    g = gun.resize((gun.width * S // 2, gun.height * S // 2), Image.NEAREST)
    big = Image.new("RGBA", (g.width * 3, g.height * 3)); big.alpha_composite(g, (g.width, g.height))
    pv = (g.width + gx * S / 2, g.height + gy2 * S / 2)
    big = big.rotate(ang, center=pv, resample=Image.NEAREST)
    anchor = ((ox + X + 2) * S, (oy + Y + 1.5) * S)                  # handle centre = glove centre
    c.alpha_composite(big, (round(anchor[0] - pv[0]), round(anchor[1] - pv[1])))
    gl = glove(armor)
    if left: gl = gl.transpose(Image.FLIP_LEFT_RIGHT)
    gl = gl.resize((4 * S, 3 * S), Image.NEAREST)
    if sup: c.alpha_composite(gl, (round((ox + sup[0]) * S), round((oy + sup[1]) * S)))
    c.alpha_composite(gl, (round((ox + X) * S), round((oy + Y) * S)))
    return c

def unarmed(armor):
    """nothing equipped: the big 5x4 gloves float at the torso's sides (rows 8-11)"""
    W, H = 38, 24; ox, oy = 10, 4
    c = Image.new("RGBA", (W * S, H * S), (42, 38, 32, 255))
    c.alpha_composite(body(armor).resize((16 * S, 16 * S), Image.NEAREST), (ox * S, oy * S))
    h = hand(armor).resize((5 * S, 4 * S), Image.NEAREST)
    c.alpha_composite(h, ((ox + 11) * S, (oy + 8) * S)); c.alpha_composite(h.transpose(Image.FLIP_LEFT_RIGHT), (ox * S, (oy + 8) * S))
    return c

if __name__ == "__main__":
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 15)
    bold = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 17)
    ANG = [0, 25, -20, 180, 155]
    ARM = ["Leather Jacket", "Sentinel Armored Suit", "Raider Jacket", "Marsh Recon Suit"]
    cw, ch = 38 * S // 2 * 1, 24 * S // 2
    rows = list(CAT)
    sheet = Image.new("RGBA", (300 + len(ANG) * (cw + 8) + 10, (len(rows) + 1) * (ch + 10) + 60), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    d.text((10, 8), "In-hand v2: hand-drawn at the new sizes, 4x3 gloves, fist on the handle, support glove under the handguard", font=bold, fill=(230, 230, 230))
    for j, a in enumerate(ANG): d.text((300 + j * (cw + 8), 36), f"aim {a}°", font=font, fill=(200, 204, 208))
    for i, key in enumerate(rows):
        y = 58 + i * (ch + 10)
        spr = b_v2.render(b_v2.ALL[key]["rows"]); bg = Image.new("RGBA", spr.size, (42, 38, 32, 255)); bg.alpha_composite(spr)
        sc = min(6, 280 // spr.width); sheet.alpha_composite(bg.resize((spr.width * sc, spr.height * sc), Image.NEAREST), (10, y + 18))
        d.text((10, y), key, font=font, fill=(200, 204, 208))
        for j, a in enumerate(ANG):
            sheet.alpha_composite(scene(ARM[i], key, a).resize((cw, ch), Image.NEAREST), (300 + j * (cw + 8), y))
    y = 58 + len(rows) * (ch + 10); d.text((10, y), "Nothing equipped (big gloves)", font=font, fill=(200, 204, 208))
    for j, arm in enumerate(ARM + ["Wanderer Exoskeleton"]):
        sheet.alpha_composite(unarmed(arm).resize((cw, ch), Image.NEAREST), (300 + j * (cw + 8), y))
    sheet.convert("RGB").save("/tmp/claude-0/hold_v2.png"); print(sheet.size)
