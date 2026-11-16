"""Approval GIFs (2026-11-16): per gun ONE shot (the gun's own parts moving: slide, hammer; shell leaving the port; hands, kick) then a pause, then the RELOAD (hands round the whole gun: the double-size guns have no cut magazine/cylinder pieces yet).
Slow motion (x2) so every pose can be judged. Usage: python3 Tools/render/sidearm_sequence.py frames.json outdir key [key ...]"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as H, held_hires_preview as P, held_hires_check as C, parts_preview as PP
S = H.S; SPR = H.SPR
import re
def hinges():
    s = open(os.path.join(H.ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read(); m = re.search(r"Hinges = new Dictionary<string, \(float, float\)> \{(.*?)\};", s, re.S)
    return {k: (float(x), float(y)) for k, x, y in re.findall(r'\["(\w+)"\] = \(([0-9.]+)f, ([0-9.]+)f\)', m.group(1))}
def shell_in_hand(r, trips):                                                                         # the same rule as HeldPoseMath.Frame: on the way back from the pouch, one round per trip
    inner = min(1.0, max(0.0, (r - 0.1) / 0.8))
    if inner <= 0 or inner >= 1: return False
    u = inner * max(1, trips) % 1.0; return 0.57 <= u < 0.98
def dense_frame(a, f, body, gun, gl, grip, k, magimg, bodyimg, mp, cyl=False, brkp=None, shell=None):
    """The reload frame of a double-size gun with its magazine moving (the game's own frame record + the cut pieces): the dropped magazine on the floor or falling, the seated/carried magazine, then the gun (the body without the magazine when it was cut), then the hands."""
    gx_, gy_, deg, hx, hy, sx, sy, has_sup, left, mdx, mdy, mshown, brk, thrown, lx, ly, ldeg, landed, brass = f
    c = Image.new("RGBA", (H.W * S, H.H * S), H.BG); c.alpha_composite(body, (H.BX * S, H.BY * S))
    if magimg is not None and mp is not None and thrown:                                            # the old magazine: falling or lying on the floor (character px, centre pivot)
        P.paste2(c, magimg, mp[2] * k, mp[3] * k, lx, ly, ldeg, left, k)
    for bx, by, bdeg, rest in brass:                                                                 # spent brass a revolver dumps (character px): lying under the unit or still falling
        d = ImageDraw.Draw(c); X, Y = (H.BX + bx) * S, (H.BY + by) * S; d.rectangle((X - 2, Y - 1, X + 3, Y + 1), fill=(201, 166, 62))
    if magimg is not None and cyl: P.paste2(c, magimg, grip[0], grip[1], gx_ + mdx, gy_ + mdy, deg, left, k)             # a cylinder swung out on its crane: behind the frame (the far side), peeking out beside the window
    if magimg is not None and mshown and not cyl: P.paste2(c, magimg, grip[0], grip[1], gx_ + mdx, gy_ + mdy, deg, left, k)       # seated, or in the hand
    g2 = bodyimg if bodyimg is not None else gun
    if brkp is not None and f[12] != 0:                                                                # a break action: the barrels hang open on the hinge (clockwise in the gun's own picture)
        bar, hinge = brkp; layer = Image.new("RGBA", g2.size); layer.alpha_composite(g2); layer.alpha_composite(bar.rotate(-f[12], center=hinge, resample=Image.BICUBIC)); g2 = layer
    elif brkp is not None: layer = Image.new("RGBA", g2.size); layer.alpha_composite(g2); layer.alpha_composite(brkp[0]); g2 = layer
    P.paste2(c, g2, grip[0], grip[1], gx_, gy_, deg, left, k)
    gg = gl.transpose(Image.FLIP_LEFT_RIGHT) if left else gl
    for (x, y, on) in ((sx, sy, has_sup), (hx, hy, 1)):
        if on: c.alpha_composite(gg, (round((H.BX + x) * S - 2 * S), round((H.BY + y) * S - 1.5 * S)))
    if shell is not None and has_sup:                                                                 # the round in the support hand (red hull for a shotgun, brass for a rifle)
        d = ImageDraw.Draw(c); X, Y = (H.BX + sx) * S, (H.BY + sy) * S
        d.rectangle((X - 3 * S // 2, Y - S // 2, X + 3 * S // 2, Y + S // 2), fill=(200, 48, 44) if shell else (222, 184, 74))
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
    mp = rel.get("mag"); trips = rel.get("trips", 1)
    brp = os.path.join(wp, f"weapon_{key}_hdbarrels.png"); isbrk = rel["style"] == "break" and os.path.exists(brp)
    brk = (Image.open(brp).convert("RGBA"), (hinges()[key][0] * k, hinges()[key][1] * k)) if isbrk else None; bodyimg = Image.open(bdp).convert("RGBA") if isbrk else bodyimg
    shellkind = None if rel["style"] != "tube" else (True if key in ("BayouPump", "RangerP8", "Hound13", "SP14Warden", "HaldenLeverShotgun", "Karusel5") else False)
    def mk(i, f):
        sh = shellkind if (shellkind is not None and shell_in_hand(i / 30.0 / rel["secs"], trips)) else None
        if magimg is not None or isbrk or sh is not None: return dense_frame(rel, f, body, gun, gl, grip, k, magimg, bodyimg, mp, iscyl, brk, sh)
        return P.frame_new(rel, f, body, gun, gl, grip, k)
    rf = [mk(i, f).crop(box).convert("RGB") for i, f in enumerate(rel["frames"])]
    if rel["style"] == "break": ki = max(range(len(rel["frames"])), key=lambda i: rel["frames"][i][12])                                  # a key frame for review: barrels most open / the first round in the hand / the middle of the reload
    elif rel["style"] == "tube": ki = next((i for i in range(len(rel["frames"])) if shell_in_hand(i / 30.0 / rel["secs"], trips)), len(rel["frames"]) // 2) + 6
    else: ki = len(rel["frames"]) // 2
    kf = mk(ki, rel["frames"][min(ki, len(rel["frames"]) - 1)]).convert("RGB"); kw, kh = kf.size; kf.crop((int(kw * 0.12), int(kh * 0.05), kw, int(kh * 0.8))).resize((int(kw * 0.88) * 2, int(kh * 0.75) * 2), Image.NEAREST).save(os.path.join(outdir, f"key_{key}.png"))
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
