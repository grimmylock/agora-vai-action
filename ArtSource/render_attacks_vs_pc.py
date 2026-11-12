# One rendering per canine of its attacks against a player character (2026-11-12, Yan: "give me one rendering of each of them doing their attacks against a pc").
# A composite at the game's scale (1 tile = 32 art pixels, shown at 4x): the real attack strips from Assets/Resources/Sprites/Mutants/anim, the real player sprite (armor atlas cell 0), the game's own blood puff
# (fx_hit_blood) and the acid strips, played on the timings and rules the code uses (contact frames, travel, the Maw's straight 8-tile charge, the Veil's retreat and re-cloak).
# It is a hand-built timeline, NOT a Unity capture: the hurt flash, knockdown and bleeding are drawn the way the effects behave, not by the game.   python3 render_attacks_vs_pc.py <out_dir>
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); R = os.path.join(HERE, "..", "Assets", "Resources", "Sprites")
A = os.path.join(R, "Mutants", "anim"); T = 32; TILES = 11; STEP = 40; SC = 4
GROUND = 112; CW = TILES * T; CH = 150
TABLE = json.load(open(os.path.join(A, "mutant_attacks.json")))
def load_strip(path, n): im = Image.open(path).convert("RGBA"); w = im.width // n; return [im.crop((i * w, 0, (i + 1) * w, im.height)) for i in range(n)]
def strip_of(key, kind): return load_strip(os.path.join(A, f"{key}_{kind}.bytes"), TABLE[key][kind]["frames"])
walk = {k: load_strip(os.path.join(A, f"{k}_walk.bytes"), 8) for k in ("veil_prowler", "pallid_prowler", "dread_prowler", "blind_maw")}
stand = {k: Image.open(os.path.join(R, "Mutants", f"mutant_hd_{k}.png")).convert("RGBA") for k in walk}
atlas = Image.open(os.path.join(R, "armor_atlas_x2.png")).convert("RGBA"); PC = atlas.crop((0, 0, 32, 32))
blood = load_strip(os.path.join(R, "Fx", "fx_hit_blood.png"), 3)
glob = load_strip(os.path.join(A, "pallid_acid_glob.bytes"), 6); splat = load_strip(os.path.join(A, "pallid_acid_splat.bytes"), 6)
dissolve = load_strip(os.path.join(A, "veil_prowler_dissolve.bytes"), 6)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
def tx(tile): return int(tile * T)                      # a tile centre's x, tile index counted from the left edge, 0.5 added by callers
def lowest(img): a = np.array(img)[..., 3] > 0; return int(np.where(a.any(axis=1))[0].max())

def put(canvas, img, cx, ground, alpha=1.0, lowest_row=None):
    """paste `img` centred on cx with its lowest opaque row (of the stand pose) on `ground`"""
    lr = lowest(img) if lowest_row is None else lowest_row
    if alpha < 1.0: a = np.array(img); a[..., 3] = (a[..., 3] * alpha).astype(np.uint8); img = Image.fromarray(a)
    canvas.alpha_composite(img, (int(cx - img.width / 2), int(ground + 1 - (lr + 1))))

def attack_frame(key, kind, tms):
    a = TABLE[key][kind]; acc = 0
    for i, d in enumerate(a["ms"]):
        if tms < acc + d: return i
        acc += d
    return a["frames"] - 1
def travel_at(key, kind, tms):
    a = TABLE[key][kind]; ms, tr = a["ms"], a["travel"]; acc = 0
    for i, d in enumerate(ms):
        if tms < acc + d: f = (tms - acc) / d; nxt = tr[i + 1] if i + 1 < len(tr) else tr[i]; return tr[i] + (nxt - tr[i]) * f
        acc += d
    return tr[-1]
def tint(img, col, k):
    a = np.array(img).astype(float); m = a[..., 3] > 0; a[m, :3] = a[m, :3] * (1 - k) + np.array(col) * k; return Image.fromarray(a.astype(np.uint8))

