# Runs plan.json through Pixel Lab animate-pixminimax (ArtSource/pixellab_anim.py), one job at a time, skipping clips already made; logs generations to gens.log.
import json, os, subprocess, sys
H = os.path.dirname(os.path.abspath(__file__)); A = os.path.join(H, "..", "..", "pixellab_anim.py")
plan = json.load(open(os.path.join(H, "plan.json"))); st = json.load(open(os.path.join(H, "stands.json")))
only = sys.argv[1:] or list(plan)
for b in only:
    cw, ch = st[b]["canvas"]
    for i, (name, n, loop, desc) in enumerate(plan[b]):
        out = os.path.join(H, b, name)
        if os.path.exists(os.path.join(out, "f_00.png")): continue
        seed = 300 + 17 * i + len(b)
        r = subprocess.run(["timeout", "400", "python3", A, os.path.join(H, b, "stand.png"), str(cw), str(ch), out, str(n), str(seed), str(loop), desc], capture_output=True, text=True)
        ok = os.path.exists(os.path.join(out, "f_00.png"))
        open(os.path.join(H, "gens.log"), "a").write(f"{b}\t{name}\tseed {seed}\t{'ok' if ok else 'FAIL'}\t{(r.stdout + r.stderr)[-200:].strip()!r}\n")
        print(b, name, "ok" if ok else "FAIL", flush=True)
print("PLAN DONE", flush=True)
