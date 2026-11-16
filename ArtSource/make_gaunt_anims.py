# The Gaunt, animated from its joints (2026-11-16; Yan: "BEFORE using spritecook try animating its actions"). The same vector art as make_gaunt.py, redrawn every frame with the joints moved (limb chains, a leaning
# upper body, a head that rotates and opens its jaw) - never a stretched or turned bitmap except the head, which is rotated as one piece. Writes frames, strips and GIFs to ArtSource/mutants_anim/gaunt/.
#   python3 ArtSource/make_gaunt_anims.py
import math, os, sys, json
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
import make_gaunt as G
HIDE, HIDE_FAR, BONE, DARK = G.HIDE, G.HIDE_FAR, G.BONE, G.DARK
CW, CH, OX, OY = 220, 150, 80, 36                         # the rig canvas and where the base drawing's origin sits on it
P0 = (34, 58)                                              # the pelvis: the upper body leans about it
def rot(p, piv, deg):
    a = math.radians(deg); dx, dy = p[0] - piv[0], p[1] - piv[1]
    return (piv[0] + dx * math.cos(a) - dy * math.sin(a), piv[1] + dx * math.sin(a) + dy * math.cos(a))
def chain(origin, lens, angs):
    pts = [origin]; x, y = origin
    for l, a in zip(lens, angs): x += l * math.sin(math.radians(a)); y += l * math.cos(math.radians(a)); pts.append((x, y))
    return pts
NEUTRAL = dict(dx=0, dy=0, lean=0, head=(0, 0, 0), jaw=0,
               legn=(29.7, -26.6, 25), legf=(26.6, -26.6, 20),
               armn=(39.8, -12.5, 21.8, -8.1), armf=(16.7, -7.8, 12.3), armn_origin=(50, 30), shake=0)
LEN_LN, LEN_LF = (16.1, 15.7, 8.9), (15.6, 15.7, 8.5)
LEN_AN, LEN_AF = (12.4, 18.4, 21.5, 14.1), (20.9, 22.2, 18.4)
_head_cache = {}
def head_of(opening):
    if opening not in _head_cache: _head_cache[opening] = G.head_image(opening)
    return _head_cache[opening]
