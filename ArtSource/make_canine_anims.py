# Canine-like mutants: RUN, BITE (close) and POUNCE (leap from a distance, bite on landing) - second try (2026-11-09), on the cut-out rig (canine_cutout.py).
# Yan rejected the first try ("all of them distort the original and look terrible"; the attacks "look bad"): now no pixel is ever resampled or stretched - the sprite is cut into
# rigid pieces that only slide by whole pixels, legs and the jaw turn at their joints, and the neutral pose is the original sprite exactly.
#   python3 make_canine_anims.py <out_dir> [keys]
import math, os, sys, json
from PIL import Image, ImageDraw
import mutant_from_ref as m
import canine_cutout as C
import anim_fx as FX

HERE = os.path.dirname(os.path.abspath(__file__))

def run_poses(n=8):
    out = []
    for i in range(n):
        ph = 2 * math.pi * i / n; c = math.cos(ph); ch = math.cos(ph - 0.9); g = max(0.0, -c); e = max(0.0, c)
        fore = lambda cc: (46 * cc - 4, -48 * max(0.0, -cc) - 8 * max(0.0, cc)); hind = lambda cc: (-44 * cc + 8, -34 * max(0.0, -cc) + 10 * max(0.0, cc))
        fu, fl = fore(c); hu, hl = hind(ch)
        out.append(dict(hdx=-int(round(e)) + int(round(g)), fdx=int(round(e)) - int(round(g)), hdy=-int(round(g)), fdy=int(round(e * 0.6)),
                        rise=-int(round(2 * (math.cos(2 * ph) + 1) / 2)), fu=fu, fl=fl, hu=hu, hl=hl, lag=0.5 * math.sin(ph)))
    return out, [42] * n

# (pose, ms, travel). A bite is a lunge of the forequarters and head, the jaw gaping then snapping; a pounce is a crouch, a leap (travel 0..1 across the jump) and a bite on landing.
BITE = [
    (dict(fu=6), 90, 0),
    (dict(hdx=1, hdy=1, fdx=-1, fdy=1, ny=1, fu=-12, fl=-18, hu=22, hl=-24, jaw=14), 70, 0),
    (dict(hdx=1, hdy=1, fdx=-1, fdy=0, ny=-1, fu=-4, fl=-12, hu=24, hl=-24, jaw=36), 70, 0),
    (dict(hdx=-1, fdx=3, fdy=0, nx=1, fu=40, fl=-8, hu=-30, hl=6, jaw=48), 45, 0),
    (dict(hdx=-2, fdx=5, fdy=1, nx=2, ny=1, fu=48, hu=-38, hl=8, jaw=50), 45, 0),
    (dict(hdx=-2, fdx=5, fdy=1, nx=2, ny=1, fu=46, hu=-38, jaw=0, flash=True), 80, 0),
    (dict(hdx=-2, fdx=4, fdy=1, nx=2, fu=44, hu=-36, jaw=3), 90, 0),
    (dict(hdx=-1, fdx=4, fdy=0, nx=1, ny=1, fu=44, hu=-36, jaw=4), 90, 0),
    (dict(fdx=1, fu=20, hu=-10, jaw=14), 80, 0),
    (dict(fu=8), 80, 0),
]
POUNCE = [
    (dict(hdx=1, hdy=1, fdx=-1, fdy=2, ny=1, rise=1, fu=10, fl=-20, hu=26, hl=-32), 110, 0.0),
    (dict(hdx=2, hdy=2, fdx=-1, fdy=2, ny=1, rise=2, fu=20, fl=-30, hu=32, hl=-40, jaw=10), 100, 0.0),
    (dict(hdx=-1, hdy=-1, fdx=2, fdy=-1, nx=1, rise=-3, fu=40, hu=-50, hl=6, jaw=26), 55, 0.12),
    (dict(hdx=-2, hdy=-1, fdx=3, fdy=-2, nx=1, rise=-9, fu=48, hu=-52, hl=8, jaw=38), 55, 0.36),
    (dict(hdx=-2, hdy=-1, fdx=3, fdy=-1, nx=2, rise=-13, fu=50, hu=-50, hl=8, jaw=48), 65, 0.62),
    (dict(hdx=-2, hdy=-2, fdx=4, fdy=1, nx=2, ny=1, rise=-9, fu=52, hu=-46, hl=8, jaw=52), 55, 0.86),
    (dict(hdx=-2, hdy=-1, fdx=5, fdy=1, nx=2, ny=1, rise=-2, fu=50, hu=-44, jaw=52), 55, 1.0),
    (dict(hdx=-2, fdx=5, fdy=1, nx=2, ny=1, fu=46, hu=-40, jaw=0, flash=True), 80, 1.0),
    (dict(hdx=-2, fdx=4, fdy=1, nx=2, fu=44, hu=-36, jaw=3), 100, 1.0),
    (dict(hdx=-1, fdx=4, nx=1, ny=1, fu=44, hu=-36, jaw=4), 100, 1.0),
    (dict(fdx=1, fu=20, hu=-10, jaw=14), 80, 1.0),
    (dict(fu=8), 80, 1.0),
]

def build(key):
    rig = C.Rig(key, C.SPECS[key]); res = {}
    p, ms = run_poses(); res["run"] = ([rig.frame(q) for q in p], ms, [0] * len(p))
    res["bite"] = ([rig.frame(q) for q, _, _ in BITE], [t for _, t, _ in BITE], [0] * len(BITE))
    res["pounce"] = ([rig.frame(q) for q, _, _ in POUNCE], [t for _, t, _ in POUNCE], [tr for _, _, tr in POUNCE])
    return rig, res

