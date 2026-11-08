"""Boss 2 preview: the brain in the vat against a bot, from the harness (the real BrainBoss code), drawn on the real Secret Lab
Alpha render with the real sprites (the vat sheet, the attack sprites, the world atlas for cover). The map is shown at 2x so the
double-resolution vat keeps its pixels.
    dotnet /tmp/test.dll brainframes frames.json      (after Tools/compile_check/runtest.sh)
    Tools/render/render.sh secret_lab_alpha
    python3 Tools/render/brain_fight.py frames.json out.mp4
"""
import sys, json, os, math, subprocess, tempfile
from PIL import Image, ImageDraw
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')
R = lambda *p: os.path.join(ROOT, *p)
S, T = 2, 32                                    # map scale, tile size on screen
X0, Y0, X1, Y1 = 26, 2, 49, 20                  # the room, in tiles
d = json.load(open(sys.argv[1])); out = sys.argv[2]
lab0 = Image.open(R('Tools/render/_render/secret_lab_alpha.png')).convert('RGBA')
lab0 = lab0.resize((lab0.width * S, lab0.height * S), Image.NEAREST)
sheet = Image.open(R('Assets/Resources/Sprites/Mutants/boss_brain_vat.png')).convert('RGBA')
def cell(r, c): return sheet.crop((c * 128, r * 160, c * 128 + 128, r * 160 + 160))
fx = lambda n: Image.open(R('Assets/Resources/Sprites/Fx', n + '.png')).convert('RGBA')
def fxs(name, n, w, h, size):                   # the hand-drawn attack effects (ArtSource/bosses/make_brain_fx.py): four phases, each animated
    im = fx(name); return [im.crop((i * w, 0, i * w + w, h)).resize(size, Image.NEAREST) for i in range(n)]
ORB = [fxs(f'boss_brain_orbs_p{p}', 8, 16, 16, (26, 26)) for p in range(4)]; BOLT = [fxs(f'boss_brain_bolt_p{p}', 4, 28, 10, (56, 20)) for p in range(4)]
RING = [fxs(f'boss_brain_ring_p{p}', 4, 64, 64, (64, 64)) for p in range(4)]; BEAM = [fxs(f'boss_brain_beam_p{p}', 4, 32, 12, (32, 12)) for p in range(4)]
CAP = [fxs(f'boss_brain_beamcap_p{p}', 4, 16, 12, (26, 20)) for p in range(4)]; GLOW = [fxs(f'boss_brain_glow_p{p}', 4, 32, 32, (32, 32)) for p in range(4)]
atlas = Image.open(R('Tools/render/_render/world_atlas.png')).convert('RGBA')
meta = {l.split()[0]: list(map(int, l.split()[1:7])) for l in open(R('Tools/render/_render/world_atlas.txt')) if l.strip()}
def prop(look):
    m = meta.get(f'p_{look}_0') or meta.get(f'p_{look}')
    x, y, w, h, pvx, pvy = m
    return atlas.crop((x, y, x + w, y + h)).resize((w * S, h * S), Image.NEAREST), pvx * S, pvy * S
