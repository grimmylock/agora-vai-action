# The brain in the vat, re-animated with Pixel Lab (2026-11-21; Yan: "we'll probably need to reanimate the brain in the vat boss now that we have credits, so reanimate him first").
# The vat sheet keeps its layout (8 columns x 11 rows of 128x160: rows 0-3 eyes open by crack phase, 4-7 eyes shut winding up, 8 drained, 9 floating up, 10 the eye; BrainBoss.ArtRow/ArtCol),
# so the game code does not change. Each row is generated from its own first frame (animate-pixminimax, the cell sent as it is), and only the pixels INSIDE THE GLASS are taken from the
# generated frames: the tank, the machines and the cables stay the hand-built ones pixel for pixel, and the white crack lines of that phase are laid back on top (they are on the glass, in front).
#   python3 ArtSource/bosses/vat_reanimate.py gen      # one Pixel Lab job per row (11), into ArtSource/bosses/vat_pixellab/row_<n>/ (skips rows already made)
#   python3 ArtSource/bosses/vat_reanimate.py build    # composes the new sheet -> Assets/Resources/Sprites/Mutants/boss_brain_vat.png (the old one is kept as boss_brain_vat_v1.png in vat_pixellab/)
import os, sys, io, json, base64, time, urllib.request, shutil
import numpy as np
from PIL import Image
H = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(H, "vat_pixellab"); SHEET = os.path.join(H, "..", "..", "Assets", "Resources", "Sprites", "Mutants", "boss_brain_vat.png")
W, HH, COLS, ROWS = 128, 160, 8, 11
GLASS = (38, 36, 94, 124)                                   # x0, y0, x1, y1 of the tank's inside (end exclusive)
STILL = "The glass tank, its cracks, the machines, cables and pipes stay completely still; only what is inside the tank moves."
OPEN = "The pink brain floating in the green liquid of the tank pulses and throbs slowly, its glowing violet eyes blink and look around, the dangling nerve stem sways, small bubbles rise. " + STILL
SHUT = "The brain floating in the tank squeezes its eyes shut and trembles hard, swelling and throbbing as it gathers psychic power, its veins pulsing, the liquid churning around it. " + STILL
PLAN = {0: (OPEN, 1), 1: (OPEN, 1), 2: (OPEN, 1), 3: (OPEN + " It is badly hurt and twitches.", 1), 4: (SHUT, 1), 5: (SHUT, 1), 6: (SHUT, 1), 7: (SHUT, 1),
        8: ("The brain hangs limp in the dark murky liquid and sinks a little, barely twitching, its eyes dull. " + STILL, 1),
        9: ("The brain slowly floats upward inside the tank, its nerve stem trailing below it, the liquid swirling. " + STILL, 0),
        10: ("Behind the brain inside the tank a giant eye slowly opens and stares straight out at the viewer. " + STILL, 0)}
def b64(im): b = io.BytesIO(); im.save(b, "PNG"); return {"type": "base64", "base64": base64.b64encode(b.getvalue()).decode()}
def call(url, data=None):
    req = urllib.request.Request("https://api.pixellab.ai" + url, data=json.dumps(data).encode() if data else None, headers={"Content-Type": "application/json"})
    return json.load(urllib.request.urlopen(req, timeout=120))
def cell(sheet, r, c): return sheet.crop((c * W, r * HH, (c + 1) * W, (r + 1) * HH))
def gen(only=None):
    src = os.path.join(OUT, "boss_brain_vat_v1.png")
    if not os.path.exists(src): shutil.copy(SHEET, src)
    sheet = Image.open(src).convert("RGBA")
    for r, (desc, loop) in PLAN.items():
        if only is not None and r not in only: continue
        d = os.path.join(OUT, "row_%d" % r)
        if os.path.exists(os.path.join(d, "f_00.png")): continue
        os.makedirs(d, exist_ok=True); first = cell(sheet, r, 0); last = cell(sheet, r, 7)
        body = {"first_frame": b64(first), "description": desc, "frame_count": 8, "seed": 900 + r, "no_background": True, "enhance_prompt": True}
        if loop: body["last_frame"] = b64(first)
        else: body["last_frame"] = b64(last)                  # the one-shots end where the hand-built row ends (fully risen; the eye fully open)
        j = call("/v2/animate-pixminimax", body); jid = j["background_job_id"]
        for _ in range(120):
            time.sleep(5); s = call("/v2/background-jobs/" + jid)
            if s.get("status") in ("completed", "failed"): break
        res = s.get("last_response") or s; fr = res.get("images") or res.get("frames") or []
        for i, f in enumerate(fr):
            dd = f.get("base64") if isinstance(f, dict) else f
            Image.open(io.BytesIO(base64.b64decode(dd.split(",")[-1]))).convert("RGBA").save(os.path.join(d, "f_%02d.png" % i))
        open(os.path.join(OUT, "gens.log"), "a").write("row %d\tseed %d\t%s\t%d frames\n" % (r, 900 + r, s.get("status"), len(fr))); print("row", r, s.get("status"), len(fr), flush=True)
def build():
    old = Image.open(os.path.join(OUT, "boss_brain_vat_v1.png")).convert("RGBA"); new = old.copy(); x0, y0, x1, y1 = GLASS
    for r in range(ROWS):
        d = os.path.join(OUT, "row_%d" % r); fs = sorted(f for f in os.listdir(d) if f.startswith("f_")) if os.path.isdir(d) else []
        if len(fs) < 8: print("row", r, "kept (no new frames)"); continue
        loop = PLAN[r][1]; pick = fs[:8]                    # a loop: frames 0..7 (the 9th repeats the first); a one-shot: the first 8 of 9
        for c in range(COLS):
            base = np.array(cell(old, r, c)); g = np.array(Image.open(os.path.join(d, pick[c])).convert("RGBA").resize((W, HH), Image.NEAREST) if Image.open(os.path.join(d, pick[c])).size != (W, HH) else Image.open(os.path.join(d, pick[c])).convert("RGBA"))
            out = base.copy(); out[y0:y1, x0:x1] = g[y0:y1, x0:x1]
            crack = (base[..., 0] > 200) & (base[..., 1] > 200) & (base[..., 2] > 200) & (base[..., 3] > 0)   # the phase's glass cracks: white, in front of everything in the tank
            out[crack] = base[crack]
            out[y0:y1, x0:x1, 3] = np.where(out[y0:y1, x0:x1, 3] == 0, base[y0:y1, x0:x1, 3], out[y0:y1, x0:x1, 3])   # never punch a hole in the tank where the generator left it empty
            new.paste(Image.fromarray(out), (c * W, r * HH))
        print("row", r, "rebuilt")
    new.save(SHEET); print("->", SHEET)
if __name__ == "__main__":
    if sys.argv[1:2] == ["gen"]: gen([int(x) for x in sys.argv[2:]] or None)
    else: build()