def human():
    im = Image.open(os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA").crop((0, 0, 16, 16))
    return im.resize((32, 32), Image.NEAREST)


def idx_at(ms, t):
    total = sum(ms); t = t % (total + 400); acc = 0
    for i, d in enumerate(ms):
        if t < acc + d: return i
        acc += d
    return len(ms) - 1


def panel(res_item, kind, sc, hum, W, H, tm):
    fr_list, ms, trav = res_item
    idx = (int(tm // ms[0]) % len(fr_list)) if kind == "run" else idx_at(ms, tm)
    fr = fr_list[idx]; im = Image.new("RGBA", (W, H), (42, 38, 32, 255)); d = ImageDraw.Draw(im)
    gy = H - 14; d.rectangle([0, gy, W, H], fill=(54, 49, 41, 255)); fw, fh = fr.size
    if kind == "run":
        off = int(tm * 0.35) % 24
        for gx in range(-24, W, 24): d.rectangle([gx - off + 24, gy + 4, gx - off + 32, gy + 5], fill=(70, 64, 54, 255))
        x = (W - fw) // 2
    else:
        hx = W - 40
        if kind == "pounce": x = int(4 + (hx - fw + 14 - 4) * trav[idx])
        else: x = hx - fw + 14
        hf = hum.copy()
        if idx >= (7 if kind == "pounce" else 5) and idx <= (9 if kind == "pounce" else 7):
            tint = Image.new("RGBA", hf.size, (200, 40, 40, 255)); hf = Image.composite(tint, hf, hf.split()[3].point(lambda v: 110 if v else 0))
        im.alpha_composite(hf, (hx, gy - 30))
    sh = Image.new("RGBA", (W, H), (0, 0, 0, 0)); ImageDraw.Draw(sh).ellipse([x + 8, gy - 3, x + fw - 12, gy + 4], fill=(20, 18, 14, 130)); im.alpha_composite(sh)
    im.alpha_composite(fr, (x, gy - fh + 12))
    if kind != "run":                                                       # effects: dust where it leaps off and lands, streaks in the air, the hit on the man
        snap = 7 if kind == "pounce" else 5; t0 = tm % (sum(ms) + 400); start = lambda i: sum(ms[:i]); k = lambda i, step: int((t0 - start(i)) // step) if t0 >= start(i) else -1
        if kind == "pounce":
            FX.dust_puff(im, 4 + 16, gy + 1, k(2, 55), seed=3)
            if 3 <= idx <= 5: FX.speed_lines(im, x + 8, gy - fh // 2 - 4, 20, k(3, 40))
            FX.dust_puff(im, hx - 4, gy + 1, k(6, 60), seed=5)
        FX.bite_hit(im, hx + 6, gy - 20, k(snap, 55))
    return im.resize((W * sc, H * sc), Image.NEAREST)


def review_gif(path, a, res, hum):
    sc = 4; fw, fh = a.W, a.H
    Wr, Wb, Wp, H = fw + 6, fw + 40, fw + 150, fh + 18
    frames = []; durs = []; prev = None
    for tm in range(0, 4800, 10):
        key = (int(tm // res["run"][1][0]) % len(res["run"][0]), idx_at(res["bite"][1], tm), idx_at(res["pounce"][1], tm))
        if key == prev: durs[-1] += 10; continue
        prev = key
        r0 = panel(res["run"], "run", sc, hum, Wr, H, tm); r1 = panel(res["bite"], "bite", sc, hum, Wb, H, tm); r2 = panel(res["pounce"], "pounce", sc, hum, Wp, H, tm)
        full = Image.new("RGBA", (max((Wr + Wb) * sc + 8, Wp * sc), H * sc * 2 + 8), (28, 26, 22, 255))
        full.alpha_composite(r0, (0, 0)); full.alpha_composite(r1, (Wr * sc + 8, 0)); full.alpha_composite(r2, (0, H * sc + 8))
        frames.append(full.convert("P", palette=Image.ADAPTIVE)); durs.append(10)
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=durs, loop=0)


def save(out, only=None):
    os.makedirs(out, exist_ok=True); meta = {}; hum = human()
    for key in C.SPECS:
        if only and key not in only: continue
        a, res = build(key); meta[key] = {}
        for name, (fr, ms, tv) in res.items():
            w, h = fr[0].size; strip = Image.new("RGBA", (w * len(fr), h), (0, 0, 0, 0))
            for i, f in enumerate(fr): strip.alpha_composite(f, (i * w, 0))
            strip.save(os.path.join(out, f"{key}_{name}_strip.png")); meta[key][name] = dict(frame=[w, h], ms=ms, travel=tv)
        review_gif(os.path.join(out, f"{key}_run_bite_pounce.gif"), a, res, hum)
        sc = 5; w, h = a.W, a.H
        sheet = Image.new("RGBA", (w * sc * 6, h * sc * 4), (42, 38, 32, 255))
        for r, frs in enumerate([res["bite"][0], res["pounce"][0]]):
            for i, f in enumerate(frs): sheet.alpha_composite(f.resize((w * sc, h * sc), Image.NEAREST), ((i % 6) * w * sc, (r * 2 + i // 6) * h * sc))
        sheet.save(os.path.join(out, f"{key}_attacks_sheet.png")); print(key, "done")
    json.dump(meta, open(os.path.join(out, "canine_anims.json"), "w"), indent=1)


if __name__ == "__main__":
    save(sys.argv[1] if len(sys.argv) > 1 else "_out/canine", set(sys.argv[2].split(",")) if len(sys.argv) > 2 else None)
