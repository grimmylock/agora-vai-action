#!/usr/bin/env python3
"""The Elder Gaunt's Pixel Lab animations (ArtSource/mutants_anim/gaunt/pixellab/, README there) -> game strips + timing table (2026-11-19).
Every strip frame is a 250x142 cell: the standing sprite (138x130, Resources/Sprites/Mutants/mutant_hd_elder_gaunt.png, now the 2-armed base) sits with its bottom on the cell's bottom edge and its centre on the cell's
centre, so the usual pivot (0.5, 16 px above the bottom) is the standing sprite's own pivot and the view's left/right flip turns the whole cell about the body. The 200x150 Pixel Lab frames carry the sprite at (6,12), the slam run's
138x130 frames at the same place; frames that dip below the sprite's feet (kneeling toes) are lifted by the overshoot so the ground stays on the cell bottom. Nothing is resampled.
python3 ArtSource/export_elder_clips.py -> Assets/Resources/Sprites/Mutants/anim/elder_gaunt_<clip>.bytes + Assets/Scripts/GameCore/ElderClips.cs"""
import os, io
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
PL = os.path.join(HERE, 'mutants_anim', 'gaunt', 'pixellab'); OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Sprites', 'Mutants', 'anim')
CW, CH, SX, SY = 250, 142, 6, 12          # cell, and where the standing sprite sits on the 200x150 canvas
PX = 50                                   # canvas -> cell: 250 = 2 * (50 + 6 + 69), the sprite's centre at 125

def src(folder, pat, k): return os.path.join(PL, folder, pat % k)
def cell(path, shift=0, small=False):
    im = Image.open(path).convert('RGBA'); cv = Image.new('RGBA', (200, 150), (0, 0, 0, 0))
    cv.alpha_composite(im, (SX, SY) if small else (0, 0))
    a = np.array(cv); low = int(np.nonzero(a[..., 3].any(axis=1))[0].max()); lift = max(0, low - (CH - 1))
    out = Image.new('RGBA', (CW, CH), (0, 0, 0, 0)); out.alpha_composite(cv.crop((0, lift, 200, lift + CH)) if lift else cv.crop((0, 0, 200, CH)), (PX + shift, 0))
    return out

def shifted(path, dx):
    c = cell(path); a = np.roll(np.array(c), dx, axis=1); return Image.fromarray(a, 'RGBA')

clips = {}
def add(name, frames, steps, ms, contacts=None):
    assert len(steps) == len(ms), name
    clips[name] = dict(frames=frames, steps=steps, ms=ms, contacts=contacts or [])

def frames(folder, pat, ks, small=False): return [cell(src(folder, pat, k), small=small) for k in ks]
def ident(n): return list(range(n))
def cum(ms, i): return sum(ms[:i])

