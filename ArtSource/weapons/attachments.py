"""Optics and under-barrel attachments (2026-11-16, Yan: PU 2x for old rifles, ACOG 4x and an 8x for modern ones, red dots for sidearms and SMGs, a laser for sidearms).
Hand-drawn in code, side view facing right, drawn at 6 texture px per in-game pixel (the double-size gun picture's density), so one sprite fits every gun.
Writes Assets/Resources/Sprites/Attachments/att_<id>.png and prints each sprite's pivot (texture px) for Optics.cs. Run: python3 ArtSource/weapons/attachments.py"""
import numpy as np
from PIL import Image
OUT = 'Assets/Resources/Sprites/Attachments/'
K = (12, 12, 16); D = (34, 36, 43); M = (62, 67, 78); L = (100, 107, 122); S = (158, 166, 182)
class C:
    def __init__(s, w, h): s.a = np.zeros((h, w, 4), np.uint8)
    def px(s, x, y, c, al=255):
        if 0 <= x < s.a.shape[1] and 0 <= y < s.a.shape[0]: s.a[y, x] = (*c, al)
    def rect(s, x0, y0, x1, y1, c, al=255):                       # x1, y1 exclusive
        for y in range(y0, y1):
            for x in range(x0, x1): s.px(x, y, c, al)
    def tube(s, x0, y0, x1, y1):                                   # a steel cylinder: highlight, light, mid, dark, shadow rows
        h = y1 - y0
        for j in range(h):
            t = j / max(1, h - 1); c = S if j == 0 else L if t < 0.3 else M if t < 0.6 else D
            s.rect(x0, y0 + j, x1, y0 + j + 1, c)
    def outline(s):                                                # 1 px black round everything drawn
        a = s.a[..., 3] > 0; pad = np.pad(a, 1); grown = np.zeros_like(pad)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dx or dy: grown |= np.roll(np.roll(pad, dy, 0), dx, 1)
        edge = grown & ~pad; out = np.zeros((a.shape[0] + 2, a.shape[1] + 2, 4), np.uint8); out[1:-1, 1:-1] = s.a
        out[edge] = (*K, 255); return out
def save(name, c, pivot):
    img = c.outline(); Image.fromarray(img).save(OUT + f'att_{name}.png'); print(name, img.shape[1], 'x', img.shape[0], 'pivot', (pivot[0] + 1, pivot[1] + 1))   # +1: the outline margin
def pu2():                                                         # a long thin side-mounted scope with a flared front bell
    c = C(62, 16); c.tube(8, 3, 52, 10); c.tube(52, 1, 61, 12); c.tube(0, 2, 9, 11)
    c.rect(57, 3, 60, 10, (70, 130, 150)); c.px(58, 4, (170, 220, 235)); c.px(58, 5, (170, 220, 235))   # the front lens
    c.rect(26, 0, 33, 3, M); c.px(27, 0, L); c.px(28, 0, L)                                         # elevation turret
    c.rect(19, 10, 24, 12, D); c.rect(35, 10, 40, 12, D); c.rect(16, 12, 44, 15, M); c.rect(16, 14, 44, 15, D)   # rings and the side mount base
    for x in (20, 37): c.px(x, 13, L)
    save('pu2', c, (30, 15)); return c
def acog():                                                        # a compact prism sight: chunky body, amber fibre tube on top
    c = C(40, 19); c.tube(4, 5, 30, 15); c.tube(30, 4, 39, 15); c.tube(0, 6, 5, 13)
    c.rect(6, 1, 26, 4, (176, 122, 40)); c.rect(6, 1, 26, 2, (230, 176, 70)); c.rect(10, 2, 14, 4, (200, 140, 48))   # the fibre-optic tube
    c.rect(35, 6, 38, 13, (70, 130, 150)); c.px(36, 7, (170, 220, 235)); c.px(36, 8, (170, 220, 235))
    c.rect(8, 15, 28, 18, M); c.rect(8, 17, 28, 18, D); c.rect(15, 15, 20, 18, D)                 # the mount and its notch
    save('acog', c, (18, 18)); return c
def scope8():                                                      # a long variable scope: big bell, turrets, two rings
    c = C(76, 19); c.tube(14, 4, 62, 13); c.tube(62, 1, 75, 16); c.tube(2, 3, 15, 14)
    c.rect(70, 4, 74, 13, (70, 130, 150)); c.px(71, 5, (170, 220, 235)); c.px(71, 6, (170, 220, 235)); c.px(71, 7, (170, 220, 235))
    c.rect(30, 0, 37, 4, M); c.rect(31, 0, 36, 1, L); c.rect(46, 1, 51, 4, M); c.px(47, 1, L)   # the turrets
    c.rect(24, 13, 30, 16, D); c.rect(50, 13, 56, 16, D); c.rect(20, 15, 60, 18, M); c.rect(20, 17, 60, 18, D)
    for x in (25, 52): c.px(x, 14, L)
    c.rect(66, 3, 69, 5, L)                                         # bell glint
    save('scope8', c, (40, 18)); return c
def reddot():                                                      # a small reflex sight: low hood, tinted window, one red dot
    c = C(24, 16); c.rect(2, 3, 20, 12, M); c.rect(2, 3, 20, 4, L); c.rect(2, 11, 20, 12, D)
    c.rect(2, 1, 20, 3, D); c.rect(2, 1, 20, 2, M)                                                   # hood
    c.rect(14, 4, 19, 11, (60, 110, 130)); c.rect(14, 4, 19, 5, (120, 175, 195)); c.px(16, 7, (255, 70, 55)); c.px(15, 7, (255, 110, 90))
    c.rect(2, 12, 22, 15, M); c.rect(2, 14, 22, 15, D); c.rect(7, 12, 11, 15, D)
    save('reddot', c, (12, 15)); return c
def laser():                                                       # a small box under the barrel, red lens at the front, a clamp on top
    c = C(20, 12); c.rect(2, 3, 17, 11, M); c.rect(2, 3, 17, 4, L); c.rect(2, 10, 17, 11, D)
    c.rect(17, 4, 19, 10, (150, 30, 30)); c.px(18, 6, (255, 110, 100)); c.px(18, 7, (255, 110, 100))
    c.rect(4, 0, 14, 3, D); c.rect(4, 0, 14, 1, M); c.rect(10, 5, 13, 8, D)                        # the clamp and a switch
    save('laser', c, (10, 0)); return c
if __name__ == '__main__':
    cs = [pu2(), acog(), scope8(), reddot(), laser()]
    sheet = Image.new('RGB', (900, 140), (84, 92, 80))
    x = 6
    for c in cs:
        im = Image.fromarray(c.outline()); im = im.resize((im.width * 3, im.height * 3), Image.NEAREST); sheet.paste(im, (x, 10), im); x += im.width + 12
    sheet.save('ArtSource/weapons/attachments_preview.png')
