#!/usr/bin/env python3
"""Regenerates ArtSource/weapons/WEAPON_STATUS.md: every weapon in the game, whether its picture is REWORKED (double-size, listed in WeaponArt.Dense in HeldWeapon.cs = the source of truth) or still OLD, with its
reference photo (from ArtSource/weapons/CREDITS.txt) and real-world pattern (ArtSource/NAME_CHANGES.md) when known.   python3 ArtSource/weapons/make_weapon_status.py"""
import os, re
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))); HERE = os.path.join(ROOT, "ArtSource", "weapons")
db = open(os.path.join(ROOT, "Assets/Scripts/GameData/WeaponDatabase.cs")).read()
held = open(os.path.join(ROOT, "Assets/Scripts/GameCore/HeldWeapon.cs")).read(); m = re.search(r"Dense = new Dictionary<string, float>\s*\{(.*?)\};", held, re.S); dense = dict(re.findall(r'\["(\w+)"\] = ([0-9.]+)f', m.group(1)))
rows = []; cat = None
for line in db.splitlines():
    c = re.match(r'\s*\["(\w+)"\] = new Dictionary<string, WeaponDef>', line)
    if c: cat = c.group(1); continue
    w = re.match(r'\s*\["(\w+)"\] = new WeaponDef \{ Name = "([^"]+)"(.*)', line)
    if w and cat:
        t = re.search(r"Tier = (\d+)", w.group(3)); rows.append((cat, w.group(1), w.group(2), int(t.group(1)) if t else 0))
credits = {}; lines = open(os.path.join(HERE, "CREDITS.txt"), encoding="utf-8", errors="ignore").read().splitlines()
for i, l in enumerate(lines):
    k = re.match(r"^(\w+)\.(png|jpg|jpeg)\s*$", l.strip())
    if k and i + 2 < len(lines): credits[k.group(1)] = (lines[i + 1].strip(), re.sub(r"\s*\|.*", "", lines[i + 2].replace("License:", "").strip()))
# references that replaced the old pipeline's photo (the file CREDITS.txt keeps the old one)
credits["Nightwalker4570"] = ("photo supplied by Yan 2026-11-13 (black polymer lever rifle; source unknown, drawing reference only)", "licence unknown")
import json
real = json.load(open(os.path.join(HERE, "gun_real_sizes.json")))
def planned(k):
    return min(156, int(round(102 * (real[k][0] / 29.0) ** 0.28))) if k in real else None
names = {}
nc = os.path.join(ROOT, "ArtSource", "NAME_CHANGES.md")
if os.path.exists(nc):
    for l in open(nc, encoding="utf-8", errors="ignore"):
        p = [x.strip() for x in l.strip().strip("|").split("|")]
        if len(p) >= 2 and p[0] and p[1] and not p[0].startswith("-"): names[p[1]] = p[0]
melee = [r for r in rows if r[0] == "melee"]; guns = [r for r in rows if r[0] != "melee"]
done = [r for r in guns if r[1] in dense]; todo = [r for r in guns if r[1] not in dense]
out = ["# Weapon pictures: reworked vs old (generated, do not edit by hand)", "",
       "Regenerate with `python3 ArtSource/weapons/make_weapon_status.py`. **Reworked** = the gun is listed in `WeaponArt.Dense` (`Assets/Scripts/GameCore/HeldWeapon.cs`): its picture is the DOUBLE-SIZE one (160x64 canvas) made by `make_gun_x2.py`, used for the menus AND in the hand.",
       "**Old** = the hand-pixelled 64x32 menu picture and the small in-hand picture from before 2026-11-13. How to rework one: `WEAPON_SPRITE_RULES.md`.", "",
       f"**Guns: {len(done)} reworked, {len(todo)} old** (melee weapons are a separate hand-pixelled set, {len(melee)} of them, not part of this rework).", "",
       "## Reworked", "", "| Key | In-game name | Category | Real-world pattern | Real length (cm) | Picture length (px) | Reference photo (source, licence) | Density k |", "|-|-|-|-|-|-|-|-|"]
def ref(k): c = credits.get(k); return f"{c[0]} ({c[1]})" if c else "none on file"
for cat_, k, n, t in done: out.append(f"| {k} | {n} | {cat_} | {names.get(n, '')} | {real.get(k, ['?'])[0]} | {planned(k)} | {ref(k)} | {dense[k]} |")
out += ["", "## Old (still to do)", "", "| Key | In-game name | Category | Tier | Real-world pattern | Real length (cm) | Planned picture length (px) | Reference photo on file (source, licence) |", "|-|-|-|-|-|-|-|-|"]
for cat_, k, n, t in sorted(todo, key=lambda r: (r[0], r[3], r[2])): out.append(f"| {k} | {n} | {cat_} | {t} | {names.get(n, '')} | {real.get(k, ['?'])[0]} | {planned(k)} | {ref(k)} |")
out += ["", "## Melee (separate set, not part of this rework)", "", ", ".join(f"{n} (`{k}`)" for _, k, n, _ in melee), ""]
open(os.path.join(HERE, "WEAPON_STATUS.md"), "w", encoding="utf-8").write("\n".join(out)); print(len(done), "reworked,", len(todo), "old,", len(melee), "melee")
