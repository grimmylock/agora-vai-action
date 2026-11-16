#!/usr/bin/env python3
"""A cut-out rig for the Elder Gaunt's NEAR ARM (2026-11-16; Yan: "try the cut-out rig on the Elder's claw"). Every pixel of every frame is a pixel of the clean SpriteCook still: the near arm is cut into three rigid
pieces (upper arm, forearm, hand) that turn about the shoulder, the elbow and the wrist (nearest-neighbour, whole pixels); the hole the arm leaves in the body is filled from its neighbours (the pixel nearest to each
missing one); the whole body then leans by row shear (gaunt_codeframes.shear) for the lunge. Neutral pose = the still exactly (check_neutral).
   python3 ArtSource/gaunt_cutout_rig.py"""
import os, sys, math, json
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gaunt_codeframes as C
HERE = os.path.dirname(os.path.abspath(__file__)); SC = os.path.join(HERE, 'mutants_anim', 'gaunt', 'spritecook')
STILL = os.path.join(SC, 'redesign_v1_arms.png')
NEAR = [(69, 40), (70, 56), (72, 70), (75, 82), (79, 94), (82, 106), (85, 116), (87, 126)]; NEAR_R = [5, 5, 5, 5, 5, 5, 8, 10]
SHOULDER, ELBOW, WRIST = (69, 40), (75, 83), (83, 107)
def seg_dist(px, py, A, B):
    vx, vy = B[0] - A[0], B[1] - A[1]; u = np.clip(((px - A[0]) * vx + (py - A[1]) * vy) / max(1e-6, vx * vx + vy * vy), 0, 1); return np.hypot(px - A[0] - u * vx, py - A[1] - u * vy)
class Rig:
    def __init__(self):
        self.im = Image.open(STILL).convert('RGBA'); a = np.array(self.im); H, W = a.shape[:2]; yy, xx = np.mgrid[0:H, 0:W]
        d = np.full((H, W), 1e9); idx = np.zeros((H, W), int)
        for i in range(len(NEAR) - 1):
            di = seg_dist(xx, yy, NEAR[i], NEAR[i + 1]) - (NEAR_R[i] + NEAR_R[i + 1]) / 2
            better = di < d; d = np.where(better, di, d); idx = np.where(better, i, idx)
        self.arm = (d <= 0) & (a[..., 3] > 0)
        self.piece = np.where(idx <= 3, 0, np.where(idx <= 5, 1, 2))                                  # 0 upper arm, 1 forearm, 2 hand
        # the body behind the arm: each missing pixel takes the nearest pixel that is not arm (transparent stays transparent when that is what is nearest)
        keep = ~self.arm; ind = ndi.distance_transform_edt(~keep, return_distances=False, return_indices=True)
        body = a.copy(); body[self.arm] = a[ind[0][self.arm], ind[1][self.arm]]; self.body = Image.fromarray(body)
        self.pieces = []
        for k in range(3):
            p = np.zeros_like(a); m = self.arm & (self.piece == k); p[m] = a[m]; self.pieces.append(Image.fromarray(p))
        self.W, self.H = W, H
    @staticmethod
    def rot(p, piv, deg):
        r = math.radians(deg); dx, dy = p[0] - piv[0], p[1] - piv[1]                                   # PIL's rotate is counter-clockwise for positive degrees; screen y points down
        return (piv[0] + dx * math.cos(r) + dy * math.sin(r), piv[1] - dx * math.sin(r) + dy * math.cos(r))
    def pose(self, sh, el, wr):
        """angles (degrees, + = the arm swings forward/up-forward, - = back) of the upper arm, forearm (relative) and hand (relative)"""
        pad = 40; canvas = Image.new('RGBA', (self.W + 2 * pad, self.H + 2 * pad), (0, 0, 0, 0)); canvas.alpha_composite(self.body, (pad, pad))
        s0 = (SHOULDER[0] + pad, SHOULDER[1] + pad); e0 = (ELBOW[0] + pad, ELBOW[1] + pad); w0 = (WRIST[0] + pad, WRIST[1] + pad)
        a1 = sh; a2 = sh + el; a3 = sh + el + wr
        e1 = self.rot(e0, s0, a1); w1 = self.rot(self.rot(w0, e0, a2 - a1 if False else 0) if False else w0, s0, a1)
        # chain: the elbow turns with the upper arm; the wrist turns with the upper arm and the forearm
        e1 = self.rot(e0, s0, a1)
        w_after_fore = self.rot(w0, e0, el); w1 = self.rot(w_after_fore, s0, a1)
        def piece(img, piv, ang_total, new_piv):
            pad_img = Image.new('RGBA', canvas.size, (0, 0, 0, 0)); pad_img.alpha_composite(img, (pad, pad))
            r = pad_img.rotate(ang_total, resample=Image.NEAREST, center=piv)                          # turn about its own joint, then slide the joint to where the chain put it
            dx, dy = int(round(new_piv[0] - piv[0])), int(round(new_piv[1] - piv[1])); out = Image.new('RGBA', canvas.size, (0, 0, 0, 0)); out.alpha_composite(r, (max(dx, 0), max(dy, 0)), (max(-dx, 0), max(-dy, 0))); return out
        canvas.alpha_composite(piece(self.pieces[0], s0, a1, s0)); canvas.alpha_composite(piece(self.pieces[1], e0, a1 + el, e1)); canvas.alpha_composite(piece(self.pieces[2], w0, a1 + el + wr, w1))
        return canvas
