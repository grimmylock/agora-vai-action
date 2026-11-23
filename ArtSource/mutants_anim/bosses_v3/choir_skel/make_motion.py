# Gallery Choir walk by skeleton (2026-11-23, Yan: 'do the choir walk like you did orderly, using the skeleton tool'): the rig of estimate-custom-skeleton (6 legs), the legs alternate in two groups, the mass bobs, then animate-with-skeleton-v3.
import json, math, io, base64, copy, sys
from PIL import Image
r = json.load(open("est.json")); rig = r["rig"]; rest = r["frames"][0]; N = 8
idx = {j["name"]: i for i, j in enumerate(rig)}
SEED = int(sys.argv[1]) if len(sys.argv) > 1 else 3
STRIDE, LIFT = float(sys.argv[2]) if len(sys.argv) > 2 else 0.055, 0.05          # fractions of the canvas width / height
FWD = -1                                                                        # forward on screen is left
legs = sorted({n.rsplit("_", 1)[0] for n in idx if n.startswith("leg_")}, key=lambda l: rest[idx[l + "_foot"]]["x"])   # left to right
GROUP = {l: i % 2 for i, l in enumerate(legs)}                                  # neighbours alternate (a legged mass walks in two groups)
def frame(k):
    t = k / N; pts = copy.deepcopy(rest); bob = 0.008 * abs(math.sin(2 * math.pi * t))
    for n in ("hips", "torso_mid", "chest", "neck", "head", "stub_root", "stub_tip", "arm_shoulder", "arm_elbow", "arm_wrist", "arm_tip") + tuple(n for n in idx if n.startswith("spike") or n.startswith("wheel")):
        if n in idx: pts[idx[n]]["y"] = round(rest[idx[n]]["y"] - bob, 4)
    for leg in legs:
        ph = (t + 0.5 * GROUP[leg]) % 1.0
        if ph < 0.5: off = STRIDE / 2 - STRIDE * (ph / 0.5); lf = 0.0
        else: u = (ph - 0.5) / 0.5; off = -STRIDE / 2 + STRIDE * u; lf = LIFT * math.sin(math.pi * u)
        dx = -off if FWD < 0 else off
        for part, w in (("foot", 1.0), ("knee", 0.5)):
            n = f"{leg}_{part}"; p = pts[idx[n]]; p["x"] = round(rest[idx[n]]["x"] + dx * w, 4); p["y"] = round(rest[idx[n]]["y"] - lf * (1.0 if part == "foot" else 0.8) - (bob if part == "knee" else 0), 4)
    for p in pts: p["x"] = round(min(0.99, max(0.01, p["x"])), 4); p["y"] = round(min(0.99, max(0.01, p["y"])), 4)
    return pts
frames = [frame(k) for k in range(N)]
im = Image.open("stand_pad.png").convert("RGBA"); b = io.BytesIO(); im.save(b, "PNG")
body = {"description": "a massive hulking mass of fused human bodies with many faces, walking on six thin human legs along its sides, a wheel on its flank, spikes of scrap on its back", "action": "walk", "direction": "west", "view": "side",
        "first_frame": {"type": "base64", "base64": base64.b64encode(b.getvalue()).decode()}, "first_frame_keypoints": rest, "keypoints": frames, "rig": rig, "seed": SEED, "no_background": True}
json.dump(body, open(f"anim_body_s{SEED}.json", "w")); print(len(frames), "frames; legs", legs)
