# Boss creature concepts (2026-11-20; Yan: "silent hill resident evil bloodborne style monsters"). Hand-built with pxart.py; NOT in the game; concept quality. python3 ArtSource/make_boss_concepts.py
import os, sys, math, random
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts'); os.makedirs(OUT, exist_ok=True)
BONE = ramp("#3a3328", "#6a604a", "#9c9378", "#c9c1a4", "#ebe4cb")
DARK = ramp("#080405", "#170a0b", "#28100f", "#3f1819", "#5a2324")
def mk(S):
    L = []
    def part(build, cols, **kw): m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
    return L, part

# ------------------------------------------------------------------ THE SINGER: a rock wall that has grown a face
def singer():
    ROCK = ramp("#0e0d10", "#1b1a1f", "#2c2b32", "#44434b", "#605e68")
    ROCK_D = ramp("#070709", "#0e0d10", "#17161b", "#232227", "#322f37")
    FLESH = ramp("#2b1d1d", "#4d3333", "#7a5a56", "#a8857d", "#d2b4aa")
    PALE = ramp("#3c3836", "#6b6460", "#9d958c", "#cdc5b9", "#f0eae0")
    LAMP = ramp("#5a2a08", "#a8550f", "#e8901e", "#ffc860", "#fff2c0")
    S = (200, 128); L, part = mk(S); rnd = random.Random(3)
    # the wall: a ragged slab of rock, flesh pushing through the cracks
    part(lambda m: m.poly([(4, 126), (6, 50), (16, 24), (40, 8), (80, 2), (120, 2), (160, 8), (184, 24), (196, 52), (196, 126)]), ROCK, tex=0.22, seed=1, depth=6.0)
    for (cx, cy, rx, ry, rot, sd) in ((34, 72, 20, 8, 70, 2), (166, 70, 18, 7, 110, 3), (22, 108, 12, 6, 20, 4), (176, 108, 13, 6, -20, 5), (60, 30, 12, 5, 30, 6), (140, 30, 12, 5, -30, 7)):
        part(lambda m, a=(cx, cy, rx, ry, rot): m.ell(*a), FLESH, tex=0.2, seed=sd, depth=2.4)
    # veins running through the stone to the face
    vn = paint(S, [])
    for (x0, y0) in ((20, 100), (36, 60), (176, 100), (164, 58), (60, 30), (140, 30), (14, 80), (186, 84)):
        x, y = x0, y0
        for i in range(26):
            tx, ty = 100 + (x0 - 100) * 0.1, 56; nx = x + (tx - x) * 0.07 + rnd.uniform(-1.6, 1.6); ny = y + (ty - y) * 0.07 + rnd.uniform(-1.6, 1.6)
            line(vn, x, y, nx, ny, DARK[3] if i % 3 else DARK[4]); x, y = nx, ny
    L.append(vn)
    # the face: a huge pale mass pushed out of the rock, lopsided, the stone biting its edges, no eyes, one weeping pit, a mouth too tall and wide on one side
    part(lambda m: m.ell(100, 64, 29, 47, 4).ell(104, 90, 22, 30, -6).ell(94, 40, 20, 22, 8), PALE, tex=0.1, seed=8, depth=7.0)
    part(lambda m: m.ell(87, 49, 8.5, 10, 14), DARK, depth=3.0)       # the pits, not alike
    part(lambda m: m.ell(113, 52, 5.5, 7, -14), DARK, depth=3.0)
    part(lambda m: m.ell(101, 78, 9, 32, 3), DARK, depth=3.4)                                                  # the mouth
    hm = paint(S, L)
    for k in range(20):                                                                                      # teeth down both lips, uneven, a few snapped
        y = 50 + k * 2.5; ln = 3 + (k * 7) % 5
        px(hm, [(92 + i, y) for i in range(ln)], rgb("#e1d9c0")); px(hm, [(110 - i, y + 1) for i in range(2 + (k * 5) % 5)], rgb("#c4bb9f"))
    for k in range(0, 20, 6): px(hm, [(100 + (k % 3), 54 + k * 2.5 + j) for j in range(3)], rgb("#5a1c1e"))
    # a miner's arm hanging out of the mouth, limp, the glove still on
    arm = Mask(S); arm.tube([(101, 70), (100, 94), (104, 112)], [6, 5, 4.4]); hm.alpha_composite(shade(arm, FLESH, depth=2.0, tex=0.1, seed=4))
    gl = Mask(S); gl.ell(104, 116, 5, 6); hm.alpha_composite(shade(gl, ramp("#2a2410", "#4a4018", "#7a6a28", "#a8963c", "#d2c068"), depth=1.8))
    line(hm, 87, 58, 86, 76, rgb("#1a0c0c")); line(hm, 88, 58, 88, 70, rgb("#2d1414"))                    # a tear running from the left pit
    for (x0, y0, x1, y1) in ((78, 30, 92, 40), (118, 34, 108, 44), (84, 100, 96, 90), (120, 92, 110, 82), (100, 22, 100, 34)): line(hm, x0, y0, x1, y1, PALE[1])    # cracks across the skin
    # chips of rock overlapping the face edge, so it is coming out of the wall
    for (cx, cy, rx, ry, rot) in ((70, 84, 8, 4, 40), (130, 96, 9, 4, -30), (76, 36, 7, 3, -20), (126, 30, 8, 3, 25), (96, 112, 10, 3, 5)):
        ch = Mask(S); ch.ell(cx, cy, rx, ry, rot); hm.alpha_composite(shade(ch, ROCK, depth=2.0, tex=0.2, seed=cx))
    # lamps set into the flesh: miners' helmets, two still lit
    for (hx, hy, lit) in ((46, 84, 1), (150, 90, 1), (30, 56, 0), (172, 62, 0), (70, 108, 0), (132, 110, 0)):
        h = Mask(S); h.ell(hx, hy, 6, 4.5); hm.alpha_composite(shade(h, ramp("#14161a", "#262a30", "#3a4048", "#566068", "#7a8690"), depth=1.8))
        l = Mask(S); l.ell(hx, hy - 1, 2.6, 2.2); hm.alpha_composite(shade(l, LAMP if lit else ramp("#101010", "#1c1c1c", "#2a2a2a", "#3a3a3a", "#4a4a4a"), depth=1.2))
        if lit: px(hm, [(hx + dx, hy - 1 + dy) for dx in range(-8, 9, 3) for dy in (-1, 0, 1) if abs(dx) > 3], LAMP[1])
    # hands pushing out of the rock: fingers curled, keeping time
    for (hx, hy, flip) in ((26, 94, 1), (60, 116, -1), (142, 118, 1), (176, 100, -1), (18, 70, -1), (186, 76, 1)):
        arm = Mask(S); arm.tube([(hx, hy + 16), (hx, hy + 6), (hx + 2 * flip, hy)], [7, 5.4, 4]); hm.alpha_composite(shade(arm, FLESH, depth=2.0, tex=0.1, seed=hx))
        fg = Mask(S)
        for k in range(5): a = math.radians(-100 + k * 24 + 10 * flip); fg.tube([(hx + 2 * flip, hy), (hx + 2 * flip + 8 * math.cos(a), hy + 8 * math.sin(a))], [1.8, 0.8])
        hm.alpha_composite(shade(fg, FLESH, depth=1.0))
    a_ = np.array(hm).astype(float); H_, W_ = a_.shape[:2]; yy, xx = np.mgrid[0:H_, 0:W_]                       # a dark vignette: the corners of the wall fall into black, the lit lamps and the face keep their light
    v = np.clip(1.0 - 0.55 * (((xx - W_ / 2) / (W_ / 2)) ** 2 + ((yy - H_ * 0.55) / (H_ * 0.6)) ** 2) ** 1.1, 0.35, 1.0)
    lamp = np.zeros((H_, W_), bool)
    a_[:, :, :3] *= v[:, :, None]; hm = Image.fromarray(np.clip(a_, 0, 255).astype(np.uint8), "RGBA")
    return outline(crop(hm, 2))

