# The Great Bell (the Choir boss, 2026-11-20): one hand-built sprite, a big cracked brass bell hung in an iron yoke, rope hanging, verdigris and old blood. 64x80, feet at the bottom, faces the viewer.
#   python3 ArtSource/make_choir_bell.py -> Assets/Resources/Sprites/Mutants/mutant_hd_the_great_bell.png
import os, sys, math, random
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
BRASS = ramp("#150e04", "#301f06", "#563c0e", "#7e5c1c", "#a88434"); GREEN = ramp("#0e2a22", "#1c4a3c", "#2e7a62", "#52b090", "#9ae0c0")
IRON = ramp("#08090a", "#15181a", "#262b2e", "#3e464b", "#5c666d"); DARK = ramp("#050304", "#100808", "#1c0c0c", "#2e1212", "#461c1c")
S = (72, 88); rnd = random.Random(8); L = []
def part(build, cols, **kw): m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
part(lambda m: m.poly([(4, 10), (68, 10), (68, 16), (4, 16)]), IRON, tex=0.2, seed=1, depth=2.2)                        # the yoke beam
part(lambda m: m.poly([(4, 10), (10, 10), (10, 86), (4, 86)]).poly([(62, 10), (68, 10), (68, 86), (62, 86)]), IRON, tex=0.2, seed=2, depth=2.2)   # the posts
part(lambda m: m.poly([(34, 16), (38, 16), (38, 26), (34, 26)]), IRON, depth=1.4)
part(lambda m: m.ell(36, 24, 8, 5).poly([(28, 24), (44, 24), (47, 34), (49, 46), (53, 58), (60, 68), (64, 74), (8, 74), (12, 68), (19, 58), (23, 46), (25, 34)]).ell(36, 74, 28, 4.5), BRASS, tex=0.16, seed=3, depth=7.0)   # the bell: crown, flaring skirt, lip
part(lambda m: m.ell(36, 75, 22, 3.4), ramp("#050304", "#0a0505", "#140a0a", "#200f0f", "#2e1616"), depth=2.0)                # the mouth, black
hm = paint(S, L); base_alpha = np.array(hm)[:, :, 3] > 0
for (x0, y0, x1, y1) in ((32, 28, 30, 44), (30, 44, 35, 58), (35, 58, 31, 74), (42, 34, 44, 52), (44, 52, 41, 68)): line(hm, x0, y0, x1, y1, DARK[1]); line(hm, x0 + 1, y0, x1 + 1, y1, DARK[3])   # the cracks
for _ in range(120): px(hm, [(rnd.randint(8, 66), rnd.randint(32, 70))], GREEN[rnd.choice((1, 2, 2, 3))])                       # verdigris
for _ in range(90): px(hm, [(rnd.randint(8, 66), rnd.randint(40, 70))], DARK[rnd.choice((2, 3, 3, 4))])                         # old blood and soot
for x in range(20, 54, 5): line(hm, x, 52, x + 2, 52, BRASS[4])                                                                 # a worn band of lettering
cl = Mask(S); cl.tube([(36, 74), (36, 84)], [5, 5]); cl.ell(36, 86, 4.5, 4.5); hm.alpha_composite(shade(cl, IRON, depth=2.0))      # the clapper
for k in range(8): px(hm, [(36 + int(2 * math.sin(k)), 76 + k)], rgb("#a8884e"))                                              # the rope into the mouth
for _ in range(20): px(hm, [(rnd.randint(6, 66), rnd.randint(12, 30))], IRON[rnd.choice((1, 2, 3))])
a_ = np.array(hm); a_[~base_alpha] = 0; hm = Image.fromarray(a_, 'RGBA')                                  # decoration only where there is metal
out = outline(hm)
dst = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites', 'Mutants', 'mutant_hd_the_great_bell.png'); out.save(dst); print(out.size)
bg = Image.new('RGBA', out.size, (24, 22, 28, 255)); bg.alpha_composite(out); bg.resize((out.width * 5, out.height * 5), Image.NEAREST).save('/tmp/bell5.png')
