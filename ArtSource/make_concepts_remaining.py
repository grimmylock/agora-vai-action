# Concepts for the three mutants that still have no updated sprite (2026-11-19; Yan: "create concepts for the mutants that do not have updated sprites yet, like you did with the gaunt ... i'll come back later and we'll run them through the sprite makers").
# Hand-built with pxart.py like make_gaunt.py (NOT generated, NOT in the game). Facing right. Output: ArtSource/mutants_anim/concepts_new/ (sprites + a labelled sheet). Written for the reference-and-sprite-maker step that follows.
#   python3 ArtSource/make_concepts_remaining.py
import os, sys, math, random
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pxart import *
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'concepts_new'); os.makedirs(OUT, exist_ok=True)
BONE = ramp("#4a4332", "#7a7058", "#a89e82", "#cfc6a8", "#ece4cb")
DARKR = ramp("#0a0506", "#1a0b0c", "#2d1214", "#431a1c", "#5e2527")

# ---------------------------------------------------------------- Rot Crawler (Swarm, T1): what is left of a person that has been eaten from the inside by a bloom
def rot_crawler():
    FLESH = ramp("#17120e", "#2f241a", "#4d392a", "#755238", "#a07650")
    FLESH_F = ramp("#0e0b09", "#1c1510", "#2e2219", "#47321f", "#634529")
    FUNG = ramp("#1e2610", "#3f5216", "#6c8a22", "#a5c93e", "#e0f27c")
    S = (84, 52); L = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
    # far limbs (folded spider-wise, elbows above the back)
    part(lambda m: m.tube([(46, 26), (54, 16), (60, 38)], [5, 3.6, 2.4]), FLESH_F, tex=0.1, seed=1)
    part(lambda m: m.tube([(22, 28), (14, 16), (10, 40)], [6, 4, 2.6]), FLESH_F, tex=0.1, seed=2)
    # the body: a long dragging barrel, spine arched, a pelvis that no longer matches the chest
    part(lambda m: m.ell(24, 29, 9, 8, -10).ell(38, 26, 14, 9, 8).tube([(40, 22), (50, 22), (58, 26)], [8, 6.5, 5.5]), FLESH, tex=0.14, seed=3, depth=3.4)
    # the belly split open and bloomed: a dark seam of rot with ribs
    cav = Mask(S); cav.ell(34, 31.5, 9, 3.2, 6); L.append(shade(cav, DARKR, depth=1.8, tex=0.08, seed=4))
    rib = Mask(S)
    for k in range(4): x = 29 + k * 4.6; rib.tube([(x, 29.5), (x + 0.8, 31.5), (x + 2, 33.2)], [1.1, 1.1, 0.7])
    L.append(shade(rib, BONE, depth=1.2))
    sp = Mask(S)
    for i, (x, y) in enumerate([(50, 19), (44, 17.5), (38, 18), (32, 19.5), (26, 22), (20, 25)]): sp.ell(x, y, 2.1, 1.8)
    L.append(shade(sp, BONE, depth=1.4))
    # the bloom: bracket fungus in tiers along the spine, a few small caps on the shoulders
    for (cx, cy, rx, ry, sd) in [(44, 15, 6.5, 2.8, 11), (35, 14, 8, 3.2, 12), (27, 16.5, 6, 2.6, 13), (19, 20, 4.6, 2.2, 14), (50, 17, 3.4, 1.8, 15)]:
        m = Mask(S); m.ell(cx, cy, rx, ry, -4); L.append(shade(m, FUNG, depth=1.6, tex=0.1, seed=sd))
        st = Mask(S); st.tube([(cx, cy + 1), (cx, cy + 3.2)], [2.2, 2.2]); L.append(shade(st, FUNG[:3], depth=1.0))
    # near limbs: forearms too long, knuckles down, hands splayed with black nails
    part(lambda m: m.tube([(46, 28), (56, 30), (62, 41), (68, 45)], [7, 5, 3.8, 2.8]), FLESH, tex=0.12, seed=5, bias=-0.1)
    part(lambda m: m.tube([(20, 31), (14, 38), (18, 44), (10, 46)], [8, 5.5, 4, 3]), FLESH, tex=0.12, seed=6, bias=-0.1)
    cl = Mask(S)
    for dx, dy in ((0, 0), (2.2, 1.6), (-1.8, 1.4)): cl.tube([(68 + dx * 0.5, 45 + dy * 0.3), (72 + dx, 47 + dy)], [1.6, 0.6])
    for dx, dy in ((0, 0), (-2.4, 1.2), (-4.2, 0.2)): cl.tube([(10 + dx * 0.4, 46), (6 + dx, 47.5 + dy)], [1.6, 0.6])
    L.append(shade(cl, ramp("#0a0808", "#1b1613", "#2b231d", "#443a30", "#625343"), depth=1.0))
    # the head: low and forward, a human skull with the jaw unhinged and hung down, moss in the sockets
    HLs = []
    def hp(build, cols, **kw): m = Mask(S); build(m); HLs.append(shade(m, cols, **kw))
    hp(lambda m: m.ell(62, 27, 7.5, 6.5, 8).poly([(60, 25), (72, 29), (74, 33), (66, 34)]), BONE, tex=0.1, seed=7, depth=2.6, bias=-0.1)
    hp(lambda m: m.poly([(64, 33), (75, 33.5), (72, 43), (66, 44), (62, 37)]), ramp("#17120e", "#33291f", "#554435", "#80684e", "#a89070"), depth=1.4, tex=0.1, seed=8)
    hp(lambda m: m.poly([(65, 33), (74, 33.3), (71, 41), (67, 42), (64, 36)]), DARKR, depth=1.0)
    hm = paint(S, HLs); tooth = rgb("#e9e1c8")
    for x in (66, 68, 70, 72, 74): px(hm, [(x, 33), (x, 34)], tooth)
    for x in (67, 69, 71): px(hm, [(x, 41), (x, 40)], rgb("#b9af92"))
    px(hm, [(x, y) for x in range(58, 62) for y in range(24, 28)], rgb("#060403")); px(hm, [(59, 25), (60, 26)], rgb("#e4f27c")); px(hm, [(60, 25)], rgb("#ffffff"))   # a hollow with one pale spore-lit point
    px(hm, [(66, 29), (67, 29)], rgb("#060403"))
    px(hm, [(60, 22), (61, 21), (63, 21), (65, 22), (62, 22)], FUNG[2])                                    # moss crowning the skull
    L.append(hm)
    return outline(crop(paint(S, L), 2))

