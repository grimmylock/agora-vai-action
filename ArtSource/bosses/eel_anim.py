"""Animation frames of Yan's eel (eel_poses.py bends his own pixels along a path; this makes the paths move). Every animation is a list of
frames rendered at 32 px a tile on one fixed canvas, with an anchor point (where the game puts the sprite's pivot), facing LEFT as he drew
it (the game flips it for right). 15 fps. Each becomes a strip Assets/Resources/Sprites/Fx/boss_eel_<name>.png (square-free: W x H frames side
by side, under Unity's 16384 px) and an entry in boss_eel_anims.json (frames, fps, loop, frame size, anchor, segments).
  rear_idle   loop    half out of the water, swaying             anchor = the coil's centre (the pool's centre)
  lunge       once    wind-up, the head darts left, hold, recover  (right: the game flips it)
  slam        once    the head rears and crashes down in front
  hurt        once    a flinch: the head recoils, the body ripples, a white flash
  snap        once    the Hydra bite: short, fast, cracks forward and down
  reveal      once    rises out of the pool, swoops at the body, bites, gulps it down the neck (mouth points + swallow frame in the json)
  vomit       once    the body comes back up the neck and is spat out (mouth points + eject frame)
  retreat     once    sinks under the surface (its death: it runs, it is not killed)
  sweep       once    the head swings round the pool in an arc, the neck trailing (faces right, 780x540)
  every strip also has boss_eel_<name>_d1..d3: the wounds at 75/50/25 % hp as overlays on the same frames
  wrap_back / wrap_front   burst / hold (loop) / release   round a 3x3 platform: two strips, the part behind the platform and the part in
              front, cut at the anchor's row (the game draws the platform between them); anchor = the platform's centre
  swim / swim_shadow   loop   a travelling wave along the body, heading left (the shadow is the lurking one)   anchor = the head
    python3 ArtSource/bosses/eel_anim.py <sump_render.png> <depths1_render.png>   -> strips, json and review GIFs / filmstrips in maps/renders/boss_eel/
"""
import sys, os, math, json, re, uuid
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import eel_poses as E, make_eel as M

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
FX = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Fx"); OUT = os.path.join(ROOT, "maps", "renders", "boss_eel")
FPS = 15
def ease(t): t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)
def lerp(a, b, t): return a + (b - a) * t

# ------------------------------------------------------------------ half out of the water (the Sump fight), anchored at the coil's centre
REAR = dict(W=620, H=340, anchor=(450, 235))
def rear_path(head=(0.0, 0.0), sway=0.0, breathe=0.0, follow=(0.75, 0.5, 0.25, 0.0)):
    ax, ay = REAR["anchor"]; hx, hy = head
    h = (ax - 96 + hx + sway, ay - 134 + hy)
    ctrl = [(-22, -102), (-6, -58), (-32, -26), (13, -6)]
    pts = [(h[0] + E.LEAD, h[1])] + [(ax + dx + (hx + sway) * w, ay + dy + hy * w) for (dx, dy), w in zip(ctrl, follow)] + [(ax, ay)]
    neck = E.lead(h) + M.bez(*pts)[1:]
    k = 1.0 + breathe
    coil = [(ax + math.cos(t) * (76 - t * 5) * k, ay + math.sin(t) * (40 - t * 3) * k) for t in np.linspace(0, 2 * math.pi * 1.4, 130)]
    tail = M.bez(coil[-1], (ax - 192, ay + 32), (ax - 320, ay + 38))
    return M.resample(neck + coil + tail[1:])

def rear_layer(**kw): return E.eel_layer(REAR["W"], REAR["H"], rear_path(**kw), -1)

def tint(layer, white=0.0, dark=0.0):
    out = layer.copy(); m = out[:, :, 3] > 0
    rgb = out[:, :, :3].astype(float)
    if white: rgb[m] = rgb[m] * (1 - white) + 255 * white
    if dark: rgb[m] = rgb[m] * (1 - dark)
    out[:, :, :3] = rgb.clip(0, 255).astype(np.uint8); return out

def anim_rear_idle(n=12):
    return [rear_layer(sway=9 * math.sin(2 * math.pi * i / n), head=(0, 5 * math.sin(4 * math.pi * i / n)), breathe=0.03 * math.sin(2 * math.pi * i / n + 1)) for i in range(n)]

