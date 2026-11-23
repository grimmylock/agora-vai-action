"""2026-11-23: the Verger's phase 2 (VergerPhase2.cs, CombatFxView 'scytheember', TacticalGridView.SpawnGhost, HeldWeaponView tint), redrawn here WITHOUT Unity so it can be looked at.
Pose data is the game's own (`dotnet /tmp/test.dll vergermoves vm.json`: the real Boss.Begin/Tick and HeldPoseMath.Frame of every Verger move); the sprites are the game's (bare coat, his scythe); the glow, afterimage and embers are
re-implemented here with the same numbers as the C# (Tint: white -> (1, 0.3, 0.22); ghost every 0.05 s, red (1,.4,.35) at alpha 0.3, fading over 0.3 s, body AND scythe; embers 1-2 px red/orange rising off random pixels of the blade). It is a preview of the intent, not a Unity screenshot.
Usage: python3 Tools/render/verger_phase2.py vm.json out_dir [move name]  -> verger_phase2.gif, verger_phase2_sheet.png, verger_rite_sheet.png"""
import json, os, random, sys
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from melee_scene import SPR, S, BG, big_glove, glove, paste
random.seed(7)
FPS = 30.0
def tint(img, g):                                   # VergerPhase2.Tint: Color.Lerp(white, (1, .3, .22), g) multiplied into the sprite
    r, gg, b = 1.0, 1 + (0.3 - 1) * g, 1 + (0.22 - 1) * g
    px = img.copy(); a = px.split()[3]; out = Image.merge("RGBA", (px.split()[0].point(lambda v: int(v * r)), px.split()[1].point(lambda v: int(v * gg)), px.split()[2].point(lambda v: int(v * b)), a)); return out
def ghost(img, alpha=0.3):                          # sprite colour (1,.4,.35) at alpha .3, a red halo 1.18x behind the blade at alpha .4,
    r, g, b, a = img.split(); return Image.merge("RGBA", (r.point(lambda v: int(v * 1.0)), g.point(lambda v: int(v * .4)), b.point(lambda v: int(v * .35)), a.point(lambda v: int(v * alpha))))

