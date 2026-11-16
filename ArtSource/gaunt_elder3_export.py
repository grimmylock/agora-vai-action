#!/usr/bin/env python3
"""Elder Gaunt v3 into the game (2026-11-16): the stand-still sprite (rest pose, third arm folded), the walk strip and the scream strip. The claw strip goes in through export_mutant_attacks.py
(it reads mutants_anim/gaunt_elder/elder_gaunt_claw_strip.png + elder_gaunt_attacks.json, which gaunt_elder3_anim.py writes), the hit silhouette through make_hit_profiles.py.
Rules the loaders expect (UnitSprites.HD / GauntAnim / HDStrip): the picture is a whole texture (or a strip of equal cells), the pivot is the cell's centre column and 16 px above the bottom row, so the feet are
on the bottom row and the body sits on one anchor column in every picture.
   python3 ArtSource/gaunt_elder3_anim.py && python3 ArtSource/gaunt_elder3_walk.py <walk spritesheet> && python3 ArtSource/gaunt_elder3_export.py"""
import os, shutil, numpy as np
from PIL import Image
import gaunt_elder3 as G, gaunt_elder3_anim as X
HERE = os.path.dirname(os.path.abspath(__file__)); R = os.path.join(HERE, '..', 'Assets', 'Resources', 'Sprites', 'Mutants'); A = os.path.join(R, 'anim')
def still():
    rest = G.compose('fold')[0]; an = X.anchor(); bb = rest.getbbox(); half = max(an - bb[0], bb[2] - an) + 2
    return rest.crop((an - half, bb[1] - 1, an + half, X.BOT + 1))                      # bottom row = the feet row
if __name__ == '__main__':
    s = still(); s.save(os.path.join(R, 'mutant_hd_elder_gaunt.png')); print('still', s.size)
    for src, dst in (('elder_gaunt_walk_strip.png', 'elder_gaunt_walk.bytes'), ('elder_gaunt_scream_strip.png', 'elder_gaunt_scream.bytes')):
        shutil.copy(os.path.join(X.OUT, src), os.path.join(A, dst)); im = Image.open(os.path.join(A, dst)); print(dst, im.size, 'cell', im.width // 8, 'x', im.height)
