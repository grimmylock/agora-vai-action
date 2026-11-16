"""Optic mounts review (2026-11-16): every eligible gun with its biggest optic on the rail and, on sidearms, the laser underneath (3x), from the generated OpticMounts.cs.
Usage: python3 Tools/render/optic_sheet.py out_prefix [per_page]"""
import os, re, sys
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, ROOT + "/ArtSource/weapons"); import mounts as M
PIV = {'pu2': (31, 16), 'acog': (19, 19), 'scope8': (41, 19), 'reddot': (13, 16), 'laser': (11, 1)}   # texture px, from attachments.py
cs = open(ROOT + "/Assets/Scripts/GameData/OpticMounts.cs").read()
MT = {k: [float(v.rstrip('f')) for v in row.split(',')] for k, row in re.findall(r'\["(\w+)"\] = new\[\] \{ ([0-9., f]*) \}', cs.split("Allowed")[0])}
def tile(k, S=3):
    g0 = Image.open(M.W + f"weapon_{k}_hd.png").convert("RGBA"); PAD = 30; im = Image.new("RGBA", (g0.width, g0.height + PAD)); im.alpha_composite(g0, (0, PAD)); c = Image.new("RGBA", im.size, (84, 92, 80, 255)); c.alpha_composite(im); t = [MT[k][0], MT[k][1] + PAD, MT[k][2], MT[k][3] + PAD]; al = M.ALLOWED[k]
    def put(att, x, y):
        a = Image.open(ROOT + f"/Assets/Resources/Sprites/Attachments/att_{att}.png").convert("RGBA"); p = PIV[att]; c.alpha_composite(a, (int(x - p[0]), int(y - p[1])))
    opt = [x for x in al if x != 'laser']
    if opt: put(opt[-1], t[0], t[1])
    if 'laser' in al: put('laser', t[2], t[3])
    bb = im.getbbox(); x0 = max(0, bb[0] - 8); x1 = min(im.width, bb[2] + 8); y0 = max(0, bb[1] - 26); y1 = min(im.height, bb[3] + 8)
    return c.crop((x0, y0, x1, y1)).resize(((x1 - x0) * S, (y1 - y0) * S), Image.NEAREST)
def main(prefix, per=8):
    keys = list(M.ALLOWED); font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
    for p in range(0, len(keys), per):
        ts = [(k, tile(k)) for k in keys[p:p + per]]; W = max(t.width for _, t in ts) + 8; H = sum(t.height + 22 for _, t in ts); sh = Image.new("RGB", (W, H), (27, 29, 31)); y = 0; dr = ImageDraw.Draw(sh)
        for k, t in ts: dr.text((4, y + 2), f"{k}  {M.ALLOWED[k]}  mount={MT[k]}", font=font, fill=(255, 255, 0)); sh.paste(t.convert("RGB"), (4, y + 20)); y += t.height + 22
        sh.save(f"{prefix}_{p // per:02d}.png"); print(f"{prefix}_{p // per:02d}.png", [k for k, _ in ts])
if __name__ == "__main__": main(sys.argv[1], int(sys.argv[2]) if len(sys.argv) > 2 else 8)
