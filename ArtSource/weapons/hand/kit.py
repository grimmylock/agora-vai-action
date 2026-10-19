# Weapon hand-pixel kit (Batch 29+).
#   python3 kit.py guide <Key> <width>   -> ASCII guide of the photo at that width + trace PNG to look at
#   python3 kit.py sheet <Key> [<Key>..] -> review sheet: photo | A (menu) | B (in hand) | held at 0/25/180 deg
# Each gun lives in guns/<Key>.py with CAT, A (rows, <=64x32), B (rows, in-hand), GRIP / SUPPORT / MUZZLE
# in B-local pixels (SUPPORT None = pistol cup / one-handed). See b_v2.py for the pilot four.
import sys, os, math, importlib, glob
sys.path.insert(0, '/home/claude/art/weapons'); sys.path.insert(0, '/home/claude/art/weapons/hand'); sys.path.insert(0, '/home/claude/art/hands')
from PIL import Image, ImageDraw, ImageFont
import weapon_pixel as wp
from batch_all import CFG
import b_v2
from common import place

CFG_FLAT = {k: v for cat in CFG.values() for k, v in cat.items()}
CAT_OF = {k: c for c, cat in CFG.items() for k in cat}
TMP = '/tmp/claude-0/kit'; os.makedirs(TMP, exist_ok=True)

def cut(key): return wp.load_cut(key, CFG_FLAT[key])

