# Double-density (x2) redraws of the props that did not read at 16 px (the audit of 2026-11-11, Yan: "go ahead, six weak ones included").
# scale = 2: one sprite pixel is half a map pixel, a tile is 32 sprite pixels wide. Front-oblique view like the rest of the world: the top surface above, the south face below.
# References (ArtSource/external/reference, SOURCES.md): Crimelike furniture and Kenney's Roguelike Indoor (how one object is redrawn per facing: the identity is carried by what
# sticks out of the silhouette and what lies on top), the CC-BY furniture views (proportions only). Everything is drawn new here in the world's own ramps.
import math
from PIL import Image, ImageDraw
import props
from props import Prop, finish
from worldlib import *

def sh(c, k): return tuple(max(0, min(255, int(round(v * k)))) for v in c[:3]) + (255,)
def X2(im, pivot, shadow, tall=False):
    p = finish(im, pivot, shadow=shadow, tall=tall); p.scale = 2; return p
def mirror(p): return Prop(p.im.transpose(Image.FLIP_LEFT_RIGHT), (p.im.width - 1 - p.pivot[0], p.pivot[1]), p.shadow, p.tall, scale=p.scale)
def var(seed): return (seed - 1000) // 77
CREAM = hx("#d9cfb0"); PAPER = hx("#c9c2a8"); INK = hx("#3a352c"); DARK = hx("#15171a")

def boxx(d, x0, y0, w, dep, h, top, face, lip=True):
    """An oblique box: top (w x dep) at (x0, y0), south face (w x h) under it. top/face are 5-step ramps (dark..light) or a single colour."""
    T = top if isinstance(top, list) else [sh(top, .6), sh(top, .8), top, sh(top, 1.15), sh(top, 1.3)]
    F = face if isinstance(face, list) else [sh(face, .6), sh(face, .8), face, sh(face, 1.15), sh(face, 1.3)]
    d.rectangle([x0, y0, x0 + w - 1, y0 + dep - 1], fill=T[3])
    d.line([(x0, y0), (x0 + w - 1, y0)], fill=T[4]); d.line([(x0, y0), (x0, y0 + dep - 1)], fill=T[4]); d.line([(x0 + w - 1, y0 + 1), (x0 + w - 1, y0 + dep - 1)], fill=T[2])
    d.rectangle([x0, y0 + dep, x0 + w - 1, y0 + dep + h - 1], fill=F[2])
    if lip: d.line([(x0, y0 + dep), (x0 + w - 1, y0 + dep)], fill=F[0])
    d.line([(x0, y0 + dep + 1), (x0, y0 + dep + h - 1)], fill=F[3]); d.line([(x0 + w - 1, y0 + dep + 1), (x0 + w - 1, y0 + dep + h - 1)], fill=F[1])
    d.line([(x0, y0 + dep + h - 1), (x0 + w - 1, y0 + dep + h - 1)], fill=F[0])
    return T, F

# ---------------------------------------------------------------- lockers: a bank of three steel lockers
def _locker_cols(seed): return P["olive"] if var(seed) % 2 else P["steel"]
def lockers_front(seed):
    S = _locker_cols(seed); im = blank(30, 48); d = ImageDraw.Draw(im)
    T, F = boxx(d, 0, 0, 30, 8, 38, S, S)
    for i in range(3):
        x = i * 10
        if i: d.line([(x, 9), (x, 44)], fill=S[0]); d.line([(x - 1, 9), (x - 1, 44)], fill=S[1])     # the gap between two doors
        for k in range(3): d.line([(x + 3, 12 + k * 2), (x + 6, 12 + k * 2)], fill=S[0])                # louvres
        d.rectangle([x + 3, 20, x + 6, 22], fill=CREAM); d.point((x + 4, 21), fill=INK)                   # the number card
        d.rectangle([x + 7, 25, x + 7, 29], fill=S[4]); d.point((x + 7, 30), fill=S[0])                    # the handle
        for k in range(2): d.line([(x + 3, 38 + k * 2), (x + 6, 38 + k * 2)], fill=S[0])                # lower louvres
    d.rectangle([1, 46, 3, 47], fill=S[0]); d.rectangle([26, 46, 28, 47], fill=S[0]); d.rectangle([0, 45, 29, 45], fill=S[0])
    if var(seed) % 3 == 1: d.line([(12, 33), (17, 36)], fill=P["rust"][1]); d.point((16, 34), fill=P["rust"][2])
    return X2(im, (15, 47), (15, 5, 1, -1), True)
def lockers_back(seed):
    S = _locker_cols(seed); im = blank(30, 48); d = ImageDraw.Draw(im)
    boxx(d, 0, 0, 30, 8, 38, S, [S[0], S[0], S[1], S[2], S[3]])
    for x in (10, 20): d.line([(x, 9), (x, 44)], fill=S[0])                                           # the panel seams
    d.rectangle([2, 18, 27, 19], fill=S[2]); d.line([(2, 20), (27, 20)], fill=S[0])                    # a stiffening rail
    d.rectangle([2, 34, 27, 35], fill=S[2]); d.line([(2, 36), (27, 36)], fill=S[0])
    for x in (4, 14, 15, 25):
        for y in (12, 27, 41): d.point((x, y), fill=S[3])                                              # rivets
    d.rectangle([12, 23, 17, 26], fill=PAPER); d.line([(13, 24), (16, 24)], fill=INK)                  # an inventory label
    d.rectangle([0, 45, 29, 45], fill=S[0]); d.rectangle([1, 46, 3, 47], fill=S[0]); d.rectangle([26, 46, 28, 47], fill=S[0])
    return X2(im, (15, 47), (15, 5, 1, -1), True)
def lockers_side(seed):
    """From the end, doors to the EAST: the long top with the three lockers' seams, the plain end panel, and what sticks out on the door side: the handles, and the last door standing ajar."""
    S = _locker_cols(seed); im = blank(24, 56); d = ImageDraw.Draw(im)
    boxx(d, 0, 0, 15, 17, 38, S, [S[0], S[1], S[1], S[2], S[3]])
    for y in (6, 11): d.line([(1, y), (13, y)], fill=S[1]); d.line([(1, y + 1), (13, y + 1)], fill=S[4])    # seams between the three lockers, seen on the top
    for y in (3, 9):                                                                                       # the far doors' handles, sticking out past the edge
        d.rectangle([15, y, 15, y + 1], fill=S[4]); d.point((16, y + 1), fill=S[0])
    d.rectangle([3, 22, 11, 23], fill=S[2]); d.line([(3, 24), (11, 24)], fill=S[0])                        # the end panel: rails and rivets
    d.rectangle([3, 40, 11, 41], fill=S[2]); d.line([(3, 42), (11, 42)], fill=S[0])
    for (x, y) in ((2, 19), (12, 19), (2, 51), (12, 51)): d.point((x, y), fill=S[4])
    # the near door, open toward the camera's right: its inside face (lighter), the louvres seen from behind, a coat hook; the locker's dark inside behind it
    d.rectangle([15, 14, 16, 52], fill=DARK)
    d.polygon([(16, 17), (22, 20), (22, 55), (16, 52)], fill=S[3]); d.line([(16, 17), (22, 20)], fill=S[4]); d.line([(22, 20), (22, 55)], fill=S[1]); d.line([(16, 52), (22, 55)], fill=S[0])
    for k in range(3): d.line([(18, 23 + k * 2), (20, 24 + k * 2)], fill=S[0])
    d.rectangle([18, 33, 20, 35], fill=CREAM)
    d.line([(21, 36), (21, 40)], fill=S[4])
    d.rectangle([0, 55, 14, 55], fill=S[0])
    return X2(im, (8, 55), (9, 7, 2, -2), True)

