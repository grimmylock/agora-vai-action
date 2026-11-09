# PROTOTYPE (Yan, 2026-11-09): the armor/NPC sprites at DOUBLE density (32x32, like the mutants and guns), redesigned with the locked look (agora-vai-pixel-art skill: front view, blocky,
# ARMLESS - hands are separate floating sprites - near-black #141617 outline, a gas mask with bright lens pixels, hood/helmet, belt, boots, faction armband) but with real form:
# volume shading per part (pxart.shade), seams, pouches, straps, plates, knee pads, and the faction colours of the skill's table. Wound overlays are separate layers per body part.
# Three Wanderer suits (light jacket, heavy plates, exo) + wound overlays. Not in the game.
#   python3 ArtSource/armor32_proto.py <out_dir>
import os, sys, math
from PIL import Image
from pxart import Mask, shade, rgb, outline, px, line

def ramp5(hexc, lo=0.5, hi=1.55):
    r, g, b, _ = rgb(hexc)
    f = [lo, lo + (1 - lo) * 0.45, 1.0, 1 + (hi - 1) * 0.5, hi]
    return [(min(255, int(r * k)), min(255, int(g * k)), min(255, int(b * k)), 255) for k in f]

WANDERER = dict(hood="#4a5a3a", suit="#5a4a36", mask="#3b4046", plate="#6a6e72", belt="#3d3226", boots="#2a2218", lens="#9fd8e8", glove="#3a3a3a", band="#d8a62e")
OL = (20, 22, 23, 255)
S = (32, 32)

def part(build, cols, **kw):
    m = Mask(S); build(m); return shade(m, cols, **kw)

