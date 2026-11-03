"""Boss 2 preview: the brain in the vat against a bot, from the harness (real BrainBoss code), drawn on the real Secret Lab Alpha
render with the real sprites. The map is shown at 2x so the double-resolution vat keeps its pixels.
    dotnet /tmp/test.dll brainframes frames.json      (after Tools/compile_check/runtest.sh)
    Tools/render/render.sh secret_lab_alpha
    python3 Tools/render/brain_fight.py frames.json out.mp4
"""
import sys, json, os, subprocess, tempfile
from PIL import Image, ImageDraw
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')
R = lambda *p: os.path.join(ROOT, *p)
S, T = 2, 32                                    # map scale, tile size on screen
X0, Y0, X1, Y1 = 26, 1, 50, 19                  # the room, in tiles
d = json.load(open(sys.argv[1])); out = sys.argv[2]
lab = Image.open(R('Tools/render/_render/secret_lab_alpha.png')).convert('RGBA')
lab = lab.resize((lab.width * S, lab.height * S), Image.NEAREST)
vat = Image.open(R('Assets/Resources/Sprites/Mutants/boss_brain_vat_strip.png')).convert('RGBA')
orb = Image.open(R('Assets/Resources/Sprites/Fx/boss_brain_orb.png')).convert('RGBA')
orbs = [orb.crop((i * 12, 0, i * 12 + 12, 12)).resize((24, 24), Image.NEAREST) for i in range(2)]
unit = Image.open(R('Assets/Resources/Sprites/armor_atlas.png')).convert('RGBA').crop((0, 0, 16, 16)).resize((32, 32), Image.NEAREST)
def strip(name):
    im = Image.open(R('Assets/Resources/Sprites/Fx', f'px_{name}.png')).convert('RGBA'); h = im.height
    return [im.crop((i * h, 0, i * h + h, h)).resize((h * 2, h * 2), Image.NEAREST) for i in range(im.width // h)]
pull, escape = strip('void_pull'), strip('void_escape')
vx, vy = d['vat']; vcx, vcy = (vx + 0.5) * T, (vy + 0.5) * T
floor = lab.crop(((X0 + 2) * T, (Y1 - 2) * T, (X0 + 3) * T, (Y1 - 1) * T))
names = ['full', 'crack 1', 'crack 2', 'crack 3: the brain half bare', 'drained - still', 'awake']
tmp = tempfile.mkdtemp(); n = 0; last = ''; pull_t = None
def save(c):
    global n
    c.crop((X0 * T, Y0 * T, X1 * T, Y1 * T)).convert('RGB').save(os.path.join(tmp, f'{n:05d}.png')); n += 1
for f in d['frames']:
    for fx, fy, tx, ty in f['slides']:       # telekinesis: the prop leaves its tile and lands on the new one
        piece = lab.crop((fx * T, fy * T, fx * T + T, fy * T + T)); lab.paste(floor, (fx * T, fy * T)); lab.paste(piece, (tx * T, ty * T))
    c = lab.copy()
    v = vat.crop((f['art'] * 96, 0, f['art'] * 96 + 96, 144))
    c.alpha_composite(v, (int(vcx - 48), int(vcy - 134)))
    if f['pull']:
        pull_t = 0 if pull_t is None else pull_t + 1
        fr = pull[(pull_t // 1) % len(pull)]; c.alpha_composite(fr, (int(vcx - fr.width / 2), int(vcy - 50 - fr.height / 2)))
    else: pull_t = None
    for i, (ox, oy) in enumerate(f['orbs']):
        c.alpha_composite(orbs[(n + i) % 2], (int(ox * T - 12), int(oy * T - 0.35 * T - 12)))
    px, py, dive = f['p']
    u = unit if not dive else unit.rotate(-70, expand=False)
    c.alpha_composite(u, (int(px * T - 16), int(py * T - 22)))
    if f['log']: last = f['log'][-1]
    dr = ImageDraw.Draw(c); bx, by = X0 * T, Y0 * T
    dr.rectangle((bx, by, bx + 420, by + 16), fill=(0, 0, 0, 170)); dr.text((bx + 4, by + 3), f"{f['t']:5.1f}s  {names[f['art']]}" + (f"  - {f['doing']}" if f['doing'] else ''), fill=(230, 220, 180))
    W = (X1 - X0) * T; dr.rectangle((bx, Y1 * T - 18, bx + W, Y1 * T), fill=(0, 0, 0, 170)); dr.text((bx + 4, Y1 * T - 15), last[:110], fill=(200, 200, 200))
    save(c)
for k in range(len(escape)):                   # the escape: the full-size void where the vat stood, the brain gone
    c = lab.copy(); v = vat.crop((4 * 96, 0, 5 * 96, 144)); v.putalpha(v.getchannel('A').point(lambda a: a * max(0, 1 - k / 12)))
    c.alpha_composite(v, (int(vcx - 48), int(vcy - 134)))
    fr = escape[k]; c.alpha_composite(fr, (int(vcx - fr.width / 2), int(vcy - 60 - fr.height / 2)))
    dr = ImageDraw.Draw(c); dr.rectangle((X0 * T, Y1 * T - 18, X1 * T, Y1 * T), fill=(0, 0, 0, 170)); dr.text((X0 * T + 4, Y1 * T - 15), "The void opens under the vat and swallows it whole.", fill=(200, 200, 200))
    save(c)
subprocess.run(['ffmpeg', '-v', 'error', '-y', '-framerate', '15', '-i', os.path.join(tmp, '%05d.png'), '-vf', 'scale=iw:ih:flags=neighbor',
                '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20', '-movflags', '+faststart', out], check=True)
print(out, n, 'frames')
