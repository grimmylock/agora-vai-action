"""Bakes the eel for Unity (Assets/Resources/Sprites/Fx/boss_eel_body_s0..3.png and boss_eel_head_s0..3.png): the body as a STRAIGHT strip (the real painter run along a horizontal line: column i = spine
sample i of 1120, one pixel each, the head end on the left, the back on top; 80 px tall, the body centred) that the game cuts into thin slices and lays along the spine, and the head as a profile sprite
(facing left) with its anchor. One per wound stage (0 clean ... 3 open wounds). python3 ArtSource/bosses/eel_bake_unity.py"""
import sys, os, json
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import eel_poses as E, eel_anim as A
NS, H, X0, YC = 1120, 80, 24, 40
box = None; info = {}
for st in range(4):
    E.set_stage(st)
    layer = np.zeros((H, NS + 2 * X0, 4), np.uint8); P = np.array([(X0 + i, float(YC)) for i in range(NS)])
    E.paint(layer, P, E.body_order(NS)); layer = E.outline_layer(layer)
    body = layer[:, X0:X0 + NS]; A.write_strip(f"body_s{st}", [body], {})
    cv = np.zeros((220, 260, 4), np.uint8); at = (130.0, 110.0); E.put_head(cv, at, -1); cv = E.outline_layer(cv)
    if box is None:
        ys, xs = np.nonzero(cv[:, :, 3] > 0); box = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
    hd = cv[box[1]:box[3], box[0]:box[2]]; A.write_strip(f"head_s{st}", [hd], {})
    info = dict(head_w=int(hd.shape[1]), head_h=int(hd.shape[0]), anchor_x=float(at[0] - box[0]), anchor_y=float(at[1] - box[1]), body_w=NS, body_h=H, body_cy=YC)
print(json.dumps(info))
