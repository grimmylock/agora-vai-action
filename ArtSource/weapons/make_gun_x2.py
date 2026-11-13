#!/usr/bin/env python3
"""Makes a gun's DOUBLE-SIZE picture (the standard since 2026-11-13, rules: ArtSource/weapons/WEAPON_SPRITE_RULES.md) from a SpriteCook still.
   python3 ArtSource/weapons/make_gun_x2.py <Key> <still.png> [--lift TOP GAMMA] [--thicken X0 N] [--dry]
 - crops the still to the gun, area-average downscales it to the length given by the REAL gun's size (gun_real_sizes.json: 102 px x (real cm / 29) ^ 0.28, capped at 156), 16 colours, a 1 px dark outline (14,14,18);
 - --lift TOP GAMMA: tone lift for black guns (maps the 2..99 percentile of the brightness to 22..TOP with the gamma);  --thicken X0 N: add N rows of barrel body colour to every column from X0 that is 3 px or thinner;
 - writes Assets/Resources/Sprites/Weapons/weapon_<Key>_hd.png as a 160x64 canvas, gun centred (the old 64x32 one is backed up in ArtSource/weapons/gun_x2_backup/);
 - prints the lines to paste into HeldWeapon.cs: WeaponArt.Dense (k = new width / old in-hand width) and the Grips row in VIRTUAL pixels (texture px / k), mapped from the old row by position on the gun."""
import sys, os, json, shutil
import numpy as np
from PIL import Image
from scipy import ndimage as nd
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(HERE)); WP = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Weapons")
OUTLINE = (14, 14, 18); CANVAS = (160, 64)
def bbox(im): a = np.array(im.convert("RGBA"))[..., 3] > 0; ys, xs = np.where(a); return int(xs.min()), int(ys.min()), int(xs.max() + 1), int(ys.max() + 1)
def lift(im, top, gamma):
    a = np.array(im).astype(float); m = a[..., 3] > 0; lum = a[..., :3].mean(2); lo, hi = np.percentile(lum[m], [2, 99]); t = np.clip((lum - lo) / (hi - lo), 0, 1) ** gamma
    new = 22 + (top - 22) * t; a[..., :3] = np.clip(a[..., :3] * np.where(lum > 0, new / np.maximum(lum, 1), 1)[..., None], 0, 255); return Image.fromarray(a.astype(np.uint8))
def shrink(im, tw, th, ncol=16):
    x = np.array(im).astype(float); al = x[..., 3] / 255.0; rgb = x[..., :3] * al[..., None]
    def box(arr): return np.array(Image.fromarray(arr.astype(np.float32), mode="F").resize((tw, th), Image.BOX))
    cov = box(al); col = np.stack([box(rgb[..., c]) for c in range(3)], -1) / np.maximum(cov[..., None], 1e-3)
    mask = cov > 0.5; out = np.zeros((th, tw, 4), np.uint8); out[..., :3] = np.clip(col, 0, 255).astype(np.uint8); out[..., 3] = mask * 255
    q = Image.fromarray(out[..., :3]).quantize(colors=ncol, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"); out[..., :3] = np.array(q); out[~mask, :3] = 0
    edge = mask & ~nd.binary_erosion(mask, structure=np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]])); out[edge, :3] = OUTLINE; return Image.fromarray(out)
def thicken(im, x0, n):
    a = np.array(im); h, w = a.shape[:2]; out = np.zeros((h + n, w, 4), np.uint8); out[:h] = a
    body = None
    for x in range(x0, w):
        r = np.where(a[:, x, 3] > 0)[0]
        if len(r) == 0 or r.max() - r.min() + 1 > 3: continue
        t = r.min(); out[t + 1 + n: h + n, x] = a[t + 1: h, x]; out[t + 1: t + 1 + n, x] = 0
        if body is None:                                                       # the barrel's body colour: the first non-outline pixel found in a thin column, else steel blue
            mid = [tuple(a[y, x, :3]) for y in r if tuple(a[y, x, :3]) != OUTLINE]; body = mid[0] if mid else (61, 70, 88)
    body = body or (61, 70, 88)
    for x in range(x0, w):
        r = np.where(a[:, x, 3] > 0)[0]
        if len(r) and r.max() - r.min() + 1 <= 3: t = r.min(); out[t + 1: t + 1 + n, x] = (*body, 255)
    return Image.fromarray(out)
