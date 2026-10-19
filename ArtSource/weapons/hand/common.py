# Shared helpers for hand-pixelled weapons (RK-12 method): draw A on a local grid with put/fill,
# centre it into 64x32, derive B by 2x2 reduction, then apply hand fixes to B.
import sys; sys.path.insert(0, '/home/claude/art/weapons')
from weapon_pixel import PAL, render, place, reduce_b

def grid(w, h): return [["."] * w for _ in range(h)]

def put(G, y, x0, s):
    for i, ch in enumerate(s):
        if ch != " " and 0 <= y < len(G) and 0 <= x0 + i < len(G[0]): G[y][x0 + i] = ch

def fill(G, y, x0, x1, ch): put(G, y, x0, ch * (x1 - x0 + 1))

def rows(G, y0, x0, lines):
    """paste a block of text rows; ' ' keeps what is there, '.' erases"""
    for j, s in enumerate(lines): put(G, y0 + j, x0, s)

def build(local, fixes_b=()):
    A = place(local, 64, 32)
    B = place(reduce_b(A), 32, 16)
    for (x, y, ch) in fixes_b: B[y][x] = ch
    return A, B

def build_ab(localA, localB):
    """hand-drawn B (half size of localA), centred the same way place() centres A"""
    for r in localB: assert len(r) == len(localB[0]), (len(r), r)
    return place(localA, 64, 32), place([list(r) for r in localB], 32, 16)
