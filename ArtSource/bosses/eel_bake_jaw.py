"""Open-mouth frames of the eel's head, cut from Yan's own drawing (no new drawing): the lower jaw is separated along the line of the mouth and swung about the hinge, so the head can be
shut, half open, as he drew it, and forced wider (the player holding the jaws apart in the jaw-hold scene). Nothing is repainted: the pixels are his, only moved (a rotation of the lower jaw's
pixels; the part of the jaw next to the hinge bends gradually so there is no tear). One strip per wound stage:
    Assets/Resources/Sprites/Fx/boss_eel_jaw_s0..3.png   - 7 frames side by side, frame i = openness [0, .2, .4, .6, .8, 1.0 (as drawn), 1.4 (forced wide)]
All frames share one canvas size and the same anchor (where the body grows from). python3 ArtSource/bosses/eel_bake_jaw.py   (prints the sizes the view needs)
"""
import sys, os, math, json
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import eel_poses as E, eel_anim as A

HINGE = (91.0, 57.0)                        # the corner of the mouth, in the head sprite (his drawing)
LINE = (-0.153, 66.4)                       # the mouth's centre line: y = LINE[0] x + LINE[1] (fitted to the maroon inside)
RAMP = 28.0                                 # the jaw bends gradually over this many pixels before the hinge
OPEN = [0.0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.4]  # openness of each frame; 1.0 = as drawn
SHUT = 15.0                                 # degrees the lower jaw swings up to shut the mouth (from as drawn)
WIDE = 9.0                                  # degrees it swings down to the forced-wide frame
PAD = 26


def angle_for(o):
    """degrees to turn the lower jaw (+ = swings up and shuts the mouth, - swings down) for openness o"""
    return SHUT * (1.0 - o) if o <= 1.0 else -WIDE * (o - 1.0) / 0.4


def jaw_frame(head, o):
    H, W = head.shape[:2]; canvas = np.zeros((H + 2 * PAD, W + 2 * PAD, 4), np.uint8)
    yy, xx = np.mgrid[0:H, 0:W]; line = LINE[0] * xx + LINE[1]
    lower_src = (yy > line) & (xx < HINGE[0] + 2) & (head[:, :, 3] > 0)
    upper = head.copy(); upper[lower_src] = 0
    ang = math.radians(angle_for(o))
    # lower jaw: every source pixel is moved (forward, so thin teeth are never dropped); the turn eases to nothing at the hinge; the few holes a turn leaves inside the solid jaw are filled from their neighbours
    sy_, sx_ = np.nonzero(lower_src); w = np.clip((HINGE[0] + 2 - sx_) / RAMP, 0.0, 1.0); a = ang * w
    rx, ry = sx_ - HINGE[0], sy_ - HINGE[1]
    dxx = HINGE[0] + rx * np.cos(a) - ry * np.sin(a); dyy = HINGE[1] + rx * np.sin(a) + ry * np.cos(a)
    ix, iy = np.rint(dxx).astype(int) + PAD, np.rint(dyy).astype(int) + PAD
    order = np.argsort(-sx_, kind="stable")                                                 # near the hinge last, so it is in front
    canvas[iy[order], ix[order]] = head[sy_[order], sx_[order]]
    solid = canvas[:, :, 3] > 0; cnt = np.zeros(solid.shape, int); acc = np.zeros(canvas.shape[:2] + (3,), int)
    for dy2, dx2 in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        sh = np.roll(np.roll(solid, dy2, 0), dx2, 1); cnt += sh; acc += np.roll(np.roll(canvas[:, :, :3], dy2, 0), dx2, 1) * sh[:, :, None]
    hole = (~solid) & (cnt >= 3)
    canvas[hole, :3] = (acc[hole] // cnt[hole][:, None]).astype(np.uint8); canvas[hole, 3] = 255
    up = np.zeros_like(canvas); up[PAD:PAD + H, PAD:PAD + W] = upper
    m = up[:, :, 3] > 0; canvas[m] = up[m]                      # the upper head in front of the lower jaw
    if o != 1.0: canvas = despeckle(canvas)                     # the forced-wide frame left a few loose tooth pixels by the jaw's tip (Yan's eye needed): bits under 10 px that touch nothing go
    return canvas


def despeckle(canvas, min_px=10):
    from scipy import ndimage as ndi
    lab, n = ndi.label(canvas[:, :, 3] > 0, structure=np.ones((3, 3)))
    if n < 2: return canvas
    sizes = ndi.sum(canvas[:, :, 3] > 0, lab, range(1, n + 1))
    for i, s in enumerate(sizes, 1):
        if s < min_px: canvas[lab == i] = 0
    return canvas


def bake():
    frames_by_stage = []; box = None
    for st in range(4):
        E.set_stage(st); head = E.HEAD.copy(); fr = [jaw_frame(head, o) for o in OPEN]
        frames_by_stage.append(fr)
        for f in fr:
            f2 = E.outline_layer(f); ys, xs = np.nonzero(f2[:, :, 3] > 0); b = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
            box = b if box is None else (min(box[0], b[0]), min(box[1], b[1]), max(box[2], b[2]), max(box[3], b[3]))
    ax, ay = E.HEAD_ANCHOR[0] + PAD - box[0], E.HEAD_ANCHOR[1] + PAD - box[1]
    for st in range(4):
        outs = [despeckle(E.outline_layer(f), 40)[box[1]:box[3], box[0]:box[2]] for f in frames_by_stage[st]]      # (a loose tooth with its outline is 15-30 px and survives the first pass: bits under 40 px that touch nothing go too)
        strip = np.concatenate(outs, axis=1); A.write_strip(f"jaw_s{st}", [strip], {})
    info = dict(frames=len(OPEN), frame_w=int(box[2] - box[0]), frame_h=int(box[3] - box[1]), anchor_x=float(ax), anchor_y=float(ay), open=OPEN)
    print(json.dumps(info)); return info


if __name__ == "__main__":
    bake()
