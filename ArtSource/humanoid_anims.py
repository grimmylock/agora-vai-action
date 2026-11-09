# Posable versions of the four new humanoid mutants (see make_mutants_humanoid.py for the static art and MUTANT_CONCEPTS.md). Each body part is drawn from joint positions, so a frame is the
# SAME art re-drawn with the joints moved (vector shapes -> volume shading), never a stretched or turned bitmap. Neutral pose == the static sprite.
#   python3 humanoid_anims.py <out_dir> [alpha,rot,mind,void]
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
from pxart import *
import anim_fx as FX
import make_mutants_humanoid as H

def rot(p, piv, deg):
    a = math.radians(deg); dx, dy = p[0] - piv[0], p[1] - piv[1]
    return (piv[0] + dx * math.cos(a) - dy * math.sin(a), piv[1] + dx * math.sin(a) + dy * math.cos(a))

def chain(origin, lens, angs):
    """joint points of a limb hanging from `origin`: each segment has a length and an angle in degrees from straight down (+ = toward the front/right)"""
    pts = [origin]; x, y = origin
    for l, a in zip(lens, angs):
        x += l * math.sin(math.radians(a)); y += l * math.cos(math.radians(a)); pts.append((x, y))
    return pts

# ================================================================== ALPHA PROWLER
ALPHA_NEUTRAL = dict(lean=0, dy=0, dx=0, head=(0, 0), jaw=0,
                     arm_n=(28.6, -4.8, 20), arm_f=(-20, 12, 10), leg_n=(41, -41, 23), leg_f=(40, -42, 4), claw=0, flash=False)

def alpha_prowler(cloaked=False, pose=None):
    p = dict(ALPHA_NEUTRAL); p.update(pose or {})
    S = (108, 92); OX, OY = 16 + p["dx"], 16 + p["dy"]
    sk, skf = (H.PALE, H.PALE_FAR) if cloaked else (H.SKIN, H.SKIN_FAR)
    bone_r = H.PALE if cloaked else H.BONE
    piv = (26, 38)
    def T(x, y, up=True):                                                   # neutral sprite coordinates -> this pose (the upper body leans about the pelvis)
        if up and p["lean"]: x, y = rot((x, y), piv, p["lean"])
        return (x + OX, y + OY)
    def Ts(pts, up=True): return [T(x, y, up) for x, y in pts]
    layers = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); layers.append(shade(m, cols, **kw)); return m
    hx, hy = p["head"]
    # legs: hips on the pelvis (not leaning). thigh, shin, foot
    ln = p["leg_n"]; lf = p["leg_f"]
    hipn = (28, 38); hipf = (26, 37)
    def leg(hip, a, width_scale=1.0):
        pts = chain(hip, (10.6, 10.6, 7.6), a); return pts
    pf = leg(hipf, lf); pn = leg(hipn, ln)
    part(lambda m: (m.tube(Ts(pf, False), [7, 5, 3.6, 3]), foot(m, pf)), skf, tex=0.1, seed=1) if False else None
    def foot(m, pts):
        fx, fy = pts[-1]; m.poly(Ts([(fx - 3, fy - 1), (fx + 6, fy - 1), (fx + 6, fy + 1.4), (fx - 3, fy + 1.4)], False))
    part(lambda m: (m.tube(Ts(pf, False), [7, 5, 3.6, 3]), foot(m, pf)), skf, tex=0.1, seed=1, bias=-0.1)
    # far arm
    sh_f = (33, 19); af = chain(sh_f, (11.8, 13.4, 11.4), p["arm_f"])
    part(lambda m: m.tube(Ts(af), [5, 4.4, 3.6, 3]), skf, tex=0.1, seed=2, bias=-0.1)
    # torso (pelvis stays, the rest leans), neck
    def torso(m):
        a = math.degrees(0); lean = p["lean"]
        m.ell(*T(26, 38, False), 7, 6)
        m.tube(Ts([(27, 36), (31, 28), (35, 22)]), [8, 9, 11])
        cx, cy = T(36, 21); m.ell(cx, cy, 9, 7, -15 + lean)
        cx, cy = T(32, 16); m.ell(cx, cy, 6, 5, lean)
        m.tube(Ts([(40, 17), (45, 14)]), [6, 5])
        m.tube([(T(44, 14)[0], T(44, 14)[1]), (T(46, 14)[0] + hx * 0.6, T(46, 14)[1] + hy * 0.6)], [5, 5])
    part(torso, sk, tex=0.14, seed=3, depth=3.4, bias=-0.12)
    # head (follows the neck), jaw hinged at the back of the mouth
    def hT(x, y): X, Y = T(x, y); return (X + hx, Y + hy)
    def hTs(pts): return [hT(x, y) for x, y in pts]
    def skull(m):
        cx, cy = hT(49, 13); m.ell(cx, cy, 6, 5.2, p["lean"] * 0.6)
        m.poly(hTs([(50, 9), (58, 13), (62, 16), (61, 18), (50, 18)]))
    part(skull, sk, tex=0.08, seed=4, depth=2.6, bias=-0.12)
    jaw_open = p["jaw"]; hing = (50, 18)
    def jawpts():
        pts = [(49, 17), (61, 18.5), (58, 21.5), (48, 20)]
        return [rot(q, hing, jaw_open) for q in pts]
    jw = Mask(S); jw.poly(hTs(jawpts())); layers.append(shade(jw, skf if not cloaked else H.PALE_FAR, depth=1.6))
    # near leg, near arm
    part(lambda m: (m.tube(Ts(pn, False), [8, 5.5, 4, 3.4]), foot(m, pn)), sk, tex=0.12, seed=5, bias=-0.12)
    sh_n = (39, 20); an = chain(sh_n, (12.5, 12, 11.7), p["arm_n"])
    part(lambda m: m.tube(Ts(an), [5.6, 4.8, 4, 3.4]), sk, tex=0.12, seed=6, bias=-0.12)
    # claws (drawn as thin lines, lit on top, so they stay claws): three on each hand, three on each foot; spread = p["claw"]
    sp = p["claw"]; claw_lines = []
    for (hxy, ang0, n_l, up) in ((an[-1], p["arm_n"][2], 7, True), (af[-1], p["arm_f"][2], 6, True)):
        for k, l in ((-1, n_l - 1), (0, n_l + 1), (1, n_l - 1)):
            a = math.radians(ang0 + k * (14 + 4 * sp)); x0, y0 = T(hxy[0] + k * 0.8, hxy[1] + 1.5); x1, y1 = T(hxy[0] + k * 0.8 + math.sin(a) * l, hxy[1] + 1.5 + math.cos(a) * l)
            claw_lines.append((x0, y0, x1, y1))
    for ptsf in (pn, pf):
        fx, fy = ptsf[-1]
        for k, l in ((-1, 3), (0, 4), (1, 3)):
            x0, y0 = T(fx + k * 1.2, fy + 1.2, False); x1, y1 = T(fx + k * 1.2 + 3.5 + l * 0.5, fy + 2.2 + k * 0.5, False); claw_lines.append((x0, y0, x1, y1))
    im = paint(S, layers)
    for (x0, y0, x1, y1) in claw_lines:
        line(im, round(x0), round(y0) + 1, round(x1), round(y1) + 1, bone_r[1]); line(im, round(x0), round(y0), round(x1), round(y1), bone_r[3])
    # dorsal spines, ribs, face
    spm = Mask(S)
    for (x, y, l) in ((33, 12, 4), (29, 15, 4.4), (26, 19, 4.6), (24, 25, 4.4), (23, 31, 4)): spm.tube([T(x, y), T(x - l * 0.6, y - l * 0.6)], [1.8, 0.5])
    im.alpha_composite(shade(spm, bone_r, depth=1.2))
    bone_d = bone_r[1] if not cloaked else bone_r[2]
    for k in range(4):
        y = 23 + k * 3; x0 = 30 + (k if k < 3 else 2); x1 = x0 + 6 - (k // 2); (ax, ay), (bx, by) = T(x0, y), T(x1, y - 1); line(im, round(ax), round(ay), round(bx), round(by), bone_d)
    dk = sk[0]; eye, eye2 = rgb("#ff3b2b"), rgb("#ffb0a0")
    px(im, [hT(51, 11), hT(52, 11), hT(53, 11), hT(51, 13), hT(52, 13)], dk); px(im, [hT(52, 12), hT(53, 12)], eye); px(im, [hT(52, 12)], eye2); px(im, [hT(60, 14)], dk)
    # the mouth: a line when shut; when open the inside is dark with teeth on both lips
    tip = (61, 18.5)
    if jaw_open <= 3:
        for x in range(50, 61): px(im, [hT(x, 18)], H.GUM[0])
        for x in (52, 54, 56, 58, 60): px(im, [hT(x, 19)], rgb("#efe9d6"))
        for x in (53, 57): px(im, [hT(x, 17)], rgb("#d9d2bc"))
    else:
        jtip = rot(tip, hing, jaw_open)
        dm = Image.new("RGBA", S, (0, 0, 0, 0)); ImageDraw.Draw(dm).polygon([hT(*hing), hT(61, 18), hT(*jtip)], fill=H.GUM[1]);
        base = im.copy(); base.alpha_composite(dm); im = Image.alpha_composite(dm, im) if False else im
        # draw the inside under the jaw and head: composite the dark wedge, then redraw jaw & head on top
        im2 = Image.new("RGBA", S, (0, 0, 0, 0)); im2.alpha_composite(dm); im2.alpha_composite(im); im = im2
        for k in range(5):
            tx = 52 + k * 2; px(im, [hT(tx, 18.5), hT(tx, 19.5)], rgb("#efe9d6"))                                    # upper fangs
        for k in range(4):
            q = rot((52.5 + k * 2.4, 19.0), hing, jaw_open); px(im, [hT(*q)], rgb("#efe9d6")); px(im, [hT(q[0], q[1] - 1)], rgb("#d9d2bc"))
        for t in (0.25, 0.4, 0.55): q = rot((50 + 10 * t, 19.2), hing, jaw_open); px(im, [hT(*q)], H.GUM[4])
    if p["flash"]:
        fx, fy = hT(60, 20)
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2)): px(im, [(fx + dx, fy + dy)], rgb("#fffbe8"))
    for (x0, y0, x1, y1) in ((42, 21, 38, 26), (47, 14, 45, 17)): (ax, ay), (bx, by) = hT(x0, y0), hT(x1, y1); line(im, round(ax), round(ay), round(bx), round(by), bone_d)
    return outline(im)

