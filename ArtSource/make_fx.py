"""Combat effect sprites (Batch 22): muzzle flashes, suppressed puff, hit puffs.
Locked agora-vai style: glows have a bright core + coloured ring, no outline, flat tones.
Muzzle flashes are drawn at the weapon pixel density (PPU 32, pivot = left middle, pointing right);
hit puffs at the character density (PPU 16, centred). Every file is a horizontal strip of frames.
Run: python make_fx.py  ->  writes ../Assets/Resources/Sprites/Fx/*.png (or ./out_fx) + preview."""
import os, sys
from PIL import Image, ImageDraw

def hexc(h, a=255):
    h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def grid(rows, pal):
    im = Image.new('RGBA', (len(rows[0]), len(rows)), (0, 0, 0, 0))
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch != '.': im.putpixel((x, y), pal[ch])
    return im

def strip(frames):
    w, h = frames[0].size
    out = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames): out.alpha_composite(f, (i * w, 0))
    return out

FIRE = {'W': hexc('#fff4c0'), 'Y': hexc('#ffc845'), 'O': hexc('#f2741c'), 'R': hexc('#b8360c'),
        'o': hexc('#f2741c', 150), 'r': hexc('#b8360c', 110)}
RAIL = {'W': hexc('#e8f6ff'), 'Y': hexc('#9fe0ff'), 'O': hexc('#4fb0e8'), 'R': hexc('#2c5a8a'),
        'o': hexc('#4fb0e8', 150), 'r': hexc('#2c5a8a', 110)}
SMOKE = {'W': hexc('#c8c8c0', 170), 'Y': hexc('#9a9a92', 140), 'O': hexc('#6e6e68', 110), 'o': hexc('#6e6e68', 60)}

# Muzzle flashes: frame 0 = full bloom, frame 1 = collapsing core. Row centre = pivot height.
MUZZLE = {
    1: [["..O..",
         ".OYO.",
         "OWWYO",
         ".OYO.",
         "..O.."],
        [".....",
         "..o..",
         ".oYo.",
         "..o..",
         "....."]],
    2: [["...R...",
         "..rOr.O",
         ".OYYOO.",
         "OWWWYYO",
         ".OYYOO.",
         "..rOr.O",
         "...R..."],
        [".......",
         ".......",
         "..oO...",
         ".oWYO..",
         "..oO...",
         ".......",
         "......."]],
    3: [["....R.....",       # shotgun: wide cone of burning powder
         "...rOr..R.",
         "..OYOO.O..",
         ".OYWYYOO.R",
         "OWWWWYYOO.",
         ".OYWYYOO.R",
         "..OYOO.O..",
         "...rOr..R.",
         "....R....."],
        ["..........",
         "..........",
         "...o.o....",
         "..oYOo.o..",
         ".OWYYOo...",
         "..oYOo.o..",
         "...o.o....",
         "..........",
         ".........."]],
    4: [["..R....R.....",    # anti-materiel: side vents of the muzzle brake + long tongue
         ".rOr..rOr....",
         "..OY..OY.....",
         "...YO.YO..R..",
         "..OYWYYOOOr..",
         "OWWWWWWYYYOOR",
         "..OYWYYOOOr..",
         "...YO.YO..R..",
         "..OY..OY.....",
         ".rOr..rOr....",
         "..R....R....."],
        [".............",
         ".............",
         ".............",
         "...o..o......",
         "..oYO.YOo....",
         ".OWWYYYOo....",
         "..oYO.YOo....",
         "...o..o......",
         ".............",
         ".............",
         "............."]],
}
PUFF = [["....", ".WY.", ".YO.", "...."],           # suppressed shot: a grey puff that drifts and fades
        [".WY.", "WYYO", "YOOo", ".Oo."],
        ["..o.", ".Oo.", "oOo.", "...."]]

# Hit puffs, 5x5 at character density, 3 frames (burst, spray, drops)
def hit(c):
    return [grid([".....", "..a..", ".aba.", "..a..", "....."], c),
            grid(["a...a", ".bab.", ".aca.", ".bab.", "a...a"], c),
            grid(["b...b", ".....", "..b..", ".....", "b...b"], c)]
