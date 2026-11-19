# Concepts v2 for Rot Crawler, Mind Weaver, Void Weaver (2026-11-19; Yan: "redo them like you did the gaunt, he looked a lot better").
# Same method and scale as make_gaunt.py: ~80-130 px tall, anatomy first (joints, ribs, spine, long limbs), muted colours, details placed pixel by pixel, one silhouette idea each. Facing right. NOT in the game.
#   python3 ArtSource/make_concepts_v2.py   -> ArtSource/mutants_anim/concepts_new/
import os, sys, math, random
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'concepts_new'); os.makedirs(OUT, exist_ok=True)
BONE = ramp("#4a4332", "#7a7058", "#a89e82", "#cfc6a8", "#ece4cb")
DARK = ramp("#0a0506", "#1a0b0c", "#2d1214", "#431a1c", "#5e2527")
def mk(S):
    L = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
    return L, part

# ================================================================ ROT CRAWLER
def rot_crawler():
    SKIN = ramp("#12120e", "#24251b", "#3a3b2a", "#5a5a40", "#807e58")
    SKIN_F = ramp("#0b0b09", "#161711", "#24251b", "#363726", "#4b4c36")
    CAP = ramp("#241d10", "#4f3f1e", "#836a34", "#bd9f5c", "#ead8a0")
    GILL = ramp("#120d08", "#2a2012", "#47381f", "#6a5530", "#8c7442")
    S = (128, 74); L, part = mk(S)
    # far limbs: a frog-folded hind leg and a spidery fore-arm, darker, behind the body
    part(lambda m: m.tube([(30, 36), (22, 22), (16, 44), (11, 62)], [9, 6, 4.4, 3.2]), SKIN_F, tex=0.1, seed=1)
    part(lambda m: m.tube([(84, 32), (92, 18), (100, 46), (106, 63)], [7, 5, 3.8, 3]), SKIN_F, tex=0.1, seed=2)
    ft = Mask(S)
    for dx in (-3, 0, 3): ft.tube([(11, 62), (8 + dx, 68)], [1.6, 0.7])
    for dx in (-2, 1, 4): ft.tube([(106, 63), (107 + dx, 69)], [1.6, 0.7])
    L.append(shade(ft, BONE, depth=1.0))
    # the body: a hunched barrel, ribs pushing through, hips much too small for the chest
    part(lambda m: m.ell(30, 38, 11, 9, -15).ell(52, 33, 22, 13, 8).ell(76, 33, 13, 12, 20).tube([(76, 30), (88, 31), (96, 35)], [11, 8, 6.5]), SKIN, tex=0.15, seed=3, depth=4.0)
    part(lambda m: m.ell(54, 44, 17, 7, 4), SKIN, tex=0.14, seed=4, depth=2.2, bias=-0.1)                    # the belly hanging low
    # the flank torn open: a dark wound, the ribs standing out of it, the bloom growing from inside
    cav = Mask(S); cav.poly([(42, 28), (50, 25), (58, 29), (62, 36), (56, 42), (47, 41), (40, 35)]); L.append(shade(cav, DARK, depth=2.4, tex=0.08, seed=5))
    rib = Mask(S)
    for k, (x0, y0, ln) in enumerate(((43, 28, 9), (48, 26.5, 12), (53, 27, 13), (58, 30, 9))):
        rib.tube([(x0, y0), (x0 + 3 + k * 0.4, y0 + ln * 0.55), (x0 + 7, y0 + ln)], [1.7, 1.8, 1.0])
    L.append(shade(rib, BONE, depth=1.3))
    sp = Mask(S)                                                                                                      # spine knuckles under the skin along the arch
    for i, (x, y) in enumerate([(88, 23), (80, 20.5), (72, 19.5), (64, 19.5), (56, 20.5), (48, 22), (40, 25), (33, 29), (28, 34)]): sp.ell(x, y, 2.7 - i * 0.1, 2.2)
    L.append(shade(sp, BONE, depth=1.5))
    # the bloom: bracket fungus in overlapping shelves up the spine and shoulder blades, gills underneath, a few threads of mycelium hanging
    for (cx, cy, rx, ry, sd) in [(80, 17, 9, 3.4, 11), (68, 15, 11, 4, 12), (56, 16, 10, 3.6, 13), (45, 19, 8, 3.2, 14), (36, 23, 6, 2.6, 15), (73, 22, 5, 2.2, 16)]:
        g = Mask(S); g.ell(cx, cy + 2.4, rx * 0.9, ry * 0.7); L.append(shade(g, GILL, depth=1.2, tex=0.12, seed=sd + 20))
        m = Mask(S); m.ell(cx, cy, rx, ry, -5); L.append(shade(m, CAP, depth=1.8, tex=0.12, seed=sd))
    my = paint(S, [])
    for (x, y, ln) in ((62, 20, 7), (66, 20, 9), (50, 21, 6), (76, 21, 8), (84, 21, 5)): line(my, x, y, x + 1, y + ln, CAP[3]); px(my, [(x + 1, y + ln)], CAP[4])
    L.append(my)
    # near limbs
    part(lambda m: m.tube([(32, 42), (38, 26), (28, 48), (20, 64)], [11, 7.5, 5, 3.6]), SKIN, tex=0.12, seed=6, bias=-0.1)                  # the haunch: knee above the back
    part(lambda m: m.tube([(80, 38), (90, 27), (98, 48), (104, 65)], [9, 6.2, 4.6, 3.6]), SKIN, tex=0.12, seed=7, bias=-0.1)                # forearm too long, elbow high
    nl = Mask(S)
    for dx, dy in ((-3, 0), (0, 1.5), (3, 0)): nl.tube([(20, 64), (17 + dx, 70 + dy)], [1.8, 0.7])
    for dx, dy in ((-3, 1), (0, 2), (3, 1), (6, 0)): nl.tube([(104, 65), (105 + dx, 71 + dy)], [1.8, 0.7])
    L.append(shade(nl, ramp("#0a0908", "#1b1713", "#2d261d", "#473c2f", "#675646"), depth=1.0))
    # the head: a person's skull dragged low, skin hanging off the cheek, jaw dislocated and swinging
    HL = []
    def hp(build, cols, **kw): m = Mask(S); build(m); HL.append(shade(m, cols, **kw))
    hp(lambda m: m.ell(102, 37, 9, 8, 8).poly([(100, 33), (112, 38), (114, 43), (104, 45)]), BONE, tex=0.1, seed=8, depth=3.0, bias=-0.1)
    hp(lambda m: m.poly([(103, 44), (116, 44), (113, 60), (105, 63), (100, 52)]), ramp("#14100c", "#2c2418", "#4d3f2a", "#76624a", "#a38c6c"), depth=1.6, tex=0.1, seed=9)
    hp(lambda m: m.poly([(104, 45), (115, 45), (112, 57), (106, 59), (102, 51)]), DARK, depth=1.0)
    hm = paint(S, HL); tooth, tooth_d = rgb("#e6dcc0"), rgb("#b3a88a")
    for x in (104, 106, 108, 110, 112, 114): px(hm, [(x, 44), (x, 45), (x, 46 if x % 4 == 0 else 45)], tooth)
    for x in (106, 108, 110, 112): px(hm, [(x, 57), (x, 56)], tooth_d)
    line(hm, 108, 50, 107, 60, rgb("#6b2a2a")); line(hm, 109, 50, 110, 58, rgb("#8a3a3a"))                                 # tongue
    px(hm, [(x, y) for x in range(96, 101) for y in range(34, 39)], rgb("#050403")); px(hm, [(97, 35)], BONE[1])         # the socket
    px(hm, [(98, 36)], rgb("#d8ff9a")); px(hm, [(99, 36)], rgb("#8fd050"))                                                # spore-lit point
    px(hm, [(110, 40), (111, 40)], rgb("#050403"))
    line(hm, 100, 31, 96, 27, CAP[2]); px(hm, [(95, 26), (96, 26), (97, 26)], CAP[3]); line(hm, 104, 30, 107, 26, CAP[2]); px(hm, [(107, 25), (108, 25)], CAP[3])   # caps in the hair line
    line(hm, 98, 31, 90, 37, SKIN[3]); line(hm, 99, 32, 92, 40, SKIN[2])                                                  # strips of scalp trailing back
    line(hm, 103, 42, 105, 50, SKIN[2])
    L.append(hm)
    return outline(crop(paint(S, L), 2))

