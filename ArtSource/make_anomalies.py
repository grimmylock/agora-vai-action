"""Animated anomaly sprites (Batch 22), one 6-frame strip per anomaly type, 16x16 per frame, ~110 ms.
Locked agora-vai style for effects: bright core, coloured ring, faint outer ring (alpha ~70),
stray specks (alpha ~170), everything snapped to the pixel grid, seeded so reruns are identical.
Pyre keeps its approved flame (make_demo.py); this adds a scorch ring under it.
Run: python make_anomalies.py -> ../Assets/Resources/Sprites/anomaly_<type>_strip.png (+ preview)."""
import math, os, random, sys
from PIL import Image, ImageDraw

def hexc(h, a=255):
    h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

N = 6
def put(im, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < 16 and 0 <= y < 16: im.putpixel((x, y), c)
def over(im, x, y, c):          # only paint where empty or over a fainter pixel
    x, y = int(round(x)), int(round(y))
    if 0 <= x < 16 and 0 <= y < 16 and im.getpixel((x, y))[3] <= c[3]: im.putpixel((x, y), c)
def ring(im, cx, cy, r, c, step=6):
    for a in range(0, 360, step):
        over(im, cx + r * math.cos(math.radians(a)), cy + r * 0.6 * math.sin(math.radians(a)), c)
def disc(im, cx, cy, r, c):
    for y in range(16):
        for x in range(16):
            if ((x - cx) ** 2 + ((y - cy) / 0.6) ** 2) <= r * r: over(im, x, y, c)

# ---- Pyre: the approved flame column + a scorched ground ring ----
def pyre(i):
    rnd = random.Random(i); im = Image.new('RGBA', (16, 16))
    disc(im, 7.5, 13.5, 5.5, hexc('#2a1a12', 150))
    cols = ['#5a1a08', '#b8360c', '#f2741c', '#ffc845', '#fff4c0']
    for y in range(16):
        for x in range(16):
            width = max(0, (y - 2) * 0.45 + rnd.uniform(-1.2, 1.2)); dist = abs(x - 7.5)
            if dist < width and 1 < y < 15:
                heat = (1 - dist / max(width, .01)) * (y / 15) + rnd.uniform(-.15, .15)
                im.putpixel((x, y), hexc(cols[min(4, max(0, int(heat * 5)))]))
    return im

# ---- Livewire: crackling arcs out of a white-hot core over a burnt patch ----
def livewire(i):
    rnd = random.Random(100 + i); im = Image.new('RGBA', (16, 16))
    disc(im, 7.5, 9.5, 5, hexc('#1c2a30', 120))
    ring(im, 7.5, 9.5, 6.5, hexc('#4fb0e8', 70))
    for arc in range(3 if i % 2 else 4):                       # jagged bolts, a new set every frame
        a = rnd.uniform(0, 2 * math.pi); length = rnd.uniform(4, 7)
        x, y = 7.5, 7.5
        for s in range(int(length)):
            a += rnd.uniform(-.7, .7)
            x += math.cos(a); y += math.sin(a) * .8
            over(im, x, y, hexc('#9fe0ff') if s < length - 2 else hexc('#4fb0e8'))
    for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)): put(im, 7 + dx, 7 + dy, hexc('#e8f6ff'))
    for _ in range(2): over(im, rnd.randint(2, 13), rnd.randint(2, 13), hexc('#fff27a', 170))   # stray sparks
    return im

# ---- Acid Bloom: a bubbling toxic puddle, bubbles swell and pop ----
def acid(i):
    rnd = random.Random(7); im = Image.new('RGBA', (16, 16))
    disc(im, 7.5, 10, 6.5, hexc('#22381a'))
    disc(im, 7.5, 10, 5.5, hexc('#3c6e1e'))
    disc(im, 7, 9.5, 3.5, hexc('#6fae2a'))
    for x in range(5, 9): over(im, x, 8, hexc('#b8e04a'))          # lit rim of the pool
    bubbles = [(4, 10), (10, 9), (7, 11), (11, 12), (5, 12)]
    for k, (bx, by) in enumerate(bubbles):
        ph = (i + k * 2) % N                                       # 0..5: grow, grow, pop, gone...
        if ph == 0: put(im, bx, by, hexc('#b8e04a'))
        elif ph == 1: put(im, bx, by, hexc('#e8ffa0')); put(im, bx + 1, by, hexc('#b8e04a'))
        elif ph == 2:
            for dx, dy in ((-1, 0), (1, 0), (0, -1)): put(im, bx + dx, by + dy, hexc('#e8ffa0', 200))
    for k in range(3):                                             # fumes drifting up
        y = 7 - ((i + k * 2) % N); x = 5 + k * 3 + (1 if (i + k) % 3 == 0 else 0)
        if y >= 0: over(im, x, y, hexc('#b8e04a', 110 - 15 * (7 - y)))
    return im

