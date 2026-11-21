# The beast's Pixel Lab clips -> game strips (2026-11-20; Yan: "use the spritethingy to do the attacks and animations like we did the Gaunt"). Source: Yan's generated stand (ai/poses/stand.png) animated with Pixel Lab animate-pixminimax
# (ArtSource/pixellab_anim.py, README_BEAST.md has the prompts, seeds, jobs and what each clip does). Every frame is a 256x232 cell with the stand sprite at the bottom centre (feet 6 px above the bottom, so the pivot is 22/232).
# Writes Assets/Resources/Sprites/Mutants/anim/verger_beast_<clip>.bytes (a PNG strip, hard alpha, decoded in code) for the clips below; the timing table is VergerBoss.Clips (VergerBoss.cs) - keep the frame counts in step.
import os, sys, io, glob
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); SRC = os.path.join(HERE, "mutants_anim", "verger_beast", "pixellab")
OUT = os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "anim")
# 2026-11-21 (Yan: "his swipes kinda target below him, not in front"): the generated rake and sweep bake a black smear crescent along the ground into frames 5-6; the game draws the claw slash itself now (CombatFxView "beastslash"), so those frames are replaced by the apex pose (frame 4, held) and the forward-lean frame 7: same count, same timing, no low smear
REMAP = {"rake": [0, 1, 2, 3, 4, 4, 7, 7, 8, 9, 10, 11, 12], "sweep": [0, 1, 2, 3, 4, 4, 7, 7, 8, 9, 10, 11, 12]}
SOURCE = {"howl": "roar"}      # clips cut from another clip's frames (the howl is the roar's first nine)
CLIPS = {"walk": 8, "howl": 9, "slam": 13, "rake": 13, "charge": 9, "sweep": 13, "roar": 13}      # clip -> how many of its frames to keep (walk: the 9th repeats the first)
def hard(im):
    a = np.array(im.convert("RGBA")); a[..., 3] = np.where(a[..., 3] >= 128, 255, 0); a[a[..., 3] == 0] = 0; return Image.fromarray(a, "RGBA")
CLEAN = {"howl", "roar"}      # 2026-11-21 (Yan: "the howl has some artifacts around it"): the generator leaves stray specks round the body in these clips; keep the body and anything within 4 px of it
def despeckle(im):
    from scipy import ndimage
    a = np.array(im); m = a[..., 3] > 0; lab, n = ndimage.label(ndimage.binary_dilation(m, iterations=4))
    if n < 2: return im
    big = lab == (1 + int(np.argmax(ndimage.sum(m, lab, range(1, n + 1))))); a[~(big & m)] = 0; return Image.fromarray(a, "RGBA")
PIN = {"walk"}      # 2026-11-21 (Yan: "why does he walk doing little jumps"): the generated walk cycle drifts, the whole body rising up to 15 px and sliding sideways; every frame is moved rigidly (no resampling) so its lowest pixel sits on one row and its mass centre on one column
def pin(ims):
    ys = [np.where(np.array(i)[..., 3].any(1))[0].max() for i in ims]; cx = []
    for i in ims:
        a = np.array(i)[..., 3] > 0; cx.append((np.where(a)[1]).mean())
    ty = int(round(np.median(ys))); tx = float(np.median(cx)); out = []
    for i, y, c in zip(ims, ys, cx):
        o = Image.new("RGBA", i.size, (0, 0, 0, 0)); o.alpha_composite(i, (int(round(tx - c)), ty - y)); out.append(o)
    return out
def main():
    os.makedirs(OUT, exist_ok=True)
    for name, n in CLIPS.items():
        fs = sorted(glob.glob(os.path.join(SRC, SOURCE.get(name, name), "f_*.png")))[:n]
        assert len(fs) == n, (name, len(fs))
        ims = [hard(Image.open(f)) for f in fs]
        if name in CLEAN: ims = [despeckle(i) for i in ims]
        if name in PIN: ims = pin(ims)
        w, h = ims[0].size
        if name in REMAP: ims = [ims[i] for i in REMAP[name]]
        strip = Image.new("RGBA", (w * n, h), (0, 0, 0, 0))
        for i, im in enumerate(ims): strip.alpha_composite(im, (i * w, 0))
        buf = io.BytesIO(); strip.save(buf, "PNG"); open(os.path.join(OUT, "verger_beast_%s.bytes" % name), "wb").write(buf.getvalue())
        print(name, n, "frames", (w, h), "->", strip.size)
if __name__ == "__main__": main()