# ---------------------------------------------------------------- the filing cabinet: four drawers
def cabinet_front(seed):
    S = P["steel"]; im = blank(20, 46); d = ImageDraw.Draw(im)
    boxx(d, 0, 0, 20, 9, 36, S, S)
    for i in range(4):
        y = 10 + i * 9
        d.rectangle([1, y, 18, y + 7], outline=S[0], fill=S[2]); d.line([(2, y + 1), (17, y + 1)], fill=S[3])
        d.rectangle([7, y + 2, 12, y + 3], fill=CREAM); d.line([(8, y + 3), (11, y + 3)], fill=INK)     # the label card
        d.rectangle([6, y + 5, 13, y + 5], fill=S[4]); d.line([(6, y + 6), (13, y + 6)], fill=S[0])     # the pull
    if var(seed) % 2: d.rectangle([3, 3, 9, 6], fill=PAPER); d.line([(4, 4), (8, 4)], fill=INK)          # a file left on top
    d.rectangle([0, 45, 19, 45], fill=S[0])
    return X2(im, (10, 45), (11, 5, 1, -1), True)
def cabinet_back(seed):
    S = P["steel"]; im = blank(20, 46); d = ImageDraw.Draw(im)
    boxx(d, 0, 0, 20, 9, 36, S, [S[0], S[0], S[1], S[2], S[3]])
    d.rectangle([2, 12, 17, 42], outline=S[0])                                                        # the pressed back panel
    d.line([(3, 13), (16, 13)], fill=S[2])
    for y in (20, 29, 38): d.line([(3, y), (16, y)], fill=S[0])
    for (x, y) in ((4, 15), (15, 15), (4, 40), (15, 40)): d.point((x, y), fill=S[3])
    if var(seed) % 2: d.rectangle([3, 3, 9, 6], fill=PAPER)
    d.rectangle([0, 45, 19, 45], fill=S[0])
    return X2(im, (10, 45), (11, 5, 1, -1), True)
def cabinet_side(seed):
    """From the end, drawers to the EAST: the top, the plain side panel, the pulls sticking out, and one drawer pulled open with its folders seen from above."""
    S = P["steel"]; im = blank(34, 50); d = ImageDraw.Draw(im)
    boxx(d, 0, 0, 22, 12, 36, S, [S[0], S[1], S[1], S[2], S[3]])
    d.rectangle([2, 15, 19, 45], outline=S[0]); d.line([(3, 16), (18, 16)], fill=S[2])                # the side panel
    for (x, y) in ((4, 18), (17, 18), (4, 43), (17, 43)): d.point((x, y), fill=S[4])
    for i in (0, 2, 3):                                                                              # the closed drawers' pulls, past the edge
        y = 16 + i * 9; d.rectangle([22, y + 2, 22, y + 4], fill=S[4]); d.point((23, y + 4), fill=S[0])
    y = 16 + 9                                                                                       # the second drawer, open: folders from above, then its steel side
    d.rectangle([22, y - 6, 32, y - 1], fill=DARK)
    for k, x in enumerate(range(23, 32, 2)): d.line([(x, y - 6), (x, y - 1)], fill=CREAM if k % 2 == 0 else hx("#b8a878")); d.point((x, y - 6), fill=hx("#c85a3a") if k == 2 else CREAM)
    d.rectangle([22, y, 32, y + 6], fill=S[2]); d.line([(22, y), (32, y)], fill=S[4]); d.line([(22, y + 6), (32, y + 6)], fill=S[0]); d.line([(32, y - 6), (32, y + 6)], fill=S[1])
    d.rectangle([33, y + 1, 33, y + 4], fill=S[4])                                                    # its pull, edge-on
    d.rectangle([4, 3, 11, 8], fill=PAPER); d.line([(5, 5), (10, 5)], fill=INK); d.line([(5, 7), (9, 7)], fill=INK)
    d.rectangle([0, 48, 21, 48], fill=S[0])
    return X2(im, (11, 48), (12, 6, 3, -2), True)

# ---------------------------------------------------------------- the desk
def _desk_top(d, x0, y0, w, dep, W, seed, rot=False):
    """What lies on a desk: a green blotter with a sheet on it, a lamp, a mug. rot = the desk seen from its end (the blotter turned with it)."""
    if not rot:
        d.rectangle([x0 + 11, y0 + 3, x0 + 23, y0 + 9], fill=hx("#3d5a4a")); d.line([(x0 + 11, y0 + 3), (x0 + 23, y0 + 3)], fill=hx("#57786a"))
        d.rectangle([x0 + 14, y0 + 4, x0 + 19, y0 + 8], fill=PAPER); d.line([(x0 + 15, y0 + 5), (x0 + 18, y0 + 5)], fill=INK); d.line([(x0 + 15, y0 + 7), (x0 + 17, y0 + 7)], fill=INK)
        lx, ly = x0 + 4, y0 + 5
    else:
        d.rectangle([x0 + 3, y0 + 6, x0 + 9, y0 + 15], fill=hx("#3d5a4a")); d.line([(x0 + 3, y0 + 6), (x0 + 9, y0 + 6)], fill=hx("#57786a"))
        d.rectangle([x0 + 4, y0 + 8, x0 + 8, y0 + 13], fill=PAPER); d.line([(x0 + 5, y0 + 9), (x0 + 5, y0 + 12)], fill=INK); d.line([(x0 + 7, y0 + 9), (x0 + 7, y0 + 11)], fill=INK)
        lx, ly = x0 + w - 6, y0 + 4
    d.ellipse([lx - 2, ly, lx + 2, ly + 2], fill=hx("#2a2c2e")); d.line([(lx, ly), (lx, ly - 5)], fill=hx("#5a5e62")); d.polygon([(lx - 3, ly - 6), (lx + 3, ly - 6), (lx + 2, ly - 9), (lx - 2, ly - 9)], fill=hx("#8a6a2a")); d.line([(lx - 2, ly - 9), (lx + 2, ly - 9)], fill=hx("#c89a3a"))   # the lamp
    if var(seed) % 2: mx, my = (x0 + w - 6, y0 + dep - 5) if not rot else (x0 + 4, y0 + dep - 4); d.rectangle([mx, my, mx + 2, my + 2], fill=hx("#b9b4a4")); d.point((mx + 1, my), fill=hx("#3a2a20"))
def desk_front(seed):
    W = P["wood"]; im = blank(36, 40); d = ImageDraw.Draw(im); y0 = 9
    d.rectangle([0, y0, 35, y0 + 11], fill=W[3]); d.line([(0, y0), (35, y0)], fill=W[4]); d.line([(0, y0), (0, y0 + 11)], fill=W[4])       # the top slab
    for x in range(5, 35, 6): d.line([(x, y0 + 1), (x, y0 + 11)], fill=W[2])
    d.rectangle([0, y0 + 12, 35, y0 + 14], fill=W[2]); d.line([(0, y0 + 14), (35, y0 + 14)], fill=W[0])                                       # its front edge
    d.rectangle([1, y0 + 15, 12, y0 + 29], fill=W[2]); d.line([(1, y0 + 15), (1, y0 + 29)], fill=W[3])                                        # the drawer pedestal
    for k in range(3):
        y = y0 + 16 + k * 5; d.rectangle([2, y, 11, y + 3], outline=W[0], fill=W[2]); d.rectangle([5, y + 1, 8, y + 2], fill=hx("#c8b070"))
    d.rectangle([13, y0 + 15, 30, y0 + 26], fill=hx("#1b1512")); d.rectangle([13, y0 + 15, 30, y0 + 19], fill=W[0])                           # the kneehole: dark, the modesty board at its back
    d.rectangle([31, y0 + 15, 34, y0 + 29], fill=W[1]); d.line([(31, y0 + 15), (31, y0 + 29)], fill=W[3])                                      # the right-hand leg panel
    d.line([(1, y0 + 29), (12, y0 + 29)], fill=W[0]); d.line([(31, y0 + 29), (34, y0 + 29)], fill=W[0])
    _desk_top(d, 0, y0, 36, 12, W, seed)
    return X2(im, (18, 38), (17, 5, 1, -1))
