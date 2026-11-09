"""Applies the grit pass (grit_pass.py, 24%) to the eel's and the brain vat's game sprites in Assets/Resources/Sprites (Yan approved it 2026-11-09).
Idempotent: ArtSource/grit_applied.json remembers the hash of each file it produced, so running it again leaves finished files alone; a file the art pipeline regenerates
(plain again) is gritted again. The plain originals are in git history (before the commit "Grit pass applied to the eel and the brain vat").
    python3 ArtSource/apply_grit.py          # apply
    python3 ArtSource/apply_grit.py --check  # list what would change"""
import os, sys, json, hashlib, glob
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import grit_pass as G

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
FX = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Fx"); MUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Mutants")
MANIFEST = os.path.join(os.path.dirname(os.path.abspath(__file__)), "grit_applied.json")
AMOUNT = 0.24
SKIP = {"boss_eel_swim_shadow.png"}                                  # a shadow, not art

def cell_of(path, size):
    n = os.path.basename(path)
    if n == "boss_brain_vat.png": return (128, 160)                  # 8 x 11 frames
    if size[1] == 340: return (620, 340)                             # the eel's move strips: frames 620 wide
    return None                                                      # body strips, heads, jaws, swim: one cell

def sha(path): return hashlib.sha256(open(path, "rb").read()).hexdigest()

def main():
    done = json.load(open(MANIFEST)) if os.path.exists(MANIFEST) else {}
    files = sorted(f for f in glob.glob(os.path.join(FX, "boss_eel_*.png")) if os.path.basename(f) not in SKIP) + [os.path.join(MUT, "boss_brain_vat.png")]
    n = 0
    for f in files:
        rel = os.path.relpath(f, ROOT).replace("\\", "/")
        if done.get(rel) == sha(f): continue
        n += 1
        if "--check" in sys.argv: print("would grit", rel); continue
        im = Image.open(f); out = G.grit(im, AMOUNT, cell_of(f, im.size)); assert out.size == im.size
        out.save(f); done[rel] = sha(f)
    if "--check" not in sys.argv: json.dump(done, open(MANIFEST, "w"), indent=1, sort_keys=True)
    print(f"{n} files {'would change' if '--check' in sys.argv else 'gritted'}, {len(files) - n} already done")

if __name__ == "__main__":
    main()
