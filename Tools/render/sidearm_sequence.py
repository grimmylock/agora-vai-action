"""Approval GIFs (2026-11-16): per gun ONE shot (the gun's own parts moving: slide, hammer; shell leaving the port; hands, kick) then a pause, then the RELOAD (hands round the whole gun: the double-size guns have no cut magazine/cylinder pieces yet).
Slow motion (x2) so every pose can be judged. Usage: python3 Tools/render/sidearm_sequence.py frames.json outdir key [key ...]"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as H, held_hires_preview as P, held_hires_check as C, parts_preview as PP
S = H.S; SPR = H.SPR
def dense_frame(a, f, body, gun, gl, grip, k, magimg, bodyimg, mp, cyl=False):
    """The reload frame of a double-size gun with its magazine moving (the game's own frame record + the cut pieces): the dropped magazine on the floor or falling, the seated/carried magazine, then the gun (the body without the magazine when it was cut), then the hands."""
    gx_, gy_, deg, hx, hy, sx, sy, has_sup, left, mdx, mdy, mshown, brk, thrown, lx, ly, ldeg, landed, brass = f
    c = Image.new("RGBA", (H.W * S, H.H * S), H.BG); c.alpha_composite(body, (H.BX * S, H.BY * S))
    if magimg is not None and mp is not None and thrown:                                            # the old magazine: falling or lying on the floor (character px, centre pivot)
        P.paste2(c, magimg, mp[2] * k, mp[3] * k, lx, ly, ldeg, left, k)
    for bx, by, bdeg, rest in brass:                                                                 # spent brass a revolver dumps (character px): lying under the unit or still falling
        d = ImageDraw.Draw(c); X, Y = (H.BX + bx) * S, (H.BY + by) * S; d.rectangle((X - 2, Y - 1, X + 3, Y + 1), fill=(201, 166, 62))
    if magimg is not None and cyl: P.paste2(c, magimg, grip[0], grip[1], gx_ + mdx, gy_ + mdy, deg, left, k)             # a cylinder swung out on its crane: behind the frame (the far side), peeking out beside the window
    if magimg is not None and mshown and not cyl: P.paste2(c, magimg, grip[0], grip[1], gx_ + mdx, gy_ + mdy, deg, left, k)       # seated, or in the hand
    P.paste2(c, bodyimg if bodyimg is not None else gun, grip[0], grip[1], gx_, gy_, deg, left, k)
    gg = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
    for (x, y, on) in ((sx, sy, has_sup), (hx, hy, 1)):
        if on: c.alpha_composite(gg, (round((H.BX + x) * S - 2 * S), round((H.BY + y) * S - 1.5 * S)))
    return c
def one(src, key, outdir, slow=2.0):
    big, box = PP.main(src, key, os.path.join(outdir, "tmp_shot_" + key))
    allan = json.load(open(src)); rel = [x for x in allan if x["key"] == key and x["style"] not in ("jam", "shot")][0]; shot = [x for x in allan if x["key"] == key and x["style"] == "shot"][0]
    D = C.dense(); k = D[key]; atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); i = rel["armor"]
    body = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST); gl = H.glove(rel["cuff"], rel["glove"])
    gun = Image.open(os.path.join(SPR, "Weapons", f"weapon_{key}_hd.png")).convert("RGBA"); grip = (rel["gx"] * k, rel["gy"] * k)
    wp = os.path.join(SPR, "Weapons"); mgp = os.path.join(wp, f"weapon_{key}_hdmag.png"); bdp = os.path.join(wp, f"weapon_{key}_hdbody.png")
    cyp = os.path.join(wp, f"weapon_{key}_hdcyl.png"); iscyl = rel["style"] == "cylinder" and os.path.exists(cyp)
    magimg = Image.open(cyp if iscyl else mgp).convert("RGBA") if (iscyl or (os.path.exists(mgp) and rel["style"] in ("mag", "belt"))) else None; bodyimg = Image.open(bdp).convert("RGBA") if magimg is not None and os.path.exists(bdp) else None
    mp = rel.get("mag")
    rf = [(dense_frame(rel, f, body, gun, gl, grip, k, magimg, bodyimg, mp, iscyl) if magimg is not None else P.frame_new(rel, f, body, gun, gl, grip, k)).crop(box).convert("RGB") for f in rel["frames"]]
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16)
    def tag(img, txt):
        im = img.copy(); d = ImageDraw.Draw(im); d.text((6, 4), f"{key}  {txt}", font=font, fill=(255, 255, 0)); return im
    frames = [tag(f, "FIRE") for f in big] + [tag(big[-1], "FIRE")] * 8 + [tag(f, f"RELOAD ({rel['style']})") for f in rf]
    d_shot = 1000 / 30.0 * slow; d_rel = 1000 * rel["secs"] / len(rf) * slow
    durs = [int(d_shot)] * len(big) + [int(d_shot)] * 8 + [int(d_rel)] * len(rf); durs[-1] = 900
    out = os.path.join(outdir, f"seq_{key}.gif"); frames[0].save(out, save_all=True, append_images=frames[1:], duration=durs, loop=0); print("saved", out, len(frames), "frames")
    for f in ("tmp_shot_" + key + ".gif", "tmp_shot_" + key + ".png"):
        try: os.remove(os.path.join(outdir, f))
        except OSError: pass
if __name__ == "__main__":
    src, outdir = sys.argv[1], sys.argv[2]; os.makedirs(outdir, exist_ok=True)
    for k in sys.argv[3:]: one(src, k, outdir)
