"""Melee 2c (Yan: "simulate a melee fight between one player and one normal npc, try to include at least 2 parries"): the duel from
the harness (`dotnet /tmp/test.dll meleeduel duel.json`: a bot on the real controls against one Veteran Raider run by the real AI,
one frame every 1/30 s of real time, so the parry's slow motion plays as you'd see it). Drawn with the game's sprites: both bodies
(leaning with their swings), the machete and his knife turned round the fist, the fists riding and turning with the swing, the
free hand, the parry sparks, blood, the perfect parry's lean-in and cold edge, health bars, and a caption of what the bot is doing.
ponytail: the effects follow CombatFxView's recipes loosely (sparks, puffs), not frame for frame.
  python3 Tools/render/melee_duel.py duel.json out_dir  -> melee_duel.mp4, melee_duel.gif, melee_duel_sheet.png"""
import json, math, os, random, subprocess, sys, tempfile
from PIL import Image, ImageDraw, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(R, "Assets/Resources/Sprites"); FONTS = os.path.join(R, "Assets/Resources/Fonts")
d = json.load(open(sys.argv[1])); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
F = d["frames"]; ox, oy = d["origin"]
Z = 4; X0, Y0, W, H = ox - 2.0, oy - 2.5, 9.0, 4.5
world = Image.open(os.path.join(R, "Tools/render/_render/perimeter.png")).convert("RGBA")
BG = world.crop((int(X0 * 16), int(Y0 * 16), int((X0 + W) * 16), int((Y0 + H) * 16))).resize((int(W * 16 * Z), int(H * 16 * Z)), Image.NEAREST)
def hexc(h, a=255): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, a)
def font(n, s): return ImageFont.truetype(os.path.join(FONTS, n), s)
CAP, SMALL = font("PixelifySans-SemiBold.ttf", 26), font("PixelifySans-SemiBold.ttf", 18)
atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA")
def body(i): i = max(0, i); return atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16))
ai, acuff, aglove, ri, rcuff, rglove = d["armor"]
PB, RB = body(ai), body(ri).transpose(Image.FLIP_LEFT_RIGHT)
MACHETE = Image.open(os.path.join(SPR, "Weapons/weapon_SteppeMachete.png")).convert("RGBA")
KNIFE = Image.open(os.path.join(SPR, "Weapons/weapon_HuntingKnife.png")).convert("RGBA")
def fx(n, k=3): im = Image.open(os.path.join(SPR, f"Fx/{n}.png")).convert("RGBA"); w = im.width // k; return [im.crop((i * w, 0, i * w + w, im.height)) for i in range(k)]
SPARK, BLOOD = fx("fx_hit_spark"), fx("fx_hit_blood")
def glove(cuff, gl, big=True):
    g = hexc(gl); lit = tuple(min(255, int(v * 1.45)) for v in g[:3]) + (255,); dark = tuple(int(v * 0.65) for v in g[:3]) + (255,)
    rows = [".KKK.", "KcllK", "KcgdK", ".KKK."] if big else [".KK.", "KcgK", ".KK."]
    img = Image.new("RGBA", (len(rows[0]), len(rows))); col = {"K": hexc("#141617"), "c": hexc(cuff), "l": lit, "g": g, "d": dark}
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img.resize((img.width * Z, img.height * Z), Image.NEAREST)
PG, PS, RG, RS = glove(acuff, aglove), glove(acuff, aglove, False), glove(rcuff, rglove), glove(rcuff, rglove, False)
def tint(img, col, k):
    im = img.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a: px[x, y] = (int(r + (col[0] - r) * k), int(g + (col[1] - g) * k), int(b + (col[2] - b) * k), a)
    return im

