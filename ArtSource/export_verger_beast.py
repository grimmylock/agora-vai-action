# The Verger's second form for the game (2026-11-20): the four hand-built poses (make_verger_beast_poses.py: stand, change, run, attack) laid on one common canvas in a strip, feet on the bottom row, centred by their bodies, written as
# Assets/Resources/Sprites/Mutants/anim/verger_beast.bytes (a PNG with the .bytes extension, decoded in code like the other mutant strips so no import setting can blur it). UnitSprites.HDFx("verger_beast", 4, pivot) reads it:
# frame 0 stand, 1 the change, 2 run (the trot's other beat is the stand), 3 attack. 32 px a tile like the other mutants; VergerBoss.BeastScale draws it at half that size. Faces right; the view mirrors it.
import os, sys, io
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_verger_beast_poses as bp
import make_verger_beast_coat as ct
from pxart import crop
W, H = 190, 190
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "Sprites", "Mutants", "anim")
def main():
    frames = [crop(ct.beast(), 1), crop(bp.draw("change"), 1), crop(bp.draw("run"), 1), crop(bp.draw("attack"), 1)]
    strip = Image.new("RGBA", (W * len(frames), H), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        assert f.width <= W and f.height <= H, (i, f.size)
        strip.alpha_composite(f, (i * W + (W - f.width) // 2, H - f.height))
    os.makedirs(OUT, exist_ok=True); buf = io.BytesIO(); strip.save(buf, "PNG")
    open(os.path.join(OUT, "verger_beast.bytes"), "wb").write(buf.getvalue()); print("strip", strip.size, [f.size for f in frames])
if __name__ == "__main__": main()
