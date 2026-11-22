#!/usr/bin/env python3
"""2026-11-22 (Yan: 'the hitboxes do not match the sprite for a lot of the mutants, every part of the body must be damageable'): compares each mutant's hit silhouette (GameData/HitProfiles.cs, built from its STAND sprite) with
every frame it is drawn with (stand, walk, run, attack strips, both facings: the silhouette is symmetric about the unit's centre, so a flipped frame is the same test mirrored). Per mutant: the share of the opaque pixels of a frame that lie
OUTSIDE the hit silhouette (a part of the body a round cannot hit: worst frame and average) and the share of the silhouette's area with no sprite in it (a miss that counts as a hit). usage: python3 ArtSource/audit_hitboxes.py"""
import os, re, glob, numpy as np
from PIL import Image
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'); R = os.path.join(ROOT, 'Assets/Resources/Sprites/Mutants')
src = open(os.path.join(ROOT, 'Assets/Scripts/GameData/HitProfiles.cs')).read()
prof = {}
for n, ppt, vals in re.findall(r'\["([^"]+)"\] = \((\d+), new\[\] \{ ([^}]*) \}\)', src): prof[n] = (int(ppt), np.array([float(v.rstrip('f')) for v in vals.split(',')]))
ATT = open(os.path.join(ROOT, 'Assets/Scripts/GameData/MutantAttackData.cs')).read()
def frames_of(strip, n):
    w = strip.shape[1] // n; return [strip[:, i * w:(i + 1) * w] for i in range(n)]
def score(a, ppt, half):
    ys = np.where(a.any(1))[0]
    if not len(ys): return None
    top, bot = ys.min(), ys.max(); cx = a.shape[1] / 2.0; out = tot = 0; fill_empty = fill_all = 0
    for r, y in enumerate(range(bot, top - 1, -1)):
        xs = np.where(a[y])[0]; h = half[r] if r < len(half) else -1
        for x in xs:
            tot += 1
            if h < 0 or abs(x + 0.5 - cx) / ppt > h: out += 1
        if 0 <= h:
            span = 2 * h * ppt; fill_all += span; have = a[y].sum(); fill_empty += max(0.0, span - have)
    return (100.0 * out / max(1, tot), 100.0 * fill_empty / max(1.0, fill_all))
rows = []
for key, (ppt, half) in prof.items():
    if key.startswith('boss:') or key in ('human', 'The Specimen'): continue
    base = key.lower().replace(' ', '_'); frames = []
    f = os.path.join(R, 'mutant_hd_%s.png' % base)
    if os.path.exists(f): frames.append(('stand', np.array(Image.open(f).convert('RGBA'))[..., 3] > 0))
    for fn in sorted(glob.glob(os.path.join(R, 'anim', base + '_*.bytes'))):
        kind = os.path.basename(fn)[len(base) + 1:-6]; strip = np.array(Image.open(fn).convert('RGBA'))[..., 3] > 0
        m = re.search(r'\["%s"\] = new Dictionary.*?\["%s"\] = new AttackAnim \{ Frames = (\d+)' % (re.escape(key), re.escape(kind)), ATT, re.S)
        n = int(m.group(1)) if m else (strip.shape[1] // strip.shape[0] if strip.shape[1] % strip.shape[0] == 0 else 8)
        if kind in ('walk', 'run'): n = {'walk': 8, 'run': 8}[kind] if strip.shape[1] % 8 == 0 else max(1, round(strip.shape[1] / strip.shape[0]))
        if strip.shape[1] % n: continue                                                  # (the Gaunts' own clips have their own frame counts: not audited here)
        for i, fr in enumerate(frames_of(strip, n)): frames.append((kind, fr))
    sc = [(k, score(fr, ppt, half)) for k, fr in frames]; sc = [(k, s) for k, s in sc if s]
    if not sc: continue
    worst = max(sc, key=lambda t: t[1][0]); avg_out = np.mean([s[0] for _, s in sc]); avg_empty = np.mean([s[1] for _, s in sc])
    rows.append((key, len(sc), avg_out, worst[1][0], worst[0], avg_empty))
print('%-16s %6s %10s %10s %14s %11s' % ('mutant', 'frames', 'unhittable', 'worst', '(clip)', 'empty hitbox'))
for r in rows: print('%-16s %6d %9.1f%% %9.1f%% %14s %10.1f%%' % (r[0], r[1], r[2], r[3], r[4], r[5]))