# ================================================================ MIND WEAVER
def mind_weaver():
    SKIN = ramp("#1c1a22", "#35323d", "#57535f", "#837e8d", "#b1acbb")
    SKIN_F = ramp("#100f14", "#1c1a22", "#2a2832", "#403d49", "#59555f")
    BRAIN = ramp("#2c0f26", "#561b46", "#8a2f6c", "#bd558f", "#e48fb9")
    NERVE = ramp("#2a2150", "#4a3a8a", "#7a64d0", "#a89af0", "#e6e0ff")
    S = (116, 156); L, part = mk(S)
    rnd = random.Random(4)
    # the trailing spine: it hangs from the pelvis in a long curve of vertebrae, ribbons of skin and a spray of loose nerves, never touching the ground
    spine = [(54, 98), (52, 108), (48, 118), (47, 128), (50, 138), (54, 146)]
    nv = paint(S, [])
    for i in range(14):
        sx = 54 + rnd.uniform(-5, 5); pts = [(sx, 100)]
        for k in range(1, 5): pts.append((sx + rnd.uniform(-9, 9) * k / 3 - 1.5 * k, 100 + k * rnd.uniform(9, 13)))
        for a, b in zip(pts, pts[1:]): line(nv, a[0], a[1], b[0], b[1], NERVE[1 + (i % 3)])
        px(nv, [(pts[-1][0], pts[-1][1])], NERVE[4])
    L.append(nv)
    rib_sk = Mask(S); rib_sk.tube([(46, 98), (40, 112), (42, 128)], [4, 3, 1.4]); rib_sk.tube([(60, 98), (66, 112), (62, 126)], [4, 3, 1.4]); L.append(shade(rib_sk, SKIN_F, depth=1.2, tex=0.1, seed=3))
    sp = Mask(S)
    for i, (x, y) in enumerate(spine): sp.ell(x, y, 3.1 - i * 0.28, 2.7 - i * 0.2)
    L.append(shade(sp, BONE, depth=1.7))
    # far arm (reaching left) and trunk: an emaciated chest, collarbones, the skin drawn tight over every rib, a belly gone to a hollow
    part(lambda m: m.tube([(42, 58), (28, 66), (18, 52), (14, 36)], [6, 4.8, 3.6, 2.6]), SKIN_F, tex=0.1, seed=1)
    part(lambda m: m.ell(52, 74, 10.5, 21, 6).poly([(46, 54), (62, 52), (64, 70), (58, 98), (46, 98), (42, 70)]), SKIN, tex=0.12, seed=2, depth=3.8)
    cav = Mask(S); cav.ell(54, 84, 5.5, 9, 4); L.append(shade(cav, DARK, depth=1.6, tex=0.06, seed=5))                 # the belly: a hollow behind the ribs
    rb = Mask(S)
    for k in range(7):
        y0 = 62 + k * 4.6; w = 5 + (2 if 1 < k < 5 else 0) - (k > 5); rb.tube([(47 - w * 0.2, y0), (52, y0 + 1.6), (57 + w * 0.5, y0 + 0.4)], [1.4, 1.5, 1.1])
    L.append(shade(rb, BONE, depth=1.1))
    cb = Mask(S); cb.tube([(44, 55), (52, 57), (60, 55)], [1.7, 1.7, 1.5]); L.append(shade(cb, BONE, depth=1.1))        # collarbones
    # the neck: a long, thin throat held up by the cords of a hanged man
    part(lambda m: m.tube([(53, 56), (54, 46), (54, 38)], [7, 5.6, 4.8]), SKIN, tex=0.1, seed=6, depth=2.6)
    # near arm (up, forward) with a hand of impossibly long fingers
    part(lambda m: m.tube([(62, 58), (78, 64), (88, 50), (92, 34)], [6.4, 5, 3.8, 2.8]), SKIN, tex=0.1, seed=7, bias=-0.1)
    fin = Mask(S)
    for (bx, by, dx, dy) in ((92, 34, -5, -14), (92, 34, 0, -16), (92, 34, 5, -13), (92, 34, 9, -9), (14, 36, -6, -12), (14, 36, -1, -15), (14, 36, 4, -13)):
        fin.tube([(bx, by), (bx + dx * 0.55, by + dy * 0.6), (bx + dx, by + dy)], [1.7, 1.2, 0.5])
    L.append(shade(fin, SKIN, depth=1.0))
    net = paint(S, [])
    L.append(net)   # (filled after the head so the cradle hangs in front of the chest)
    # the head: a long skull, the cranium cracked open and peeled back in bone petals, the brain bare and faintly lit, eyes sewn, a stitched mouth
    HL, hp = mk(S)
    def petals(m):
        for ang in (-70, -45, -20, 5, 30, 56):
            r = math.radians(ang - 90); cx, cy = 54 + 8 * math.cos(r), 20 + 8 * math.sin(r)
            m.tube([(cx, cy), (cx + 7 * math.cos(r), cy + 7 * math.sin(r)), (cx + 12 * math.cos(r + 0.18), cy + 12 * math.sin(r + 0.18))], [3.4, 2.2, 0.9])
    hp(petals, BONE, depth=1.4, tex=0.08, seed=9)
    hp(lambda m: m.ell(54, 19, 9.2, 6.4), BRAIN, depth=3.2, tex=0.22, seed=10)
    hp(lambda m: m.ell(54, 29, 8.4, 13).poly([(48, 38), (60, 38), (57, 46), (51, 46)]), SKIN, tex=0.1, seed=11, depth=3.4, bias=-0.1)
    hm = paint(S, HL)
    for ex in (50, 58):
        px(hm, [(ex + i, 29) for i in range(-2, 3)], rgb("#07050b"))
        for st in (-1, 1): line(hm, ex + st, 27, ex + st + 1, 32, NERVE[4])
    px(hm, [(x, 40) for x in range(49, 60)], rgb("#07050b")); px(hm, [(x, 39) for x in range(50, 59, 2)] + [(x, 41) for x in range(49, 60, 2)], NERVE[3])
    px(hm, [(54, y) for y in range(31, 36)], SKIN[1]); px(hm, [(53, 35), (55, 35)], SKIN[1])
    for (a, b, c, d, e, f) in ((48, 17, 52, 14, 56, 16), (50, 21, 55, 19, 60, 21), (47, 20, 49, 17, 51, 14)): line(hm, a, b, c, d, BRAIN[1]); line(hm, c, d, e, f, BRAIN[4])
    px(hm, [(48 + i, 23) for i in range(0, 14, 2)], NERVE[4])
    line(hm, 51, 42, 50, 50, SKIN[2]); line(hm, 57, 42, 58, 52, SKIN[1])                                              # skin flaps off the jaw
    L.append(hm)
    ends = ((14, 22), (92, 20))                                                                                   # the cat's cradle: strands sagging between the fingertips, hung in front of the body
    for sag, col in ((34, NERVE[2]), (44, NERVE[3]), (54, NERVE[2])):
        prev = None
        for i in range(0, 41):
            t = i / 40; x = ends[0][0] + (ends[1][0] - ends[0][0]) * t; y = ends[0][1] + (ends[1][1] - ends[0][1]) * t + sag * 4 * t * (1 - t)
            if prev: line(net, prev[0], prev[1], x, y, col)
            prev = (x, y)
    for (x, y) in ((30, 52), (54, 60), (78, 52), (42, 58), (66, 58)): px(net, [(x, y), (x + 1, y), (x, y + 1)], NERVE[4])
    L.append(net)
    return outline(crop(paint(S, L), 2))