def alpha_poses():
    N = ALPHA_NEUTRAL
    run = []
    for i in range(8):
        ph = 2 * math.pi * i / 8; r = math.cos(ph); rf = math.cos(ph - 0.7); lg = math.cos(ph - 2.0); lgf = math.cos(ph - 2.0 - 0.7)
        run.append(dict(lean=30 + 5 * math.cos(2 * ph), dy=-int(round(3 * (math.cos(2 * ph) + 1) / 2)) + 2, head=(3, 2), jaw=0,
                        arm_n=(26 + 50 * r, 4 + 36 * r, 14 + 22 * r), arm_f=(26 + 50 * rf, 4 + 36 * rf, 14 + 22 * rf),
                        leg_n=(38 - 40 * lg, -38 + 26 * lg - 6, 24), leg_f=(38 - 40 * lgf, -38 + 26 * lgf - 6, 24)))
    bite = [dict(), dict(lean=8, head=(-2, 1), jaw=10, arm_n=(20, -10, 14), leg_n=(46, -46, 20)),
            dict(lean=6, head=(-3, 0), jaw=26, arm_n=(12, -18, 10), arm_f=(-6, 4, 6), leg_n=(48, -48, 20)),
            dict(lean=16, head=(5, 3), dx=3, jaw=40, arm_n=(55, 30, 30), arm_f=(30, 20, 22), claw=3, leg_n=(38, -34, 22)),
            dict(lean=22, head=(9, 5), dx=5, jaw=44, arm_n=(70, 46, 34), arm_f=(40, 28, 26), claw=4, leg_n=(30, -26, 22)),
            dict(lean=22, head=(9, 5), dx=5, jaw=0, flash=True, arm_n=(70, 46, 34), arm_f=(40, 28, 26), claw=4, leg_n=(30, -26, 22)),
            dict(lean=20, head=(8, 5), dx=5, jaw=3, arm_n=(66, 44, 34), arm_f=(38, 26, 26), claw=2, leg_n=(30, -26, 22)),
            dict(lean=20, head=(8, 6), dx=5, jaw=4, arm_n=(66, 44, 34), arm_f=(38, 26, 26), claw=2, leg_n=(30, -26, 22)),
            dict(lean=10, head=(3, 2), dx=2, jaw=14, arm_n=(40, 10, 24)), dict()]
    bite_ms = [90, 70, 70, 50, 50, 80, 90, 90, 80, 80]
    pounce = [dict(lean=24, dy=3, head=(-1, 2), arm_n=(40, 30, 28), arm_f=(30, 22, 24), leg_n=(70, -66, 20), leg_f=(66, -60, 20)),
              dict(lean=30, dy=5, head=(0, 3), jaw=8, arm_n=(48, 38, 30), arm_f=(40, 32, 26), leg_n=(78, -74, 20), leg_f=(74, -70, 20)),
              dict(lean=14, dy=-3, dx=1, head=(3, 0), jaw=24, arm_n=(78, 60, 40), arm_f=(66, 50, 34), claw=4, leg_n=(-18, -50, 40), leg_f=(-12, -46, 36)),
              dict(lean=6, dy=-8, dx=2, head=(5, -1), jaw=36, arm_n=(98, 82, 48), arm_f=(88, 72, 42), claw=5, leg_n=(-34, -58, 44), leg_f=(-28, -52, 40)),
              dict(lean=2, dy=-12, dx=3, head=(7, -2), jaw=44, arm_n=(110, 96, 54), arm_f=(100, 86, 48), claw=6, leg_n=(-42, -64, 46), leg_f=(-36, -58, 42)),
              dict(lean=14, dy=-8, dx=4, head=(8, 2), jaw=46, arm_n=(100, 84, 50), arm_f=(92, 76, 44), claw=6, leg_n=(-30, -56, 44), leg_f=(-24, -50, 40)),
              dict(lean=22, dy=-2, dx=5, head=(9, 4), jaw=46, arm_n=(84, 62, 40), arm_f=(74, 54, 36), claw=5, leg_n=(8, -36, 30), leg_f=(14, -30, 28)),
              dict(lean=22, dx=5, head=(9, 5), jaw=0, flash=True, arm_n=(72, 50, 34), arm_f=(62, 44, 30), claw=4, leg_n=(30, -26, 22)),
              dict(lean=20, dx=5, head=(8, 5), jaw=3, arm_n=(66, 44, 34), arm_f=(56, 38, 28), claw=2, leg_n=(30, -26, 22)),
              dict(lean=20, dx=5, head=(8, 6), jaw=4, arm_n=(66, 44, 34), arm_f=(56, 38, 28), claw=2, leg_n=(30, -26, 22)),
              dict(lean=10, dx=2, head=(3, 2), jaw=14, arm_n=(40, 10, 24)), dict()]
    pounce_ms = [110, 100, 55, 55, 65, 55, 55, 80, 100, 100, 80, 80]
    pounce_tr = [0, 0, .12, .36, .62, .86, 1, 1, 1, 1, 1, 1]
    return dict(run=(run, [42] * 8, [0] * 8), bite=(bite, bite_ms, [0] * 10), pounce=(pounce, pounce_ms, pounce_tr))

