# The three room mutants (Yan's sheet, Pixel Lab clips in mutants_anim/room_mutants/) -> game strips + C# tables (2026-11-22).  python3 export_room_mutants.py
# Ward Wretch (std: walk, melee, pounce), Ward Hulk (heavy: walk, melee; no lunge, Yan: "remove lunge from brute"), Ward Crawler (crawl: walk, melee, pounce).
# The game draws mutants facing RIGHT (TacticalGridView mirrors them): every frame is flipped. Stand and walk: cropped so the feet are on the bottom row (pivot 16 px above it). Attacks keep their canvas, PivotY=(rows under the feet + 16)/H.
# A lunge's baked travel is cancelled (each frame is shifted back so the body stays put) and becomes the Travel curve (MutantAttack slides the body by it).
import os, glob, json, numpy as np
from PIL import Image
H = os.path.dirname(os.path.abspath(__file__)); R = os.path.join(H, "mutants_anim", "room_mutants"); OUT = os.path.join(H, "..", "Assets", "Resources", "Sprites", "Mutants"); AN = os.path.join(OUT, "anim")
SPEC = [("std", "Ward Wretch", ["walk", "melee", "pounce"]), ("heavy", "Ward Hulk", ["walk", "melee"]), ("crawl", "Ward Crawler", ["walk", "melee", "pounce"])]
SRC = {"pounce": "lunge"}
def load(path): return Image.open(path).convert("RGBA").transpose(Image.FLIP_LEFT_RIGHT)
def hard(im): a = np.array(im); a[..., 3] = np.where(a[..., 3] >= 128, 255, 0); a[a[..., 3] == 0] = 0; return Image.fromarray(a, "RGBA")
def bottom(a): ys = np.where(a[..., 3] > 0)[0]; return ys.max() if len(ys) else -1
def strip(frames, path):
    w, h = frames[0].size; s = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames): s.alpha_composite(f, (i * w, 0))
    s.save(path, "PNG")
def timing(n, hit):
    ms = []
    for i in range(n):
        if i < hit: ms.append(int(110 + 90 * i / max(1, hit - 1)))
        elif i == hit: ms.append(80)
        else: ms.append(int(110 + 50 * (i - hit) / max(1, n - hit)))
    return ms
def main():
    os.makedirs(AN, exist_ok=True); lines = []
    for key, name, clips in SPEC:
        slug = name.lower().replace(" ", "_")
        stand = hard(load(os.path.join(R, key, "stand.png"))); sa = np.array(stand); bb = bottom(sa); stand.crop((0, 0, stand.width, bb + 1)).save(os.path.join(OUT, f"mutant_hd_{slug}.png"))
        d = {}
        for clip in clips:
            src = SRC.get(clip, clip); fs = sorted(glob.glob(os.path.join(R, key, src, "f_*.png"))); frs = [hard(load(f)) for f in fs]
            if clip == "walk":
                frs = frs[:8]; low = max(bottom(np.array(f)) for f in frs); frs = [f.crop((0, 0, f.width, low + 1)) for f in frs]; strip(frs, os.path.join(AN, f"{slug}_walk.bytes")); continue
            n = len(frs); W, Hh = frs[0].size; arrs = [np.array(f) for f in frs]; low = max(bottom(a) for a in arrs); margin = Hh - 1 - low
            def xr(a): xs = np.where(a[..., 3] > 0)[1]; return (xs.min(), xs.max()) if len(xs) else (0, 0)
            if clip == "pounce":     # the clip leaps in place (the game's Slide carries the body): the Travel curve runs from the frame the feet leave the ground to the frame they land again; the bite lands at the landing
                ymax = [int(np.where(a[..., 3] > 0)[0].max()) if (a[..., 3] > 0).any() else low for a in arrs]; air = [i for i, y in enumerate(ymax) if y < low - 5]
                l0, l1 = (air[0], air[-1] + 1) if air else (n // 3, n // 2 + 2); l1 = min(l1, n - 1)
                tr = [0.0 if i <= l0 else 1.0 if i >= l1 else (i - l0) / float(l1 - l0) for i in range(n)]; contact = l1
            else:
                ext = [xr(a)[1] for a in arrs]; contact = int(np.argmax(ext[2:])) + 2; tr = [0.0] * n
            ms = timing(n, contact); strip(frs, os.path.join(AN, f"{slug}_{clip}.bytes"))
            kind = "bite" if False else clip
            d[kind] = f'                ["{kind}"] = new AttackAnim {{ Frames = {n}, W = {W}, H = {Hh}, Contact = {contact}, ContactMs = {sum(ms[:contact])}, TotalMs = {sum(ms)}, PivotY = {(margin + 16) / Hh:.5f}f, Ms = new[] {{ {", ".join(map(str, ms))} }}, Travel = new[] {{ {", ".join("%.2ff" % t for t in tr)} }} }},'
            print(name, clip, "frames", n, "contact", contact, "margin", margin)
        lines.append(f'            ["{name}"] = new Dictionary<string, AttackAnim>\n            {{\n' + "\n".join(d.values()) + "\n            },")
    open(os.path.join(H, "mutants_anim", "room_mutants", "attack_table.cs.txt"), "w").write("\n".join(lines)); print("\n".join(lines)[:600])
main()