def suit32(kind="light", pal=WANDERER):
    P = {k: ramp5(v) for k, v in pal.items() if k not in ("lens", "band")}
    heavy = kind in ("heavy", "exo"); exo = kind == "exo"
    tw = 12 if kind == "light" else 15 if kind == "heavy" else 16                # torso width in 32-px pixels (the 16-px sprites: 6 and 8 wide, doubled)
    cx = 16; hw = tw / 2.0
    layers = []
    # legs and boots: two separate legs with a gap, boots with a sole
    for dx in (-1, 1):
        lx = cx + dx * (3.3 if not heavy else 3.8)
        wl = 4.8 if not heavy else 5.4
        layers.append(part(lambda m, lx=lx, wl=wl: m.tube([(lx, 21.5), (lx, 28.5)], [wl, wl - 0.2]), P["suit"], depth=2.4, tex=0.08, seed=3 + dx, bias=-0.1))
        layers.append(part(lambda m, lx=lx, wl=wl: m.poly([(lx - wl / 2 - 0.3, 28), (lx + wl / 2 + 0.3, 28), (lx + wl / 2 + 0.7, 30.8), (lx - wl / 2 - 0.7, 30.8)]), P["boots"], depth=1.8, bias=-0.05))
        if exo:
            layers.append(part(lambda m, lx=lx: m.ell(lx, 24.6, 2.9, 2.2), P["plate"], depth=2.0, bias=0.0))                       # knee pads
    # torso: rounded shoulders, chest
    def torso(m):
        m.poly([(cx - hw + 1, 12.4), (cx + hw - 1, 12.4), (cx + hw, 14), (cx + hw - 0.6, 22), (cx - hw + 0.6, 22), (cx - hw, 14)])
    layers.append(part(torso, P["suit"], depth=3.2, tex=0.1, seed=11, bias=-0.04))
    layers.append(part(lambda m: m.poly([(cx - hw + 0.6, 20.4), (cx + hw - 0.6, 20.4), (cx + hw - 0.6, 22.4), (cx - hw + 0.6, 22.4)]), P["belt"], depth=1.2))       # belt
    layers.append(part(lambda m: m.poly([(cx - 1.3, 20.5), (cx + 1.3, 20.5), (cx + 1.3, 22.3), (cx - 1.3, 22.3)]), ramp5("#c9a63e", 0.6, 1.25), depth=1.0))        # buckle
    for dx in (-1, 1):                                                           # two chest pouches
        layers.append(part(lambda m, dx=dx: m.poly([(cx + dx * hw * 0.5 - 1.5, 15.2), (cx + dx * hw * 0.5 + 1.5, 15.2), (cx + dx * hw * 0.5 + 1.5, 18.4), (cx + dx * hw * 0.5 - 1.5, 18.4)]), P["suit"], depth=1.3, bias=0.12))
    if heavy:                                                                    # a chest plate and shoulder pads, smaller than the suit so the suit still shows
        layers.append(part(lambda m: m.poly([(cx - hw * 0.5, 13.4), (cx + hw * 0.5, 13.4), (cx + hw * 0.45, 19.6), (cx - hw * 0.45, 19.6)]), P["plate"], depth=2.2, bias=0.02))
        for dx in (-1, 1): layers.append(part(lambda m, dx=dx: m.ell(cx + dx * (hw - 0.4), 13.8, 2.8, 2.3), P["plate"], depth=2.2, bias=0.02))
    # head: hood (heavy: a helmet in the hood's colour with a plate brow), mask, filter, lens
    hr = 5.5 if not heavy else 5.9
    layers.append(part(lambda m: m.ell(cx, 7.0, hr, 5.4), P["hood"], depth=3.4, tex=0.08, seed=21))
    if heavy: layers.append(part(lambda m: m.poly([(cx - hr + 0.4, 5.6), (cx + hr - 0.4, 5.6), (cx + hr - 0.6, 7.2), (cx - hr + 0.6, 7.2)]), P["plate"], depth=1.2, bias=0.02))
    layers.append(part(lambda m: m.poly([(cx - 3.8, 8.0), (cx + 3.8, 8.0), (cx + 3.2, 11.6), (cx - 3.2, 11.6)]), P["mask"], depth=2.0, bias=-0.02))              # the gas mask
    layers.append(part(lambda m: m.ell(cx, 11.8, 1.7, 1.2), ramp5("#202428"), depth=1.1))                                                                         # filter canister
    im = Image.new("RGBA", S, (0, 0, 0, 0))
    for l in layers: im.alpha_composite(l)
    lens = rgb(pal["lens"]); lens_hi = (255, 255, 255, 255); lens_dk = tuple(int(v * 0.55) for v in lens[:3]) + (255,)
    for dx in (-1, 1):                                                           # lenses: 2x2 each, one bright pixel, one dark
        x = cx + dx * 2.2
        px(im, [(x - 0.5, 8.8), (x + 0.5, 8.8), (x - 0.5, 9.8), (x + 0.5, 9.8)], lens); px(im, [(x - 0.5 if dx < 0 else x + 0.5, 8.8)], lens_hi); px(im, [(x + 0.5 if dx < 0 else x - 0.5, 9.8)], lens_dk)
    seam = P["suit"][0]
    line(im, cx, 12.6, cx, 20, seam); px(im, [(cx - 1, 12.6), (cx + 1, 12.6)], P["suit"][1])
    if kind == "light":
        for k in range(5): px(im, [(cx - hw + 1.2 + k * 0.7, 14 + k)], P["suit"][1])                      # a diagonal strap
    for x in range(int(cx - hw + 1), int(cx + hw - 1)): px(im, [(x, 13)], P["suit"][3] if not heavy else P["plate"][4])                              # lit top edge of the shoulders
    band = rgb(pal["band"]); px(im, [(cx - hw + 0.5, 13.5), (cx - hw + 1.5, 13.5), (cx - hw + 0.5, 14.5), (cx - hw + 1.5, 14.5)], band)               # faction armband
    if heavy:
        for dx in (-1, 1): px(im, [(cx + dx * (hw - 0.4) - 1, 12.5), (cx + dx * (hw - 0.4), 12.5)], P["plate"][4])
        line(im, int(cx - hw * 0.4), 16, int(cx + hw * 0.4), 16, P["plate"][1])
    return outline(im, OL)

