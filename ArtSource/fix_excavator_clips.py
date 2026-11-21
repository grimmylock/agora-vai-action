# The Excavator's Pixel Lab clips, fixed (2026-11-21; Yan: "it has ground drawn below it that doesn't match the artstyle, also the zombie inside is not one of our sprites for husks").
# No new generation: every frame of ArtSource/mutants_anim/bosses_v3/excavator/<clip>/ is rewritten in place (the originals are kept once in <clip>/orig/):
#  1. the painted ground goes: inside the ground zone (under the tracks, the rock pile left of them, the dirt right of them), every pixel that is the same as in the stand (the dirt does
#     not move) is cleared, then every pixel there in a colour found only in the ground and never on the machine (the dirt where a frame shifted it); what moves there (the bucket coming down, dust, rubble flying) stays. Specks left in the zone (< 6 px islands) are cleared too.
#  2. the driver is a real Husk: the cab window is found in each frame (the stand's window with its generated driver, matched within +-40 px), the old driver is painted out with the
#     window's own dark glass and the real Husk sprite (husk_atlas_x2.png cell 40, 1:1, never resampled) sits behind the glass, his legs hidden by the seat. Frames where the window is
#     not found (the turntable turned away) are left as they are.
#   python3 ArtSource/fix_excavator_clips.py     then  python3 ArtSource/export_boss_clips.py
import os, glob, shutil
import numpy as np
from PIL import Image
from scipy import ndimage
H = os.path.dirname(os.path.abspath(__file__)); D = os.path.join(H, "mutants_anim", "bosses_v3", "excavator")
HUSKS = os.path.join(H, "..", "Assets", "Resources", "Sprites", "husk_atlas_x2.png")
W_ = (134, 94, 160, 134)                       # the cab window and its frame in the stand (x0, y0, x1, y1)
THRESH = 60
GLASS = (136, 96, 158, 125)                    # the window's inside, above the seat
def zone(h, w):
    y, x = np.mgrid[0:h, 0:w]
    return (y >= 183) | ((x < 72) & (y >= 152)) | ((x > 222) & (y >= 168))
def husk(i=40):
    im = Image.open(HUSKS).convert("RGBA"); cols = im.width // 32
    return np.array(im.crop(((i % cols) * 32, (i // cols) * 32, (i % cols) * 32 + 32, (i // cols) * 32 + 32)))
def window_patch(stand):
    x0, y0, x1, y1 = W_; p = stand[y0:y1, x0:x1].copy(); gx0, gy0, gx1, gy1 = GLASS
    p[gy0 - y0:gy1 - y0, gx0 - x0:gx1 - x0, :3] = (8, 7, 10); p[gy0 - y0:gy1 - y0, gx0 - x0:gx1 - x0, 3] = 255
    hk = husk(); hx, hy = 146 - 16, 97                                     # the Husk's 32 px cell, centred in the window, his head near the top of the glass
    for yy in range(32):
        for xx in range(32):
            ax, ay = hx + xx, hy + yy
            if hk[yy, xx, 3] and gx0 <= ax < gx1 and gy0 <= ay < gy1: p[ay - y0, ax - x0] = hk[yy, xx]
    return p
def main():
    stand_path = os.path.join(D, "idle", "orig", "f_00.png") if os.path.exists(os.path.join(D, "idle", "orig", "f_00.png")) else os.path.join(D, "idle", "f_00.png")
    stand = np.array(Image.open(stand_path).convert("RGBA")).astype(int); h, w = stand.shape[:2]; Z = zone(h, w); patch = window_patch(stand.astype(np.uint8))
    x0, y0, x1, y1 = W_; tpl = stand[y0:y1, x0:x1]; tm = tpl[..., 3] > 0
    gx0, gy0, gx1, gy1 = GLASS; tm[gy0 - y0:gy1 - y0, gx0 - x0:gx1 - x0] = False                 # match on the window's FRAME only: the generated driver changes every frame
    q = lambda c: (c[..., 0] // 12) * 10000 + (c[..., 1] // 12) * 100 + (c[..., 2] // 12)        # colours quantised (12 levels a channel)
    yy_, xx_ = np.mgrid[0:h, 0:w]; body = (yy_ < 150) & (stand[..., 3] > 0)
    bq = set(q(stand[body]).tolist()); bq |= {v + d for v in list(bq) for d in (1, -1, 100, -100, 10000, -10000)}
    groundq = set(q(stand[Z & (stand[..., 3] > 0)]).tolist()) - bq                                 # colours found only in the painted ground, never on the machine
    for cd in sorted(glob.glob(os.path.join(D, "*", ""))):
        clip = os.path.basename(os.path.dirname(cd))
        if clip.endswith("_v1"): continue
        od = os.path.join(cd, "orig")
        if not os.path.isdir(od):
            os.makedirs(od); [shutil.copy(f, od) for f in glob.glob(os.path.join(cd, "f_*.png"))]
        found = 0; fs = sorted(glob.glob(os.path.join(od, "f_*.png")))
        for f in fs:
            a = np.array(Image.open(f).convert("RGBA")).astype(int)
            same = Z & (np.abs(a[..., :3] - stand[..., :3]).sum(-1) < 70) & (a[..., 3] > 0)     # 1. the ground: unchanged dirt in the zone
            a[same] = 0
            only = Z & (a[..., 3] > 0) & np.isin(q(a), list(groundq))                                # 1b. the ground's own colours, wherever the frame shifted it
            a[only] = 0
            lab, n = ndimage.label((a[..., 3] > 0) & Z)
            if n:
                sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
                for k, s in enumerate(sizes, 1):
                    if s < 6: a[lab == k] = 0
            best, bxy = 1e9, None                                                                # 2. the cab window, then the real Husk in it
            for dy in range(-20, 21, 1):
                for dx in range(-40, 41, 1):
                    yy, xx = y0 + dy, x0 + dx
                    if yy < 0 or xx < 0 or yy + tpl.shape[0] > h or xx + tpl.shape[1] > w: continue
                    reg = a[yy:yy + tpl.shape[0], xx:xx + tpl.shape[1]]
                    e = np.abs(reg[..., :3] - tpl[..., :3]).sum(-1)[tm].mean()
                    if e < best: best, bxy = e, (xx, yy)
            if __import__("os").environ.get("EXC_DEBUG"): print(" ", clip, os.path.basename(f), round(best, 1), bxy)
            if bxy is not None and (best < THRESH or (abs(bxy[0] - x0) <= 20 and abs(bxy[1] - y0) <= 6 and best < 140)):   # near the window's own spot a looser match is enough (the generator redraws small details each frame)
                xx, yy = bxy; a[yy:yy + patch.shape[0], xx:xx + patch.shape[1]] = patch; found += 1
            Image.fromarray(a.astype(np.uint8)).save(os.path.join(cd, os.path.basename(f)))
        print(clip, len(fs), "frames, Husk placed in", found)
if __name__ == "__main__": main()
