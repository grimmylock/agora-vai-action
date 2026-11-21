# 2026-11-21 dungeon QA: walls that touch only at a corner (two '#' on one diagonal of a 2x2, floor on the other) look like odd dark blocks with a gap
# that reads as walkable. diag_fill closes each such corner with one wall tile: the plain floor tile of the two with more walls beside it.
def diag_fill(g, wall='#', plain='.'):
    H, W = len(g), len(g[0]); n = 0
    def walls(x, y): return sum(1 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if 0 <= x + dx < W and 0 <= y + dy < H and g[y + dy][x + dx] == wall)
    for y in range(H - 1):
        for x in range(W - 1):
            a, b, c, d = g[y][x], g[y][x + 1], g[y + 1][x], g[y + 1][x + 1]
            for (w1, w2, f1, f2) in (((x, y), (x + 1, y + 1), (x + 1, y), (x, y + 1)), ((x + 1, y), (x, y + 1), (x, y), (x + 1, y + 1))):
                if g[w1[1]][w1[0]] == wall and g[w2[1]][w2[0]] == wall and g[f1[1]][f1[0]] != wall and g[f2[1]][f2[0]] != wall:
                    cands = [f for f in (f1, f2) if g[f[1]][f[0]] == plain]
                    if cands:
                        fx, fy = max(cands, key=lambda f: walls(*f)); g[fy][fx] = wall; n += 1
    return n