def anim_lunge():
    f = []
    for i in range(14):
        if i < 5: e = ease(i / 4); hd = (34 * e, -24 * e)                               # wind-up: the head pulls back and up
        elif i < 8: e = ease((i - 4) / 3); hd = (lerp(34, -190, e), lerp(-24, 30, e))    # the strike: it darts left and down
        elif i < 10: hd = (-190 + (i % 2) * 3, 30)                                       # hold
        else: e = ease((i - 9) / 4); hd = (-190 * (1 - e), 30 * (1 - e))                 # recover
        f.append(rear_layer(head=hd, follow=(0.8, 0.6, 0.35, 0.05)))
    return f

def anim_slam():
    f = []
    for i in range(10):
        if i < 4: e = ease(i / 3); hd = (20 * e, -40 * e)
        elif i < 6: e = ease((i - 3) / 2); hd = (lerp(20, -46, e), lerp(-40, 104, e))   # it crashes down in front of the coil
        elif i < 8: hd = (-46, 104)
        else: e = ease((i - 7) / 2); hd = (-46 * (1 - e), 104 * (1 - e))
        f.append(rear_layer(head=hd, follow=(0.8, 0.6, 0.4, 0.1)))
    return f

def anim_hurt():
    d = [1.0, 1.0, 0.7, 0.45, 0.25, 0.12, 0.05, 0.0]
    return [tint(rear_layer(head=(26 * d[i], -14 * d[i]), breathe=0.05 * d[i]), white=(0.55 if i < 2 else 0.2 if i == 2 else 0.0)) for i in range(8)]

MOUTHS = {}          # per animation: the mouth's position in every frame, relative to the anchor (the game puts the body there)
WATERLINE = 14       # rows below the anchor where the pool's surface cuts the body in the reveal and the retreat
def shifted(layer, dy, clip):
    """the layer moved down dy px, everything below row `clip` cut off (it is under the water)"""
    out = np.zeros_like(layer); H = layer.shape[0]; dy = int(dy)
    if dy < H: out[dy:] = layer[:H - dy]
    out[clip:] = 0; return out

def rear_mouth(name, **kw):
    P = rear_path(**kw); m = E.mouth_at(P[0], -1); MOUTHS.setdefault(name, []).append([round(m[0] - REAR["anchor"][0], 1), round(m[1] - REAR["anchor"][1], 1)])

BODY = (0.0, -176.0)        # where the body lies, relative to the pool's centre: just north of the pool
BITE = (BODY[0] - E.MOUTH[0] + 96, BODY[1] - E.MOUTH[1] + 134)    # the head offset that puts the mouth on it
def rpose(name, i, **kw):
    rear_mouth(name, **kw); return rear_layer(**kw)

def anim_snap():
    """Hydra's bite: short and fast, a quick pull-back and the head cracks forward and down, then it draws back (shorter than the lunge)"""
    f = []
    for i in range(10):
        if i < 2: e = ease((i + 1) / 2); hd = (22 * e, -14 * e)
        elif i < 4: e = ease((i - 1) / 2); hd = (lerp(22, -104, e), lerp(-14, 58, e))
        elif i < 5: hd = (-104, 58)
        else: e = ease((i - 4) / 5); hd = (-104 * (1 - e), 58 * (1 - e))
        f.append(rpose("snap", i, head=hd, follow=(0.8, 0.6, 0.35, 0.05)))
    return f

def anim_reveal():
    """out of the pool: it rises through the surface, swoops at the body, bites, gulps it down the neck, and settles into the rear-idle pose"""
    f = []; MOUTHS["reveal"] = []
    for i in range(8): e = ease((i + 1) / 8); f.append(shifted(rear_layer(head=(0, 0)), (1 - e) * 250, REAR["anchor"][1] + WATERLINE)); rear_mouth("reveal", head=(0, 0))
    for i in range(5): e = ease((i + 1) / 5); hd = (BITE[0] * e, BITE[1] * e); f.append(rpose("reveal", i, head=hd, follow=(0.8, 0.6, 0.4, 0.1)))
    for i in range(2): f.append(rpose("reveal", i, head=(BITE[0] + 3 * (i % 2), BITE[1]), follow=(0.8, 0.6, 0.4, 0.1)))     # the bite: the body is gone from here (swallow frame 13)
    for i in range(8):
        e = ease((i + 1) / 8); hd = (BITE[0] * (1 - e), BITE[1] * (1 - e)); c = LEAD_ + 190 * e
        P = rear_path(head=hd, follow=(0.8, 0.6, 0.4, 0.1)); rear_mouth("reveal", head=hd, follow=(0.8, 0.6, 0.4, 0.1))
        f.append(E.eel_layer(REAR["W"], REAR["H"], P, -1, bulge=(c, 16, 0.55 * (1 - e * 0.4))))
    for i in range(3): f.append(rpose("reveal", i, head=(0, 0)))
    return f

