# The werewolf's claw slash, drawn (2026-11-21; Yan: "the double swipe barely looks like an attack"): three parallel claw crescents sweeping in front of the beast, 4 frames (they draw in, burn full, thin, fade), pointing right; CombatFxView ("beastslash")
# turns them toward the target and plays them over 0.22 s at every blow. Hand-built with pxart masks (hard pixels, no antialiasing): Assets/Resources/Sprites/Fx/fx_beast_slash.png (4 frames of 80x120).
import os, sys, math
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import Mask
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "Sprites", "Fx")
W, H = 80, 120
CORE = (250, 242, 222, 255); MID = (222, 200, 168, 255); EDGE = (176, 34, 42, 255); DIM = (120, 96, 90, 255)
def crescent(r_out, r_in, a0, a1, cx=8, cy=32, taper=True):
    """a crescent between two arcs of different radii/centres: thick in the middle, a point at each end; angles in degrees (0 = right, + = down)"""
    pts_o, pts_i = [], []
    n = 36
    for k in range(n + 1):
        a = math.radians(a0 + (a1 - a0) * k / n); t = k / n; wob = math.sin(math.pi * t)                 # 0 at the tips, 1 in the middle
        pts_o.append((cx + r_out * math.cos(a), cy + r_out * math.sin(a)))
        pts_i.append((cx + (r_out - (r_out - r_in) * wob) * math.cos(a), cy + (r_out - (r_out - r_in) * wob) * math.sin(a)))
    return pts_o + pts_i[::-1]
def frame(sweep, bright, fade):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for k, (ro, thick, dcy, span) in enumerate(((46, 13, -31, 24), (54, 17, 0, 28), (46, 13, 31, 24))):          # three claws, parallel: the middle one longest and thickest
        a0 = -span; a1 = a0 + 2 * span * sweep
        for col, shrink in ((EDGE, 0), (MID, 3.0), (CORE, 6.0)):
            if thick - shrink < 1: continue
            m = Mask((W, H)); m.poly(crescent(ro, ro - (thick - shrink), a0, a1, cx=6, cy=60 + dcy))
            bits = m.bits(); px = np.array(im)
            c = col if bright else DIM
            px[bits] = c if fade >= 1 else (c[0], c[1], c[2], int(255 * fade)); im = Image.fromarray(px, "RGBA")
    return im
def main():
    os.makedirs(OUT, exist_ok=True)
    frames = [frame(0.45, True, 1), frame(1.0, True, 1), frame(1.0, True, 1), frame(1.0, False, 0.55)]     # draw in, full, full (held a beat), faded
    strip = Image.new("RGBA", (W * 4, H), (0, 0, 0, 0))
    for i, f in enumerate(frames): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(OUT, "fx_beast_slash.png"))
    prev = Image.new("RGBA", (W * 4 * 4 + 40, H * 4 + 20), (52, 50, 44, 255)); big = strip.resize((strip.width * 4, strip.height * 4), Image.NEAREST); prev.alpha_composite(big, (20, 10))
    prev.save("/tmp/claude-0/-home-user-agora-vai-action/fb53e947-100c-5282-8ff4-5d048554abfc/scratchpad/beast_slash_fx.png"); print(strip.size)
if __name__ == "__main__": main()
