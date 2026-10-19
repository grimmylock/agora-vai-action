# Draws HudView.cs's layout (same numbers) over the mock frame, to check proportions offline.
import sys
from PIL import Image, ImageDraw, ImageFont
F = "/home/claude/art/fonts/"
def font(name, px): return ImageFont.truetype(F + name, int(px))
TITLE, BODY = "Silkscreen-Regular.ttf", "PixelifySans-Regular.ttf"
def hx(h, a=255): return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
FILL, EDGE, LIP = hx("15171a", 240), hx("3a3e41"), hx("2b2f33")
CREAM, DIM, YEL, RED, CYAN, PUR, GRN, EMPTY = hx("e6dcc0"), hx("7d7a70"), hx("e8c547"), hx("c8443a"), hx("4fb6c9"), hx("9d7bd8"), hx("62b25a"), hx("2a2c2e")
W, H = 1920, 1080
im = Image.open(sys.argv[1] if len(sys.argv) > 1 else "/tmp/claude-0/mock_frame.png").convert("RGBA")
ov = Image.new("RGBA", im.size); d = ImageDraw.Draw(ov)

def place(anchor, pos, size):
    """Unity anchor/pivot at the same corner, anchoredPosition pos (y up) -> top-left px (y down)"""
    ax, ay = anchor; x = ax * W + pos[0] - ax * size[0]; y = (1 - ay) * H - pos[1] - (1 - ay) * size[1]
    return int(x), int(y)
def box(anchor, pos, size):
    x, y = place(anchor, pos, size)
    d.rectangle([x, y, x + size[0] - 1, y + size[1] - 1], fill=EDGE)
    d.rectangle([x + 2, y + 2, x + size[0] - 3, y + size[1] - 3], fill=FILL)
    d.line([(x + 2, y + 2), (x + size[0] - 3, y + 2)], fill=LIP)
    return x + 2, y + 2
def txt(o, pos, s, f, px, c, spacing=0, w=None, align="l"):
    x, y = o[0] + pos[0], o[1] - pos[1]
    fn = font(f, px)
    spacing = spacing * px / 100          # TMP characterSpacing is in 1/100 em
    if spacing:
        cx = x
        if align == "c" and w: cx = x + (w - sum(fn.getlength(ch) + spacing for ch in s)) / 2
        if align == "r" and w: cx = x + w - sum(fn.getlength(ch) + spacing for ch in s)
        for ch in s: d.text((cx, y), ch, font=fn, fill=c); cx += fn.getlength(ch) + spacing
    else:
        if align == "c" and w: x += (w - fn.getlength(s)) / 2
        d.text((x, y), s, font=fn, fill=c)
def rect(o, pos, size, c): d.rectangle([o[0] + pos[0], o[1] - pos[1], o[0] + pos[0] + size[0] - 1, o[1] - pos[1] + size[1] - 1], fill=c)
def segs(o, pos, n, w, h, gap, k, c):
    for i in range(n): rect(o, (pos[0] + i * (w + gap), pos[1]), (w, h), c if i < k else EMPTY)

z = box((0, 1), (20, -20), (420, 78))
txt(z, (16, -10), "THE PERIMETER", TITLE, 24, CREAM, 6); txt(z, (16, -46), "DAY 3   TIER 1   4 EXITS", TITLE, 12, DIM, 2)
tb = box((.5, 1), (0, -18), (320, 42)); txt(tb, (0, -9), "YOUR TURN   SURGE IN 2", TITLE, 16, YEL, 6, 316, "c")
tc = box((.5, 1), (0, -70), (390, 58)); txt(tc, (14, -8), "RAIDER  VETERAN", TITLE, 15, CREAM, 6); txt(tc, (250, -8), "HOSTILE", TITLE, 15, RED, 6, 122, "r")
segs(tc, (14, -34), 10, 33.4, 12, 2, 6, RED)
pc = box((0, 0), (20, 20), (520, 180))
rect(pc, (10, -10), (104, 104), EDGE); rect(pc, (12, -12), (100, 100), hx("0e0f11"))
txt(pc, (128, -12), "HP", TITLE, 14, DIM, 6); segs(pc, (168, -12), 10, 26, 18, 2, 7, RED); txt(pc, (452, -10), "72", TITLE, 16, CREAM, 6)
txt(pc, (128, -46), "AC 13", TITLE, 14, CREAM, 6)
for i, (s, c) in enumerate([("BLEEDING", RED), ("RAD", GRN)]):
    x0 = 232 + i * 96; rect(pc, (x0, -44), (92, 24), c); rect(pc, (x0 + 2, -46), (88, 20), FILL); txt(pc, (x0, -46), s, TITLE, 12, c, 3, 92, "c")