def desk_back(seed):
    W = P["wood"]; im = blank(36, 40); d = ImageDraw.Draw(im); y0 = 9
    d.rectangle([0, y0, 35, y0 + 11], fill=W[3]); d.line([(0, y0), (35, y0)], fill=W[4]); d.line([(0, y0), (0, y0 + 11)], fill=W[4])
    for x in range(5, 35, 6): d.line([(x, y0 + 1), (x, y0 + 11)], fill=W[2])
    d.rectangle([0, y0 + 12, 35, y0 + 14], fill=W[2]); d.line([(0, y0 + 14), (35, y0 + 14)], fill=W[0])
    d.rectangle([1, y0 + 15, 34, y0 + 26], fill=W[1]); d.rectangle([3, y0 + 17, 32, y0 + 24], outline=W[0]); d.line([(4, y0 + 18), (31, y0 + 18)], fill=W[2])   # the modesty panel, framed
    d.rectangle([1, y0 + 27, 4, y0 + 29], fill=W[1]); d.rectangle([31, y0 + 27, 34, y0 + 29], fill=W[1]); d.rectangle([5, y0 + 27, 30, y0 + 28], fill=hx("#1b1512"))   # legs, the dark under the panel
    im2 = im.copy(); d2 = ImageDraw.Draw(im2)
    _desk_top(d2, 0, y0, 36, 12, W, seed)
    top = im2.crop((0, 0, 36, y0 + 12)).transpose(Image.FLIP_LEFT_RIGHT); im.paste(top, (0, 0))                                                 # the same things, seen from the other side
    return X2(im, (18, 38), (17, 5, 1, -1))
def desk_side(seed):
    """From the end, drawers to the EAST: the long top with the blotter turned, the end panel, and the top drawer pulled open on the right."""
    W = P["wood"]; im = blank(28, 46); d = ImageDraw.Draw(im); y0 = 9
    d.rectangle([0, y0, 17, y0 + 20], fill=W[3]); d.line([(0, y0), (17, y0)], fill=W[4]); d.line([(0, y0), (0, y0 + 20)], fill=W[4])
    for y in range(y0 + 5, y0 + 20, 6): d.line([(1, y), (17, y)], fill=W[2])
    d.rectangle([0, y0 + 21, 17, y0 + 23], fill=W[2]); d.line([(0, y0 + 23), (17, y0 + 23)], fill=W[0])
    d.rectangle([1, y0 + 24, 16, y0 + 35], fill=W[1]); d.rectangle([3, y0 + 26, 14, y0 + 33], outline=W[0]); d.line([(4, y0 + 27), (13, y0 + 27)], fill=W[2])   # the end panel
    d.line([(1, y0 + 35), (16, y0 + 35)], fill=W[0])
    # the pedestal's top drawer, pulled open to the east at the near end: papers inside, its side, the brass pull edge-on
    yy = y0 + 25
    d.rectangle([17, yy - 5, 25, yy - 1], fill=hx("#1b1512")); d.rectangle([18, yy - 5, 21, yy - 2], fill=PAPER); d.rectangle([22, yy - 4, 24, yy - 2], fill=hx("#b8a878"))
    d.rectangle([17, yy, 25, yy + 4], fill=W[2]); d.line([(17, yy), (25, yy)], fill=W[4]); d.line([(17, yy + 4), (25, yy + 4)], fill=W[0]); d.line([(25, yy - 5), (25, yy + 4)], fill=W[1])
    d.rectangle([26, yy + 1, 26, yy + 3], fill=hx("#c8b070"))
    for y in (y0 + 6, y0 + 13): d.point((18, y), fill=hx("#c8b070"))                                   # the other pulls, far along the edge
    _desk_top(d, 0, y0, 18, 21, W, seed, rot=True)
    return X2(im, (9, 44), (10, 6, 2, -2))

# the desk tipped over as cover (Props.cs): `face` = the way its top now faces
def desk_tipped(face):
    def f(seed):
        W = P["wood"]; U = [sh(W[0], .9), W[0], W[1], W[2], W[3]]                                  # the underside: unvarnished, darker
        if face == "s":                                                                              # the top stands toward the camera: a big board on edge, the legs going away behind its upper edge
            im = blank(38, 34); d = ImageDraw.Draw(im)
            for x in (3, 31): d.rectangle([x, 2, x + 3, 9], fill=U[2]); d.line([(x, 2), (x + 3, 2)], fill=U[4])                  # the two upper legs, seen end-on above the board
            d.rectangle([0, 8, 37, 10], fill=W[2]); d.line([(0, 8), (37, 8)], fill=W[4])                                        # the board's upper edge (its thickness)
            d.rectangle([0, 11, 37, 31], fill=W[3]); d.line([(0, 11), (0, 31)], fill=W[4]); d.line([(37, 11), (37, 31)], fill=W[1]); d.line([(0, 31), (37, 31)], fill=W[0])
            for y in range(15, 31, 5): d.line([(1, y), (36, y)], fill=W[2])                                                       # the planks, now running across
            d.rectangle([14, 15, 26, 23], fill=sh(hx("#3d5a4a"), .9)); d.line([(14, 15), (26, 15)], fill=hx("#57786a"))            # the blotter, still stuck on
            for (x, y) in ((8, 19), (30, 26)): d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=DARK); d.point((x - 1, y - 1), fill=W[4])   # bullet holes
            return X2(im, (19, 32), (18, 4, 1, -1))
        if face == "n":                                                                              # the top faces away: we look at the underside, the drawers and the legs pointing at us
            im = blank(38, 36); d = ImageDraw.Draw(im)
            d.rectangle([0, 6, 37, 8], fill=U[3]); d.line([(0, 6), (37, 6)], fill=W[4])
            d.rectangle([0, 9, 37, 31], fill=U[2]); d.line([(0, 31), (37, 31)], fill=U[0]); d.line([(0, 9), (0, 31)], fill=U[3])
            d.rectangle([2, 11, 35, 29], outline=U[1])                                                                           # the frame rails under the top
            d.rectangle([3, 12, 15, 28], fill=W[1]); d.rectangle([3, 12, 15, 14], fill=W[2])                                      # the drawer pedestal, its bottom toward us
            for k in range(3): d.line([(4, 17 + k * 4), (14, 17 + k * 4)], fill=U[0])
            for (x, y) in ((3, 26), (32, 26), (32, 12)):                                                                          # legs coming toward the camera: short, their end grain lit
                d.rectangle([x, y, x + 3, y + 7], fill=W[2]); d.rectangle([x, y + 5, x + 3, y + 8], fill=W[3]); d.line([(x, y + 8), (x + 3, y + 8)], fill=W[0])
            return X2(im, (19, 34), (18, 4, 1, -1))
        im = blank(30, 44); d = ImageDraw.Draw(im)                                                    # e: the top faces east. The board is seen edge-on down the right side; legs and drawers lie to the west
        d.rectangle([22, 2, 26, 4], fill=W[4]); d.rectangle([22, 5, 26, 40], fill=W[3]); d.line([(22, 5), (22, 40)], fill=W[4]); d.line([(26, 5), (26, 40)], fill=W[1]); d.line([(22, 40), (26, 40)], fill=W[0])
        d.rectangle([27, 6, 27, 39], fill=W[1])
        for (y, ln) in ((6, 16), (30, 16)):                                                           # two legs lying out to the west, one behind the other
            d.rectangle([22 - ln, y, 21, y + 3], fill=U[3]); d.line([(22 - ln, y), (21, y)], fill=W[4]); d.rectangle([22 - ln, y + 4, 21, y + 6], fill=U[1]); d.line([(22 - ln, y + 6), (21, y + 6)], fill=U[0])
        d.rectangle([9, 14, 21, 27], fill=W[2]); d.line([(9, 14), (21, 14)], fill=W[4]); d.rectangle([9, 28, 21, 36], fill=W[1]); d.line([(9, 36), (21, 36)], fill=W[0])   # the pedestal on its side
        for x in (13, 17): d.line([(x, 29), (x, 35)], fill=U[0])
        d.rectangle([11, 31, 11, 33], fill=hx("#c8b070")); d.rectangle([15, 31, 15, 33], fill=hx("#c8b070"))
        p = X2(im, (16, 42), (12, 5, 0, -1))
        return mirror(p) if face == "w" else p
    return f