# ================================================================ VOID WEAVER
def void_weaver():
    CLOTH = ramp("#08070e", "#110e1e", "#1d1632", "#2e2350", "#473a78")
    CLOTH_F = ramp("#050409", "#0b0913", "#130f22", "#1d1632", "#2b2248")
    HOOD_IN = ramp("#020206", "#04030a", "#08060f", "#0f0b1c", "#181230")
    BONEV = ramp("#2f2c3b", "#575269", "#8a849e", "#b9b3cc", "#e5e1f2")
    GLOW = ramp("#412a8a", "#6a46d0", "#9a78fa", "#cdb8ff", "#fbf6ff")
    S = (132, 168); L, part = mk(S); rnd = random.Random(7)
    # the trailing cloth: a long torn train behind it (it leans forward), strips fraying into motes
    part(lambda m: m.poly([(48, 52), (64, 50), (44, 112), (16, 150), (6, 158), (14, 120), (30, 90)]), CLOTH_F, tex=0.12, seed=1, depth=3.0)
    hem = Mask(S)
    for i in range(8):
        x = 4 + i * 7; ln = 8 + rnd.randint(0, 16); hem.poly([(x, 152 - i), (x + 6, 150 - i), (x + 2 + rnd.uniform(-2, 2), 152 - i + ln)])
    L.append(shade(hem, CLOTH_F, depth=1.2, tex=0.1, seed=2))
    # the body robe: a stooped, narrow torso, a flared skirt hung in three torn tiers, the front of it lifting as it drifts
    part(lambda m: m.poly([(52, 46), (74, 44), (84, 84), (96, 124), (82, 150), (60, 156), (40, 146), (46, 100), (42, 70)]), CLOTH, tex=0.14, seed=3, depth=4.0)
    part(lambda m: m.poly([(48, 96), (86, 100), (96, 126), (80, 142), (62, 134), (50, 118)]), CLOTH_F, tex=0.1, seed=4, depth=2.0)
    part(lambda m: m.poly([(50, 118), (92, 126), (84, 148), (62, 154), (46, 144)]), CLOTH, tex=0.12, seed=5, depth=2.2, bias=-0.1)
    # shoulders: hunched, one higher than the other; a ridge of bone under the cloth at the shoulder blade
    part(lambda m: m.ell(62, 54, 20, 9, 10), CLOTH, tex=0.1, seed=6, depth=3.0)
    sb = Mask(S); sb.tube([(48, 50), (56, 46), (66, 48)], [3, 3.2, 2.4]); L.append(shade(sb, BONEV, depth=1.2))
    # far arm hangs, long sleeve to the knee; near arm reaches forward, the sleeve trailing off the wrist in strips
    part(lambda m: m.tube([(50, 56), (44, 80), (46, 104), (50, 124)], [8, 6.6, 5.4, 4.4]), CLOTH_F, tex=0.1, seed=7)
    part(lambda m: m.tube([(76, 54), (90, 66), (104, 70), (116, 66)], [9, 7, 5.6, 4.6]), CLOTH, tex=0.1, seed=8, bias=-0.1)
    st = Mask(S)
    for i in range(5): st.tube([(98 + i * 2, 70), (96 + i * 2 - 2, 86 + i * 3 + rnd.randint(0, 5))], [2.2, 0.7])
    L.append(shade(st, CLOTH, depth=1.0))
    hands = Mask(S)
    for k in range(6):
        a_ = math.radians(-35 + k * 14); hands.tube([(116, 66), (116 + 12 * math.cos(a_), 66 + 12 * math.sin(a_) * 0.9)], [1.6, 0.5])                 # six long fingers, reaching
    hands.ell(116, 66, 3.4, 3.2)
    for k in range(6): a_ = math.radians(60 + k * 11); hands.tube([(50, 124), (50 + 4 * math.cos(a_), 124 + 12 * math.sin(a_))], [1.5, 0.5])        # the hanging hand
    hands.ell(50, 124, 2.8, 3)
    L.append(shade(hands, BONEV, depth=1.3))
    # fold lines in the cloth, by hand: darker valleys, lit ridges
    fl = paint(S, [])
    for (x0, y0, x1, y1) in ((58, 66, 52, 100), (68, 66, 74, 104), (62, 104, 56, 140), (78, 108, 86, 138), (50, 124, 46, 146)):
        line(fl, x0, y0, x1, y1, CLOTH[0]); line(fl, x0 + 2, y0, x1 + 2, y1, CLOTH[4])
    L.append(fl)
    # the cracks of light
    cr = paint(S, [])
    for (x0, y0) in ((64, 76), (74, 90), (58, 112), (80, 118)):
        x, y = x0, y0
        for i in range(9):
            nx, ny = x + rnd.choice((-2, -1, 0, 1, 2)), y + 3 + rnd.randint(0, 2); line(cr, x, y, nx, ny, GLOW[2] if i % 2 else GLOW[3]); x, y = nx, ny
    L.append(cr)
    # the head: a hood that bends forward like a heron's neck; inside it only dark and a small pale mask with a slit
    HL, hp = mk(S)
    hp(lambda m: m.tube([(64, 46), (66, 34), (70, 22)], [12, 10, 9]).poly([(56, 30), (62, 12), (76, 3), (92, 10), (100, 24), (96, 38), (84, 46), (64, 48)]), CLOTH, tex=0.12, seed=9, depth=3.6)
    hp(lambda m: m.ell(87, 27, 10.5, 14, -8), HOOD_IN, depth=2.0)
    hp(lambda m: m.ell(89, 27, 4.4, 7.6, -8), BONEV, tex=0.03, seed=10, depth=2.8)
    hm = paint(S, HL)
    for y in range(21, 34): px(hm, [(90, y)], GLOW[4] if 25 < y < 31 else GLOW[2])
    px(hm, [(89, y) for y in range(24, 32)] + [(91, y) for y in range(26, 30)], GLOW[1])
    L.append(hm)
    # the broken halo behind the hood: shards that turn, none touching, one trailing a thread of light to the hand
    sh_l = paint(S, [])
    for i in range(7):
        a = math.radians(-190 + i * 44); cx, cy = 76 + 26 * math.cos(a), 24 + 17 * math.sin(a)
        sh = Mask(S); sh.poly([(cx, cy - 4.4), (cx + 2.2, cy + 0.5), (cx, cy + 5), (cx - 1.8, cy)]); sh_l.alpha_composite(shade(sh, GLOW, depth=1.2))
    L.insert(0, sh_l)
    for i in range(22): px(L[-1], [(rnd.randint(6, 100), rnd.randint(150, 166))], GLOW[rnd.choice((1, 2, 3))])
    return outline(crop(paint(S, L), 2))

if __name__ == '__main__':
    items = [('rot_crawler', 'ROT CRAWLER (Swarm, T1)', rot_crawler()), ('mind_weaver', 'MIND WEAVER (T3)', mind_weaver()), ('void_weaver', 'VOID WEAVER (T5)', void_weaver())]
    K = 5; pad = 40
    W = sum(im.width * K for _, _, im in items) + pad * (len(items) + 1); H = max(im.height for _, _, im in items) * K + pad * 2
    sheet = Image.new('RGBA', (W, H), (24, 22, 28, 255)); x = pad; d = ImageDraw.Draw(sheet)
    for key, label, im in items:
        im.save(os.path.join(OUT, key + '_concept.png')); big = im.resize((im.width * K, im.height * K), Image.NEAREST)
        sheet.alpha_composite(big, (x, H - big.height - pad)); d.text((x, 10), label, fill=(220, 215, 200, 255)); x += big.width + pad
    sheet.convert('RGB').save(os.path.join(OUT, 'concepts_sheet.png')); print([(k, im.size) for k, _, im in items])