# wound overlays: one transparent layer per body part, drawn at the same 32x32 (the game would tint/scale them with Body's severity)
BLOOD = [(70, 12, 14, 255), (112, 18, 20, 255), (150, 26, 28, 255), (188, 44, 42, 255)]
def wound(part_name, level=2):
    im = Image.new("RGBA", S, (0, 0, 0, 0)); cx = 16
    def blob(pts, cols=BLOOD):
        for (x, y, r) in pts:
            for yy in range(int(y - r) - 1, int(y + r) + 2):
                for xx in range(int(x - r) - 1, int(x + r) + 2):
                    d = math.hypot(xx - x, yy - y)
                    if d <= r: px(im, [(xx, yy)], cols[0] if d > r * 0.7 else cols[1] if d > r * 0.35 else cols[2])
    if part_name == "head":
        blob([(cx + 2.5, 5.5, 1.6), (cx + 3, 7.5, 1.0)]); line(im, cx + 3, 8, cx + 3, 12, BLOOD[1]); px(im, [(cx + 3, 6)], BLOOD[3])
    elif part_name == "torso":
        blob([(cx - 2, 17, 2.8 if level > 1 else 1.8), (cx - 3, 19.5, 1.6)]); px(im, [(cx - 2, 16), (cx - 2, 17)], (30, 6, 8, 255)); px(im, [(cx - 2, 15.5)], BLOOD[3])
        line(im, cx - 2, 20, cx - 2, 22, BLOOD[1])
    elif part_name == "leg":
        blob([(cx + 3.2, 24, 2.0)]); line(im, cx + 3, 26, cx + 4, 29, BLOOD[1]); px(im, [(cx + 3, 25)], BLOOD[3])
    elif part_name == "arm":
        blob([(cx - 7, 17, 1.8)]); line(im, cx - 7, 19, cx - 7, 22, BLOOD[1])
    return im

def sheet(out):
    os.makedirs(out, exist_ok=True)
    suits = [suit32(k) for k in ("light", "heavy", "exo")]
    for k, s in zip(("light", "heavy", "exo"), suits): s.save(os.path.join(out, f"wanderer_{k}_32.png"))
    wounded = suits[0].copy()
    for p in ("head", "torso", "leg"):
        w = wound(p); mask = suits[0].split()[3]
        w2 = Image.composite(w, Image.new("RGBA", S, (0, 0, 0, 0)), mask); wounded.alpha_composite(w2)
    wounded.save(os.path.join(out, "wanderer_light_32_wounded.png"))
    return suits + [wounded]

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "_out/armor32"
    items = sheet(out)
    old = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA")
    sc = 10; cells = [it for it in items]
    W = (len(cells) + 3) * (32 * sc + 20) + 20; H = 32 * sc + 40
    sheet_im = Image.new("RGBA", (W, H), (70, 68, 64, 255)); x = 20
    for c in cells: sheet_im.alpha_composite(c.resize((32 * sc, 32 * sc), Image.NEAREST), (x, 20)); x += 32 * sc + 20
    for i in (45, 24 + 14, 45 + 13):                                      # three current Wanderer suits for comparison (the atlas cells, doubled to the same screen size)
        cc, rr = i % 16, i // 16; cell = old.crop((cc * 16, rr * 16, cc * 16 + 16, rr * 16 + 16)).resize((32, 32), Image.NEAREST)
        sheet_im.alpha_composite(cell.resize((32 * sc, 32 * sc), Image.NEAREST), (x, 20)); x += 32 * sc + 20
    sheet_im.convert("RGB").save(os.path.join(out, "proto_sheet.png")); print("ok")
