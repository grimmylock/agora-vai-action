"""2026-11-21: a creature boss's Pixel Lab clips at game scale in its own lair (BossArt.cs, BOSS_CONCEPTS_V3.md), next to a player, for review.
Reads the clip table from Assets/Scripts/GameCore/BossArt.cs (frames, ms), the strips from Resources/Sprites/Mutants/anim/boss_<key>_<clip>.bytes, the scale from BossArt.Scales,
and the map render + landmark from Tools/render/_render (render.sh <map> first). The world render is 16 px a tile; it is doubled to 32 px a tile, the strips' own scale.
  python3 Tools/render/boss_clip_preview.py <boss> <map> "<landmark>" [clip,clip,...] -> maps/renders/bosses_v3/<boss>_in_lair.gif (+ .png, one frame)"""
import os, re, sys
from PIL import Image
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
boss, mapname, landmark = sys.argv[1], sys.argv[2], sys.argv[3]
src = open(os.path.join(R, "Assets/Scripts/GameCore/BossArt.cs")).read()
clips = {m.group(1): (int(m.group(2)), [int(x) for x in m.group(3).split(",")]) for m in re.finditer(r'\["(\w+)"\] = new VergerBoss\.Clip \{ Name = "boss_%s_\w+", Frames = (\d+), Ms = new float\[\] \{ ([^}]*) \}' % boss, src)}
sm = re.search(r'\["%s"\] = ([0-9.]+)f' % boss, src[src.index("Scales"):]); S = float(sm.group(1)) if sm else 1.0
order = sys.argv[4].split(",") if len(sys.argv) > 4 else [c for c in ["idle", "idle", "walk", "drive"] if c in clips] + [c for c in clips if c not in ("idle", "walk", "drive", "hurt", "death")] + [c for c in ("hurt", "death") if c in clips]
lab = [l.split("\t") for l in open(os.path.join(R, "Tools/render/_render/labels_%s.txt" % mapname)) if l.strip()]
lx, ly = next((int(a[1]), int(a[2])) for a in lab if len(a) > 3 and a[3].strip() == landmark)
world = Image.open(os.path.join(R, "Tools/render/_render/%s.png" % mapname)).convert("RGBA")
x0, y0 = max(0, lx - 12), max(0, ly - 10); x1, y1 = min(world.width // 16, x0 + 24), min(world.height // 16, y0 + 18)
bg = world.crop((x0 * 16, y0 * 16, x1 * 16, y1 * 16)); bg = bg.resize((bg.width * 2, bg.height * 2), Image.NEAREST)
pl = Image.open(os.path.join(R, "Assets/Resources/Sprites/armor_atlas_x2.png")).convert("RGBA").crop((32, 0, 64, 32))
fx, fy = (lx - x0) * 32 + 16, (ly - y0) * 32 + 32
frames, durs = [], []
for name in order:
    if name not in clips: continue
    n, ms = clips[name]; strip = Image.open(os.path.join(R, "Assets/Resources/Sprites/Mutants/anim/boss_%s_%s.bytes" % (boss, name))).convert("RGBA"); w = strip.width // n
    for i in range(n):
        fr = strip.crop((i * w, 0, (i + 1) * w, strip.height))
        if S != 1.0: fr = fr.resize((max(1, int(w * S)), max(1, int(strip.height * S))), Image.NEAREST)
        im = bg.copy(); im.alpha_composite(fr, (fx - fr.width // 2, fy - fr.height + int(6 * S)))
        im.alpha_composite(pl, (fx - int(fr.width * 0.5) - 72, fy - 28))
        frames.append(im.convert("RGB")); durs.append(ms[i])
out = os.path.join(R, "maps/renders/bosses_v3"); os.makedirs(out, exist_ok=True)
frames[0].save(os.path.join(out, "%s_in_lair.gif" % boss), save_all=True, append_images=frames[1:], duration=durs, loop=0)
frames[min(len(frames) - 1, len(frames) // 2)].save(os.path.join(out, "%s_in_lair.png" % boss)); print(boss, len(frames), "frames ->", out)
