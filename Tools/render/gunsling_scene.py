"""RT 7z-c: the Gunslinger dive scene (Yan: "make a scene where he is dual wielding pistols and shooting back at enemies while
jumping, like max payne"). Everything comes from `dotnet /tmp/test.dll gunslingframes gs.json`: a real fight frame by frame
(Rt.PlayerTick with the Gunslinger kit, two Raiders on RtAi, Gunplay rounds), the poses from Rt.PoseOf and HeldPoseMath
(Frame, Offhand). Drawn the way the view builds it: the body with its gloves and guns as one group (TacticalGridView marker ->
HeldWeaponView children), squashed and turned together, lifted; shadows on the ground; tracers from the muzzle to where each
round stopped, muzzle flashes and blood puffs from the game's own Fx sprites. Real time: bullet time plays at x0.3.
  python3 Tools/render/gunsling_scene.py gs.json out_dir   -> gunslinger_dive.gif + gunslinger_dive_strip.png"""
import json, math, os, sys
from PIL import Image, ImageDraw, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(R, "Assets/Resources/Sprites")
d = json.load(open(sys.argv[1])); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
frames = d["frames"]
world = Image.open(os.path.join(R, "Tools/render/_render/perimeter.png")).convert("RGBA")
atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA")
Z = 2                                   # canvas px per character px (a weapon px is half a character px)
X0, Y0, W, H = 75.5, 164.0, 17.0, 9.5   # the view, tiles
BG = world.crop((int(X0 * 16), int(Y0 * 16), int((X0 + W) * 16), int((Y0 + H) * 16))).resize((int(W * 16 * Z), int(H * 16 * Z)), Image.NEAREST)

def hexc(h, a=255): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, a)
def body(i): i = max(0, i); return atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16))
def glove(cuff, gl):                                       # HeldWeaponView.GloveSmall
    img = Image.new("RGBA", (4, 3)); col = {"K": hexc("#141617"), "c": hexc(cuff), "g": hexc(gl)}
    for y, row in enumerate([".KK.", "KcgK", ".KK."]):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img.resize((4 * Z, 3 * Z), Image.NEAREST)
def gun(key): return Image.open(os.path.join(SPR, f"Weapons/weapon_{key}.png")).convert("RGBA")
fx = {n: Image.open(os.path.join(SPR, f"Fx/{n}.png")).convert("RGBA") for n in ("fx_muzzle_1", "fx_hit_blood")}
def frame0(img, n=2): w = img.width // n; return img.crop((0, 0, w, img.height))

P = d["player"]; E = d["enemies"]
pbody, pglove, pgun = body(P["armor"]), glove(P["cuff"], P["glove"]), gun("PSh18Pchela")
ebody = [body(e["armor"]) for e in E]; eglove = [glove(e["cuff"], e["glove"]) for e in E]; egun = [gun(e["gun"]) for e in E]

def place_gun(layer, sprite, g, fr):
    """A gun turned round its grip point at the frame's spot (character px, body-local), flipped when aiming left."""
    gx, gy, deg, left = fr[0], fr[1], fr[2], fr[8]
    s = sprite.transpose(Image.FLIP_TOP_BOTTOM) if left else sprite
    px, py = g[0], (s.height - g[1]) if left else g[1]
    big = Image.new("RGBA", (128, 128)); big.alpha_composite(s, (int(64 - px), int(64 - py)))
    big = big.rotate(deg, resample=Image.NEAREST, center=(64, 64))
    cx, cy = 48 + Z * (gx - 8), 48 + Z * (gy - 8)
    layer.alpha_composite(big, (int(round(cx - 64)), int(round(cy - 64))))
    a = math.radians(deg); mx, my = g[4] - g[0], (g[1] - g[5]) * (-1 if left else 1)     # the muzzle, gun space (y up)
    return (cx + mx * math.cos(a) - my * math.sin(a), cy - (mx * math.sin(a) + my * math.cos(a)))

def place_glove(layer, gl, x, y, left):
    g = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
    layer.alpha_composite(g, (int(round(48 + Z * (x - 8) - g.width / 2)), int(round(48 + Z * (y - 8) - g.height / 2))))

def unit(canvas, bodyimg, guns, gloves, tx, ty, lift=0.0, tilt=0.0, sx=1.0, sy=1.0, shadow=1.0):
    """One marker: body + gloves + guns drawn body-local, then squashed, turned and lifted together. Returns muzzles (canvas px)."""
    feet = 7 / 16 * (1 - sy) * math.cos(math.radians(tilt))
    gx, gy = (tx - X0) * 16 * Z, (ty - Y0) * 16 * Z
    sw, sh = 12 * Z * shadow, 4 * Z * shadow; shy = gy + 7 * Z * math.cos(math.radians(tilt))
    ImageDraw.Draw(canvas).ellipse((gx - sw / 2, shy - sh / 2, gx + sw / 2, shy + sh / 2), fill=(50, 33, 37, 140))
    layer = Image.new("RGBA", (96, 96))
    layer.alpha_composite(bodyimg.resize((16 * Z, 16 * Z), Image.NEAREST), (32, 32))
    muzzles = [place_gun(layer, s, g, fr) for s, g, fr in guns]
    for gl, x, y, left in gloves: place_glove(layer, gl, x, y, left)
    w, h = max(1, round(96 * sx)), max(1, round(96 * sy))
    layer = layer.resize((w, h), Image.NEAREST)
    rot = layer.rotate(tilt, resample=Image.NEAREST, expand=True)
    cx, cy = gx, gy - lift * 16 * Z + feet * 16 * Z
    canvas.alpha_composite(rot, (int(round(cx - rot.width / 2)), int(round(cy - rot.height / 2))))
    out = []
    for mx, my in muzzles:                                  # body-local -> world, the same squash and turn
        lx, ly = (mx - 48) * sx, (my - 48) * sy; a = math.radians(tilt)
        out.append((cx + lx * math.cos(a) + ly * math.sin(a), cy - lx * math.sin(a) + ly * math.cos(a)))
    return out