# ---------------------------------------------------------------- the armchair: high back, rolled arms, a sagging cushion
def armchair(seed):
    f = [ramp("#3a2624", "#523432", "#6a4642", "#83605a", "#9a7a70"), ramp("#2c3328", "#3d4636", "#515b47", "#69725c", "#828a72"), ramp("#3a3226", "#514632", "#6a5c42", "#857556", "#9c8c6a")][var(seed) % 3]
    im = blank(28, 34); d = ImageDraw.Draw(im)
    d.rectangle([5, 0, 22, 17], fill=f[2]); d.line([(5, 0), (22, 0)], fill=f[4]); d.line([(5, 1), (5, 16)], fill=f[3]); d.line([(22, 1), (22, 16)], fill=f[1])            # the tall back
    d.rectangle([8, 3, 19, 13], outline=f[1]); d.line([(9, 4), (18, 4)], fill=f[3])                                                                                        # its sewn panel
    for (x, y) in ((11, 7), (16, 7), (11, 10), (16, 10)): d.point((x, y), fill=f[0])                                                                                        # buttons
    for x0 in (0, 21):                                                                                                                                                    # the arms: a padded top we look down on, the front of the arm under it
        d.rectangle([x0, 11, x0 + 6, 18], fill=f[3]); d.line([(x0, 11), (x0 + 6, 11)], fill=f[4]); d.line([(x0, 18), (x0 + 6, 18)], fill=f[1])
        d.rectangle([x0, 19, x0 + 6, 29], fill=f[2]); d.line([(x0, 19), (x0, 29)], fill=f[3]); d.line([(x0 + 6, 19), (x0 + 6, 29)], fill=f[1]); d.line([(x0, 29), (x0 + 6, 29)], fill=f[0])
    d.rectangle([7, 14, 20, 22], fill=f[4]); d.line([(7, 14), (20, 14)], fill=f[1]); d.line([(8, 18), (13, 19)], fill=f[3]); d.line([(13, 19), (19, 18)], fill=f[3])            # the seat cushion, seen from above, sagging
    d.rectangle([7, 23, 20, 25], fill=f[3]); d.line([(7, 23), (20, 23)], fill=f[2])                                                                                         # the cushion's front edge
    d.rectangle([7, 26, 20, 29], fill=f[1]); d.line([(7, 29), (20, 29)], fill=f[0])                                                                                         # the skirt
    d.rectangle([1, 30, 3, 31], fill=hx("#2a1c14")); d.rectangle([24, 30, 26, 31], fill=hx("#2a1c14"))                                                                        # feet
    if var(seed) % 2 == 0: d.polygon([(15, 16), (18, 17), (16, 20)], fill=hx("#d8d2c0")); d.point((17, 18), fill=hx("#a8a290"))                                               # stuffing out of a tear
    return X2(im, (14, 31), (13, 5, 1, -1))

FURNITURE = {"lockers": lockers_front, "lockers_fn": lockers_back, "lockers_fe": lockers_side, "lockers_fw": lambda s: mirror(lockers_side(s)),
             "cabinet": cabinet_front, "cabinet_fn": cabinet_back, "cabinet_fe": cabinet_side, "cabinet_fw": lambda s: mirror(cabinet_side(s)),
             "desk": desk_front, "desk_fn": desk_back, "desk_fe": desk_side, "desk_fw": lambda s: mirror(desk_side(s)),
             "desk_s": desk_tipped("s"), "desk_n": desk_tipped("n"), "desk_e": desk_tipped("e"), "desk_w": desk_tipped("w"), "armchair": armchair}
FURN_VARIANTS = {"lockers": 2, "lockers_fn": 2, "lockers_fe": 2, "lockers_fw": 2, "cabinet": 2, "cabinet_fn": 2, "cabinet_fe": 1, "cabinet_fw": 1, "desk": 2, "desk_fn": 2, "desk_fe": 2, "desk_fw": 2,
                 "desk_s": 1, "desk_n": 1, "desk_e": 1, "desk_w": 1, "armchair": 3}
# ---------------------------------------------------------------- a wrecked car (facing east): roof and bonnet from above, the body side under them
CAR = [ramp("#4a2a1c", "#6a3a22", "#8a4e2a", "#a8683a", "#c08650"), ramp("#232c36", "#33404e", "#465668", "#5c6e80", "#78899a"), ramp("#2a3324", "#3c4832", "#525f44", "#6b785a", "#869274"), ramp("#4a463c", "#625d50", "#7d7766", "#989280", "#b2ad9a")]
def wreck(seed):
    C = CAR[var(seed) % 4]; R = P["rust"]; r = rng(seed); im = blank(76, 42); d = ImageDraw.Draw(im); G = hx("#11161a")
    d.polygon([(4, 20), (16, 19), (22, 8), (50, 8), (58, 19), (72, 21), (72, 33), (4, 33)], fill=C[2])                              # the body's side, the cabin rising out of it
    d.polygon([(23, 3), (49, 3), (50, 8), (22, 8)], fill=C[4]); d.line([(23, 3), (49, 3)], fill=sh(C[4], 1.12))                      # the roof, from above
    d.polygon([(5, 15), (16, 14), (16, 19), (4, 20)], fill=C[3]); d.line([(5, 15), (16, 14)], fill=C[4])                             # the boot lid
    d.polygon([(57, 14), (71, 16), (72, 21), (58, 19)], fill=C[3]); d.line([(57, 14), (71, 16)], fill=C[4])                          # the bonnet
    d.polygon([(22, 9), (35, 9), (35, 18), (18, 18)], fill=G); d.polygon([(37, 9), (50, 9), (56, 18), (37, 18)], fill=G)              # the side windows, glass gone
    d.line([(36, 9), (36, 32)], fill=C[0]); d.line([(19, 19), (57, 19)], fill=C[1])                                                  # the door pillar and the belt line
    for (a, b) in (((23, 17), (27, 12)), ((40, 16), (44, 11)), ((47, 17), (49, 14))): d.line([a, b], fill=hx("#8fb3bd"))              # shards left in the frames
    d.line([(4, 26), (72, 26)], fill=C[1]); d.rectangle([4, 30, 72, 33], fill=C[1]); d.line([(4, 33), (72, 33)], fill=C[0])            # the sill
    d.line([(27, 22), (30, 22)], fill=C[0]); d.line([(43, 22), (46, 22)], fill=C[0])                                                 # door handles
    d.rectangle([69, 23, 72, 26], fill=hx("#d8d0a8") if var(seed) % 2 else DARK); d.rectangle([4, 23, 5, 26], fill=hx("#7a2a22"))      # a headlight (or its empty socket), a tail lamp
    for cx, flat in ((17, var(seed) % 2 == 0), (58, False)):                                                                         # wheels: tyre, rim; one of them flat on its rim
        d.ellipse([cx - 7, 27, cx + 7, 40 - (3 if flat else 0)], fill=P["tire"][0]); d.ellipse([cx - 6, 28, cx + 6, 38 - (3 if flat else 0)], fill=P["tire"][1])
        d.ellipse([cx - 3, 30, cx + 3, 35 - (2 if flat else 0)], fill=P["steel"][2]); d.point((cx, 32), fill=P["steel"][0])
        d.arc([cx - 9, 23, cx + 9, 41], 180, 360, fill=C[0])
    for _ in range(46):                                                                                                              # rust eating up from the sills and round the arches
        x, y = r.randrange(5, 72), r.choice((21, 22, 24, 25, 27, 28, 29, 30, 31, 32, 16, 5))
        if im.getpixel((x, y))[3] and im.getpixel((x, y))[:3] not in (G[:3], P["tire"][0][:3], P["tire"][1][:3]): d.point((x, y), fill=R[r.randrange(1, 4)])
    return X2(im, (38, 39), (34, 7, 2, -1))

