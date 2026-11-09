"""Yan, 2026-11-09: "keep the current sprites but doubled". The armor/NPC sprites are drawn at 16x16 (Assets/Resources/Sprites/armor_*.png, written by make_armor_avatars,
make_back_atlas, make_walk_atlas, make_poses atlas, make_unique_armor: all of that still works at 16 px and every Python tool that reads those files keeps working). This script writes a
DOUBLED copy next to each, `armor_*_x2.png` (2x2 nearest-neighbour: the same picture, the same size on screen), and UnitSprites loads the `_x2` file when it exists (cell size read from
the texture), so wounds, blood and finer suit detail can be drawn at the mutants' density later. Re-run it after any change to the 16 px files.
    python3 ArtSource/double_armor.py"""
import os, sys
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); DIR = os.path.join(HERE, "..", "Assets", "Resources", "Sprites")
def main():
    n = 0
    for f in sorted(os.listdir(DIR)):
        if not (f.startswith("armor_") and f.endswith(".png")) or f.endswith("_x2.png"): continue
        im = Image.open(os.path.join(DIR, f)).convert("RGBA")
        im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(os.path.join(DIR, f[:-4] + "_x2.png")); n += 1
    print(n, "files doubled into", os.path.normpath(DIR))
if __name__ == "__main__": main()