# ================================================================== ROT CRAWLER
ROT_NEUTRAL = dict(lean=0, dy=0, dx=0, head=(0, 0), jaw=0, arm_n=(16.7, 26.6, 45), arm_f=(-6, 24, 40), leg_n=(37.9, -69, -98), leg_f=(36, -66, -95), flash=False)

def rot_crawler(pose=None):
    p = dict(ROT_NEUTRAL); p.update(pose or {})
    S = (88, 72); OX, OY = 14 + p["dx"], 14 + p["dy"]
    piv = (13, 24)
    def T(x, y, up=True):
        if up and p["lean"]: x, y = rot((x, y), piv, p["lean"])
        return (x + OX, y + OY)
    def Ts(pts, up=True): return [T(x, y, up) for x, y in pts]
    hx, hy = p["head"]
    def hT(x, y): X, Y = T(x, y); return (X + hx, Y + hy)
    def hTs(pts): return [hT(x, y) for x, y in pts]
    layers = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); layers.append(shade(m, cols, **kw)); return m
    FLESH, FLESH_FAR, FUNGUS, PUS = H.FLESH, H.FLESH_FAR, H.FUNGUS, H.PUS
    af = chain((33, 21), (10.2, 6.7, 4.2), p["arm_f"]); lf = chain((12, 25), (10, 8.5, 7.1), p["leg_f"])
    an = chain((36, 19), (10.4, 6.7, 4.2), p["arm_n"]); ln = chain((14, 25), (11.4, 8.5, 7.1), p["leg_n"])
    part(lambda m: (m.tube(Ts(af), [4.6, 3.8, 3.2, 2.8]), m.tube(Ts(lf, False), [6, 4.6, 3.4, 3])), FLESH_FAR, tex=0.1, seed=1, bias=-0.1)
    def trunk(m):
        m.ell(*T(13, 24, False), 6.5, 6); m.tube(Ts([(15, 22), (24, 21), (33, 18)]), [9, 10, 11])
        cx, cy = T(24, 27); m.ell(cx, cy, 8.5, 5.5, p["lean"]); cx, cy = T(34, 18); m.ell(cx, cy, 7.5, 6.5, p["lean"])
    part(trunk, FLESH, tex=0.2, seed=2, depth=4, bias=-0.08)
    def headm(m):
        m.tube([hT(38, 18), hT(42, 22)], [6, 5.4]); cx, cy = hT(44, 23); m.ell(cx, cy, 5.4, 5.2)
    part(headm, FLESH, tex=0.12, seed=3, bias=-0.05)
    jaw_open = p["jaw"]; hing = (43, 26)
    jm = Mask(S); jm.poly(hTs([rot(q, hing, jaw_open) for q in ((42, 26), (49, 27), (48, 32), (43, 30))])); layers.append(shade(jm, FLESH, depth=1.6, bias=-0.05))
    part(lambda m: m.tube(Ts(ln, False), [7.4, 5.2, 3.8, 3.2]), FLESH, tex=0.12, seed=4, bias=0.06)
    part(lambda m: m.tube(Ts(an), [5.8, 4.4, 3.6, 3.2]), FLESH, tex=0.12, seed=5, bias=0.06)
    # fingers and toes drawn as short lines
    fl = []
    for hxy, ang0, sign in ((an[-1], p["arm_n"][2], 1), (af[-1], p["arm_f"][2], 1)):
        for k in (-1, 0, 1):
            a = math.radians(ang0 + k * 18 + 60); fl.append((T(hxy[0], hxy[1]), T(hxy[0] + math.sin(a) * 5, hxy[1] + math.cos(a) * 5 + 0.5)))
    for pts in (ln, lf):
        fx, fy = pts[-1]
        for k in (-1, 0, 1):
            a = math.radians(-100 + k * 20); fl.append((T(fx, fy, False), T(fx + math.sin(a) * 4.5, fy + math.cos(a) * 4.5 + 0.3, False)))
    for (cx, cy, r) in ((11, 15, 2.8), (19, 11, 3.6), (27, 10, 3.2), (34, 9, 2.6)):
        X, Y = T(cx, cy); X0, Y0 = T(cx + 0.3, cy + r + 4)
        st = Mask(S); st.tube([(X, Y + r), (X0, Y0)], [2, 2.4]); layers.append(shade(st, FUNGUS, depth=1.0, bias=-0.2))
        cap = Mask(S); cap.ell(X, Y, r, r * 0.7, p["lean"] * 0.5); layers.append(shade(cap, FUNGUS, depth=r, tex=0.12, seed=int(cx)))
    pat = Mask(S); pat.ell(*T(36, 15), 4, 2.4); pat.ell(*T(22, 29), 3.4, 2.2); layers.append(shade(pat, FUNGUS, depth=1.6, tex=0.3, seed=9, bias=-0.15))
    im = paint(S, layers)
    for (a, b) in fl: line(im, round(a[0]), round(a[1]), round(b[0]), round(b[1]), FLESH[3])
    dk = FLESH[0]
    px(im, [hT(44, 21), hT(45, 21), hT(46, 21), hT(44, 22), hT(46, 22), hT(45, 22)], dk); px(im, [hT(45, 22)], rgb("#e8e0a0"))
    px(im, [hT(48, 22), hT(49, 22)], dk); px(im, [hT(49, 22)], rgb("#e8e0a0"))
    if jaw_open <= 4:
        for x in range(44, 50): px(im, [hT(x, 27)], rgb("#2a0f10"))
        for x in (45, 47, 49): px(im, [hT(x, 28)], rgb("#e9e2c4"))
    else:
        dm = Image.new("RGBA", S, (0, 0, 0, 0)); jt = rot((49, 27), hing, jaw_open); ImageDraw.Draw(dm).polygon([hT(*hing), hT(50, 26.5), hT(*jt)], fill=rgb("#2a0f10"))
        im2 = Image.new("RGBA", S, (0, 0, 0, 0)); im2.alpha_composite(dm); im2.alpha_composite(im); im = im2
        for x in (45, 47, 49): px(im, [hT(x, 27.5), hT(x, 28.5)], rgb("#e9e2c4"))
        for k in range(3): q = rot((45 + k * 2, 27), hing, jaw_open); px(im, [hT(*q)], rgb("#e9e2c4"))
    if p["flash"]:
        fx, fy = hT(50, 29)
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0)): px(im, [(fx + dx, fy + dy)], rgb("#fffbe8"))
    for k in range(4): a, b = hT(41 - k, 18 + k), hT(39 - k, 22 + k); line(im, round(a[0]), round(a[1]), round(b[0]), round(b[1]), dk)
    for (x, y) in ((20, 24), (28, 25), (31, 18), (13, 22)):
        X, Y = T(x, y); px(im, [(round(X), round(Y)), (round(X) + 1, round(Y))], dk); px(im, [(round(X) - 1, round(Y)), (round(X) + 2, round(Y)), (round(X), round(Y) - 1), (round(X) + 1, round(Y) - 1)], PUS[2])
    return outline(im)

