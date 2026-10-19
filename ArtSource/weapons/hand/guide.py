# print a block-majority downscale of a hand A grid as text, as an underlay for hand-drawing a new B size
import sys, importlib, collections
sys.path.insert(0, '/home/claude/art/weapons/hand'); sys.path.insert(0, '/home/claude/art')
def crop(G):
    ys = [y for y, r in enumerate(G) if any(c != "." for c in r)]; xs = [x for x in range(len(G[0])) if any(G[y][x] != "." for y in ys)]
    return [r[xs[0]:xs[-1] + 1] for r in G[ys[0]:ys[-1] + 1]]
def down(G, w):
    G = crop(G); H, W = len(G), len(G[0]); f = W / w; h = round(H / f)
    out = []
    for y in range(h):
        row = ""
        for x in range(w):
            cells = [G[j][i] for j in range(int(y * f), min(H, max(int(y * f) + 1, int((y + 1) * f)))) for i in range(int(x * f), min(W, max(int(x * f) + 1, int((x + 1) * f))))]
            sol = [c for c in cells if c != "."]
            row += collections.Counter(sol).most_common(1)[0][0] if len(sol) * 2 >= len(cells) else "."
        out.append(row)
    return out
if __name__ == "__main__":
    mod, w = sys.argv[1], int(sys.argv[2])
    m = importlib.import_module(mod)
    G = m.G
    print(f"{len(crop(G)[0])}x{len(crop(G))} -> {w}")
    print("   " + "".join(str(i % 10) for i in range(w)))
    for i, r in enumerate(down(G, w)): print(f"{i:2d} {r}")
