# Lab furniture (Yan, 2026-11-19: "the labs need more lab stuff, microscopes, computers, etc."): a microscope on a bench, a computer terminal on a desk, a centrifuge, a cold-storage cabinet.
# Same interface as props.py (finish(im, pivot, shadow, tall)); hooked in at the end of props.py. Used by Furnish.cs (lab zones swap some benches, desks, machines and lockers for these).
import math
from PIL import ImageDraw
from props import finish, SCREEN, TRACE, LED, GLASS, GLASS_HI
from worldlib import *

BRASS = hx("#b59a4c"); ENAMEL = hx("#26292b"); ENAMEL_HI = hx("#4a5055"); CYAN = hx("#7fe0e8")

def microscope(seed):
    """A lab bench with a microscope on it: black enamel body, a brass tube and stage, slides in a tray, a small lamp."""
    s = P["steel"]; im = blank(20, 17); d = ImageDraw.Draw(im)
    d.rectangle([0, 9, 19, 12], fill=s[3]); d.line([(0, 9), (19, 9)], fill=s[4]); d.rectangle([0, 13, 19, 14], fill=s[1])      # the bench top and its edge
    for x in (1, 18): d.line([(x, 14), (x, 16)], fill=s[0])
    d.rectangle([7, 7, 13, 8], fill=ENAMEL); d.line([(7, 7), (13, 7)], fill=ENAMEL_HI)                                       # the base
    d.line([(12, 7), (12, 3)], fill=ENAMEL, width=2); d.line([(11, 2), (9, 2)], fill=ENAMEL)                                  # the arm
    d.line([(9, 2), (7, 5)], fill=ENAMEL, width=2); px(im, 9, 1, BRASS); px(im, 10, 1, BRASS); px(im, 8, 0, GLASS_HI)          # the tube leaning down to the stage, the eyepiece
    d.rectangle([6, 6, 10, 6], fill=BRASS); px(im, 7, 5, BRASS)                                                                # the stage and the objective
    d.rectangle([14, 8, 18, 8], fill=s[2])                                                                                     # a slide tray
    for i, x in enumerate((15, 16, 17)): px(im, x, 7, (hx("#c06bff"), hx("#7fa45e"), hx("#c8443a"))[(i + seed) % 3])
    px(im, 2, 8, hx("#f0e8b8")); px(im, 3, 8, hx("#f0e8b8")); d.line([(2, 7), (3, 7)], fill=s[0])                               # a lamp
    return finish(im, (10, 16), shadow=(20, 4, 2, -1))

def terminal(seed):
    """A computer terminal on a desk: a boxy CRT with green text, a keyboard, a beige tower with a blinking light, papers."""
    s = P["steel"]; im = blank(20, 18); d = ImageDraw.Draw(im); r = rng(seed)
    d.rectangle([0, 10, 19, 13], fill=s[2]); d.line([(0, 10), (19, 10)], fill=s[3]); d.rectangle([0, 14, 19, 17], fill=s[1]); d.line([(0, 17), (19, 17)], fill=s[0])   # the desk
    d.rectangle([3, 1, 12, 9], fill=hx("#b8b09a")); d.rectangle([4, 2, 11, 7], fill=SCREEN); d.line([(3, 1), (12, 1)], fill=hx("#d6cfb8"))                      # the monitor
    for y in (3, 4, 5, 6):
        for x in range(5, 5 + r.randint(2, 6)): px(im, x, y, TRACE if (x + y) % 3 else hx("#4fa05a"))
    px(im, 7, 9, hx("#8f8870")); px(im, 8, 9, hx("#8f8870"))
    d.rectangle([4, 11, 11, 12], fill=hx("#8f8870")); [px(im, x, 11, hx("#c8c0a8")) for x in range(5, 11, 2)]                                                   # the keyboard
    d.rectangle([14, 3, 18, 9], fill=hx("#a8a088")); d.line([(14, 3), (18, 3)], fill=hx("#d6cfb8")); d.line([(15, 5), (17, 5)], fill=ENAMEL)                  # the tower
    px(im, 17, 7, LED[(seed % 3)]); px(im, 15, 7, LED[0])
    px(im, 13, 11, hx("#e8e2cc")); px(im, 14, 11, hx("#e8e2cc")); px(im, 14, 12, hx("#cfc8b0"))                                                                  # papers
    return finish(im, (10, 17), shadow=(20, 4, 2, -1))

def centrifuge(seed):
    """A bench centrifuge: a squat steel drum with a clear lid, a small display and a dial."""
    s = P["steel"]; im = blank(16, 16); d = ImageDraw.Draw(im)
    d.rectangle([1, 8, 14, 14], fill=s[2]); d.line([(1, 8), (14, 8)], fill=s[3]); d.line([(1, 14), (14, 14)], fill=s[0]); d.line([(1, 8), (1, 14)], fill=s[3]); d.line([(14, 8), (14, 14)], fill=s[1])
    d.ellipse([2, 1, 13, 9], fill=s[3], outline=s[1]); d.ellipse([4, 2, 11, 7], fill=GLASS); d.ellipse([5, 3, 8, 5], fill=GLASS_HI)
    for a in range(0, 360, 60):
        x = 7.5 + math.cos(math.radians(a + seed * 17)) * 2.2; y = 4.5 + math.sin(math.radians(a + seed * 17)) * 1.2
        px(im, int(x), int(y), (hx("#c8443a"), hx("#7fa45e"))[a // 60 % 2])
    d.rectangle([3, 11, 7, 12], fill=SCREEN); px(im, 4, 11, TRACE); px(im, 6, 11, TRACE); px(im, 10, 11, LED[0]); px(im, 12, 11, LED[1])
    return finish(im, (8, 14), shadow=(16, 4, 1, 0))

def fridge_lab(seed):
    """A laboratory cold-storage cabinet: a tall steel box with a glass door showing racks of vials, a handle, a cyan temperature readout."""
    s = P["steel"]; im = blank(16, 28); d = ImageDraw.Draw(im)
    d.rectangle([0, 2, 15, 26], fill=s[2]); d.line([(0, 2), (15, 2)], fill=s[4]); d.line([(0, 2), (0, 26)], fill=s[3]); d.line([(15, 2), (15, 26)], fill=s[1]); d.line([(0, 26), (15, 26)], fill=s[0])
    d.rectangle([2, 5, 13, 19], fill=hx("#1d2a2c")); d.rectangle([2, 5, 13, 5], fill=hx("#3e5256"))
    for ry, col in ((8, hx("#7fa45e")), (12, hx("#c06bff")), (16, hx("#c8443a"))):
        d.line([(3, ry + 2), (12, ry + 2)], fill=s[1])
        for x in range(3, 13, 2): px(im, x, ry, col if (x + seed) % 4 else GLASS_HI); px(im, x, ry + 1, col)
    d.rectangle([2, 21, 8, 23], fill=SCREEN); px(im, 3, 22, CYAN); px(im, 4, 22, CYAN); px(im, 6, 22, CYAN); px(im, 11, 22, LED[0]); d.line([(13, 8), (13, 14)], fill=s[4])
    d.line([(0, 0), (15, 0)], fill=s[1]); d.rectangle([1, 0, 14, 1], fill=s[3])
    return finish(im, (8, 26), shadow=(16, 4, 1, 0), tall=True)

NEW = {"microscope": microscope, "terminal": terminal, "centrifuge": centrifuge, "fridge_lab": fridge_lab}
VARIANTS = {"microscope": 2, "terminal": 3, "centrifuge": 2, "fridge_lab": 2}
