# 2026-11-21: ground textures from the Pixel Lab tilesets (make_tileset.py): the all-"lower" wang tile is the seamless floor; 64x64 = 4x4 of it, each cell flipped / rotated differently so the repeat does not show.
# TILESETS maps the ground name (WorldArt.GroundName) to (tileset json, which terrain: "lower" or "upper").
import os, io, json, base64, random
from PIL import Image, ImageEnhance
H = os.path.dirname(os.path.abspath(__file__))
TILESETS = {"cavefloor": ("cave.json", "lower"), "labtile": ("lab.json", "lower")}   # Yan 2026-11-21: "keep cave floor and lab tiles" (concrete, hospital floor and rock top were trials; their jsons stay)
def _img(o): d = o["base64"] if isinstance(o, dict) else o; return Image.open(io.BytesIO(base64.b64decode(d.split(",")[-1]))).convert("RGBA")
def base_tile(jsonname, terrain):
    p = os.path.join(H, jsonname)
    if not os.path.exists(p): return None
    d = json.load(open(p))
    if "tileset" not in d and d.get("tileset_id"):       # the job result only names the tileset: fetch its tiles once
        import urllib.request; d = json.load(urllib.request.urlopen("https://api.pixellab.ai/v2/tilesets/" + d["tileset_id"], timeout=60)); json.dump(d, open(p, "w"))
    for t in d["tileset"]["tiles"]:
        if all(v == terrain for v in t["corners"].values()): return _img(t["image"]).convert("RGBA")
    return None
def pattern(jsonname, terrain, seed=7):
    t = base_tile(jsonname, terrain)
    if t is None: return None
    r = random.Random(seed); w, h = t.size; out = Image.new("RGBA", (w * 4, h * 4))
    for cy in range(4):
        for cx in range(4):
            v = t
            if r.random() < 0.5: v = v.transpose(Image.FLIP_LEFT_RIGHT)
            if r.random() < 0.5: v = v.transpose(Image.FLIP_TOP_BOTTOM)
            if r.random() < 0.5: v = v.transpose(Image.TRANSPOSE)
            out.paste(v, (cx * w, cy * h))
    return out
def register(GROUNDS):
    for name, (j, terr) in TILESETS.items():
        if pattern(j, terr) is not None: GROUNDS[name + "_pl"] = (lambda jj=j, tt=terr: (lambda seed=3: pattern(jj, tt, seed)))()