unit = Image.open(R('Assets/Resources/Sprites/armor_atlas.png')).convert('RGBA').crop((0, 0, 16, 16)).resize((32, 32), Image.NEAREST)
def strip(name):
    im = Image.open(R('Assets/Resources/Sprites/Fx', f'px_{name}.png')).convert('RGBA'); h = im.height
    return [im.crop((i * h, 0, i * h + h, h)).resize((h * 2, h * 2), Image.NEAREST) for i in range(im.width // h)]
pull, escape = strip('void_pull'), strip('void_escape')
vx, vy = d['vat']; vbx, vby = (vx + 0.5) * T, (vy + 0.5) * T          # the vat's base point on screen
floor = lab0.crop((40 * T, 16 * T, 41 * T, 18 * T))
tmp = tempfile.mkdtemp(); n = 0; last = ''; pull_t = 0; prev_wind = 0; flash = 0
NAMES = {0: 'full', 1: 'crack 1', 2: 'crack 2', 3: 'crack 3 - the brain half bare', 8: 'drained - still', 9: 'awake - rising', 10: 'the eye'}
def save(c):
    global n
    c.crop((X0 * T, Y0 * T, X1 * T, Y1 * T)).convert('RGB').save(os.path.join(tmp, f'{n:05d}.png')); n += 1
def glow(c, x, y, r, a, ph=0):
    im = GLOW[ph][n % 4].resize((int(r * 2), int(r * 2)), Image.NEAREST); im.putalpha(im.getchannel('A').point(lambda v: min(255, int(v * a / 120))))
    c.alpha_composite(im, (int(x - im.width / 2), int(y - im.height / 2)))
for f in d['frames']:
    lab = lab0.copy()
    for gx, gy in f['gone']:                    # cover broken or torn loose
        lab.paste(floor, (gx * T, (gy - 1) * T))
    for lx, ly, look in f['landed']:            # thrown cover that came down somewhere else
        im, pvx, pvy = prop(look); lab.alpha_composite(im, (int(lx * T + T / 2 - pvx), int((ly + 1) * T - 2 - pvy)))
    c = lab
    cx, cy = f['c']; bx, by = cx * T, cy * T
    row, col = f['row'], f['col']; ph = (row % 4) if row < 8 else 3
    c.alpha_composite(cell(row, col), (int(vbx - 64), int(vby - 150)))
    if f['wind']: glow(c, bx, by - 0.4 * T, 22 + 10 * math.sin(n * 0.9) + (14 if f['doing'] == 'pulse' else 0), 120, ph)
    if prev_wind and not f['wind'] and row < 8: flash = 3
    prev_wind = f['wind']
    if flash: glow(c, bx, by - 0.4 * T, 40, 200, ph); flash -= 1
    if f['pull']:
        fr = pull[pull_t % len(pull)]; pull_t += 1; c.alpha_composite(fr, (int(bx - fr.width / 2), int(by - fr.height / 2 - 10)))
    if f['pulse'] >= 0.4:                       # the wave front (the strip's own colours, scaled to its radius, fading)
        r = f['pulse'] * T; a = max(0, min(255, int(255 * (1.2 - f['pulse'] / 16)))); im = RING[ph][n % 4].resize((max(4, int(2 * r)), max(3, int(1.5 * r))), Image.NEAREST)
        im.putalpha(im.getchannel('A').point(lambda v: int(v * a / 255))); c.alpha_composite(im, (int(bx - im.width / 2), int(by - im.height / 2)))
    if f['beams']:
        for ang, ln in f['beams']:
            r = math.radians(ang); oy0 = by - 0.35 * T
            for k in range(int(math.ceil(ln))):         # a tile at a time, each on its own frame, like the game
                seg = BEAM[ph][(k + n) % 4]; seg = seg.crop((0, 0, int(32 * min(1, ln - k)), 12)).resize((int(32 * min(1, ln - k)), 14), Image.NEAREST).rotate(-ang, expand=True, resample=Image.NEAREST)
                mx, my = bx + math.cos(r) * (k + 0.5) * T, oy0 + math.sin(r) * (k + 0.5) * T; c.alpha_composite(seg, (int(mx - seg.width / 2), int(my - seg.height / 2)))
            cp = CAP[ph][n % 4].rotate(-ang, expand=True, resample=Image.NEAREST); c.alpha_composite(cp, (int(bx - cp.width / 2), int(oy0 - cp.height / 2)))
    for ox, oy, dx, dy, kind, age in f['orbs']:
        sx, sy = ox * T, oy * T - 0.35 * T
        if kind == 'bolt':
            deg = math.degrees(math.atan2(-dy, dx))
            for k in (2, 1, 0):
                im = BOLT[ph][(n + k) % 4].rotate(deg, expand=True, resample=Image.NEAREST)
                if k: im.putalpha(im.getchannel('A').point(lambda a, k=k: int(a * (1 - 0.35 * k))))
                c.alpha_composite(im, (int(sx - dx * 0.35 * k * T - im.width / 2), int(sy - dy * 0.35 * k * T - im.height / 2)))
        else:
            im = ORB[ph][(4 if kind == 'void' else 0) + (n + len(f['orbs'])) % 4]
            s = min(1, 0.4 + age * 3); im = im.resize((max(2, int(26 * s)), max(2, int(26 * s))), Image.NEAREST)
            c.alpha_composite(im, (int(sx - im.width / 2), int(sy - im.height / 2)))
    for tx, ty, look, flying, age in f['throws']:
        im, pvx, pvy = prop(look)
        im = im.rotate(age * 540 if flying else math.sin(age * 18) * 8, expand=True, resample=Image.NEAREST)
        up = min(1, age / 0.25) * (0.6 + 0.08 * math.sin(age * 14)) * T
        tint = Image.new('RGBA', im.size, (192, 107, 255, 0)); tint.putalpha(im.getchannel('A').point(lambda a: a // 4))
        c.alpha_composite(im, (int(tx * T - im.width / 2), int(ty * T - up - im.height / 2))); c.alpha_composite(tint, (int(tx * T - im.width / 2), int(ty * T - up - im.height / 2)))
    px, py, dive = f['p']
    u = unit if not dive else unit.rotate(-70, expand=False)
    c.alpha_composite(u, (int(px * T - 16), int(py * T - 22)))
    if f['log']: last = f['log'][-1]
    dr = ImageDraw.Draw(c); x0, y0 = X0 * T, Y0 * T
    dr.rectangle((x0, y0, x0 + 470, y0 + 16), fill=(0, 0, 0, 170))
    dr.text((x0 + 4, y0 + 3), f"{f['t']:5.1f}s  {NAMES.get(row, NAMES.get(row - 4, ''))}" + (f"  - {f['doing']}" if f['doing'] else ''), fill=(230, 220, 180))
    dr.rectangle((x0, Y1 * T - 18, X1 * T, Y1 * T), fill=(0, 0, 0, 170)); dr.text((x0 + 4, Y1 * T - 15), last[:118], fill=(200, 200, 200))
    save(c)
for k in range(len(escape)):                    # the escape
    c = lab.copy(); v = cell(10, 7); v.putalpha(v.getchannel('A').point(lambda a: int(a * max(0, 1 - k / 14))))
    c.alpha_composite(v, (int(vbx - 64), int(vby - 150)))
    fr = escape[k]; c.alpha_composite(fr, (int(bx - fr.width / 2), int(by - fr.height / 2 - 20)))
    dr = ImageDraw.Draw(c); dr.rectangle((X0 * T, Y1 * T - 18, X1 * T, Y1 * T), fill=(0, 0, 0, 170))
    dr.text((X0 * T + 4, Y1 * T - 15), "The void opens in the vat and swallows it whole.", fill=(200, 200, 200))
    save(c)
subprocess.run(['ffmpeg', '-v', 'error', '-y', '-framerate', '15', '-i', os.path.join(tmp, '%05d.png'), '-c:v', 'libx264', '-pix_fmt', 'yuv420p',
                '-crf', '20', '-movflags', '+faststart', out], check=True)
print(out, n, 'frames', 'bot hits', d['hits'], 'dives', d['dives'])