# idle (loops), walk (loops)
add('idle', frames('idle', 'idle_%d.png', range(8)), ident(8), [170] * 8)
add('walk', frames('walk_loop', 'walk_%d.png', range(8)), ident(8), [110] * 8)
# scream + point (2.6 s: the call lands at 1.2 s, the arm fully out)
sp_ms = [150, 120, 120, 140, 160, 180, 80, 160, 220, 260, 260, 260, 490]
add('scream', frames('scream_point', 'sp_%d.png', range(13)), ident(13), sp_ms)
# the lingering enrage roar: scream frames 0-5, then 1.6 s head-back with a baked 1 px shake, then released backwards
rf = frames('scream_point', 'sp_%d.png', range(6)); p = lambda k: src('scream_point', 'sp_%d.png', k)
rf += [shifted(p(4), 1), shifted(p(5), -1), shifted(p(4), -1), shifted(p(5), 1)]
steps = [0, 1, 2, 3, 4, 5] + [6, 7, 8, 9] * 4 + [3, 2, 1, 0]; ms = [120, 110, 110, 130, 150, 170] + [100] * 16 + [110] * 4
add('roar', rf, steps, ms)
# the standalone slam (1 s warning, hit on step 5) and the shriek (a lunge from the crouch; the wave leaves at 0.55 s)
sl_ms = [120, 220, 260, 300, 100, 80, 250, 200, 300]
add('slam', frames('slam', 'slam_%d.png', range(9), small=True), ident(9), sl_ms, [cum(sl_ms, 5)])
add('shriek', frames('combo3', 'c3_%d.png', range(8, 17)), ident(9), [500, 60, 90, 110, 120, 160, 180, 200, 240], [550])
# the rise from the kneel and the death (the last frame holds)
add('rise', frames('rise', 'rise_%d.png', range(13)), ident(13), [220, 200, 200, 180, 170, 160, 150, 140, 130, 130, 150, 200, 400])
add('death', frames('death', 'death_%d.png', range(13)), ident(13), [140, 140, 150, 160, 170, 180, 190, 200, 220, 260, 320, 400, 1000])
# the combos: 2 hits (claw, claw), 3 (+ the lunge), 4 (+ the slam). Contacts = ms at which a hit lands (a frame's start).
c2_ms = [100, 120, 140, 140, 60, 90, 120, 140, 60, 90, 160, 200, 260]
e = frames('combo2', 'c2_%d.png', range(13))
add('combo2', e, ident(13), c2_ms, [cum(c2_ms, 4), cum(c2_ms, 8)])
f = frames('combo3', 'c3_%d.png', range(8, 17))
c3_ms = [100, 120, 140, 140, 60, 90, 120, 140, 60, 90, 120, 80, 80] + [120, 60, 90, 120, 140, 160, 180, 200, 240]
add('combo3', e + f, ident(22), c3_ms, [cum(c3_ms, 4), cum(c3_ms, 8), cum(c3_ms, 14)])
fsel = [f[k] for k in (0, 1, 2, 3, 4, 6, 8)]                                   # run F frames 8,9,10,11,12,14,16
sl = frames('slam', 'slam_%d.png', range(1, 9), small=True)
c4_ms = [100, 120, 140, 140, 60, 90, 120, 140, 60, 90, 120, 70, 70] + [110, 60, 90, 110, 120, 90, 90] + [110, 140, 150, 90, 60, 70, 220, 300]
add('combo4', e + fsel + sl, ident(28), c4_ms, [cum(c4_ms, 4), cum(c4_ms, 8), cum(c4_ms, 14), cum(c4_ms, 24)])

os.makedirs(OUT, exist_ok=True)
for name, c in clips.items():
    strip = Image.new('RGBA', (CW * len(c['frames']), CH), (0, 0, 0, 0))
    for i, fr in enumerate(c['frames']): strip.alpha_composite(fr, (i * CW, 0))
    buf = io.BytesIO(); strip.save(buf, 'PNG'); open(os.path.join(OUT, 'elder_gaunt_%s.bytes' % name), 'wb').write(buf.getvalue())
    print(name, len(c['frames']), 'cells,', len(c['steps']), 'steps,', sum(c['ms']), 'ms', c['contacts'])

lines = ['// GENERATED by ArtSource/export_elder_clips.py - do not edit by hand. The Elder Gaunt\'s Pixel Lab clips (ArtSource/mutants_anim/gaunt/pixellab/): strip size, the order the cells play (Step), ms per step and, for the combos, the ms at which a hit lands.',
         'using System.Collections.Generic;', '', 'namespace GameCore', '{', '    public static partial class ElderClips', '    {', '        public static readonly Dictionary<string, Clip> All = new Dictionary<string, Clip>', '        {']
for name, c in clips.items():
    A = lambda xs: ('new[] { ' + ', '.join(str(x) for x in xs) + ' }') if xs else 'new int[0]'
    lines.append('            ["%s"] = new Clip { Cells = %d, Step = %s, Ms = %s, Contacts = %s, TotalMs = %d },' % (name, len(c['frames']), A(c['steps']), A(c['ms']), A(c['contacts']), sum(c['ms'])))
lines += ['        };', '        public const int W = %d, H = %d;' % (CW, CH), '        public const float PivotY = 16f / %d;' % CH, '    }', '}']
open(os.path.join(ROOT, 'Assets', 'Scripts', 'GameCore', 'ElderClips.Data.cs'), 'w').write('\n'.join(lines) + '\n')
