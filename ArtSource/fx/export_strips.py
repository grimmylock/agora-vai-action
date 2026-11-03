"""pixelfx sheets -> the game's FX strips (RT fx 3). Each effect becomes one horizontal strip of square frames in
Assets/Resources/Sprites/Fx/px_<name>.png, the effect's origin (muzzle, impact point, anomaly centre) at the middle of every frame,
so CombatFxView plays it with pivot (0.5, 0.5); frames at 15 fps (every 2nd of pixelfx's 30). A .meta beside it lets Unity
import up to 16384 px wide, uncompressed. The colours are set when rendering (Zone ramps for guns, the library's own for electric):
    cd ArtSource/fx/pixelfx && node tools/render.mjs <id> --sheet --opts '<json>'      (see RENDERS below)
    python3 ArtSource/fx/export_strips.py
"""
import json, os, re, uuid, subprocess
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
PFX = os.path.join(HERE, "pixelfx")
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "Sprites", "Fx")
TEMPLATE = os.path.join(OUT, "fx_hit_spark.png.meta")
FIRE = ["#1a1210", "#3a2218", "#6a3a20", "#9a5a2a", "#c4823a", "#dcae62", "#eed69a", "#fbf2d8"]
GUN = {"palette": FIRE, "light": 0.4}
# game name -> (pixelfx id, render options, lift). lift = px above the pixelfx origin where the game's anchor is: pixelfx stands
# effects on the ground under a 24x38 character, so a muzzle flash sits `height` px up (26) - the game anchors it at the muzzle itself. Guns are drawn at half-pixels in game (ppu 32), electric at full (ppu 16): Yan, 2026-11-03.
RENDERS = {
    "muzzle_pistol": ("projectiles/muzzle-pistol", GUN, 26), "muzzle_smg": ("projectiles/muzzle-smg", GUN, 26),
    "muzzle_shotgun": ("projectiles/muzzle-shotgun", GUN, 26), "hit_spark": ("impacts/hit-spark", GUN, 20),
    "ricochet": ("impacts/ricochet", dict(GUN, stone="#6b6a60"), 0), "bullet_impact": ("impacts/bullet-impact", dict(GUN, stone="#6b6a60"), 24),
    # electrified wraps pixelfx's own 24x38 character (its outline shows), so the Livewire field is the drifting ball lightning
    "anomaly_livewire": ("lightning/ball-lightning", {}, 0), "zap_hit": ("lightning/zap-hit", {}, 0),
    # Boss 2: the brain's void pull (our small black hole) and its escape (the library's full-size one)
    "void_pull": ("zone/void-pull", {}, 0), "void_escape": ("shadow/black-hole", {}, 0),
}
MAX_W = 16384

def export(name, fid, opts, lift):
    subprocess.run(["node", "tools/render.mjs", fid, "--sheet", "--opts", json.dumps(opts)], cwd=PFX, check=True, capture_output=True)
    base = fid.split("/")[1]
    meta = json.load(open(os.path.join(PFX, "records", fid, base + ".json")))
    px = meta["meta"]["pixelfx"]; fw, fh = px["frameSize"]; ox, oy = px["pivot"]; oy -= lift
    sheet = Image.open(os.path.join(PFX, "records", fid, base + ".png")).convert("RGBA")
    frames = [sheet.crop((f["frame"]["x"], f["frame"]["y"], f["frame"]["x"] + fw, f["frame"]["y"] + fh)) for f in meta["frames"]]
    step = 2
    while True:
        pick = frames[::step]
        boxes = [f.getbbox() for f in pick if f.getbbox()]
        x0 = min(b[0] for b in boxes); y0 = min(b[1] for b in boxes); x1 = max(b[2] for b in boxes); y1 = max(b[3] for b in boxes)
        half = max(ox - x0, x1 - ox, oy - y0, y1 - oy) + 1
        S = 2 * half
        if S * len(pick) <= MAX_W: break
        step += 1
    strip = Image.new("RGBA", (S * len(pick), S))
    for i, f in enumerate(pick): strip.alpha_composite(f, (i * S + half - ox, half - oy))
    dst = os.path.join(OUT, f"px_{name}.png"); strip.save(dst)
    m = open(TEMPLATE).read()
    m = re.sub(r"guid: \w+", "guid: " + uuid.uuid4().hex, m, count=1)
    m = re.sub(r"maxTextureSize: \d+", "maxTextureSize: 16384", m)
    m = re.sub(r"textureCompression: \d+", "textureCompression: 0", m)
    if not os.path.exists(dst + ".meta"): open(dst + ".meta", "w").write(m)
    print(f"px_{name}: {len(pick)} frames of {S}x{S} at {30 // step} fps")
    return len(pick), 30 // step

if __name__ == "__main__":
    for n, (fid, o, lift) in RENDERS.items(): export(n, fid, o, lift)
