"""Optics and under-barrel attachments (2026-11-16, Yan: PU 2x for old rifles, ACOG 4x and an 8x for modern ones, red dots for sidearms and SMGs, a laser for sidearms).
Hand-drawn in code, side view facing right, drawn at 6 texture px per in-game pixel (the double-size gun picture's density), so one sprite fits every gun.
Writes Assets/Resources/Sprites/Attachments/att_<id>.png and prints each sprite's pivot (texture px) for Optics.cs. Run: python3 ArtSource/weapons/attachments.py"""
import numpy as np
from PIL import Image
OUT = 'Assets/Resources/Sprites/Attachments/'
K = (6, 6, 8); D = (15, 16, 18); M = (28, 29, 33); L = (48, 50, 56); S = (80, 82, 90)
W = (176, 176, 180); CH = (112, 114, 122); GR = (58, 118, 66); GRL = (96, 158, 98); GL = (16, 22, 28); GLL = (66, 88, 100)   # white dial rim, chrome, fibre green, lens glass
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
def ellipse(c, cx, cy, rx, ry, col, al=255):
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0: c.px(x, y, col, al)
def pu2():                                                         # reference: a slim matte tube, a rubber-ringed eyepiece, two rings, a white-rimmed turret on top and the big side-mount bracket with its round hole
    c = C(62, 24)
    c.tube(11, 4, 52, 11); c.tube(52, 3, 59, 12)                   # the tube and its thicker objective end
    c.rect(0, 3, 12, 12, D); c.tube(2, 3, 11, 12); c.rect(0, 5, 3, 10, M); c.px(1, 6, L)                         # eyepiece with its rubber ring
    c.rect(56, 4, 59, 11, GL); c.px(57, 5, GLL); c.px(57, 6, GLL)                                                  # objective glass
    c.rect(24, 1, 33, 4, D); c.rect(24, 1, 33, 2, L); c.rect(26, 0, 31, 1, W)                                     # the turret on top with its white dial rim
    for x in (14, 40): c.rect(x, 2, x + 5, 4, M); c.rect(x, 11, x + 5, 13, M); c.px(x + 1, 2, L)                    # the two rings
    c.rect(19, 13, 41, 23, M); c.rect(19, 13, 41, 14, L); c.rect(19, 22, 41, 23, D); c.rect(40, 13, 41, 23, D)    # the bracket: one tall plate under the tube ...
    c.rect(41, 19, 49, 23, D); c.rect(41, 19, 49, 20, M)                                                           # ... with a lug that hooks the receiver
    c.rect(15, 21, 19, 23, D)                                                                                       # and a toe at the other end
    for yy in range(15, 21):
        for xx in range(26, 33):
            if (xx - 29.0) ** 2 + (yy - 17.5) ** 2 <= 6.5: c.a[yy, xx] = (0, 0, 0, 0)                               # the round hole
    c.px(22, 15, S); c.px(37, 15, S)
    save('pu2', c, (31, 22)); return c
def acog():                                                        # reference: a black prism sight, a green fibre strip on top, a round adjuster cap on the side, a flat-top rail mount
    c = C(40, 22)
    c.tube(5, 6, 29, 16); c.tube(29, 5, 38, 16); c.tube(0, 8, 6, 15)
    c.rect(33, 7, 37, 14, GL); c.rect(33, 7, 37, 8, GLL); c.px(34, 9, GLL)                                         # objective glass
    c.rect(7, 3, 27, 6, D); c.rect(7, 3, 27, 4, M); c.rect(8, 4, 26, 5, GR); c.rect(14, 4, 20, 5, GRL)                    # the fibre housing is black like the body; one thin, muted green line shows
    c.rect(26, 4, 29, 6, D)                                                                                         # housing end
    ellipse(c, 15, 11, 3.2, 3.2, M); ellipse(c, 15, 11, 2.2, 2.2, L); c.px(14, 10, S)                                # the adjuster cap
    c.rect(8, 16, 30, 20, M); c.rect(8, 16, 30, 17, L); c.rect(8, 19, 30, 20, D); c.rect(18, 17, 22, 20, D)       # the rail mount and its clamp notch
    c.px(10, 18, S); c.px(27, 18, S)
    save('acog', c, (19, 21)); return c