class Scene:
    def __init__(self, caption): self.frames = []; self.caption = caption; self.t = 0
    def frame(self, draw_fn, caption=None):
        im = Image.new("RGBA", (CW, CH), (46, 43, 38, 255)); d = ImageDraw.Draw(im)
        d.rectangle([0, GROUND + 1, CW, CH], fill=(60, 55, 46, 255))
        for i in range(TILES + 1): d.line([(i * T, GROUND + 1), (i * T, GROUND + 5)], fill=(100, 94, 82))
        for i in range(TILES): d.text((i * T + 12, GROUND + 8), str(i), font=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 11), fill=(120, 112, 98))
        draw_fn(im); big = im.resize((CW * SC, CH * SC), Image.NEAREST); ImageDraw.Draw(big).text((16, 10), caption or self.caption, font=font, fill=(222, 214, 190))
        self.frames.append(big.convert("RGB")); self.t += STEP

def pc_state(t_hit, now, kind="bite", pc_x=7.5, aside=0):
    """(image, x offset px, y offset px) of the pc `now` ms into the scene when it was hit at t_hit (None = not yet)"""
    img = PC; dx = 0; dy = -aside; extras = []
    if t_hit is not None and now >= t_hit:
        e = now - t_hit
        if e < 90: img = tint(PC, (255, 255, 255), 0.75)
        if e < 140: dx = 2
        if kind == "knockdown":
            ang = 0 if e > 1000 else (-82 if e < 800 else -82 * (1 - (e - 800) / 200)); ang = min(0, ang) if e < 100 else ang
            if e >= 120: img = img.rotate(-ang if False else ang, resample=Image.BICUBIC, center=(16, 31), expand=False)
        if kind == "toxic" and e < 1400: img = tint(img, (130, 255, 60), 0.35 * (1 - e / 1400))
        if kind == "bleed" and e < 1500: img = tint(img, (190, 30, 40), 0.25)
    return img, dx, dy

def blood_puff(canvas, cx, t_hit, now):
    if t_hit is None or not (0 <= now - t_hit < 200): return
    f = blood[min(2, int((now - t_hit) / 70))]; f = f.resize((f.width * 2, f.height * 2), Image.NEAREST); canvas.alpha_composite(f, (int(cx - 6), GROUND - 22))
def drips(canvas, cx, t_hit, now):
    if t_hit is None or not (300 <= now - t_hit < 1500): return
    d = ImageDraw.Draw(canvas)
    for k in range(3): y = GROUND - 14 + int(((now - t_hit) * 0.03 + k * 5) % 14); d.point((cx - 3 + k * 3, y), fill=(190, 30, 40, 255)); d.point((cx - 3 + k * 3, y + 1), fill=(150, 20, 30, 255))

def run_bite(sc, key, mtile, pc_tile, kind="bite", extra=0):
    a = TABLE[key][kind]; t_hit = a["contact_ms"]; fr = strip_of(key, kind); L = lowest(stand[key])
    for now in range(0, a["total_ms"] + 300, STEP):
        def draw(im, now=now):
            put(im, fr[attack_frame(key, kind, now)], tx(mtile + 0.5), GROUND, lowest_row=L_attack[key]); pcimg, dx, dy = pc_state(t_hit, now)
            blood_puff(im, tx(pc_tile + 0.5), t_hit, now); put(im, pcimg, tx(pc_tile + 0.5) + dx, GROUND + dy, lowest_row=30)
        sc.frame(draw)
L_attack = {k: lowest(strip_of(k, "bite")[0]) for k in ("veil_prowler", "pallid_prowler", "dread_prowler", "blind_maw")}

def pounce_scene(sc, key, from_tile, land_tile, pc_tile, effect, caption):
    a = TABLE[key]["pounce"]; fr = strip_of(key, "pounce"); t_hit = a["contact_ms"]
    for now in range(0, a["total_ms"] + 1300, STEP):
        k = travel_at(key, "pounce", now); x = tx(from_tile + 0.5) + (tx(land_tile + 0.5) - tx(from_tile + 0.5)) * k
        def draw(im, now=now, x=x):
            put(im, fr[attack_frame(key, "pounce", now)], x, GROUND, lowest_row=L_attack[key]); pcimg, dx, dy = pc_state(t_hit, now, effect)
            blood_puff(im, tx(pc_tile + 0.5), t_hit, now)
            if effect == "bleed": drips(im, tx(pc_tile + 0.5), t_hit, now)
            put(im, pcimg, tx(pc_tile + 0.5) + dx, GROUND + dy, lowest_row=30)
        sc.frame(draw, caption)