# ------------------------------------------------------------------ THE ORDERLY: a hunched giant in a stained apron, a gauze mask sewn into a grin
def orderly():
    SKIN = ramp("#16110f", "#2c2222", "#4a3a3a", "#6e5856", "#957c78")
    APRON = ramp("#26231f", "#4a453d", "#767061", "#a49d89", "#d2ccb8")
    STEEL = ramp("#0b0d0f", "#191e21", "#2e353a", "#4c565d", "#78838a")
    RUBBER = ramp("#040404", "#0e0f0e", "#1a1b19", "#292b28", "#3a3d38")
    GAUZE = ramp("#3d3a33", "#6c685b", "#9c9583", "#c8c2ae", "#ece7d6")
    S = (150, 176); L, part = mk(S); rnd = random.Random(7)
    # short thick legs, wide boots: it carries its weight low
    part(lambda m: m.tube([(50, 104), (44, 132), (42, 152)], [17, 14, 12]), SKIN, tex=0.1, seed=1)
    part(lambda m: m.tube([(84, 104), (92, 130), (98, 152)], [17, 14, 12]), SKIN, tex=0.1, seed=2)
    part(lambda m: m.poly([(30, 152), (54, 152), (58, 170), (24, 170)]), RUBBER, depth=2.4); part(lambda m: m.poly([(88, 152), (108, 152), (114, 170), (80, 170)]), RUBBER, depth=2.4)
    # the torso leans forward from the hips: a hump of back, the apron slung over it
    part(lambda m: m.ell(60, 62, 30, 36, 14).ell(46, 52, 24, 28, 24).poly([(34, 70), (86, 78), (96, 108), (28, 106)]), SKIN, tex=0.12, seed=3, depth=5.0)
    part(lambda m: m.poly([(44, 54), (88, 62), (98, 110), (104, 142), (92, 138), (86, 148), (74, 138), (62, 150), (50, 138), (36, 146), (34, 110)]), APRON, tex=0.14, seed=4, depth=4.0)
    st = paint(S, [])
    for (x0, y0, x1, y1) in ((56, 70, 52, 104), (68, 74, 72, 112), (80, 80, 84, 118), (48, 108, 46, 140), (62, 112, 64, 146), (76, 116, 78, 140), (90, 112, 92, 136)):   # fold lines, dark in the valley, light on the ridge
        line(st, x0, y0, x1, y1, APRON[1]); line(st, x0 + 2, y0, x1 + 2, y1, APRON[4])
    for yy in range(96, 148):                                                                                 # the blood climbs the hem: denser the lower it is
        for _ in range(int((yy - 90) / 5)): px(st, [(rnd.randint(36, 100), yy)], DARK[rnd.choice((2, 3, 3, 4))])
    for (x0, y0, ln) in ((54, 74, 26), (70, 84, 34), (84, 90, 22)):
        for i in range(ln): px(st, [(x0, y0 + i)], DARK[3] if i % 3 else DARK[4])
    L.append(st)
    # the IV stand: grown out of the shoulder, bent, the bag swinging, a tube into the neck
    part(lambda m: m.tube([(34, 52), (22, 20), (28, 4)], [3.6, 2.8, 2.4]), STEEL, depth=1.4)
    part(lambda m: m.ell(30, 16, 6.5, 10, 8), ramp("#162416", "#264226", "#427242", "#72a870", "#b0e0ac"), depth=2.8)
    tb = Mask(S); tb.tube([(30, 26), (40, 40), (54, 46)], [1.5, 1.4, 1.2]); L.append(shade(tb, ramp("#2a0e0e", "#4a1818", "#7a2828", "#a63c3c", "#d06c6c"), depth=1.0))
    # the arms: the near arm hangs to the floor, the bed rail dragging from its fist at an angle; the far arm bent up with hooks
    part(lambda m: m.tube([(92, 58), (112, 84), (116, 118), (118, 150)], [17, 13, 10.5, 9]), SKIN, tex=0.12, seed=5, bias=-0.1)
    rail = Mask(S); rail.poly([(116, 148), (124, 148), (146, 174), (136, 176)]).poly([(112, 142), (128, 142), (130, 154), (112, 154)]); L.append(shade(rail, STEEL, depth=1.8))
    rail2 = Mask(S); rail2.poly([(126, 150), (132, 150), (150, 172), (144, 174)]); L.append(shade(rail2, STEEL, depth=1.2))
    teeth = paint(S, [])
    for i in range(9): px(teeth, [(128 + i * 2.2, 152 + i * 2.4), (126 + i * 2.2, 152 + i * 2.4)], STEEL[4])
    L.append(teeth)
    glove = Mask(S); glove.ell(118, 150, 6, 7); L.append(shade(glove, ramp("#2a2420", "#4a4036", "#7a6c5a", "#a8977c", "#d0bf9f"), depth=1.8))   # a yellowed rubber glove
    part(lambda m: m.tube([(34, 62), (12, 84), (14, 112), (24, 124)], [13, 10, 8, 6]), SKIN, tex=0.1, seed=6)
    hooks = Mask(S)
    for dx in (-5, 0, 5): hooks.tube([(24, 124), (22 + dx, 134), (28 + dx, 142)], [2.4, 1.6, 0.8])
    L.append(shade(hooks, STEEL, depth=1.0))
    # the head: low, forward, bigger than it should be; gauze wound over a face that is not there; a grin stitched off-centre; one dark eye hole, off-centre
    part(lambda m: m.ell(70, 42, 19, 22, -10).ell(78, 50, 14, 16, 0), GAUZE, tex=0.14, seed=8, depth=5.0, bias=-0.05)
    hm = paint(S, L)
    for (x0, y0, x1, y1) in ((52, 24, 90, 34), (50, 38, 92, 28), (56, 46, 94, 52), (54, 30, 88, 56), (60, 56, 92, 44), (52, 22, 58, 60)): line(hm, x0, y0, x1, y1, GAUZE[1]); line(hm, x0, y0 + 1, x1, y1 + 1, GAUZE[4])
    px(hm, [(x, y) for x in range(60, 67) for y in range(32, 39)], rgb("#050303")); px(hm, [(63, 34), (64, 35)], rgb("#d03a3a"))    # the one eye: a dull red point in the hole
    for i in range(16): px(hm, [(60 + i * 1.8, 52 + 2.6 * math.sin(i * 0.8) + i * 0.35)], rgb("#160d0b"))                    # the stitched grin, slanting
    for i in range(15): line(hm, 61 + i * 1.8, 49 + i * 0.3, 62 + i * 1.8, 57 + i * 0.35, rgb("#26150f"))
    for _ in range(60): px(hm, [(rnd.randint(54, 92), rnd.randint(26, 62))], DARK[rnd.choice((2, 3, 4))])
    for k in range(6): px(hm, [(66 + k, 60 + k * 2)], DARK[4])                                                  # something dark seeping out under the chin
    tag = Mask(S); tag.poly([(78, 74), (92, 76), (92, 88), (78, 86)]); hm.alpha_composite(shade(tag, ramp("#26261f", "#46463b", "#72726a", "#a4a494", "#d2d2c2"), depth=1.2))
    for k in range(3): px(hm, [(80 + k * 3, 80), (80 + k * 3, 82)], rgb("#2a2a24"))
    return outline(crop(hm, 2))

if __name__ == '__main__':
    items = [('singer', 'THE SINGER (T2)', singer()), ('orderly', 'THE ORDERLY (T6)', orderly())]
    K = 4; pad = 40
    W = sum(im.width * K for _, _, im in items) + pad * 3; H = max(im.height for _, _, im in items) * K + pad * 2
    sh = Image.new('RGBA', (W, H), (22, 20, 24, 255)); x = pad; d = ImageDraw.Draw(sh)
    for k, label, im in items:
        im.save(os.path.join(OUT, k + '_concept.png')); sh.alpha_composite(im.resize((im.width * K, im.height * K), Image.NEAREST), (x, H - im.height * K - pad)); d.text((x, 10), label, fill=(220, 215, 200, 255)); x += im.width * K + pad
    sh.convert('RGB').save(os.path.join(OUT, 'boss_concepts_sheet.png')); print([(k, i.size) for k, _, i in items])
