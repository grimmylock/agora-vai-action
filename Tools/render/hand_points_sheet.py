"""Hand points review (2026-11-16): each gun's double-size picture at 3x with its three points from WeaponArt.Grips (virtual px x density k = texture px):
cyan ring = grip hand, magenta ring = support hand (none for one-handers), yellow = muzzle. Ring size = the glove (about 4 virtual px across). Usage: hand_points_sheet.py out_prefix [per_page]"""
import os, re, sys
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
src = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read()
dm = re.search(r"Dense = new Dictionary<string, float>\s*\{(.*?)\};", src, re.S); D = {k: float(v) for k, v in re.findall(r'\["(\w+)"\] = ([0-9.]+)f', dm.group(1))}
gm = re.search(r"Dictionary<string, float\[\]> Grips = new Dictionary<string, float\[\]>\s*\{(.*?)\n        \};", src, re.S); G = {}
for k, row in re.findall(r'\["(\w+)"\] = new\[\] \{([^}]*)\}', gm.group(1)):
    G[k] = [float('nan') if 'NaN' in v else float(v.strip().rstrip('f')) for v in row.split(',')]
def tile(k, S=3):
    im = Image.open(os.path.join(ROOT, f"Assets/Resources/Sprites/Weapons/weapon_{k}_hd.png")).convert("RGBA"); d = D[k]; g = G[k]
    bb = im.getbbox(); x0 = max(0, bb[0] - 6); x1 = min(im.width, bb[2] + 6); y0 = max(0, bb[1] - 6); y1 = min(im.height, bb[3] + 6)
    c = Image.new("RGBA", im.size, (84, 92, 80, 255)); c.alpha_composite(im); c = c.crop((x0, y0, x1, y1)).resize(((x1 - x0) * S, (y1 - y0) * S), Image.NEAREST); dr = ImageDraw.Draw(c)
    def pt(x, y, col, r, lab):
        if x != x: return
        X = (x * d - x0 + 0.5) * S; Y = (y * d - y0 + 0.5) * S; R = r * d * S
        dr.ellipse((X - R, Y - R, X + R, Y + R), outline=col, width=1); dr.line((X - 6, Y, X + 6, Y), fill=col, width=2); dr.line((X, Y - 6, X, Y + 6), fill=col, width=2); dr.text((X + R + 2, Y - 8), lab, fill=col)
    for v in range(0, int(im.width / d) + 1):                                   # a grid in VIRTUAL px (the units of the Grips rows): every 5th labelled
        X = (v * d - x0) * S; 
        if 0 <= X < c.width: dr.line((X, 0, X, c.height), fill=(255, 255, 255, 60) if v % 5 else (255, 255, 255, 140), width=1); 
        if v % 5 == 0 and 0 <= X < c.width: dr.text((X + 2, 0), str(v), fill=(255, 255, 160))
    for v in range(0, int(im.height / d) + 1):
        Y = (v * d - y0) * S
        if 0 <= Y < c.height: dr.line((0, Y, c.width, Y), fill=(255, 255, 255, 60) if v % 5 else (255, 255, 255, 140), width=1)
        if v % 5 == 0 and 0 <= Y < c.height: dr.text((0, Y + 1), str(v), fill=(255, 255, 160))
    pt(g[0], g[1], (0, 255, 255), 2, "grip"); pt(g[2], g[3], (255, 0, 255), 2, "sup"); pt(g[4], g[5], (255, 255, 0), 0.6, "muzzle")
    return c
def main(prefix, per=7):
    keys = [k for k in D if k in G]; font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
    for p in range(0, len(keys), per):
        ts = [(k, tile(k)) for k in keys[p:p + per]]; W = max(t.width for _, t in ts) + 8; H = sum(t.height + 22 for _, t in ts)
        sh = Image.new("RGB", (W, H), (27, 29, 31)); y = 0; dr = ImageDraw.Draw(sh)
        for k, t in ts: dr.text((4, y + 2), f"{k}  k={D[k]:.2f}  grip={G[k][0]},{G[k][1]}  sup={G[k][2]},{G[k][3]}  muzzle={G[k][4]},{G[k][5]}", font=font, fill=(255, 255, 0)); sh.paste(t.convert("RGB"), (4, y + 20)); y += t.height + 22
        sh.save(f"{prefix}_{p // per:02d}.png"); print(f"{prefix}_{p // per:02d}.png", [k for k, _ in ts])
if __name__ == "__main__": main(sys.argv[1], int(sys.argv[2]) if len(sys.argv) > 2 else 7)