def anim_vomit(n=24):
    """the body comes back up the neck as a lump, it rears back, thrusts and spits it out in front of the pool, then droops"""
    f = []; MOUTHS["vomit"] = []
    for i in range(n):
        if i < 8: e = ease((i + 1) / 8); hd = (10 * e, 4 * e); c = LEAD_ + 190 * (1 - e); amt = 0.55
        elif i < 12: e = ease((i - 7) / 4); hd = (lerp(10, 36, e), lerp(4, -36, e)); c = LEAD_ + 10 * (1 - e); amt = 0.55
        elif i < 14: e = ease((i - 11) / 2); hd = (lerp(36, BITE[0], e), lerp(-36, BITE[1], e)); c = LEAD_; amt = 0.55 * (1 - e)
        elif i < 17: hd = (BITE[0] + (i % 2) * 2, BITE[1]); c = LEAD_; amt = 0.0
        else: e = ease((i - 16) / 7); hd = (BITE[0] * (1 - e) , BITE[1] * (1 - e) + 14 * math.sin(math.pi * e)); c = LEAD_; amt = 0.0
        kw = dict(head=hd, follow=(0.8, 0.6, 0.4, 0.1)); rear_mouth("vomit", **kw)
        f.append(E.eel_layer(REAR["W"], REAR["H"], rear_path(**kw), -1, bulge=(c, 16, amt) if amt else None))
    return f

def anim_retreat(n=14):
    """done and beaten: the head dips and the whole body sinks under the surface (the pool is dark below: the clip hides it)"""
    return [shifted(tint(rear_layer(head=(-30 * ease(i / 6), 40 * ease(i / 6)), breathe=-0.1 * ease(i / 8), follow=(0.9, 0.8, 0.5, 0.1)), dark=0.25 * ease(i / n)), ease(i / (n - 1)) * 300, REAR["anchor"][1] + WATERLINE) for i in range(n)]

# the circular sweep: the head swings round the pool in an arc over its far side, the neck trailing behind it like a flail
SW = dict(W=780, H=540, anchor=(390, 410))
def sweep_path(th, lean=1.0, R=215.0, direction=1):
    ax, ay = SW["anchor"]; hx, hy = ax + math.cos(th) * R, ay + math.sin(th) * R
    tx, ty = -math.sin(th) * direction, math.cos(th) * direction           # the head's direction of travel
    c1 = (hx - tx * 70 * lean + (ax - hx) * 0.2, hy - ty * 70 * lean + (ay - hy) * 0.2); c2 = (hx + (ax - hx) * 0.65 - tx * 40 * lean, hy + (ay - hy) * 0.65 - ty * 40 * lean)
    west = direction > 0 and False
    lead = [(hx - k, hy) for k in range(E.LEAD)] if direction > 0 else [(hx + k, hy) for k in range(E.LEAD)]
    nk = lead[-1]; neck = lead + M.bez(nk, c1, c2, (ax, ay))[1:]
    t = np.linspace(0, 2 * math.pi * 1.4, 130); coil = [(ax + math.cos(a) * (76 - a * 5), ay + math.sin(a) * (40 - a * 3)) for a in t]
    tail = M.bez(coil[-1], (ax - 192, ay + 32), (ax - 320, ay + 38))
    return M.resample(neck + coil + tail[1:])

def anim_sweep(n=18):
    f = []; MOUTHS["sweep"] = []
    for i in range(n):
        e = ease(i / (n - 1)); th = lerp(math.pi + 0.5, 2 * math.pi - 0.5, e)         # left of the pool, over the top, to the right
        P = sweep_path(th, direction=1); f.append(E.eel_layer(SW["W"], SW["H"], P, 1))
        m = E.mouth_at(P[0], 1); MOUTHS["sweep"].append([round(m[0] - SW["anchor"][0], 1), round(m[1] - SW["anchor"][1], 1)])
    return f

