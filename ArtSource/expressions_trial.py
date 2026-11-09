#!/usr/bin/env python3
"""Trial (Yan, 2026-11-09): THREE expressions (calm / fighting / hurt) for ONE suit, a random tier-1 pick: Heavy Trenchcoat (atlas cell 15). Edits only the face pixels of its atlas cell
(face = skin y3 x6-9 and y4 x5-10, eyes y4 x6/x9). Writes ArtSource/expressions_trial/trenchcoat_<state>.png (16 px) and _x2.png (32 px) and a sheet. Not in the atlases or the game.
   python3 ArtSource/expressions_trial.py"""
import os
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, 'expressions_trial'); os.makedirs(OUT, exist_ok=True)
SK, SKD, BR, EYE = (200, 145, 106, 255), (158, 110, 79, 255), (59, 42, 30, 255), (42, 32, 24, 255)
IDX = 15
def base():
    a = Image.open(os.path.join(HERE, '..', 'Assets/Resources/Sprites/armor_atlas.png')).convert('RGBA')
    c = a.crop((IDX % 16 * 16, IDX // 16 * 16, IDX % 16 * 16 + 16, IDX // 16 * 16 + 16)).copy(); p = c.load()
    for x in (6, 7, 8, 9): p[x, 3] = SK                                  # bare skin above the eyes, eyes dark: the calm face
    for x in (5, 7, 8, 10): p[x, 4] = SK
    p[6, 4] = p[9, 4] = EYE
    return c
def state(name):
    c = base(); p = c.load()
    if name == 'fighting':                       # brows pulled down at the middle (a V), the eyes glaring under them, cheek in shade
        p[7, 3] = p[8, 3] = BR; p[6, 3] = p[9, 3] = SK
        p[5, 4] = p[10, 4] = SKD
    elif name == 'hurt':                         # eyes screwed shut (two-pixel dashes at the outer sides), a pinched, shaded middle
        for x in (5, 6, 9, 10): p[x, 4] = EYE
        p[7, 4] = p[8, 4] = SKD
        p[7, 3] = p[8, 3] = SKD                  # a pinched forehead in shade, no dark brow (it merged with the dashes into a blindfold)
    return c
if __name__ == '__main__':
    names = ['calm', 'fighting', 'hurt']; sc = 14
    sh = Image.new('RGBA', (len(names) * (16 * sc + 10) + 10, 16 * sc + 20 + 16 * 4 + 20), (60, 58, 54, 255))
    for k, n in enumerate(names):
        c = state(n); c.save(os.path.join(OUT, f'trenchcoat_{n}.png')); c.resize((32, 32), Image.NEAREST).save(os.path.join(OUT, f'trenchcoat_{n}_x2.png'))
        sh.alpha_composite(c.resize((16 * sc, 16 * sc), Image.NEAREST), (10 + k * (16 * sc + 10), 10))
        sh.alpha_composite(c.resize((64, 64), Image.NEAREST), (10 + k * (16 * sc + 10), 16 * sc + 20))        # and at 4x, nearer the size it is seen at
    sh.convert('RGB').save(os.path.join(OUT, 'trenchcoat_expressions.png')); print('ok')