POSES = [(0, 0, 0), (-25, -6, -3), (-70, -10, -5), (-110, -12, -6), (-20, 8, 4), (52, 10, 5), (68, 12, 5), (22, 4, 2)]      # arm angles per frame: contact (the strike) on frame 5
MS = [110, 150, 140, 70, 70, 60, 110, 110]
def build_claw():
    r = Rig(); frames = []
    for (sh, el, wr), (lean, dx) in zip(POSES, C.CLAW):
        f = r.pose(sh, el, wr)
        a = np.array(f); ys = np.where(a[..., 3].any(1))[0]
        frames.append(C.shear(f, lean, dx))
    bb = [f.getbbox() for f in frames]; y0 = min(b[1] for b in bb); y1 = max(b[3] for b in bb)
    xc = (frames[0].getbbox()[0] + frames[0].getbbox()[2]) // 2; half = max(max(abs(b[0] - xc), abs(b[2] - xc)) for b in bb) + 2
    fs = [f.crop((xc - half, y0, xc + half, y1)) for f in frames]; W, H = fs[0].size
    strip = Image.new('RGBA', (W * 8, H), (0, 0, 0, 0))
    for i, f in enumerate(fs): strip.alpha_composite(f, (i * W, 0))
    od = os.path.join(HERE, 'mutants_anim', 'gaunt_elder'); os.makedirs(od, exist_ok=True)
    strip.save(os.path.join(od, 'elder_gaunt_claw_strip.png')); strip.save(os.path.join(od, 'elder_gaunt_claw_cutout_strip.png'))
    json.dump({'elder_gaunt': {'claw': {'frame': [W, H], 'ms': MS, 'travel': [0] * 8}}}, open(os.path.join(od, 'elder_gaunt_attacks.json'), 'w'), indent=1)
    print('claw strip', W, 'x', H); return fs
if __name__ == '__main__':
    r = Rig(); n = np.array(r.pose(0, 0, 0))[40:168, 40:168]; b = np.array(r.im); vis = (n[..., 3] > 0) | (b[..., 3] > 0); print('neutral differs from the still in', int(((n != b).any(2) & vis).sum()), 'visible pixels')
    fs = build_claw(); out = os.path.join(HERE, '..', 'maps', 'renders', 'gaunt'); os.makedirs(out, exist_ok=True)
    ims = []
    for f in fs:
        c = Image.new('RGBA', (f.width * 2 + 20, f.height * 2 + 20), (26, 14, 14, 255)); c.alpha_composite(f.resize((f.width * 2, f.height * 2), Image.NEAREST), (10, 10)); ims.append(c.convert('P', palette=Image.ADAPTIVE))
    ims[0].save(os.path.join(out, 'gaunt_elder_claw_cutout.gif'), save_all=True, append_images=ims[1:], duration=[m * 2 for m in MS], loop=0, disposal=2)
    S = 2; W, H = fs[0].size; sheet = Image.new('RGBA', (4 * (W * S + 6), 2 * (H * S + 6)), (26, 14, 14, 255))
    for i, f in enumerate(fs): sheet.alpha_composite(f.resize((W * S, H * S), Image.NEAREST), ((i % 4) * (W * S + 6), (i // 4) * (H * S + 6)))
    sheet.save(os.path.join(out, 'gaunt_elder_claw_cutout_frames.png'))