def save(sc, path):
    sc.frames[0].save(path, save_all=True, append_images=sc.frames[1:], duration=STEP, loop=0); print(path, len(sc.frames), "frames", sc.frames[0].size)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/vs_pc"; os.makedirs(out, exist_ok=True)
    PCT = 8
    # ---- Dread: pounce from 5 tiles (knocks down), then a bite
    sc = Scene(""); pounce_scene(sc, "dread_prowler", 2, PCT - 1, PCT, "knockdown", "Dread Prowler: pounce from 5 tiles - knocks the target down"); run_bite(sc, "dread_prowler", PCT - 1, PCT, "bite"); save(sc, os.path.join(out, "dread_vs_pc.gif"))
    # ---- Veil: cloaked at 4 tiles, uncloaks as it pounces, bites, slips 3 tiles away, re-cloaks 3 s later
    sc = Scene(""); key = "veil_prowler"; a = TABLE[key]["pounce"]; fr = strip_of(key, "pounce"); t_hit = a["contact_ms"]; start_tile = PCT - 4
    for now in range(0, 900, STEP):
        sc.frame(lambda im: (put(im, stand[key], tx(start_tile + 0.5), GROUND, alpha=0.3), put(im, PC, tx(PCT + 0.5), GROUND, lowest_row=30)), "Veil Prowler: cloaked, stalking")
    for now in range(0, a["total_ms"] + 200, STEP):
        k = travel_at(key, "pounce", now); x = tx(start_tile + 0.5) + (tx(PCT - 0.5) - tx(start_tile + 0.5)) * k; fade = max(0.0, 1 - now / 350.0)
        def draw(im, now=now, x=x, fade=fade):
            f = fr[attack_frame(key, "pounce", now)]; put(im, f, x, GROUND, alpha=1 - 0.7 * fade, lowest_row=L_attack[key])
            pcimg, dx, dy = pc_state(t_hit, now, "bite"); blood_puff(im, tx(PCT + 0.5), t_hit, now); put(im, pcimg, tx(PCT + 0.5) + dx, GROUND + dy, lowest_row=30)
        sc.frames  # noqa
        sc.frame(draw, "Veil Prowler: shows itself, pounces, bites")
    run_bite_end = None
    for now in range(0, 1000, STEP):                                         # slips away 3 tiles, walking
        x = tx(PCT - 0.5) - (now / 1000.0) * 3 * T; wf = walk[key][int(now / 125) % 8]
        sc.frame(lambda im, x=x, wf=wf: (put(im, wf, x, GROUND, lowest_row=lowest(stand[key])), put(im, PC, tx(PCT + 0.5), GROUND, lowest_row=30)), "Veil Prowler: slips 3 tiles away")
    for now in range(0, 3000, STEP):
        sc.frame(lambda im: (put(im, stand[key], tx(PCT - 3.5), GROUND, lowest_row=lowest(stand[key])), put(im, PC, tx(PCT + 0.5), GROUND, lowest_row=30)), "Veil Prowler: waits unhit ... (3 s)")
    for now in range(0, 400, STEP):
        i = min(5, int(now / 350 * 6)); sc.frame(lambda im, i=i: (put(im, dissolve[i], tx(PCT - 3.5), GROUND, alpha=1 - 0.7 * min(1, now / 350.0), lowest_row=lowest(stand[key])), put(im, PC, tx(PCT + 0.5), GROUND, lowest_row=30)), "Veil Prowler: re-cloaks")
    for now in range(0, 1000, STEP): sc.frame(lambda im: (put(im, stand[key], tx(PCT - 3.5), GROUND, alpha=0.3, lowest_row=lowest(stand[key])), put(im, PC, tx(PCT + 0.5), GROUND, lowest_row=30)), "Veil Prowler: cloaked again")
    save(sc, os.path.join(out, "veil_vs_pc.gif"))
    # ---- Pallid: spit from 5 tiles (the glob flies, splashes, toxic), then a bite when adjacent
    key = "pallid_prowler"; sp = TABLE[key]["spit"]; fr = strip_of(key, "spit"); mz = sp["muzzle"]; t_spawn = sp["contact_ms"]; mt = 3; dist_px = tx(PCT + 0.5) - tx(mt + 0.5); fly = dist_px / T / 10.0 * 1000; t_hit = t_spawn + fly; sc = Scene("")
    SPL = [60, 60, 70, 80, 120, 140]
    for now in range(0, int(t_hit + 900), STEP):
        def draw(im, now=now):
            fi = attack_frame(key, "spit", now); fx0 = tx(mt + 0.5) - sp["w"] // 2; put(im, fr[fi], tx(mt + 0.5), GROUND, lowest_row=L_attack[key])
            if t_spawn <= now < t_hit:
                k = (now - t_spawn) / fly; mx = fx0 + mz[4][0] - 14; my = GROUND + 1 - (sp["h"] - mz[4][1]) - 0; gx = mx + (tx(PCT + 0.5) - mx) * k; gy = my + ((GROUND - 16) - my) * k
                g = glob[int(now / 70) % 6]; im.alpha_composite(g, (int(gx - 13), int(gy - 5)))
            if now >= t_hit:
                e = now - t_hit; acc = 0
                for j, dd in enumerate(SPL):
                    acc += dd
                    if e < acc: im.alpha_composite(splat[j], (tx(PCT + 0.5) - 11, GROUND - 14 + 1)); break
            pcimg, dx, dy = pc_state(t_hit, now, "toxic"); put(im, pcimg, tx(PCT + 0.5) + dx, GROUND + dy, lowest_row=30)
        sc.frame(draw, "Pallid Prowler: acid spit from 5 tiles (toxic)")
    run_bite(sc, key, PCT - 1, PCT, "bite"); save(sc, os.path.join(out, "pallid_vs_pc.gif"))
    # ---- Maw: a charge down a straight line (the pc stands in it: bitten and bleeding), then the same charge when the pc steps aside (it runs the whole 8 tiles and stumbles)
    key = "blind_maw"; sc = Scene(""); bite = strip_of(key, "bite"); L = lowest(stand[key]); wk = walk[key]; spd = 7.0; start = 2.5
    def charge(aside, caption):
        run0 = 300; x_end_tiles = start + 8.0; hit_t = None; pos = start
        for now in range(0, 3400, STEP):
            if now < run0: img = bite[2]; pos = start
            else:
                if hit_t is None:
                    pos = min(x_end_tiles, start + spd * (now - run0) / 1000.0); img = wk[int((now - run0) / 62) % 8]
                    if aside == 0 and abs(pos - (PCT + 0.5)) <= 0.95: hit_t = now; pos = PCT + 0.5 - 0.95
                    elif aside and pos >= x_end_tiles: hit_t = -now
                else: img = None
            def draw(im, now=now, pos=pos, img=img, hit_t=hit_t):
                if hit_t is not None and hit_t > 0:
                    e = now - hit_t; bi = 4 + min(5, int(e / 90)); pic = bite[bi]
                elif hit_t is not None and hit_t < 0: pic = stand[key]
                else: pic = img
                put(im, pic, tx(pos), GROUND, lowest_row=(L_attack[key] if (pic in bite) else L))
                hurt = hit_t if (hit_t is not None and hit_t > 0) else None; pcimg, dx, dy = pc_state(hurt, now, "bleed", aside=aside)
                blood_puff(im, tx(PCT + 0.5), hurt, now); drips(im, tx(PCT + 0.5), hurt, now); put(im, pcimg, tx(PCT + 0.5) + dx, GROUND + dy, lowest_row=30)
            sc.frame(draw, caption)
            if hit_t is not None and now - abs(hit_t) > 1500: break
    charge(0, "Blind Maw: charges in a straight line - bites, bleeding"); charge(10, "Blind Maw: the player steps aside - it runs all 8 tiles and stumbles")
    save(sc, os.path.join(out, "maw_vs_pc.gif"))
