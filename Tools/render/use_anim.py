"""Use (Yan: "healing doesn't have an animation right now"; "crouching also doesn't play the crouch animation"): the item-use
animations from the harness (`dotnet /tmp/test.dll useframes u.json`: HeldPoseMath.Frame + ApplyUse, what HeldWeaponView draws) -
the pistol dipping, the free glove with the item (Sprites/Items/use_<kind>.png) - facing right and left; plus the sneaking crouch
frame next to standing. -> use_anim.gif, use_sheet.png
    python3 Tools/render/use_anim.py u.json out_dir"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, BG, glove, paste
S = 6
d = json.load(open(sys.argv[1])); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
idx = 115                                                      # one of the suits
def cell(name): im = Image.open(os.path.join(SPR, name)).convert("RGBA"); return im.crop(((idx % 16) * 16, (idx // 16) * 16, (idx % 16) * 16 + 16, (idx // 16) * 16 + 16))
body, crouch = cell("armor_atlas.png"), cell("armor_pose_crouch.png")
pistol = Image.open(os.path.join(SPR, "Weapons/weapon_PSh18Pchela.png")).convert("RGBA")
g1 = glove("#5a4a36", "#3d4148").resize((4 * S, 3 * S), Image.NEAREST)
items = {k["kind"]: Image.open(os.path.join(SPR, f"Items/use_{k['kind']}.png")).convert("RGBA") for k in d["kinds"]}
W = 16 * S * 2
GRIP = {k["kind"]: tuple(k["grip"]) for k in d["kinds"]}
def draw(kind, fr):
    c = Image.new("RGBA", (W * 2, W + 30), BG)
    for j, (gx, gy, deg, hx, hy, sx, sy, left, ideg) in enumerate(fr):
        u = Image.new("RGBA", (W, W)); o = 8 * S
        u.alpha_composite(body.resize((16 * S, 16 * S), Image.NEAREST), (o, o))
        paste(u, pistol, tuple(d["grip"]), (o + gx * S, o + gy * S), deg, flip=bool(left))
        it = items[kind].transpose(Image.FLIP_LEFT_RIGHT) if left else items[kind]
        gp = (16 - GRIP[kind][0], GRIP[kind][1]) if left else GRIP[kind]
        paste(u, it, gp, (o + sx * S, o + sy * S), ideg)                                  # the item, held at its grip point
        gl = g1 if left else g1.transpose(Image.FLIP_LEFT_RIGHT)
        u.alpha_composite(gl, (round(o + sx * S - 2 * S), round(o + sy * S - 1.5 * S)))
        gg = g1.transpose(Image.FLIP_LEFT_RIGHT) if left else g1
        u.alpha_composite(gg, (round(o + hx * S - 2 * S), round(o + hy * S - 1.5 * S)))
        c.alpha_composite(u, (j * W, 30))
    ImageDraw.Draw(c).text((8, 6), kind, font=font, fill=(235, 235, 235))
    return c
cols = [[draw(k["kind"], fr) for fr in k["frames"]] for k in d["kinds"]]
frames = []
for i in range(len(cols[0])):
    f = Image.new("RGB", (W * 2 * len(cols), W + 30)); [f.paste(col[i], (n * W * 2, 0)) for n, col in enumerate(cols)]; frames.append(f)
frames[0].save(os.path.join(out, "use_anim.gif"), save_all=True, append_images=frames[1:], duration=33, loop=0)
sheet = Image.new("RGB", (W * 2 * 4, (W + 30) * 4 + W + 40), (27, 29, 31))
for r, i in enumerate([8, 22, 36, 50]): sheet.paste(frames[i], (0, r * (W + 30)))
y = 4 * (W + 30) + 10; dr = ImageDraw.Draw(sheet); dr.text((8, y), "standing / crouched (sneaking)", font=font, fill=(235, 235, 235))
for n, im in enumerate((body, crouch)): sheet.paste(im.resize((16 * S, 16 * S), Image.NEAREST), (20 + n * 130, y + 24), im.resize((16 * S, 16 * S), Image.NEAREST))
sheet.save(os.path.join(out, "use_sheet.png")); print("use anims ->", out)