# ---------------------------------------------------------------- Mind Weaver (T3): a seer whose skull has opened like a flower round a lit brain; its eyes are sewn shut, it sees by the threads
def mind_weaver():
    ROBE = ramp("#100c1a", "#1e1730", "#33264f", "#4f3a78", "#7557a6")
    SKIN = ramp("#2a2530", "#4a4352", "#726a7e", "#a49cb0", "#cfc7d8")
    BRAIN = ramp("#3b1230", "#6c1f58", "#a63a86", "#d567b0", "#f4a6d8")
    THREAD = ramp("#3a2a66", "#6a4cb0", "#9d7cf0", "#c9b2ff", "#f0e8ff")
    S = (84, 108); L = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
    # the hanging threads: it has no legs, only a fall of fine strands that stop short of the ground
    th = Mask(S)
    rnd = random.Random(5)
    for i in range(11):
        x = 28 + i * 2.6; ln = 22 + rnd.randint(-6, 12) + (4 - abs(i - 5)) * 2.2
        th.tube([(x, 62), (x + rnd.uniform(-2, 2), 62 + ln * 0.55), (x + rnd.uniform(-4, 4), 62 + ln)], [1.9, 1.3, 0.6])
    L.append(shade(th, THREAD[:4], depth=1.0, tex=0.1, seed=3))
    # the shroud: a narrow torn robe that widens to the hem and ends in strips
    part(lambda m: m.poly([(34, 40), (44, 40), (48, 62), (56, 78), (22, 78), (30, 62)]).ell(39, 46, 6.5, 10, 6), ROBE, tex=0.14, seed=2, depth=3.2)
    part(lambda m: m.tube([(40, 34), (41, 40)], [4, 5]), SKIN, tex=0.1, seed=12)
    # far arm (up, weaving) and near arm (up, weaving): impossibly long fingers
    part(lambda m: m.tube([(34, 40), (20, 44), (14, 32), (18, 20)], [5, 3.8, 3, 2.4]), ramp("#0a0810", "#161220", "#241d36", "#39304f", "#524668"), tex=0.1, seed=4)
    part(lambda m: m.tube([(46, 40), (60, 46), (66, 34), (62, 22)], [5.4, 4, 3.2, 2.6]), SKIN, tex=0.1, seed=5, bias=-0.15)
    fin = Mask(S)
    for (bx, by, dx, dy) in ((18, 20, -3, -9), (18, 20, 1, -10), (18, 20, 4, -8), (62, 22, -4, -9), (62, 22, 0, -10), (62, 22, 3, -8)):
        fin.tube([(bx, by), (bx + dx * 0.6, by + dy * 0.6), (bx + dx, by + dy)], [1.5, 1.1, 0.6])
    L.append(shade(fin, SKIN, depth=1.0))
    # the threads it weaves: a lit lattice between the hands
    thr = paint(S, []); 
    for k in range(5):
        a = (18 + k * 2 - 4, 12 + (k % 2)); b = (62 + k * 1.2 - 3, 14 + (k % 2) * 2)
        line(thr, a[0], a[1], b[0], b[1] + (k - 2) * 2, THREAD[2] if k % 2 else THREAD[3])
    L.append(thr)
    # the head: an over-large skull, the crown opened into bone petals round a bare, lit brain
    def crown(m):
        for ang in (-62, -36, -12, 12, 36, 62):
            r = math.radians(ang - 90); cx, cy = 40 + 7 * math.cos(r), 15 + 7 * math.sin(r)
            m.tube([(cx, cy), (cx + 6 * math.cos(r), cy + 6 * math.sin(r)), (cx + 10 * math.cos(r + 0.15), cy + 10 * math.sin(r + 0.15))], [3.0, 2.0, 0.9])
    part(crown, BONE, depth=1.3, tex=0.08, seed=6)
    part(lambda m: m.ell(40, 15, 8.4, 6), BRAIN, depth=3.0, tex=0.2, seed=7)
    part(lambda m: m.ell(40, 26, 8, 13).poly([(34, 34), (46, 34), (43, 40), (37, 40)]), SKIN, tex=0.1, seed=8, depth=3.4, bias=-0.1)
    hm = paint(S, L)
    # sewn-shut eyes: dark slits crossed with thread; a thin stitched mouth; the brain's folds picked out by hand
    for ex in (36, 44):
        px(hm, [(ex + i, 26) for i in range(-2, 3)], rgb("#0a0610"))
        for st in (-1, 1): line(hm, ex + st, 24, ex + st + 1, 28, THREAD[4])
    px(hm, [(x, 35) for x in range(37, 44)], rgb("#0a0610")); px(hm, [(x, 34) for x in range(38, 43, 2)] + [(x, 36) for x in range(37, 44, 2)], THREAD[3])
    px(hm, [(40, y) for y in range(29, 33)], SKIN[1])   # a nose ridge, no nostrils: it does not breathe
    for fold in ((34, 13, 38, 15, 41, 12), (36, 17, 40, 19, 45, 16), (33, 17, 36, 14, 38, 11)):
        pass
    # a faint glow where the brain meets the bone
    px(hm, [(34 + i, 19) for i in range(0, 13, 2)], THREAD[4])
    return outline(crop(hm, 2))

