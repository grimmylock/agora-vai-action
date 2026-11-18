#!/usr/bin/env python3
"""Aiming straight up or down (Yan, 2026-11-18: "the weapons get into a weird angle covering the body"): the same soldiers with the old hand placement (the grip on the body's centre line at 90 degrees) and the new
(HeldPoseMath.MinSide: the hand stays 2.5 px out from the centre line). The game's own armour and gun sprites at 6x on a flat ground; not a Unity capture (no lighting, the back view is not drawn here: the game
shows the back and puts the gun under the body when aiming up, Batch 128).   python3 Tools/render/aim_vertical.py out.png"""
import sys, os
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import squad_scene as S
def row(min_side, angs, weapons, armor):
    S.MIN_SIDE = min_side; W = 96; sc = Image.new('RGBA', (len(angs) * W, 96), (66, 62, 54, 255))
    for i, (ang, (w, cat)) in enumerate(zip(angs, weapons)):
        f = (i * W + 48, 78); S.shadow(sc, f); S.soldier(sc, f, armor, w, cat, ang, None)
    return sc.resize((sc.width * 3, sc.height * 3), Image.NEAREST)
angs = [90, 75, 60, 0, -60, -75, -90]; ws = [('VK74', 'assault'), ('BR86Bulldog', 'assault'), ('BayouPump', 'shotgun'), ('Corvan57', 'assault'), ('BlackHarrier', 'smg'), ('VK74', 'assault'), ('BR86Bulldog', 'assault')]
old = row(0.0, angs, ws, 98); new = row(2.5, angs, ws, 98)
out = Image.new('RGBA', (old.width, old.height * 2 + 30), (30, 30, 30, 255)); out.alpha_composite(old, (0, 0)); out.alpha_composite(new, (0, old.height + 30))
d = ImageDraw.Draw(out); d.text((6, old.height + 8), 'NEW (hand kept 2.5 px out from the body)   top row = OLD', fill=(230, 220, 190, 255))
out.convert('RGB').save(sys.argv[1] if len(sys.argv) > 1 else 'aim_vertical.png'); print(out.size)
