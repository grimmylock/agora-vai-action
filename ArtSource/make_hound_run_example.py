# (Yan 2026-11-09: run 30% faster: 85 ms a frame, the walk beside it stays 110 ms in the GIF) Hound RUN cycle, example v2 (2026-11-09). Preview only, not in the game: python3 make_hound_run_example.py <out_dir> -> hound_run_vs_walk.gif, hound_run_sheet.png, hound_run_strip.png
import sys, math
import os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image
import make_mutant_frames as M, mutant_from_ref as m
body, jaw, legs = M.pallid_rig()
N = 8
FRONT_X = 30
full = body.copy(); full.alpha_composite(jaw)
CX = 26.0
def bend(img, stretch, dyf, dyb):
    """the whole body bent as ONE piece (no seam): every column slides up/down along a smooth curve (rear -> front) and the body is stretched about its middle"""
    out = Image.new("RGBA", img.size, (0, 0, 0, 0)); sp, op = img.load(), out.load(); Wd, Hd = img.size
    for x in range(Wd):
        sx = int(round(CX + (x - CX) / stretch))
        if not 0 <= sx < Wd: continue
        u = min(1.0, max(0.0, (sx - 8) / 36.0)); u = u * u * (3 - 2 * u)
        dy = int(round(dyb + (dyf - dyb) * u))
        for y in range(Hd):
            if sp[sx, y][3]:
                yy = y + dy
                if 0 <= yy < Hd: op[x, yy] = sp[sx, y]
    return out
def despeckle(img, min_px=10):
    """drop leftover bits: any connected clump of fewer than `min_px` pixels (a drool drop the bend cut loose, a paw fragment, a lone shading pixel)"""
    p = img.load(); Wd, Hd = img.size; seen = set()
    for y0 in range(Hd):
        for x0 in range(Wd):
            if (x0, y0) in seen or not p[x0, y0][3]: continue
            comp = [(x0, y0)]; seen.add((x0, y0)); k = 0
            while k < len(comp):
                x, y = comp[k]; k += 1
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        q = (x + dx, y + dy)
                        if 0 <= q[0] < Wd and 0 <= q[1] < Hd and q not in seen and p[q][3]: seen.add(q); comp.append(q)
            if len(comp) < min_px:
                for q in comp: p[q] = (0, 0, 0, 0)
    return img
def gallop(n=N):
    out = []
    W, H, L, T = 66, 50, 8, 10
    for i in range(n):
        ph = 2 * math.pi * i / n
        c = math.cos(ph); ch = math.cos(ph - 0.9); cf2 = math.cos(ph - 0.35); ch2 = math.cos(ph - 1.35)
        def fore(cc): return 46 * cc - 4, -48 * max(0.0, -cc) - 8 * max(0.0, cc)
        def hind(cc): return -44 * cc + 8, -34 * max(0.0, -cc) + 10 * max(0.0, cc)
        rise = -int(round(3 * (math.cos(2 * ph) + 1) / 2)) + 1
        g = max(0.0, -c)
        stretch = 1.0 + 0.09 * c
        dyb, dyf = -int(round(1.8 * g)), int(round(0.8 * g)) - int(round(0.8 * max(0.0, c)))   # arched over the hips when gathered, the head reaching down-forward when stretched
        dxf = int(round((27 - CX) * (stretch - 1) + 0)); dxb = int(round((12 - CX) * (stretch - 1)))
        b = bend(full, stretch, dyf, dyb)
        f = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        for name, cc, fn in (("far_hind", ch2, hind), ("far_fore", cf2, fore)):
            u, l = fn(cc); hindleg = "hind" in name
            M.pose_leg(f, legs[name], u * 0.95, l, L + (dxb if hindleg else dxf), T + rise + (dyb if hindleg else dyf), 0)
        f.alpha_composite(b, (L, T + rise))
        for name, cc, fn in (("near_hind", ch, hind), ("near_fore", c, fore)):
            u, l = fn(cc); hindleg = "hind" in name
            M.pose_leg(f, legs[name], u, l, L + (dxb if hindleg else dxf), T + rise + (dyb if hindleg else dyf), 0)
        out.append(m.outline(despeckle(f)))
    return out
fr = gallop()
w, h = fr[0].size
walk = M.pallid_walk3(body, jaw, legs)
sc = 8
def strip(frames, name):
    s = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames): s.alpha_composite(f, (i * w, 0))
    return s
# review GIF: walk vs run side by side, with a ground line
gifs = []; durs = []
def frame_at(wi, ri):
    im = Image.new("RGBA", (w * sc * 2 + 30, h * sc + 20), (42, 38, 32, 255))
    im.alpha_composite(Image.new("RGBA", (w * sc, h * sc), (52, 48, 40, 255)), (10, 10)); im.alpha_composite(Image.new("RGBA", (w * sc, h * sc), (52, 48, 40, 255)), (20 + w * sc, 10))
    im.alpha_composite(walk[wi].resize((w * sc, h * sc), Image.NEAREST), (10, 10)); im.alpha_composite(fr[ri].resize((w * sc, h * sc), Image.NEAREST), (20 + w * sc, 10))
    return im.convert("P", palette=Image.ADAPTIVE)
WALK_MS, RUN_MS = 110, 42                  # the run is 30% faster than the walk's 110 ms a frame (Yan, 2026-11-09)
prev = None
for tm in range(0, 8 * 110 * 17, 5):       # a common timeline, so each side keeps its own speed
    key = ((tm // WALK_MS) % len(walk), (tm // RUN_MS) % len(fr))
    if key != prev: gifs.append(frame_at(*key)); durs.append(5); prev = key
    else: durs[-1] += 5
gifs[0].save(sys.argv[1] + "/hound_run_vs_walk.gif", save_all=True, append_images=gifs[1:], duration=durs, loop=0)
sheet = Image.new("RGBA", (w * 4 * N // 2, (h * 4 + 10) * 2), (42, 38, 32, 255))
for i, f in enumerate(fr):
    sheet.alpha_composite(f.resize((w * 4, h * 4), Image.NEAREST), ((i % (N // 2)) * w * 4, (i // (N // 2)) * (h * 4 + 10)))
sheet.save(sys.argv[1] + "/hound_run_sheet.png")
strip(fr, "run").save(sys.argv[1] + "/hound_run_strip.png")
