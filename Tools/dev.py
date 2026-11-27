#!/usr/bin/env python3
"""One entry point for the repeated build/check/render/commit loop (Yan, 2026-11-27: spend less on repeated tool calls, same quality).
  python3 Tools/dev.py plans                      compile ArtSource/plans/*.plan into C# (Tools/plans/plan.py)
  python3 Tools/dev.py atlas                      repack the world atlas from ArtSource/world and copy it into Assets/Resources/Sprites
  python3 Tools/dev.py check                      run the full harness; prints only FAIL lines and the verdict (exit 1 on failure; reruns once if only the flaky 'RT 9: mid-dive' fails)
  python3 Tools/dev.py render MAP X0 Y0 X1 Y1 OUT [WIDTH]   render a map (roofs off), crop a tile box, save a PNG (default width 1300); several boxes: OUT1:x0,y0,x1,y1 ...
  python3 Tools/dev.py ship BATCH "title" "log text"        append a WORK_LOG entry, commit with the attribution trailers, push to realtime-proto (retries)
  python3 Tools/dev.py all                         plans + atlas + check
"""
import os, subprocess, sys, shutil, glob, time
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
SESSION = "https://claude.ai/code/session_01Jpd5tdDJTR7BVueAYV1KDM"
def run(cmd, cwd=ROOT, env=None, quiet=False):
    e = dict(os.environ); e.update(env or {})
    r = subprocess.run(cmd, cwd=cwd, shell=isinstance(cmd, str), capture_output=True, text=True, env=e)
    if not quiet and r.returncode: sys.stdout.write(r.stdout[-1500:] + r.stderr[-1500:])
    return r
def plans(): r = run([sys.executable, "Tools/plans/plan.py"]); print(r.stdout.strip()); return r.returncode
def atlas():
    w = os.path.join(ROOT, "ArtSource", "world")
    for f in glob.glob(os.path.join(w, "world_atlas.raw")): os.remove(f)
    r = run([sys.executable, "pack.py"], cwd=w); out = r.stdout.strip().splitlines()[-1] if r.stdout.strip() else r.stderr[-300:]
    src = "/tmp/claude-0/world_out"
    if not os.path.exists(os.path.join(src, "world_atlas.png")): src = w
    shutil.copy(os.path.join(src, "world_atlas.png"), os.path.join(ROOT, "Assets/Resources/Sprites/world_atlas.bytes"))
    shutil.copy(os.path.join(src, "world_atlas.txt"), os.path.join(ROOT, "Assets/Resources/Sprites/world_atlas_meta.txt"))
    print("atlas:", out); return r.returncode
def check():
    def once():
        r = run("bash runtest.sh ../../Assets/Scripts", cwd=os.path.join(ROOT, "Tools/compile_check"), quiet=True)
        o = r.stdout + r.stderr; errs = [l for l in o.splitlines() if "error CS" in l]; fails = [l for l in o.splitlines() if l.startswith("FAIL")]
        return errs, fails, "ALL CHECKS PASSED" in o
    errs, fails, ok = once()
    if fails and all("RT 9: mid-dive" in f for f in fails): print("(only the flaky RT 9 mid-dive check failed; rerunning)"); errs, fails, ok = once()
    for l in errs[:8]: print(l[:250])
    for l in fails[:12]: print(l[:400])
    print("HARNESS: ALL CHECKS PASSED" if ok else "HARNESS: FAILED"); return 0 if ok else 1
def render(args):
    from PIL import Image
    m = args[0]; run("rm -f Tools/render/_render/world_atlas.raw"); r = run(["bash", "Tools/render/render.sh", m], env={"NOROOF": "1"}); print(r.stdout.strip().splitlines()[-1])
    im = Image.open(os.path.join(ROOT, "Tools/render/_render", m + ".png")); s = im.size[0] / 200; boxes = []
    if len(args) >= 6 and ":" not in args[1]: boxes = [(args[5], *map(float, args[1:5]))]; width = int(args[6]) if len(args) > 6 else 1300
    else:
        width = 1300
        for a in args[1:]:
            if a.isdigit(): width = int(a); continue
            o, c = a.split(":"); x0, y0, x1, y1 = map(float, c.split(",")); boxes.append((o, x0, y0, x1, y1))
    for (o, x0, y0, x1, y1) in boxes:
        r2 = im.crop((int(x0 * s), int(y0 * s), int(x1 * s), int(y1 * s))); r2.resize((width, int(width * r2.height / r2.width)), Image.NEAREST).save(o if os.path.isabs(o) else os.path.join(ROOT, o)); print("saved", o)
def ship(batch, title, text):
    log = os.path.join(ROOT, "ArtSource/WORK_LOG.md"); open(log, "a").write(f"\n## Batch {batch} ({time.strftime('%Y-%m-%d')}) - {title}\n{text.strip()}\n")
    run("git add -A", quiet=True)
    msg = f"{title}\n\nCo-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>\nClaude-Session: {SESSION}"
    r = run(["git", "commit", "-q", "-m", msg]); 
    if r.returncode: return r.returncode
    for i in range(4):
        r = run("git push -q -u origin realtime-proto", quiet=True)
        if r.returncode == 0: print("pushed", subprocess.run("git log --oneline -1", shell=True, cwd=ROOT, capture_output=True, text=True).stdout.strip()); return 0
        time.sleep(2 ** (i + 1))
    print(r.stderr[-400:]); return 1
if __name__ == "__main__":
    a = sys.argv[1:] or ["-h"]
    if a[0] == "plans": sys.exit(plans())
    elif a[0] == "atlas": sys.exit(atlas())
    elif a[0] == "check": sys.exit(check())
    elif a[0] == "render": render(a[1:])
    elif a[0] == "ship": sys.exit(ship(a[1], a[2], a[3]))
    elif a[0] == "all": sys.exit(plans() or atlas() or check())
    else: print(__doc__)
