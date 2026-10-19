"""Animated bonfire (Batch 68): 6-frame strip, 16x24 per frame, ~110 ms, same flame family as the Pyre
anomaly (make_anomalies.py pyre: same 5-colour palette, heat = distance from the axis x height, re-rolled
every frame) but a tame camp fire: stone ring, crossed logs, ember bed, a narrower swaying tongue and
sparks drifting up. Seeded, so reruns are identical.
Run: python make_bonfire.py -> ../Assets/Resources/Sprites/bonfire_strip.png (+ bonfire_preview.png/.gif)."""
import math, os, random
from PIL import Image, ImageDraw

N, W, H = 6, 16, 24
FIRE = ['#5a1a08', '#b8360c', '#f2741c', '#ffc845', '#fff4c0']     # the Pyre palette
ROCK = ['#3a3836', '#55524e', '#706b64', '#8a847b']
WOOD = ['#2e1d12', '#4a2f1c', '#6b4527']

def hexc(h, a=255):
    h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
def put(im, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H: im.putpixel((x, y), c)
def over(im, x, y, c):          # only over empty or fainter pixels
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H and im.getpixel((x, y))[3] <= c[3]: im.putpixel((x, y), c)

CX, CY = 7.5, 19.5               # centre of the stone ring; the sprite's foot is row 22
def stones(im, front):
    for k in range(9):
        a = k / 9 * 2 * math.pi + 0.3
        x, y = CX + math.cos(a) * 5.6, CY + math.sin(a) * 2.2
        if (y > CY) != front: continue
        c = ROCK[2 + k % 2] if front else ROCK[1 + k % 2]
        put(im, x, y, hexc(c)); put(im, x + 1, y, hexc(ROCK[1] if front else ROCK[0]))
        if front: put(im, x, y - 1, hexc(ROCK[3]))                       # lit top of the near stones

def frame(i):
    rnd = random.Random(500 + i); im = Image.new('RGBA', (W, H))
    for y in range(H):                                                     # warm light on the ground
        for x in range(W):
            e = ((x - CX) / 7.5) ** 2 + ((y - CY) / 3.4) ** 2
            if e <= 1: put(im, x, y, hexc('#f2741c', int(18 + 40 * (1 - e) * (0.85 + 0.15 * math.sin(i)))))
    stones(im, False)
    for (x0, y0, x1, y1) in ((3, 21, 12, 17), (3, 17, 12, 21), (5, 22, 10, 16)):   # three crossed logs
        for s in range(10):
            t = s / 9; put(im, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, hexc(WOOD[1 + (s % 3 == 0)]))
    for _ in range(7):                                                     # ember bed, flickers per frame
        put(im, CX + rnd.uniform(-3, 3), CY + rnd.uniform(-0.8, 1.2), hexc(FIRE[rnd.randint(1, 3)]))
    sway = math.sin(i / N * 2 * math.pi) * 0.9                           # the tongue leans side to side
    top = 6 + rnd.randint(-1, 1)
    for y in range(top, 20):                                               # Pyre flame, upside-down cone
        t = (y - top) / (20 - top)
        width = max(0, min(3.0, t * 3.8 + rnd.uniform(-0.9, 0.9)))
        axis = CX + sway * (1 - t)
        for x in range(W):
            dist = abs(x - axis)
            if dist < width:
                heat = (1 - dist / max(width, .01)) * (0.35 + 0.75 * t) + rnd.uniform(-.15, .15)
                put(im, x, y, hexc(FIRE[min(4, max(0, int(heat * 5)))]))
    stones(im, True)
    for k in range(3):                                                     # sparks: loop over the 6 frames
        ph = (i + k * 2) % N
        x = CX + (k - 1) * 2.2 + math.sin(ph + k) * 1.2
        y = top - 1 - ph * 1.0
        over(im, x, y, hexc(FIRE[4] if ph < 2 else FIRE[3] if ph < 4 else FIRE[2], 230 - ph * 30))
    return im

def build(out):
    frames = [frame(i) for i in range(N)]
    s = Image.new('RGBA', (W * N, H))
    for i, f in enumerate(frames): s.alpha_composite(f, (W * i, 0))
    s.save(os.path.join(out, 'bonfire_strip.png'))
    return frames

def preview(frames, path, gif):
    S = 8; bg = hexc('#2a2a1e')
    sheet = Image.new('RGBA', (W * S * N + 12 * (N + 1), H * S + 24), hexc('#1b1d1f'))
    for i, f in enumerate(frames):
        cell = Image.new('RGBA', (W, H), bg); cell.alpha_composite(f)
        sheet.alpha_composite(cell.resize((W * S, H * S), Image.NEAREST), (12 + i * (W * S + 12), 12))
    sheet.save(path)
    g = []
    for f in frames:
        cell = Image.new('RGBA', (W, H), bg); cell.alpha_composite(f)
        g.append(cell.resize((W * 8, H * 8), Image.NEAREST).convert('RGB'))
    g[0].save(gif, save_all=True, append_images=g[1:], duration=110, loop=0)

if __name__ == '__main__':
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.join(here, '..', 'Assets', 'Resources', 'Sprites')
    fr = build(out)
    preview(fr, os.path.join(here, 'bonfire_preview.png'), os.path.join(here, 'bonfire_preview.gif'))
    print('wrote bonfire_strip.png to', out)
