"""Animation frames of Yan's eel (eel_poses.py bends his own pixels along a path; this makes the paths move). Every animation is a list of
frames rendered at 32 px a tile on one fixed canvas, with an anchor point (where the game puts the sprite's pivot), facing LEFT as he drew
it (the game flips it for right). 15 fps. Each becomes a strip Assets/Resources/Sprites/Fx/boss_eel_<name>.png (square-free: W x H frames side
by side, under Unity's 16384 px) and an entry in boss_eel_anims.json (frames, fps, loop, frame size, anchor, segments).
  rear_idle   loop    half out of the water, swaying             anchor = the coil's centre (the pool's centre)
  lunge       once    wind-up, the head darts left, hold, recover  (right: the game flips it)
  slam        once    the head rears and crashes down in front
  hurt        once    a flinch: the head recoils, the body ripples, a white flash
  die         once    the head drops, the coil slackens, it goes dark
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

def anim_die(n=18):
    f = []
    for i in range(n):
        e = ease(i / 12)
        f.append(tint(rear_layer(head=(-122 * e, 152 * e), breathe=-0.22 * e, follow=(0.9, 0.8, 0.5, 0.1)), dark=0.25 * ease((i - 8) / 9)))
    return f

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
    for name, fr, loop in (("rear_idle", anim_rear_idle(), True), ("lunge", anim_lunge(), False), ("slam", anim_slam(), False), ("hurt", anim_hurt(), False), ("die", anim_die(), False)):
        anims[name] = write_strip(name, fr, dict(loop=loop, anchor=[ax, ay], faces="left"))
        if name == "lunge": gif(os.path.join(OUT, f"anim_{name}.gif"), [on_sump(l, ax, ay) for l in fr], None)          # GIFs only for the key ones (11 MB for all)
        print(name, len(fr), "frames")
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
    sheet([("lunge", anim_lunge())], 4, 0.5, "sheet_lunge.png"); sheet([("die", anim_die())], 5, 0.4, "sheet_die.png"); sheet([("swim", sw)], 4, 0.45, "sheet_swim.png")
    wsh = []
    for i in range(len(back)):
        t = np.zeros_like(back[i]); pl = np.array(plat.convert("RGBA")); t[:] = back[i]
        im = Image.fromarray(back[i]); im.alpha_composite(plat, (wax - 48, way - 48 - 6)); im.alpha_composite(Image.fromarray(front[i])); wsh.append(np.array(im))
    sheet([("wrap", wsh)], 6, 0.42, "sheet_wrap.png")
