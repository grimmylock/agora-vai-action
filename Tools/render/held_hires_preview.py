"""Preview (2026-11-13): what a gun looks like in the hands when its in-hand picture is the big MENU picture drawn smaller (a per-gun scale), next to the current tiny one. Uses the game's own pose frames
(`dotnet /tmp/test.dll heldframes frames.json`, here only the Rinaldi) exactly like held_anim.py; the big sprite is shrunk by nearest-neighbour sampling to the on-screen size of the old one (what point filtering does).
No reload pieces for the big one (the cylinder swing-out has no big pieces yet: the whole gun is drawn).   Usage: python3 Tools/render/held_hires_preview.py frames.json out.png"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as H
S = H.S; SPR = H.SPR
def paste2(c, img, px, py, at_x, at_y, deg, flip, k):
    """like held_anim.paste but one sprite pixel = S/2/k screen pixels (k = how many times denser the big sprite is)"""
    g = img.transpose(Image.FLIP_TOP_BOTTOM) if flip else img
    if flip: py = g.height - py
    sc = S / 2.0 / k; g = g.resize((max(1, round(g.width * sc)), max(1, round(g.height * sc))), Image.NEAREST)
    pad = max(g.size) * 2; big = Image.new("RGBA", (pad * 2, pad * 2)); big.alpha_composite(g, (pad - round(px * sc), pad - round(py * sc)))
    big = big.rotate(deg, center=(pad, pad), resample=Image.NEAREST); c.alpha_composite(big, (round((H.BX + at_x) * S) - pad, round((H.BY + at_y) * S) - pad))
def frame_new(anim, f, body, gun, gl, grip, k):
    gx_, gy_, deg, hx, hy, sx, sy, has_sup, left = f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8]
    c = Image.new("RGBA", (H.W * S, H.H * S), H.BG); c.alpha_composite(body, (H.BX * S, H.BY * S)); paste2(c, gun, grip[0], grip[1], gx_, gy_, deg, left, k)
    gg = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
    for (x, y, on) in ((sx, sy, has_sup), (hx, hy, 1)):
        if on: c.alpha_composite(gg, (round((H.BX + x) * S - 2 * S), round((H.BY + y) * S - 1.5 * S)))
    return c
def main(src, out):
    anims = [a for a in json.load(open(src)) if a["style"] == "cylinder"]; a = anims[0]
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); i = a["armor"]
    body = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST); gl = H.glove(a["cuff"], a["glove"])
    old = Image.open(os.path.join(SPR, "Weapons", "weapon_RinaldiR6.png")).convert("RGBA")
    big = Image.open(os.path.join(SPR, "Weapons", "weapon_RinaldiR6_hd.png")).convert("RGBA")
    import numpy as np
    def bb(im): m = np.array(im)[..., 3] > 0; ys, xs = np.where(m); return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    ox0, oy0, ox1, oy1 = bb(old); nx0, ny0, nx1, ny1 = bb(big); k = (nx1 - nx0) / float(ox1 - ox0)
    gx, gy = a["gx"], a["gy"]; grip_new = ((gx - ox0) * k + nx0, (gy - oy0) * k + ny0); print("density factor %.2f, old grip (%.1f,%.1f) -> new (%.1f,%.1f)" % (k, gx, gy, *grip_new))
    parts = (Image.open(os.path.join(SPR, "Weapons", "weapon_RinaldiR6_body.png")).convert("RGBA"), Image.open(os.path.join(SPR, "Weapons", "weapon_RinaldiR6_cyl.png")).convert("RGBA"))
    n = len(a["frames"]); picks = [n - 1, 0, n // 5, n // 2, 3 * n // 4]; labels = ["rest (aiming)", "start", "20 %", "50 %", "75 %"]
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16); cw, ch = H.W * S, H.H * S
    sheet = Image.new("RGBA", (130 + len(picks) * (cw + 8), 36 + 2 * (ch + 26)), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    for r, (lab, fn) in enumerate((("CURRENT in-hand picture", lambda f: H.frame(a, f, body, old, gl, parts)), ("MENU picture drawn smaller\n(no reload pieces yet)", lambda f: frame_new(a, f, body, big, gl, grip_new, k)))):
        y = 36 + r * (ch + 26); d.text((8, y + ch // 2 - 20), lab, font=font, fill=(255, 255, 0))
        for j, p in enumerate(picks): sheet.alpha_composite(fn(a["frames"][p]), (130 + j * (cw + 8), y)); d.text((134 + j * (cw + 8), y + ch + 3), labels[j], font=font, fill=(200, 200, 200))
    d.text((8, 8), "The character holding the Rinaldi: current vs the menu picture drawn at the same on-screen size (the game's own pose code)", font=font, fill=(235, 235, 235))
    sheet.convert("RGB").save(out); print("saved", out, sheet.size)
if __name__ == "__main__": main(sys.argv[1], sys.argv[2])
