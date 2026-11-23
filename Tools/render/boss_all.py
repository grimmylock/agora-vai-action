"""2026-11-23 (boss_all.py: visor_all.py for the other human bosses; Yan: "do them all in sequence, I'll review everything when done"). The original: (Yan: "show me all of the Visor's moves, including the combos, the stance and everything else"): one animated sheet, in order: stance, walking, guard (the parry), guard broken (stagger), open (recovering), on one knee (broken, waiting for the finisher), then
every move from its wind-up through every blow of its combo to the opening after it, each captioned with its hits and poses. The game's own pose code (`visorstates`, `visormoves` dumps), his real sprites. 30 frames a second of game time.
Usage (boss_all.py, the same for Spool / the Scrap King / the Verger): python3 Tools/render/boss_all.py moves.json DefName out.gif"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, S, BG, big_glove, glove, paste
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
hw = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read(); boss = open(os.path.join(ROOT, "Assets/Scripts/GameCore/Boss.cs")).read()
def tab(n, k):
    m = re.search(r'\["%s"\] = ([0-9.]+)f' % k, hw.split(n + " = new")[1].split("};")[0]); return float(m.group(1)) if m else 1.0
PKEYS = ["crouch", "crouch_h", "kneel", "kneel_h", "lean_h_l", "lean_h_r", "lean_l", "lean_r", "lunge_h_l", "lunge_h_r", "lunge_l", "lunge_r", "lying", "sit_h_l", "sit_h_r", "sit_l", "sit_r", "stagger_h_l", "stagger_h_r", "stagger_l", "stagger_r"]
_d = json.load(open(sys.argv[1])); DEF = sys.argv[2]; _w = _d["weapons"][0]
strip = Image.open(os.path.join(SPR, _d["body"])).convert("RGBA"); cellsb = [strip.crop((i * 16, 0, i * 16 + 16, 16)).resize((16 * S, 16 * S), Image.NEAREST) for i in range(max(1, strip.width // 16))]
_pf = os.path.join(SPR, _d["body"][:-4] + "_poses.png"); poses = Image.open(_pf).convert("RGBA") if os.path.exists(_pf) else None
def posed(k, base): return base if (not k or poses is None) else poses.crop((PKEYS.index(k) * 16, 0, PKEYS.index(k) * 16 + 16, 16)).resize((16 * S, 16 * S), Image.NEAREST)
fist = big_glove(_d["armor"][1], _d["armor"][2]).resize((5 * S, 4 * S), Image.NEAREST); other = glove(_d["armor"][1], _d["armor"][2]).resize((4 * S, 3 * S), Image.NEAREST)
W = 16 * S * 3; ub = 16 * S; font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 17); small = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 14)
KEY = _w["key"]; kd, hs = tab("Dense", KEY), tab("HandScale", KEY)
if kd > 1: im = Image.open(os.path.join(SPR, f"Weapons/weapon_{KEY}_hd.png")).convert("RGBA"); img = im.resize((round(im.width / kd * hs), round(im.height / kd * hs)), Image.LANCZOS)
else: _m = Image.open(os.path.join(SPR, f"Weapons/weapon_{KEY}.png")).convert("RGBA"); img = _m.resize((round(_m.width * hs), round(_m.height * hs)), Image.NEAREST) if hs != 1 else _m
moves = _w; grip = [moves["grip"][0] * hs, moves["grip"][1] * hs]
SPRING = os.environ.get("SPRING") == "1" and KEY == "VergerScythe"; _sp = {"pos": None, "vel": 0.0}
def spring_deg(target, dt=1 / 30.0, w=18.0, z=0.32, mx=75.0):       # WeaponSpring.Step, the same numbers
    if _sp["pos"] is None or abs(target - _sp["pos"]) > 300: _sp["pos"], _sp["vel"] = target, 0.0; return target
    n = max(1, int(-(-dt // (1 / 120.0)))); h = dt / n
    for _ in range(n): acc = w * w * (target - _sp["pos"]) - 2 * z * w * _sp["vel"]; _sp["vel"] += acc * h; _sp["pos"] += _sp["vel"] * h
    _sp["pos"] = max(target - mx, min(target + mx, _sp["pos"])); return _sp["pos"]
def draw(f, caption, sub="", base=None, drop=0, squash=1.0, lane=None, air=0.0):
    gx, gy, deg, hx, hy, sup, sx, sy, lean, move, doing, length, hdeg = f[:13]; bkey = f[14] if len(f) > 14 else ""
    if SPRING: deg = spring_deg(deg)
    glint = f[13] if len(f) > 13 and not isinstance(f[13], str) else 0
    u = Image.new("RGBA", (W, W)); b = posed(bkey, base or cellsb[0])
    drop = drop - int(air * 16 * S)                                                  # the leap: up off the ground (tiles -> px)
    u.alpha_composite(b, (ub, ub + drop))
    wi = img if length > 0.99 else img.resize((max(1, round(img.width * length)), img.height), Image.NEAREST)
    paste(u, wi, (grip[0] * length, grip[1]), (ub + gx * S, ub + gy * S + drop), deg)
    fz = fist.rotate(hdeg, expand=True, resample=Image.NEAREST); u.alpha_composite(fz, (round(ub + hx * S - fz.width / 2), round(ub + hy * S - fz.height / 2 + drop)))
    if sup: u.alpha_composite(other, (round(ub + sx * S - 2 * S), round(ub + sy * S - 1.5 * S + drop)))
    u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST); c = Image.new("RGBA", (W, W), BG); c.alpha_composite(u); c = c.crop((40, 10, W - 40, W - 20))
    c = c.resize((c.width * 3 // 2, c.height * 3 // 2), Image.NEAREST); wide = Image.new("RGBA", (c.width + 330, c.height), BG); wide.alpha_composite(c, (0, 0)); c = wide; d = ImageDraw.Draw(c)
    if lane:                                                                          # the rhythm counter: a notch over the head, one mark per blow still to come, spaced by the real gaps (0.55 tiles a second)
        hx_, hy_ = c.width // 2 - 8, 14
        d.rectangle((hx_ - 2, hy_ - 9, hx_ + 1, hy_ + 9), fill=(235, 235, 235))
        for secs, red in lane:
            if secs > 3.4: continue
            x = hx_ + max(0.0, secs) * 0.55 * 16 * S * 1.5; col = (255, 56, 42) if red else (255, 255, 235); d.rectangle((x - 14, hy_ - 3, x + 14, hy_ + 3), fill=col)
    if glint: r = 16; x, y = c.width - 400, 60; d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 60, 40) if glint == 2 else (255, 255, 230))
    out = Image.new("RGB", (c.width, c.height + 56), BG[:3]); out.paste(c.convert("RGB"), (0, 56)); dd = ImageDraw.Draw(out)
    dd.text((8, 6), caption, font=font, fill=(240, 200, 90)); dd.text((8, 32), sub, font=small, fill=(210, 210, 210)); return out
frames = []
def add(fs, caption, sub="", hold=10, **kw):
    for f in fs: frames.append(draw(f, caption, sub, **kw))
    for _ in range(hold): frames.append(draw(fs[-1], caption, sub, **kw))
stance = [f for f in moves["frames"][:15]]
add(stance, "STANCE", DEF)
# the moves
names = {i: f"pose {i}" for i in range(12)}
if DEF == "Verger": vi = boss.index("static Move[] VergerScytheMovesRaw()"); blk = boss[vi:boss.index("};", vi)]
else: vi = boss.index("public static readonly Def %s = new Def" % DEF); blk = boss[vi:boss.index("            }, 1f),", vi)]
info = {}
for m in re.finditer(r'new Move \{ Name = "([^"]+)"([^}]*)\}', blk):
    t = m.group(2); h = re.search(r"Hits = (\d+)", t); p = re.search(r"Poses = new\[\] \{([^}]*)\}", t); gp = re.search(r"Gap = ([0-9.]+)f", t)
    info[m.group(1)] = (int(h.group(1)) if h else 1, [names[int(x)] for x in p.group(1).split(",")] if p else [], "RED: dive it" if "Danger = true" in t else "WHITE: parry it", "phase 2 only" if "Phase2Only" in t else "")
MV = {}
for m in re.finditer(r'new Move \{ Name = "([^"]+)"([^}]*)\}', blk):
    t = m.group(2); w = re.search(r"Wind = ([0-9.]+)f", t); h = re.search(r"Hits = (\d+)", t); g = re.search(r"Gaps = new\[\] \{([^}]*)\}", t); g1 = re.search(r"Gap = ([0-9.]+)f", t); lp = re.search(r"Leap = ([0-9.]+)f", t)
    hits = int(h.group(1)) if h else 1; gaps = [float(x.replace("f", "")) for x in g.group(1).split(",")] if g else [float(g1.group(1))] if g1 else [0.4]
    wd = float(w.group(1)) if w else 0.6; sp = 0.8 if ("Phase2Only" in t and DEF != "Verger") else 1.0
    if DEF == "Verger": wd += 0.3                                              # VergerWindExtra
    MV[m.group(1)] = dict(wind=wd * sp, hits=hits, gaps=[g_ * sp for g_ in gaps], leap=float(lp.group(1)) if lp else 0.0, red=[True] if "Danger = true" in t else [False])
for mi, title in enumerate(moves["moves"]):
    nm = re.sub(r" \(red[^)]*\)| \[P[12]\]", "", title); n_hits, ps, kind, ph = info.get(nm, (1, [], "", ""))
    idx = [i for i, f in enumerate(moves["frames"]) if f[9] == mi and f[10]]
    if not idx: continue
    blockend = max(i for i, f in enumerate(moves["frames"]) if f[9] == mi)
    seq = moves["frames"][max(0, idx[0] - 4):blockend + 1]; first = max(0, idx[0] - 4)
    sub = f"{n_hits} hit{'s' if n_hits > 1 else ''}: " + ", ".join(ps) + f"   |   {kind}" + (f"   |   {ph}" if ph else "")
    ms = MV.get(nm); lanes, airs = [], []
    for k in range(len(seq)):
        now = (first + k - idx[0]) / 30.0                                           # seconds since Begin
        lane, air = [], 0.0
        if ms and 0 <= now:
            tt = ms["wind"]; hits = []
            for i in range(ms["hits"]):
                if i: tt += ms["gaps"][min(i - 1, len(ms["gaps"]) - 1)]
                hits.append((tt - now, ms["red"][min(i, len(ms["red"]) - 1)]))
            lane = [h for h in hits if h[0] > -0.03]
            if ms["leap"] > 0: x = (now - (ms["wind"] - ms["leap"])) / ms["leap"]; air = 1.3 * 4 * x * (1 - x) if 0 < x < 1 else 0.0
        lanes.append(lane); airs.append(air)
    for k, f in enumerate(seq): frames.append(draw(f, nm.upper(), sub, lane=lanes[k], air=airs[k]))
    for _ in range(12): frames.append(draw(seq[-1], nm.upper(), sub, lane=[], air=0.0))
if os.environ.get("FRAMEDIR"):                                                      # raw frames (no GIF merging of identical ones), for side-by-side comparisons
    os.makedirs(os.environ["FRAMEDIR"], exist_ok=True)
    for i_, fr_ in enumerate(frames[:int(os.environ.get("FRAMEMAX", "100000"))]): fr_.save(os.path.join(os.environ["FRAMEDIR"], "%05d.png" % i_))
    print(len(frames), "raw frames"); sys.exit(0)
frames[0].save(sys.argv[3], save_all=True, append_images=frames[1:], duration=1000 // 30, loop=0); print(len(frames), "frames", frames[0].size)