def guide(key, w):
    cfg = dict(CFG_FLAT[key]); im = cut(key)
    g = wp.to_grid(im, w, cfg)
    print(f"{key}: {w}x{len(g)}")
    print("   " + "".join(str(i // 10) if i % 10 == 0 else " " for i in range(w)))
    print("   " + "".join(str(i % 10) for i in range(w)))
    for y, r in enumerate(g): print(f"{y:2d} " + "".join(r))
    h = len(g); s = max(6, 900 // w)
    t = im.resize((w * s, h * s), Image.LANCZOS)
    bg = Image.new('RGBA', t.size, (255, 255, 255, 255)); bg.alpha_composite(t); d = ImageDraw.Draw(bg)
    for x in range(w + 1): d.line([(x * s, 0), (x * s, h * s)], fill=(255, 0, 0, 255) if x % 5 == 0 else (210, 180, 255, 255))
    for y in range(h + 1): d.line([(0, y * s), (w * s, y * s)], fill=(0, 0, 255, 255) if y % 5 == 0 else (210, 180, 255, 255))
    for x in range(0, w, 5): d.text((x * s + 2, 1), str(x), fill=(255, 0, 0, 255))
    for y in range(0, h, 5): d.text((2, y * s + 2), str(y), fill=(0, 0, 255, 255))
    p = f'{TMP}/trace_{key}_{w}.png'; bg.convert('RGB').save(p); print(p)

def load(key):
    if key in b_v2.ALL:
        import vk74, bayou, psh18_full
        A = {"VK74": vk74.A, "BayouPump": bayou.A, "PSh18Pchela": psh18_full.A}.get(key)
        if key == "RK12Kuvalda":
            import importlib.util as ilu; cwd = os.getcwd(); os.chdir(TMP)
            sp = ilu.spec_from_file_location("rsh12", "/home/claude/art/rsh12_hand64.py"); rk = ilu.module_from_spec(sp); sp.loader.exec_module(rk)
            os.chdir(cwd); A = rk.G
        d = b_v2.ALL[key]
        return dict(CAT=CAT_OF.get(key, "sidearm"), A=A, B=d["rows"], GRIP=d["grip"], SUPPORT=d["support"], MUZZLE=d["muzzle"], placed=True)
    m = importlib.import_module("guns." + key); importlib.reload(m)
    return dict(CAT=m.CAT, A=m.A, B=m.B, GRIP=m.GRIP, SUPPORT=m.SUPPORT, MUZZLE=m.MUZZLE, placed=False)

def check(rows, name):
    for i, r in enumerate(rows): assert len(r) == len(rows[0]), f"{name} row {i}: {len(r)} != {len(rows[0])}  {r!r}"
    for r in rows:
        for ch in r: assert ch == "." or ch in b_v2.PAL, f"{name}: bad char {ch!r}"

def a_image(g):
    A = g["A"] if g["placed"] else place([list(r) for r in g["A"]], 64, 32)
    if not g["placed"]: check(g["A"], "A"); assert len(g["A"][0]) <= 64 and len(g["A"]) <= 32
    rows = ["".join(r) for r in A]
    im = Image.new("RGBA", (64, 32)); px = im.load()
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch != ".": px[x, y] = wp.hexc(b_v2.PAL[ch]) + (255,)
    out = im.copy(); o = out.load()
    for y in range(32):
        for x in range(64):
            if not px[x, y][3] and any(0 <= x + dx < 64 and 0 <= y + dy < 32 and px[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = b_v2.K
    return out

def b_image(g): check(g["B"], "B"); return b_v2.render(g["B"])

def grips(g):
    nan = float("nan"); (gx, gy), s, (mx, my) = g["GRIP"], g["SUPPORT"], g["MUZZLE"]
    return [gx + 1, gy + 1, s[0] + 1 if s else nan, s[1] + 1 if s else nan, mx + 1, my + 1]

# ---- held preview (same rules as HeldWeapon.cs Batch 28: 4x3 gloves, fist centred on the grip point) ----
from ingame_sim import body, hand, pose
def glove(armor):
    h = hand(armor).load(); out = Image.new("RGBA", (4, 3)); col = {"K": h[0, 1], "c": h[1, 1], "g": h[2, 2]}
    for y, r in enumerate([".KK.", "KcgK", ".KK."]):
        for x, ch in enumerate(r):
            if ch != ".": out.putpixel((x, y), col[ch])
    return out
S = 12
def scene(armor, g, ang):
    W, H = 40, 24; ox, oy = 12, 4
    c = Image.new("RGBA", (W * S, H * S), (42, 38, 32, 255))
    c.alpha_composite(body(armor).resize((16 * S, 16 * S), Image.NEAREST), (ox * S, oy * S))
    gx, gy, sx, sy, mx, my = grips(g); cat = g["CAT"]
    X, Y, sup, left = pose(cat, ang, gx, gy, None if math.isnan(sx) else sx, None if math.isnan(sy) else sy)
    if cat == "melee": sup = None
    if left: X -= 1; sup = (sup[0] - 1, sup[1]) if sup else sup
    gun = b_image(g); gy2 = gy
    if left: gun = gun.transpose(Image.FLIP_TOP_BOTTOM); gy2 = gun.height - gy
    gi = gun.resize((gun.width * S // 2, gun.height * S // 2), Image.NEAREST)
    big = Image.new("RGBA", (gi.width * 3, gi.height * 3)); big.alpha_composite(gi, (gi.width, gi.height))
    pv = (gi.width + gx * S / 2, gi.height + gy2 * S / 2)
    big = big.rotate(ang, center=pv, resample=Image.NEAREST)
    c.alpha_composite(big, (round((ox + X + 2) * S - pv[0]), round((oy + Y + 1.5) * S - pv[1])))
    gl = glove(armor)
    if left: gl = gl.transpose(Image.FLIP_LEFT_RIGHT)
    gl = gl.resize((4 * S, 3 * S), Image.NEAREST)
    if sup: c.alpha_composite(gl, (round((ox + sup[0]) * S), round((oy + sup[1]) * S)))
    c.alpha_composite(gl, (round((ox + X) * S), round((oy + Y) * S)))
    return c

ARMS = ["Leather Jacket", "Sentinel Armored Suit", "Raider Jacket", "Marsh Recon Suit", "Kokon Suit", "Contractor Light"]
def sheet(keys, out=None):
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 14)
    RH = 32 * 5 + 30; cw = 40 * S // 3; ch = 24 * S // 3
    W = 260 + 64 * 5 + 20 + 52 * 6 + 20 + 3 * (cw + 6)
    sh = Image.new("RGBA", (W, len(keys) * RH + 10), (27, 29, 31, 255)); d = ImageDraw.Draw(sh)
    for i, key in enumerate(keys):
        y = 6 + i * RH; g = load(key)
        try: ph = cut(key); ph.thumbnail((250, 140)); bg = Image.new("RGBA", ph.size, (235, 235, 235, 255)); bg.alpha_composite(ph); sh.alpha_composite(bg, (6, y + 20))
        except Exception: pass
        b = b_image(g)
        d.text((6, y), f"{key}  [{g['CAT']}]  B {len(g['B'][0])}x{len(g['B'])}", font=font, fill=(200, 204, 208))
        a = a_image(g); bga = Image.new("RGBA", a.size, (42, 38, 32, 255)); bga.alpha_composite(a)
        sh.alpha_composite(bga.resize((320, 160), Image.NEAREST), (260, y + 20))
        sb = max(2, min(6, 300 // b.width)); bgb = Image.new("RGBA", b.size, (42, 38, 32, 255)); bgb.alpha_composite(b)
        sh.alpha_composite(bgb.resize((b.width * sb, b.height * sb), Image.NEAREST), (600, y + 20))
        for j, ang in enumerate((0, 25, 180)):
            sh.alpha_composite(scene(ARMS[(i + j) % len(ARMS)], g, ang).resize((cw, ch), Image.NEAREST), (W - 3 * (cw + 6) + j * (cw + 6), y + 20))
    out = out or f"{TMP}/sheet.png"; sh.convert("RGB").save(out); print(out, sh.size)

if __name__ == "__main__":
    if sys.argv[1] == "guide": guide(sys.argv[2], int(sys.argv[3]))
    elif sys.argv[1] == "sheet": sheet(sys.argv[2:])
