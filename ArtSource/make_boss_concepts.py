# Boss creature concepts (2026-11-20; Yan: "silent hill resident evil bloodborne style monsters"). Hand-built with pxart.py; NOT in the game; concept quality. python3 ArtSource/make_boss_concepts.py
import os, sys, math, random
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'boss_concepts'); os.makedirs(OUT, exist_ok=True)
BONE = ramp("#3a3328", "#6a604a", "#9c9378", "#c9c1a4", "#ebe4cb")
DARK = ramp("#080405", "#170a0b", "#28100f", "#3f1819", "#5a2324")

HUSKS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites', 'husk_atlas_x2.png')
def husk_cell(i):
    """The real Husk sprite (front view, 32 px cell, index i in husk_atlas_x2.png), pasted at 1:1 - never resampled, so the concept carries the true Husk look for the sprite makers."""
    im = Image.open(HUSKS).convert('RGBA'); cols = im.width // 32
    return im.crop(((i % cols) * 32, (i // cols) * 32, (i % cols) * 32 + 32, (i // cols) * 32 + 32))

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


# ------------------------------------------------------------------ MOTHER HOLLOW: a bloated pale pack-mother, her brood growing out of her flank
def mother_hollow():
    # Built from the Pallid Prowler's own look (Resources/Sprites/Mutants/mutant_hd_pallid_prowler.png): a lean bone-grey hound with a ridged shoulder hump, thin long legs and an acid-green glowing maw. She is that, grown huge and swollen with her brood; the pups are small Pallid Prowlers pushing out of sacs, mouths already green.
    SKIN = ramp("#1a1916", "#363329", "#625d50", "#958e7c", "#c4bca8")
    SKIN_F = ramp("#0d0c0b", "#181714", "#27251f", "#3d3a31", "#57534a")
    SAC = ramp("#1c241a", "#36482e", "#5e7a50", "#8fb07c", "#c4e0b0")
    PUP = ramp("#2a2822", "#4c493e", "#797464", "#a9a390", "#d4cdb8")
    GREEN = ramp("#0c2606", "#1f5a0c", "#46a018", "#82e034", "#d2ff90")
    S = (212, 128); L, part = mk(S); rnd = random.Random(11)
    # far legs: thin and long like the Prowler's, folded too far
    part(lambda m: m.tube([(132, 66), (146, 90), (138, 106), (150, 116)], [8, 5.4, 4, 3]), SKIN_F, tex=0.1, seed=1)
    part(lambda m: m.tube([(54, 70), (38, 92), (54, 106), (44, 116)], [10, 6.4, 4.6, 3.2]), SKIN_F, tex=0.1, seed=2)
    # the body: the Prowler's hunched hump of shoulder, a narrow chest, the belly swollen and hanging
    part(lambda m: m.ell(84, 46, 40, 15, -6).ell(126, 36, 26, 20, 16).ell(46, 52, 15, 14, 0).ell(96, 74, 36, 20, 2), SKIN, tex=0.16, seed=3, depth=5.5)
    sp = Mask(S)
    for i in range(12): x = 142 - i * 9; sp.ell(x, 20 + abs(math.sin(i * 0.55)) * 2.5 + (i * 0.9), 3.0 - i * 0.05, 2.7)
    L.append(shade(sp, BONE, depth=1.7))
    rb = Mask(S)
    for k in range(7): x0 = 100 + k * 4.4; rb.tube([(x0, 36), (x0 + 3, 48 + k * 0.6), (x0 + 1, 60)], [2.2, 2.0, 1.2])
    L.append(shade(rb, BONE, depth=1.3))
    cav = Mask(S); cav.ell(114, 47, 18, 12, 4); L.insert(len(L) - 1, shade(cav, DARK, depth=2.2, tex=0.08, seed=5))
    # the brood: pale sacs with green veins along the belly and flank
    for (cx, cy, r) in ((56, 60, 12), (80, 80, 13), (106, 84, 12), (130, 74, 10)):
        part(lambda m, a=(cx, cy, r): m.ell(a[0], a[1], a[2], a[2] * 0.85), SAC, tex=0.14, seed=cx, depth=3.2)
    part(lambda m: m.tube([(124, 66), (140, 90), (132, 108), (146, 120)], [13, 8.4, 6, 4.4]), SKIN, tex=0.12, seed=6, bias=-0.1)
    part(lambda m: m.tube([(48, 64), (28, 86), (50, 106), (34, 120)], [16, 10, 6.4, 4.4]), SKIN, tex=0.12, seed=7, bias=-0.1)
    pw = Mask(S)
    for dx in (-4, 0, 4): pw.tube([(146, 120), (148 + dx, 126)], [2.0, 0.8]); pw.tube([(34, 120), (31 + dx, 126)], [2.0, 0.8])
    L.append(shade(pw, BONE, depth=1.0))
    # the neck dropped forward and the Prowler's long wedge of a head, huge, the jaw hanging open on a green glow
    part(lambda m: m.tube([(140, 34), (160, 44), (174, 54)], [18, 13, 10]), SKIN, tex=0.12, seed=8, depth=3.4)
    HL, hp = mk(S)
    hp(lambda m: m.ell(176, 52, 10, 8.5, 8).poly([(172, 47), (196, 54), (206, 61), (204, 65), (182, 67)]), SKIN, tex=0.1, seed=9, depth=3.2)
    hp(lambda m: m.poly([(180, 66), (204, 66), (200, 88), (190, 92), (178, 78)]), ramp("#14130f", "#2a2820", "#48443a", "#6e695a", "#9a937e"), depth=1.8, tex=0.1, seed=10)
    hp(lambda m: m.poly([(182, 66), (202, 67), (198, 84), (190, 88), (181, 76)]), GREEN, depth=3.0)
    hm_ = paint(S, HL)
    for x in range(182, 204, 2): px(hm_, [(x, 66), (x, 67), (x, 70 if x % 4 == 0 else 68)], rgb("#e4e0cc"))
    for x in range(184, 198, 3): px(hm_, [(x, 86), (x, 85), (x, 83 if x % 6 == 0 else 84)], rgb("#c0bba4"))
    px(hm_, [(x, y) for x in range(173, 178) for y in range(47, 51)], rgb("#050503"))                          # a socket, no eye: it has none
    px(hm_, [(x, y) for x in range(186, 190) for y in range(52, 55)], rgb("#050503"))
    L.append(hm_)
    dr = paint(S, [])
    for x in (184, 190, 196): line(dr, x, 88, x - 1, 108 + (x % 5), GREEN[2]); px(dr, [(x - 1, 109 + (x % 5))], GREEN[4])           # green drool
    L.append(dr)
    hm = paint(S, L)
    # the pups: small Pallid Prowlers, pale, green-mouthed, pushing out of the sacs, a front paw already out
    for (cx, cy, r, ang) in ((56, 60, 12, 200), (80, 80, 13, 250), (106, 84, 12, 280), (130, 74, 10, 320)):
        a = math.radians(ang); hx, hy = cx + math.cos(a) * r * 0.55, cy + math.sin(a) * r * 0.5
        hd = Mask(S); hd.ell(hx, hy, 8, 6, ang - 180); hd.poly([(hx + math.cos(a) * 5, hy + math.sin(a) * 4 - 2), (hx + math.cos(a) * 11, hy + math.sin(a) * 9), (hx + math.cos(a) * 5, hy + math.sin(a) * 4 + 3)])
        hm.alpha_composite(shade(hd, PUP, depth=2.0, tex=0.08, seed=cx))
        mo = Mask(S); mo.ell(hx + math.cos(a) * 7, hy + math.sin(a) * 6, 2.8, 2.0); hm.alpha_composite(shade(mo, GREEN, depth=1.2))
        pawm = Mask(S); pawm.tube([(hx, hy + 3), (hx + math.cos(a + 1.2) * 8, hy + math.sin(a + 1.2) * 8)], [2.4, 1.6]); hm.alpha_composite(shade(pawm, PUP, depth=1.0))
    for (cx, cy) in ((56, 60), (80, 80), (106, 84), (130, 74)):
        x, y = cx, cy
        for i in range(16): nx = x + rnd.uniform(-1.6, 1.6) - 0.7; ny = y - 1.7 + rnd.uniform(-0.6, 0.6); line(hm, x, y, nx, ny, GREEN[1] if i % 2 else GREEN[2]); x, y = nx, ny
    a_ = np.array(hm).astype(float); H_, W_ = a_.shape[:2]; yy, xx = np.mgrid[0:H_, 0:W_]
    v = np.clip(1.0 - 0.5 * (((xx - W_ / 2) / (W_ / 2)) ** 2 + ((yy - H_ * 0.5) / (H_ * 0.62)) ** 2) ** 1.1, 0.45, 1.0)
    a_[:, :, :3] *= v[:, :, None]; hm = Image.fromarray(np.clip(a_, 0, 255).astype(np.uint8), "RGBA")
    return outline(crop(hm, 2))

# ------------------------------------------------------------------ THE FERRYMAN: a drowned man in an oilskin coat, a ship's lantern for a head
def ferryman():
    COAT = ramp("#0c1410", "#18261f", "#2a4034", "#46644f", "#6c8c6c")
    COAT_F = ramp("#070b09", "#0e1612", "#18261f", "#243a2e", "#365040")
    IRON = ramp("#0a0b0c", "#181b1e", "#2c3236", "#48515a", "#6e7a84")
    GLASS = ramp("#0e2a1e", "#1a5a3c", "#34a068", "#7adca0", "#d2fae6")
    ROPE = ramp("#241c10", "#4a3a20", "#7a6238", "#a8884e", "#d0b06a")
    FLESH = ramp("#1a2220", "#2e3c3a", "#4c605c", "#74908a", "#a4c0b8")
    S = (130, 190); L, part = mk(S); rnd = random.Random(5)
    # wading boots with the water standing round them
    part(lambda m: m.tube([(50, 124), (46, 156), (44, 172)], [20, 16, 14]), COAT_F, tex=0.1, seed=1)
    part(lambda m: m.tube([(80, 124), (86, 154), (90, 172)], [20, 16, 14]), COAT, tex=0.1, seed=2)
    part(lambda m: m.poly([(34, 172), (56, 172), (60, 184), (28, 184)]), ramp("#050606", "#0e1010", "#1a1d1c", "#2a2e2c", "#3c413e"), depth=2.0); part(lambda m: m.poly([(82, 172), (100, 172), (108, 184), (76, 184)]), ramp("#050606", "#0e1010", "#1a1d1c", "#2a2e2c", "#3c413e"), depth=2.0)
    # the oilskin coat: long, slick, split at the hem, stooped
    part(lambda m: m.poly([(42, 50), (90, 50), (104, 120), (112, 168), (94, 160), (82, 172), (66, 158), (50, 172), (38, 160), (24, 166), (30, 120)]), COAT, tex=0.12, seed=3, depth=5.0)
    part(lambda m: m.ell(66, 54, 30, 10, 0), COAT, tex=0.1, seed=4, depth=3.0)
    cl = paint(S, [])
    for (x0, y0, x1, y1) in ((52, 62, 46, 150), (66, 62, 66, 156), (80, 62, 86, 152), (58, 100, 54, 160), (74, 100, 78, 160)): line(cl, x0, y0, x1, y1, COAT[0]); line(cl, x0 + 2, y0, x1 + 2, y1, COAT[4])
    L.append(cl)
    # rope bindings across the chest and a lifebelt hanging at the hip, red and white
    rp = Mask(S); rp.tube([(36, 58), (66, 84), (96, 108)], [3.4, 3.4, 3.4]); rp.tube([(96, 58), (66, 84), (36, 108)], [3.4, 3.4, 3.4]); rp.tube([(40, 118), (66, 124), (92, 118)], [3.0, 3.0, 3.0]); L.append(shade(rp, ROPE, depth=1.4, tex=0.2, seed=6))
    bw = Mask(S); bw.ell(94, 126, 13, 13); inner = Mask(S); inner.ell(94, 126, 6, 6); bw.cut(inner); L.append(shade(bw, ramp("#5a1010", "#9a1c1c", "#c8352e", "#e8665a", "#f8a8a0"), depth=2.2))
    for ang in (30, 120, 210, 300):
        a = math.radians(ang); w = Mask(S); w.ell(94 + math.cos(a) * 9.5, 126 + math.sin(a) * 9.5, 3.6, 3.6); L.append(shade(w, ramp("#6a6860", "#9a978c", "#c4c1b4", "#e6e3d6", "#faf8ee"), depth=1.2))
    # arms: long, ending in barnacled gloves; one holds a boat hook, a dripping oar with iron hooks
    part(lambda m: m.tube([(40, 56), (28, 88), (30, 120), (34, 142)], [14, 11, 9, 8]), COAT_F, tex=0.1, seed=7)
    part(lambda m: m.tube([(92, 56), (112, 88), (110, 126), (106, 150)], [15, 12, 10, 8.4]), COAT, tex=0.1, seed=8, bias=-0.1)
    gl = Mask(S); gl.ell(34, 146, 6, 7); gl.ell(106, 154, 6.4, 7); L.append(shade(gl, FLESH, depth=1.8, tex=0.2, seed=9))
    shaft = Mask(S); shaft.tube([(106, 156), (112, 90), (116, 30), (118, 6)], [3.2, 3.0, 3.0, 2.6]); L.append(shade(shaft, ROPE, depth=1.2, tex=0.2, seed=2))
    hk = Mask(S); hk.tube([(118, 8), (126, 4), (128, 14), (122, 20)], [3, 2.6, 2.2, 1.4]); hk.tube([(116, 30), (124, 34), (123, 44)], [2.6, 2, 1.2]); L.append(shade(hk, IRON, depth=1.2))
    # barnacles and weed on the shoulders and sleeves
    bn = paint(S, [])
    for _ in range(46):
        x = rnd.choice((rnd.randint(30, 52), rnd.randint(86, 108))); y = rnd.randint(50, 100); px(bn, [(x, y), (x + 1, y)], rnd.choice([ROPE[3], BONE[3], BONE[2]]))
    for x0 in (44, 56, 90, 100):
        for k in range(18): px(bn, [(x0 + int(2 * math.sin(k * 0.6)), 56 + k)], rnd.choice([COAT[3], COAT[2], GLASS[1]]))
    L.append(bn)
    # the head: a ship's lantern, iron frame and ring, four panes of sick green glass, a pale shape pressed against it from inside
    fr = Mask(S); fr.poly([(48, 8), (84, 8), (88, 18), (88, 56), (84, 62), (48, 62), (44, 56), (44, 18)]); L.append(shade(fr, IRON, depth=2.4))
    gp = Mask(S); gp.poly([(51, 14), (81, 14), (81, 56), (51, 56)]); L.append(shade(gp, GLASS, depth=5.0, gain=1.1, bias=0.1))
    ring = Mask(S); ring.ell(66, 4, 7, 5); r2 = Mask(S); r2.ell(66, 4, 4, 3); ring.cut(r2); L.append(shade(ring, IRON, depth=1.4))
    hm = paint(S, L)
    pale = Mask(S); pale.ell(66, 36, 11, 15, 0); hm.alpha_composite(shade(pale, ramp("#1a2a24", "#34504a", "#587a72", "#88b0a6", "#bce0d4"), depth=2.8))   # the face of whoever is inside
    for (x0, y0, x1, y1) in ((57, 20, 56, 54), (66, 14, 66, 56), (75, 20, 76, 54), (51, 35, 81, 35)): line(hm, x0, y0, x1, y1, IRON[2])                  # the panes' lead
    px(hm, [(60, 30), (61, 30), (72, 30), (73, 30)], rgb("#04100a")); px(hm, [(66, y) for y in range(42, 47)], rgb("#04100a"))                       # hollow eyes and a long mouth
    for k in range(5): px(hm, [(52 + k, 20 + k * 6)], GLASS[4])
    px(hm, [(x, 58) for x in range(54, 80, 3)], GLASS[3])                                                                                           # a pool of light at the base of the glass
    # water: running off the coat in streaks, a ring round the boots, drips off the hook
    for _ in range(70): x = rnd.randint(28, 110); y = rnd.randint(70, 168); line(hm, x, y, x, y + rnd.randint(3, 8), rnd.choice([COAT[4], GLASS[1], FLESH[3]]))
    for r, col in ((24, FLESH[1]), (30, FLESH[2]), (38, FLESH[1])):
        for t in range(0, 360, 6):
            a = math.radians(t); x = 66 + math.cos(a) * r * 1.5; y = 180 + math.sin(a) * r * 0.22
            if 0 <= x < S[0] and 0 <= y < S[1]: hm.putpixel((int(x), int(y)), tuple(col))
    a_ = np.array(hm).astype(float); H_, W_ = a_.shape[:2]; yy, xx = np.mgrid[0:H_, 0:W_]
    v = np.clip(1.0 - 0.45 * (((xx - W_ / 2) / (W_ / 2)) ** 2 + ((yy - H_ * 0.5) / (H_ * 0.62)) ** 2) ** 1.1, 0.5, 1.0); glow = ((xx > 44) & (xx < 90) & (yy < 62))
    v = np.where(glow, 1.0, v); a_[:, :, :3] *= v[:, :, None]; hm = Image.fromarray(np.clip(a_, 0, 255).astype(np.uint8), "RGBA")
    return outline(crop(hm, 2))


# ------------------------------------------------------------------ THE SPECIMEN, GROWN: the Lab Alpha brain after the vat, mobile and many-mouthed (round 1 art: a pink-brown brain, violet glowing eyes, green vat, red cables)
def specimen_grown():
    BRAIN = ramp("#2a1210", "#552520", "#80453c", "#a86a5c", "#d49a88")
    EYE = ramp("#240c48", "#4e1e9a", "#7c40d8", "#b484ff", "#f2e6ff")
    GLASS = ramp("#06140e", "#0c2c1c", "#14472f", "#22704a", "#4ea878")
    CABLE = ramp("#2a0808", "#561212", "#8a2020", "#b83838", "#e07070")
    FLESHY = ramp("#2e1816", "#5a302c", "#8a5448", "#b8806e", "#dcaa98")
    S = (190, 170); L, part = mk(S); rnd = random.Random(9)
    # the spinal legs: chains of vertebrae splaying out and down, tips probing the floor; far ones darker
    legs = [((70, 92), [(40, 106), (22, 128), (14, 156)], 0), ((84, 100), [(66, 122), (58, 144), (52, 164)], 0), ((124, 100), [(142, 122), (150, 144), (156, 164)], 1), ((142, 90), [(166, 108), (178, 130), (180, 158)], 1), ((96, 104), [(92, 128), (96, 150), (104, 166)], 0), ((112, 104), [(120, 128), (118, 150), (122, 166)], 1)]
    for (st, pts, near) in legs:
        sp = Mask(S); chain = [st] + pts
        for a, b in zip(chain, chain[1:]):
            n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1])) / 5) + 1
            for i in range(n): u = i / n; w = 4.2 - 0.8 * (chain.index(a)); sp.ell(a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u, max(1.8, w), max(1.8, w))
        L.append(shade(sp, BONE if near else ramp("#2a2519", "#4a4332", "#6e6650", "#948a70", "#b8ae92"), depth=1.6))
        tp = Mask(S); tx, ty = pts[-1]; tp.ell(tx, ty, 3.4, 3.0); L.append(shade(tp, FLESHY, depth=1.4))
    # the brain: huge, folded, the two hemispheres parted, a lab collar still clamped on the stalk, glass shards fused into the top
    part(lambda m: m.ell(96, 64, 52, 40, 0).ell(70, 56, 28, 30, 10).ell(122, 56, 28, 30, -10), ramp("#1a0a09", "#2a1210", "#3d1c18", "#552520", "#6e3028"), tex=0.2, seed=3, depth=7.0)    # the dark body under the lobes: every crevice stays black
    for k in range(70):                                                                                           # the folds: many overlapping lobes, each shaded on its own so the surface bulges and creases
        ang = rnd.uniform(0, 2 * math.pi); rr = math.sqrt(rnd.random()); cx = 96 + math.cos(ang) * rr * 44; cy = 62 + math.sin(ang) * rr * 32
        if abs(cx - 96) < 3: continue
        lb = Mask(S); lb.ell(cx, cy, rnd.uniform(7, 13), rnd.uniform(5, 9), rnd.uniform(-60, 60)); L.append(shade(lb, BRAIN, depth=3.2, tex=0.16, seed=k, gain=1.05))
    hm = paint(S, L)
    for _ in range(60): px(hm, [(rnd.randint(58, 138), rnd.randint(34, 90))], rgb("#e8b8a8"))                       # wet highlights
    fissure = Mask(S); fissure.tube([(96, 26), (98, 52), (94, 76)], [3, 3.4, 2.6]); hm.alpha_composite(shade(fissure, ramp("#100606", "#1a0a09", "#2a1210", "#3d1c18", "#552520"), depth=1.2))
    # the eyes: three, violet, wrong sizes, the big one low on the left hemisphere, two small ones among the folds
    for (ex, ey, r) in ((74, 70, 9), (112, 54, 5), (124, 76, 4)):
        w = Mask(S); w.ell(ex, ey, r + 1.6, r + 1.2); hm.alpha_composite(shade(w, ramp("#1a0a08", "#2e1612", "#4a2420", "#6a3a32", "#8a5a50"), depth=1.6))
        e = Mask(S); e.ell(ex, ey, r, r * 0.9); hm.alpha_composite(shade(e, EYE, depth=r * 0.6))
        px(hm, [(ex - r * 0.3, ey - r * 0.3), (ex - r * 0.3 + 1, ey - r * 0.3)], rgb("#ffffff")); px(hm, [(ex, ey), (ex + 1, ey)], rgb("#10002a"))
        for t in range(9):
            b = t * 0.7 + ex; x, y = ex + math.cos(b) * (r + 1), ey + math.sin(b) * (r + 1)
            for i in range(9): nx = x + math.cos(b) * 1.3 + rnd.uniform(-0.8, 0.8); ny = y + math.sin(b) * 1.3 + rnd.uniform(-0.8, 0.8); line(hm, x, y, nx, ny, CABLE[2] if i % 2 else CABLE[3]); x, y = nx, ny
    # mouths on the nerve tips hanging from the underside: ringed with teeth, the lab's name card still stuck in the slime
    for (mx, my, ang) in ((52, 100, 200), (140, 104, 330), (96, 112, 270)):
        a = math.radians(ang); t = Mask(S); t.tube([(mx, my), (mx + math.cos(a) * 12, my + math.sin(a) * -10 + 12)], [6, 5]); hm.alpha_composite(shade(t, FLESHY, depth=2.0, tex=0.1, seed=mx))
        cx, cy = mx + math.cos(a) * 12, my + math.sin(a) * -10 + 12
        o = Mask(S); o.ell(cx, cy, 8, 6.6); hm.alpha_composite(shade(o, DARK, depth=1.4))
        for k in range(14): b = k * math.pi / 7; px(hm, [(cx + math.cos(b) * 7.2, cy + math.sin(b) * 5.8), (cx + math.cos(b) * 6.0, cy + math.sin(b) * 4.8)], rgb("#e8e0cc"))
        for k in range(3): line(hm, cx - 2 + k * 2, cy + 6, cx - 2 + k * 2, cy + 13 + k, FLESHY[3])
    collar = Mask(S); collar.ell(96, 98, 14, 5); inner = Mask(S); inner.ell(96, 98, 8, 3); collar.cut(inner); hm.alpha_composite(shade(collar, ramp("#101214", "#22272b", "#3a4248", "#566068", "#7a8690"), depth=1.8))
    card = Mask(S); card.poly([(108, 100), (120, 102), (119, 110), (107, 108)]); hm.alpha_composite(shade(card, ramp("#303028", "#58584a", "#8a8a78", "#b8b8a4", "#e0e0cc"), depth=1.0))
    for k in range(3): px(hm, [(109 + k * 3, 104), (109 + k * 3, 105)], rgb("#303028"))
    # shards of the vat glass fused into the top, green; cables still trailing from the crown back towards the vat
    for (gx, gy, r, ang) in ((70, 26, 9, 20), (118, 22, 10, -25), (96, 12, 7, 5)):
        g = Mask(S); g.poly([(gx - r * 0.4, gy + r), (gx - r * 0.2, gy - r), (gx + r * 0.5, gy - r * 0.7), (gx + r * 0.3, gy + r)]); hm.alpha_composite(shade(g, GLASS, depth=1.8))
        line(hm, gx - r * 0.2, gy - r, gx + r * 0.3, gy + r * 0.9, GLASS[4])
    for (x0, y0) in ((80, 24), (100, 20), (116, 26)):
        x, y = x0, y0
        for i in range(34): nx = x + rnd.uniform(-1.4, 0.4) - 0.6 + (0.3 if x0 > 100 else 0); ny = y - 1.4; line(hm, x, y, nx, ny, CABLE[2 + i % 2]); x, y = nx, ny
    for i in range(40):                                                                                            # loose nerve strands hanging under the brain
        x = 60 + rnd.uniform(0, 74); y = 92 + rnd.uniform(-2, 6); ln = rnd.uniform(8, 30)
        for k in range(int(ln)): px(hm, [(x + 2 * math.sin(k * 0.4 + i), y + k)], FLESHY[rnd.choice((1, 2, 3))])
    # white crackle across the brain where the current still runs
    for (x0, y0) in ((60, 44), (130, 52), (92, 86)):
        x, y = x0, y0
        for i in range(7): nx = x + rnd.choice((-3, -2, 2, 3)); ny = y + rnd.randint(-2, 3); line(hm, x, y, nx, ny, rgb("#e8f0ff")); x, y = nx, ny
    a_ = np.array(hm).astype(float); H_, W_ = a_.shape[:2]; yy, xx = np.mgrid[0:H_, 0:W_]
    v = np.clip(1.0 - 0.45 * (((xx - W_ / 2) / (W_ / 2)) ** 2 + ((yy - H_ * 0.45) / (H_ * 0.6)) ** 2) ** 1.1, 0.5, 1.0); a_[:, :, :3] *= v[:, :, None]
    hm = Image.fromarray(np.clip(a_, 0, 255).astype(np.uint8), "RGBA")
    return outline(crop(hm, 2))

