# Pixel Lab create-tileset (2026-11-21; Yan: "try the 16px pixel lab tilesets"): python3 make_tileset.py <name> "<lower>" "<upper>" [seed] [mode]
# Saves the raw response to tiles_pl/<name>.json and every tile to tiles_pl/<name>/<tile name>.png. One job at a time.
import sys, os, io, json, time, base64, urllib.request
from PIL import Image
H = os.path.dirname(os.path.abspath(__file__)); name, lower, upper = sys.argv[1:4]; seed = int(sys.argv[4]) if len(sys.argv) > 4 else 1; mode = sys.argv[5] if len(sys.argv) > 5 else "pro"
body = {"lower_description": lower, "upper_description": upper, "tile_size": {"width": 16, "height": 16}, "mode": mode, "transition_size": 0.0, "view": "high top-down", "seed": seed}
def call(url, data=None):
    req = urllib.request.Request("https://api.pixellab.ai" + url, data=json.dumps(data).encode() if data else None, headers={"Content-Type": "application/json"})
    try: return json.load(urllib.request.urlopen(req, timeout=120))
    except urllib.error.HTTPError as e: print(e.code, e.read()[:600]); raise
r = call("/v2/create-tileset", body); jid = r.get("background_job_id"); print({k: v for k, v in r.items() if k != "tileset"})
if jid:
    for _ in range(90):
        time.sleep(5); j = call("/v2/background-jobs/" + jid)
        if j.get("status") in ("completed", "failed"): break
    r = j.get("last_response") or j
tid = r.get("tileset_id") or (r.get("tileset") or {}).get("id")
if tid and "tiles" not in (r.get("tileset") or r): r = call("/v2/tilesets/" + tid)   # the job result only names it: fetch the tiles
json.dump(r, open(os.path.join(H, name + ".json"), "w"))
ts = r.get("tileset") or r; os.makedirs(os.path.join(H, name), exist_ok=True); n = 0
def walk(o, path=""):
    global n
    if isinstance(o, dict):
        if "base64" in o and isinstance(o["base64"], str) and len(o["base64"]) > 100:
            Image.open(io.BytesIO(base64.b64decode(o["base64"].split(",")[-1]))).convert("RGBA").save(os.path.join(H, name, (path.strip("_") or "img") + ".png")); n += 1; return
        for k, v in o.items(): walk(v, path + "_" + str(k)[:24])
    elif isinstance(o, list):
        for i, v in enumerate(o): walk(v, path + "_" + str(i))
walk(ts); print("saved", n, "images ->", name)