def render(**kw):
    p = dict(NEUTRAL); p.update(kw); lean = p['lean']
    def B(x, y):                                           # base (drawing) coordinates -> rig canvas, whole body shifted
        return (x + OX + p['dx'], y + OY + p['dy'])
    def U(x, y):                                           # the same for the upper body, which leans about the pelvis
        x, y = rot((x, y), P0, lean); return B(x, y)
    L = []
    def part(build, cols, **kw2):
        m = Mask((CW, CH)); build(m); L.append(shade(m, cols, **kw2))
    # legs hang from the pelvis: thigh, shin, foot
    def leg(origin, lens, angs, widths, cols, seed):
        hip = B(*origin); pts = chain(hip, lens[:2], angs[:2]); foot_end = chain(pts[-1], lens[2:], angs[2:])[-1]
        part(lambda m: m.tube(pts + [foot_end], list(widths[:3]) + [widths[3]]), cols, tex=0.1, seed=seed, bias=-0.1)
        return pts + [foot_end]
    legf = leg((34, 60), LEN_LF, p['legf'], (8, 5.4, 3.8, 3), HIDE_FAR, 1)
    # far arm
    ao = U(52, 32); arf = chain(ao, LEN_AF, p['armf'])
    part(lambda m: m.tube(arf, [6, 4.4, 3.2, 2.6]), HIDE_FAR, tex=0.1, seed=2)
    # trunk, chest cavity, ribs, spine
    part(lambda m: m.ell(*B(34, 58), 7.5, 6.5).ell(*U(40, 44), 10.5, 15, 18 + lean).ell(*U(46, 31), 8, 7, -20 + lean), HIDE, tex=0.14, seed=3, depth=3.6, bias=-0.1)
    cav = Mask((CW, CH)); cav.ell(*U(42, 44), 7.2, 12, 18 + lean); L.append(shade(cav, DARK, depth=2.4, tex=0.08, seed=4))
    rib = Mask((CW, CH))
    for k in range(6):
        y0 = 31 + k * 4.1; x0 = 36 + k * 0.8; w = 7 + (3 if 1 < k < 5 else 0)
        rib.tube([U(x0, y0), U(x0 + w * 0.55, y0 + 1.6), U(x0 + w, y0 + 5)], [1.5, 1.6, 1.0])
    rib.tube([U(43, 30), U(45, 36), U(47, 42), U(47, 47)], [1.2, 1.2, 1.0, 0.8]); L.append(shade(rib, BONE, depth=1.3))
    sp = Mask((CW, CH))
    for i, (x, y) in enumerate([(52, 24), (46, 28), (41, 32), (37, 38), (34, 45), (32, 52), (31, 59)]): sp.ell(*U(x, y), 2.5 - i * 0.12, 2.1)
    L.append(shade(sp, BONE, depth=1.5))
    # near leg, near arm (with its claws), the near foot's toes
    legn = leg((37, 58), LEN_LN, p['legn'], (9, 6, 4.2, 3.4), HIDE, 5)
    ao = U(*p['armn_origin']); arn = chain(ao, LEN_AN, p['armn'])
    part(lambda m: m.tube(arn, [6.4, 5.2, 4, 3.4, 3]), HIDE, tex=0.12, seed=6, bias=-0.1)
    def claws(base, ang, n, spread, ln):
        cl = Mask((CW, CH))
        for i in range(n):
            a = ang + (i - (n - 1) / 2) * spread; cl.tube([base, (base[0] + ln * math.sin(math.radians(a)), base[1] + ln * math.cos(math.radians(a)))], [1.8, 0.7])
        L.append(shade(cl, BONE, depth=1.1))
    claws(arn[-1], p['armn'][-1], 4, 14, 7.5); claws(arf[-1], p['armf'][-1], 4, 14, 6.5)
    claws(legn[-1], 90, 3, 22, 4.2); claws(legf[-1], 90, 3, 22, 4.2)
    # neck and head
    hdx, hdy, hdeg = p['head']; anchor_base = (66, 19)
    hp = U(*anchor_base); hp = (hp[0] + hdx, hp[1] + hdy); n0 = U(48, 29); n1 = U(58, 21); n1 = (n1[0] + hdx * 0.4, n1[1] + hdy * 0.4)
    part(lambda m: m.tube([n0, n1, hp], [7, 5, 4.2]), HIDE, tex=0.12, seed=7, bias=-0.1)
    im = paint((CW, CH), L)
    hm = head_of(p['jaw']); hm = hm.rotate(hdeg - lean, resample=Image.NEAREST, center=(66, 19 + G.OY)) if (hdeg - lean) else hm
    tmp = Image.new('RGBA', (CW, CH), (0, 0, 0, 0)); tmp.alpha_composite(hm, (int(round(hp[0] - 66)), int(round(hp[1] - (19 + G.OY)))))
    im.alpha_composite(tmp)
    # hide strips and the broken rib
    line(im, *[int(v) for v in U(38, 62)], *[int(v) for v in U(36, 70)], HIDE[2]); line(im, *[int(v) for v in U(41, 36)], *[int(v) for v in U(50, 33)], BONE[4])
    return outline(im)