def unit(c, bod, weapon, g, fr, fist, free, tx, ty, flash=0.0, flip=False):
    gx, gy, deg, hx, hy, sx, sy, sup, left, lean, hdeg, length = fr
    L = Image.new("RGBA", (64 * Z, 64 * Z)); o = 24 * Z                     # the unit on its own layer, turned by its lean
    b = tint(bod, (255, 90, 80), flash) if flash > 0 else bod
    L.alpha_composite(b.resize((16 * Z, 16 * Z), Image.NEAREST), (o, o))
    s = weapon.transpose(Image.FLIP_TOP_BOTTOM) if left else weapon
    if length < 0.99: s = s.resize((max(1, round(s.width * length)), s.height), Image.NEAREST)
    s = s.resize((s.width * Z // 2, s.height * Z // 2), Image.NEAREST)
    pvx, pvy = g[0] * length * Z / 2, ((weapon.height - g[1]) if left else g[1]) * Z / 2
    big = Image.new("RGBA", (64 * Z, 64 * Z)); big.alpha_composite(s, (int(32 * Z - pvx), int(32 * Z - pvy)))
    big = big.rotate(deg, resample=Image.NEAREST, center=(32 * Z, 32 * Z))
    L.alpha_composite(big, (int(o + gx * Z - 32 * Z), int(o + gy * Z - 32 * Z)))
    fz = (fist.transpose(Image.FLIP_LEFT_RIGHT) if left else fist).rotate(hdeg, expand=True, resample=Image.NEAREST)
    L.alpha_composite(fz, (int(o + hx * Z - fz.width / 2), int(o + hy * Z - fz.height / 2)))
    if sup:
        fr_ = free.transpose(Image.FLIP_LEFT_RIGHT) if not left else free
        L.alpha_composite(fr_, (int(o + sx * Z - fr_.width / 2), int(o + sy * Z - fr_.height / 2)))
    L = L.rotate(lean, center=(o + 8 * Z, o + 8 * Z), resample=Image.NEAREST)
    X, Y = (tx - X0) * 16 * Z, (ty - Y0) * 16 * Z
    ImageDraw.Draw(c).ellipse((X - 6 * Z, Y + 5 * Z, X + 6 * Z, Y + 9 * Z), fill=(30, 26, 22, 110))
    c.alpha_composite(L, (int(X - 8 * Z - o), int(Y - 8 * Z - o)))

parts = []
def burst(x, y, col, n, sp, life, t, seed):
    r = random.Random(seed)
    for _ in range(n):
        a = r.random() * math.tau; v = sp * r.uniform(0.5, 1.2)
        parts.append([x, y, math.cos(a) * v, math.sin(a) * v - 40, t, life * r.uniform(0.7, 1.2), col, None, 1])
def puff(x, y, frs, col, life, scale, t): parts.append([x, y, 0, 0, t, life, col, frs, scale])

VIG = Image.new("L", (BG.width // 8, BG.height // 8))
for y in range(VIG.height):
    for x in range(VIG.width):
        dx, dy = (x + .5) / VIG.width * 2 - 1, (y + .5) / VIG.height * 2 - 1; rr = math.sqrt(dx * dx * .8 + dy * dy * .8)
        v = min(1, max(0, (rr - .74) / (1.18 - .74))); VIG.putpixel((x, y), int(255 * v * v * (3 - 2 * v)))
VIG = VIG.resize(BG.size, Image.BILINEAR)
hurt = {}; pool = []; shake = [-9, 0]
def draw(i):
    f = F[i]; t = i / 30
    c = BG.copy(); dd = ImageDraw.Draw(c)
    px, py, php, ptilt = f["p"]; rx, ry, rhp, ralive, rbroken = f["r"]
    P = lambda x, y: ((x - X0) * 16 * Z, (y - Y0) * 16 * Z)
    (PX, PY), (RX, RY) = P(px, py), P(rx, ry)
    for e in f["ev"]:
        kind, byr, hit = e
        if kind == "parry":
            mx, my = (PX + RX) / 2, (PY + RY) / 2 - 7 * Z
            puff(mx, my, SPARK, (255, 236, 180), 0.3 if hit else 0.15, 3.6 if hit else 2.0, t)
            burst(mx, my, (255, 236, 180), 24 if hit else 8, 260 if hit else 140, 0.45, t, i)
            shake[:] = [t, 0.2 if hit else 0.06]
        elif kind == "impact" and hit:
            X, Y = (PX, PY) if byr else (RX, RY)
            puff(X, Y - 4 * Z, BLOOD, (180, 30, 40), 0.22, 2.4, t); burst(X, Y - 4 * Z, (150, 20, 28), 10, 160, 0.35, t, i)
            hurt["p" if byr else "r"] = t; pool.append((X + random.Random(i).uniform(-8, 8) * Z / 4, Y + 6 * Z))
    for X, Y in pool: dd.ellipse((X - 3 * Z, Y - Z, X + 3 * Z, Y + Z), fill=(110, 18, 24, 200))
    def drawp(): unit(c, PB, MACHETE, d["gm"], f["pf"], PG, PS, px, py, max(0, 1 - (t - hurt.get("p", -9)) / 0.15) * 0.7)
    def drawr():
        if not ralive:
            lying = RB.resize((16 * Z, 16 * Z), Image.NEAREST).rotate(-90, expand=True, resample=Image.NEAREST)
            c.alpha_composite(lying, (int(RX - lying.width / 2), int(RY + 2 * Z - lying.height / 2))); return
        unit(c, RB, KNIFE, d["gk"], f["rf"], RG, RS, rx, ry, max(0, 1 - (t - hurt.get("r", -9)) / 0.15) * 0.7)
    for _, fn in sorted([(py, drawp), (ry, drawr)], key=lambda r: r[0]): fn()
    keep = []
    for pr in parts:
        x, y, vx, vy, born, life, col, frs, scale = pr
        age = t - born
        if age < 0 or age > life: continue
        keep.append(pr); k = age / life; a = int(255 * (1 - k))
        if frs:
            fr_ = frs[min(len(frs) - 1, int(k * len(frs)))]
            im = tint(fr_.resize((max(1, int(fr_.width * Z * scale / 2)), max(1, int(fr_.height * Z * scale / 2))), Image.NEAREST), col, 1.0)
            im.putalpha(im.getchannel("A").point(lambda v: v * a // 255)); c.alpha_composite(im, (int(x - im.width / 2), int(y - im.height / 2)))
        else:
            xx, yy = x + vx * age, y + vy * age + 300 * age * age; dd.rectangle((xx, yy, xx + Z / 2, yy + Z / 2), fill=col + (a,))
    parts[:] = keep
    punch = f["punch"]
    if punch > 0.01:
        k = 1 - 0.09 * punch; cw, ch = c.width * k, c.height * k; x0, y0 = (c.width - cw) / 2, (c.height - ch) / 2
        c = c.crop((int(x0), int(y0), int(x0 + cw), int(y0 + ch))).resize(BG.size, Image.NEAREST)
        edge = Image.new("RGBA", c.size, (184, 230, 255, 0)); edge.putalpha(VIG.point(lambda v: int(v * 0.6 * punch))); c.alpha_composite(edge)
    amp = shake[1] * max(0, 1 - (t - shake[0]) / 0.15) * 16 * Z
    if amp > 0.5:
        r = random.Random(i); c = c.transform(c.size, Image.AFFINE, (1, 0, r.uniform(-amp, amp), 0, 1, r.uniform(-amp, amp)), Image.NEAREST)
    dd = ImageDraw.Draw(c)
    for k, (name, hp, mx_) in enumerate((("YOU", php, d["maxhp"][0]), ("RAIDER (VETERAN)", rhp, d["maxhp"][1]))):
        x = 16 if k == 0 else c.width - 16 - 220
        dd.text((x, 10), name, font=SMALL, fill=(232, 220, 192)); dd.rectangle((x, 34, x + 220, 46), fill=(20, 18, 18))
        dd.rectangle((x + 2, 36, x + 2 + 216 * max(0, hp) / mx_, 44), fill=(184, 48, 42))
    cap = f["label"] + ("   (slow motion)" if f["ts"] < 0.9 else "")
    dd.text((16, c.height - 40), cap, font=CAP, fill=(255, 225, 140) if "parry" in f["label"] or "broken" in f["label"] else (232, 220, 192))
    dd.text((c.width - 90, c.height - 34), f"{t:4.1f} s", font=SMALL, fill=(200, 200, 200))
    return c

tmp = tempfile.mkdtemp(); sheet, want = [], []
first = {}
for i, f in enumerate(F):
    for e in f["ev"]:
        if e[0] == "parry": want.append(i + 3)
        if e[0] == "impact" and e[2] and e[1] and "cut" not in first: first["cut"] = i + 2
    if f["r"][4] and "broken" not in first: first["broken"] = i + 8
want = sorted(set(want + list(first.values()) + [0]))
gif = []
for i in range(len(F)):
    im = draw(i).convert("RGB"); im = im.resize((im.width * 2, im.height * 2), Image.NEAREST); im.save(os.path.join(tmp, f"{i:05d}.png"))
    if i % 2 == 0: gif.append(im.resize((im.width // 3, im.height // 3), Image.LANCZOS))
    if i in want: sheet.append(im)
subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "30", "-i", os.path.join(tmp, "%05d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p",
                "-crf", "20", os.path.join(out, "melee_duel.mp4")], check=True)
gif[0].save(os.path.join(out, "melee_duel.gif"), save_all=True, append_images=gif[1:], duration=66, loop=0)
cw, ch = sheet[0].width // 2, sheet[0].height // 2; cols = 2; rows = (len(sheet) + 1) // 2
s = Image.new("RGB", (cols * cw + 24, rows * ch + 8 * (rows + 1)), (24, 26, 28))
for n, im in enumerate(sheet): s.paste(im.resize((cw, ch), Image.LANCZOS), (8 + (n % 2) * (cw + 8), 8 + (n // 2) * (ch + 8)))
s.save(os.path.join(out, "melee_duel_sheet.png"))
for fn in os.listdir(tmp): os.remove(os.path.join(tmp, fn))
os.rmdir(tmp); print(len(F), "frames ->", out)
