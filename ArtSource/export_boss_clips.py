# The new creature bosses' Pixel Lab clips -> game strips + the C# clip table (2026-11-21; Yan: "use [the 2k Pixel Lab credits] to create the new bosses after you finish the concepts").
# Source: ArtSource/mutants_anim/bosses_v3/<boss>/<clip>/f_*.png (animate-pixminimax runs from bosses_v3/<boss>/stand.png, the pick from BOSS_CONCEPTS_V3.md; plan.json has the prompts, gens.log every call).
# Writes Assets/Resources/Sprites/Mutants/anim/boss_<boss>_<clip>.bytes (a PNG strip, hard alpha) and rewrites the table between "// clips begin" / "// clips end" in Assets/Scripts/GameCore/BossArt.cs.
# The stand sits at the bottom centre of each canvas with its lowest pixel 6 px above the bottom (pixellab_anim.py), so the feet pivot is 6/H.
# Loops (idle, walk) drop the last frame (it repeats the first). The hit frame of an attack is found, not guessed: the frame whose silhouette reaches furthest toward where the boss faces (or, for a boss facing the camera, the frame that differs most from the first).
import os, glob, json, re
import numpy as np
from PIL import Image
H = os.path.dirname(os.path.abspath(__file__)); SRC = os.path.join(H, "mutants_anim", "bosses_v3")
OUT = os.path.join(H, "..", "Assets", "Resources", "Sprites", "Mutants", "anim"); CS = os.path.join(H, "..", "Assets", "Scripts", "GameCore", "BossArt.cs")
FACING = {"mother_hollow": "left", "singer": "front", "ferryman": "front", "specimen": "front", "excavator": "left", "orderly": "front", "specimen_v2": "front", "singer_wall": "front"}
LOOPS = {"idle", "walk", "drive"}
HIT = {("mother_hollow", "bite"): 5, ("mother_hollow", "spit"): 5, ("mother_hollow", "pounce"): 8, ("singer", "note"): 4, ("singer", "scream"): 3, ("singer", "grasp"): 6, ("ferryman", "thrust"): 4, ("ferryman", "hook"): 9, ("ferryman", "sweep"): 7, ("ferryman", "flare"): 3, ("specimen", "mind"): 4, ("specimen_v2", "laser"): 4, ("specimen_v2", "orbs"): 7, ("specimen_v2", "mind"): 4, ("excavator", "sweep"): 5, ("excavator", "slam"): 5, ("excavator", "scoop"): 13, ("orderly", "grab"): 5, ("orderly", "drip"): 6, ("specimen", "scream"): 6}      # read off the strips by eye where the automatic pick missed (a slash streak or the glob widens the silhouette later than the blow)
MS = {"idle": 150, "walk": 110, "drive": 110}          # per-frame defaults; attacks get a wind-up / snap / recover shape below
def hard(im):
    a = np.array(im.convert("RGBA")); a[..., 3] = np.where(a[..., 3] >= 128, 255, 0); a[a[..., 3] == 0] = 0; return Image.fromarray(a, "RGBA")
def hit_frame(ims, facing):
    al = [np.array(i)[..., 3] > 0 for i in ims]
    if facing in ("left", "right"):
        ext = [(np.where(a.any(0))[0].min() if facing == "left" else -np.where(a.any(0))[0].max()) if a.any() else 1e9 for a in al]
        return int(np.argmin(ext[1:])) + 1
    diff = [np.logical_xor(al[0], a).sum() for a in al]
    return int(np.argmax(diff[1:])) + 1
def timing(n, hit, loop, name):
    if loop: return [MS.get(name, 120)] * n
    ms = []
    for i in range(n):
        if i < hit: ms.append(int(110 + 90 * i / max(1, hit - 1)))          # the wind-up slows into the hit (the telegraph)
        elif i == hit: ms.append(80)                                          # the snap
        else: ms.append(int(120 + 60 * (i - hit) / max(1, n - hit)))         # the recovery
    return ms