# ------------------------------------------------------------------ THE EXCAVATOR: a mining rig with a husk grown into the cab, the bucket a jaw, hoses like veins
def excavator():
    RUST = ramp("#1c0e06", "#3e1e0a", "#683412", "#8e5220", "#b87c3c")
    PAINT = ramp("#2e2808", "#524a10", "#7a6c1c", "#a08c30", "#c4b058")
    IRON = ramp("#0a0b0c", "#181b1e", "#2c3236", "#48515a", "#6e7a84")
    TRACK = ramp("#060606", "#121312", "#202220", "#323532", "#484c48")
    HOSE = ramp("#050505", "#0e0e0e", "#1a1a1a", "#2a2a2a", "#3e3e3e")
    GLASS = ramp("#0e1a16", "#1c3a30", "#2e5c4c", "#56907c", "#a8d8c4")
    FLESH = ramp("#241816", "#4a2e2a", "#78504a", "#a47c70", "#cfa898")
    S = (260, 160); L, part = mk(S); rnd = random.Random(2)
    # crawler tracks: a long rounded body, road wheels, link teeth
    part(lambda m: m.ell(80, 138, 62, 16).poly([(24, 130), (136, 130), (136, 150), (24, 150)]), TRACK, tex=0.1, seed=1, depth=4.0)
    for wx in range(34, 130, 16): w = Mask(S); w.ell(wx, 140, 7.4, 7.4); L.append(shade(w, IRON, depth=2.4))
    tr = paint(S, [])
    for x in range(22, 140, 4): px(tr, [(x, 124 + (1 if x % 8 else 0)), (x, 154)], TRACK[4])
    L.append(tr)
    # the house: a heavy rust-and-yellow box, peeling paint, the engine hump behind the cab
    part(lambda m: m.poly([(30, 76), (118, 76), (126, 128), (24, 128)]), RUST, tex=0.24, seed=2, depth=5.0)
    part(lambda m: m.poly([(34, 74), (62, 70), (66, 88), (34, 92)]), PAINT, tex=0.3, seed=3, depth=2.4)
    for (fx, fy) in ((44, 100), (66, 108), (92, 112), (108, 98)):                                                      # louvred engine vents, a few slats bent
        v_ = Mask(S); v_.poly([(fx - 8, fy - 5), (fx + 8, fy - 5), (fx + 8, fy + 5), (fx - 8, fy + 5)]); L.append(shade(v_, IRON, depth=1.4))
        for k in range(4): line_im = paint(S, []); line(line_im, fx - 7, fy - 3 + k * 2.4, fx + 7, fy - 3 + k * 2.4 + (1 if (fx + k) % 3 == 0 else 0), RUST[1]); L.append(line_im)
    pl = paint(S, [])
    for _ in range(80): px(pl, [(rnd.randint(30, 120), rnd.randint(78, 126))], rnd.choice([RUST[0], RUST[1], PAINT[1]]))
    for yy in range(96, 126, 6): line(pl, 32, yy, 118, yy + 1, RUST[1])
    L.append(pl)
    # the cab: a cage with a wet window; the husk behind it is grown into the seat, flesh climbing the frame
    part(lambda m: m.poly([(84, 40), (122, 40), (124, 82), (82, 82)]), IRON, depth=2.4)
    part(lambda m: m.poly([(88, 46), (118, 46), (119, 78), (87, 78)]), GLASS, depth=4.0, gain=1.1)
    hm = paint(S, L)
    hm.alpha_composite(husk_cell(40), (88, 46))                                                                                       # the real Husk, 1:1, in the driver's seat behind the glass
    gl_ = Image.new('RGBA', S, (0, 0, 0, 0)); ImageDraw.Draw(gl_).rectangle([88, 46, 118, 78], fill=(60, 120, 100, 70)); hm.alpha_composite(gl_)               # the wet, tinted glass over him
    for k in range(5): line(hm, 90 + k * 6, 48, 89 + k * 6, 76, GLASS[1])                                                                           # condensation running down the inside
    # the boom and arm: two heavy beams, hoses along them like veins, a hard hat still on the boom
    arm = Mask(S); arm.tube([(118, 66), (140, 38), (162, 20)], [20, 18, 16]); hm.alpha_composite(shade(arm, RUST, tex=0.3, seed=5, depth=4.0))
    arm2 = Mask(S); arm2.tube([(160, 20), (196, 40), (232, 64)], [15, 13, 11]); hm.alpha_composite(shade(arm2, RUST, tex=0.3, seed=6, depth=3.6))
    for (a0, b0, a1, b1) in ((126, 80, 150, 40), (166, 28, 214, 66)):                                                 # hydraulic cylinders: steel pistons beside the beams
        cy_ = Mask(S); cy_.tube([(a0, b0), (a1, b1)], [5, 5]); hm.alpha_composite(shade(cy_, IRON, depth=1.6))
    pj = Mask(S); pj.ell(162, 20, 6, 6); hm.alpha_composite(shade(pj, IRON, depth=2.0))
    for (x0, y0, x1, y1) in ((124, 60, 158, 22), (130, 66, 164, 28), (162, 22, 228, 60), (160, 30, 226, 68)):
        n = int(max(abs(x1 - x0), abs(y1 - y0))); 
        for i in range(n + 1): u = i / n; x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u + 2 * math.sin(u * 14); px(hm, [(x, y)], HOSE[2 + (i // 3) % 2])
    hat = Mask(S); hat.ell(150, 30, 7, 4); hm.alpha_composite(shade(hat, PAINT, depth=1.6))
    # the bucket: an ordinary dented steel scoop on the arm tip, five blunt digging teeth along the lip, dirt and rust caked in it
    sc = Mask(S); sc.poly([(224, 54), (240, 50), (256, 78), (252, 108), (230, 104), (222, 82)]); hm.alpha_composite(shade(sc, IRON, tex=0.2, seed=7, depth=3.4))
    lip = Mask(S); lip.poly([(240, 104), (252, 108), (254, 112), (236, 110)]); hm.alpha_composite(shade(lip, IRON, depth=1.2))
    for k in range(5):
        tx = 232 + k * 5; t_ = Mask(S); t_.poly([(tx, 106), (tx + 4, 106), (tx + 3, 116), (tx + 1, 116)]); hm.alpha_composite(shade(t_, ramp("#2a2218", "#4a3c28", "#6e5a3c", "#96805a", "#baa47a"), depth=1.0))
    for _ in range(40): px(hm, [(rnd.randint(228, 252), rnd.randint(60, 104))], rnd.choice([RUST[1], RUST[2], PAINT[0]]))
    pin = Mask(S); pin.ell(230, 58, 4, 4); hm.alpha_composite(shade(pin, IRON, depth=2.0))
    # exhaust stack and steam, rust streaks, oil stains
    stk = Mask(S); stk.tube([(46, 76), (46, 52)], [6, 6]); hm.alpha_composite(shade(stk, IRON, depth=1.6))
    for k in range(10): px(hm, [(46 + rnd.uniform(-3, 3) + k * 0.5, 50 - k * 2.6)], rnd.choice([(150, 150, 150, 110), (110, 110, 110, 90)]))
    for _ in range(30): x = rnd.randint(30, 120); y = rnd.randint(80, 126); line(hm, x, y, x, y + rnd.randint(4, 10), RUST[0])
    a_ = np.array(hm).astype(float); H_, W_ = a_.shape[:2]; yy, xx = np.mgrid[0:H_, 0:W_]
    v = np.clip(1.0 - 0.4 * (((xx - W_ / 2) / (W_ / 2)) ** 2 + ((yy - H_ * 0.55) / (H_ * 0.6)) ** 2) ** 1.1, 0.55, 1.0); a_[:, :, :3] *= v[:, :, None]
    hm = Image.fromarray(np.clip(a_, 0, 255).astype(np.uint8), "RGBA")
    return outline(crop(hm, 2))


# ------------------------------------------------------------------ THE CHOIR: a bell tower with a column of Husks climbing it, every mouth open on the same note (the Husks are the REAL Husk sprites, pasted 1:1)
def choir():
    STONE = ramp("#0e0e11", "#1c1c21", "#2c2c33", "#43434c", "#5e5e68")
    STONE_D = ramp("#08080a", "#101013", "#18181c", "#242429", "#33333a")
    BRASS = ramp("#2a1c08", "#5a3c10", "#946a1c", "#cc9a34", "#f4d06a")
    ROPE = ramp("#241c10", "#4a3a20", "#7a6238", "#a8884e", "#d0b06a")
    CORD = ramp("#2a0a0a", "#561414", "#8a2424", "#b64040", "#e07878")
    S = (200, 300); L, part = mk(S); rnd = random.Random(21)
    # the tower: a tall square stone shaft, a belfry with an open arch, a pitched roof, cracks and moss
    part(lambda m: m.poly([(52, 70), (148, 70), (152, 298), (48, 298)]), STONE, tex=0.26, seed=1, depth=6.0)
    part(lambda m: m.poly([(44, 70), (156, 70), (146, 60), (100, 20), (54, 60)]), STONE_D, tex=0.2, seed=2, depth=4.0)       # the roof
    part(lambda m: m.poly([(70, 82), (130, 82), (130, 130), (112, 142), (88, 142), (70, 130)]), ramp("#020203", "#06060a", "#0c0c12", "#14141c", "#1e1e28"), depth=2.0)    # the belfry arch, dark
    hm = paint(S, L)
    for y in range(150, 296, 14): line(hm, 52, y, 148, y + 1, STONE_D[1])                                                       # course lines
    for _ in range(60): x = rnd.randint(52, 148); y = rnd.randint(150, 296); line(hm, x, y, x + rnd.choice((-1, 0, 1)), y + rnd.randint(3, 9), STONE_D[0])
    # the bell: brass, hung in the arch, a rope down, the clapper swinging
    bl = Mask(S); bl.poly([(84, 138), (116, 138), (122, 116), (116, 96), (100, 90), (84, 96), (78, 116)]); hm.alpha_composite(shade(bl, BRASS, tex=0.1, seed=3, depth=4.4))
    bt = Mask(S); bt.tube([(100, 140), (100, 152)], [6, 6]); hm.alpha_composite(shade(bt, IRON if 'IRON' in globals() else BRASS, depth=1.2)); cl_ = Mask(S); cl_.ell(100, 154, 4, 4); hm.alpha_composite(shade(cl_, BRASS, depth=1.6))
    line(hm, 100, 90, 100, 82, ROPE[3])
    # the Husks: real sprites, a staggered column climbing the front, heads thrown back, mouths open (a black oval painted over the lower face)
    idx = [40, 33, 44, 36, 16, 31, 39, 47, 35, 22, 13, 28, 41, 8, 17, 45, 3, 26]
    ys = [266 - k * 15 for k in range(18)]
    prev_x = 84
    for k, (i, y) in enumerate(zip(idx, ys)):
        x = 84 + int(14 * math.sin(k * 1.3)) + rnd.randint(-4, 4)
        cell = husk_cell(i); hm.alpha_composite(cell, (x, y))
        prev_x_ = x
        mo = Mask(S); mo.ell(x + 16, y + 9, 3.4, 4.4); hm.alpha_composite(shade(mo, ramp("#020202", "#080404", "#140808", "#241010", "#3a1818"), depth=1.2))     # the open mouth
        for t in range(3): px(hm, [(x + 14 + t * 2, y + 6)], rgb("#cfc8b4"))
        prev_x = prev_x_                                                         # a row of teeth along the top
        if k > 0:                                                                                                                      # sinew from this mouth to the head below: fused mouth to mouth
            px_y0 = y + 12; px_y1 = ys[k - 1] + 6; ox = prev_x
            for (dx0, dx1) in ((15, 14), (17, 18), (16, 16)): line(hm, x + dx0, px_y0, ox + dx1, px_y1, CORD[2 + (dx0 % 2)])
    # the note: arcs of sound leaving the belfry, brass-coloured, and a few cracked chunks of masonry in the air
    for r, a in ((34, 90), (46, 70), (58, 50)):
        for t in range(-60, 61, 3):
            ang = math.radians(t); px(hm, [(100 + math.cos(ang) * r * 1.5, 116 + math.sin(ang) * r)], (*BRASS[3][:3], a * 2 if a * 2 < 255 else 255)); px(hm, [(100 - math.cos(ang) * r * 1.5, 116 + math.sin(ang) * r)], (*BRASS[3][:3], a * 2 if a * 2 < 255 else 255))
    a_ = np.array(hm).astype(float); H_, W_ = a_.shape[:2]; yy, xx = np.mgrid[0:H_, 0:W_]
    v = np.clip(1.0 - 0.5 * (((xx - W_ / 2) / (W_ / 2)) ** 2 + ((yy - H_ * 0.5) / (H_ * 0.6)) ** 2) ** 1.1, 0.5, 1.0); a_[:, :, :3] *= v[:, :, None]
    hm = Image.fromarray(np.clip(a_, 0, 255).astype(np.uint8), "RGBA")
    return outline(crop(hm, 2))

if __name__ == '__main__':
    items = [('singer', 'THE SINGER (T2)', singer()), ('orderly', 'THE ORDERLY (T6)', orderly()), ('mother_hollow', 'MOTHER HOLLOW (T1)', mother_hollow()), ('ferryman', 'THE FERRYMAN (T3)', ferryman()), ('specimen_grown', 'THE SPECIMEN, GROWN (T4)', specimen_grown()), ('excavator', 'THE EXCAVATOR (T5)', excavator()), ('choir', 'THE CHOIR (T6)', choir())]
    K = 2; pad = 40
    W = sum(im.width * K for _, _, im in items) + pad * 3; H = max(im.height for _, _, im in items) * K + pad * 2
    sh = Image.new('RGBA', (W, H), (22, 20, 24, 255)); x = pad; d = ImageDraw.Draw(sh)
    for k, label, im in items:
        im.save(os.path.join(OUT, k + '_concept.png')); sh.alpha_composite(im.resize((im.width * K, im.height * K), Image.NEAREST), (x, H - im.height * K - pad)); d.text((x, 10), label, fill=(220, 215, 200, 255)); x += im.width * K + pad
    sh.convert('RGB').save(os.path.join(OUT, 'boss_concepts_sheet.png')); print([(k, i.size) for k, _, i in items])
