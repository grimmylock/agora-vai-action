# Exports the canine attack animations to the game (2026-11-12): every strip as <key>_<attack>.bytes (a PNG, decoded in code like the walks), the acid effect strips, and ONE table,
# Assets/Resources/Sprites/Mutants/anim/mutant_attacks.json, with for each mutant and attack: the frame size, the frame durations, the travel (0..1 across a leap), the CONTACT frame (where the damage and
# the hit effects land), the sprite pivot, and for the Pallid's spit the spawn frame and the muzzle point of each frame.   python3 ArtSource/export_mutant_attacks.py
import json, os, shutil, numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "anim"); os.makedirs(OUT, exist_ok=True)
SETS = {"veil_prowler": "veil_attacks", "pallid_prowler": "pallid_attacks", "dread_prowler": "dread_attacks", "blind_maw": "maw_attacks"}
CONTACT = {"veil_prowler": {"bite": 5, "bite2": 7, "pounce": 8}, "pallid_prowler": {"bite": 5, "bite2": 7}, "blind_maw": {"bite": 5, "bite2": 7, "pounce": 8}, "dread_prowler": {"bite": 5, "bite2": 5, "pounce": 8}}
table = {}
for key, folder in SETS.items():
    d = os.path.join(HERE, "mutants_anim", folder); js = json.load(open(os.path.join(d, [f for f in os.listdir(d) if f.endswith(".json") and "attacks" in f][0])))[key]
    spit = json.load(open(os.path.join(d, "pallid_spit.json")))[key] if key == "pallid_prowler" else None
    table[key] = {}
    for atk, meta in js.items():
        strip = Image.open(os.path.join(d, f"{key}_{atk}_strip.png")).convert("RGBA"); fw, fh = meta["frame"]; n = strip.width // fw
        f0 = np.array(strip.crop((0, 0, fw, fh)))[..., 3] > 0; last = int(np.where(f0.any(axis=1))[0].max())          # the stand pose's lowest row = the ground contact row
        shutil.copy(os.path.join(d, f"{key}_{atk}_strip.png"), os.path.join(OUT, f"{key}_{atk}.bytes"))
        ms = meta["ms"]; hit = CONTACT[key][atk]
        table[key][atk] = {"frames": n, "w": fw, "h": fh, "ms": ms, "travel": meta["travel"], "contact": hit, "contact_ms": int(sum(ms[:hit])), "total_ms": int(sum(ms)), "pivot_y": round(((fh - (last + 1)) + 16) / fh, 5)}
    if spit:
        sp = spit["spit"]; strip = Image.open(os.path.join(d, "pallid_prowler_spit_strip.png")).convert("RGBA"); fw, fh = sp["frame"]
        f0 = np.array(strip.crop((0, 0, fw, fh)))[..., 3] > 0; last = int(np.where(f0.any(axis=1))[0].max())
        shutil.copy(os.path.join(d, "pallid_prowler_spit_strip.png"), os.path.join(OUT, "pallid_prowler_spit.bytes"))
        table[key]["spit"] = {"frames": strip.width // fw, "w": fw, "h": fh, "ms": sp["ms"], "travel": [0] * (strip.width // fw), "contact": sp["spawn_frame"], "contact_ms": int(sum(sp["ms"][:sp["spawn_frame"]])), "total_ms": int(sum(sp["ms"])),
                              "pivot_y": round(((fh - (last + 1)) + 16) / fh, 5), "muzzle": sp["muzzle"], "glob": spit["acid_glob"], "splat": spit["acid_splat"]}
        shutil.copy(os.path.join(d, "fx", "pallid_acid_glob_strip.png"), os.path.join(OUT, "pallid_acid_glob.bytes")); shutil.copy(os.path.join(d, "fx", "pallid_acid_splat_strip.png"), os.path.join(OUT, "pallid_acid_splat.bytes"))
json.dump(table, open(os.path.join(OUT, "mutant_attacks.json"), "w"), indent=1)
for k, v in table.items(): print(k, {a: (m["frames"], f'{m["w"]}x{m["h"]}', f'contact {m["contact_ms"]}/{m["total_ms"]} ms') for a, m in v.items()})