txt(pc, (128, -82), "AP", TITLE, 14, DIM, 6); segs(pc, (168, -84), 4, 16, 16, 8, 3, YEL)
txt(pc, (320, -82), "MP", TITLE, 14, DIM, 6); segs(pc, (360, -84), 4, 16, 16, 8, 4, CYAN)
for x0, lab, c, f in ((12, "FOOD", YEL, .7), (184, "WATER", CYAN, .6), (356, "REST", PUR, .8)):
    txt(pc, (x0, -130), lab, TITLE, 12, c, 6); rect(pc, (x0 + 62, -134), (92, 14), EMPTY); rect(pc, (x0 + 62, -134), (int(92 * f), 14), c)
wc = box((1, 0), (-20, 20), (560, 180))
rect(wc, (10, -10), (196, 104), hx("0e0f11"))
try:
    gun = Image.open("/tmp/claude-0/weapons_hand_out/weapon_VK74_hd.png").convert("RGBA"); s = min(184 / gun.width, 92 / gun.height)
    gun = gun.resize((int(gun.width * s), int(gun.height * s)), Image.NEAREST); ov.alpha_composite(gun, (wc[0] + 16 + (184 - gun.width) // 2, wc[1] + 16 + (92 - gun.height) // 2))
except Exception as e: print(e)
txt(wc, (220, -10), "VK-74", TITLE, 20, CREAM, 6)
for i, (s, c) in enumerate([("WORN", YEL), ("SINGLE", DIM)]):
    x0 = 220 + i * 100; rect(wc, (x0, -42), (96, 24), c); rect(wc, (x0 + 2, -44), (92, 20), FILL); txt(wc, (x0, -44), s, TITLE, 12, c, 3, 96, "c")
txt(wc, (220, -72), "18", TITLE, 38, CREAM, 6); txt(wc, (306, -84), "/ 30", TITLE, 18, DIM, 6)
txt(wc, (410, -78), "STANDARD", TITLE, 12, DIM, 6, 140, "r"); txt(wc, (410, -94), "+60 SPARE", TITLE, 12, DIM, 6, 140, "r")
for i in range(4):
    rect(wc, (10 + i * 62, -122), (56, 48), EDGE); rect(wc, (12 + i * 62, -124), (52, 44), hx("0e0f11")); txt(wc, (14 + i * 62, -124), str(i + 1), TITLE, 10, DIM)
    txt(wc, (12 + i * 62, -140), ["MEDKIT", "BANDAG", "MRE", "GRENAD"][i], BODY, 13, CREAM, 0, 52, "c")
txt(wc, (264, -138), "BELT", TITLE, 12, DIM, 6)
for i in range(4): rect(wc, (320 + i * 56, -122), (50, 48), EDGE); rect(wc, (322 + i * 56, -124), (46, 44), hx("0e0f11"))
et = box((1, 0), (-20, 210), (240, 44)); txt(et, (0, -11), "SPACE  END TURN", TITLE, 15, YEL, 6, 236, "c")
x, y = place((.5, 0), (0, 8), (900, 20)); txt((x, y), (0, 0), "I GEAR    J QUESTS    P CHARACTER    M ZONE    F1 CONTROLS", TITLE, 12, DIM, 6, 900, "c")
lx, ly = place((0, 0), (24, 212), (760, 150))
for i, l in enumerate(["VK-74 hits Raider for 14 damage.", "Raider is in light cover: +1 AC.", "Raider fires at you - miss.", "You are bleeding (-2 HP).", "Picked up Frag Grenade."]):
    for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)): d.text((lx + ox, ly + 40 + i * 22 + oy), l, font=font(BODY, 18), fill=(0, 0, 0, 255))
    d.text((lx, ly + 40 + i * 22), l, font=font(BODY, 18), fill=CREAM)
im.alpha_composite(ov); im.convert("RGB").save("/tmp/claude-0/mock_hud.png"); print("ok")
