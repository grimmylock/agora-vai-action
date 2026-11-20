# The beast's Pixel Lab clips -> game strips (2026-11-20; Yan: "use the spritethingy to do the attacks and animations like we did the Gaunt"). Source: Yan's generated stand (ai/poses/stand.png) animated with Pixel Lab animate-pixminimax
# (ArtSource/pixellab_anim.py, README_BEAST.md has the prompts, seeds, jobs and what each clip does). Every frame is a 256x232 cell with the stand sprite at the bottom centre (feet 6 px above the bottom, so the pivot is 22/232).
# Writes Assets/Resources/Sprites/Mutants/anim/verger_beast_<clip>.bytes (a PNG strip, hard alpha, decoded in code) for the clips below; the timing table is VergerBoss.Clips (VergerBoss.cs) - keep the frame counts in step.
import os, sys, io, glob
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); SRC = os.path.join(HERE, "mutants_anim", "verger_beast", "pixellab")
OUT = os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "anim")
CLIPS = {"walk": 8, "slam": 13, "rake": 13, "charge": 9, "sweep": 13, "roar": 13}      # clip -> how many of its frames to keep (walk: the 9th repeats the first)
def hard(im):
    a = np.array(im.convert("RGBA")); a[..., 3] = np.where(a[..., 3] >= 128, 255, 0); a[a[..., 3] == 0] = 0; return Image.fromarray(a, "RGBA")
def main():
    os.makedirs(OUT, exist_ok=True)
    for name, n in CLIPS.items():
        fs = sorted(glob.glob(os.path.join(SRC, name, "f_*.png")))[:n]
        assert len(fs) == n, (name, len(fs))
        ims = [hard(Image.open(f)) for f in fs]; w, h = ims[0].size
        strip = Image.new("RGBA", (w * n, h), (0, 0, 0, 0))
        for i, im in enumerate(ims): strip.alpha_composite(im, (i * w, 0))
        buf = io.BytesIO(); strip.save(buf, "PNG"); open(os.path.join(OUT, "verger_beast_%s.bytes" % name), "wb").write(buf.getvalue())
        print(name, n, "frames", (w, h), "->", strip.size)
if __name__ == "__main__": main()