def auto_target(key):
    """a gun not yet in gun_x2_targets.json: read its OLD sizes from the game files (the 64x32 menu picture's content width, the in-hand picture's canvas, the old Grips row in HeldWeapon.cs) and store them, so a re-run never doubles twice"""
    import re
    hd = Image.open(os.path.join(WP, f"weapon_{key}_hd.png")).convert("RGBA"); assert hd.size == (64, 32), f"{key}: the menu picture is {hd.size}, not the old 64x32: already reworked? add it to gun_x2_targets.json by hand"
    mw = bbox(hd); ih = Image.open(os.path.join(WP, f"weapon_{key}.png")).convert("RGBA"); ib = bbox(ih)
    src = open(os.path.join(ROOT, "Assets", "Scripts", "GameCore", "HeldWeapon.cs")).read(); m = re.search(r'\["%s"\] = new\[\] \{ ([^}]*) \}' % key, src)
    if not m: sys.exit(f"{key}: no Grips row in HeldWeapon.cs")
    g = [None if t.strip() == "float.NaN" else float(t.strip().rstrip("f")) for t in m.group(1).split(",")]
    return {"menu_w": mw[2] - mw[0], "inhand": [ib[2] - ib[0], ib[3] - ib[1]], "grips": g}
def target_width(key, tg):
    """the picture length in px from the REAL gun's length (gun_real_sizes.json): 102 * (cm / 29) ** 0.28, capped at 156; a gun without a real length falls back to 2 x the old menu width"""
    rs = json.load(open(os.path.join(HERE, "gun_real_sizes.json")))
    if key in rs: return min(156, int(round(102 * (rs[key][0] / 29.0) ** 0.28)))
    return int(round(tg["menu_w"] * 2 * tg.get("size", 1.0)))
def main():
    args = sys.argv[1:]
    if len(args) < 2: print(__doc__); return
    key, still = args[0], args[1]; dry = "--dry" in args
    tfile = os.path.join(HERE, "gun_x2_targets.json"); alltg = json.load(open(tfile)); tg = alltg.get(key)
    if tg is None:
        tg = auto_target(key); alltg[key] = tg
        if not dry: json.dump(alltg, open(tfile, "w"), indent=1)
        print(f"  (read the old sizes of {key} from the game files: {tg})")
    sp = Image.open(still).convert("RGBA"); x0, y0, x1, y1 = bbox(sp); sp = sp.crop((x0, y0, x1, y1))
    if "--lift" in args: i = args.index("--lift"); sp = lift(sp, float(args[i + 1]), float(args[i + 2]))
    tw = target_width(key, tg); th = int(round(sp.height * tw / sp.width)); img = shrink(sp, tw, th)
    if "--thicken" in args: i = args.index("--thicken"); img = thicken(img, int(args[i + 1]), int(args[i + 2])); th = img.height
    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0)); nx0 = (CANVAS[0] - tw) // 2; ny0 = (CANVAS[1] - th) // 2; canvas.alpha_composite(img, (nx0, ny0))
    k = tw / float(tg["inhand"][0]); ow, oh = tg["inhand"]; g = tg["grips"]; v = []
    for i in range(0, 6, 2):
        if g[i] is None: v += [None, None]; continue
        v += [round((nx0 + g[i] / ow * tw) / k, 1), round((ny0 + g[i + 1] / oh * th) / k, 1)]     # by position on the gun, then to virtual px (texture px / k)
    row = ", ".join("float.NaN" if t is None else f"{t:g}f" for t in v)
    print(f"{key}: content {tw}x{th} on a {CANVAS[0]}x{CANVAS[1]} canvas at ({nx0},{ny0}); density k = {k:.2f}")
    print(f'  WeaponArt.Dense:   ["{key}"] = {k:.2f}f,'); print(f'  WeaponArt.Grips:   ["{key}"] = new[] {{ {row} }},')
    if dry: return
    dst = os.path.join(WP, f"weapon_{key}_hd.png"); bk = os.path.join(HERE, "gun_x2_backup"); os.makedirs(bk, exist_ok=True); bp = os.path.join(bk, f"{key}_hd_old.png")
    if os.path.exists(dst) and not os.path.exists(bp): shutil.copy(dst, bp)
    canvas.save(dst); print("  wrote", dst)
if __name__ == "__main__": main()