# ------------------------------------------------------------------ the actions (joint poses; angles from straight down, + toward the front/right)
S = math.sin; C = math.cos; PI = math.pi
def walk(k, n=8, run=False):
    f = 2 * PI * k / n
    amp = 38 if run else 24; bend0 = 66 if run else 58; lean = 13 if run else 4; bob = 3.2 if run else 1.2
    def leg(ph, base):
        thigh = base + amp * S(ph); bend = bend0 + (26 if run else 20) * max(0.0, S(ph + PI / 2)); shin = thigh - bend; foot = 18 + 0.35 * (thigh - base)
        return (thigh, shin, foot)
    arm_sw = (34 if run else 20) * S(f + PI)                                           # arms swing against the legs
    armn = (40 + arm_sw, -12 + 0.4 * arm_sw, 22 + 0.3 * arm_sw, -8 + (10 if run else 4) * S(f)); armf = (17 - arm_sw * 0.8, -8 - arm_sw * 0.3, 12)
    return dict(dy=-bob * abs(C(f)) + (0 if run else 0), dx=0, lean=lean + (2 if run else 1) * S(2 * f), legn=leg(f, 28), legf=leg(f + PI, 26), armn=armn, armf=armf,
                head=(1 if run else 0, (1.5 if run else 0.8) * S(2 * f + 0.6), (-4 if run else -1.5) * S(f)), jaw=(3 if run else 0))
def claw(k):                                                                           # 8 frames: rear back, wind up, lash across, follow through, recover (contact on frame 4)
    T = [dict(lean=0, armn=(39.8, -12.5, 21.8, -8.1)),
         dict(lean=-8, dx=-1, armn=(-70, -100, -120, -130), head=(-1, -1, 10), jaw=2),
         dict(lean=-15, dx=-3, armn=(-125, -150, -160, -165), head=(-2, -2, 18), jaw=5, armf=(10, -5, 8)),
         dict(lean=4, dx=3, armn=(0, 40, 68, 78), head=(2, 1, 0), jaw=8, armf=(24, 5, 15)),
         dict(lean=15, dx=9, armn=(58, 78, 90, 96), head=(7, 3, -10), jaw=11, armf=(34, 14, 20)),
         dict(lean=17, dx=10, armn=(55, 42, 30, 26), head=(8, 4, -12), jaw=6, armf=(30, 12, 18)),
         dict(lean=9, dx=5, armn=(52, 20, 10, 0), head=(3, 1, -4), jaw=2),
         dict(lean=3, dx=1, armn=(42, -8, 18, -6), head=(0, 0, 0), jaw=0)]
    d = dict(T[k]); d.setdefault('legn', (34, -22, 25) if k in (3, 4, 5) else (29.7, -26.6, 25)); d.setdefault('legf', (20, -34, 20) if k in (3, 4, 5) else (26.6, -26.6, 20)); return d
def scream(k):                                                                         # 8 frames: draw breath, throw the head back, hold and shake, close
    T = [dict(),
         dict(lean=-3, head=(0, 2, -8), jaw=0, dy=0.5),
         dict(lean=-7, head=(-1, -1, 14), jaw=3, armn=(46, 5, 18, 0)),
         dict(lean=-10, head=(-2, -2, 28), jaw=8, dx=-1, armn=(54, 20, 25, 8), armf=(30, 12, 18)),
         dict(lean=-10, head=(-2, -3, 30), jaw=9, dx=1, armn=(58, 26, 30, 12), armf=(34, 16, 22)),
         dict(lean=-10, head=(-2, -2, 28), jaw=8, dx=-1, armn=(54, 20, 25, 8), armf=(30, 12, 18)),
         dict(lean=-5, head=(-1, 0, 14), jaw=3),
         dict(lean=-1, head=(0, 0, 3), jaw=0)]
    return dict(T[k])
def point(k):                                                                          # 6 frames: lift the long arm, hold it out straight at the target
    T = [dict(),
         dict(lean=3, armn=(52, 20, 30, 40), head=(1, 0, -2)),
         dict(lean=6, armn=(62, 52, 62, 70), head=(2, 0, -4), jaw=2),
         dict(lean=7, armn=(70, 78, 86, 90), head=(3, 0, -5), jaw=2),
         dict(lean=7, armn=(72, 80, 88, 92), head=(3, 0, -5), jaw=3),
         dict(lean=7, armn=(70, 78, 86, 90), head=(3, 0, -5), jaw=2)]
    return dict(T[k])