def rot_poses():
    run = []
    for i in range(8):
        ph = 2 * math.pi * i / 8; r = math.cos(ph); rf = math.cos(ph + math.pi); lg = math.cos(ph + math.pi * 0.5); lgf = math.cos(ph + math.pi * 1.5)
        run.append(dict(lean=6 + 3 * math.cos(2 * ph), dy=-int(round(2 * (math.cos(2 * ph) + 1) / 2)) + 1, head=(1, 1),
                        arm_n=(16 + 40 * r, 26 + 20 * r, 45 + 10 * r), arm_f=(16 + 40 * rf, 26 + 20 * rf, 45 + 10 * rf),
                        leg_n=(38 + 26 * lg, -69 + 14 * lg, -98), leg_f=(38 + 26 * lgf, -69 + 14 * lgf, -98)))
    bite = [dict(), dict(lean=-4, head=(-2, 1), jaw=12, arm_n=(4, 22, 40), leg_n=(46, -74, -98)),
            dict(lean=-6, head=(-3, 0), jaw=34, arm_n=(0, 20, 40), arm_f=(-14, 18, 36), leg_n=(48, -76, -98)),
            dict(lean=10, head=(6, 3), dx=3, jaw=50, arm_n=(40, 30, 50), arm_f=(30, 28, 46), leg_n=(30, -60, -98)),
            dict(lean=14, head=(10, 5), dx=5, jaw=54, arm_n=(52, 38, 54), arm_f=(40, 32, 48), leg_n=(24, -54, -98)),
            dict(lean=14, head=(10, 5), dx=5, jaw=0, flash=True, arm_n=(52, 38, 54), arm_f=(40, 32, 48), leg_n=(24, -54, -98)),
            dict(lean=12, head=(9, 5), dx=5, jaw=3, arm_n=(50, 36, 52), arm_f=(38, 30, 46), leg_n=(24, -54, -98)),
            dict(lean=12, head=(9, 6), dx=5, jaw=4, arm_n=(50, 36, 52), arm_f=(38, 30, 46), leg_n=(24, -54, -98)),
            dict(lean=6, head=(3, 2), dx=2, jaw=14, arm_n=(30, 28, 46)), dict()]
    pounce = [dict(lean=-8, dy=4, head=(-1, 3), arm_n=(-10, 40, 60), arm_f=(-20, 36, 56), leg_n=(70, -90, -110), leg_f=(66, -86, -106)),
              dict(lean=-12, dy=6, head=(-1, 4), jaw=8, arm_n=(-16, 44, 64), arm_f=(-26, 40, 60), leg_n=(78, -96, -116), leg_f=(74, -92, -112)),
              dict(lean=8, dy=-4, dx=1, head=(4, 0), jaw=26, arm_n=(70, 50, 50), arm_f=(60, 44, 46), leg_n=(-30, -60, -80), leg_f=(-24, -56, -76)),
              dict(lean=10, dy=-9, dx=2, head=(6, -1), jaw=40, arm_n=(92, 66, 54), arm_f=(84, 60, 50), leg_n=(-46, -70, -90), leg_f=(-40, -66, -86)),
              dict(lean=8, dy=-12, dx=3, head=(8, -2), jaw=50, arm_n=(104, 78, 58), arm_f=(96, 72, 54), leg_n=(-54, -76, -96), leg_f=(-48, -72, -92)),
              dict(lean=14, dy=-8, dx=4, head=(9, 2), jaw=54, arm_n=(92, 64, 54), arm_f=(84, 58, 50), leg_n=(-36, -66, -88), leg_f=(-30, -62, -84)),
              dict(lean=16, dy=-2, dx=5, head=(10, 4), jaw=54, arm_n=(70, 50, 54), arm_f=(60, 44, 48), leg_n=(10, -50, -90)),
              dict(lean=14, dx=5, head=(10, 5), jaw=0, flash=True, arm_n=(52, 38, 54), arm_f=(40, 32, 48), leg_n=(24, -54, -98)),
              dict(lean=12, dx=5, head=(9, 5), jaw=3, arm_n=(50, 36, 52), arm_f=(38, 30, 46), leg_n=(24, -54, -98)),
              dict(lean=12, dx=5, head=(9, 6), jaw=4, arm_n=(50, 36, 52), arm_f=(38, 30, 46), leg_n=(24, -54, -98)),
              dict(lean=6, dx=2, head=(3, 2), jaw=14, arm_n=(30, 28, 46)), dict()]
    return dict(run=(run, [70] * 8, [0] * 8), bite=(bite, [90, 70, 70, 50, 50, 80, 90, 90, 80, 80], [0] * 10),
                pounce=(pounce, [110, 100, 55, 55, 65, 55, 55, 80, 100, 100, 80, 80], [0, 0, .12, .36, .62, .86, 1, 1, 1, 1, 1, 1]))

