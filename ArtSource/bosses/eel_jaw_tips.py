"""Where the player's two hands go on the eel's head in the jaw-hold (Yan, 2026-11-06: a sketch, the character stands in FRONT of the open mouth, one hand on the tip of the upper jaw and one on the tip of the
lower jaw, the jaws closing as he pushes): for each jaw frame of eel_bake_jaw.py, the front of the upper jaw (fixed) and the front of the lower jaw (it swings with the jaw), in pixels from the anchor (where the
body grows from), for the head as drawn (looking left; a mirrored head mirrors x).  python3 ArtSource/bosses/eel_jaw_tips.py   prints the numbers the Unity view (CombatFxView.Eel.cs) carries.
Importable: TIPS = [(ux, uy, lx, ly) per frame], and the openness values OPEN."""
import sys, os, math
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import eel_poses as E, eel_bake_jaw as J


def _front(mask, rows, run):
    """the front of a jaw: the leftmost column that is `run` pixels thick (vertically) within the given rows - the barbels and the whiskers are thinner and are ignored; returns (x, y of the middle of that run)"""
    for x in range(mask.shape[1]):
        best = None
        for y in rows:
            if y + run <= mask.shape[0] and mask[y:y + run, x].all(): best = y + run // 2; break
        if best is not None: return (x, best)
    return None


def compute():
    H0, A0 = E.HEAD.copy(), E.HEAD_ANCHOR; H, W = H0.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W]; line = J.LINE[0] * xx + J.LINE[1]
    lower = (yy > line) & (xx < J.HINGE[0] + 2) & (H0[:, :, 3] > 0); upper = (H0[:, :, 3] > 0) & ~lower
    u = _front(upper, range(36, 62), 8); lo = _front(lower, range(58, 90), 6)
    out = []
    for o in J.OPEN:
        a = math.radians(J.angle_for(o)); rx, ry = lo[0] - J.HINGE[0], lo[1] - J.HINGE[1]
        lx = J.HINGE[0] + rx * math.cos(a) - ry * math.sin(a); ly = J.HINGE[1] + rx * math.sin(a) + ry * math.cos(a)
        out.append((u[0] - A0[0], u[1] - A0[1], lx - A0[0], ly - A0[1]))
    return out, u, lo


TIPS, _U, _L = compute()
OPEN = J.OPEN

if __name__ == "__main__":
    print("upper tip", _U, "lower tip as drawn", _L)
    for o, t in zip(OPEN, TIPS): print(o, [round(v, 1) for v in t])
    print("C#: UpX", [round(t[0] / 32, 3) for t in TIPS]); print("UpY", [round(-t[1] / 32, 3) for t in TIPS])
    print("LoX", [round(t[2] / 32, 3) for t in TIPS]); print("LoY", [round(-t[3] / 32, 3) for t in TIPS])
