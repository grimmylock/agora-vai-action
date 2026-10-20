import math
from bench import G, show
PAL = {"0": "#22262a", "1": "#33373b", "2": "#4c5258", "3": "#727980", "4": "#a9b0b5",
       "a": "#4c2812", "b": "#743f1c", "c": "#a0602f", "d": "#b97a45",
       "r": "#7a1a17", "R": "#c0302a", "s": "#e06a50", "y": "#f0d878", "K": "#141617"}
def kris(g, x0, x1, yc, amp, waves, w0, shade_top="3"):
    """A wavy blade: its middle snakes up and down `waves` times, its width tapers from w0 to 1 at the tip."""
    for x in range(x0, x1 + 1):
        t = (x - x0) / (x1 - x0)
        mid = yc + amp * math.sin(t * waves * 2 * math.pi)
        w = max(1, round(w0 * (1 - t ** 2.6)))
        top = round(mid - w / 2)
        for k in range(w):
            rising = math.cos(t * waves * 2 * math.pi) > 0.3            # the slope faces up toward the light
            g.set(x, top + k, ("4" if rising else shade_top) if k == 0 else "1" if k == w - 1 else "3" if k == 1 and w > 3 else "2")
m = G(60, 13)
for x in range(0, 12): m.col(x, 5 if x else 6, "dccb" + "a" if x else "cba")
for x, y in [(3, 7), (4, 8), (8, 7), (9, 6), (6, 8)]: m.set(x, y, "a")
m.col(12, 4, "srRRRRr"); m.col(13, 4, "RRrrrrr"); m.set(12, 4, "y")            # red binding, brass tie
m.col(14, 1, "4"); m.col(15, 0, "33"); m.col(14, 2, "3322"); m.col(15, 2, "23322"); m.col(16, 3, "2222"); m.col(17, 4, "122")
m.set(15, 4, "R")                                                              # the red eye in the fin
m.col(15, 9, "21"); m.set(15, 11, "1")                                         # the small spike under the guard
kris(m, 16, 58, 6.5, 0.9, 3.5, 5)
h = G(23, 7)
for x in range(0, 6): h.col(x, 2, "db" if x else "c")
h.set(3, 3, "a"); h.col(6, 1, "RRrr"); h.set(6, 1, "y")
h.col(7, 0, "3322"); h.set(8, 1, "2"); h.set(7, 1, "R"); h.set(7, 4, "1")
for x in range(8, 22):                                                         # in the hand the waves are placed by hand
    r = 2 if ((x - 8) // 3) % 2 == 0 else 3
    h.col(x, r, ("4" if r == 2 else "3") + "1" if x <= 17 else "3")
show("ObeliskRitualBlade", [m, h], PAL)