def sunktruck(seed):
    """A lorry gone down in the marsh to its axles: the cab (roof from above, a dark windscreen), the load bed full of water, reeds growing through, the waterline all round."""
    C = ramp("#3a2a1e", "#553a26", "#70502e", "#8c6a3e", "#a6865a") if var(seed) % 2 == 0 else ramp("#262e32", "#364248", "#4a5860", "#60707a", "#7c8c94")
    R = P["rust"]; r = rng(seed); im = blank(88, 46); d = ImageDraw.Draw(im); Wt = P["water"]
    d.ellipse([0, 26, 87, 45], fill=Wt[0]); d.ellipse([3, 28, 84, 43], fill=Wt[1])                                                    # the pool it sits in
    d.rectangle([6, 14, 56, 34], fill=C[1]); d.rectangle([6, 14, 56, 16], fill=C[3]); d.rectangle([8, 17, 54, 26], fill=Wt[1])         # the bed: rails, water standing in it
    for x in range(10, 54, 9): d.line([(x, 27), (x, 34)], fill=C[0])
    d.line([(6, 27), (56, 27)], fill=C[3]); d.line([(6, 34), (56, 34)], fill=C[0])
    d.rectangle([58, 6, 82, 34], fill=C[2]); d.rectangle([58, 6, 82, 14], fill=C[4]); d.line([(58, 6), (82, 6)], fill=sh(C[4], 1.1))    # the cab: roof, then its side
    d.rectangle([61, 17, 71, 24], fill=hx("#11161a")); d.line([(62, 23), (66, 18)], fill=hx("#8fb3bd"))                                # the door window
    d.rectangle([74, 17, 80, 24], fill=hx("#11161a")); d.line([(72, 15), (72, 34)], fill=C[0])
    d.rectangle([83, 20, 86, 34], fill=C[1]); d.rectangle([83, 20, 86, 22], fill=C[3])                                                 # the bonnet's stub
    for x0 in range(4, 86, 3): d.point((x0 + r.randrange(2), 35), fill=Wt[3])                                                          # the waterline
    d.rectangle([4, 36, 86, 45], fill=(0, 0, 0, 0)); d.ellipse([2, 30, 85, 45], outline=Wt[2])
    d.rectangle([5, 35, 86, 37], fill=Wt[1])
    for _ in range(40):
        x, y = r.randrange(7, 84), r.randrange(8, 34)
        if im.getpixel((x, y))[:3] in (C[1][:3], C[2][:3]): d.point((x, y), fill=R[r.randrange(1, 4)])
    for x in (14, 30, 47, 52):                                                                                                        # reeds up through the bed
        h = r.randrange(8, 14); d.line([(x, 24), (x + r.choice((-1, 0, 1)), 24 - h)], fill=P["drygrass"][2]); d.rectangle([x - 1, 22 - h, x, 25 - h], fill=hx("#4a3424"))
    p = X2(im, (44, 38), None)
    return mirror(p) if var(seed) % 2 else p

# ---------------------------------------------------------------- a dumped engine on a timber skid
def engine(seed):
    G = ramp("#2e3a2c", "#3f4d3a", "#53614a", "#6a775c", "#84907a"); S = P["steel"]; R = P["rust"]; W = P["wood"]; r = rng(seed); im = blank(34, 28); d = ImageDraw.Draw(im)
    d.rectangle([1, 23, 32, 26], fill=W[1]); d.line([(1, 23), (32, 23)], fill=W[3]); d.line([(1, 26), (32, 26)], fill=W[0])            # the skid
    d.rectangle([4, 9, 27, 13], fill=G[4]); d.line([(4, 9), (27, 9)], fill=sh(G[4], 1.12))                                              # the valve cover's top
    for x in (7, 13, 19, 25): d.point((x, 11), fill=S[0])                                                                               # its bolts
    d.rectangle([14, 6, 17, 9], fill=S[2]); d.rectangle([14, 5, 17, 6], fill=S[4])                                                      # the oil filler cap
    d.rectangle([4, 14, 27, 20], fill=G[2]); d.line([(4, 14), (27, 14)], fill=G[0]); d.line([(4, 15), (4, 20)], fill=G[3])              # the block's side
    for x in (7, 12, 17, 22):                                                                                                          # four exhaust ports, the manifold gone
        d.ellipse([x - 1, 15, x + 2, 18], fill=R[0]); d.point((x, 16), fill=DARK)
    d.rectangle([5, 21, 26, 23], fill=S[1]); d.line([(5, 23), (26, 23)], fill=S[0])                                                     # the sump
    d.ellipse([26, 12, 32, 22], fill=S[1]); d.ellipse([27, 13, 31, 21], fill=S[3]); d.ellipse([28, 15, 30, 19], fill=S[0])              # the crank pulley, a loop of belt hanging off it
    d.line([(29, 12), (26, 8)], fill=DARK); d.line([(26, 8), (24, 9)], fill=DARK)
    d.rectangle([1, 12, 4, 19], fill=S[2]); d.line([(1, 12), (4, 12)], fill=S[4])                                                       # the bell housing
    for _ in range(14):
        x, y = r.randrange(5, 27), r.randrange(14, 21); d.point((x, y), fill=R[r.randrange(1, 4)])
    return X2(im, (17, 26), (15, 4, 1, -1))

