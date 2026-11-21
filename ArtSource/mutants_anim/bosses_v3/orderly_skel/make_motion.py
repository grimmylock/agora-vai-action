# Orderly walk by skeleton (2026-11-21): the custom rig from estimate-custom-skeleton, every leg posed by hand per frame, then animate-with-skeleton-v3.
import json, math, io, base64, copy
from PIL import Image
r = json.load(open("est.json")); rig = r["rig"]; rest = r["frames"][0]; W, H = 188, 148; N = 8
idx = {j["name"]: i for i, j in enumerate(rig)}
LEGS = {"front_near": 0, "hind_near": 0, "mid_near": 1, "hind_far": 1}          # leg: its pair (pairs alternate)
STRIDE = {"front_near": 0.085, "hind_near": 0.07, "mid_near": 0.045, "hind_far": 0.05}; LIFT = 0.07            # foot travel (x fraction of the width), lift (y fraction of the height)
FWD = -1                                                                       # forward on screen is left
def frame(k):
    t = k / N; pts = copy.deepcopy(rest); bob = 0.010 * abs(math.sin(2 * math.pi * t))
    for name in ("hips", "chest", "neck", "head", "blade_near_shoulder", "blade_near_elbow", "blade_near_wrist", "blade_near_tip", "blade_far_shoulder", "blade_far_elbow", "blade_far_wrist", "blade_far_tip", "body_bag_root", "body_bag_mid", "body_bag_tip", "iv_pole_base", "iv_pole_top", "iv_hook", "blood_bag_top", "blood_bag_bottom"):
        pts[idx[name]]["y"] = round(rest[idx[name]]["y"] - bob, 4)
    sway = 0.012 * math.sin(2 * math.pi * t)
    for name in ("iv_pole_top", "iv_hook", "blood_bag_top", "blood_bag_bottom"): pts[idx[name]]["x"] = round(rest[idx[name]]["x"] + sway, 4)
    for name in ("blade_near_tip", "blade_far_tip", "blade_near_wrist", "blade_far_wrist"): pts[idx[name]]["y"] = round(pts[idx[name]]["y"] + 0.012 * math.sin(2 * math.pi * t + 1), 4)
    for leg, pair in LEGS.items():
        ph = (t + 0.5 * pair) % 1.0; s = STRIDE[leg]
        if ph < 0.5: off = s / 2 - s * (ph / 0.5); lf = 0.0                      # stance: the foot slides back under the body
        else: u = (ph - 0.5) / 0.5; off = -s / 2 + s * u; lf = LIFT * math.sin(math.pi * u)  # swing: forward in an arc
        dx = FWD * off * -1 if False else (-off if FWD < 0 else off)           # off > 0 = forward: forward is -x
        for part in ("foot", "toe"):
            if f"{leg}_{part}" not in idx: continue
            p = pts[idx[f"{leg}_{part}"]]; p["x"] = round(rest[idx[f"{leg}_{part}"]]["x"] + dx, 4); p["y"] = round(rest[idx[f"{leg}_{part}"]]["y"] - lf, 4)
        kn = pts[idx[f"{leg}_knee"]]; kn["x"] = round(rest[idx[f"{leg}_knee"]]["x"] + dx * 0.5, 4); kn["y"] = round(rest[idx[f"{leg}_knee"]]["y"] - lf * 0.9 - bob, 4)
    return pts
frames = [frame(k) for k in range(N)]
im = Image.open("stand_pad.png").convert("RGBA"); b = io.BytesIO(); im.save(b, "PNG")
body = {"description": "pale hunched insect-like creature made of a human patient, two bloody saw-blade arms held up like a mantis, four thin legs, a black body bag on its back and a blood bag hanging on an IV pole", "action": "walk", "direction": "west", "view": "side",
        "first_frame": {"type": "base64", "base64": base64.b64encode(b.getvalue()).decode()}, "first_frame_keypoints": rest, "keypoints": frames, "rig": rig, "seed": 3, "no_background": True}
json.dump(body, open("anim_body.json", "w")); print(len(frames), "frames")
