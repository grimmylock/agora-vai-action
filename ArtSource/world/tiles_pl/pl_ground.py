# 2026-11-21: ground textures from the Pixel Lab tilesets (make_tileset.py): the all-"lower" wang tile is the seamless floor; 64x64 = 4x4 of it, each cell flipped / rotated differently so the repeat does not show.
# TILESETS maps the ground name (WorldArt.GroundName) to (tileset json, which terrain: "lower" or "upper").
import os, io, json, base64, random
from PIL import Image, ImageEnhance
H = os.path.dirname(os.path.abspath(__file__))
TILESETS = {"cavefloor": ("cave.json", "lower"), "labtile": ("lab.json", "lower"), "floor": ("wood.json", "lower")}   # Yan 2026-11-21: "keep cave floor and lab tiles" (concrete, hospital floor and rock top were trials; their jsons stay)
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
def pattern(jsonname, terrain, seed=7, transpose=True):
    t = base_tile(jsonname, terrain)
    if t is None: return None
    r = random.Random(seed); w, h = t.size; out = Image.new("RGBA", (w * 4, h * 4))
    for cy in range(4):
        for cx in range(4):
            v = t
            if r.random() < 0.5: v = v.transpose(Image.FLIP_LEFT_RIGHT)
            if r.random() < 0.5: v = v.transpose(Image.FLIP_TOP_BOTTOM)
            if transpose and r.random() < 0.5: v = v.transpose(Image.TRANSPOSE)
            out.paste(v, (cx * w, cy * h))
    return out
def register(GROUNDS):
    for name, (j, terr) in TILESETS.items():
        tr = name != "floor"          # planks keep their direction: no transposing the wooden floor
        if pattern(j, terr) is not None: GROUNDS[name + "_pl"] = (lambda jj=j, tt=terr, t2=tr: (lambda seed=3: pattern(jj, tt, seed, t2)))()

# ---- wall faces (2026-11-21, Yan: "try walls next"): the tilesets' all-"upper" tile (a seamless wall / rock / plate texture) as the front face of a wall material; the autotile geometry, doors, windows and weathering of walls.py stay.
WALL_FACES = {"plaster": ("hospital.json", "upper", 0.30, 0.72), "concrete": ("lab.json", "upper", 1.0, 1.0), "cliff": ("cave.json", "upper", 1.0, 1.0), "hull": ("concrete.json", "upper", 0.25, 0.62)}   # (tileset, terrain, saturation, brightness): the green plaster and the orange plate came out loud
def face_fn(jsonname, terrain, sat=1.0, val=1.0):
    t = base_tile(jsonname, terrain)
    if t is None: return None
    if sat != 1.0 or val != 1.0:
        from PIL import ImageEnhance; a = t.getchannel("A"); t = ImageEnhance.Brightness(ImageEnhance.Color(t.convert("RGB")).enhance(sat)).enhance(val).convert("RGBA"); t.putalpha(a)
    px = t.convert("RGBA").load(); w, h = t.size
    def f(x, k, H, r):
        c = px[x % w, k % h]; sh = 1.04 - 0.28 * (k / max(1, H - 1))          # lit at the top edge, darker toward the floor
        return (min(255, int(c[0] * sh)), min(255, int(c[1] * sh)), min(255, int(c[2] * sh)), 255)
    return f
def apply_walls(MATERIALS):
    for mat, (j, terr, sat, val) in WALL_FACES.items():
        fn = face_fn(j, terr, sat, val)
        if fn is None or mat not in MATERIALS: continue
        H, T, organic, cap, face, lip = MATERIALS[mat]; MATERIALS[mat] = (H, T, organic, cap, fn, lip)