def main(src, out, move="Widow's Harvest"):
    d = json.load(open(src)); os.makedirs(out, exist_ok=True); wpn = d["weapons"][0]; _, acuff, aglove = d["armor"]
    body = Image.open(os.path.join(SPR, "armor_unique_verger_coat_bare.png")).convert("RGBA").crop((0, 0, 16, 16)).resize((16 * S, 16 * S), Image.NEAREST)
    hat = Image.open(os.path.join(SPR, "Fx/fx_verger_hat.png")).convert("RGBA")
    wimg = Image.open(os.path.join(SPR, f"Weapons/weapon_{wpn['key']}.png")).convert("RGBA")
    fist = big_glove(acuff, aglove).resize((5 * S, 4 * S), Image.NEAREST); other = glove(acuff, aglove).resize((4 * S, 3 * S), Image.NEAREST)
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
    W = 16 * S * 3; ub = 16 * S
    mi = [i for i, m in enumerate(wpn["moves"]) if m.startswith(move)][0]
    idx = [i for i, f in enumerate(wpn["frames"]) if f[9] == mi]; lo, hi = max(0, idx[0] - 10), min(len(wpn["frames"]), idx[-1] + 14); idx = list(range(lo, hi))
    def layers(frm, glow):
        gx, gy, deg, hx, hy, sup, sx, sy, lean, mv, swinging, length = frm[:12]; hdeg = frm[12]
        wi = wimg if length > 0.99 else wimg.resize((max(1, round(wimg.width * length)), wimg.height), Image.NEAREST); wi = tint(wi, glow)
        wl = Image.new("RGBA", (W, W)); paste(wl, wi, (wpn["grip"][0] * length, wpn["grip"][1]), (ub + gx * S, ub + gy * S), deg)
        bl = Image.new("RGBA", (W, W)); bl.alpha_composite(body, (ub, ub))
        fz = fist.rotate(hdeg, expand=True, resample=Image.NEAREST); fl = Image.new("RGBA", (W, W)); fl.alpha_composite(fz, (round(ub + hx * S - fz.width / 2), round(ub + hy * S - fz.height / 2)))
        if sup: fl.alpha_composite(other, (round(ub + sx * S - 2 * S), round(ub + sy * S - 1.5 * S)))
        return bl, wl, fl, lean, swinging
    def scene(i_list, glow=1.0, ghosts=True, embers=True, hat_at=None, label=""):
        frames = []; parts = []; last_g = -9
        for n, i in enumerate(i_list):
            t = n / FPS; bl, wl, fl, lean, swing = layers(wpn["frames"][i], glow)
            u = Image.new("RGBA", (W, W))
            if ghosts and swing:
                if t - last_g >= 0.05:
                    last_g = t; parts.append(("g", t, ghost(bl), ghost(wl), ghost(fl)))
            parts = [p for p in parts if p[0] != "g" or t - p[1] < 0.3]
            for p in parts:
                if p[0] != "g": continue
                k = 1 - (t - p[1]) / 0.3
                for im in p[2:]:
                    im = im.copy(); a = im.split()[3].point(lambda v: int(v * k)); im.putalpha(a); u.alpha_composite(im)
            if glow > 0.25:                                        # the halo: a red copy 1.18x behind the blade (TacticalGridView.SpawnHalo)
                hl = wl.resize((round(W * 1.18), round(W * 1.18)), Image.NEAREST).crop((round(W * .09), round(W * .09), round(W * .09) + W, round(W * .09) + W)); u.alpha_composite(ghost(hl, 0.4 * glow))
            u.alpha_composite(bl); u.alpha_composite(wl); u.alpha_composite(fl)
            if embers and glow > 0 and swing:
                ys, xs = [], []; px = wl.load()
                pts = [(x, y) for y in range(0, W, 3) for x in range(0, W, 3) if px[x, y][3] > 0]
                for _ in range(1 + (random.random() < 0.4)):
                    if pts: x, y = random.choice(pts); parts.append(("e", t, x, y, random.uniform(-6, 6), random.uniform(25, 70), random.uniform(0.4, 0.9), random.random() < 0.35))
            nxt = []
            for p in parts:
                if p[0] == "g": nxt.append(p); continue
                age = t - p[1]
                if age > p[6]: continue
                nxt.append(p); x = p[2] + p[4] * age * S / 2; y = p[3] - p[5] * age * S / 2; col = (255, 190, 90, 255) if p[7] else (255, 55 + int(random.random() * 35), 26, 255)
                ImageDraw.Draw(u).rectangle((x, y, x + S // 2, y + S // 2), fill=col)
            parts = nxt
            u = u.rotate(lean, center=(ub + 8 * S, ub + 8 * S), resample=Image.NEAREST)
            c = Image.new("RGBA", (W, W + 40), BG); c.alpha_composite(u, (0, 40)); dr = ImageDraw.Draw(c); dr.text((8, 6), label, font=font, fill=(240, 200, 90)); frames.append(c)
        return frames
    # 1) the move in phase 2: glow, ghosts, embers  vs  phase 1 for comparison
    p2 = scene(idx, 1.0, True, True, label=f"phase 2: {move}"); p1 = scene(idx, 0.0, False, False, label=f"phase 1 look (no effects)")
    gif = []
    for a, b in zip(p1, p2):
        c = Image.new("RGB", (W * 2, W + 40)); c.paste(b.convert("RGB"), (0, 0)); c.paste(a.convert("RGB"), (W, 0)); gif.append(c)
    gif[0].save(os.path.join(out, "verger_phase2.gif"), save_all=True, append_images=gif[1:], duration=int(1000 / FPS), loop=0)
    pick = [gif[i] for i in range(4, len(gif), max(1, len(gif) // 5))][:5]
    sheet = Image.new("RGB", (W * 2, (W + 40) * len(pick))); [sheet.paste(f, (0, k * (W + 40))) for k, f in enumerate(pick)]; sheet.save(os.path.join(out, "verger_phase2_sheet.png"))
    # 2) the rite: stance frames at glow 0 / .33 / .66 / 1 (embers building), the hat lying on the floor
    stance = [i for i, f in enumerate(wpn["frames"]) if not f[10]][5:30]
    rite = []
    for g, lab in [(0.0, "3.0 s: hand reaches the blade"), (0.33, "4.2 s"), (0.66, "5.4 s"), (1.0, "6.6 s: the flash, full red")]:
        fr = scene(stance, g, False, g > 0, label=lab)[-1].copy()
        h = hat.resize((hat.width * S // 2, hat.height * S // 2), Image.NEAREST); fr.alpha_composite(h, (ub + 5 * S, 40 + ub + 14 * S))      # the hat on the stones beside him
        rite.append(fr)
    rs = Image.new("RGB", (W * 4, W + 40)); [rs.paste(f.convert("RGB"), (k * W, 0)) for k, f in enumerate(rite)]; rs.save(os.path.join(out, "verger_rite_sheet.png")); print("verger phase 2 ->", out)
if __name__ == "__main__": main(sys.argv[1], sys.argv[2], *(sys.argv[3:4]))
