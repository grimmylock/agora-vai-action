#!/usr/bin/env python3
"""Elder Gaunt v3 claw + scream strips, built from the three drawn arm poses (gaunt_elder3.py) on one pixel-exact body (2026-11-16).
Every frame = the body + one arm layer (fold / wind / strike, optionally nudged or turned a little for a smear frame), then the body LEAN (rows shifted sideways in
proportion to their height above the feet, nothing resampled) and, on the held scream, a one-pixel shake. Cells are symmetric about ANCHOR (the centre of the rest pose, third arm included, the
same rule the walk uses) with the feet on the bottom row, so the sprite pivot (centre, 16 px above the bottom) stays put when the game switches between still, walk, claw and scream.
   python3 gaunt_elder3_anim.py          -> mutants_anim/gaunt_elder/elder_gaunt_claw_strip.png (+ _attacks.json), anim/elder_gaunt_scream.bytes, previews in maps/renders/gaunt/"""
import os, json, numpy as np
from PIL import Image
import gaunt_elder3 as G
HERE = os.path.dirname(os.path.abspath(__file__)); A = os.path.join(HERE, '..', 'Assets', 'Resources', 'Sprites', 'Mutants', 'anim')
OUT = os.path.join(HERE, 'mutants_anim', 'gaunt_elder'); PRE = os.path.join(HERE, '..', 'maps', 'renders', 'gaunt')
BOT, TOP = 188, 60                      # feet row and antler tips of the body (the lean is measured against these)
PAD = 40
def layer(pose, dx=0, dy=0, rot=0.0, pivot=(112, 118)):
    im = G.arm_layer(pose); W, H = im.size
    if rot: im = im.rotate(rot, resample=Image.NEAREST, center=pivot)            # + = counter-clockwise: the hand swings up
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0)); out.alpha_composite(im, (dx, dy)) if dx >= 0 and dy >= 0 else out.paste(im, (dx, dy)); return out
def shear(im, lean, dx=0, shake=0):
    a = np.array(im); H, W = a.shape[:2]; out = np.zeros((H, W + 2 * PAD, 4), np.uint8)
    for y in range(H):
        if not a[y, :, 3].any(): continue
        h = max(0.0, BOT - y) / (BOT - TOP)                                         # 0 at the feet, 1 at the antler tips, more for a raised arm
        s = int(round(dx * min(1.0, h * 3) + lean * h * h + shake * min(1.0, h * 2)))              # the held scream trembles: a whole-body 1 px, the sign flips every frame
        x0 = PAD + s; lo = max(0, -x0); hi = min(W, W + 2 * PAD - x0); out[y, x0 + lo: x0 + hi] = a[y, lo:hi]
    return Image.fromarray(out)
def frame(pose, lean=0, dx=0, shake=0, **arm):
    body = G.load_body()
    ar = layer(pose, dx=arm.get('ax', 0), dy=arm.get('ay', 0), rot=arm.get('rot', 0.0)); cv = Image.new('RGBA', ar.size, (0, 0, 0, 0)); cv.alpha_composite(body); cv.alpha_composite(ar)
    return shear(cv, lean, dx, shake)
# ---- the two sequences: (arm pose, lean, dx, shake, arm nudge)
CLAW = [('fold', 0, 0, 0, {}), ('fold', -3, -2, 0, dict(ay=-3)), ('wind', -6, -4, 0, {}), ('strike', -8, -4, 0, dict(rot=20, ax=-4, ay=-6)),
        ('strike', 3, 3, 0, {}), ('strike', 11, 10, 0, dict(rot=-9, ay=3)), ('fold', 7, 5, 0, dict(ax=4)), ('fold', 2, 1, 0, {})]
CLAW_MS = [110, 150, 140, 70, 70, 60, 110, 110]                                      # contact = end of frame 5 = 540 ms
CALL = [('fold', 0, 0, 0, {}), ('fold', -3, 0, 0, dict(ay=-3)), ('wind', -7, -1, 1, {}), ('wind', -9, -1, -1, {}),
        ('strike', 6, 2, 0, {}), ('strike', 5, 2, 0, {}), ('strike', 4, 1, 0, dict(rot=-6, ay=2)), ('fold', 1, 0, 0, {})]
CALL_MS = [200, 260, 300, 380, 380, 420, 380, 280]                                   # the call lands 1.2 s in = frame 5 (the snap to point)
def cells(seq, anchor):
    fs = [frame(p, l, d, s, **k) for p, l, d, s, k in seq]
    off = PAD                                                                       # frames carry PAD px of side room: the body column x is x + PAD
    bb = [f.getbbox() for f in fs]; y0 = min(b[1] for b in bb); y1 = BOT + 1
    half = max(max(abs(b[0] - (anchor + off)), abs(b[2] - (anchor + off))) for b in bb) + 2
    return [f.crop((anchor + off - half, y0, anchor + off + half, y1)) for f in fs]
def strip(fs):
    W, H = fs[0].size; s = Image.new('RGBA', (W * len(fs), H), (0, 0, 0, 0))
    for i, f in enumerate(fs): s.alpha_composite(f, (i * W, 0))
    return s
def gif(fs, ms, path, scale=2):
    ims = []
    for f in fs:
        c = Image.new('RGBA', (f.width * scale + 20, f.height * scale + 20), (26, 14, 14, 255)); c.alpha_composite(f.resize((f.width * scale, f.height * scale), Image.NEAREST), (10, 10)); ims.append(c.convert('P', palette=Image.ADAPTIVE))
    ims[0].save(path, save_all=True, append_images=ims[1:], duration=[m * 2 for m in ms], loop=0, disposal=2)
def sheet(fs, path, scale=2, cols=4):
    W, H = fs[0].size; r = (len(fs) + cols - 1) // cols; sh = Image.new('RGBA', (cols * (W * scale + 6), r * (H * scale + 6)), (26, 14, 14, 255))
    for i, f in enumerate(fs): sh.alpha_composite(f.resize((W * scale, H * scale), Image.NEAREST), ((i % cols) * (W * scale + 6), (i // cols) * (H * scale + 6)))
    sh.save(path)
def anchor():
    r = G.compose('fold')[0].getbbox(); return (r[0] + r[2]) // 2
if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True); os.makedirs(PRE, exist_ok=True); an = anchor(); print('anchor x', an)
    claw = cells(CLAW, an); call = cells(CALL, an)
    W, H = claw[0].size; strip(claw).save(os.path.join(OUT, 'elder_gaunt_claw_strip.png'))
    json.dump({'elder_gaunt': {'claw': {'frame': [W, H], 'ms': CLAW_MS, 'travel': [0] * 8}}}, open(os.path.join(OUT, 'elder_gaunt_attacks.json'), 'w'), indent=1)
    strip(call).save(os.path.join(OUT, 'elder_gaunt_scream_strip.png'))
    gif(claw, CLAW_MS, os.path.join(PRE, 'elder3_claw.gif')); sheet(claw, os.path.join(PRE, 'elder3_claw_frames.png'))
    gif(call, CALL_MS, os.path.join(PRE, 'elder3_call.gif')); sheet(call, os.path.join(PRE, 'elder3_call_frames.png'))
    print('claw cell', W, 'x', H, ' scream cell', call[0].size)
