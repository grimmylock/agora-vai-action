#!/usr/bin/env python3
"""The two Gaunts' SpriteCook animations into the game (2026-11-16). Downloads each finished job's spritesheet (jobs.json), cuts it into frames, removes stray specks (a loose bone or crumb the animator
leaves drifting: components under 8% of the largest piece), crops every frame of a strip to ONE box (feet on the bottom row, symmetric about the figure's centre so the sprite pivot stays on it) and writes
the strips the game loads: Sprites/Mutants/anim/<who>_walk.bytes (and _scream.bytes), and for the claw the attack strip + table input for export_mutant_attacks.py.
   python3 ArtSource/gaunt_spritecook_export.py [name ...]      names: elder_walk elder_claw elder_call wild_walk wild_claw wild_call"""
import os, sys, io, json, subprocess, urllib.request
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
HERE = os.path.dirname(os.path.abspath(__file__)); SC = os.path.join(HERE, 'mutants_anim', 'gaunt', 'spritecook'); A = os.path.join(HERE, '..', 'Assets', 'Resources', 'Sprites', 'Mutants', 'anim')
JOBS = json.load(open(os.path.join(SC, 'jobs.json')))
WHO = {'elder': 'elder_gaunt', 'wild': 'gaunt'}; FW = 144
MS = {'walk': [110] * 8, 'claw': [110, 150, 140, 70, 70, 60, 110, 110], 'call': [200, 260, 300, 380, 380, 420, 380, 280]}
def fetch(name):
    d = json.loads(subprocess.run(['python3', '-I', os.path.join(HERE, 'spritecook_mcp.py'), 'check_job_status', json.dumps({'job_id': JOBS[name]})], capture_output=True, text=True).stdout)
    if d.get('status') != 'succeeded': raise SystemExit(f'{name}: {d.get("status")}')
    im = Image.open(io.BytesIO(urllib.request.urlopen(d['output']['spritesheet_url'], timeout=90).read())).convert('RGBA'); fw = d['output']['metadata']['frame_width']
    return [im.crop((i * fw, 0, (i + 1) * fw, im.height)) for i in range(im.width // fw)]
def clean(f):
    a = np.array(f); m = a[..., 3] > 0; lab, n = ndi.label(m, structure=np.ones((3, 3)))
    if n <= 1: return f
    sizes = ndi.sum(m, lab, range(1, n + 1)); keep = [i + 1 for i, s in enumerate(sizes) if s >= 0.08 * sizes.max()]
    a[~np.isin(lab, keep)] = 0; return Image.fromarray(a)
def build(name, frames):
    kind = name.split('_')[1]; who = WHO[name.split('_')[0]]
    fs = [clean(f) for f in frames]; bb = [f.getbbox() for f in fs]
    y0 = min(b[1] for b in bb); y1 = max(b[3] for b in bb); xc = int(np.mean([(b[0] + b[2]) / 2 for b in bb]))
    half = max(max(abs(b[0] - xc), abs(b[2] - xc)) for b in bb) + 2
    fs = [f.crop((xc - half, y0, xc + half, y1)) for f in fs]; W, H = fs[0].size
    strip = Image.new('RGBA', (W * len(fs), H), (0, 0, 0, 0))
    for i, f in enumerate(fs): strip.alpha_composite(f, (i * W, 0))
    d = os.path.join(SC, name); os.makedirs(d, exist_ok=True)
    for i, f in enumerate(fs): f.save(os.path.join(d, f'frame{i}.png'))
    strip.save(os.path.join(d, 'strip.png'))
    if kind == 'walk': strip.save(os.path.join(A, f'{who}_walk.png')); os.replace(os.path.join(A, f'{who}_walk.png'), os.path.join(A, f'{who}_walk.bytes'))
    elif kind == 'call': strip.save(os.path.join(A, f'{who}_scream.png')); os.replace(os.path.join(A, f'{who}_scream.png'), os.path.join(A, f'{who}_scream.bytes'))
    elif kind == 'claw':
        od = os.path.join(HERE, 'mutants_anim', f'gaunt_{name.split("_")[0]}'); os.makedirs(od, exist_ok=True)
        strip.save(os.path.join(od, f'{who}_claw_strip.png'))
        json.dump({who: {'claw': {'frame': [W, H], 'ms': MS['claw'], 'travel': [0] * 8}}}, open(os.path.join(od, f'{who}_attacks.json'), 'w'), indent=1)
    print(name, 'frame', W, 'x', H, '->', who, kind)
if __name__ == '__main__':
    for n in (sys.argv[1:] or [k for k in JOBS]): build(n, fetch(n))
