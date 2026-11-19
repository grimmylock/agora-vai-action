"""Tier 2 "The Crown" (Yan, 2026-11-19: "give me a preview of the duel, animations and all"): the duel at the Scrap King's Nest - Spool against the King while you hold off the waves - from the harness
(`dotnet /tmp/test.dll crownduel duel.json`: the real CrownJob.DuelTick / DuelFinish, the boss framework's beats - a wind-up held for a second, the swing starting just before the hit - and the real held-weapon poses,
one frame every 1/30 s). Drawn with the game's sprites on the Scrapyard render around the Nest: the King (armor_unique_the_scrap_king) with the sledgehammer, Spool (the Barbwire Coat) with the bat, fists riding and
turning with each swing, the free hand, sparks where the weapons meet, a screen shake, and captions for what the waves are doing offscreen. ponytail: the kneel is drawn as a lowered, tilted body (the unique suits have no pose strips);
the effects follow CombatFxView's recipes loosely; the waves are only a caption (you are fighting them).
  python3 Tools/render/crown_duel.py duel.json out_dir  -> crown_duel.gif, crown_duel_sheet.png"""
import json, math, os, random, sys
from PIL import Image, ImageDraw, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(R, "Assets/Resources/Sprites"); FONTS = os.path.join(R, "Assets/Resources/Fonts")
d = json.load(open(sys.argv[1])); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
F = d["frames"]; ox, oy = d["origin"]
NX, NY = 186, 108                                       # the Scrap King's Nest on the Scrapyard map (TierMaps.cs)
Z = 5; X0, Y0, W, H = NX - 2.5, NY - 3.0, 9.0, 5.0
world = Image.open(os.path.join(R, "Tools/render/_render/scrapyard.png")).convert("RGBA")
BG = world.crop((int(X0 * 16), int(Y0 * 16), int((X0 + W) * 16), int((Y0 + H) * 16))).resize((int(W * 16 * Z), int(H * 16 * Z)), Image.NEAREST)
def hexc(h, a=255): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, a)
def font(n, s): return ImageFont.truetype(os.path.join(FONTS, n), s)
CAP, SMALL, TITLE = font("PixelifySans-SemiBold.ttf", 28), font("PixelifySans-SemiBold.ttf", 20), font("Silkscreen-Bold.ttf", 26)
def cell(name): return Image.open(os.path.join(SPR, name)).convert("RGBA").crop((0, 0, 16, 16))
KB, SB = cell("armor_unique_the_scrap_king.png"), cell("armor_unique_barbwire_coat.png")
HAMMER = Image.open(os.path.join(SPR, "Weapons/weapon_Sledgehammer.png")).convert("RGBA"); BAT = Image.open(os.path.join(SPR, "Weapons/weapon_BarbedWireBat.png")).convert("RGBA")
def fx(n, k=3): im = Image.open(os.path.join(SPR, f"Fx/{n}.png")).convert("RGBA"); w = im.width // k; return [im.crop((i * w, 0, i * w + w, im.height)) for i in range(k)]
SPARK = fx("fx_hit_spark")
def glove(cuff, gl, big=True):
    g = hexc(gl); lit = tuple(min(255, int(v * 1.45)) for v in g[:3]) + (255,); dark = tuple(int(v * 0.65) for v in g[:3]) + (255,)
    rows = [".KKK.", "KcllK", "KcgdK", ".KKK."] if big else [".KK.", "KcgK", ".KK."]
    img = Image.new("RGBA", (len(rows[0]), len(rows))); col = {"K": hexc("#141617"), "c": hexc(cuff), "l": lit, "g": g, "d": dark}
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img.resize((img.width * Z, img.height * Z), Image.NEAREST)
KG, KS, SG, SS = glove(*d["ck"]), glove(*d["ck"], False), glove(*d["cs"]), glove(*d["cs"], False)
def tint(img, col, k):
    im = img.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a: px[x, y] = (int(r + (col[0] - r) * k), int(g + (col[1] - g) * k), int(b + (col[2] - b) * k), a)
    return im

