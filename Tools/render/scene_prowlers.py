"""2026-11-23, trailer scene 1 (Yan: "you can use the Blind Maws if the Pallid Prowlers is hard" / "2" = choreograph): the starting character against three Blind Maws in the Chapel Crypt's long corridor, on the real map.
CHOREOGRAPHED: the positions, the timing of the charges, bites and deaths are scripted here (the real AI stalled in the harness: see WORK_LOG 9dh); what is real is the map render, every sprite (the Maws' own run/bite/pounce strips, the character, the
pistol drawn from the game's pose code, the new walk/run motion), the strips' frame timings (MutantAttackData) and the carry/aim logic. Not Unity footage.
  dotnet /tmp/test.dll sceneprowlers /tmp/prowlers.json ; python3 Tools/render/scene_prowlers.py /tmp/prowlers.json out_dir -> scene_prowlers.gif / .mp4"""
import json, math, os, random, subprocess, sys
from PIL import Image, ImageDraw, ImageFont
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))); SPR = os.path.join(R, "Assets/Resources/Sprites")
d = json.load(open(sys.argv[1])); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
Z = 3; VW, VH = 14.0, 7.9; OW, OH = int(VW * 16 * Z * 2), int(VH * 16 * Z * 2); FPS = 30; DUR = 11.0
world = Image.open(os.path.join(R, "Tools/render/_render/chapel_crypt_noroof.png")).convert("RGBA")
def hexc(h, a=255): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, a)
def cell(n, ai): im = Image.open(os.path.join(SPR, n)).convert("RGBA"); return im.crop(((ai % 16) * 16, (ai // 16) * 16, (ai % 16) * 16 + 16, (ai // 16) * 16 + 16))
ai = d["armor"]; front = {"s": cell("armor_atlas.png", ai), "a": cell("armor_walk_a.png", ai), "b": cell("armor_walk_b.png", ai)}
back = {"s": cell("armor_back_atlas.png", ai), "a": cell("armor_back_walk_a.png", ai), "b": cell("armor_back_walk_b.png", ai)}
hw = open(os.path.join(R, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
import re
def tab(name, key, default):
    m = re.search(r'\["%s"\] = ([0-9.]+)f' % key, hw.split(name + " = new")[1].split("};")[0]); return float(m.group(1)) if m else default
k_ = tab("Dense", d["key"], 1.0); gun_hd = Image.open(os.path.join(SPR, f"Weapons/weapon_{d['key']}_hd.png")).convert("RGBA")
gun = gun_hd.resize((max(1, round(gun_hd.width / k_)), max(1, round(gun_hd.height / k_))), Image.LANCZOS) if k_ > 1 else Image.open(os.path.join(SPR, f"Weapons/weapon_{d['key']}.png")).convert("RGBA")
def glove(cuff, gl):
    g = hexc(gl); lit = tuple(min(255, int(v * 1.45)) for v in g[:3]) + (255,); dark = tuple(int(v * 0.65) for v in g[:3]) + (255,)
    img = Image.new("RGBA", (4, 3)); col = {"K": hexc("#141617"), "c": hexc(cuff), "g": g}
    for y, row in enumerate([".KK.", "KcgK", ".KK."]):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), col[ch])
    return img.resize((4 * Z, 3 * Z), Image.NEAREST)
GL = glove(d["cuff"], d["glove"])
def strip(n, frames):
    im = Image.open(os.path.join(SPR, f"Mutants/anim/blind_maw_{n}.bytes")).convert("RGBA"); w = im.width // frames; return [im.crop((i * w, 0, i * w + w, im.height)) for i in range(frames)]
RUN, WALK = strip("run", 8), strip("walk", 8); ATK = {k: strip(k, v["frames"]) for k, v in d["anims"].items()}
def fx(n, k=3): im = Image.open(os.path.join(SPR, f"Fx/{n}.png")).convert("RGBA"); w = im.width // k; return [im.crop((i * w, 0, i * w + w, im.height)) for i in range(k)]
BLOOD, SPARK, POOL = fx("fx_hit_blood"), fx("fx_hit_spark"), fx("fx_blood_pool", 5)
def tint(img, col, k):
    im = img.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a: px[x, y] = (int(r + (col[0] - r) * k), int(g + (col[1] - g) * k), int(b + (col[2] - b) * k), a)
    return im
# ---------- the script ----------
CX = 19.5
def lerp(a, b, u): return a + (b - a) * max(0.0, min(1.0, u))
def player(t):
    """-> y, state ('run','aim','walkaim','hold'), moving, facing front?"""
    if t < 1.8: return 9.0 + 4.4 * t, "run", True
    if t < 3.1: return 16.9, "aim", False
    if t < 4.0: y = lerp(16.9, 15.6, (t - 3.1) / 0.9); return y, "walkaim", True
    if t < 4.6: return 15.6, "aim", False
    if t < 5.6: y = lerp(15.6, 14.0, (t - 4.6) / 1.0); return y, "walkaim", True
    if t < 7.6: return 14.0, "aim", False
    return lerp(14.0, 14.0, 0), "carry", False
# each maw: (spawn y, spawn time, arrive t, attack kind, attack start, death t)
MAWS = [dict(y0=30.5, t0=0.6, run_to=19.9, tr=2.7, kind="bite", ta=2.7, dead=4.2, dx=-0.3),
        dict(y0=33.0, t0=1.0, run_to=19.9, tr=4.6, kind="pounce", ta=4.2, dead=5.9, dx=0.35),
        dict(y0=35.5, t0=1.4, run_to=17.9, tr=6.3, kind="bite2", ta=6.3, dead=8.2, dx=0.0)]
def maw_pos(m, t):
    if t < m["t0"]: return None
    run_speed = 5.6; yrun = max(m["run_to"], m["y0"] - run_speed * (t - m["t0"]))
    return CX + m["dx"], yrun
shots = []                                  # (t, mawindex)
for i, (a, b, m) in enumerate([(2.35, 4.2, 0), (4.35, 5.9, 1), (6.0, 8.2, 2)]):
    n = int((b - a) / 0.17)
    for k in range(n): shots.append((a + k * 0.17, m))
def at(t, ms):                              # frame index for a clip with per-frame ms
    tm = t * 1000.0; acc = 0
    for i, v in enumerate(ms):
        acc += v
        if tm < acc: return i
    return len(ms) - 1
parts = []
def puff(x, y, frames_, col, t, life, scale=2.2): parts.append((x, y, frames_, col, t, life, scale))
bites = [(2.7 + 0.355, 0), (4.2 + 0.65 + 0.0, 1), (6.3 + 0.495, 2)]
pl_hp = lambda t: 1000 - 90 * sum(1 for (tb, _) in bites if t >= tb)
cam_y = [10.0]
def to_px(x, y, cy): return (x - (CX - VW / 2)) * 16 * Z, (y - (cy - VH / 2)) * 16 * Z
def draw(i):
    t = i / FPS; py_, pst, moving = player(t)
    live = [maw_pos(m, t)[1] for m in MAWS if maw_pos(m, t) is not None and t < m['dead']]; near = min(live) if live else py_
    tgt = (py_ + min(near, py_ + 5.0)) / 2 + 0.2; cam_y[0] += (tgt - cam_y[0]) * 0.12; cy = cam_y[0]
    x0, y0 = (CX - VW / 2) * 16, (cy - VH / 2) * 16
    c = world.crop((int(round(x0)), int(round(y0)), int(round(x0)) + int(VW * 16), int(round(y0)) + int(VH * 16))).resize((int(VW * 16 * Z), int(VH * 16 * Z)), Image.NEAREST)
    dd = ImageDraw.Draw(c)
    # y-sorted bodies
    items = []
    # the player
    pxp, pyp = to_px(CX, py_, cy)
    def draw_player():
        walking = moving; spd = 4.4 if pst == "run" else 2.0
        stride = sum([0]) ; q = int(t * spd * (0.59 if pst == "run" else 0.5) * 4) % 4 if walking else 1
        facing_back = (pst == "run" and False) or (pst == "walkaim" and False)
        cells = front; body = cells["s"] if q in (1, 3) or not walking else (cells["a"] if q == 0 else cells["b"]); hop = 1 / 16 if walking and q in (0, 2) else 0
        aimed = pst in ("aim", "walkaim"); frame = d["frames"][0]["fa"] if aimed else d["frames"][0]["fc"]
        gx, gy, deg, hx, hy, sx, sy, sup, left = frame
        dx = dyl = 0.0
        if not aimed and walking:                                     # the carry swing (WeaponCarry.Pistol, a run): +-2 px, 1 up, 7 degrees
            phase = t * 4.4 * 2 * math.pi / 2.03; s1 = math.sin(phase); dx = round(s1 * 2); dyl = round(s1 * s1 * 1) ; deg += -s1 * 7
        elif aimed and walking: dyl = -(1 if math.sin(t * 2.0 * 2 * math.pi / 2.4 * 2) > 0.2 else 0)
        layer = Image.new("RGBA", (144, 144)); layer.alpha_composite(body.resize((16 * Z, 16 * Z), Image.NEAREST), (72 - 8 * Z, 72 - 8 * Z))
        s_ = gun.transpose(Image.FLIP_TOP_BOTTOM) if left else gun; s_ = s_.resize((s_.width * Z // 2, s_.height * Z // 2), Image.NEAREST)
        gpx, gpy = d["grip"][0] * Z / 2, ((gun.height - d["grip"][1]) if left else d["grip"][1]) * Z / 2
        big = Image.new("RGBA", (240, 240)); big.alpha_composite(s_, (int(120 - gpx), int(120 - gpy))); big = big.rotate(deg, resample=Image.NEAREST, center=(120, 120))
        layer.alpha_composite(big, (int(round(72 + Z * (gx + dx - 8) - 120)), int(round(72 + Z * (gy - dyl - 8) - 120))))
        gl = GL.transpose(Image.FLIP_LEFT_RIGHT) if left else GL
        layer.alpha_composite(gl, (int(round(72 + Z * (hx + dx - 8) - gl.width / 2)), int(round(72 + Z * (hy - dyl - 8) - gl.height / 2))))
        if sup: layer.alpha_composite(gl, (int(round(72 + Z * (sx + dx - 8) - gl.width / 2)), int(round(72 + Z * (sy - dyl - 8) - gl.height / 2))))
        lean = 0.0
        rot = layer.rotate(lean, resample=Image.NEAREST, expand=True)
        sh = Image.new("RGBA", c.size); ImageDraw.Draw(sh).ellipse((pxp - 6 * Z, pyp + 5 * Z, pxp + 6 * Z, pyp + 9 * Z), fill=(20, 18, 16, 120)); c.alpha_composite(sh)
        c.alpha_composite(rot, (int(round(pxp - rot.width / 2)), int(round(pyp - hop * 16 * Z - rot.height / 2))))
    items.append((py_, draw_player))
    for mi, m in enumerate(MAWS):
        pos = maw_pos(m, t)
        if pos is None: continue
        mx, my = pos
        def draw_maw(m=m, mi=mi, mx=mx, my=my):
            dead = t >= m["dead"]; sx_, sy_ = to_px(mx, my, cy)
            if dead:
                age = t - m["dead"]; fr = ATK[m["kind"]][-1] if m["kind"] in ATK else RUN[0]
                pool = POOL[min(4, int(age / 0.12))].resize((20 * Z, 14 * Z), Image.NEAREST); c.alpha_composite(pool, (int(sx_ - pool.width / 2), int(sy_ + 3 * Z - pool.height / 2)))
                return
            ta = t - m["ta"]; kind = m["kind"]
            if t >= m["tr"] - 0.0 and ta >= 0 and ta * 1000 < sum(d["anims"][kind]["ms"]): fr = ATK[kind][at(ta, d["anims"][kind]["ms"])]; piv = (fr.width / 2, fr.height * (1 - d["anims"][kind]["pivot"]))
            else: fr = RUN[int((t - m["t0"]) / 0.08) % 8]; piv = (fr.width / 2, fr.height - 16)
            fl = max(0.0, 1 - (t - hurt_t(mi)) / 0.15) * 0.7
            if fl > 0: fr = tint(fr, (255, 70, 60), fl)
            sp = fr.resize((max(1, fr.width * Z // 2), max(1, fr.height * Z // 2)), Image.NEAREST)               # the game flips them left/right only: head right here (the corridor runs up and down)
            c.alpha_composite(sp, (int(sx_ - piv[0] * Z / 2), int(sy_ - piv[1] * Z / 2)))
        items.append((my, draw_maw))
    for _, fn in sorted(items, key=lambda r: r[0]): fn()
    # shots: tracer, muzzle, hit
    for (ts, mi) in shots:
        age = t - ts
        if 0 <= age < 0.07:
            m = MAWS[mi]; pos = maw_pos(m, ts)
            if pos is None or ts >= m["dead"]: continue
            tx, ty = to_px(pos[0], pos[1], cy); mz = (pxp + 6 * Z, pyp + 2 * Z)
            dd.line((mz[0], mz[1], tx, ty), fill=(255, 236, 160, 255), width=2); dd.ellipse((mz[0] - 6, mz[1] - 4, mz[0] + 6, mz[1] + 8), fill=(255, 220, 120, 230))
            if age < 0.03: puff(pos[0], pos[1], BLOOD, (180, 30, 40), ts, 0.2, 2.2)
    keep = []
    for (x, y, frs, col, born, life, sc) in parts:
        age = t - born
        if 0 <= age < life:
            keep.append((x, y, frs, col, born, life, sc)); fr = frs[min(len(frs) - 1, int(age / life * len(frs)))]; px_, py2 = to_px(x, y, cy)
            im = tint(fr.resize((int(fr.width * Z * sc / 2), int(fr.height * Z * sc / 2)), Image.NEAREST), col, 1.0); c.alpha_composite(im, (int(px_ - im.width / 2), int(py2 - im.height / 2)))
    parts[:] = keep
    for (tb, mi) in bites:
        if 0 <= t - tb < 0.2:
            m = MAWS[mi]; pos = maw_pos(m, tb); px_, py2 = to_px(CX, py_ , cy)
            if t - tb < 0.04: puff(CX, py_ - 0.2, BLOOD, (180, 30, 40), t, 0.25, 2.6)
    big = c.resize((OW, OH), Image.NEAREST); dd2 = ImageDraw.Draw(big)
    f_ = ImageFont.truetype(os.path.join(R, "Assets/Resources/Fonts/PixelifySans-SemiBold.ttf"), 22)
    hp = pl_hp(t); x0_, y0_ = 30, OH - 54
    dd2.text((x0_, y0_ - 26), "YOU", font=f_, fill=(232, 220, 192)); dd2.rectangle((x0_, y0_, x0_ + 300, y0_ + 14), fill=(20, 22, 23)); dd2.rectangle((x0_ + 2, y0_ + 2, x0_ + 2 + 296 * hp / 1000, y0_ + 12), fill=(120, 170, 90))
    return big
HURT = {}
def hurt_t(mi):
    ts = [s for (s, m) in shots if m == mi]; return max([x for x in ts if x <= cur_t[0]], default=-9)
cur_t = [0.0]
if __name__ == "__main__":
    tmp = os.path.join(out, "_f"); os.makedirs(tmp, exist_ok=True); n = int(DUR * FPS)
    for i in range(n):
        cur_t[0] = i / FPS; draw(i).convert("RGB").save(os.path.join(tmp, f"{i:05d}.png"))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(tmp, "%05d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", os.path.join(out, "scene_prowlers.mp4")], check=True)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(tmp, "%05d.png"), "-vf", "scale=720:-1:flags=neighbor,split[a][b];[a]palettegen=max_colors=128[p];[b][p]paletteuse=dither=none", "-loop", "0", os.path.join(out, "scene_prowlers.gif")], check=True)
    keys = [10, 60, 100, 150, 200, 260]; sheet = Image.new("RGB", (OW // 2 * 3, OH // 2 * 2))
    for q, k in enumerate(keys): sheet.paste(Image.open(os.path.join(tmp, f"{min(k, n - 1):05d}.png")).resize((OW // 2, OH // 2), Image.LANCZOS), ((q % 3) * OW // 2, (q // 3) * OH // 2))
    sheet.save(os.path.join(out, "scene_prowlers_sheet.png"))
    for fn in os.listdir(tmp): os.remove(os.path.join(tmp, fn))
    os.rmdir(tmp); print("ok", n)