# ================================================================== MIND WEAVER
MIND_NEUTRAL = dict(dy=0, dx=0, tilt=0, arm_n=(52, 139, 166), arm_f=(-56, -135, -166), sag=13, glow=0.0, flare=0.0, sway=0.0, brain=0.0, ring=0.0, beam=0.0, thread=True)

def mind_weaver(pose=None):
    p = dict(MIND_NEUTRAL); p.update(pose or {})
    S = (112, 96); OX, OY = 22 + p["dx"], 8 + p["dy"]
    piv = (34, 44)
    def T(x, y):
        if p["tilt"]: x, y = rot((x, y), piv, p["tilt"])
        return (x + OX, y + OY)
    def Ts(pts): return [T(x, y) for x, y in pts]
    layers = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); layers.append(shade(m, cols, **kw)); return m
    sw = p["sway"]
    tat = [(20, 70), (25, 63), (28, 76), (33, 64), (37, 74), (40, 62)]
    shroud = [(24, 34), (44, 34), (49, 52), (45, 66), (tat[5][0] + sw * 2, tat[5][1]), (tat[4][0] + sw * 3, tat[4][1]), (tat[3][0] + sw * 2, tat[3][1]), (tat[2][0] + sw * 3, tat[2][1]), (tat[1][0] + sw * 2, tat[1][1]), (tat[0][0] + sw * 3, tat[0][1]), (20, 52)]
    part(lambda m: m.poly(Ts(shroud)), H.SHROUD, tex=0.22, seed=2, depth=5, bias=-0.1)
    af = chain((26, 38), (10.8, 8.5, 8.2), [180 - a if False else a for a in p["arm_f"]]) if False else None
    def arm(sh, angs, lens):
        pts = [sh]; x, y = sh
        for l, a in zip(lens, angs): x += l * math.cos(math.radians(a - 90)); y += l * math.sin(math.radians(a - 90)) * -1 * -1; pts.append((x, y))
        return pts
    def arm2(sh, angs, lens):                                              # angles measured from straight UP, + = clockwise (toward the right)
        pts = [sh]; x, y = sh
        for l, a in zip(lens, angs): x += l * math.sin(math.radians(a)); y -= l * math.cos(math.radians(a)); pts.append((x, y))
        return pts
    # convert neutral (angles from straight down in the static art) -> from straight up: a_up = 180 - a_down
    pf = arm2((26, 38), [180 - a for a in p["arm_f"]], (10.8, 8.5, 8.2)); pn = arm2((43, 37), [180 - a for a in p["arm_n"]], (11.4, 9.2, 8.2))
    part(lambda m: m.tube(Ts(pf), [4, 3, 2.6, 2.4]), H.SHROUD_FAR, tex=0.1, seed=3, bias=-0.05)
    part(lambda m: m.tube(Ts(pn), [4.4, 3.2, 2.8, 2.6]), H.MIND_SKIN, tex=0.1, seed=4, bias=-0.1)
    part(lambda m: (m.ell(*T(34, 40), 7.5, 7), m.tube(Ts([(34, 33), (35, 27)]), [5, 4.4])), H.MIND_SKIN, tex=0.14, seed=5, bias=-0.12, depth=3.5)
    part(lambda m: (m.ell(*T(35, 17), 14, 13, p["tilt"]), m.ell(*T(40, 23), 9, 8, p["tilt"])), H.MIND_SKIN, tex=0.1, seed=6, depth=6, bias=-0.1)
    br = Mask(S); br.ell(*T(35, 11), 12.5, 8.5, p["tilt"]); br.ell(*T(30, 14), 6.5, 5, p["tilt"]); layers.append(shade(br, H.BRAIN, tex=0.22, seed=7, depth=4, bias=-0.04 + 0.22 * p["brain"]))
    im = paint(S, layers)
    import random; rnd = random.Random(5)
    for (x0, y0, x1, y1) in ((26, 8, 32, 12), (31, 6, 38, 9), (37, 7, 44, 10), (25, 13, 31, 15), (33, 11, 41, 13), (30, 15, 36, 17)):
        n = int(max(abs(x1 - x0), 1) * 2)
        for i in range(n + 1):
            u = i / n; x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u + 1.2 * math.sin(u * 9 + rnd.random()); X, Y = T(x, y)
            px(im, [(round(X), round(Y))], H.BRAIN[0]); px(im, [(round(X), round(Y) - 1)], H.BRAIN[4])
    big = 3 + (1 if p["flare"] > 0.5 else 0)
    for (ex, ey) in ((38, 21), (44, 23)):
        X, Y = T(ex, ey); X, Y = round(X), round(Y)
        for dx in range(big):
            for dy in range(big): px(im, [(X + dx, Y + dy)], rgb("#fbf3ff"))
        px(im, [(X + 1, Y + 1)], rgb("#8a3cff")); px(im, [(X - 1, Y), (X - 1, Y + 1), (X + big, Y + 1), (X + big, Y + 2)], H.MIND_SKIN[0])
        if p["flare"] > 0:
            for k in range(1, 1 + int(3 * p["flare"])): px(im, [(X - k, Y + 1), (X + big - 1 + k, Y + 1), (X + 1, Y - k)], rgb("#e6d0ff"))
    X, Y = T(41, 14); X, Y = round(X), round(Y); px(im, [(X, Y), (X + 1, Y), (X, Y + 1), (X + 1, Y + 1)], rgb("#e6d0ff")); px(im, [(X, Y)], rgb("#8a3cff"))
    for x in range(40, 47): X, Y = T(x, 29); px(im, [(round(X), round(Y))], H.MIND_SKIN[0])
    # fingers: four long lines at the end of each arm, along the last segment
    for pts, angs in ((pn, [180 - a for a in p["arm_n"]]), (pf, [180 - a for a in p["arm_f"]])):
        hx, hy = pts[-1]; a0 = angs[-1]
        for k, l in ((-2.6, 7), (-0.9, 9), (0.9, 9), (2.6, 7)):
            a = math.radians(a0 + k * 7); x1, y1 = hx + math.sin(a) * l, hy - math.cos(a) * l
            (ax, ay), (bx, by) = T(hx + k * 0.4, hy), T(x1, y1); line(im, round(ax), round(ay), round(bx), round(by), H.MIND_SKIN[4]); line(im, round(ax), round(ay) + 1, round(bx), round(by) + 1, H.MIND_SKIN[1])
    # the thread between the hands (sag p["sag"]), brightened by p["glow"]; motes
    if p["thread"]:
        (x0, y0), (x1, y1) = pf[-1], pn[-1]; n = int(max(abs(x1 - x0), 1) * 1.5)
        for i in range(n + 1):
            u = i / n; x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u - p["sag"] * math.sin(math.pi * u) + 1.4 * math.sin(i * 0.7); X, Y = T(x, y)
            px(im, [(round(X), round(Y))], H.GLOW[4] if (i % 3 == 0 or p["glow"] > 0.6) else H.GLOW[3]); px(im, [(round(X), round(Y) + 1)], H.GLOW[1])
            if p["glow"] > 0.3: px(im, [(round(X), round(Y) - 1)], H.GLOW[2])
    for (x, y) in ((24, 8), (50, 12), (16, 14), (56, 20), (30, 3)): X, Y = T(x, y + 1.5 * math.sin(p["sway"] * 3 + x)); px(im, [(round(X), round(Y))], H.GLOW[3]); px(im, [(round(X) + 1, round(Y))], H.GLOW[1])
    # the blast: rings spreading to the right from the hands and a beam
    if p["ring"] > 0 or p["beam"] > 0:
        cx, cy = T(*pn[-1]); d = ImageDraw.Draw(im)
        for r0 in (0.0, 0.5):
            rr = (p["ring"] + r0) % 1.0 * 26 if p["ring"] > 0 else 0
            if rr > 1: d.arc([cx - rr * 0.6, cy - rr, cx + rr * 0.6, cy + rr], -75, 75, fill=H.GLOW[3], width=1)
            if rr > 4: d.arc([cx - rr * 0.6 + 2, cy - rr + 2, cx + rr * 0.6 - 2, cy + rr - 2], -75, 75, fill=H.GLOW[1], width=1)
        if p["beam"] > 0:
            L = int(p["beam"] * 40)
            for k in range(L): X = cx + 2 + k; Y = cy + 1.5 * math.sin(k * 0.5); px(im, [(round(X), round(Y))], H.GLOW[4]); px(im, [(round(X), round(Y) - 1), (round(X), round(Y) + 1)], H.GLOW[2])
    return outline(im)

