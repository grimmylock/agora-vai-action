# 2026-11-22: the Orderly's four attack clips assembled frame by frame from the clean candidates (cand/<clip>_s<seed>/f_NN.png, 276x178); frames with ghost smears are replaced by clean ones from another seed/clip. Run from orderly_skel/.
import os, shutil
from PIL import Image
C = "cand/%s/f_%02d.png"
SETS = {
 "saw_rush": [("saw_rush_s11", k) for k in range(8)],
 "lash": [("lash_s11", k) for k in range(8)],
 "lamp_tear": [("STAND", 0), ("lamp_tear_s7", 1), ("lamp_tear_s11", 2), ("lamp_tear_s11", 3), ("lamp_tear_s11", 4), ("lamp_tear_s11", 4), ("lamp_tear_s11", 6), ("lamp_tear_s11", 7)],
 # overhead raise from the clean lamp-tear frames, then the chop and the flat sweep of the far blade
 "double_saw": [("STAND", 0), ("lamp_tear_s7", 1), ("lamp_tear_s11", 3), ("double_saw_s7", 3), ("double_saw_s7", 4), ("double_saw_s7", 5), ("double_saw_s7", 6), ("double_saw_s11", 7)],
}
for clip, seq in SETS.items():
    d = f"../orderly/{clip}"; shutil.rmtree(d, ignore_errors=True); os.makedirs(d)
    for i, (t, k) in enumerate(seq):
        src = "../orderly/idle/f_00.png" if t == "STAND" else C % (t, k); Image.open(src).convert("RGBA").save(f"{d}/f_{i:02d}.png")
print("ok")
