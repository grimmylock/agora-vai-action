#!/usr/bin/env python3
"""Add a Pixel Lab prop to the game in one command (Yan, 2026-11-27).
  python3 Tools/props/addprop.py NAME "description" WIDTHxHEIGHT [--dirs 1|8] [--glyph X] [--hard] [--seed N]
Does: uploads the style anchor (the atlas desk), creates the object with Pixel Lab (create-object-pro-flash, view low top-down; WIDTH/HEIGHT multiples of 4),
waits, downloads it to ArtSource/world/settle_pl/raw_NAME.png (8 directions: raw_NAME_e/_w/_n/_s.png), registers the look in settle_pl/props_registry.json,
adds a plan glyph for it to Interiors.Pieces (the glyph is picked from the free ones unless --glyph; --hard = tall hard cover), regenerates ArtSource/ASSET_MANIFEST.json,
and writes a contact sheet to maps/renders/buildings/prop_NAME.png. Then: `python3 Tools/dev.py atlas` and use the glyph in a .plan (or c.Put(x, y, ScatterCover, "NAME") in TierMaps).
Costs about 5 generations (1 direction, up to 96 px) or 7+ (8 directions): the script prints the estimate before submitting and needs --yes to go on.
The Pixel Lab token comes from the environment (the cloud proxy adds it) or PIXELLAB_TOKEN / ArtSource/.pixellab_token (see ArtSource/pixellab_auth.py)."""
import os, sys, re, json, time, subprocess, zipfile, io, argparse, glob
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "ArtSource"))
SETTLE = os.path.join(ROOT, "ArtSource", "world", "settle_pl")
def api(path, data=None):
    import urllib.request, urllib.error, pixellab_auth
    req = urllib.request.Request("https://api.pixellab.ai" + path, data=json.dumps(data).encode() if data is not None else None, headers=pixellab_auth.HEADERS)
    try: return json.load(urllib.request.urlopen(req, timeout=180))
    except urllib.error.HTTPError as e: raise SystemExit(f"Pixel Lab {e.code}: {e.read()[:600].decode('utf8','replace')}")
def anchor():
    import base64; from PIL import Image
    p = os.path.join(SETTLE, "anchor_desk.png"); im = Image.open(p)
    return {"image": {"type": "base64", "base64": base64.b64encode(open(p, "rb").read()).decode(), "format": "png"}, "size": {"width": im.width, "height": im.height}, "usage_description": "match this furniture style: dark outline, muted weathered colours, same shading"}
def free_glyph(src):
    used = set(re.findall(r"\['(.)'\] = \(\"", src)) | set("#nD.")
    for c in "!@$%^&*()[]{}<>?~:;,-_+=|/xXQqJjZzVvUu0123456789":
        if c not in used and c not in "'\"\\": return c
    for c in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ":
        if c not in used: return c
    for c in "§£¥µÆØÞßÐÅÄÖÜÉÈÀÇÑ¤¶±×÷¿¡":
        if c not in used: return c
    raise SystemExit("no free glyph left")
def register(name, files, glyph, hard):
    reg_p = os.path.join(SETTLE, "props_registry.json"); reg = json.load(open(reg_p)) if os.path.exists(reg_p) else {}
    for look, suffix in files.items(): reg[look] = suffix
    json.dump(reg, open(reg_p, "w"), indent=1)
    ip = os.path.join(ROOT, "Assets", "Scripts", "GameCore", "Interiors.cs"); src = open(ip).read()
    g = glyph or free_glyph(src); entry = f"['{g}'] = (\"{name}\", {'true' if hard else 'false'}),"
    k = src.index("static GroundType FloorOf"); end = src.rfind("};", 0, k)
    open(ip, "w").write(src[:end] + "    " + entry + "\n        " + src[end:])
    return g
def main():
    ap = argparse.ArgumentParser(); ap.add_argument("name"); ap.add_argument("description"); ap.add_argument("size"); ap.add_argument("--dirs", type=int, default=1, choices=(1, 8))
    ap.add_argument("--glyph"); ap.add_argument("--hard", action="store_true"); ap.add_argument("--seed", type=int, default=0); ap.add_argument("--yes", action="store_true"); a = ap.parse_args()
    w, h = map(int, a.size.lower().split("x")); assert w % 4 == 0 and h % 4 == 0 and 16 <= w <= 256 and 16 <= h <= 256, "size: multiples of 4, 16..256"
    est = (5 if max(w, h) <= 96 else 6 if max(w, h) <= 208 else 9) if a.dirs == 1 else (6 if max(w, h) <= 88 else 8 if max(w, h) <= 128 else 10)
    print(f"{a.name}: {w}x{h}, {a.dirs} direction(s), about {est} generations"); 
    if not a.yes: raise SystemExit("add --yes to spend them")
    body = {"description": a.description, "name": a.name, "view": "low top-down", "image_size": {"width": w, "height": h}, "n_directions": a.dirs, "seed": a.seed,
            "style_image": anchor()}
    r = api("/v2/create-object-pro-flash", body); oid, jid = r["object_id"], r["background_job_id"]; print("object", oid, "job", jid)
    for _ in range(120):
        time.sleep(6); j = api("/v2/background-jobs/" + jid)
        if j.get("status") in ("completed", "failed"): break
    if j.get("status") != "completed": raise SystemExit("job " + str(j.get("status")))
    z = subprocess.run(["curl", "-s", f"https://api.pixellab.ai/mcp/objects/{oid}/download"], capture_output=True).stdout
    files = {}
    if a.dirs == 1:
        open(os.path.join(SETTLE, f"raw_{a.name}.png"), "wb").write(z); files[a.name] = a.name
    else:
        zf = zipfile.ZipFile(io.BytesIO(z))
        for d, suf in (("east", "e"), ("west", "w"), ("north", "n"), ("south", "s")):
            open(os.path.join(SETTLE, f"raw_{a.name}_{suf}.png"), "wb").write(zf.read(f"rotations/{d}.png")); files[f"{a.name}_{suf}"] = f"{a.name}_{suf}"
    g = register(a.name, files, a.glyph, a.hard); print("registered:", ", ".join(files), "| plan glyph:", repr(g) + (" (south-facing look)" if a.dirs == 1 else " is the first look only; add glyphs for the rotated looks with --glyph if you want them in plans"))
    subprocess.run([sys.executable, os.path.join(ROOT, "Tools", "asset_qa", "make_manifest.py")])
    from PIL import Image
    ims = [Image.open(os.path.join(SETTLE, f"raw_{f}.png")).convert("RGBA") for f in files]; W = sum(i.width * 3 + 10 for i in ims) + 10
    sh = Image.new("RGBA", (W, max(i.height for i in ims) * 3 + 16), (60, 50, 40, 255)); x = 8
    for i in ims: b = i.resize((i.width * 3, i.height * 3), Image.NEAREST); sh.paste(b, (x, 8), b); x += b.width + 10
    out = os.path.join(ROOT, "maps", "renders", "buildings", f"prop_{a.name}.png"); sh.save(out); print("contact sheet:", out); print("next: python3 Tools/dev.py atlas")
if __name__ == "__main__": main()
