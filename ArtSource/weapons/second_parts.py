#!/usr/bin/env python3
"""Drawn second parts (2026-11-16, Yan: "build the second part renderer"; the automatic rifles' charging handles and the VK-127's bolt).
The AK-pattern guns' handle sits on the far (right) side of the receiver, so the left-view pictures show none: this DRAWS a small steel handle knob on the receiver's top edge (a new detail, not Yan's pixels;
everything else is his picture untouched). The handle is weapon_<Key>_part2.png (style "charge"); the VK-127's bolt handle is weapon_<Key>_part.png (style "bolt", over the receiver); both come with
weapon_<Key>_body.png = the gun picture as it is (hdbody when the magazine is cut out). Run: python3 ArtSource/weapons/second_parts.py"""
import os, numpy as np
from PIL import Image
W = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Sprites', 'Weapons') + '/'
K = (14, 14, 18, 255); HI = (176, 182, 192, 255); MID = (112, 118, 130, 255); LO = (62, 66, 76, 255)
HANDLES = {'VK74': 100, 'VKM': 100, 'VKS74U': 100, 'VK74Obelisk': 100, 'VK103': 95}     # key: the receiver column the handle sits on
def src(k):
    fn = W + f'weapon_{k}_hdbody.png'
    return np.array(Image.open(fn if os.path.exists(fn) else W + f'weapon_{k}_hd.png').convert('RGBA'))
def px(a, x, y, c):
    if 0 <= y < a.shape[0] and 0 <= x < a.shape[1]: a[y, x] = c
def handle(k, x):
    base = src(k); col = np.where(base[:, x, 3] > 0)[0]; top = int(col.min())                       # the receiver's top edge at that column
    part = np.zeros_like(base)
    for dx in range(-3, 3):                                                                         # a 6 x 4 knob sitting on the ridge, a lit top row, a shaded bottom row, a dark outline
        for dy in range(-5, -1):
            edge = dx in (-3, 2) or dy == -5
            px(part, x + dx, top + dy, K if edge else HI if dy == -4 else LO if dy == -2 else MID)
    for dx in (-1, 0): px(part, x + dx, top - 1, LO)                                                  # the stem joins the knob to the ridge: no gap
    px(part, x - 2, top - 1, K); px(part, x + 1, top - 1, K)
    Image.fromarray(part).save(W + f'weapon_{k}_part2.png'); Image.fromarray(base).save(W + f'weapon_{k}_body.png')
    print(f'{k}: ["{k}"] = ("charge", 6f, 0f, 0f),')
def bolt(k, x, y):
    base = src(k); part = np.zeros_like(base)
    for i in range(6):                                                                              # the stem, standing up and leaning back from the bolt body
        px(part, x - i // 2, y - i, K); px(part, x + 1 - i // 2, y - i, HI); px(part, x + 2 - i // 2, y - i, HI); px(part, x + 3 - i // 2, y - i, K)
    cx, cy = x - 1, y - 8
    for dx in range(-2, 3):
        for dy in range(-2, 3):
            if abs(dx) + abs(dy) <= 3: px(part, cx + dx, cy + dy, K if abs(dx) + abs(dy) == 3 else HI if dx < 0 and dy < 0 else LO if dx > 0 and dy > 0 else MID)
    Image.fromarray(part).save(W + f'weapon_{k}_part.png'); Image.fromarray(base).save(W + f'weapon_{k}_body.png')
    print(f'{k}: ["{k}"] = ("bolt", 16f),')
if __name__ == '__main__':
    for k, x in HANDLES.items(): handle(k, x)
    bolt('VK127Grom', 92, 10)
