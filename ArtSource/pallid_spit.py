# Pallid Prowler RANGED ATTACK: the head goes up and down like the bite and a green acid glob is spat (2026-11-12). Yan: "remove the pounce from the pallid prowler and add a ranged attack to him, he moves
# his head up and down (similar to the bite) and a ranged green acid-like projectile is shot (green like his mouth)".
# Body and head only (the legs stay planted, his own leg pixels); the glob and its impact are separate sprite strips so the game can fly the glob from the muzzle to the target and play the splat on the target.
#   python3 pallid_spit.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
import pallid_attacks as PA
import veil_attacks as VA

# the mouth's own greens (sampled from the sprite: (150,255,55) is the bright one)
G_LIGHT, G_BRIGHT, G_MID, G_DARK, G_DEEP = (226, 255, 170), (150, 255, 55), (96, 212, 38), (46, 130, 26), (22, 70, 18)

# (pose, ms). nrot > 0 lifts the nose end (a column shear), nx/ny move the head; the glob leaves the muzzle on the thrust frame.
SPIT = [
    (dict(), 90),
    (dict(bx=-1, nx=-1, ny=0, nrot=6), 100),
    (dict(bx=-2, nx=-1, ny=-1, nrot=12), 120),
    (dict(bx=-2, nx=-1, ny=-1, nrot=12), 70),                         # held, gathering
    (dict(bx=2, by=1, nx=3, ny=2, nrot=-8), 45),                      # the thrust: the glob is spat here
    (dict(bx=2, by=1, nx=3, ny=2, nrot=-6), 80),
    (dict(bx=1, nx=1, ny=1, nrot=-2), 90),
    (dict(), 100),
]
SPAWN = 4
def muzzle(p):
    """where the glob appears, in frame pixels: the snout tip (45,12) moved with the body and the head"""
    return (PA.ox + 45 + p.get("bx", 0) + p.get("nx", 0), PA.oy + 12 + p.get("by", 0) + p.get("ny", 0) - p.get("rise", 0) - int(round(p.get("nrot", 0) / 3.0)))

def blob(w, h, cx, cy, r, wob, ph):
    im = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        for x in range(w):
            ang = math.atan2(y - cy, x - cx); rr = r + wob * math.sin(3 * ang + ph); d = math.hypot(x - cx, y - cy)
            if d <= rr:
                t = d / rr; col = G_BRIGHT if t < 0.45 else G_MID if t < 0.8 else G_DARK
                im[y, x] = (*col, 255)
    return im

def glob_frames(n=6, w=18, h=10):
    """a teardrop of acid: a round head with a tapering tail streaming behind it (to the left), the tail wobbling frame to frame, one or two drops shaken off"""
    out = []
    for i in range(n):
        ph = 2 * math.pi * i / n; a = np.zeros((h, w, 4), np.uint8); cx, cy, r = w - 5.0, h / 2.0, 2.8 + 0.3 * math.sin(ph); L = 8.0
        for y in range(h):
            for x in range(w):
                d = math.hypot(x - cx, y - cy); inside = d <= r
                if not inside and x < cx:
                    t = (cx - x) / L; wob = 0.6 * math.sin(ph + 0.9 * (cx - x)); inside = t <= 1.0 and abs(y - cy - wob * t) <= r * (1.0 - t) * 0.9
                if inside:
                    t = min(1.0, d / r) if x >= cx - 1 else 0.6 + 0.4 * (cx - x) / L
                    col = G_BRIGHT if (x >= cx - 2 and d < r * 0.55) else G_MID if t < 0.85 else G_DARK
                    a[y, x] = (*col, 255)
        a[int(cy) - 2, int(cx) - 1] = (*G_LIGHT, 255)                                                 # a highlight on the head
        for k, dx in enumerate((10, 13)):                                                              # drops shaken off the tail
            x = int(round(cx - dx)); y = int(round(cy + (1.3 if k == 0 else -1.2) * math.sin(ph + k)))
            if 0 <= x < w and 0 <= y < h and not a[y, x, 3]: a[y, x] = (*(G_MID if k == 0 else G_DARK), 255)
        out.append(Image.fromarray(a))
    return out

def splat_frames(n=6, w=22, h=14):
    """the hit: a burst of acid (drops flying up and out), a puddle that spreads as a flat ellipse, then shrinks to a dark sizzling spot and fades"""
    out = []; rng = np.random.RandomState(7); drops = [(rng.uniform(-1, 1), rng.uniform(0.4, 1.0)) for _ in range(8)]
    for i in range(n):
        a = np.zeros((h, w, 4), np.uint8); cx, cy = w / 2.0 - 0.5, h - 4.0
        if i < 4:
            pr = 2.0 + 2.2 * (i + 1)
            for y in range(h):
                for x in range(w):
                    if ((x - cx) / pr) ** 2 + ((y - cy) / (pr * 0.38)) ** 2 <= 1.0:
                        e = ((x - cx) / pr) ** 2 + ((y - cy) / (pr * 0.38)) ** 2
                        a[y, x] = (*(G_BRIGHT if (i == 0 or e < 0.3) else G_MID if e < 0.8 else G_DARK), 255)
            for dx, up in drops:
                x = int(round(cx + dx * (2 + 3.2 * i))); y = int(round(cy - up * (6 - 1.7 * i)))
                if 0 <= x < w and 0 <= y < h and not a[y, x, 3]: a[y, x] = (*G_BRIGHT, 255)
        else:
            pr = 6.0 - 1.9 * (i - 3)
            for y in range(h):
                for x in range(w):
                    e = ((x - cx) / pr) ** 2 + ((y - cy) / (pr * 0.38)) ** 2
                    if e <= 1.0: a[y, x] = (*(G_DARK if i == 4 else G_DEEP), 255 if i == 4 else 140)
            if i == 4: a[int(cy) - 1, int(cx) - 2] = (*G_MID, 255); a[int(cy) - 2, int(cx) + 2] = (*G_BRIGHT, 255); a[int(cy) - 1, int(cx) + 1] = (*G_LIGHT, 255)    # sizzle bubbles
        out.append(Image.fromarray(a))
    return out

