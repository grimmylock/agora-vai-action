#!/usr/bin/env python3
"""Run strips snapped to the standing sprite's colours (2026-11-16, Yan: "check their color pallets and match the standing, walking and running ones, in the preview the run for dread looks like he's another color").
SpriteCook's gallops drift: near-black (9,8,9) where the stand uses (1,1,1), body browns a few steps off, stray greys at the paws. Every opaque pixel of <key>_run.bytes goes to the nearest colour of the
stand + walk palettes (the walk is a hand-built or already matched strip), weighted so the dark end keeps its steps. The untouched strips are kept in ArtSource/mutants_anim/run_strips_original/.
   python3 ArtSource/match_run_palette.py [key ...]"""
import os, sys, shutil, numpy as np
from PIL import Image
A = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites', 'Mutants') + '/'
KEEP = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'run_strips_original') + '/'
KEYS = ['pallid_prowler', 'veil_prowler', 'dread_prowler', 'blind_maw']
def colours(path, minshare=0.0):
    a = np.array(Image.open(path).convert('RGBA')).reshape(-1, 4); a = a[a[:, 3] > 0][:, :3]; u, c = np.unique(a, axis=0, return_counts=True)
    return u[c / c.sum() >= minshare]
def run(k):
    pal = np.unique(np.vstack([colours(A + f'mutant_hd_{k}.png'), colours(A + f'anim/{k}_walk.bytes', 0.0005)]), axis=0).astype(float)
    src = A + f'anim/{k}_run.bytes'; os.makedirs(KEEP, exist_ok=True)
    if not os.path.exists(KEEP + f'{k}_run.bytes'): shutil.copy(src, KEEP + f'{k}_run.bytes')
    im = np.array(Image.open(KEEP + f'{k}_run.bytes').convert('RGBA')); m = im[..., 3] > 0; px = im[m][:, :3].astype(float)
    # luminance-aware distance: the eye sees steps in the darks, so the dark colours are not allowed to merge as freely as the lights
    w = np.array([0.30, 0.59, 0.11]); d = (((px[:, None, :] - pal[None, :, :]) ** 2) * w).sum(-1) ** 0.5
    new = pal[d.argmin(1)]; out = im.copy(); out[m, :3] = new.astype(np.uint8)
    Image.fromarray(out).save(A + f'anim/{k}_run.png'); os.replace(A + f'anim/{k}_run.png', src)
    print(k, 'palette', len(pal), 'colours; mean abs change', round(float(np.abs(new - px).mean()), 2), '; colours in the run now', len(np.unique(new, axis=0)))
if __name__ == '__main__':
    for k in (sys.argv[1:] or KEYS): run(k)
