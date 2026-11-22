import json, os, subprocess
H = os.path.dirname(os.path.abspath(__file__)); A = os.path.join(H, "..", "..", "pixellab_anim.py")
SPEC = {"std": ((64, 80), "the pale hunched screaming patient mutant in a tan gown"), "heavy": ((72, 80), "the heavy bloated screaming mutant in a dark ragged gown"), "crawl": ((80, 64), "the pale crouching crawling screaming mutant in a tan gown")}
CL = [("walk", 8, 1, "side view facing left, {d} walks forward in a lurching shambling walk cycle, legs and arms clearly moving in turn, stays in place"),
      ("lunge", 12, 0, "side view facing left, {d} crouches then lunges forward hard with its arms stretched out and mouth wide, then recovers"),
      ("melee", 12, 0, "side view facing left, {d} swings one arm in a fast clawing swipe forward, then the other, then returns to stance")]
for n, (cv, d) in SPEC.items():
    os.makedirs(os.path.join(H, n), exist_ok=True)
    import shutil; shutil.copy(os.path.join(H, n + "_a.png"), os.path.join(H, n, "stand.png"))
    for i, (cn, nf, loop, desc) in enumerate(CL):
        out = os.path.join(H, n, cn)
        if os.path.exists(os.path.join(out, "f_00.png")): continue
        r = subprocess.run(["timeout", "400", "python3", A, os.path.join(H, n, "stand.png"), str(cv[0]), str(cv[1]), out, str(nf), str(400 + 13 * i), str(loop), desc.format(d=d)], capture_output=True, text=True)
        print(n, cn, "ok" if os.path.exists(os.path.join(out, "f_00.png")) else "FAIL", flush=True)
print("RM DONE", flush=True)
