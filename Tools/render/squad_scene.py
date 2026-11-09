#!/usr/bin/env python3
"""Stills of NPCs 'in the world' (Yan, 2026-11-09): armour sprites from the real atlases (the 32 px doubled copies), weapon sprites at the game's own scale, hands placed by a port of
HeldPoseMath.Compute (HeldWeapon.cs), over a real map render (Tools/render/render.sh bonfire_village). NOT a Unity capture: no lighting, shadows, tint or camera.
    python3 Tools/render/squad_scene.py squad|fight out.png
"""
import sys, os, math
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..')); SP = os.path.join(ROOT, 'Assets/Resources/Sprites')
_c = {}
def img(p):
    if p not in _c: _c[p] = Image.open(os.path.join(SP, p)).convert('RGBA')
    return _c[p]
def cell(atlas, idx):
    a = img(atlas + '_x2.png'); return a.crop((idx % 16 * 32, idx // 16 * 32, idx % 16 * 32 + 32, idx // 16 * 32 + 32))
GRIPS = {'assault': (12.5, 10, 22, 8.5), 'smg': (10.5, 10.5, 20, 9.5), 'shotgun': (9.5, 8.5, 14, 9), 'sidearm': (10.5, 11.5, None, None), 'sniper': (14.5, 10, 19.5, 7.5)}
REACH = {'sidearm': 4, 'melee': 4, 'smg': 3.5}
def rnd(v): return math.floor(v + 0.5)
def compute(cat, ang, g):
    a = math.radians(ang); c, s = math.cos(a), math.sin(a); left = c < -1e-4; reach = REACH.get(cat, 3)
    r = rnd(8 + reach * abs(c)); gx = 13 - r if left else r; gy = min(rnd(9.5 - reach * s), 10)
    dx, dy, px, py = c, -s, s, c
    if left: px, py = -px, -py
    if g[2] is None: ax = ay = 1
    else: ax = (g[2] - g[0]) * .5; ay = (g[3] - g[1]) * .5
    return left, gx, gy, min(rnd(gx + ax * dx + ay * px), 99), min(rnd(gy + ax * dy + ay * py), 11)
GL = {'K': (20, 22, 23, 255), 'g': (61, 65, 72, 255), 'l': (88, 94, 104, 255)}
def glove(canvas, cx, cy):        # 3x2 body px at 2x: dark outline, grey fill, one light pixel
    d = ImageDraw.Draw(canvas); x, y = cx * 2, cy * 2
    d.rectangle((x - 1, y - 1, x + 7, y + 5), fill=GL['K']); d.rectangle((x, y, x + 5, y + 3), fill=GL['g']); d.rectangle((x, y, x + 1, y + 1), fill=GL['l'])
def soldier(scene, foot, armor, weapon, cat, ang, walk=None, shoot=False, back=False):
    """foot = (x, y) of the soldier's feet in scene px; walk None/'a'/'b' picks the walk frame; ang = aim degrees (CCW from right), None = carried low"""
    body = cell('armor_atlas' if walk is None else 'armor_walk_' + walk, armor)
    ox, oy = foot[0] - 16, foot[1] - 30                    # a 32 px cell, feet about 2 px above its bottom
    g = GRIPS[cat]; wp = img('Weapons/weapon_%s.png' % weapon)
    if ang is None: ang = -20 if True else 0
    left, gx, gy, sx, sy = compute(cat, ang, g)
    scene.alpha_composite(body, (ox, oy))
    gripx = g[0] if not left else 32 - g[0]
    w = wp.transpose(Image.FLIP_LEFT_RIGHT) if left else wp
    pad = 40; big = Image.new('RGBA', (32 + pad * 2, 16 + pad * 2), (0, 0, 0, 0)); big.alpha_composite(w, (pad, pad))
    pivot = (pad + gripx, pad + g[1]); rot = ang - 180 if left else ang
    big2 = Image.new('RGBA', (200, 200), (0, 0, 0, 0)); big2.alpha_composite(big, (100 - int(pivot[0]), 100 - int(pivot[1])))
    big2 = big2.rotate(rot, resample=Image.NEAREST, center=(100, 100))
    hx, hy = ox + (gx + 1.5) * 2, oy + (gy + 1) * 2
    scene.alpha_composite(big2, (int(hx - 100), int(hy - 100)))
    if shoot:
        fx = img('Fx/fx_muzzle_%d.png' % 2); mdx = (g and (32 - g[0] if False else 0))
        mx = 32 - 0; L = 34 if not left else -34
        mp = (hx + math.cos(math.radians(ang)) * (32 - g[0] + 2), hy - math.sin(math.radians(ang)) * (32 - g[0] + 2)) if not left else (hx + math.cos(math.radians(ang)) * (g[0] * 0 + 32 - g[0] + 2), hy - math.sin(math.radians(ang)) * (32 - g[0] + 2))
        f = fx.rotate(ang if not left else ang - 180, expand=True, resample=Image.NEAREST); f = f.transpose(Image.FLIP_LEFT_RIGHT) if False else f
        scene.alpha_composite(f, (int(mp[0] - f.width / 2), int(mp[1] - f.height / 2)))
    if g[2] is not None or True:
        glove(scene, ox / 2 + sx - 0 if False else 0, 0) if False else None
    sx2, sy2 = ox + (sx + 1.5 - 1.5) * 2, oy + sy * 2
    gx2, gy2 = ox + gx * 2, oy + gy * 2
    for (x, y) in ([(sx2, sy2)] if True else []) + [(gx2, gy2)]:
        d = ImageDraw.Draw(scene); d.rectangle((x - 1, y - 1, x + 7, y + 5), fill=GL['K']); d.rectangle((x, y, x + 5, y + 3), fill=GL['g']); d.rectangle((x, y, x + 1, y + 1), fill=GL['l'])
def shadow(scene, foot):
    sh = Image.new('RGBA', scene.size, (0, 0, 0, 0)); ImageDraw.Draw(sh).ellipse([foot[0] - 11, foot[1] - 4, foot[0] + 11, foot[1] + 4], fill=(18, 16, 12, 100)); scene.alpha_composite(sh)
def world(at=(1700, 1580), tiles=(18, 8)):
    m = Image.open(os.path.join(HERE, '_render/bonfire_village.png')).convert('RGBA'); cx, cy = at; tw, th = tiles
    return m.crop((cx - tw * 8, cy - th * 8, cx + tw * 8, cy + th * 8)).resize((tw * 32, th * 32), Image.NEAREST)
if __name__ == '__main__':
    mode, out = sys.argv[1], sys.argv[2]; sc = world(); H = sc.height
    if mode == 'squad':
        y0 = H // 2 + 20
        crew = [(44, 'BR86Bulldog', 'assault', 'a'), (38, 'Corvan57', 'assault', None), (30, 'BayouPump', 'shotgun', 'b'), (41, 'BlackHarrier', 'smg', 'a')]
        for i, (a, w, c, wk) in enumerate(crew):
            f = (228 + (i % 2) * 34 - 17, 60 + i * 56); shadow(sc, f); soldier(sc, f, a, w, c, -72 - i * 6, wk)
    else:
        y0 = H // 2 + 40
        A = [(44, 'BR86Bulldog', 'assault', 'a', 0), (38, 'BayouPump', 'shotgun', None, 8)]
        B = [(60, 'Corvan57', 'assault', 'b', 180), (66, 'BlackHarrier', 'smg', None, 172)]
        for i, (a, w, c, wk, an) in enumerate(A):
            f = (130 + i * 70, y0 + i * 26); shadow(sc, f); soldier(sc, f, a, w, c, an, wk, shoot=(i == 0))
        for i, (a, w, c, wk, an) in enumerate(B):
            f = (sc.width - 150 - i * 70, y0 + i * 22 - 10); shadow(sc, f); soldier(sc, f, a, w, c, an, wk, shoot=(i == 0))
    sc = sc.resize((sc.width * 2, sc.height * 2), Image.NEAREST); sc.convert('RGB').save(out); print(out, sc.size)
