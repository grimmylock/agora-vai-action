#!/usr/bin/env python3
"""Builds Tools/editor/plan_editor.html: a single-file plan editor (open it in a browser, no server, no install).
  python3 Tools/editor/build_editor.py
It embeds the real prop sprites (cut from Assets/Resources/Sprites/world_atlas.bytes by world_atlas_meta.txt) for every glyph in Interiors.Pieces and the text of every ArtSource/plans/*.plan,
so re-run it after adding props or plans (dev.py atlas does not). The editor saves .plan files (Tools/plans/plan.py compiles them); see Tools/editor/README.md."""
import re, os, io, json, base64, glob
from PIL import Image
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
src = open(os.path.join(ROOT, "Assets/Scripts/GameCore/Interiors.cs"), encoding="utf8").read()
pieces = {g: (l, h == "true") for g, l, h in re.findall(r"\['(.)'\] = \(\"([a-z_0-9]+)\", (true|false)\)", src)}
meta = {}
for ln in open(os.path.join(ROOT, "Assets/Resources/Sprites/world_atlas_meta.txt")):
    p = ln.split()
    if len(p) >= 7: meta[p[0]] = tuple(map(int, p[1:7]))
atlas = Image.open(os.path.join(ROOT, "Assets/Resources/Sprites/world_atlas.bytes")).convert("RGBA")
out = {}
for g, (look, hard) in pieces.items():
    k = next((n for n in (f"p_{look}_0", f"p_{look}") if n in meta), None)
    if not k: out[g] = {"look": look, "hard": hard}; continue
    x, y, w, h, ox, oy = meta[k]; buf = io.BytesIO(); atlas.crop((x, y, x + w, y + h)).save(buf, "PNG")
    out[g] = {"look": look, "hard": hard, "img": base64.b64encode(buf.getvalue()).decode(), "w": w, "h": h, "ox": ox, "oy": oy}
plans = {os.path.basename(p): open(p, encoding="utf8").read() for p in sorted(glob.glob(os.path.join(ROOT, "ArtSource/plans/*.plan")))}
swap = {"A": "I", "I": "A", "1": "2", "2": "1", "3": "4", "4": "3", "L": "Z", "Z": "L", "E": "W", "W": "E", "5": "6", "6": "5", "t": "j", "j": "t"}
tpl = open(os.path.join(ROOT, "Tools/editor/plan_editor.template.html"), encoding="utf8").read()
html = tpl.replace("__DATA__", json.dumps({"pieces": out, "plans": plans, "swap": swap}, ensure_ascii=False))
open(os.path.join(ROOT, "Tools/editor/plan_editor.html"), "w", encoding="utf8").write(html)
print("plan_editor.html:", len(html) // 1024, "KB,", len(out), "glyphs,", len(plans), "plan files")