font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
traces = []                                                 # (born game t, x0, y0, x1, y1, hit, who)
def draw(i):
    f = frames[i]; t = f["t"]; c = BG.copy()
    lift, tilt, sx, sy, sh = f["pose"]
    def fr(v): return v  # [GunX,GunY,GunDeg,GripX,GripY,SupX,SupY,HasSup,Left]
    rows = [(e[1], ("e", k, e)) for k, e in enumerate(f["en"])] + [(f["y"], ("p", 0, None))]
    muz = {}
    for _, (kind, k, e) in sorted(rows, key=lambda r: r[0]):   # y-sorted, as the game does
        if kind == "p":
            m = unit(c, pbody, [(pgun, P["g"], f["L"]), (pgun, P["g"], f["R"])],
                     [(pglove, f["L"][3], f["L"][4], f["L"][8]), (pglove, f["R"][3], f["R"][4], f["R"][8])], f["x"], f["y"], lift, tilt, sx, sy, sh)
            muz["L"], muz["R"] = m[0], m[1]
        else:
            x, y, alive, hp, efr = e
            if not alive: continue
            gl = [(eglove[k], efr[3], efr[4], efr[8])] + ([(eglove[k], efr[5], efr[6], efr[8])] if efr[7] else [])
            muz["e%d" % k] = unit(c, ebody[k], [(egun[k], E[k]["g"], efr)], gl, x, y)[0]
    dd = ImageDraw.Draw(c)
    for s in f["shots"]:
        who, ex, ey, hit = s
        if who in muz: traces.append((t, *muz[who], (ex - X0) * 16 * Z, (ey - Y0) * 16 * Z, hit, who))
    for (born, x0, y0, x1, y1, hit, who) in traces:
        age = t - born
        if age > 0.06: continue
        col = (255, 236, 170, 230) if who in "RL" else (255, 190, 140, 200)
        dd.line([(x0, y0), (x1, y1)], fill=col, width=Z)
        if age < 0.03:
            fl = frame0(fx["fx_muzzle_1"]).resize((frame0(fx["fx_muzzle_1"]).width * Z // 2 + 2, frame0(fx["fx_muzzle_1"]).height * Z // 2 + 2), Image.NEAREST)
            c.alpha_composite(fl, (int(x0 - fl.width / 2), int(y0 - fl.height / 2)))
        if hit and age < 0.05:
            b = frame0(fx["fx_hit_blood"]).resize((frame0(fx["fx_hit_blood"]).width * Z, frame0(fx["fx_hit_blood"]).height * Z), Image.NEAREST)
            c.alpha_composite(b, (int(x1 - b.width / 2), int(y1 - b.height / 2)))
    big = c.resize((c.width * 2, c.height * 2), Image.NEAREST)
    pane = Image.new("RGBA", (big.width, big.height + 34), (24, 26, 28, 255)); pane.alpha_composite(big, (0, 34))
    pd = ImageDraw.Draw(pane)
    pd.text((10, 8), "Gunslinger: left button = right-hand gun, right button = left-hand gun", font=font, fill=(235, 220, 140))
    if f["slow"]: pd.text((big.width - 130, 8), "BULLET TIME", font=font, fill=(150, 200, 255))
    return pane

# real time: bullet time (slow = 1) runs at x0.3
clock, stamps = 0.0, []
for i, f in enumerate(frames):
    stamps.append(clock); step = (frames[i + 1]["t"] - f["t"]) if i + 1 < len(frames) else 1 / 60
    clock += step / (0.3 if f["slow"] else 1.0)
rendered = [draw(i) for i in range(len(frames))]
gif, t = [], 0.0
while t <= clock:
    i = max(k for k in range(len(frames)) if stamps[k] <= t)
    gif.append(rendered[i].convert("P", palette=Image.ADAPTIVE, colors=160)); t += 1 / 30
gif[0].save(os.path.join(out, "gunslinger_dive.gif"), save_all=True, append_images=gif[1:], duration=33, loop=0, disposal=2)
keys = [0.05, 0.31, 0.43, 0.54, 0.66, 0.9, 1.36, 1.83]
cells = [rendered[min(range(len(frames)), key=lambda k: abs(frames[k]["t"] - kt))] for kt in keys]
cw, ch = cells[0].width // 2, cells[0].height // 2
strip = Image.new("RGBA", (cw * 4 + 30, ch * 2 + 10), (24, 26, 28, 255))
for n, cimg in enumerate(cells): strip.alpha_composite(cimg.resize((cw, ch), Image.LANCZOS), ((n % 4) * (cw + 10), (n // 4) * (ch + 10)))
strip.save(os.path.join(out, "gunslinger_dive_strip.png"))
print(len(gif), "gif frames,", round(clock, 2), "s real")