def mind_poses():
    idle = []
    for i in range(8):
        ph = 2 * math.pi * i / 8
        idle.append(dict(dy=int(round(2 * math.sin(ph))), sway=math.sin(ph), arm_n=(52 + 5 * math.sin(ph), 139, 166), arm_f=(-56 - 5 * math.sin(ph), -135, -166), sag=13 + 1.5 * math.sin(ph), tilt=0.8 * math.sin(ph)))
    # cast (the psi blast): the hands draw apart, the thread goes taut and white, the eyes flare, then both hands thrust forward and a ring/beam goes out
    cast = [dict(), dict(dy=-1, sway=0.5, arm_n=(60, 130, 160), arm_f=(-62, -128, -160), sag=7, glow=0.2, brain=0.1),
            dict(dy=-2, sway=0.8, arm_n=(78, 120, 150), arm_f=(-76, -118, -150), sag=4, glow=0.5, flare=0.4, brain=0.3),
            dict(dy=-3, sway=1, arm_n=(98, 108, 140), arm_f=(-96, -104, -140), sag=1.5, glow=0.8, flare=0.8, brain=0.5),
            dict(dy=-3, sway=1, arm_n=(110, 100, 134), arm_f=(-108, -98, -134), sag=0.5, glow=1, flare=1, brain=0.7, tilt=-3),
            dict(dy=-2, dx=2, tilt=4, sway=0.6, arm_n=(150, 150, 150), arm_f=(60, 70, 80), sag=0, glow=1, flare=1, brain=1.0, ring=0.05, beam=0.25, thread=False),
            dict(dy=-1, dx=3, tilt=6, sway=0.3, arm_n=(150, 150, 150), arm_f=(60, 70, 80), glow=1, flare=1, brain=0.8, ring=0.25, beam=0.7, thread=False),
            dict(dx=3, tilt=6, arm_n=(146, 146, 146), arm_f=(66, 74, 82), glow=0.7, flare=0.6, brain=0.5, ring=0.5, beam=1.0, thread=False),
            dict(dx=2, tilt=3, arm_n=(120, 130, 140), arm_f=(30, 50, 70), glow=0.4, flare=0.2, brain=0.2, ring=0.8, beam=0.5, thread=False),
            dict(dx=1, arm_n=(80, 120, 150), arm_f=(-40, -100, -140), sag=9, glow=0.2, thread=True), dict(dy=1, arm_n=(56, 136, 162), arm_f=(-56, -132, -160), sag=12), dict()]
    return dict(run=(idle, [90] * 8, [0] * 8), bite=(cast, [100, 80, 80, 80, 70, 60, 60, 60, 80, 90, 90][:len(cast)], [0] * len(cast)), pounce=(cast, [100] * len(cast), [0] * len(cast)))

# ================================================================== VOID WEAVER
VOID_NEUTRAL = dict(dy=0, dx=0, sway=0.0, arm_n=(40, 14), arm_f=(-40, -16), hole=1.0, crack=0.0, motes=0.0, pulse=0.0, stream=0.0)

