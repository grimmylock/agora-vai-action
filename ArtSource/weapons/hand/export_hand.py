"""Exports every hand-pixelled weapon: Sprites/Weapons/weapon_<Key>.png (B, in hand) + weapon_<Key>_hd.png (A, menus)
and the C# WeaponArt.Grips rows (grips.cs.txt). Replaces the photo pipeline (export_weapons.py) for all 59 weapons."""
import sys, os, math
sys.path.insert(0, '/home/claude/art/weapons/hand')
import kit
OUT = sys.argv[1] if len(sys.argv) > 1 else '/tmp/claude-0/weapons_hand_out'
os.makedirs(OUT, exist_ok=True)
keys = [k for c in kit.CFG.values() for k in c] + ["RK12Kuvalda"]
rows = []
for k in keys:
    g = kit.load(k)
    kit.b_image(g).save(f'{OUT}/weapon_{k}.png'); kit.a_image(g).save(f'{OUT}/weapon_{k}_hd.png')
    f = lambda v: "float.NaN" if math.isnan(v) else f"{round(v * 2) / 2:g}f"
    rows.append(f'            ["{k}"] = new[] {{ {", ".join(f(v) for v in kit.grips(g))} }},')
open(f'{OUT}/grips.cs.txt', 'w').write("\n".join(rows) + "\n")
print(len(keys), "weapons ->", OUT)
