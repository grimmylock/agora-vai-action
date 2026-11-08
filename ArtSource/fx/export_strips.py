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
SMOKE = ["#121314", "#1f2122", "#2e3132", "#43474a", "#5c6164", "#7c8184", "#a2a6a8"]
DUST = ["#1c1813", "#3a3229", "#5a4f40", "#7d7052", "#a0936f", "#cfc4a4", "#efe8d4"]
GOLD = ["#2a1a08", "#5a3a12", "#8a6020", "#b88c3a", "#dcb862", "#efdca0", "#fbf4dc"]
MURK = ["#0b100f", "#141c1a", "#1d2a26", "#2a3b35", "#3e554b", "#5d7a6c", "#8aa596", "#c4d6ca"]
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
    # RT fx 4 (Yan: "if they fit add them, we can change later"): the library's ready-made ones that fit, in the Zone's ramps, half-pixels
    "small_explosion": ("explosions/small-explosion", {"palette": FIRE, "stone": "#5d5c56"}, 0),        # the frag, the barrel, a cooked-off grenade
    "smoke_bomb": ("explosions/smoke-bomb", {"palette": SMOKE}, 0),                                     # the smoke grenade, the Visor's vanishing
    "parry": ("impacts/parry", {"palette": GOLD}, 30),                                                  # the perfect parry
    "blunt_hit": ("impacts/blunt-hit", {"palette": DUST}, 22),                                          # the bat, the hammer, the sledge
    "acid_splash": ("nature/acid-splash", {}, 0),                                                       # the acid anomaly going off
    "splash": ("water/splash", {"palette": MURK, "stone": False}, 0),                                   # the eel bursting out (no trigger yet)
    "ripples": ("water/ripples", {"palette": MURK}, 0),                                                 # wading
    "whirlpool": ("water/whirlpool", {"palette": MURK}, 0),                                             # a pump draining a level
    "thunderclap": ("lightning/thunderclap", {}, 0),                                                    # the electrified water (no trigger yet)
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
    import sys                                      # python3 export_strips.py [name ...]: only those (the default: all)
    for n, (fid, o, lift) in RENDERS.items():
        if len(sys.argv) < 2 or n in sys.argv[1:]: export(n, fid, o, lift)