def scope8():                                                      # reference: a long matte variable scope, flip-up caps open at both ends, turrets, two rings
    c = C(80, 26)
    c.tube(14, 8, 62, 16); c.tube(62, 4, 77, 19); c.tube(3, 6, 15, 17)
    c.rect(72, 7, 76, 17, GL); c.rect(72, 7, 76, 8, GLL); c.px(73, 9, GLL); c.px(73, 10, GLL)                      # objective glass
    c.rect(3, 8, 5, 15, M); c.px(3, 9, L)                                                                          # eyepiece ring
    ellipse(c, 74, 2.5, 5.5, 2.4, D); ellipse(c, 74, 2.5, 4.2, 1.5, M); c.rect(70, 3, 78, 5, D)                      # the front cap, flipped up
    ellipse(c, 6, 3.0, 4.5, 2.2, D); ellipse(c, 6, 3.0, 3.2, 1.3, M); c.rect(3, 4, 10, 6, D)                         # the rear cap, flipped up
    c.rect(31, 3, 41, 9, D); c.rect(31, 3, 41, 4, L); c.rect(33, 2, 39, 3, M); c.rect(32, 5, 40, 6, M)               # the big turret on top
    c.rect(47, 5, 53, 9, D); c.rect(47, 5, 53, 6, L)                                                                # the second turret
    for x in (24, 54): c.rect(x, 6, x + 6, 8, CH); c.rect(x, 16, x + 6, 19, M); c.rect(x, 6, x + 6, 7, S)           # chrome-edged rings
    c.rect(20, 19, 62, 22, M); c.rect(20, 19, 62, 20, L); c.rect(20, 21, 62, 22, D)                                # the rail clamp
    c.rect(66, 6, 69, 8, L)
    save('scope8', c, (41, 24)); return c
def reddot():                                                      # a small reflex sight: low hood, tinted window, one red dot
    c = C(24, 16); c.rect(2, 3, 20, 12, M); c.rect(2, 3, 20, 4, L); c.rect(2, 11, 20, 12, D)
    c.rect(2, 1, 20, 3, D); c.rect(2, 1, 20, 2, M)                                                   # hood
    c.rect(14, 4, 19, 11, GL); c.rect(14, 4, 19, 5, GLL); c.px(16, 7, (255, 70, 55)); c.px(15, 7, (255, 110, 90))
    c.rect(2, 12, 22, 15, M); c.rect(2, 14, 22, 15, D); c.rect(7, 12, 11, 15, D)
    save('reddot', c, (12, 15)); return c
def laser():                                                       # a small box under the barrel, red lens at the front, a clamp on top
    c = C(20, 12); c.rect(2, 3, 17, 11, M); c.rect(2, 3, 17, 4, L); c.rect(2, 10, 17, 11, D)
    c.rect(17, 4, 19, 10, (150, 30, 30)); c.px(18, 6, (255, 110, 100)); c.px(18, 7, (255, 110, 100))
    c.rect(4, 0, 14, 3, D); c.rect(4, 0, 14, 1, M); c.rect(10, 5, 13, 8, D)                        # the clamp and a switch
    save('laser', c, (10, 0)); return c
if __name__ == '__main__':
    cs = [pu2(), acog(), scope8(), reddot(), laser()]
    sheet = Image.new('RGB', (980, 150), (84, 92, 80))
    x = 6
    for c in cs:
        im = Image.fromarray(c.outline()); im = im.resize((im.width * 3, im.height * 3), Image.NEAREST); sheet.paste(im, (x, 10), im); x += im.width + 12
    sheet.save('ArtSource/weapons/attachments_preview.png')
