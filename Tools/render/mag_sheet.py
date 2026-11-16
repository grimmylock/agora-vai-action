"""Magazine cut review (2026-11-16): for each double-size gun with a *_hdmag.png: the gun with the magazine tinted red (visible cuts) or shown behind it (hidden ones), the points (well = cyan, centre = yellow), and the magazine alone at 6x.
Usage: python3 Tools/render/mag_sheet.py out_prefix key [key ...]"""
import os, re, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")); W = ROOT + "/Assets/Resources/Sprites/Weapons/"
src = open(ROOT + "/Assets/Scripts/GameCore/HeldWeapon.cs").read()
D = {k: float(v) for k, v in re.findall(r'\["(\w+)"\] = ([0-9.]+)f', re.search(r"Dense = new Dictionary<string, float>\s*\{(.*?)\};", src, re.S).group(1))}
PT = {k: [float(x.rstrip('f')) for x in row.split(',')] for k, row in re.findall(r'\["(\w+)"\] = new\[\] \{ ([0-9., f]*) \}', open(ROOT + "/Assets/Scripts/GameCore/WeaponMags.cs").read())}
def tile(k, S=3):
    gun = Image.open(W + f"weapon_{k}_hd.png").convert("RGBA"); mag = Image.open(W + f"weapon_{k}_hdmag.png").convert("RGBA"); hid = not os.path.exists(W + f"weapon_{k}_hdbody.png")
    body = gun if hid else Image.open(W + f"weapon_{k}_hdbody.png").convert("RGBA"); c = Image.new("RGBA", gun.size, (84, 92, 80, 255))
    if hid: c.alpha_composite(mag, (0, 0)); c.alpha_composite(gun)
    else:
        c.alpha_composite(body); ov = np.array(mag); ov[..., :3] = (255, 60, 60); ov[..., 3] = (ov[..., 3] > 0) * 150; c.alpha_composite(Image.fromarray(ov))
    c = c.resize((c.width * S, c.height * S), Image.NEAREST); d = ImageDraw.Draw(c); p = PT[k]; kk = D[k]
    for (x, y, col) in ((p[0], p[1], (0, 255, 255)), (p[2], p[3], (255, 255, 0))): d.ellipse((x * kk * S - 5, y * kk * S - 5, x * kk * S + 5, y * kk * S + 5), outline=col, width=2)
    bb = mag.getbbox(); m = mag.crop(bb); m = m.resize((m.width * 6, m.height * 6), Image.NEAREST)
    return c, m
def main(prefix, keys):
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15); ts = [(k,) + tile(k) for k in keys]
    Wd = max(t[1].width + t[2].width + 20 for t in ts); H = sum(max(t[1].height, t[2].height) + 24 for t in ts); sh = Image.new("RGB", (Wd, H), (27, 29, 31)); y = 0; dr = ImageDraw.Draw(sh)
    for k, c, m in ts:
        dr.text((4, y + 2), f"{k}  points {PT[k]}", font=font, fill=(255, 255, 0)); sh.paste(c.convert("RGB"), (4, y + 22)); mm = m.convert("RGBA"); sh.paste(mm.convert("RGB"), (c.width + 16, y + 22), mm); y += max(c.height, m.height) + 24
    sh.save(prefix + ".png"); print(prefix + ".png", sh.size)
if __name__ == "__main__": main(sys.argv[1], sys.argv[2:])
