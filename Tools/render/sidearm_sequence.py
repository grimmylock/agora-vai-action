"""Approval GIFs (2026-11-16): per gun ONE shot (the gun's own parts moving: slide, hammer; shell leaving the port; hands, kick) then a pause, then the RELOAD (hands round the whole gun: the double-size guns have no cut magazine/cylinder pieces yet).
Slow motion (x2) so every pose can be judged. Usage: python3 Tools/render/sidearm_sequence.py frames.json outdir key [key ...]"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import held_anim as H, held_hires_preview as P, held_hires_check as C, parts_preview as PP
S = H.S; SPR = H.SPR
def one(src, key, outdir, slow=2.0):
    big, box = PP.main(src, key, os.path.join(outdir, "tmp_shot_" + key))
    allan = json.load(open(src)); rel = [x for x in allan if x["key"] == key and x["style"] not in ("jam", "shot")][0]; shot = [x for x in allan if x["key"] == key and x["style"] == "shot"][0]
    D = C.dense(); k = D[key]; atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA"); i = rel["armor"]
    body = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)).resize((16 * S, 16 * S), Image.NEAREST); gl = H.glove(rel["cuff"], rel["glove"])
    gun = Image.open(os.path.join(SPR, "Weapons", f"weapon_{key}_hd.png")).convert("RGBA"); grip = (rel["gx"] * k, rel["gy"] * k)
    rf = [P.frame_new(rel, f, body, gun, gl, grip, k).crop(box).convert("RGB") for f in rel["frames"]]
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
