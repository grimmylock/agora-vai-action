# Re-rolls weak clips (rerolls.txt: boss<TAB>clip<TAB>motion), one Pixel Lab job at a time: the old run is kept as <clip>_v1 and the new one takes the clip's name. Logged to gens.log.
import json, os, shutil, subprocess
H = os.path.dirname(os.path.abspath(__file__)); A = os.path.join(H, "..", "..", "pixellab_anim.py")
st = json.load(open(os.path.join(H, "stands.json"))); plan = json.load(open(os.path.join(H, "plan.json")))
for line in open(os.path.join(H, "rerolls.txt")):
    if not line.strip() or line.startswith("#"): continue
    b, clip, desc = line.rstrip("\n").split("\t")
    out = os.path.join(H, b, clip); old = out + "_v1"
    if os.path.exists(old): continue                                         # already re-rolled
    n = next(p[1] for p in plan[b] if p[0] == clip); loop = next(p[2] for p in plan[b] if p[0] == clip)
    shutil.move(out, old); cw, ch = st[b]["canvas"]; seed = 777 + len(clip) * 13 + len(b)
    r = subprocess.run(["timeout", "400", "python3", A, os.path.join(H, b, "stand.png"), str(cw), str(ch), out, str(n), str(seed), str(loop), desc], capture_output=True, text=True)
    ok = os.path.exists(os.path.join(out, "f_00.png"))
    if not ok: shutil.rmtree(out, ignore_errors=True); shutil.move(old, out)
    open(os.path.join(H, "gens.log"), "a").write(f"{b}\t{clip} (re-roll)\tseed {seed}\t{'ok' if ok else 'FAIL'}\t{(r.stdout + r.stderr)[-160:].strip()!r}\n")
    print(b, clip, "re-roll", "ok" if ok else "FAIL", flush=True)
print("REROLLS DONE", flush=True)