# ---------------------------------------------------------------- the lathe
def _lathe(seed, back):
    G = P["olive"] if var(seed) % 2 else ramp("#2f3a3c", "#3f4e50", "#536567", "#6b7f80", "#8a9c9c"); S = P["steel"]; R = P["rust"]; r = rng(seed); im = blank(56, 38); d = ImageDraw.Draw(im)
    for x0 in (2, 40): boxx(d, x0, 20, 14, 3, 13, G, [G[0], G[1], G[1], G[2], G[3]])                                                     # the two cabinet feet
    d.rectangle([16, 26, 39, 29], fill=S[1]); d.line([(16, 26), (39, 26)], fill=S[3]); d.line([(16, 29), (39, 29)], fill=S[0])            # the chip tray between them
    boxx(d, 1, 14, 54, 4, 5, G, [G[0], G[1], G[2], G[3], G[4]])                                                                           # the bed
    d.line([(16, 15), (53, 15)], fill=S[4]); d.line([(16, 16), (53, 16)], fill=S[2])                                                      # the ways, bare steel
    boxx(d, 1, 2, 15, 5, 12, G, G)                                                                                                        # the headstock
    if not back:
        d.rectangle([3, 9, 8, 12], fill=DARK); d.point((4, 10), fill=hx("#7fe08a")); d.point((6, 10), fill=hx("#e0503a"))                   # its switch plate
        for y in (14, 16): d.line([(10, y), (13, y)], fill=G[0])
        d.rectangle([16, 6, 20, 13], fill=S[3]); d.line([(16, 6), (20, 6)], fill=S[4]); d.line([(20, 7), (20, 13)], fill=S[1]); d.line([(18, 7), (18, 13)], fill=S[1])   # the chuck, edge-on, its jaws
        d.line([(21, 9), (30, 9)], fill=S[4]); d.line([(21, 10), (30, 10)], fill=S[1])                                                    # a bar still in it
        boxx(d, 26, 11, 9, 3, 6, S, [S[0], S[1], S[2], S[3], S[4]])                                                                       # the carriage
        d.rectangle([29, 7, 31, 11], fill=S[1]); d.point((30, 7), fill=S[4])                                                              # the tool post
        d.ellipse([27, 19, 33, 25], outline=S[4], fill=None); d.point((30, 22), fill=S[4]); d.line([(30, 19), (30, 25)], fill=S[2])         # the apron handwheel
        boxx(d, 43, 7, 9, 4, 7, G, G); d.line([(40, 11), (43, 11)], fill=S[4]); d.ellipse([51, 9, 55, 14], outline=S[4])                    # the tailstock, its centre and handwheel
    else:
        boxx(d, 3, 9, 11, 3, 9, S, [S[0], S[1], S[2], S[2], S[3]])                                                                        # the motor housing hung on the back
        for y in (14, 16, 18): d.line([(5, y), (11, y)], fill=S[0])
        d.polygon([(14, 4), (22, 4), (24, 13), (14, 13)], fill=G[1]); d.line([(14, 4), (22, 4)], fill=G[3])                                # the belt guard
        boxx(d, 43, 7, 9, 4, 7, G, [G[0], G[1], G[1], G[2], G[3]])
        d.rectangle([26, 12, 34, 13], fill=S[2])
        for x in range(18, 52, 4): d.point((x, 21), fill=G[0])
    for _ in range(12):
        x, y = r.randrange(2, 54), r.randrange(3, 32)
        if im.getpixel((x, y))[3]: d.point((x, y), fill=R[r.randrange(1, 4)])
    return X2(im, (28, 36), (26, 6, 1, -1), True)
def machine(seed): return _lathe(seed, False)
def machine_back(seed): return _lathe(seed, True)

# ---------------------------------------------------------------- floor hatches (five kinds)
def hatch(seed):
    v = var(seed) % 5; C = P["concrete"]; S = P["steel"]; W = P["wood"]; R = P["rust"]; r = rng(seed); im = blank(38, 30); d = ImageDraw.Draw(im)
    d.polygon([(3, 6), (34, 6), (37, 27), (0, 27)], fill=C[2]); d.line([(3, 6), (34, 6)], fill=C[4]); d.line([(0, 27), (37, 27)], fill=C[0])     # the concrete collar, seen from above
    d.line([(0, 28), (37, 28)], fill=C[0])
    if v == 0 or v == 4:                                                                                                                  # a steel double-leaf hatch, chequer plate; 4: with a wheel
        d.polygon([(7, 9), (30, 9), (32, 24), (5, 24)], fill=S[2]); d.line([(7, 9), (30, 9)], fill=S[4]); d.line([(5, 24), (32, 24)], fill=S[0]); d.line([(18, 9), (18, 24)], fill=S[0])
        for y in range(11, 24, 3):
            for x in range(8 + (y % 2), 31, 4): d.point((x, y), fill=S[3])
        for x in (6, 31): d.rectangle([x - 1, 12, x, 14], fill=S[0]); d.rectangle([x - 1, 19, x, 21], fill=S[0])                              # hinges
        if v == 4: d.ellipse([14, 12, 22, 20], outline=S[4]); d.line([(14, 16), (22, 16)], fill=S[4]); d.line([(18, 12), (18, 20)], fill=S[4]); d.point((18, 16), fill=S[0])
        else: d.rectangle([20, 15, 23, 16], fill=S[4]); d.rectangle([13, 15, 16, 16], fill=S[4])
        for x in range(6, 32, 4): d.line([(x, 25), (x + 1, 25)], fill=hx("#c8a02a"))
    elif v == 1:                                                                                                                          # a round manhole, the cover shoved half off the black hole
        d.ellipse([8, 9, 28, 24], fill=DARK); d.ellipse([9, 10, 27, 23], outline=C[0])
        d.ellipse([15, 7, 35, 21], fill=S[1]); d.ellipse([15, 6, 35, 20], fill=S[2]); d.ellipse([17, 8, 33, 18], outline=S[3])
        for (x, y) in ((21, 11), (25, 14), (29, 11), (25, 9)): d.point((x, y), fill=S[4])
        d.line([(12, 14), (12, 22)], fill=S[3]); d.line([(10, 16), (14, 16)], fill=S[3]); d.line([(10, 20), (14, 20)], fill=S[3])               # rungs going down
    elif v == 2:                                                                                                                          # a wooden cellar trapdoor, strap hinges, an iron ring
        d.polygon([(7, 9), (30, 9), (32, 24), (5, 24)], fill=W[2]); d.line([(7, 9), (30, 9)], fill=W[4]); d.line([(5, 24), (32, 24)], fill=W[0])
        for x in (12, 18, 24): d.line([(x, 10), (x, 23)], fill=W[0])
        for y in (12, 20): d.rectangle([6, y, 16, y + 1], fill=S[1]); d.point((8, y), fill=S[4])
        d.ellipse([22, 14, 27, 19], outline=S[4]); d.point((24, 14), fill=S[1])
    else:                                                                                                                                 # a drain grate
        d.polygon([(7, 9), (30, 9), (32, 24), (5, 24)], fill=DARK)
        for x in range(8, 31, 3): d.line([(x, 9), (x, 24)], fill=S[2]); d.point((x, 9), fill=S[4])
        d.polygon([(7, 9), (30, 9), (32, 24), (5, 24)], outline=S[1]); d.line([(6, 16), (31, 16)], fill=S[1])
    for _ in range(10):
        x, y = r.randrange(2, 36), r.randrange(7, 27)
        if im.getpixel((x, y))[:3] == C[2][:3]: d.point((x, y), fill=C[r.choice((1, 3))])
    return X2(im, (18, 27), None)

# ---------------------------------------------------------------- cables across the floor
def cables(seed):
    r = rng(seed); im = blank(32, 14); d = ImageDraw.Draw(im); ph = r.uniform(0, 6.28)
    for k, (dark, mid, lite) in enumerate(((hx("#17191b"), hx("#2c3033"), hx("#565c60")), (hx("#4a1a18"), hx("#8a2c28"), hx("#c05a50")))):
        pts = [(x, 4 + k * 4 + math.sin(x / 5.0 + ph + k * 1.7) * (2.2 - k * 0.6)) for x in range(0, 32)]
        for (x, y) in pts: d.rectangle([x, int(round(y)), x, int(round(y)) + 2], fill=mid)
        for (x, y) in pts: d.point((x, int(round(y)) + 2), fill=dark); d.point((x, int(round(y))), fill=lite) if x % 3 else None
    cx = 12 + var(seed) * 7; d.rectangle([cx, 3, cx + 4, 8], fill=hx("#8a8e90")); d.line([(cx, 3), (cx + 4, 3)], fill=hx("#c0c4c6")); d.line([(cx, 8), (cx + 4, 8)], fill=hx("#3a3e40"))   # a coupling
    return X2(im, (16, 11), None)

