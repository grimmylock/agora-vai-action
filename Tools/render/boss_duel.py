"""Boss 1b (Yan: "try rendering the fight"): the Visor's duel from the harness (`dotnet /tmp/test.dll bossframes duel.json` - a bot
playing the fight through the real Boss / Parry / Melee / Rt code, one frame every 1/30 s of REAL time, so hit-stops, the perfect
parry's slow motion and bullet time play at the speed you'd see them). Drawn with the game's sprites: both bodies (his from
armor_unique_black_visor_suit.png), the hatchet and his knife turned round their grip in the floating hand (HeldPoseMath frames),
the effects CombatFxView plays for each event (glints, parry sparks, stagger, the phase 2 aura, the finisher, the smoke), the
perfect parry's lean-in and cold edge, a screen shake, and the boss HUD (HudView.Boss.cs: name card, health bar with the draining
chunk, STAGGERED / EXECUTE) in the game's fonts. ponytail: the HUD is drawn to match HudView's layout and colours, not taken from Unity.
  python3 Tools/render/boss_duel.py duel.json out_dir  -> boss_duel.mp4, boss_duel_sheet.png"""
import json, math, os, random, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(R, "Assets/Resources/Sprites")
FONTS = os.path.join(R, "Assets/Resources/Fonts")
d = json.load(open(sys.argv[1])); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
frames = d["frames"]; ox, oy = d["origin"]; WHICH = d.get("which", "visor")
Z = 3                                             # canvas px per character px
X0, Y0, W, H = ox - 5.5, oy - 4.4, 14.0, 7.9      # the view, tiles
OUT_W, OUT_H = int(W * 16 * Z * 2), int(H * 16 * Z * 2)
world = Image.open(os.path.join(R, "Tools/render/_render/%s.png" % d.get("world", "perimeter"))).convert("RGBA")
# Yan: "they went out of the screen during the duel": the view follows the fight, as the game's camera follows you - eased
# toward the middle of the two of them (CAM_EASE a frame), the ground cropped from the world render under it each frame
CAM_EASE = 0.08
def ground(x0, y0):
    return world.crop((int(round(x0 * 16)), int(round(y0 * 16)), int(round(x0 * 16)) + int(W * 16), int(round(y0 * 16)) + int(H * 16))).resize((int(W * 16 * Z), int(H * 16 * Z)), Image.NEAREST)
BG = ground(X0, Y0)

def hexc(h, a=255): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, a)
def font(n, s): return ImageFont.truetype(os.path.join(FONTS, n), s)
TITLE, BODY, SMALL = font("Silkscreen-Bold.ttf", 64), font("PixelifySans-SemiBold.ttf", 26), font("PixelifySans-SemiBold.ttf", 20)
NAME = font("Silkscreen-Bold.ttf", 24)
CREAM, YELLOW, RED = hexc("#e8dcc0"), hexc("#e0c060"), hexc("#b8302a")

atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA")
walkA, walkB = Image.open(os.path.join(SPR, "armor_walk_a.png")).convert("RGBA"), Image.open(os.path.join(SPR, "armor_walk_b.png")).convert("RGBA")
ai = max(0, d["armor"]); pbody = atlas.crop(((ai % 16) * 16, (ai // 16) * 16, (ai % 16) * 16 + 16, (ai // 16) * 16 + 16))
vstrip = Image.open(os.path.join(SPR, "armor_unique_barbwire_coat.png" if WHICH == "spool" else "armor_unique_black_visor_suit.png")).convert("RGBA")
vcell = [vstrip.crop((i * 16, 0, i * 16 + 16, 16)) for i in range(6)]
# Melee 3: the swing's body frames - his from the unique strip (front row), yours from armor_pose_<key>.png
PKEYS = ["crouch", "crouch_h", "kneel", "kneel_h", "lean_h_l", "lean_h_r", "lean_l", "lean_r", "lunge_h_l", "lunge_h_r", "lunge_l", "lunge_r", "lying", "sit_h_l", "sit_h_r", "sit_l", "sit_r", "stagger_h_l", "stagger_h_r", "stagger_l", "stagger_r"]
vposes = Image.open(os.path.join(SPR, "armor_unique_black_visor_suit_poses.png")).convert("RGBA") if WHICH != "spool" else None
def vpose(key, base): return vposes.crop((PKEYS.index(key) * 16, 0, PKEYS.index(key) * 16 + 16, 16)) if key and vposes is not None else base
PP = {}
def ppose(key, base):
    if not key: return base
    if key not in PP: PP[key] = Image.open(os.path.join(SPR, f"armor_pose_{key}.png")).convert("RGBA").crop(((ai % 16) * 16, (ai // 16) * 16, (ai % 16) * 16 + 16, (ai // 16) * 16 + 16))
    return PP[key]
import re
_hw = open(os.path.join(R, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
def _tab(name, key, default):
    m = re.search(r'\["%s"\] = ([0-9.]+)f' % key, _hw.split(name + " = new")[1].split("};")[0]); return float(m.group(1)) if m else default
def weapon_img(key):
    """2026-11-23: the weapon as the game now draws it in the hand: the inventory picture shrunk by Dense[key] when it has one (else the mini), times HandScale[key]; returns the picture at the old in-hand scale and the scale to apply to the grip."""
    k, hs = _tab("Dense", key, 1.0), _tab("HandScale", key, 1.0)
    if k > 1.0: im = Image.open(os.path.join(SPR, f"Weapons/weapon_{key}_hd.png")).convert("RGBA"); return im.resize((max(1, round(im.width / k * hs)), max(1, round(im.height / k * hs))), Image.LANCZOS), hs
    im = Image.open(os.path.join(SPR, f"Weapons/weapon_{key}.png")).convert("RGBA"); return im.resize((max(1, round(im.width * hs)), max(1, round(im.height * hs))), Image.NEAREST), hs
hatchet, _hsp = weapon_img("TacticalHatchet"); d["gh"] = [v * _hsp for v in d["gh"]]
knife, _hsb = weapon_img(d.get("bkey", "SerratedTrenchKnife")); d["gk"] = [v * _hsb for v in d["gk"]]
def fx(n, k=3): im = Image.open(os.path.join(SPR, f"Fx/{n}.png")).convert("RGBA"); w = im.width // k; return [im.crop((i * w, 0, i * w + w, im.height)) for i in range(k)]
SPARK, PSI, BLOOD, PUFF = fx("fx_hit_spark"), fx("fx_hit_psi"), fx("fx_hit_blood"), fx("fx_muzzle_puff")
POOL = fx("fx_blood_pool", 5)

def glove(cuff, gl):                              # HeldWeaponView's glove (5x4, design B), character px
    g = hexc(gl); lit = tuple(min(255, int(v * 1.45)) for v in g[:3]) + (255,); dark = tuple(int(v * 0.65) for v in g[:3]) + (255,)
    img = Image.new("RGBA", (5, 4)); col = {"K": hexc("#141617"), "c": hexc(cuff), "l": lit, "g": g, "d": dark}
    for y, row in enumerate([".KKK.", "KcllK", "KcgdK", ".KKK."]):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img.resize((5 * Z, 4 * Z), Image.NEAREST)
PGLOVE, VGLOVE = glove(d["cuff"], d["glove"]), glove("#1f2226", "#2a2c30")

def tint(img, col, k):
    """Blend toward a colour (the view's hit flash)."""
    im = img.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a: px[x, y] = (int(r + (col[0] - r) * k), int(g + (col[1] - g) * k), int(b + (col[2] - b) * k), a)
    return im

def place_weapon(layer, sprite, g, fr):
    gx, gy, deg, left = fr[0], fr[1], fr[2], fr[8]
    s = sprite.transpose(Image.FLIP_TOP_BOTTOM) if left else sprite
    s = s.resize((s.width * Z // 2, s.height * Z // 2), Image.NEAREST)
    px, py = g[0] * Z / 2, ((sprite.height - g[1]) if left else g[1]) * Z / 2
    big = Image.new("RGBA", (240, 240)); big.alpha_composite(s, (int(120 - px), int(120 - py)))
    big = big.rotate(deg, resample=Image.NEAREST, center=(120, 120))
    cx, cy = 72 + Z * (gx - 8), 72 + Z * (gy - 8)
    layer.alpha_composite(big, (int(round(cx - 120)), int(round(cy - 120))))

def smear_img(sprite, col, a):
    """Melee 4: the smear's ghost of the blade - tinted, see-through."""
    im = tint(sprite, col, 0.75); im.putalpha(im.getchannel("A").point(lambda v: v * a // 255)); return im
def unit(canvas, body, weapon, g, fr, glv, tx, ty, pose=(0, 0, 1, 1, 1), flash=0.0, jitter=0, smear=(), red=False):
    lift, tilt, sx, sy, shadow = pose
    gx, gy = (tx - X0) * 16 * Z + jitter, (ty - Y0) * 16 * Z
    sw, sh = 12 * Z * shadow, 4 * Z * shadow; shy = gy + 7 * Z
    ImageDraw.Draw(canvas).ellipse((gx - sw / 2, shy - sh / 2, gx + sw / 2, shy + sh / 2), fill=(30, 26, 22, 120))
    layer = Image.new("RGBA", (144, 144))
    b = tint(body, (255, 90, 80), flash) if flash > 0 else body
    layer.alpha_composite(b.resize((16 * Z, 16 * Z), Image.NEAREST), (72 - 8 * Z, 72 - 8 * Z))
    for k, sm in enumerate(smear or []):                                                         # the strike's smear, oldest faintest
        sw = weapon if sm[3] > 0.99 else weapon.resize((max(1, round(weapon.width * sm[3])), weapon.height), Image.NEAREST)
        place_weapon(layer, smear_img(sw, (255, 64, 46) if red else (255, 248, 225), int(150 * (1 - k / 4))), (g[0] * sm[3], g[1]), [sm[0], sm[1], sm[2]] + list(fr[3:]))
    if weapon is not None:
        if len(fr) > 11 and fr[11] < 0.99: weapon = weapon.resize((max(1, round(weapon.width * fr[11])), weapon.height), Image.NEAREST); g = (g[0] * fr[11], g[1])   # a sweep foreshortens
        place_weapon(layer, weapon, g, fr)
        gl = glv.transpose(Image.FLIP_LEFT_RIGHT) if fr[8] else glv
        if len(fr) > 10: gl = gl.rotate(fr[10], expand=True, resample=Image.NEAREST)          # the fist turns with the swing
        layer.alpha_composite(gl, (int(round(72 + Z * (fr[3] - 8) - gl.width / 2)), int(round(72 + Z * (fr[4] - 8) - gl.height / 2))))
        if fr[7]:                                                                                 # the free hand (guard, or on a two-handed handle)
            sg = glv.resize((4 * Z, 3 * Z), Image.NEAREST); sg = sg if fr[8] else sg.transpose(Image.FLIP_LEFT_RIGHT)
            layer.alpha_composite(sg, (int(round(72 + Z * (fr[5] - 8) - sg.width / 2)), int(round(72 + Z * (fr[6] - 8) - sg.height / 2))))
    if len(fr) > 9: tilt = tilt + fr[9]                                                          # the body leans with the swing
    if sx != 1 or sy != 1: layer = layer.resize((max(1, round(144 * sx)), max(1, round(144 * sy))), Image.NEAREST)
    rot = layer.rotate(tilt, resample=Image.NEAREST, expand=True)
    canvas.alpha_composite(rot, (int(round(gx - rot.width / 2)), int(round(gy - lift * 16 * Z - rot.height / 2))))

# ---- particles (the CombatFxView recipes, simplified) ----
parts = []   # [x, y, vx, vy, born, life, colour, size, gravity, sprite frames or None, scale]
def burst(x, y, col, n, speed, life, size=1, t=0.0, seed=0):
    r = random.Random(seed)
    for _ in range(n):
        a = r.random() * math.tau; sp = speed * r.uniform(0.5, 1.2)
        parts.append([x, y, math.cos(a) * sp, math.sin(a) * sp - 0.6, t, life * r.uniform(0.7, 1.2), col, size, 6.0, None, 1, X0, Y0])
def puff(x, y, frames_, col, life, rise=0.0, scale=1.0, t=0.0):
    parts.append([x, y, 0, -rise, t, life, col, 1, 0.0, frames_, scale, X0, Y0])

WHITE, REDG, STEEL = (255, 255, 235), (255, 46, 36), (255, 236, 180)
shake_at, shake_amp = -9.0, 0.0
def shake(t, amp):
    global shake_at, shake_amp
    if amp >= shake_amp or t - shake_at > 0.15: shake_at, shake_amp = t, amp

hurt = {}            # who -> real time of the last hit (the red flash)
last_xy = {}; strides = {}   # for the walking feet
dead_at = None; ghost = 1.0; prev_b = None; walk_t = {"p": 0.0, "b": 0.0}; last_pos = {}
def draw(i):
    global dead_at, ghost, prev_b
    f = frames[i]; t = f["rt"]
    global X0, Y0
    mx_, my_ = (f["p"][0] + f["b"][0]) / 2, (f["p"][1] + f["b"][1]) / 2
    tx0, ty0 = mx_ - W / 2, my_ - H / 2 - 0.4
    X0 += (tx0 - X0) * CAM_EASE; Y0 += (ty0 - Y0) * CAM_EASE
    c = ground(X0, Y0); dd = ImageDraw.Draw(c)
    px_, py_, php, focus = f["p"]; bx, by, bhp, phase, broken, finished, gone, stag, danger = f["b"][:9]
    hx, hy = (bx - X0) * 16 * Z, (by - Y0) * 16 * Z
    pxs, pys = (px_ - X0) * 16 * Z, (py_ - Y0) * 16 * Z
    for e in f["ev"]:                                       # this frame's events -> effects
        kind, by_boss, psi, hit = e
        if kind == "glint":
            col = REDG if psi else WHITE
            puff(hx, hy - 0.75 * 16 * Z, SPARK, col, 0.32, 0, 2.6, t); burst(hx, hy - 0.75 * 16 * Z, col, 7, 70, 0.25, 2, t, i)
        elif kind == "parry":
            mx, my = (pxs + hx) / 2, (pys + hy) / 2 - 0.45 * 16 * Z
            puff(mx, my, SPARK, STEEL, 0.3 if hit else 0.15, 0, 3.6 if hit else 2.0, t)
            burst(mx, my, STEEL, 24 if hit else 8, 160 if hit else 90, 0.5 if hit else 0.3, 2, t, i)
            if hit: burst(mx, my, (255, 255, 255), 10, 230, 0.35, 3, t, i + 1)
            shake(t, 0.22 if hit else 0.07)
        elif kind == "stagger":
            burst(hx, hy - 0.75 * 16 * Z, STEEL, 12, 110, 0.4, 2, t, i); shake(t, 0.1)
        elif kind == "aura":
            for k in range(10): puff(hx + random.Random(i * 10 + k).uniform(-25, 25), hy - random.Random(k).uniform(0, 45), PSI, REDG, 0.6, 70, 2.2, t)
            burst(hx, hy - 20, REDG, 26, 200, 0.6, 3, t, i); shake(t, 0.3)
        elif kind == "finisher":
            for k in range(5): puff(hx + random.Random(k).uniform(-15, 15), hy - random.Random(k + 9).uniform(5, 35), BLOOD, (180, 30, 40), 0.45, 20, 2.6, t)
            burst(hx, hy - 25, (150, 15, 20), 30, 180, 0.7, 3, t, i); shake(t, 0.35)
        elif kind == "smoke":
            r = random.Random(i)
            for k in range(14): puff(hx + r.uniform(-40, 40), hy + r.uniform(-40, 10), PUFF, (184, 188, 184), r.uniform(1.4, 2.4), r.uniform(10, 30), r.uniform(5, 8), t)
        elif kind == "impact" and hit:
            tx_, ty_ = (pxs, pys) if by_boss else (hx, hy)
            puff(tx_, ty_ - 15, BLOOD, (180, 30, 40), 0.2, 0, 2.2, t); hurt["p" if by_boss else "b"] = t
        elif kind == "dodge":
            puff(pxs, pys, PUFF, (150, 140, 115), 0.4, 15, 4, t)
    # the boss's embers in phase 2
    if phase == 2 and not broken and not finished and i % 3 == 0:
        r = random.Random(i); puff(hx + r.uniform(-18, 18), hy - r.uniform(-5, 30), PSI, (255, 52, 30), 0.55, 55, 1.6, t)
    # bodies, y-sorted
    pmoved = (round(px_, 2), round(py_, 2)) != last_pos.get("p"); bmoved = (round(bx, 2), round(by, 2)) != last_pos.get("b")
    last_pos["p"], last_pos["b"] = (round(px_, 2), round(py_, 2)), (round(bx, 2), round(by, 2))
    # 2026-11-23: the walking life as the game now draws it: the feet at UnitMotionView.AnimFactor's cadence (half a foot cycle a tile walking, 0.59 running), a 1 px hop on the foot-down frames, a 2-3 degree lean toward the sideways travel
    def stride_of(key, x, y):
        lx, ly = last_xy.get(key, (x, y)); dist_ = math.hypot(x - lx, y - ly); last_xy[key] = (x, y); spd = dist_ * 30
        if dist_ > 0.002: strides[key] = strides.get(key, 0.0) + dist_ * (0.5 + 0.09 * max(0.0, min(1.0, spd - 3.0)))
        return dist_ > 0.002, int(strides.get(key, 0.0) * 4) % 4, spd, x - lx
    pmv, pq, pspd, pdx = stride_of("p", px_, py_); bmv, bq, bspd, bdx = stride_of("b", bx, by)
    def drawp():
        pose = list(f["pose"]); fl = max(0.0, 1 - (t - hurt.get("p", -9)) / 0.15) * 0.7
        key_ = f["pf"][12] if len(f["pf"]) > 12 else ""
        base = pbody
        if pmv and not key_ and pq in (0, 2):                       # a foot down: the walk frame and the hop
            wa_ = (walkA if pq == 0 else walkB).crop(((ai % 16) * 16, (ai // 16) * 16, (ai % 16) * 16 + 16, (ai // 16) * 16 + 16)); base = wa_; pose[0] += 1 / 16
        if pmv and abs(pdx) > 0.002: pose[1] += -(2 + max(0.0, min(1.0, pspd - 3.4))) * (1 if pdx > 0 else -1)
        unit(c, ppose(key_, base), hatchet, d["gh"], f["pf"], PGLOVE, px_, py_, tuple(pose), fl, smear=f.get("ps"))
    def drawb():
        global dead_at
        if gone: return
        if finished:
            if dead_at is None: dead_at = t
            age = min(1.0, (t - dead_at) / 0.6); pool = POOL[min(4, int(age * 5))].resize((16 * Z, 12 * Z), Image.NEAREST)
            c.alpha_composite(pool, (int(hx - pool.width / 2), int(hy + 2 * Z - pool.height / 2)))
            lying = vcell[0].resize((16 * Z, 16 * Z), Image.NEAREST).rotate(90 if px_ > bx else -90, resample=Image.NEAREST, expand=True)
            c.alpha_composite(lying, (int(hx - lying.width / 2), int(hy + 3 * Z - lying.height / 2))); return
        cell = vpose(f["bf"][12] if len(f["bf"]) > 12 else "", vcell[(1 if bq == 0 else 2 if bq == 2 else 0) if bmv else 0])
        fl = max(0.0, 1 - (t - hurt.get("b", -9)) / 0.15) * 0.7
        if broken: unit(c, cell, knife, d["gk"], f["bf"], VGLOVE, bx, by + 0.12, (0, -8, 1, 0.9, 1), fl)   # on one knee
        else: unit(c, cell, knife, d["gk"], f["bf"], VGLOVE, bx, by, (0, 0, 1, 1, 1), max(fl, 0.25 if stag else 0), jitter=(Z if stag and i % 2 else 0), smear=f.get("bs"), red=f.get("red"))
    for _, fn in sorted([(py_, drawp), (by, drawb)], key=lambda r: r[0]): fn()
    # particles
    keep = []
    for pr in parts:
        x, y, vx, vy, born, life, col, size, grav, frs, scale, cx0, cy0 = pr
        x += (cx0 - X0) * 16 * Z; y += (cy0 - Y0) * 16 * Z                 # world-fixed: shift by how far the camera moved
        age = t - born
        if age < 0 or age > life: continue
        keep.append(pr)
        k = age / life; a = int(255 * (1 - k))
        if frs:
            fr_ = frs[min(len(frs) - 1, int(k * len(frs)))]; sz = max(1, int(fr_.width * Z * scale / 2))
            im = tint(fr_.resize((sz, max(1, int(fr_.height * Z * scale / 2))), Image.NEAREST), col, 1.0)
            im.putalpha(im.getchannel("A").point(lambda v: v * a // 255))
            c.alpha_composite(im, (int(x - im.width / 2), int(y + vy * age - im.height / 2)))
        else:
            xx, yy = x + vx * age, y + vy * age + 0.5 * grav * 60 * age * age
            dd.rectangle((xx, yy, xx + size * Z / 2, yy + size * Z / 2), fill=col + (a,))
    parts[:] = keep
    # camera: the perfect parry's lean-in, the shake
    punch = f["punch"]
    if punch > 0.01:
        k = 1 - 0.09 * punch; cw, ch = c.width * k, c.height * k; cx0, cy0 = (c.width - cw) / 2, (c.height - ch) / 2
        c = c.crop((int(cx0), int(cy0), int(cx0 + cw), int(cy0 + ch))).resize((int(W * 16 * Z), int(H * 16 * Z)), Image.NEAREST)
    st = t - shake_at; amp = shake_amp * max(0.0, 1 - st / 0.15) * 16 * Z
    big = c.resize((OUT_W, OUT_H), Image.NEAREST)
    if amp > 0.5:
        r = random.Random(i); big = big.transform(big.size, Image.AFFINE, (1, 0, r.uniform(-amp, amp) * 2, 0, 1, r.uniform(-amp, amp) * 2), Image.NEAREST)
    hud(big, f, punch)
    return big

# the cold edge (HudView.Boss.cs's vignette)
VIG = Image.new("L", (OUT_W // 8, OUT_H // 8))
for y in range(VIG.height):
    for x in range(VIG.width):
        dx, dy = (x + .5) / VIG.width * 2 - 1, (y + .5) / VIG.height * 2 - 1; r = math.sqrt(dx * dx * .8 + dy * dy * .8)
        v = min(1, max(0, (r - .74) / (1.18 - .74))); VIG.putpixel((x, y), int(255 * v * v * (3 - 2 * v)))
VIG = VIG.resize((OUT_W, OUT_H), Image.BILINEAR)

def hud(img, f, punch):
    global ghost
    dd = ImageDraw.Draw(img)
    if punch > 0.01:
        edge = Image.new("RGBA", img.size, (184, 230, 255, 0)); edge.putalpha(VIG.point(lambda v: int(v * 0.6 * punch)))
        img.alpha_composite(edge)
    bx, by, bhp, phase, broken, finished, gone, stag, danger = f["b"][:9]
    show = not gone or (f["rt"] - getattr(hud, "gone_at", f["rt"])) < 3
    if gone and not hasattr(hud, "gone_at"): hud.gone_at = f["rt"]
    if show:
        hp = 0 if broken or finished else max(0, bhp / d["maxhp"]); ghost = max(hp, ghost - 0.35 / 30)
        BW = 560 * 2; x0 = (img.width - BW) // 2; y0 = 54 * 2 - 28
        dd.rectangle((x0, y0, x0 + BW, y0 + 28), fill=(20, 22, 23, 255)); dd.rectangle((x0 + 4, y0 + 4, x0 + BW - 4, y0 + 24), fill=(20, 18, 18, 255))
        dd.rectangle((x0 + 4, y0 + 4, x0 + 4 + (BW - 8) * ghost, y0 + 24), fill=(242, 217, 153, 255))
        fill = RED if not stag else (255, 200, 190, 255) if int(f["rt"] * 30) % 2 else RED
        if phase == 2 and not stag: fill = (int(184 + 71 * (0.5 + 0.5 * math.sin(f["rt"] * 6))), 64, 48, 255)
        if hp > 0: dd.rectangle((x0 + 4, y0 + 4, x0 + 4 + (BW - 8) * hp, y0 + 24), fill=fill)
        name = ("SPOOL" if WHICH == "spool" else "THE VISOR") + ("  II" if phase == 2 else "")
        dd.text((img.width // 2, y0 - 22), name, font=NAME, fill=CREAM, anchor="mm")
        gd = f["b"][9] if len(f["b"]) > 9 else 0
        opn = f["b"][10] if len(f["b"]) > 10 else 0
        state = "DEFEATED" if finished else "EXECUTE - E" if broken else "STAGGERED" if stag else "OPEN" if opn else f"GUARD {gd}/3" if gd else ""
        if state: dd.text((img.width // 2, y0 + 50), state, font=NAME, fill=(255, 90, 76, 255) if broken else YELLOW, anchor="mm")
    title, sub, age = f["splash"]
    if 0 <= age < 3.2 and title:
        a = min(1, age / 0.15) * min(1, (3.2 - age) / 0.6)
        lay = Image.new("RGBA", img.size); ld = ImageDraw.Draw(lay)
        sp = 6 + 10 * min(1, age / 3.2); txt = (" " * 0).join(title)
        f_ = font("Silkscreen-Bold.ttf", int(64 * (1 + 0.06 * age)))
        ld.text((img.width // 2, img.height // 2 - 120), title, font=f_, fill=CREAM[:3] + (int(255 * a),), anchor="mm", stroke_width=4, stroke_fill=(0, 0, 0, int(255 * a)))
        ld.text((img.width // 2, img.height // 2 - 40), sub, font=BODY, fill=YELLOW[:3] + (int(255 * a),), anchor="mm", stroke_width=2, stroke_fill=(0, 0, 0, int(200 * a)))
        img.alpha_composite(lay)
    # the player's corner: health and Focus
    php, focus = f["p"][2], f["p"][3]
    x0, y0 = 30, img.height - 70
    dd.text((x0, y0 - 30), "YOU", font=SMALL, fill=CREAM)
    dd.rectangle((x0, y0, x0 + 300, y0 + 14), fill=(20, 22, 23)); dd.rectangle((x0 + 2, y0 + 2, x0 + 2 + 296 * php / 1000, y0 + 12), fill=(120, 170, 90))
    dd.rectangle((x0, y0 + 20, x0 + 300, y0 + 30), fill=(20, 22, 23)); dd.rectangle((x0 + 2, y0 + 22, x0 + 2 + 296 * focus / 100, y0 + 28), fill=(98, 192, 224))
    dd.text((x0 + 310, y0 + 18), "FOCUS", font=SMALL, fill=(98, 192, 224))
    if f["slow"]: dd.text((img.width - 30, img.height - 50), "BULLET TIME", font=NAME, fill=(150, 200, 255), anchor="rm")

if __name__ == "__main__":
    # 2026-11-23: an excerpt for the trailer: argv[3], argv[4] = start and end in real seconds (default: 3 s before the first perfect parry, 12 s long); mp4 + gif
    ev_i = [i for i, f in enumerate(frames) for e in f["ev"] if e[0] == "parry" and e[3]]
    t0 = float(sys.argv[3]) if len(sys.argv) > 3 else (frames[ev_i[0]]["rt"] - 3.0 if ev_i else 0.0)
    t1 = float(sys.argv[4]) if len(sys.argv) > 4 else t0 + 12.0
    i0 = next(i for i, f in enumerate(frames) if f["rt"] >= t0); i1 = next((i for i, f in enumerate(frames) if f["rt"] >= t1), len(frames))
    tmp = os.path.join(out, "_frames"); os.makedirs(tmp, exist_ok=True); n = 0
    for i in range(max(0, i0 - 45), i1):                   # 1.5 s of warm-up so the camera and the effects are where they would be
        img = draw(i)
        if i >= i0: img.convert("RGB").save(os.path.join(tmp, f"{n:05d}.png")); n += 1
    name = f"duel_{WHICH}"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "30", "-i", os.path.join(tmp, "%05d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", os.path.join(out, name + ".mp4")], check=True)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "30", "-i", os.path.join(tmp, "%05d.png"), "-vf", "scale=720:-1:flags=neighbor,split[a][b];[a]palettegen=max_colors=128[p];[b][p]paletteuse=dither=none", "-loop", "0", os.path.join(out, name + ".gif")], check=True)
    mid = [Image.open(os.path.join(tmp, f"{k:05d}.png")) for k in (0, n // 4, n // 2, 3 * n // 4)]
    s_ = Image.new("RGB", (OUT_W, OUT_H * 2 // 2)); 
    sheet = Image.new("RGB", (OUT_W // 2 * 2, OUT_H // 2 * 2)); [sheet.paste(m.resize((OUT_W // 2, OUT_H // 2), Image.LANCZOS), ((q % 2) * OUT_W // 2, (q // 2) * OUT_H // 2)) for q, m in enumerate(mid)]; sheet.save(os.path.join(out, name + "_sheet.png"))
    for fn in os.listdir(tmp): os.remove(os.path.join(tmp, fn))
    os.rmdir(tmp); print(n, "frames,", round(frames[i1 - 1]["rt"] - frames[i0]["rt"], 1), "s ->", os.path.join(out, name + ".mp4"), "/ .gif")