def strip(fr):
    w, h = fr[0].size; s = Image.new("RGBA", (w * len(fr), h), (0, 0, 0, 0))
    for i, f in enumerate(fr): s.alpha_composite(f, (i * w, 0))
    return s

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/pallid_attacks"; fx = os.path.join(out, "fx"); os.makedirs(fx, exist_ok=True)
    fr = [PA.render(p) for p, _ in SPIT]; ms = [t for _, t in SPIT]; mz = [muzzle(p) for p, _ in SPIT]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"spit_{i}.png"))
    strip(fr).save(os.path.join(out, "pallid_prowler_spit_strip.png"))
    G = glob_frames(); Sp = splat_frames(); strip(G).save(os.path.join(fx, "pallid_acid_glob_strip.png")); strip(Sp).save(os.path.join(fx, "pallid_acid_splat_strip.png"))
    json.dump({"pallid_prowler": {"spit": {"frame": [PA.W, PA.H], "ms": ms, "spawn_frame": SPAWN, "muzzle": [list(m) for m in mz], "ground_row": PA.GROUND},
               "acid_glob": {"frame": list(G[0].size), "frames": len(G), "ms": 70, "anchor": "the glob's head is 5 px from the right edge, trail to the left; fly it from the muzzle at about 10 tiles a second, rotate it to the aim"},
               "acid_splat": {"frame": list(Sp[0].size), "frames": len(Sp), "ms": [60, 60, 70, 80, 120, 140], "anchor": "bottom-centre on the target"}}}, open(os.path.join(out, "pallid_spit.json"), "w"), indent=1)
    # preview on ONE timeline (40 ms steps): the body plays its frames in order and only once; the glob leaves the muzzle at the start of the spawn frame and flies to a target 70 px away while the body
    # carries on to the recover frame (an earlier preview replayed the thrust pose while the glob flew, which looked like a second thrust); the splat plays on arrival.
    sc = 8; pw = PA.W + 90; ph_ = PA.H; frames = []
    def base():
        bg = Image.new("RGBA", (pw, ph_), (54, 49, 41, 255)); d = ImageDraw.Draw(bg); d.rectangle([0, PA.GROUND + 1, pw, ph_], fill=(60, 55, 46, 255)); return bg
    starts = np.concatenate([[0], np.cumsum(ms)])[:-1]; t_spawn = int(starts[SPAWN]); FLY = 280; SPL = [60, 60, 70, 80, 120, 140]; t_hit = t_spawn + FLY; t_end = max(int(sum(ms)), t_hit + sum(SPL)) + 300
    mx, my = mz[SPAWN]; tx = mx + 70
    for t in range(0, t_end, 40):
        bi = max(i for i in range(len(ms)) if starts[i] <= t) if t < sum(ms) else len(ms) - 1
        bg = base(); bg.alpha_composite(fr[bi])
        if t_spawn <= t < t_hit:
            k = (t - t_spawn) / float(FLY); x = int(mx + (tx - mx) * k); g = G[((t - t_spawn) // 40) % len(G)]; bg.alpha_composite(g, (x - g.width + 5, my - g.height // 2))
        elif t >= t_hit:
            el = t - t_hit; si = 0; acc = 0
            for j, d in enumerate(SPL):
                acc += d
                if el < acc: si = j; break
                si = j
            if el < sum(SPL): bg.alpha_composite(Sp[si], (tx - Sp[si].width // 2, PA.GROUND - Sp[si].height + 1))
        frames.append(bg)
    q = [f.resize((pw * sc, ph_ * sc), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE) for f in frames]
    q[0].save(os.path.join(out, "spit_preview.gif"), save_all=True, append_images=q[1:], duration=40, loop=0)
    sheet = Image.new("RGBA", (PA.W * 4 * sc, PA.H * 2 * sc), (105, 115, 90, 255))
    for k, f in enumerate(fr): sheet.alpha_composite(f.resize((PA.W * sc, PA.H * sc), Image.NEAREST), ((k % 4) * PA.W * sc, (k // 4) * PA.H * sc))
    sheet.convert("RGB").save(os.path.join(out, "spit_sheet.png"))
    fxs = Image.new("RGBA", ((G[0].width * len(G) + 6) * 12, (G[0].height + Sp[0].height + 4) * 12), (105, 115, 90, 255))
    t = strip(G); fxs.alpha_composite(t.resize((t.width * 12, t.height * 12), Image.NEAREST), (0, 0)); t = strip(Sp); fxs.alpha_composite(t.resize((t.width * 12, t.height * 12), Image.NEAREST), (0, (G[0].height + 2) * 12)); fxs.convert("RGB").save(os.path.join(out, "fx_sheet.png"))
    print("spit frames", len(fr), "glob frames", len(G), "splat frames", len(Sp), "muzzle", mz)
