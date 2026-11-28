#!/usr/bin/env python3
"""Plan compiler: ArtSource/plans/*.plan  ->  Assets/Scripts/GameCore/InteriorPlans.Generated.cs  (partial class Interiors).
Replaces the per-building throwaway Python scripts: write the ASCII layout, run `python3 Tools/plans/plan.py`, it checks and emits the C#.
(The harness then checks every plan again: floors, reachability from all doors, nothing in a row the wall below hides, and that the stamped plan survives the map build.)

File format (several plans per file are fine):
    plan CabinC            # name = the C# field (Interiors.CabinC and Interiors.CabinCFloor)
    floor w                # floor letter under every tile that is not a wall/window/door: w wood, t lab tile, k kitchen tile, c concrete, d dirt, g gravel, s sand
    region k 1 1 5 4       # optional, repeatable: floor letter k for x 1..5, y 1..4 (inclusive, plan coordinates)
    mirror CabinCB         # optional: also emit a left-right mirrored copy under that name (east/west facing glyphs are swapped)
    rows
    ###n###
    #..K..#
    ...
    end
Glyphs: '#' wall, 'n' window, 'D' door, '.' floor, any furniture glyph of Interiors.Pieces (the tool reads the legend from Interiors.cs).
Usage: python3 Tools/plans/plan.py [--check]      (--check: validate only, write nothing)
"""
import re, sys, glob, os
from collections import deque
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
OUT = os.path.join(ROOT, "Assets", "Scripts", "GameCore", "InteriorPlans.Generated.cs")
SWAP = {"A": "I", "I": "A", "1": "2", "2": "1", "3": "4", "4": "3", "L": "Z", "Z": "L", "E": "W", "W": "E", "5": "6", "6": "5", "t": "j", "j": "t"}
def glyphs():
    src = open(os.path.join(ROOT, "Assets", "Scripts", "GameCore", "Interiors.cs")).read()
    return set(re.findall(r"\['(.)'\] = \(\"", src)) | set("#nD.")
def parse(path):
    plans, cur = [], None
    for ln, raw in enumerate(open(path).read().split("\n"), 1):
        line = raw.split("#", 1)[0].rstrip() if raw.strip().startswith(("plan ", "floor ", "region ", "mirror ")) and "#" in raw and not raw.strip().startswith("##") else raw.rstrip()
        s = line.strip()
        if cur is None:
            if s.startswith("plan "): cur = {"name": s.split()[1], "floor": "w", "regions": [], "mirror": None, "rows": None, "file": path, "line": ln}
            elif s and not s.startswith("#"): raise SystemExit(f"{path}:{ln}: expected 'plan <Name>'")
        elif cur.get("_inff"):
            if s == "endfloor": cur["_inff"] = False
            else: cur["floorrows"].append(s)
        elif cur["rows"] is not None:
            if s == "end": plans.append(cur); cur = None
            else: cur["rows"].append(line)
        elif s == "floorrows": cur["floorrows"] = []; cur["_inff"] = True            # the editor's per-tile floor letters (one row per plan row, '.' = the default floor), ended by 'endfloor'
        elif s.startswith("floor "): cur["floor"] = s.split()[1]
        elif s.startswith("region "): p = s.split(); cur["regions"].append((p[1], *map(int, p[2:6])))
        elif s.startswith("mirror "): cur["mirror"] = s.split()[1]
        elif s == "rows": cur["rows"] = []
    if cur is not None: raise SystemExit(f"{path}: plan {cur['name']} has no 'end'")
    return plans
def floors(p):
    rows = p["rows"]; h, w = len(rows), len(rows[0]); out = []
    for y in range(h):
        r = ""
        for x in range(w):
            ch = rows[y][x]
            if ch in "#nD": r += "."; continue
            f = p["floor"]
            fr = p.get("floorrows")
            if fr and y < len(fr) and x < len(fr[y]) and fr[y][x] != ".": r += fr[y][x]; continue
            for (fl, x0, y0, x1, y1) in p["regions"]:
                if x0 <= x <= x1 and y0 <= y <= y1: f = fl
            r += f
        out.append(r)
    return out
