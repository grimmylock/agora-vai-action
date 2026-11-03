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
orbs = fx('boss_brain_orbs'); orbs = [orbs.crop((i * 12, 0, i * 12 + 12, 12)).resize((24, 24), Image.NEAREST) for i in range(4)]
bolt = fx('boss_brain_bolt'); bolts = [bolt.crop((i * 18, 0, i * 18 + 18, 8)).resize((36, 16), Image.NEAREST) for i in range(2)]
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
def glow(c, x, y, r, a):
    g = Image.new('RGBA', c.size, (0, 0, 0, 0)); dg = ImageDraw.Draw(g)
    for k in range(4, 0, -1): dg.ellipse((x - r * k / 4, y - r * k / 4, x + r * k / 4, y + r * k / 4), fill=(192, 107, 255, int(a * (5 - k) / 4)))
    c.alpha_composite(g)
for f in d['frames']:
    lab = lab0.copy()
    for gx, gy in f['gone']:                    # cover broken or torn loose
        lab.paste(floor, (gx * T, (gy - 1) * T))
    for lx, ly, look in f['landed']:            # thrown cover that came down somewhere else
        im, pvx, pvy = prop(look); lab.alpha_composite(im, (int(lx * T + T / 2 - pvx), int((ly + 1) * T - 2 - pvy)))
    c = lab
    cx, cy = f['c']; bx, by = cx * T, cy * T
    row, col = f['row'], f['col']
    c.alpha_composite(cell(row, col), (int(vbx - 64), int(vby - 150)))
    if f['wind']: glow(c, bx, by - 0.4 * T, 22 + 10 * math.sin(n * 0.9) + (14 if f['doing'] == 'pulse' else 0), 120)
    if prev_wind and not f['wind'] and row < 8: flash = 3
    prev_wind = f['wind']
    if flash: glow(c, bx, by - 0.4 * T, 40, 200); flash -= 1
    if f['pull']:
        fr = pull[pull_t % len(pull)]; pull_t += 1; c.alpha_composite(fr, (int(bx - fr.width / 2), int(by - fr.height / 2 - 10)))
    if f['pulse'] >= 0.4:                       # the wave front
        r = f['pulse'] * T; a = max(0, min(255, int(255 * (1.2 - f['pulse'] / 16))))
        g = Image.new('RGBA', c.size, (0, 0, 0, 0)); dg = ImageDraw.Draw(g)
        dg.ellipse((bx - r, by - r * 0.75, bx + r, by + r * 0.75), outline=(240, 224, 255, a), width=4)
        dg.ellipse((bx - r + 5, by - r * 0.75 + 4, bx + r - 5, by + r * 0.75 - 4), outline=(192, 107, 255, a // 2), width=6)
        c.alpha_composite(g)
    if f['beams']:
        g = Image.new('RGBA', c.size, (0, 0, 0, 0)); dg = ImageDraw.Draw(g)
        for ang, ln in f['beams']:
            r = math.radians(ang); ex, ey = bx + math.cos(r) * ln * T, by - 0.35 * T + math.sin(r) * ln * T
            dg.line((bx, by - 0.35 * T, ex, ey), fill=(192, 107, 255, 110), width=13)
            dg.line((bx, by - 0.35 * T, ex, ey), fill=(255, 240, 255, 255), width=4)
        c.alpha_composite(g)
    for ox, oy, dx, dy, kind, age in f['orbs']:
        sx, sy = ox * T, oy * T - 0.35 * T
        if kind == 'bolt':
            deg = math.degrees(math.atan2(-dy, dx))
            for k in (2, 1, 0):
                im = bolts[n % 2].rotate(deg, expand=True, resample=Image.NEAREST)
                if k: im.putalpha(im.getchannel('A').point(lambda a, k=k: int(a * (1 - 0.35 * k))))
                c.alpha_composite(im, (int(sx - dx * 0.35 * k * T - im.width / 2), int(sy - dy * 0.35 * k * T - im.height / 2)))
        else:
            im = orbs[(2 if kind == 'void' else 0) + n % 2]
            s = min(1, 0.4 + age * 3); im = im.resize((max(2, int(24 * s)), max(2, int(24 * s))), Image.NEAREST)
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
