# The werewolf's combos, stitched from the approved Pixel Lab clips (2026-11-21; Yan: "a few combos for the werewolf boss like the Gaunt has"; no generation: the trial budget is spent). Same method as the Gaunt's combo3/combo4 stitches:
# frames of one clip up to its recovery, then the next clip from a wind-up frame, the join chosen by silhouette overlap (alpha IoU) so the picture does not jump. Smear frames (the swipe blobs) are never join points.
#   python3 ArtSource/make_beast_combos.py   -> prints the joins, writes Assets/Resources/Sprites/Mutants/anim/verger_beast_<combo>.bytes and ArtSource/mutants_anim/verger_beast/combos.json (frames, hit frames; ms are filled from the clips' own ms)
import os, sys, io, json, glob, re
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); SRC = os.path.join(HERE, "mutants_anim", "verger_beast", "pixellab")
OUT = os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "anim")
CS = open(os.path.join(HERE, "..", "Assets", "Scripts", "GameCore", "VergerBoss.cs")).read()
CLIPS = {m.group(1): (int(m.group(2)), [float(x) for x in m.group(3).split(",")], int(m.group(4))) for m in re.finditer(r'\["(\w+)"\]\s*=\s*new Clip \{[^}]*?Frames = (\d+), Ms = new float\[\] \{([^}]*)\}, Hit = (-?\d+)', CS)}
SMEAR = {"rake": {5}, "sweep": {5, 6}, "slam": {4}}          # frames that are a swipe blob / shockwave: never a join point
def hard(im):
    a = np.array(im.convert("RGBA")); a[..., 3] = np.where(a[..., 3] >= 128, 255, 0); a[a[..., 3] == 0] = 0; return Image.fromarray(a, "RGBA")
REMAP = {"rake": [0, 1, 2, 3, 4, 4, 7, 7, 8, 9, 10, 11, 12], "sweep": [0, 1, 2, 3, 4, 4, 7, 7, 8, 9, 10, 11, 12]}          # the baked low smears are not used (export_verger_beast_clips.py): the game draws the slash
FR = {n: [hard(Image.open(f)) for f in sorted(glob.glob(os.path.join(SRC, n, "f_*.png")))[:CLIPS[n][0]]] for n in ("rake", "sweep", "slam")}
FR = {n: ([fr[i] for i in REMAP[n]] if n in REMAP else fr) for n, fr in FR.items()}
MASK = {n: [np.array(f)[..., 3] > 0 for f in FR[n]] for n in FR}
def iou(a, b): return (a & b).sum() / max(1, (a | b).sum())
# Chosen joins (the candidates were listed by silhouette overlap and by the gap between the two hits): (frame of A where it hands over, frame of B it continues from).
# Rake -> Sweep is a fast follow-up (0.37 s, overlap 0.62); the Smash (slam) as a finisher keeps its whole raise-the-arms wind-up (from frame 1) as its tell: its raised-arm pose never matches the others closely (overlap 0.55-0.57).
JOINS = {("rake", "sweep"): (8, 4), ("rake", "slam"): (9, 1), ("sweep", "slam"): (10, 1)}
def join(A, B):
    i, j = JOINS[(A, B)]; return (iou(MASK[A][i], MASK[B][j]), i, j)
COMBOS = {"rakesweep": ["rake", "sweep"], "rakeslam": ["rake", "slam"], "rampage": ["rake", "sweep", "slam"]}
def build():
    out = {}
    for name, seq in COMBOS.items():
        frames, ms, hits, tags = [], [], [], []
        cur = 0
        for k, clip in enumerate(seq):
            n, cms, hit = CLIPS[clip]
            if k == 0: start = 0
            else:
                _, i_prev, j = join(seq[k - 1], clip); start = j
            end = n - 1 if k == len(seq) - 1 else join(clip, seq[k + 1])[1]
            for f in range(start, end + 1):
                if f == hit: hits.append(len(frames))
                frames.append(FR[clip][f]); ms.append(cms[f]); tags.append(f"{clip}{f}")
        out[name] = dict(frames=frames, ms=ms, hits=hits, tags=tags)
    return out
def main():
    os.makedirs(OUT, exist_ok=True); info = {}
    for A, B in (("rake", "sweep"), ("rake", "slam"), ("sweep", "slam")): s, i, j = join(A, B); print(f"join {A}[..{i}] -> {B}[{j}..]  silhouette overlap {s:.2f}")
    for name, d in build().items():
        w, h = d["frames"][0].size; strip = Image.new("RGBA", (w * len(d["frames"]), h), (0, 0, 0, 0))
        for i, f in enumerate(d["frames"]): strip.alpha_composite(f, (i * w, 0))
        buf = io.BytesIO(); strip.save(buf, "PNG"); open(os.path.join(OUT, "verger_beast_%s.bytes" % name), "wb").write(buf.getvalue())
        gaps = [round((sum(d["ms"][d["hits"][k]:d["hits"][k + 1]])) / 1000, 2) for k in range(len(d["hits"]) - 1)]
        info[name] = dict(frames=len(d["frames"]), ms=d["ms"], hits=d["hits"], gaps=gaps, tags=d["tags"], pre_hit=sum(d["ms"][:d["hits"][0]]))
        print(name, len(d["frames"]), "frames, hits at", d["hits"], "gaps (s)", gaps, "total ms", sum(d["ms"]), "|", " ".join(d["tags"]))
    json.dump(info, open(os.path.join(SRC, "..", "combos.json"), "w"), indent=1)
if __name__ == "__main__": main()