ANIMS = {'walk': ([walk(k) for k in range(8)], [110] * 8),
         'run': ([walk(k, run=True) for k in range(8)], [70] * 8),
         'claw': ([claw(k) for k in range(8)], [100, 160, 140, 70, 60, 90, 110, 110]),
         'scream': ([scream(k) for k in range(8)], [100, 140, 120, 100, 100, 100, 140, 120]),
         'point': ([point(k) for k in range(6)], [90, 90, 110, 220, 220, 220])}

def bite(k):                                                                           # 8 frames: draw back, the neck lunges, the jaw snaps shut on frame 4, recoil (contact on frame 4)
    T = [dict(),
         dict(lean=-6, dx=-2, head=(-3, 1, 6), jaw=6, armn=(46, 0, 22, -4)),
         dict(lean=-10, dx=-4, head=(-5, 2, 10), jaw=10, armn=(52, 8, 24, 0)),
         dict(lean=8, dx=5, head=(6, 3, -4), jaw=12, armn=(36, -16, 20, -10), armf=(22, -4, 14)),
         dict(lean=16, dx=10, head=(13, 5, -9), jaw=1, armn=(30, -18, 16, -12), armf=(26, -2, 14)),
         dict(lean=15, dx=10, head=(12, 5, -9), jaw=0, armn=(30, -18, 16, -12)),
         dict(lean=8, dx=5, head=(5, 2, -3), jaw=3),
         dict(lean=2, dx=1, head=(1, 0, -1), jaw=0)]
    d = dict(T[k]); d.setdefault('legn', (33, -24, 25) if k in (3, 4, 5) else (29.7, -26.6, 25)); d.setdefault('legf', (22, -32, 20) if k in (3, 4, 5) else (26.6, -26.6, 20)); return d
def idle(k, n=8):                                                                      # breathing: the chest and shoulders rise, the head sways, a finger twitches
    f = 2 * PI * k / n
    return dict(dy=-0.7 * S(f), lean=0.8 * S(f), head=(0.3 * S(f + 1), 0.6 * S(f + 0.4), 1.6 * S(f + 1.2)), jaw=(1 if S(f + 2) > 0.6 else 0), armn=(39.8 + 1.2 * S(f), -12.5, 21.8 + 1.5 * S(f + 1), -8.1))
def death(k):                                                                          # 8 frames: thrown back by the blow, the knees go, it folds forward and falls on its side (the last frames turn the whole picture)
    T = [dict(lean=-12, dx=-3, head=(-3, 0, 16), jaw=8, armn=(30, 20, 30, 20), armf=(30, 10, 20)),
         dict(lean=-6, dx=-4, dy=4, head=(-2, 3, 8), jaw=6, legn=(55, -10, 20), legf=(50, -12, 20), armn=(25, 10, 24, 14)),
         dict(lean=-14, dx=-2, dy=12, head=(-3, 6, 14), jaw=5, legn=(75, 10, 30), legf=(70, 8, 30), armn=(30, 15, 25, 10)),
         dict(lean=-22, dx=-1, dy=22, head=(-6, 10, 22), jaw=4, legn=(86, 40, 40), legf=(82, 36, 36), armn=(40, 30, 20, 8), armf=(36, 30, 24))]
    return dict(T[k]) if k < 4 else None
ANIMS['bite'] = ([bite(k) for k in range(8)], [110, 130, 150, 60, 50, 80, 110, 110])
ANIMS['idle'] = ([idle(k) for k in range(8)], [160] * 8)