REMAP = {("specimen_v2", "laser"): [0, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 12]}   # the baked beam frames (3-11) are replaced by the eye charging and flickering: the game draws the beam
KEY_OUT = {("specimen_v2", "laser")}       # 2026-11-21: clips whose baked effect is drawn by the game instead (the Specimen's beam: CombatFxView.SpecimenBeams): the hot magenta beam pixels are keyed out
def key_out(im):
    a = np.array(im); hot = (a[..., 0] > 170) & (a[..., 2] > 170) & (a[..., 1] < 120); a[hot] = 0; return Image.fromarray(a, "RGBA")
RECOLOR = {("specimen_v2", "orbs"), ("specimen_v2", "mind")}      # the generator's hot magenta -> the vat brain's own violet ramp (by brightness), so the effect matches the vat's orbs and eyes
VIOLET = [(40, 18, 64), (74, 42, 122), (118, 72, 190), (156, 112, 226), (206, 176, 250)]
def recolor(im):
    a = np.array(im).astype(int); hot = (a[..., 0] > 150) & (a[..., 2] > 150) & (a[..., 1] < 140) & (a[..., 3] > 0)
    lum = (a[..., 0] + a[..., 1] + a[..., 2]) / 3.0; idx = np.clip(((lum - 60) / 170 * (len(VIOLET) - 1)).round().astype(int), 0, len(VIOLET) - 1)
    for k, c in enumerate(VIOLET): a[hot & (idx == k), :3] = c
    return Image.fromarray(a.astype(np.uint8), "RGBA")
def main():
    os.makedirs(OUT, exist_ok=True); rows = {}
    for bd in sorted(glob.glob(os.path.join(SRC, "*", ""))):
        boss = os.path.basename(os.path.dirname(bd))
        if boss not in FACING: continue
        for cd in sorted(glob.glob(os.path.join(bd, "*", ""))):
            clip = os.path.basename(os.path.dirname(cd)); fs = sorted(glob.glob(os.path.join(cd, "f_*.png")))
            if clip.endswith("_v1"): continue                                     # a re-rolled clip's old run (run_rerolls.py keeps it)
            if not fs: continue
            ims = [hard(Image.open(f)) for f in fs]; loop = clip in LOOPS
            if (boss, clip) in REMAP: ims = [ims[i] for i in REMAP[(boss, clip)]]
            if (boss, clip) in KEY_OUT: ims = [key_out(i) for i in ims]
            if (boss, clip) in RECOLOR: ims = [recolor(i) for i in ims]
            if loop and len(ims) > 2: ims = ims[:-1]
            n = len(ims); w, h = ims[0].size
            hit = -1 if loop or clip in ("hurt", "death") else HIT.get((boss, clip), hit_frame(ims, FACING[boss]))
            strip = Image.new("RGBA", (w * n, h), (0, 0, 0, 0))
            for i, im in enumerate(ims): strip.alpha_composite(im, (i * w, 0))
            strip.save(os.path.join(OUT, "boss_%s_%s.bytes" % (boss, clip)), "PNG")
            rows.setdefault(boss, []).append((clip, n, timing(n, hit, loop, clip), hit, loop, w, h))
            print(boss, clip, n, "frames", (w, h), "hit", hit)
    lines = []
    for boss, cl in rows.items():
        w, h = cl[0][5], cl[0][6]
        lines.append('            ["%s"] = new Art { Key = "%s", W = %d, H = %d, Facing = "%s", Clips = new Dictionary<string, VergerBoss.Clip> {' % (boss, boss, w, h, FACING[boss]))
        for clip, n, ms, hit, loop, _, _ in cl:
            lines.append('                ["%s"] = new VergerBoss.Clip { Name = "boss_%s_%s", Frames = %d, Ms = new float[] { %s }, Hit = %d%s },' % (clip, boss, clip, n, ", ".join(str(m) for m in ms), hit, ", Loop = true" if loop else ""))
        lines.append("            } },")
    src = open(CS).read()
    src = re.sub(r"(// clips begin\n).*?(\s*// clips end)", lambda m: m.group(1) + "\n".join(lines) + m.group(2), src, flags=re.S)
    open(CS, "w").write(src); print("table ->", CS)
if __name__ == "__main__": main()