def mirrored(rows): return ["".join(SWAP.get(c, c) for c in r[::-1]) for r in rows]
def check(p, known):
    rows, errs = p["rows"], []
    if not rows: return ["no rows"]
    w = len(rows[0]); h = len(rows)
    for y, r in enumerate(rows):
        if len(r) != w: errs.append(f"row {y} is {len(r)} wide, row 0 is {w}")
        for x, c in enumerate(r):
            if c not in known: errs.append(f"unknown glyph {c!r} at {x},{y}")
    if errs: return errs
    # furniture in the row the wall below hides: nudge up one row (as the harness demands), report it
    for y in range(h - 2, 0, -1):
        for x in range(w):
            c = rows[y][x]
            if c not in "#nD." and rows[y + 1][x] in "#n":
                if rows[y - 1][x] != ".": errs.append(f"{c!r} at {x},{y} is hidden by the wall below and the tile above is taken")
                else: rows[y - 1] = rows[y - 1][:x] + c + rows[y - 1][x + 1:]; rows[y] = rows[y][:x] + "." + rows[y][x + 1:]; print(f"  nudged {c!r} at {x},{y} up one row (hidden by the wall below)")
    seen, q = set(), deque()
    for y in range(h):
        for x in range(w):
            if rows[y][x] == "D":
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and rows[ny][nx] == "." and (nx, ny) not in seen: seen.add((nx, ny)); q.append((nx, ny))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and rows[ny][nx] == "." and (nx, ny) not in seen: seen.add((nx, ny)); q.append((nx, ny))
    lost = [(x, y) for y in range(h) for x in range(w) if rows[y][x] == "." and (x, y) not in seen]
    if lost: errs.append(f"{len(lost)} floor tiles cannot be reached from a door, e.g. {lost[:6]}")
    if not any("D" in r for r in rows): errs.append("no door")
    return errs
def emit(name, rows, fl):
    a = "".join(f'            "{r}",\n' for r in rows); b = "".join(f'            "{r}",\n' for r in fl)
    return f"        public static readonly string[] {name} = {{\n{a}        }};\n        public static readonly string[] {name}Floor = {{\n{b}        }};\n"
def main():
    known = glyphs(); plans = []
    for f in sorted(glob.glob(os.path.join(ROOT, "ArtSource", "plans", "*.plan"))): plans += parse(f)
    code, bad = [], 0
    for p in plans:
        errs = check(p, known)
        if errs: bad += 1; print(f"FAIL {p['name']} ({os.path.basename(p['file'])}:{p['line']})"); [print("   ", e) for e in errs]; continue
        code.append(emit(p["name"], p["rows"], floors(p))); print(f"ok   {p['name']} {len(p['rows'][0])}x{len(p['rows'])}")
        if p["mirror"]:
            m = dict(p); m["name"], m["rows"], m["regions"] = p["mirror"], mirrored(p["rows"]), [(f, 0 + (len(p["rows"][0]) - 1 - x1), y0, (len(p["rows"][0]) - 1 - x0), y1) for (f, x0, y0, x1, y1) in p["regions"]]
            code.append(emit(m["name"], m["rows"], floors(m))); print(f"ok   {m['name']} (mirror)")
    if bad: raise SystemExit(f"{bad} plan(s) failed; nothing written")
    if "--check" in sys.argv: return
    hdr = "// GENERATED by Tools/plans/plan.py from ArtSource/plans/*.plan - edit the .plan files, not this file.\nnamespace GameCore\n{\n    public static partial class Interiors\n    {\n"
    open(OUT, "w").write(hdr + "\n".join(code) + "    }\n}\n"); print(f"wrote {len(code)} arrays -> {os.path.relpath(OUT, ROOT)}")
if __name__ == "__main__": main()