def void_weaver(cloaked=False, pose=None):
    p = dict(VOID_NEUTRAL); p.update(pose or {})
    S = (116, 100); OX, OY = 16 + p["dx"], 6 + p["dy"]
    def T(x, y): return (x + OX, y + OY)
    def Ts(pts): return [T(x, y) for x, y in pts]
    cl = H.VOID_CLOTH_PALE if cloaked else H.VOID_CLOTH
    layers = []
    def part(build, cols, **kw):
        m = Mask(S); build(m); layers.append(shade(m, cols, **kw)); return m
    sw = p["sway"]
    hem = [(56, 70), (46, 76), (38, 70), (30, 77), (20, 72), (16, 70)]
    robe = [(26, 28), (46, 28), (52, 48), (56 + sw, 70)] + [(x + sw * (1 + 0.4 * i), y) for i, (x, y) in enumerate(hem[1:])] + [(22, 48)]
    af = chain((24, 36), (15.6, 14.6), p["arm_f"]); an = chain((48, 34), (15.6, 16.5), p["arm_n"])
    part(lambda m: m.tube(Ts(af), [7, 6, 4]), cl, tex=0.2, seed=2, bias=-0.15)
    part(lambda m: m.poly(Ts(robe)), cl, tex=0.25, seed=3, depth=7, bias=-0.1)
    hood = Mask(S); hood.ell(*T(37, 20), 11, 14); hood.poly(Ts([(27, 24), (47, 24), (50, 36), (24, 36)])); layers.append(shade(hood, cl, tex=0.25, seed=4, depth=6, bias=-0.08))
    part(lambda m: m.tube(Ts(an), [8, 6.4, 4.4]), cl, tex=0.2, seed=5, bias=-0.1)
    im = paint(S, layers)
    fm = Mask(S); fm.ell(*T(38, 21), 6.4 * p["hole"], 9 * (0.9 + 0.1 * p["hole"])); face = fm.bits()
    for y in range(S[1]):
        for x in range(S[0]):
            if face[y, x]: im.putpixel((x, y), rgb("#020106"))
    X, Y = T(39, 20); px(im, [(round(X), round(Y)), (round(X), round(Y) + 1)], rgb("#f4eeff")); px(im, [(round(X) + 1, round(Y))], H.RIFT[1])
    if p["pulse"] > 0.3:
        for k in range(int(3 * p["pulse"])): px(im, [(round(X) - 1 - k, round(Y)), (round(X) + 2 + k, round(Y)), (round(X), round(Y) - 1 - k), (round(X), round(Y) + 2 + k)], H.RIFT[3])
    for (x0, y0, x1, y1, dark) in ((30, 36, 28, 70, True), (36, 38, 36, 74, False), (42, 36, 45, 72, True), (33, 40, 32, 68, False), (39, 42, 41, 70, False)):
        n = int(max(abs(y1 - y0), 1))
        for i in range(n + 1):
            u = i / n; x, y = T(x0 + (x1 - x0) * u + math.sin(u * 5 + x0) * 0.8 + sw * u, y0 + (y1 - y0) * u); x, y = round(x), round(y)
            if 0 <= y < S[1] and im.getpixel((x, y))[3] and not (face[y, x] if 0 <= y < face.shape[0] and 0 <= x < face.shape[1] else False): px(im, [(x, y)], cl[0] if dark else cl[3])
    cr = p["crack"]
    for pts in (((37, 38), (39, 46), (36, 54), (38, 62), (36, 70)), ((44, 40), (46, 48), (43, 56), (45, 64)), ((30, 42), (29, 50), (31, 58)), ((38, 50), (43, 52), (46, 48))):
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            n = int(max(abs(x1 - x0), abs(y1 - y0), 1))
            for i in range(n + 1):
                x, y = T(x0 + (x1 - x0) * i / n + sw * (y0 - 38) / 32, y0 + (y1 - y0) * i / n); x, y = round(x), round(y)
                if im.getpixel((x, y))[3]:
                    px(im, [(x, y)], H.RIFT[2] if cr < 0.5 else H.RIFT[3]); px(im, [(x + 1, y)], H.RIFT[0] if cr < 0.5 else H.RIFT[2])
                    if i % 4 == 0 or cr > 0.7: px(im, [(x, y)], H.RIFT[3] if cr < 0.8 else H.RIFT[4])
                    if cr > 0.4: px(im, [(x - 1, y)], H.RIFT[1])
    for pts, ang0 in ((an, p["arm_n"][-1]), (af, p["arm_f"][-1])):
        hx, hy = pts[-1]
        for k, l in ((-3, 9), (-1, 11), (1, 11), (3, 9)):
            a = math.radians(ang0 + k * 7); (ax, ay), (bx, by) = T(hx + k * 0.4, hy), T(hx + math.sin(a) * l, hy + math.cos(a) * l)
            line(im, round(ax), round(ay), round(bx), round(by), H.HAND[3]); line(im, round(ax), round(ay) + 1, round(bx), round(by) + 1, H.HAND[1])
    import random; rnd = random.Random(11)
    for i in range(34):
        x = 16 + rnd.randint(0, 40); y = 76 + rnd.randint(0, 14) - int(p["motes"] * 10 * ((i % 5) / 4.0))
        X, Y = T(x, y)
        if p["stream"] > 0: X += p["stream"] * (4 + (i % 7) * 4)
        if rnd.random() < 0.55: px(im, [(round(X), round(Y))], H.RIFT[0] if i % 3 else H.RIFT[2])
        if i % 5 == 0: px(im, [(round(X) + 1, round(Y)), (round(X), round(Y) + 1)], H.RIFT[1])
    if p["pulse"] > 0:
        cx, cy = T(38, 21); d = ImageDraw.Draw(im); rr = 6 + p["pulse"] * 30
        d.arc([cx - rr, cy - rr, cx + rr, cy + rr], -60, 60, fill=H.RIFT[3], width=1)
        if rr > 8: d.arc([cx - rr + 3, cy - rr + 3, cx + rr - 3, cy + rr - 3], -60, 60, fill=H.RIFT[1], width=1)
    if p["stream"] > 0:
        cx, cy = T(44, 21)
        for k in range(int(p["stream"] * 46)): X = cx + 6 + k; Y = cy + 1.8 * math.sin(k * 0.45) ; px(im, [(round(X), round(Y))], H.RIFT[4]); px(im, [(round(X), round(Y) - 1), (round(X), round(Y) + 1)], H.RIFT[1])
    return outline(im)

def void_poses():
    idle = []
    for i in range(8):
        ph = 2 * math.pi * i / 8
        idle.append(dict(dy=int(round(2 * math.sin(ph))), sway=1.5 * math.sin(ph), arm_n=(40 + 4 * math.sin(ph), 14), arm_f=(-40 - 4 * math.sin(ph), -16), motes=0.3 + 0.3 * math.sin(ph), crack=0.1 + 0.1 * math.sin(ph)))
    # overload: the hands rise and open, the hole widens, the cracks blaze white, a pulse and a stream of motes go out toward the target (right)
    cast = [dict(), dict(dy=-1, arm_n=(46, 20), arm_f=(-46, -22), hole=1.05, crack=0.3, motes=0.4),
            dict(dy=-2, arm_n=(70, 50), arm_f=(-70, -52), hole=1.2, crack=0.6, motes=0.7, pulse=0.2),
            dict(dy=-3, arm_n=(100, 90), arm_f=(-100, -92), hole=1.4, crack=0.9, motes=1.0, pulse=0.35),
            dict(dy=-4, arm_n=(120, 118), arm_f=(-110, -110), hole=1.55, crack=1.0, motes=1.0, pulse=0.5, stream=0.2),
            dict(dy=-4, dx=2, arm_n=(130, 126), arm_f=(-110, -110), hole=1.6, crack=1.0, pulse=0.7, stream=0.6, motes=1.0),
            dict(dy=-3, dx=3, arm_n=(130, 126), arm_f=(-100, -100), hole=1.5, crack=0.9, pulse=0.9, stream=1.0, motes=0.8),
            dict(dy=-2, dx=3, arm_n=(110, 100), arm_f=(-80, -80), hole=1.3, crack=0.6, pulse=1.0, stream=1.0, motes=0.5),
            dict(dy=-1, dx=2, arm_n=(80, 60), arm_f=(-60, -50), hole=1.15, crack=0.4, stream=0.5, motes=0.3),
            dict(dx=1, arm_n=(52, 28), arm_f=(-48, -26), hole=1.05, crack=0.2), dict()]
    return dict(run=(idle, [100] * 8, [0] * 8), bite=(cast, [110, 90, 80, 80, 70, 60, 60, 60, 80, 90], [0] * len(cast)), pounce=(cast, [110] * len(cast), [0] * len(cast)))

