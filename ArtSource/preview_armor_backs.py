# Batch 128: front | back review sheet for one faction's armors (make_armor_avatars.build(back=True)), with the faction
# armband baked in the way export_to_unity.py does it (back: the mirrored shoulder). Bottom strip: 3x and true size.
#   python3 preview_armor_backs.py <Faction> <out.png>
import sys, io, contextlib
from PIL import Image, ImageDraw, ImageFont
with contextlib.redirect_stdout(io.StringIO()):
    import make_faction_armors as fa     # builds the catalog (and its own preview files) on import
av = fa.av

def band(img, fac, spots):
    img = img.copy(); c = av.hexc(fa.FACTION[fac][0])
    for x, y in spots:
        r, g, b, a = img.getpixel((x, y))
        if a and r + g + b > 80: img.putpixel((x, y), c); break
    return img

FAC, OUTP = sys.argv[1], sys.argv[2]
rr = [r for r in fa.rows if r[0] == FAC]
Z = 8; W = 40 + 3 * (2 * 16 * Z + 60); H = 80 + 5 * (16 * Z + 40) + 150
sheet = Image.new("RGBA", (W, H), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 15)
tf = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
d.text((40, 24), f"{FAC} - front | back (8x); bottom: 3x and true size on the ground", font=tf, fill=(230, 230, 230))
small = []
for fac, tier, arch, name, st, look, ex in rr:
    fr = band(av.build(name, st[1] / 100, st[2], L=look), fac, ((5, 8), (4, 7)))
    bk = band(av.build(name, st[1] / 100, st[2], L=look, back=True), fac, ((10, 8), (11, 7)))
    small.append((fr, bk))
    t = [r[3] for r in rr if r[1] == tier].index(name)
    x0 = 40 + t * (2 * 16 * Z + 60); y0 = 70 + (tier - 1) * (16 * Z + 40)
    sheet.alpha_composite(fr.resize((16 * Z, 16 * Z), Image.NEAREST), (x0, y0))
    sheet.alpha_composite(bk.resize((16 * Z, 16 * Z), Image.NEAREST), (x0 + 16 * Z + 8, y0))
    d.text((x0, y0 + 16 * Z + 4), f"T{tier} {name}", font=f, fill=(184, 188, 192))
y = H - 130; d.rectangle([40, y - 10, W - 40, H - 20], fill=(42, 38, 32))
x = 60
for fr, bk in small:
    for im in (fr, bk):
        sheet.alpha_composite(im.resize((48, 48), Image.NEAREST), (x, y)); sheet.alpha_composite(im, (x + 16, y + 64)); x += 52
    x += 12
sheet.convert("RGB").save(OUTP)