BLOOD = {'a': hexc('#e0525a'), 'b': hexc('#b3212a'), 'c': hexc('#ffd0d0')}
SPARK = {'a': hexc('#ffc845'), 'b': hexc('#f2741c'), 'c': hexc('#fff4c0')}
PSI = {'a': hexc('#c06bff'), 'b': hexc('#6b5a7a'), 'c': hexc('#f0e0ff')}

# Gore (Batch 24): blood pool that spreads under a body (5 frames, 16x12, character density) and
# directional splatter decals (4 variants, 10x8, spray pointing right - rotated in game).
import math, random
BLOODC = {'e': hexc('#4e0e12', 235), 'd': hexc('#6e1218', 235), 'b': hexc('#8a1a20', 235), 'm': hexc('#b3212a', 235), 'h': hexc('#e0525a', 235)}
def blood_pool(i):
    rnd = random.Random(41); im = Image.new('RGBA', (16, 12)); r0 = 1.6 + i * 1.25
    bumps = [rnd.uniform(-0.18, 0.18) for _ in range(12)]
    for y in range(12):
        for x in range(16):
            dx, dy = (x - 7.5) / 1.0, (y - 6) / 0.62
            a = math.atan2(dy, dx); d = math.hypot(dx, dy)
            r = r0 * (1 + bumps[int((a + math.pi) / (2 * math.pi) * 12) % 12])
            if d <= r: im.putpixel((x, y), BLOODC['e'] if d > r - 1 else BLOODC['d'] if d > r * .6 else BLOODC['b'])
    if i >= 2: im.putpixel((6, 5), BLOODC['m']); im.putpixel((7, 5), BLOODC['m'])
    return im
def blood_splat(v):
    rnd = random.Random(70 + v); im = Image.new('RGBA', (10, 8))
    for _ in range(14):                                  # droplets thrown along +x, thinning out
        t = rnd.random(); x = int(t * 9); y = int(4 + rnd.gauss(0, 0.6 + t * 1.6))
        if 0 <= y < 8: im.putpixel((x, y), BLOODC['m'] if t < .35 else BLOODC['b'] if t < .7 else BLOODC['d'])
    for x in range(3): im.putpixel((x, 4), BLOODC['m'])
    return im

def build(out):
    os.makedirs(out, exist_ok=True)
    sheets = {}
    for n, frames in MUZZLE.items():
        sheets[f'fx_muzzle_{n}'] = strip([grid(f, FIRE) for f in frames])
    sheets['fx_muzzle_rail'] = strip([grid(f, RAIL) for f in MUZZLE[2]])
    sheets['fx_muzzle_puff'] = strip([grid(f, SMOKE) for f in PUFF])
    sheets['fx_hit_blood'] = strip(hit(BLOOD))
    sheets['fx_hit_spark'] = strip(hit(SPARK))
    sheets['fx_hit_psi'] = strip(hit(PSI))
    sheets['fx_blood_pool'] = strip([blood_pool(i) for i in range(5)])
    sheets['fx_blood_splat'] = strip([blood_splat(v) for v in range(4)])
    for k, im in sheets.items(): im.save(os.path.join(out, k + '.png'))
    return sheets

def preview(sheets, path, scale=10):
    rows = list(sheets.items())
    W = 40 + max(im.width for _, im in rows) * scale + 220
    H = 20 + sum(im.height * scale + 34 for _, im in rows)
    sheet = Image.new('RGBA', (W, H), hexc('#1b1d1f')); d = ImageDraw.Draw(sheet)
    y = 14
    for k, im in rows:
        d.text((20, y), k, fill=hexc('#b8bcc0'))
        bg = Image.new('RGBA', (im.width * scale, im.height * scale), hexc('#2a2620'))
        bg.alpha_composite(im.resize((im.width * scale, im.height * scale), Image.NEAREST))
        sheet.alpha_composite(bg, (220, y)); y += im.height * scale + 34
    sheet.save(path)

if __name__ == '__main__':
    here = os.path.dirname(os.path.abspath(__file__))
    target = os.path.join(here, '..', 'Assets', 'Resources', 'Sprites', 'Fx')
    out = target if os.path.isdir(os.path.dirname(target)) else (sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, 'out_fx'))
    s = build(out)
    preview(s, os.path.join(here, 'fx_preview.png'))
    print('wrote', len(s), 'sheets to', out)
