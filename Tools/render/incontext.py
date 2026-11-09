#!/usr/bin/env python3
"""In-context check for new creature art (Yan, 2026-11-09: he worries new art will look "weirdly HD" next to the world and the human NPCs).

Puts candidate sprites on a REAL map render (Tools/render/render.sh <map>: 16 px a tile, the game's own tiles and props) next to human NPCs and the finished mutants,
all at their real relative pixel density: the world and the humans are drawn at 16 px a tile (shown here at 2x), mutants and bosses are drawn at double density (so their pixels
are half the size). Nothing is lit, shadowed or tinted here: it is a size/density/finish comparison, not a Unity screenshot.

    bash Tools/render/render.sh bonfire_village                       # once (about 15 s)
    python3 Tools/render/incontext.py cand1.png [cand2.png ...] --out ctx.png
    python3 Tools/render/incontext.py --demo --out ctx.png            # finished mutants + the first-pass humanoids
Options: --bosses (add the eel, a rear-up frame, and the brain vat), --map NAME (default bonfire_village), --at X,Y (pixel in the render, default a dirt road), --tiles W,H (default 22,10), --view N (final zoom, default 2).
A sprite file is a PNG at the size it would have in the game (a 48x32 hound is 1.5 x 1 tiles); sprite names ending in `_x2` are shown at 2x (art drawn at 16 px density)."""
import argparse, os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
MUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Mutants")

def bottom(im):
    bb = im.getbbox(); return bb[3] if bb else im.height

def human(col, row):
    a = Image.open(os.path.join(ROOT, "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA")
    return a.crop((col * 16, row * 16, col * 16 + 16, row * 16 + 16)).resize((32, 32), Image.NEAREST)        # the world is shown at 2x, so a 16 px man is 32 px

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("sprites", nargs="*"); ap.add_argument("--map", default="bonfire_village"); ap.add_argument("--at", default="1700,1580")
    ap.add_argument("--tiles", default="22,10"); ap.add_argument("--view", type=int, default=2); ap.add_argument("--out", default="incontext.png"); ap.add_argument("--demo", action="store_true"); ap.add_argument("--bosses", action="store_true", help="add the eel (a rear-up frame) and the brain vat, drawn at their real density")
    a = ap.parse_args()
    mp = os.path.join(HERE, "_render", a.map + ".png")
    if not os.path.exists(mp): sys.exit(f"render the map first: bash Tools/render/render.sh {a.map}")
    cx, cy = [int(v) for v in a.at.split(",")]; tw, th = [int(v) for v in a.tiles.split(",")]
    world = Image.open(mp).convert("RGBA").crop((cx - tw * 8, cy - th * 8, cx + tw * 8, cy + th * 8)).resize((tw * 32, th * 32), Image.NEAREST)
    cast = [("man", human(0, 0)), ("man", human(5, 1)), ("man", human(9, 2))]
    for n in ("pallid_prowler", "dread_prowler", "blight_brute"):
        cast.append((n, Image.open(os.path.join(MUT, f"mutant_hd_{n}.png")).convert("RGBA")))
    if a.bosses:
        fx = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Fx")
        eel = Image.open(os.path.join(fx, "boss_eel_rear_idle.png")).convert("RGBA").crop((0, 0, 620, 340)); eel = eel.crop(eel.getbbox())
        vat = Image.open(os.path.join(MUT, "boss_brain_vat.png")).convert("RGBA").crop((0, 0, 128, 160)); vat = vat.crop(vat.getbbox())
        cast += [("eel", eel), ("brain vat", vat)]
    cands = list(a.sprites)
    if a.demo:
        d = os.path.join(ROOT, "ArtSource", "mutants_anim", "humanoid")
        cands += [os.path.join(d, f"mutant_hd_{n}.png") for n in ("alpha_prowler", "rot_crawler", "mind_weaver")]
    for c in cands:
        im = Image.open(c).convert("RGBA")
        if os.path.splitext(c)[0].endswith("_x2"): im = im.resize((im.width * 2, im.height * 2), Image.NEAREST)
        cast.append(("NEW " + os.path.basename(c), im))
    # a row of everyone on a common ground line (their feet), spaced out, over the real ground
    base = world.height - 40; x = 30; gap = 26
    scene = world.copy()
    for name, im in cast:
        if x + im.width > scene.width - 10:                                  # wrap to a second row further up
            x = 30; base -= 120
        y = base - bottom(im)
        sh = Image.new("RGBA", scene.size, (0, 0, 0, 0)); ImageDraw.Draw(sh).ellipse([x + 4, base - 4, x + im.width - 4, base + 5], fill=(18, 16, 12, 110)); scene.alpha_composite(sh)   # a plain contact shadow, nothing more
        scene.alpha_composite(im, (x, y)); x += im.width + gap
    scene = scene.resize((scene.width * a.view, scene.height * a.view), Image.NEAREST)
    scene.convert("RGB").save(a.out); print(a.out, scene.size, f"{len(cast)} figures ({len(cands)} new)")

if __name__ == "__main__":
    main()
