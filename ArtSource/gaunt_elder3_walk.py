#!/usr/bin/env python3
"""Elder Gaunt v3 walk (2026-11-16). SpriteCook animated the TWO-armed body (job f19ad1e9, asset 13fce2c0: clean, all 8 frames); the first try on the three-armed rest pose came out tangled
(job d5701d27: debris and a mangled third arm in half the frames). The folded third arm is added here: for every walk frame the head is found again (silhouette match of the antlers and skull
against the still body) and the rest-pose arm (gaunt_elder3.arm_layer('fold')) is laid under the body at the same offset, its colours snapped to the frame's own palette.
   python3 ArtSource/gaunt_elder3_walk.py <spritesheet.png>   -> mutants_anim/gaunt_elder/elder_gaunt_walk_strip.png (+ preview GIF/sheet, anim/elder_gaunt_walk.bytes)"""
import os, sys, numpy as np
from PIL import Image
from scipy import ndimage as ndi
import gaunt_elder3 as G, gaunt_elder3_anim as X
HERE = os.path.dirname(os.path.abspath(__file__)); A = os.path.join(HERE, '..', 'Assets', 'Resources', 'Sprites', 'Mutants', 'anim')
TIGHT = (25, 58, 145, 191)               # the crop of the body that was uploaded for the walk (B3 frame)
FW = 146
def specks(f, frac=0.08):
    a = np.array(f); m = a[..., 3] > 0; lab, n = ndi.label(m, structure=np.ones((3, 3)))
    if n <= 1: return f
    sz = ndi.sum(m, lab, range(1, n + 1)); keep = [i + 1 for i, s in enumerate(sz) if s >= frac * sz.max()]; a[~np.isin(lab, keep)] = 0; return Image.fromarray(a)
def find_head(frame, template, region, rng=24):
    """best (dx, dy) laying the template's head region on the frame (silhouette IoU)."""
    fa = np.array(frame)[..., 3] > 0; ta = np.array(template)[..., 3] > 0; x0, y0, x1, y1 = region; t = ta[y0:y1, x0:x1]; best = (-1, 0, 0)
    for dy in range(-rng, rng + 1):
        for dx in range(-rng, rng + 1):
            ys, xs = y0 + dy, x0 + dx
            if ys < 0 or xs < 0 or ys + t.shape[0] > fa.shape[0] or xs + t.shape[1] > fa.shape[1]: continue
            f = fa[ys:ys + t.shape[0], xs:xs + t.shape[1]]; sc = (f & t).sum() / max(1, (f | t).sum())
            if sc > best[0]: best = (sc, dx, dy)
    return best
def snap(arm, frame):
    """arm colours -> nearest colour of the frame's own palette (the animator's walk uses ~24 colours)."""
    fa = np.array(frame); pal = np.unique(fa[fa[..., 3] > 0][:, :3], axis=0).astype(int); a = np.array(arm); m = a[..., 3] > 0
    px = a[m][:, :3].astype(int); d = ((px[:, None, :] - pal[None, :, :]) ** 2).sum(2); a[m, :3] = pal[d.argmin(1)]; return Image.fromarray(a)
def build(sheet_path):
    sheet = Image.open(sheet_path).convert('RGBA'); n = sheet.width // FW; frames = [specks(sheet.crop((i * FW, 0, (i + 1) * FW, sheet.height))) for i in range(n)]
    tight = G.load_body().crop(TIGHT); region = (50, 0, 120, 48)            # antlers + skull in the tight crop
    arm_full = G.arm_layer('fold'); out = []; info = []
    for f in frames:
        sc, dx, dy = find_head(f, tight, region)
        arm = snap(arm_full, f).crop((TIGHT[0], TIGHT[1], TIGHT[2] + 30, TIGHT[3]))       # +30 px of room on the right for the arm
        cv = Image.new('RGBA', (FW + 40, FW), (0, 0, 0, 0)); cv.alpha_composite(arm, (dx, dy)); cv.alpha_composite(f)     # the arm is BEHIND the body
        out.append(cv); info.append((round(sc, 2), dx, dy))
    return out, info
STILL_HEAD = -18          # where the still puts the skull template's left edge, in px from its cell's centre column (the walk is centred so the body does not jump when the game switches pictures)
def cells(fs, heads):
    bb = [f.getbbox() for f in fs]; y0 = min(b[1] for b in bb); y1 = max(b[3] for b in bb)
    xc = int(round(np.mean(heads) - STILL_HEAD)); half = max(max(abs(b[0] - xc), abs(b[2] - xc)) for b in bb) + 2
    return [f.crop((xc - half, y0, xc + half, y1)) for f in fs]
if __name__ == '__main__':
    fs, info = build(sys.argv[1]); print('head match (iou, dx, dy):', info)
    cs = cells(fs, [50 + i[1] for i in info]); W, H = cs[0].size; st = X.strip(cs); od = X.OUT; os.makedirs(od, exist_ok=True)
    st.save(os.path.join(od, 'elder_gaunt_walk_strip.png')); X.gif(cs, [110] * 8, os.path.join(X.PRE, 'elder3_walk.gif')); X.sheet(cs, os.path.join(X.PRE, 'elder3_walk_frames.png')); print('walk cell', W, 'x', H)
