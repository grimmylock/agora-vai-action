# Batch 130c (Yan: "can't we draw them bigger and reduce their size similar to what we did with the guns?"): the mutants at double
# resolution, from Yan's references (ArtSource/refs/mutants/) through mutant_from_ref.auto (cut-out, palette + bright accents,
# mode per block, despeckle, outline). Shown at 32 px per tile like the in-hand weapons, so a 48x32 hound covers the same ground
# as a 24x16 one did, with four times the pixels. All face right (the game mirrors them).
#   python3 make_mutants_hd.py <sheet.png> [out_dir]
import os, sys
from PIL import Image, ImageDraw, ImageFont
import mutant_from_ref as m

HERE = os.path.dirname(os.path.abspath(__file__))
# key: (canvas w, h, faces left in the reference, reduction options)
SPEC = {
    "pallid_prowler": (48, 32, False, {}),
    "blind_maw":      (48, 32, True, {}),
    "veil_prowler":   (48, 32, True, {"crop": (2, 6, 340, 190), "tol": 18}),
    "dread_prowler":  (64, 48, True, {}),
    "blight_brute":   (64, 64, False, {"k": 6, "accents": 1, "sat_min": 110}),
}
NAMES = {"pallid_prowler": "Pallid Prowler", "blind_maw": "Blind Maw", "veil_prowler": "Veil Prowler",
         "dread_prowler": "Dread Prowler", "blight_brute": "Blight Brute"}

def sprites(outlined=True):
    out = []
    for key, (cw, ch, left, kw) in SPEC.items():
        im, main, extra = m.auto(os.path.join(HERE, "refs", "mutants", key + ".png"), cw, ch, do_outline=outlined, **kw)
        if key == "blight_brute": im = m.despeckle(im, keep={m.hexc(c)[:3] for c in extra})   # its moss is busy: a second pass
        if left: im = im.transpose(Image.FLIP_LEFT_RIGHT)
        out.append((NAMES[key], key, im))
    return out

def sheet(path, items):
    human = Image.open(os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA").crop((0, 0, 16, 16))
    W, H = 1560, 70 + 3 * 300 + 200
    img = Image.new("RGBA", (W, H), m.hexc("#1b1d1f")); d = ImageDraw.Draw(img)
    f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 15)
    tf = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    d.text((30, 20), "Mutants at double resolution (shown like the guns: 2 sprite px per character px) - reference | sprite 4x", font=tf, fill=(230, 230, 230))
    for i, (name, key, im) in enumerate(items):
        col, row = i % 2, i // 2
        x0, y0 = 30 + col * 770, 70 + row * 300
        r = Image.open(os.path.join(HERE, "refs", "mutants", key + ".png")).convert("RGBA"); r.thumbnail((300, 260)); img.alpha_composite(r, (x0, y0))
        d.rectangle([x0 + 310, y0, x0 + 310 + 64 * 4, y0 + 64 * 4], fill=m.hexc("#2a2620"))
        big = im.resize((im.width * 4, im.height * 4), Image.NEAREST)
        img.alpha_composite(big, (x0 + 310 + (256 - big.width) // 2, y0 + 256 - big.height))
        d.text((x0, y0 + 262), f"{name} ({im.width}x{im.height}, covers {im.width // 2}x{im.height // 2} character px)", font=f, fill=(184, 188, 192))
    # true scale: the human at 2x next to each mutant at 1x = how they share the screen in game (and at 3x that)
    y = H - 190; d.rectangle([30, y - 10, W - 30, H - 20], fill=m.hexc("#2a2620")); x = 50
    d.text((50, y - 6), "in game, magnified 3x (human 16 px, mutants at half-size pixels):", font=f, fill=(184, 188, 192))
    for im, s in [(human, 6)] + [(it[2], 3) for it in items]:
        img.alpha_composite(im.resize((im.width * s, im.height * s), Image.NEAREST), (x, y + 180 - 10 - im.height * s)); x += im.width * s + 30
    img.convert("RGB").save(path)

if __name__ == "__main__":
    items = sprites()
    sheet(sys.argv[1] if len(sys.argv) > 1 else "mutants_hd_sheet.png", items)
    if len(sys.argv) > 2:
        os.makedirs(sys.argv[2], exist_ok=True)
        for _, key, im in items: im.save(os.path.join(sys.argv[2], f"mutant_{key}.png"))
