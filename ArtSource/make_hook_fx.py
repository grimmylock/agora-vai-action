# The Verger's Hook, drawn (2026-11-20; Yan: "improve the chain, doesn't look like a hook right now"): a real iron gaff hook and interlocking chain links, hand-built at native pixel size with pxart (not resampled).
#   Assets/Resources/Sprites/Fx/fx_hook_head.png   one frame, 16x12: an eye ring (the chain's end), a shank, a deep J-bend and a barbed point curling back; it points RIGHT, the curl below
#   Assets/Resources/Sprites/Fx/fx_hook_links.png  two frames of 8x8: a link seen face-on (an oval ring with a hole) and the next one edge-on (a thick bar), they alternate along the chain
# CombatFxView.cs ("hookchain") rotates them along the line and flips the curl so it always hangs below. python3 ArtSource/make_hook_fx.py
import os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "Sprites", "Fx")
IRON = ramp("#101214", "#22272c", "#3b444c", "#66727c", "#aab6c0")
INK = (12, 14, 16, 255)
def hook():
    S = (16, 12); L = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
    ring = Mask(S); ring.ell(2.6, 3.4, 2.5, 2.5); hole = Mask(S); hole.ell(2.6, 3.4, 1.0, 1.0); ring.cut(hole); L.append(shade(ring, IRON, depth=1.2))                        # the eye the chain is shackled to
    part(lambda m: m.tube([(4.6, 3.4), (9.0, 3.4)], [2.3, 2.3]), IRON, depth=1.3)                                                                                     # the shank
    part(lambda m: m.tube([(9.0, 3.4), (12.2, 4.4), (13.4, 7.2), (11.4, 9.6), (8.6, 9.4)], [2.3, 2.2, 2.0, 1.6, 0.9]), IRON, depth=1.3)                                  # the bend, curling back on itself
    part(lambda m: m.poly([(8.6, 9.5), (6.8, 8.0), (8.9, 8.4)]), IRON, depth=0.8)                                                                                      # the barb: a sharp tooth on the point
    im = paint(S, L)
    px(im, [(7, 2), (8, 2), (10, 3), (11, 4)], IRON[4]); px(im, [(4, 2)], IRON[4])                                                                                       # a glint along the top of the shank and the bend
    return im
def links():
    sheet = Image.new("RGBA", (16, 8), (0, 0, 0, 0))
    a = Mask((8, 8)); a.ell(4, 4, 3.4, 2.3); h = Mask((8, 8)); h.ell(4, 4, 1.4, 0.9); a.cut(h)                                                                           # face-on: an oval ring, the hole showing the dark behind
    fa = shade(a, IRON, depth=1.0)
    b = Mask((8, 8)); b.tube([(4, 2.2), (4, 5.8)], [2.2, 2.2]); fb = shade(b, IRON, depth=1.0)                                                                        # edge-on: a thick short bar
    sheet.alpha_composite(outline(fa, INK), (0, 0)); sheet.alpha_composite(outline(fb, INK), (8, 0))
    return sheet
def main():
    os.makedirs(OUT, exist_ok=True)
    h = outline(hook(), INK); h.save(os.path.join(OUT, "fx_hook_head.png")); links().save(os.path.join(OUT, "fx_hook_links.png"))
    big = Image.new("RGBA", (h.width * 12 + 200, 140), (52, 50, 44, 255)); big.alpha_composite(h.resize((h.width * 12, h.height * 12), Image.NEAREST), (10, 10))
    L = Image.open(os.path.join(OUT, "fx_hook_links.png")); big.alpha_composite(L.resize((L.width * 8, L.height * 8), Image.NEAREST), (h.width * 12 + 30, 20))
    big.save("/tmp/claude-0/-home-user-agora-vai-action/fb53e947-100c-5282-8ff4-5d048554abfc/scratchpad/hook_sprites.png"); print(h.size, L.size)
if __name__ == "__main__": main()