# ---- Spindle: a dust vortex turning around a dark eye (two long arms, galaxy-like) ----
def spindle(i):
    im = Image.new('RGBA', (16, 16)); rot = i * math.pi / N               # 2 arms -> loops every 180 deg
    ring(im, 7.5, 8, 7, hexc('#8a7a5a', 60), 4)
    for arm in range(2):
        for s in range(22):
            t = s / 21; r = 0.8 + t * 6.4; a = rot + arm * math.pi + t * 4.4
            if s % 4 == 3: continue                                     # broken streaks read as dust
            c = hexc('#d8c8a0') if t < .3 else hexc('#b8a888') if t < .65 else hexc('#8a7a5a', 190)
            over(im, 7.5 + r * math.cos(a), 8 + r * 0.6 * math.sin(a), c)
    for dx, dy in ((0, 0), (1, 0)): put(im, 7 + dx, 8 + dy, hexc('#1b1d1f'))
    put(im, 7, 7, hexc('#9a86c8')); put(im, 8, 7, hexc('#4c3c6e'))
    rnd = random.Random(300 + i)                                   # debris flung along the rim
    for _ in range(2):
        a = rnd.uniform(0, 2 * math.pi); over(im, 7.5 + 6.5 * math.cos(a), 8 + 4 * math.sin(a), hexc('#4c4128', 170))
    return im

# ---- Radiation pocket: a faint green shimmer over the ground, motes rising ----
def radiation(i):
    im = Image.new('RGBA', (16, 16)); rnd = random.Random(500 + i)
    disc(im, 7.5, 10, 5.5, hexc('#a8ffb0', 28))                    # the contaminated patch, barely there
    for _ in range(7):                                             # shimmer: a few pixels flicker each frame
        x, y = rnd.randint(3, 12), rnd.randint(7, 13)
        if ((x - 7.5) ** 2 + ((y - 10) / .6) ** 2) <= 30: over(im, x, y, hexc('#a8ffb0', rnd.choice((70, 110, 150))))
    for k, (mx, my0) in enumerate([(4, 12), (8, 13), (11, 11), (6, 10), (10, 13)]):
        y = my0 - ((i + k * 3) % N) * 1.5
        a = 220 - int((my0 - y) * 25)
        if a > 40: put(im, mx + (1 if (i + k) % 4 == 0 else 0), y, hexc('#d8f070' if k % 2 else '#a8ffb0', a))
    return im

TYPES = {'pyre': pyre, 'livewire': livewire, 'acid_bloom': acid, 'spindle': spindle, 'radiation': radiation}

def build(out):
    os.makedirs(out, exist_ok=True); strips = {}
    for k, fn in TYPES.items():
        frames = [fn(i) for i in range(N)]
        s = Image.new('RGBA', (16 * N, 16))
        for i, f in enumerate(frames): s.alpha_composite(f, (16 * i, 0))
        s.save(os.path.join(out, f'anomaly_{k}_strip.png')); frames[0].save(os.path.join(out, f'anomaly_{k}.png'))
        strips[k] = frames
    return strips

def preview(strips, path, gif):
    S = 8
    sheet = Image.new('RGBA', (16 * S * N + 220, len(strips) * (16 * S + 12) + 12), hexc('#1b1d1f')); d = ImageDraw.Draw(sheet)
    for r, (k, fr) in enumerate(strips.items()):
        y = 12 + r * (16 * S + 12); d.text((12, y + 60), k, fill=hexc('#b8bcc0'))
        for i, f in enumerate(fr):
            cell = Image.new('RGBA', (16, 16), hexc('#2a2620')); cell.alpha_composite(f)
            sheet.alpha_composite(cell.resize((16 * S, 16 * S), Image.NEAREST), (200 + i * 16 * S, y))
    sheet.save(path)
    frames = []
    for i in range(N):
        g = Image.new('RGBA', (len(strips) * 16 * 6, 16 * 6), hexc('#2a2620'))
        for c, fr in enumerate(strips.values()): g.alpha_composite(fr[i].resize((96, 96), Image.NEAREST), (c * 96, 0))
        frames.append(g.convert('RGB'))
    frames[0].save(gif, save_all=True, append_images=frames[1:], duration=110, loop=0)

if __name__ == '__main__':
    here = os.path.dirname(os.path.abspath(__file__))
    target = os.path.join(here, '..', 'Assets', 'Resources', 'Sprites')
    out = target if os.path.isdir(target) else (sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, 'out_anomalies'))
    st = build(out)
    preview(st, os.path.join(here, 'anomalies_preview.png'), os.path.join(here, 'anomalies_preview.gif'))
    print('wrote', len(st), 'anomaly strips to', out)
