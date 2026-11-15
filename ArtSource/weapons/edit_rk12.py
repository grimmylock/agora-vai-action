#!/usr/bin/env python3
"""RK-12 Kuvalda, variation A: A's blocky shape was right, its hammer was a scribble and its handguard had white glare (Yan, 2026-11-15, no new credits): (1) the white glare pixels (brightness > 215) become a mid grey, (2) the scribble behind the cylinder (x 8-29, y 11-23 of the still) is cleared and a clean curved hammer horn is drawn in its place, in the frame's own greys with the dark outline.
Works on batch10/RK12Kuvalda_A.png (the original is kept as RK12Kuvalda_A_orig.png); then: python3 ArtSource/weapons/batch_native.py install ArtSource/weapons/spritecook_pilot/batch10.json A,A,B,B"""
import os, shutil, numpy as np
from PIL import Image, ImageDraw
D = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'spritecook_pilot', 'batch10'); P = os.path.join(D, 'RK12Kuvalda_A.png'); ORIG = os.path.join(D, 'RK12Kuvalda_A_orig.png')
if not os.path.exists(ORIG): shutil.copy(P, ORIG)
a = np.array(Image.open(ORIG).convert('RGBA')); lum = a[..., :3].mean(2)
a[(a[..., 3] > 0) & (lum > 215), :3] = (150, 152, 158)                                 # glare
reg = a[10:23, 78:101]; reg[(reg[..., 3] > 0) & (reg[..., :3].mean(2) > 185), :3] = (128, 131, 138)   # light edge lines round the knurled tube
a[11:24, 8:29] = 0                                                                     # the scribble
im = Image.fromarray(a, 'RGBA'); d = ImageDraw.Draw(im)
OUT, DK, MID, LT = (14, 14, 18, 255), (44, 46, 52, 255), (80, 83, 90, 255), (122, 126, 134, 255)
horn = [(29, 19), (26, 15), (22, 13), (17, 12), (13, 12), (12, 13), (15, 14), (20, 15), (24, 18), (28, 22), (29, 23)]
d.polygon(horn, fill=MID, outline=OUT)
d.polygon([(29, 17), (27, 15), (24, 17), (27, 21), (29, 23)], fill=MID)                    # a heavier base
d.line([(28, 18), (25, 15), (21, 13), (16, 12.5), (14, 12.5)], fill=LT)                # a highlight along the top of the horn
d.line([(29, 23), (24, 20), (20, 17)], fill=DK)                                       # its shadow side
Image.fromarray(np.array(im), 'RGBA').save(P); print('saved', P)