# ---------------------------------------------------------------- the hidden cache: a crate under a tarp, roped, stones on the corners
def cache(seed, open_=False):
    T = ramp("#2c3328", "#3a4334", "#4c5643", "#5f6a53", "#77826a"); W = P["wood"]; K = P["rock"]; im = blank(34, 28); d = ImageDraw.Draw(im)
    if not open_:
        d.polygon([(4, 6), (27, 5), (32, 14), (31, 24), (2, 25), (1, 14)], fill=T[2])                                                    # the tarp over the box: a lit top, a draped front
        d.polygon([(5, 6), (27, 5), (30, 13), (3, 14)], fill=T[3]); d.line([(5, 6), (27, 5)], fill=T[4])
        for x in (8, 14, 21, 26): d.line([(x, 14), (x - 1, 24)], fill=T[1])                                                              # folds
        d.line([(3, 14), (30, 13)], fill=T[1])
        d.line([(2, 19), (31, 18)], fill=hx("#b8a878")); d.line([(16, 5), (16, 24)], fill=hx("#b8a878")); d.point((16, 18), fill=hx("#6a5c3a"))   # the rope, knotted
        d.polygon([(24, 20), (31, 20), (31, 25), (24, 25)], fill=W[2]); d.line([(24, 20), (31, 20)], fill=W[4]); d.line([(27, 21), (27, 25)], fill=W[0])   # a corner of the crate showing
    else:
        d.polygon([(0, 14), (9, 9), (11, 24), (1, 26)], fill=T[1]); d.polygon([(0, 14), (9, 9), (10, 15), (1, 19)], fill=T[3])              # the tarp thrown back
        d.rectangle([9, 6, 31, 12], fill=DARK); d.rectangle([9, 6, 31, 6], fill=W[4]); d.line([(9, 6), (9, 12)], fill=W[3]); d.line([(31, 6), (31, 12)], fill=W[1])   # the crate, open
        d.rectangle([12, 9, 16, 11], fill=hx("#6a775c")); d.rectangle([19, 8, 24, 11], fill=hx("#8a6a3a")); d.point((27, 10), fill=hx("#c8b070"))   # things in it
        d.rectangle([9, 13, 31, 24], fill=W[2]); d.line([(9, 13), (31, 13)], fill=W[0]); d.line([(9, 24), (31, 24)], fill=W[0])
        for x in (15, 21, 27): d.line([(x, 14), (x, 23)], fill=W[1])
        d.line([(10, 18), (30, 18)], fill=W[3])
    for (x, y) in ((1, 23), (29, 24)) if not open_ else ((2, 24),): d.ellipse([x, y - 2, x + 5, y + 2], fill=K[2]); d.line([(x + 1, y - 2), (x + 4, y - 2)], fill=K[4])   # the stones
    return X2(im, (17, 26), (15, 4, 1, -1))

# ---------------------------------------------------------------- scrap heaps (three compositions, five variants)
def scrap(seed):
    v = var(seed) % 5; S = P["steel"]; R = P["rust"]; T = P["tin"]; r = rng(seed); im = blank(56, 38); d = ImageDraw.Draw(im)
    if v in (0, 3):                                                                                                                     # corrugated sheets stacked crooked, a big gear leaning on them, pipes
        for k, (x0, y0) in enumerate(((4, 18), (8, 13), (2, 23))):
            d.polygon([(x0, y0), (x0 + 34, y0 - 3), (x0 + 37, y0 + 5), (x0 + 3, y0 + 8)], fill=T[1 + k % 2])
            for x in range(x0 + 3, x0 + 34, 4): d.line([(x, y0), (x + 3, y0 + 7)], fill=T[0])
            d.line([(x0, y0), (x0 + 34, y0 - 3)], fill=T[3])
        cx, cy = 42, 22
        for a in range(0, 360, 30): d.rectangle([cx + int(11 * math.cos(math.radians(a))) - 1, cy + int(11 * math.sin(math.radians(a))) - 1, cx + int(11 * math.cos(math.radians(a))) + 1, cy + int(11 * math.sin(math.radians(a))) + 1], fill=S[1])
        d.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=S[2]); d.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], outline=S[4]); d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=DARK)
        for a in range(0, 360, 90): d.line([(cx, cy), (cx + int(8 * math.cos(math.radians(a + 20))), cy + int(8 * math.sin(math.radians(a + 20))))], fill=S[1])
        for y in (31, 34): d.rectangle([6, y, 34, y + 2], fill=S[2]); d.line([(6, y), (34, y)], fill=S[4]); d.ellipse([33, y - 1, 37, y + 3], fill=DARK, outline=S[3])
    elif v in (1, 4):                                                                                                                   # a car door, a wheel rim, a bucket, a length of angle iron
        C = CAR[(var(seed) + 1) % 4]
        d.polygon([(4, 12), (30, 9), (33, 30), (6, 33)], fill=C[2]); d.polygon([(7, 13), (27, 11), (28, 19), (8, 21)], fill=hx("#11161a")); d.line([(4, 12), (30, 9)], fill=C[4])
        d.line([(9, 20), (13, 15)], fill=hx("#8fb3bd")); d.rectangle([23, 24, 27, 25], fill=C[0]); d.line([(6, 33), (33, 30)], fill=C[0])
        d.ellipse([32, 20, 52, 36], fill=P["tire"][0]); d.ellipse([34, 22, 50, 34], fill=S[2]); d.ellipse([39, 25, 45, 31], fill=DARK); d.ellipse([34, 22, 50, 34], outline=S[4])
        for a in range(0, 360, 72): d.point((42 + int(6 * math.cos(math.radians(a))), 28 + int(4 * math.sin(math.radians(a)))), fill=S[0])
        d.polygon([(38, 6), (48, 6), (46, 16), (40, 16)], fill=T[2]); d.ellipse([38, 4, 48, 8], fill=DARK, outline=T[3]); d.arc([36, 0, 50, 10], 190, 350, fill=S[3])
        d.line([(2, 35), (30, 36)], fill=S[3]); d.line([(2, 36), (30, 37)], fill=S[1])
    else:                                                                                                                               # crushed oil drums, a coil of chain
        for (x0, y0, c) in ((4, 14, R), (24, 18, ramp("#232c36", "#33404e", "#465668", "#5c6e80"))):
            d.polygon([(x0, y0 + 4), (x0 + 6, y0), (x0 + 20, y0 + 1), (x0 + 24, y0 + 7), (x0 + 22, y0 + 16), (x0 + 3, y0 + 17)], fill=c[2])
            d.polygon([(x0, y0 + 4), (x0 + 6, y0), (x0 + 20, y0 + 1), (x0 + 24, y0 + 7), (x0 + 12, y0 + 8)], fill=c[3])
            for yy in (y0 + 10, y0 + 13): d.line([(x0 + 2, yy), (x0 + 22, yy - 1)], fill=c[0])
            d.ellipse([x0 + 9, y0 + 2, x0 + 13, y0 + 5], fill=DARK)
        for k in range(4): d.ellipse([34 + k, 6 + k * 2, 50 - k, 16 + k], outline=S[1 + k % 3])
        d.line([(42, 16), (46, 30)], fill=S[2]); d.line([(46, 30), (52, 33)], fill=S[2])
    for _ in range(40):
        x, y = r.randrange(2, 54), r.randrange(4, 36)
        px0 = im.getpixel((x, y))
        if px0[3] and px0[:3] != DARK[:3]: d.point((x, y), fill=R[r.randrange(1, 4)])
    return X2(im, (28, 36), (24, 5, 1, -1))