LEAD_ = E.LEAD

# ------------------------------------------------------------------ wrapped round a 3x3 platform, anchored at the platform's centre
WRAP = dict(W=560, H=420, anchor=(350, 215))
def wrap_path(tight=1.0, rise=0.0, visible=1.0):
    ax, ay = WRAP["anchor"]; rx, ry = 48 + 36, 48 + 31
    ring = [(ax + math.cos(t) * (rx + t * 4.0) * tight, ay + math.sin(t) * (ry + t * 3.4) * tight) for t in np.linspace(-0.35, -0.35 + 2 * math.pi * 1.5, 150)]
    h0 = (ax + 57.6, ay - 106 + rise)
    neck = E.lead(h0) + M.bez((h0[0] + E.LEAD, h0[1]), (h0[0] + E.LEAD + 40, h0[1] + 6), ring[0])[1:]
    tail = M.bez(ring[-1], (ax - 128, ay), (ax - 256, ay - 42), (ax - 298, ay + 74), (ax - 176, ay + 125), (ax + 80, ay + 106))
    P = M.resample(neck + ring + tail[1:]); return P[:max(E.LEAD + 40, int(len(P) * visible))]

def wrap_layer(**kw): return E.eel_layer(WRAP["W"], WRAP["H"], wrap_path(**kw), -1)

def anim_wrap():
    """burst (8), hold (6, loops), release (8): returns the full layers and the segment table"""
    f = []
    for i in range(8): e = ease((i + 1) / 8); f.append(wrap_layer(tight=lerp(1.45, 1.0, e), rise=lerp(46, 0, e), visible=lerp(0.3, 1.0, e)))
    for i in range(6):
        p = math.sin(2 * math.pi * i / 6); f.append(wrap_layer(tight=1.0 - 0.035 * (0.5 + 0.5 * p), rise=-4 * p))        # the electricity is added in main
    for i in range(8): e = ease((i + 1) / 8); f.append(wrap_layer(tight=lerp(1.0, 1.5, e), rise=lerp(0, 46, e), visible=lerp(1.0, 0.25, e)))
    return f, {"burst": [0, 7], "hold": [8, 13], "release": [14, 21]}

def zap_layer(layer, P, seed):
    """electricity crawling along the coils (the hold frames): his one accent colour, a few jagged arcs, a different set every frame"""
    im = Image.fromarray(layer); a = np.array(M.zap(np.array(im), [tuple(p) for p in P], seed, 5)); return a

# ------------------------------------------------------------------ swimming (a travelling wave), anchored at the head
SWIM = dict(W=900, H=240, anchor=(140, 120))
def anim_swim(n=16, length=780, amp=36, lam=260):
    ax, ay = SWIM["anchor"]; f = []
    for i in range(n):
        ph = i / n; pts = []
        for s in range(0, length, 3):
            env = min(1.0, 0.25 + s / 220.0)
            pts.append((ax + E.LEAD + s, ay + amp * env * math.sin(2 * math.pi * (s / lam - ph))))
        P = M.resample(E.lead((ax, ay)) + M.bez(*pts[::8] + [pts[-1]], n=24)[1:])
        f.append(E.eel_layer(SWIM["W"], SWIM["H"], P, -1))
    return f

# ------------------------------------------------------------------ damage: the same frames re-rendered at stage 1..3 (75 / 50 / 25 % hp), kept as overlays
DAMAGE_AT = [0.75, 0.5, 0.25]
def overlay_set(name, fn, base, entry, make=None):
    """overlay strips boss_eel_<name>_d1..d3: only the pixels that differ from the clean frame, same size and frame order. The game draws the base strip and
    the overlay of the stage it has reached (each already holds every earlier wound), so nothing is stored twice but the wounds"""
    entry["damage"] = dict(at=DAMAGE_AT, files=[])
    for st in (1, 2, 3):
        E.set_stage(st); fr = fn(); E.set_stage(0); ov = []
        for b, d in zip(base, fr):
            diff = (b != d).any(axis=2); o = np.zeros_like(d); o[diff] = d[diff]; ov.append(o)
        write_strip(f"{name}_d{st}", ov, {}); entry["damage"]["files"].append(f"boss_eel_{name}_d{st}")

# ------------------------------------------------------------------ export
def strip(frames): return np.concatenate(frames, axis=1)