def build(out):
    os.makedirs(out, exist_ok=True)
    frames = {name: [render(**p) for p in poses] for name, (poses, ms) in ANIMS.items()}
    frames['still'] = [render()]
    dth = [render(**death(k)) for k in range(4)]                                       # then the fall: the last collapsed picture turns onto its side about its feet
    base = dth[3]; bb0 = base.getbbox(); feet = ((bb0[0] + bb0[2]) // 2, bb0[3])
    for ang in (30, 58, 82, 90):
        r = base.rotate(ang, resample=Image.NEAREST, center=(feet[0] - 14, feet[1]), expand=False); dth.append(r)      # thrown back: it falls over backwards
    frames['death'] = dth; ANIMS['death'] = (None, [90, 120, 130, 160, 120, 110, 110, 900])
    allf = [f for fs in frames.values() for f in fs]
    bb = [f.getbbox() for f in allf]; y0 = min(b[1] for b in bb) - 1; y1 = max(b[3] for b in bb) + 1
    sb = frames['still'][0].getbbox(); xc = (sb[0] + sb[2]) // 2                      # the standing figure's centre: the sprite pivot (0.5) must stay on it in every strip
    size = {}
    for n, fs in list(frames.items()):
        half = max(max(abs(f.getbbox()[0] - xc), abs(f.getbbox()[2] - xc)) for f in fs) + 2
        frames[n] = [f.crop((xc - half, y0, xc + half, y1)) for f in fs]; size[n] = frames[n][0].size
        strip = Image.new('RGBA', (size[n][0] * len(fs), size[n][1]), (0, 0, 0, 0))
        for i, f in enumerate(frames[n]): strip.alpha_composite(f, (i * size[n][0], 0))
        strip.save(os.path.join(out, 'gaunt_%s_strip.png' % n))
    W, H = size['still']
    meta = {n: dict(frame=list(size[n]), ms=ANIMS[n][1], travel=[0] * len(ANIMS[n][1])) for n in ANIMS}
    json.dump({'gaunt': {'claw': meta['claw'], 'bite': meta['bite']}}, open(os.path.join(out, 'gaunt_attacks.json'), 'w'), indent=1)
    json.dump({n: dict(frame=list(size[n]), ms=ANIMS[n][1]) for n in ANIMS}, open(os.path.join(out, 'gaunt_anims.json'), 'w'), indent=1)
    return frames, (W, H)
def gif(frames, W, H, out):                                                            # half speed, on the alarm-red dark: every action in a row
    names = ['idle', 'walk', 'run', 'claw', 'bite', 'scream', 'point', 'death']; Sx = 2
    seq = []
    for n in names:
        ms = ANIMS[n][1]
        for rep in range(2 if n in ('walk', 'run') else 1):
            for i, f in enumerate(frames[n]): seq.append((n, f, ms[i] * 2))
    ims = []; durs = []
    for n, f, d in seq:
        c = Image.new('RGBA', (W * Sx + 20, H * Sx + 34), (26, 14, 14, 255)); big = f.resize((W * Sx, H * Sx), Image.NEAREST); c.alpha_composite(big, (10, 24))
        from PIL import ImageDraw; ImageDraw.Draw(c).text((10, 6), n, fill=(235, 225, 200, 255)); ims.append(c.convert('P', palette=Image.ADAPTIVE)); durs.append(d)
    ims[0].save(out, save_all=True, append_images=ims[1:], duration=durs, loop=0, disposal=2)
def export(out):
    A = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites', 'Mutants'); os.makedirs(os.path.join(A, 'anim'), exist_ok=True)
    import shutil
    # the standing sprite is the rig's neutral frame on the same canvas as every strip (nothing jumps between poses)
    idle = Image.open(os.path.join(out, 'gaunt_still_strip.png')); idle.save(os.path.join(A, 'mutant_hd_gaunt.png'))
    for n in ('walk', 'run', 'scream', 'point', 'idle', 'death'): shutil.copy(os.path.join(out, 'gaunt_%s_strip.png' % n), os.path.join(A, 'anim', 'gaunt_%s.bytes' % n))
if __name__ == '__main__':
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'gaunt'); fr, (W, H) = build(out); gif(fr, W, H, os.path.join(out, 'gaunt_actions.gif')); export(out); print('canvas', W, H)