# ---------------------------------------------------------------- a smashed specimen vat
def vatsmall(seed):
    S = P["steel"]; r = rng(seed); im = blank(30, 52); d = ImageDraw.Draw(im); GL = hx("#9fd0d8"); GD = hx("#4f8a94"); FL = hx("#2f5a4a")
    d.ellipse([1, 38, 28, 50], fill=FL); d.ellipse([4, 40, 25, 49], fill=hx("#3f7260"))                                                  # the spilled fluid
    boxx(d, 3, 36, 24, 4, 7, S, [S[0], S[1], S[2], S[3], S[4]])                                                                          # the base
    boxx(d, 3, 0, 24, 4, 4, S, [S[0], S[1], S[2], S[3], S[4]])                                                                           # the lid, still on its two rods
    for x in (5, 24): d.line([(x, 8), (x, 36)], fill=S[1]); d.line([(x + 1, 8), (x + 1, 36)], fill=S[3])
    d.polygon([(7, 36), (7, 22), (10, 26), (12, 17), (15, 27), (18, 20), (20, 28), (23, 24), (23, 36)], fill=(GD[0], GD[1], GD[2], 150))   # what is left of the glass: a jagged ring
    for (a, b) in (((7, 36), (7, 22)), ((7, 22), (10, 26)), ((10, 26), (12, 17)), ((12, 17), (15, 27)), ((15, 27), (18, 20)), ((18, 20), (20, 28)), ((20, 28), (23, 24)), ((23, 24), (23, 36))): d.line([a, b], fill=GL)
    d.line([(9, 28), (9, 34)], fill=hx("#e8f6f8")); d.line([(21, 30), (21, 34)], fill=hx("#e8f6f8"))
    d.line([(14, 8), (13, 14)], fill=hx("#5a3a3a")); d.line([(16, 8), (17, 16)], fill=hx("#3a4a5a")); d.point((13, 15), fill=hx("#c8a02a"))  # tubes hanging from the lid
    for _ in range(9):
        x, y = r.randrange(2, 28), r.randrange(42, 50); d.point((x, y), fill=GL); d.point((x + 1, y), fill=hx("#e8f6f8")) if r.random() < .4 else None
    return X2(im, (15, 48), (13, 4, 1, -1), True)

# ---------------------------------------------------------------- the north-south pieces of the concrete block and the jersey barrier: the long top going away, the END toward the camera
def block_v(seed):
    v = var(seed) % 5; C = P["concrete"]; R = P["rust"]; r = rng(seed); im = blank(22, 50); d = ImageDraw.Draw(im)
    boxx(d, 2, 0, 18, 34, 14, C, [C[0], C[1], C[2], C[3], C[4]])
    for y in (11, 22): d.line([(3, y), (18, y)], fill=C[1]); d.line([(3, y + 1), (18, y + 1)], fill=C[4])                                  # the joints between the blocks in the row
    d.rectangle([4, 2, 17, 31], outline=C[2])                                                                                             # the cast edge chamfer round the top
    d.rectangle([5, 38, 8, 41], fill=C[1]); d.rectangle([13, 38, 16, 41], fill=C[1])                                                       # fork pockets in the end
    if v == 1: d.rectangle([7, 43, 14, 45], fill=hx("#d8d2c0")); d.line([(8, 44), (13, 44)], fill=hx("#3a352c"))                            # a stencilled number
    if v == 2: d.line([(4, 36), (10, 42)], fill=C[0]); d.line([(10, 42), (9, 47)], fill=C[0])                                              # cracked
    if v == 3: d.polygon([(15, 34), (20, 34), (20, 40)], fill=(0, 0, 0, 0)); d.line([(15, 35), (19, 40)], fill=R[2]); d.line([(16, 35), (16, 38)], fill=R[1])   # a corner knocked off, rebar showing
    if v == 4:
        for _ in range(16): d.point((r.randrange(3, 19), r.randrange(1, 33)), fill=P["moss"][r.randrange(2)])
    for _ in range(14): d.point((r.randrange(3, 19), r.randrange(1, 47)), fill=C[r.choice((1, 3))])
    return X2(im, (11, 48), (13, 30, 3, -10))
def jersey_v(seed):
    v = var(seed) % 5; C = P["concrete"]; R = P["rust"]; r = rng(seed); im = blank(24, 52); d = ImageDraw.Draw(im)
    # the run seen from above: a narrow crest down the middle, the sloped flanks either side, the wide foot; it carries on behind the end's outline, so there is no waist in the silhouette
    d.rectangle([1, 2, 22, 49], fill=C[1]); d.rectangle([4, 1, 19, 46], fill=C[2]); d.rectangle([9, 0, 14, 36], fill=C[4]); d.line([(9, 0), (9, 36)], fill=sh(C[4], 1.08)); d.line([(15, 0), (15, 36)], fill=C[3])
    for y in (12, 24): d.line([(1, y), (22, y)], fill=C[0])                                                                               # joints
    # the end toward the camera: the barrier's own section drawn over it - narrow head, sloped shoulders, wide foot
    prof = [(9, 37), (14, 37), (15, 42), (19, 46), (22, 47), (22, 50), (1, 50), (1, 47), (4, 46), (8, 42)]
    d.polygon(prof, fill=C[3]); d.line(prof + [prof[0]], fill=C[0]); d.line([(9, 37), (14, 37)], fill=C[4]); d.line([(1, 50), (22, 50)], fill=C[0])
    d.rectangle([10, 44, 13, 46], fill=C[1])                                                                                              # the drain slot
    if v in (1, 2):
        col = hx("#c8a02a") if v == 1 else hx("#c8443a")
        for y in range(2, 34, 6): d.polygon([(9, y), (14, y), (14, y + 2), (9, y + 2)], fill=col)                                          # painted bands on the crest
        d.polygon([(9, 37), (14, 37), (14, 39), (9, 39)], fill=col)
    if v == 3: d.polygon([(9, 6), (14, 6), (14, 10), (11, 11)], fill=C[1]); d.line([(11, 7), (13, 10)], fill=R[2])                          # a bite out of the crest
    if v == 4:
        for _ in range(18): d.point((r.randrange(2, 22), r.randrange(2, 35)), fill=P["moss"][r.randrange(2)])
    for _ in range(14): d.point((r.randrange(2, 22), r.randrange(2, 49)), fill=C[r.choice((1, 3))]) if True else None
    return X2(im, (12, 50), (13, 30, 3, -10))

REST = {"wreck": wreck, "sunktruck": sunktruck, "engine": engine, "machine": machine, "machine_fn": machine_back, "hatch": hatch, "cables": cables,
        "stash_cache": lambda s: cache(s, False), "stash_cache_open": lambda s: cache(s, True), "scrap": scrap, "vatsmall": vatsmall, "block_v": block_v, "jersey_v": jersey_v}
REST_VARIANTS = {"wreck": 4, "sunktruck": 2, "engine": 2, "machine": 2, "machine_fn": 2, "hatch": 5, "cables": 2, "stash_cache": 1, "stash_cache_open": 1, "scrap": 5, "vatsmall": 2, "block_v": 5, "jersey_v": 5}

ALL = {**FURNITURE, **REST}; ALL_VARIANTS = {**FURN_VARIANTS, **REST_VARIANTS}

def sheet(names, out, cols=6, cell=190, k=4):
    from PIL import ImageDraw as ID
    rows = (len(names) + cols - 1) // cols; S = Image.new("RGBA", (cols * cell, rows * (cell + 14)), (105, 115, 90, 255)); dd = ID.Draw(S)
    for i, n in enumerate(names):
        nm, v = n if isinstance(n, tuple) else (n, 0)
        p = ALL[nm](1000 + v * 77); im = p.im.resize((p.im.width * k, p.im.height * k), Image.NEAREST)
        S.alpha_composite(im, ((i % cols) * cell + (cell - im.width) // 2, (i // cols) * (cell + 14) + cell - 4 - im.height)); dd.text(((i % cols) * cell + 3, (i // cols) * (cell + 14) + cell), nm + (f" v{v}" if v else ""), fill=(255, 255, 255, 255))
    S.convert("RGB").save(out); print(out, S.size)