def write_strip(name, frames, meta):
    H, W = frames[0].shape[:2]; assert W * len(frames) <= 16384, (name, W * len(frames))
    Image.fromarray(strip(frames)).save(os.path.join(FX, f"boss_eel_{name}.png"))
    tpl = os.path.join(FX, "fx_hit_spark.png.meta"); dst = os.path.join(FX, f"boss_eel_{name}.png.meta")
    if not os.path.exists(dst):
        m = open(tpl).read(); m = re.sub(r"guid: \w+", "guid: " + uuid.uuid4().hex, m, count=1)
        m = re.sub(r"maxTextureSize: \d+", "maxTextureSize: 16384", m); m = re.sub(r"textureCompression: \d+", "textureCompression: 0", m)
        open(dst, "w").write(m)
    return dict(meta, file=f"boss_eel_{name}", frames=len(frames), fps=FPS, frame=[W, H])

def gif(path, bgs, frames_pos, loop_ms=1000 // FPS):
    imgs = [f.convert("P", palette=Image.ADAPTIVE, colors=200) for f in bgs]
    imgs[0].save(path, save_all=True, append_images=imgs[1:], duration=loop_ms, loop=0, disposal=2, optimize=True)

if __name__ == "__main__":
    sump, l1 = sys.argv[1], sys.argv[2]; os.makedirs(OUT, exist_ok=True); anims = {}
    ar = Image.open(os.path.join(ROOT, "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA"); man = ar.crop((0, 0, 16, 16)).resize((32, 32), Image.NEAREST)
    # backgrounds: the Sump at game scale round the pool, and a Level 1 basin with a stand-in platform for the wrap
    S = Image.open(sump).convert("RGBA"); S = S.resize((S.width * 2, S.height * 2), Image.NEAREST)
    PX, PY = int(30.0 * 32), int(24.5 * 32); VX0, VY0, VW, VH = PX - 480, PY - 400, 960, 640          # the view: the pool in the middle
    def on_sump(layer, ax_, ay_, player=(22, 21), extra=None):
        bg = S.crop((VX0, VY0, VX0 + VW, VY0 + VH)); bg.alpha_composite(man, (player[0] * 32 - VX0, player[1] * 32 - VY0))
        bg.alpha_composite(Image.fromarray(layer), (PX - ax_ - VX0, PY - ay_ - VY0)); return bg.convert("RGB")
    ax, ay = REAR["anchor"]
    SPECS = (("rear_idle", anim_rear_idle, True, REAR), ("lunge", anim_lunge, False, REAR), ("snap", anim_snap, False, REAR), ("slam", anim_slam, False, REAR), ("hurt", anim_hurt, False, REAR),
             ("reveal", anim_reveal, False, REAR), ("vomit", anim_vomit, False, REAR), ("retreat", anim_retreat, False, REAR), ("sweep", anim_sweep, False, SW))
    for name, fn, loop, cv in SPECS:
        E.set_stage(0); fr = fn(); a_ = cv["anchor"]
        anims[name] = write_strip(name, fr, dict(loop=loop, anchor=list(a_), faces="left" if cv is REAR else "right"))
        if name in MOUTHS: anims[name]["mouth"] = MOUTHS[name]
        overlay_set(name, fn, fr, anims[name])
        if name in ("lunge", "snap", "sweep"): gif(os.path.join(OUT, f"anim_{name}.gif"), [on_sump(l, a_[0], a_[1]) for l in fr], None)
        print(name, len(fr), "frames")
    anims["reveal"]["swallow_frame"] = 13; anims["vomit"]["eject_frame"] = 13
    # the wrap: back and front strips, cut at the anchor's row; the review puts them either side of a stand-in platform
    wf, seg = anim_wrap()
    for i in range(8, 14): wf[i] = zap_layer(wf[i], wrap_path()[40:420], 100 + i)
    wax, way = WRAP["anchor"]; back = []; front = []
    for l in wf:
        b = l.copy(); b[way:] = 0; fr_ = l.copy(); fr_[:way] = 0; back.append(b); front.append(fr_)
    anims["wrap_back"] = write_strip("wrap_back", back, dict(loop=False, anchor=[wax, way], faces="left", segments=seg, loops="hold"))
    anims["wrap_front"] = write_strip("wrap_front", front, dict(loop=False, anchor=[wax, way], faces="left", segments=seg, loops="hold"))
    B = Image.open(l1).convert("RGBA"); B = B.resize((B.width * 2, B.height * 2), Image.NEAREST); plat = Image.fromarray(M.platform(96, 96))
    cx0, cy0 = 25 * 32, 20 * 32
    def wrap_frame(i):
        bg = B.crop((cx0 - 480, cy0 - 330, cx0 + 480, cy0 + 310)); ox, oy = 480 - wax, 330 - way
        bg.alpha_composite(Image.fromarray(back[i]), (ox, oy)); bg.alpha_composite(plat, (480 - 48, 330 - 48 - 6)); bg.alpha_composite(man, (480 - 16, 330 - 26)); bg.alpha_composite(Image.fromarray(front[i]), (ox, oy))
        return bg.convert("RGB")
    order = list(range(0, 8)) + list(range(8, 14)) * 2 + list(range(14, 22)); gif(os.path.join(OUT, "anim_wrap.gif"), [wrap_frame(i) for i in order], None); print("wrap", len(wf), "frames")
    sw = anim_swim(); anims["swim"] = write_strip("swim", sw, dict(loop=True, anchor=list(SWIM["anchor"]), faces="left"))
    anims["swim_shadow"] = write_strip("swim_shadow", [E.shadow(l) for l in sw], dict(loop=True, anchor=list(SWIM["anchor"]), faces="left"))
    def swim_frame(i):
        bg = S.crop((VX0, VY0, VX0 + VW, VY0 + VH)).copy(); bg.alpha_composite(Image.fromarray(E.shadow(sw[i])), (PX + 360 - SWIM["anchor"][0] - VX0, PY + 60 - SWIM["anchor"][1] - VY0)); return bg.convert("RGB")
    print("swim", len(sw), "frames")
    json.dump(anims, open(os.path.join(FX, "boss_eel_anims.json"), "w"), indent=1); print("json", list(anims))
    # filmstrip sheets (every frame, over the dark water) for review: the rear animations, the lunge, the death, the wrap with a stand-in platform, the swim
    from PIL import ImageDraw
    def sheet(groups, cols, scale, out, plat_at=None):
        tiles = []
        for nm, lab in groups:
            for i, l in enumerate(lab): tiles.append((f"{nm} {i}", l))
        h_, w_ = tiles[0][1].shape[:2]; w, h = int(w_ * scale), int(h_ * scale); rows = (len(tiles) + cols - 1) // cols
        sh = Image.new("RGB", (cols * w, rows * h), (14, 20, 18)); d = ImageDraw.Draw(sh)
        for k, (t, l) in enumerate(tiles):
            im = Image.new("RGBA", (w_, h_), (14, 20, 18, 255)); im.alpha_composite(Image.fromarray(l)); sh.paste(im.resize((w, h), Image.NEAREST).convert("RGB"), ((k % cols) * w, (k // cols) * h)); d.text(((k % cols) * w + 3, (k // cols) * h + 2), t, fill=(230, 220, 190))
        sh.save(os.path.join(OUT, out)); return sh.size
    sheet([("rear_idle", anim_rear_idle())], 6, 0.33, "sheet_rear_idle.png"); sheet([("hurt", anim_hurt()), ("slam", anim_slam())], 6, 0.33, "sheet_hurt_slam.png")
    sheet([("lunge", anim_lunge())], 4, 0.5, "sheet_lunge.png"); sheet([("snap", anim_snap())], 5, 0.5, "sheet_snap.png"); sheet([("reveal", anim_reveal())], 6, 0.4, "sheet_reveal.png"); sheet([("vomit", anim_vomit())], 6, 0.4, "sheet_vomit.png"); sheet([("retreat", anim_retreat())], 5, 0.4, "sheet_retreat.png"); sheet([("sweep", anim_sweep())], 5, 0.4, "sheet_sweep.png"); sheet([("swim", sw)], 4, 0.45, "sheet_swim.png")
    wsh = []
    for i in range(len(back)):
        t = np.zeros_like(back[i]); pl = np.array(plat.convert("RGBA")); t[:] = back[i]
        im = Image.fromarray(back[i]); im.alpha_composite(plat, (wax - 48, way - 48 - 6)); im.alpha_composite(Image.fromarray(front[i])); wsh.append(np.array(im))
    sheet([("wrap", wsh)], 6, 0.42, "sheet_wrap.png")