def unit(c, bod, weapon, g, fr, fist, free, tx, ty, flash=0.0, kneel=0.0):
    gx, gy, deg, hx, hy, sx, sy, sup, left, lean, hdeg, length = fr[:12]
    L = Image.new("RGBA", (64 * Z, 64 * Z)); o = 24 * Z
    b = tint(bod, (255, 90, 80), flash) if flash > 0 else bod
    L.alpha_composite(b.resize((16 * Z, 16 * Z), Image.NEAREST), (o, o + int(kneel * 3 * Z)))
    s = weapon.transpose(Image.FLIP_TOP_BOTTOM) if left else weapon
    if length < 0.99: s = s.resize((max(1, round(s.width * length)), s.height), Image.NEAREST)
    s = s.resize((s.width * Z // 2, s.height * Z // 2), Image.NEAREST)
    pvx, pvy = g[0] * length * Z / 2, ((weapon.height - g[1]) if left else g[1]) * Z / 2
    big = Image.new("RGBA", (64 * Z, 64 * Z)); big.alpha_composite(s, (int(32 * Z - pvx), int(32 * Z - pvy)))
    big = big.rotate(deg, resample=Image.NEAREST, center=(32 * Z, 32 * Z))
    ky = int(kneel * 3 * Z)
    L.alpha_composite(big, (int(o + gx * Z - 32 * Z), int(o + gy * Z - 32 * Z + ky)))
    fz = (fist.transpose(Image.FLIP_LEFT_RIGHT) if left else fist).rotate(hdeg, expand=True, resample=Image.NEAREST)
    L.alpha_composite(fz, (int(o + hx * Z - fz.width / 2), int(o + hy * Z - fz.height / 2 + ky)))
    if sup:
        fr_ = free.transpose(Image.FLIP_LEFT_RIGHT) if not left else free
        L.alpha_composite(fr_, (int(o + sx * Z - fr_.width / 2), int(o + sy * Z - fr_.height / 2 + ky)))
    L = L.rotate(lean - 9 * kneel * (1 if not left else -1), center=(o + 8 * Z, o + 8 * Z), resample=Image.NEAREST)
    X, Y = (tx - ox + NX - X0) * 16 * Z, (ty - oy + NY - Y0) * 16 * Z
    ImageDraw.Draw(c).ellipse((X - 6 * Z, Y + 5 * Z, X + 6 * Z, Y + 9 * Z), fill=(30, 26, 22, 120))
    c.alpha_composite(L, (int(X - 8 * Z - o), int(Y - 8 * Z - o)))

parts = []
def burst(x, y, col, n, sp, life, t, seed):
    r = random.Random(seed)
    for _ in range(n):
        a = r.random() * math.tau; v = sp * r.uniform(0.5, 1.2)
        parts.append([x, y, math.cos(a) * v, math.sin(a) * v - 40, t, life * r.uniform(0.7, 1.2), col, None, 1])
def puff(x, y, frs, col, life, scale, t): parts.append([x, y, 0, 0, t, life, col, frs, scale])
shake = [-9, 0]; hurt = {}; stopped_until = [-1]
FIN = d["finish"]
def caption(t):
    if t < 2.0: return "Spool: \"He is mine. You keep the rest off my back.\""
    if t < FIN - 0.1:
        w = 1 if t < 7 else 2 if t < 13 else 3
        return f"WAVE {w} OF 3 - you hold the Raiders off-screen; Spool duels the King"
    if t < FIN + 2.2: return "Last wave dead: Spool's bat comes round. The King kneels."
    return "E at the Nest: take the crown. He is exiled for good."
def waves_bar(t):
    return 0 if t < 7 else 1 if t < 13 else 2 if t < FIN - 0.1 else 3

def draw(i):
    f = F[i]; t = f["t"]
    c = BG.copy(); dd = ImageDraw.Draw(c)
    kx, ky, kfr = f["k"][0], f["k"][1], f["k"][2]; sx, sy, sfr = f["s"][0], f["s"][1], f["s"][2]
    P = lambda x, y: ((x - ox + NX - X0) * 16 * Z, (y - oy + NY - Y0) * 16 * Z)
    (KX, KY), (SX, SY) = P(kx, ky), P(sx, sy)
    for e in f["ev"]:
        if e[0] == "clash":
            mx, my = (KX + SX) / 2, (KY + SY) / 2 - 7 * Z
            puff(mx, my, SPARK, (255, 236, 180), 0.3, 3.2, t); burst(mx, my, (255, 236, 180), 20, 240, 0.4, t, i)
            shake[:] = [t, 0.10]; hurt["s" if e[1] else "k"] = t
        elif e[0] == "finish":
            puff(KX, KY - 8 * Z, SPARK, (255, 255, 255), 0.5, 5.0, t); burst(KX, KY - 8 * Z, (255, 225, 160), 40, 320, 0.6, t, i + 7)
            shake[:] = [t, 0.3]; hurt["k"] = t; stopped_until[0] = i + 9
    kneel = 0.0 if t < FIN else min(1.0, (t - FIN) / 0.4)
    def drawk(): unit(c, KB, HAMMER, d["gk"], kfr, KG, KS, kx, ky, max(0, 1 - (t - hurt.get("k", -9)) / 0.15) * 0.7, kneel)
    def draws(): unit(c, SB, BAT, d["gs"], sfr, SG, SS, sx, sy, max(0, 1 - (t - hurt.get("s", -9)) / 0.15) * 0.7)
    for _, fn in sorted([(ky, drawk), (sy, draws)], key=lambda r: r[0]): fn()
    keep = []
    for pr in parts:
        x, y, vx, vy, born, life, col, frs, scale = pr; age = t - born
        if age < 0 or age > life: continue
        keep.append(pr); k = age / life; a = int(255 * (1 - k))
        if frs:
            fr_ = frs[min(len(frs) - 1, int(k * len(frs)))]
            im = tint(fr_.resize((max(1, int(fr_.width * Z * scale / 2)), max(1, int(fr_.height * Z * scale / 2))), Image.NEAREST), col, 1.0)
            im.putalpha(im.getchannel("A").point(lambda v: v * a // 255)); c.alpha_composite(im, (int(x - im.width / 2), int(y - im.height / 2)))
        else:
            xx, yy = x + vx * age, y + vy * age + 300 * age * age; dd.rectangle((xx, yy, xx + Z / 2, yy + Z / 2), fill=col + (a,))
    parts[:] = keep
    amp = shake[1] * max(0, 1 - (t - shake[0]) / 0.15) * 16 * Z
    if amp > 0.5:
        r = random.Random(i); c = c.transform(c.size, Image.AFFINE, (1, 0, r.uniform(-amp, amp), 0, 1, r.uniform(-amp, amp)), Image.NEAREST)
    dd = ImageDraw.Draw(c)
    dd.text((16, 10), "SPOOL AND THE SCRAP KING", font=TITLE, fill=(224, 192, 96)); dd.text((16, 44), "HOLD OFF THE WAVES", font=SMALL, fill=(232, 220, 192))
    for nm, X, Y, col in (("THE SCRAP KING", KX, KY, (232, 190, 120)), ("SPOOL", SX, SY, (190, 210, 230))):
        w = dd.textlength(nm, font=SMALL); dd.text((X - w / 2, Y - 17 * Z), nm, font=SMALL, fill=col)
    wb = waves_bar(t)
    for n in range(3):
        x = c.width - 40 - (2 - n) * 34; dd.rectangle((x, 14, x + 26, 28), fill=(184, 48, 42) if n < wb else (60, 52, 48)); dd.rectangle((x, 14, x + 26, 28), outline=(20, 18, 18))
    dd.text((c.width - 190, 36), "WAVES CLEARED", font=SMALL, fill=(200, 200, 200))
    dd.text((16, c.height - 44), caption(t), font=CAP, fill=(255, 225, 140) if t >= FIN - 0.1 else (232, 220, 192))
    dd.text((c.width - 90, c.height - 36), f"{t:4.1f} s", font=SMALL, fill=(200, 200, 200))
    return c

frames = []; sheet = []; hold = {}
want = {0}
for i, f in enumerate(F):
    for e in f["ev"]:
        if e[0] == "clash" and len([w for w in want if w > 30]) < 3: want.add(i - 8); want.add(i + 2)      # a wind-up, and the hit
        if e[0] == "finish": want.update({i - 3, i + 4, i + 40})
for i in range(len(F)):
    if stopped_until[0] > i and i % 1 == 0: pass
    im = draw(i).convert("RGB"); im = im.resize((im.width * 2 // 2, im.height * 2 // 2), Image.NEAREST)
    if i % 3 == 0: frames.append(im.resize((im.width * 5 // 9, im.height * 5 // 9), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE, colors=96))
    if i in want: sheet.append(im)
frames[0].save(os.path.join(out, "crown_duel.gif"), save_all=True, append_images=frames[1:], duration=100, loop=0, optimize=True)
cw, ch = sheet[0].width // 2, sheet[0].height // 2; cols = 2; rows = (len(sheet) + 1) // 2
s = Image.new("RGB", (cols * cw + 24, rows * ch + 8 * (rows + 1)), (24, 26, 28))
for n, im in enumerate(sheet): s.paste(im.resize((cw, ch), Image.LANCZOS), (8 + (n % 2) * (cw + 8), 8 + (n // 2) * (ch + 8)))
s.save(os.path.join(out, "crown_duel_sheet.png")); print(len(F), "frames,", len(sheet), "in the sheet ->", out)