# ---------------------------------------------------------------- Void Weaver (T5): a tall figure in many torn layers, a smooth blank mask with one slit of light, a ring of broken shards where a halo would be
def void_weaver():
    ROBE = ramp("#0b0912", "#171226", "#251c3e", "#3a2c60", "#574290")
    ROBE_F = ramp("#07060c", "#0e0b18", "#171226", "#251c3e", "#352a58")
    MASK = ramp("#4b4658", "#77718a", "#a49fb8", "#d0cce0", "#f3f1fa")
    GLOW = ramp("#4a2a9a", "#7a4ae0", "#a47cff", "#d2b8ff", "#fbf5ff")
    S = (96, 132); L = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); L.append(shade(m, cols, **kw)); return m
    # the far cloak panel and the hem strips that fray into motes
    part(lambda m: m.poly([(34, 40), (58, 38), (72, 112), (50, 118), (24, 114), (28, 80)]), ROBE_F, tex=0.12, seed=1, depth=3.0)
    rnd = random.Random(9)
    hem = Mask(S)
    for i in range(9):
        x = 24 + i * 5.4; ln = 8 + rnd.randint(0, 14); hem.poly([(x, 110), (x + 5, 110), (x + 2.5 + rnd.uniform(-1.5, 1.5), 110 + ln)])
    L.append(shade(hem, ROBE_F, depth=1.2, tex=0.1, seed=2))
    # the main robe: narrow at the shoulders, falling in a long flare, a torn over-layer cut on the diagonal
    part(lambda m: m.poly([(36, 36), (58, 34), (62, 70), (76, 110), (52, 118), (26, 114), (32, 72)]), ROBE, tex=0.14, seed=3, depth=3.6)
    part(lambda m: m.poly([(40, 60), (62, 66), (74, 110), (52, 100), (46, 84)]), ROBE_F, tex=0.1, seed=13, depth=2.0)
    part(lambda m: m.ell(47, 38, 14, 5.5), ROBE, tex=0.1, seed=4, depth=2.4, bias=0.05)
    # arms: long sleeves hanging, hands of six long fingers
    part(lambda m: m.tube([(35, 42), (28, 62), (30, 82), (33, 98)], [6.5, 5.6, 5, 4.2]), ROBE_F, tex=0.1, seed=5)
    part(lambda m: m.tube([(58, 42), (65, 62), (64, 82), (60, 98)], [7, 6, 5.4, 4.4]), ROBE, tex=0.1, seed=6, bias=-0.1)
    hands = Mask(S)
    for (hx, hy, s_) in ((33, 98, 1), (60, 98, 1)):
        for k in range(6): a_ = math.radians(60 + k * 11); hands.tube([(hx, hy), (hx + 4 * math.cos(a_) * s_, hy + 10 * math.sin(a_))], [1.5, 0.6])
        hands.ell(hx, hy, 2.6, 3.0)
    L.append(shade(hands, MASK, depth=1.3))
    # the cracks of light running down the cloth
    cr = paint(S, [])
    for (x0, y0) in ((43, 56), (53, 62), (47, 76)):
        x, y = x0, y0
        for i in range(8):
            nx, ny = x + rnd.choice((-2, -1, 0, 1, 2)), y + 3 + rnd.randint(0, 2); line(cr, x, y, nx, ny, GLOW[2] if i % 2 else GLOW[3]); x, y = nx, ny
    L.append(cr)
    # the head: a tall pointed hood, a small blank mask set deep inside it with one slit of light
    part(lambda m: m.poly([(37, 18), (47, 2), (57, 18), (60, 40), (34, 40)]).ell(47, 22, 11, 13), ROBE, tex=0.1, seed=7, depth=3.0)
    part(lambda m: m.ell(47, 24, 7, 10), ramp("#05040a", "#0a0812", "#120e20", "#1c1630", "#2a2148"), depth=2.5)
    part(lambda m: m.ell(47, 25, 5.2, 8), MASK, tex=0.04, seed=8, depth=2.6)
    hm = paint(S, L)
    for y in range(19, 33): px(hm, [(47, y)], GLOW[4] if 23 < y < 30 else GLOW[2])
    px(hm, [(46, y) for y in range(22, 31)] + [(48, y) for y in range(24, 29)], GLOW[1])
    # the broken halo: shards turning at head height, none touching
    for i in range(7):
        a = math.radians(-160 + i * 49); cx, cy = 47 + 24 * math.cos(a), 22 + 12 * math.sin(a)
        sh = Mask(S); sh.poly([(cx, cy - 4), (cx + 2.2, cy + 1), (cx, cy + 4.5), (cx - 1.6, cy)]); hm.alpha_composite(shade(sh, GLOW, depth=1.2))
    # motes falling from the hem
    for i in range(16): px(hm, [(rnd.randint(24, 74), rnd.randint(118, 130))], GLOW[rnd.choice((1, 2, 3))])
    return outline(crop(hm, 2))

if __name__ == '__main__':
    items = [('rot_crawler', 'ROT CRAWLER (Swarm, T1)', rot_crawler()), ('mind_weaver', 'MIND WEAVER (T3)', mind_weaver()), ('void_weaver', 'VOID WEAVER (T5)', void_weaver())]
    K = 6; pad = 40
    W = sum(im.width * K for _, _, im in items) + pad * (len(items) + 1); H = max(im.height for _, _, im in items) * K + pad * 2
    sheet = Image.new('RGBA', (W, H), (24, 22, 28, 255)); x = pad; d = ImageDraw.Draw(sheet)
    for key, label, im in items:
        im.save(os.path.join(OUT, key + '_concept.png')); big = im.resize((im.width * K, im.height * K), Image.NEAREST)
        sheet.alpha_composite(big, (x, H - big.height - pad)); d.text((x, 10), label, fill=(220, 215, 200, 255)); x += big.width + pad
    sheet.convert('RGB').save(os.path.join(OUT, 'concepts_sheet.png')); print([(k, im.size) for k, _, im in items])
