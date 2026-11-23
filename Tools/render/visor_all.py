"""2026-11-23 (Yan: "show me all of the Visor's moves, including the combos, the stance and everything else"): one animated sheet, in order: stance, walking, guard (the parry), guard broken (stagger), open (recovering), on one knee (broken, waiting for the finisher), then
every move from its wind-up through every blow of its combo to the opening after it, each captioned with its hits and poses. The game's own pose code (`visorstates`, `visormoves` dumps), his real sprites. 30 frames a second of game time.
Usage: python3 Tools/render/visor_all.py states.json moves.json out.gif"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, S, BG, big_glove, glove, paste
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
hw = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read(); boss = open(os.path.join(ROOT, "Assets/Scripts/GameCore/Boss.cs")).read()
def tab(n, k):
    m = re.search(r'\["%s"\] = ([0-9.]+)f' % k, hw.split(n + " = new")[1].split("};")[0]); return float(m.group(1)) if m else 1.0
PKEYS = ["crouch", "crouch_h", "kneel", "kneel_h", "lean_h_l", "lean_h_r", "lean_l", "lean_r", "lunge_h_l", "lunge_h_r", "lunge_l", "lunge_r", "lying", "sit_h_l", "sit_h_r", "sit_l", "sit_r", "stagger_h_l", "stagger_h_r", "stagger_l", "stagger_r"]
strip = Image.open(os.path.join(SPR, "armor_unique_black_visor_suit.png")).convert("RGBA"); cellsb = [strip.crop((i * 16, 0, i * 16 + 16, 16)).resize((16 * S, 16 * S), Image.NEAREST) for i in range(6)]
poses = Image.open(os.path.join(SPR, "armor_unique_black_visor_suit_poses.png")).convert("RGBA")
def posed(k, base): return base if not k else poses.crop((PKEYS.index(k) * 16, 0, PKEYS.index(k) * 16 + 16, 16)).resize((16 * S, 16 * S), Image.NEAREST)
fist = big_glove("#1f2226", "#2a2c30").resize((5 * S, 4 * S), Image.NEAREST); other = glove("#1f2226", "#2a2c30").resize((4 * S, 3 * S), Image.NEAREST)
W = 16 * S * 3; ub = 16 * S; font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 17); small = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 14)
kd, hs = tab("Dense", "SerratedTrenchKnife"), tab("HandScale", "SerratedTrenchKnife")
if kd > 1: im = Image.open(os.path.join(SPR, "Weapons/weapon_SerratedTrenchKnife_hd.png")).convert("RGBA"); img = im.resize((round(im.width / kd * hs), round(im.height / kd * hs)), Image.LANCZOS)
else: img = Image.open(os.path.join(SPR, "Weapons/weapon_SerratedTrenchKnife.png")).convert("RGBA")
moves = json.load(open(sys.argv[2]))["weapons"][0]; grip = [moves["grip"][0] * hs, moves["grip"][1] * hs]
def draw(f, caption, sub="", base=None, drop=0, squash=1.0):
    gx, gy, deg, hx, hy, sup, sx, sy, lean, move, doing, length, hdeg = f[:13]; bkey = f[14] if len(f) > 14 else ""
    glint = f[13] if len(f) > 13 and not isinstance(f[13], str) else 0
    u = Image.new("RGBA", (W, W)); b = posed(bkey, base or cellsb[0])
    u.alpha_composite(b, (ub, ub + drop))
    wi = img if length > 0.99 else img.resize((max(1, round(img.width * length)), img.height), Image.NEAREST)
    paste(u, wi, (grip[0] * length, grip[1]), (ub + gx * S, ub + gy * S + drop), deg)
    fz = fist.rotate(hdeg, expand=True, resample=Image.NEAREST); u.alpha_composite(fz, (round(ub + hx * S - fz.width / 2), round(ub + hy * S - fz.height / 2 + drop)))
    if sup: u.alpha_composite(other, (round(ub + sx * S - 2 * S), round(ub + sy * S - 1.5 * S + drop)))
    u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST); c = Image.new("RGBA", (W, W), BG); c.alpha_composite(u); c = c.crop((40, 10, W - 40, W - 20))
    c = c.resize((c.width * 3 // 2, c.height * 3 // 2), Image.NEAREST); d = ImageDraw.Draw(c)
    if glint: r = 16; x, y = c.width - 60, 60; d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 60, 40) if glint == 2 else (255, 255, 230))
    out = Image.new("RGB", (c.width, c.height + 56), BG[:3]); out.paste(c.convert("RGB"), (0, 56)); dd = ImageDraw.Draw(out)
    dd.text((8, 6), caption, font=font, fill=(240, 200, 90)); dd.text((8, 32), sub, font=small, fill=(210, 210, 210)); return out
frames = []
def add(fs, caption, sub="", hold=10, **kw):
    for f in fs: frames.append(draw(f, caption, sub, **kw))
    for _ in range(hold): frames.append(frames[-1])
st = json.load(open(sys.argv[1]))["segments"]
add(st[0]["frames"], "STANCE", st[0]["label"].split(": ")[1])
walk = []
for i in range(36):                                                  # walking: the two walk cells alternate every quarter of a stride; the stance pose on top
    q = (i // 5) % 4; walk.append((st[0]["frames"][0], cellsb[1] if q == 0 else cellsb[2] if q == 2 else cellsb[0]))
for f, c in walk: frames.append(draw(f, "WALK", "his feet alternate, the knife stays in its stance", base=c))
for _ in range(8): frames.append(frames[-1])
add(st[1]["frames"], "GUARD", "the parry (F): the knife up across the chest")
add(st[2]["frames"], "GUARD BROKEN", "a perfect parry knocks his arm up: stagger (slow motion in the game)")
add(st[3]["frames"], "OPEN", "the opening after a combo: the weapon sinks, the shoulders heave - hit him now")
add(st[0]["frames"][:30], "ON ONE KNEE", "at 0 HP he breaks and waits for the finisher (E)", base=posed("kneel_h", cellsb[0]), drop=0)
# the moves
names = {0: "backhand", 1: "rising", 2: "ice-pick", 3: "thrust", 4: "spin", 5: "execution", 6: "jab", 7: "cross", 8: "feint"}
vi = boss.index("public static readonly Def Visor = new Def"); blk = boss[vi:boss.index("};", vi)]
info = {}
for m in re.finditer(r'new Move \{ Name = "([^"]+)"([^}]*)\}', blk):
    t = m.group(2); h = re.search(r"Hits = (\d+)", t); p = re.search(r"Poses = new\[\] \{([^}]*)\}", t); gp = re.search(r"Gap = ([0-9.]+)f", t)
    info[m.group(1)] = (int(h.group(1)) if h else 1, [names[int(x)] for x in p.group(1).split(",")] if p else [], "RED: dive it" if "Danger = true" in t else "WHITE: parry it", "phase 2 only" if "Phase2Only" in t else "")
for mi, title in enumerate(moves["moves"]):
    nm = re.sub(r" \(red\)| \[P2\]", "", title); n_hits, ps, kind, ph = info.get(nm, (1, [], "", ""))
    idx = [i for i, f in enumerate(moves["frames"]) if f[9] == mi and f[10]]
    if not idx: continue
    blockend = max(i for i, f in enumerate(moves["frames"]) if f[9] == mi)
    seq = moves["frames"][max(0, idx[0] - 4):blockend + 1]
    sub = f"{n_hits} hit{'s' if n_hits > 1 else ''}: " + ", ".join(ps) + f"   |   {kind}" + (f"   |   {ph}" if ph else "")
    add(seq, nm.upper(), sub, hold=12)
frames[0].save(sys.argv[3], save_all=True, append_images=frames[1:], duration=1000 // 30, loop=0); print(len(frames), "frames", frames[0].size)
