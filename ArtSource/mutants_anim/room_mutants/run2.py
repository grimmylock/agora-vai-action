# re-roll of the lunge (all three, on a canvas twice as wide so the body can travel) and the melee (thin and heavy), 2026-11-22 (Yan: the lunge reads as a push, the melee as punching the air)
import os, subprocess, shutil
H = os.path.dirname(os.path.abspath(__file__)); A = os.path.join(H, "..", "..", "pixellab_anim.py")
D = {"std": "the pale hunched screaming patient mutant in a tan gown", "heavy": "the heavy bloated screaming mutant in a torn tan gown", "crawl": "the pale crouching crawling screaming mutant in a tan gown"}
CV = {"std": (112, 80), "heavy": (120, 80), "crawl": (128, 64)}
CV_M = {"std": (96, 80), "heavy": (104, 80)}
JOBS = [("std", "lunge", 12, "side view facing left, {d} crouches low, then springs off the ground and flies forward through the air to the left, body stretched out horizontally, feet off the ground, arms reaching far ahead with open hands, mouth wide, then lands on the ground far to the left of where it started"),
        ("heavy", "lunge", 12, "side view facing left, {d} lowers its shoulders, then charges and leaps forward to the left with its whole body, arms stretched out ahead of it, feet leaving the ground, then crashes down onto the ground far to the left of where it started"),
        ("crawl", "lunge", 12, "side view facing left, {d} coils low, then pounces forward through the air to the left with its arms and body stretched out, mouth wide, then lands on all fours far to the left of where it started"),
        ("std", "melee", 12, "side view facing left, {d} faces a target in front of it on the left, raises one arm high behind its head, then slashes it down and forward hard to hit the target in front of it, its body leaning into the blow, then pulls the arm back"),
        ("heavy", "melee", 12, "side view facing left, {d} faces a target in front of it on the left, lifts both fists high overhead, then smashes them down and forward in front of it onto the target, its whole body leaning into the blow, then straightens up")]
for i, (n, c, nf, desc) in enumerate(JOBS):
    out = os.path.join(H, n, c)
    if os.path.exists(os.path.join(out, "f_00.png")): continue
    cv = CV[n] if c == "lunge" else CV_M[n]
    r = subprocess.run(["timeout", "400", "python3", A, os.path.join(H, n, "stand.png"), str(cv[0]), str(cv[1]), out, str(nf), str(520 + 7 * i), "0", desc.format(d=D[n])], capture_output=True, text=True)
    print(n, c, "ok" if os.path.exists(os.path.join(out, "f_00.png")) else "FAIL", flush=True)
print("RM2 DONE", flush=True)