def render(creature, cloaked=False):
    if creature == "alpha": poses = alpha_poses(); fn = lambda q: alpha_prowler(cloaked, q)
    elif creature == "rot": poses = rot_poses(); fn = lambda q: rot_crawler(q)
    elif creature == "mind": poses = mind_poses(); fn = lambda q: mind_weaver(q)
    elif creature == "void": poses = void_poses(); fn = lambda q: void_weaver(cloaked, q)
    out = {}
    for name, (ps, ms, tr) in poses.items(): out[name] = ([fn(q) for q in ps], ms, tr)
    return out

def contact(res, path, sc=6, cols=6):
    allf = [(n, f) for n, (fr, ms, tr) in res.items() if n != "run" for f in fr] + [("run", f) for f in res["run"][0]]
    w, h = allf[0][1].size; rows = (len(allf) + cols - 1) // cols
    sheet = Image.new("RGBA", (w * sc * cols, h * sc * rows), (42, 38, 32, 255))
    for i, (n, f) in enumerate(allf): sheet.alpha_composite(f.resize((w * sc, h * sc), Image.NEAREST), ((i % cols) * w * sc, (i // cols) * h * sc))
    sheet.save(path)

def human():
    im = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA").crop((0, 0, 16, 16))
    return im.resize((32, 32), Image.NEAREST)

def idx_at(ms, t):
    total = sum(ms); t = t % (total + 400); acc = 0
    for i, d in enumerate(ms):
        if t < acc + d: return i
        acc += d
    return len(ms) - 1

def review_gif(path, res, snap, names, dist=70, sc=4):
    """panels side by side: each anim over a ground with the target (a man) at the right; frames are placed by their canvas so nothing jitters. snap = frame index where the attack lands
    (its sprite's right edge touches the man); the animation's travel (0..1) walks the sprite in from `dist` px away."""
    hum = human(); fr0 = res["run"][0][0]; w, h = fr0.size
    panels = [(n, res[k], snap.get(k)) for n, k in names]
    PW = w + 24 + dist; PH = h + 14; gy = PH - 12; hx = PW - 36
    frames = []; durs = []; prev = None
    for tm in range(0, 5200, 10):
        key = tuple((int(tm // r[1][0]) % len(r[0])) if k == "run" else idx_at(r[1], tm) for (n, k), (_, r, s) in zip(names, panels)); 
        if key == prev: durs[-1] += 10; continue
        prev = key; row = Image.new("RGBA", (PW * len(panels) * sc + 8 * (len(panels) - 1), PH * sc), (28, 26, 22, 255))
        for pi, ((n, k), (_, r, s)) in enumerate(zip(names, panels)):
            fl, ms, tr = r; idx = key[pi]; im = Image.new("RGBA", (PW, PH), (42, 38, 32, 255)); d = ImageDraw.Draw(im); d.rectangle([0, gy, PW, PH], fill=(54, 49, 41, 255))
            if k == "run":
                off = int(tm * 0.35) % 24
                for gx in range(-24, PW, 24): d.rectangle([gx - off + 24, gy + 4, gx - off + 32, gy + 5], fill=(70, 64, 54, 255))
                x = (PW - w) // 2
            else:
                bb = fl[s][0].getbbox() if False else fl[s].getbbox(); end_x = hx + 3 - bb[2]
                x = int(end_x - (1.0 - (tr[idx] if k == "pounce" else 1.0)) * (dist if k == "pounce" else 0))
                hf = hum.copy()
                if s is not None and s <= idx <= s + 2:
                    tint = Image.new("RGBA", hf.size, (200, 40, 40, 255)); hf = Image.composite(tint, hf, hf.split()[3].point(lambda v: 110 if v else 0))
                im.alpha_composite(hf, (hx, gy - 30))
            fr = fl[idx]; bb = fr.getbbox(); foot = (bb[3] if bb else h) ; im.alpha_composite(fr, (x, gy - h + 8))
            if k != "run" and s is not None:
                t0 = tm % (sum(ms) + 400); st = lambda i: sum(ms[:i]); kk = lambda i, step: int((t0 - st(i)) // step) if t0 >= st(i) else -1
                if k == "pounce":
                    FX.dust_puff(im, 24, gy + 1, kk(2, 55), seed=3)
                    if 3 <= idx <= 5: FX.speed_lines(im, x + 30, gy - h // 2, 20, kk(3, 40))
                    FX.dust_puff(im, hx - 4, gy + 1, kk(6, 60), seed=5)
                if n in ("bite", "pounce"): FX.bite_hit(im, hx + 6, gy - 20, kk(s, 55))
            row.alpha_composite(im.resize((PW * sc, PH * sc), Image.NEAREST), (pi * (PW * sc + 8), 0))
        frames.append(row.convert("P", palette=Image.ADAPTIVE)); durs.append(10)
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=durs, loop=0)

def save_all(out, which):
    os.makedirs(out, exist_ok=True); meta = {}
    cfg = {"alpha": dict(snap=dict(bite=5, pounce=7), names=[("run", "run"), ("bite", "bite"), ("pounce", "pounce")], cloak=False, key="alpha_prowler"),
           "rot": dict(snap=dict(bite=5, pounce=7), names=[("crawl", "run"), ("bite", "bite"), ("pounce", "pounce")], cloak=False, key="rot_crawler"),
           "mind": dict(snap=dict(bite=6), names=[("float", "run"), ("psi blast", "bite")], cloak=False, key="mind_weaver"),
           "void": dict(snap=dict(bite=6), names=[("drift", "run"), ("neural overload", "bite")], cloak=False, key="void_weaver")}
    for c in which:
        cf = cfg[c]; res = render(c, False); key = cf["key"]; meta[key] = {}
        names = {"run": {"alpha": "run", "rot": "crawl", "mind": "float", "void": "drift"}[c], "bite": {"mind": "cast", "void": "overload"}.get(c, "bite"), "pounce": "pounce"}
        for k, (fl, ms, tr) in res.items():
            if c in ("mind", "void") and k == "pounce": continue
            w, h = fl[0].size; strip = Image.new("RGBA", (w * len(fl), h), (0, 0, 0, 0))
            for i, f in enumerate(fl): strip.alpha_composite(f, (i * w, 0))
            strip.save(os.path.join(out, f"{key}_{names[k]}_strip.png")); meta[key][names[k]] = dict(frame=[w, h], ms=ms, travel=tr)
        res2 = {k: v for k, v in res.items() if not (c in ("mind", "void") and k == "pounce")}
        review_gif(os.path.join(out, f"{key}_animations.gif"), res2, cf["snap"], [n for n in cf["names"]])
        contact(res2 if False else res, os.path.join(out, f"{key}_sheet.png"), sc=3, cols=8)
        print(key, "done")
        if c in ("alpha", "void"):
            res3 = render(c, True) if False else None
    return meta

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "_out/humanoid_anim"; which = sys.argv[2].split(",") if len(sys.argv) > 2 else ["alpha", "rot", "mind", "void"]
    meta = save_all(out, which); json.dump(meta, open(os.path.join(out, "humanoid_anims.json"), "w"), indent=1)
