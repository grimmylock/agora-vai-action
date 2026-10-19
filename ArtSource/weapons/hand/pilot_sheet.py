# Pilot review sheet: RK-12 (reference quality) + the 3 hand-pixelled guns, old vs new, and in hand.
# Also writes the new sprites to /tmp/claude-0/hand_out/weapon_<Key>.png / _hd.png
import sys, os, math, re; sys.path.insert(0, '/home/claude/art/weapons/hand'); sys.path.insert(0, '/home/claude/art/hands')
from PIL import Image, ImageDraw, ImageFont
from common import render
import vk74, psh18, bayou, psh18_full
from ingame_sim import body, hand, pose

OUT = '/tmp/claude-0/hand_out'; os.makedirs(OUT, exist_ok=True)
OLD = '/tmp/claude-0/weapons_out'
GUNS = [("RK12Kuvalda", "sidearm", None), ("PSh18Pchela", "sidearm", psh18), ("VK74", "assault", vk74), ("BayouPump", "shotgun", bayou)]
for key, _, m in GUNS:
    if m: render(m.A).save(f'{OUT}/weapon_{key}_hd.png'); render(m.B).save(f'{OUT}/weapon_{key}.png')
path = lambda key, hd=False: f'{OUT if key != "RK12Kuvalda" else OLD}/weapon_{key}{"_hd" if hd else ""}.png'

GR = {}
for line in open(f'{OLD}/grips.cs.txt'):
    mm = re.search(r'\["(\w+)"\] = new\[\] \{ (.*) \}', line)
    if mm: GR[mm.group(1)] = [float('nan') if 'NaN' in v else float(v.strip().rstrip('f')) for v in mm.group(2).split(',')]

S = 12
def scene(armor, key, cat, ang):
    W, H = 34, 24; ox, oy = 9, 4
    c = Image.new("RGBA", (W * S, H * S), (42, 38, 32, 255))
    c.alpha_composite(body(armor).resize((16 * S, 16 * S), Image.NEAREST), (ox * S, oy * S))
    gx, gy, sx, sy, mx, my = GR[key]
    X, Y, sup, left = pose(cat, ang, gx, gy, None if math.isnan(sx) else sx, None if math.isnan(sy) else sy)
    gun = Image.open(path(key)).convert('RGBA'); gy2 = gy
    if left: gun = gun.transpose(Image.FLIP_TOP_BOTTOM); gy2 = gun.height - gy
    g = gun.resize((gun.width * S // 2, gun.height * S // 2), Image.NEAREST)
    big = Image.new("RGBA", (g.width * 3, g.height * 3)); big.alpha_composite(g, (g.width, g.height))
    pv = (g.width + gx * S / 2, g.height + gy2 * S / 2)
    big = big.rotate(ang, center=pv, resample=Image.NEAREST)
    anchor = ((ox + X + (1 if left else 2)) * S, (oy + Y + 1) * S)
    c.alpha_composite(big, (round(anchor[0] - pv[0]), round(anchor[1] - pv[1])))
    h = hand(armor)
    if left: h = h.transpose(Image.FLIP_LEFT_RIGHT)
    h = h.resize((5 * S, 4 * S), Image.NEAREST)
    if sup: c.alpha_composite(h, (round((ox + sup[0] - 1) * S), round((oy + sup[1] - 1) * S)))
    c.alpha_composite(h, (round((ox + X - 1) * S), round((oy + Y - 1) * S)))
    return c

def big(im, s):
    c = Image.new('RGBA', im.size, (42, 38, 32, 255)); c.alpha_composite(im); return c.resize((im.width * s, im.height * s), Image.NEAREST)

font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 15)
bold = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 18)
RH = 32 * 6 + 36
sheet = Image.new('RGBA', (66 * 6 * 2 + 34 * 6 + 2 * 34 * S // 2 + 70, (len(GUNS) + 1) * RH + 50), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
d.text((12, 10), "Pilot: hand-pixelled weapons vs the RK-12 bar  -  old pipeline A | new A | new B (in-hand sprite) | in hand", font=bold, fill=(230, 230, 230))
for i, (key, cat, m) in enumerate(GUNS):
    y = 46 + i * RH; x = 12
    d.text((x, y), key + ("  (reference, unchanged)" if not m else ""), font=font, fill=(200, 204, 208)); y += 22
    old = Image.open(f'{OLD}/weapon_{key}_hd.png').convert('RGBA')
    new = Image.open(path(key, True)).convert('RGBA')
    sheet.alpha_composite(big(old, 6), (x, y)); x += old.width * 6 + 12
    sheet.alpha_composite(big(new, 6), (x, y)); x += new.width * 6 + 12
    b = Image.open(path(key)).convert('RGBA'); sheet.alpha_composite(big(b, 6), (x, y)); x += b.width * 6 + 12
    for j, (arm, ang) in enumerate((("Leather Jacket", 0), ("Sentinel Armored Suit", 25))):
        sc = scene(arm, key, cat, ang).resize((34 * S // 2, 24 * S // 2), Image.NEAREST)
        sheet.alpha_composite(sc, (x + j * (sc.width + 8), y))
y = 46 + len(GUNS) * RH
d.text((12, y), "PSh18Pchela option 2: menu sprite fills the 64x32 canvas like the RK-12 (in-hand B stays the same)", font=font, fill=(200, 204, 208))
full = render(psh18_full.A); full.save(f'{OUT}/weapon_PSh18Pchela_hd_full.png')
sheet.alpha_composite(big(Image.open(f'{OLD}/weapon_RK12Kuvalda_hd.png').convert('RGBA'), 6), (12, y + 22))
sheet.alpha_composite(big(full, 6), (12 + 66 * 6 + 12, y + 22))
sheet.convert('RGB').save('/tmp/claude-0/pilot_weapons.png'); print(sheet.size)
