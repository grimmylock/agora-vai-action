"""Writes Sprites/Weapons/weapon_<Key>.png (B, in hand) + weapon_<Key>_hd.png (A, menus) for every weapon,
a preview sheet, and the C# grip table for WeaponArt.Grips."""
import sys, os, math, shutil; sys.path.insert(0, '/home/claude/art/weapons')
import numpy as np
from PIL import Image
from weapon_pixel import render
from points import all_points, guess

OUT = sys.argv[1] if len(sys.argv) > 1 else '/tmp/claude-0/weapons_out'
os.makedirs(OUT, exist_ok=True)
pts = all_points()
rows = []
for n, (cat, A, B, p) in pts.items():
    render(B).save(os.path.join(OUT, f'weapon_{n}.png'))
    render(A).save(os.path.join(OUT, f'weapon_{n}_hd.png'))
    rows.append((n, cat, p))
# RK-12 Kuvalda: the hand-pixelled one from the first weapon test (outline padding included)
shutil.copy('/home/claude/art/rsh12_hand32.png', os.path.join(OUT, 'weapon_RK12Kuvalda.png'))
shutil.copy('/home/claude/art/rsh12_hand64.png', os.path.join(OUT, 'weapon_RK12Kuvalda_hd.png'))
im = Image.open('/home/claude/art/rsh12_hand32.png').convert('RGBA'); a = np.array(im)[..., 3]
g = [["1" if a[y, x] > 0 and tuple(np.array(im)[y, x, :3]) != (20, 22, 23) else "." for x in range(im.width)] for y in range(im.height)]
# guess() expects a 32-wide grid: pad/crop centrally
g32 = [row[1:33] for row in g[1:17]]
pk = guess(g32, "sidearm"); pk = {k: (v[0] + 1, v[1] + 1) if v else v for k, v in pk.items()}
rows.append(("RK12Kuvalda", "sidearm", pk))
# Batch 28: hand-pixelled pilot guns. B = drawn at the new in-hand sizes (hand/b_v2.py), A = hand-drawn menu art.
sys.path.insert(0, '/home/claude/art/weapons/hand')
import b_v2, vk74, bayou, psh18_full
from common import render as render_a
HAND_A = {"VK74": vk74.A, "BayouPump": bayou.A, "PSh18Pchela": psh18_full.A}
for n in b_v2.ALL:
    b_v2.render(b_v2.ALL[n]["rows"]).save(os.path.join(OUT, f'weapon_{n}.png'))
    if n in HAND_A: render_a(HAND_A[n]).save(os.path.join(OUT, f'weapon_{n}_hd.png'))
    gx, gy, sx, sy, mx, my = b_v2.grips(n)
    p = {"grip": (gx, gy), "sup": None if math.isnan(sx) else (sx, sy), "muzzle": (mx, my)}
    rows = [r if r[0] != n else (n, r[1], p) for r in rows]
def f(v): return "float.NaN" if v is None else f"{round(v * 2) / 2:g}f"
cs = []
for n, cat, p in rows:
    gx, gy = p["grip"]; sup = p.get("sup"); mx, my = p["muzzle"]
    sx, sy = (None, None) if sup is None else sup
    cs.append(f'            ["{n}"] = new[] {{ {f(gx)}, {f(gy)}, {f(sx)}, {f(sy)}, {f(mx)}, {f(my)} }},')
open(os.path.join(OUT, 'grips.cs.txt'), 'w').write("\n".join(cs) + "\n")
print(len(rows), "weapons ->", OUT)
