# Orderly attack clips by skeleton (2026-11-22): saw_rush, double_saw, lash, lamp_tear. Same rig as make_motion.py; every pose is rest + a few hand-set numbers.
# usage: python3 make_motion2.py  -> body_<clip>.json (post with pixellab_call.py /v2/animate-with-skeleton-v3)
import json, math, io, base64, copy, sys
from PIL import Image
r = json.load(open("est.json")); rig = r["rig"]; rest = r["frames"][0]; W, H = 188, 148; N = 8
PW, PH, OX, OY = 236, 172, 44, 24                                  # padded canvas: room for blades reaching out left and up
SEED = int(sys.argv[1]) if len(sys.argv) > 1 else 7
idx = {j["name"]: i for i, j in enumerate(rig)}
UP = ["hips", "chest", "neck", "head", "body_bag_root", "body_bag_mid", "body_bag_tip", "blade_near_shoulder", "blade_far_shoulder", "iv_pole_base", "iv_pole_top", "iv_hook", "blood_bag_top", "blood_bag_bottom"]
BODY = ["hips", "chest", "neck", "head", "body_bag_root", "body_bag_mid", "body_bag_tip", "iv_pole_base", "iv_pole_top", "iv_hook", "blood_bag_top", "blood_bag_bottom", "blade_near_shoulder", "blade_far_shoulder"]
def P(n, pts): p = pts[idx[n]]; return p["x"] * W, p["y"] * H
def put(pts, n, x, y): pts[idx[n]]["x"] = x / W; pts[idx[n]]["y"] = y / H
def move(pts, names, dx, dy):
    for n in names:
        x, y = P(n, pts); put(pts, n, x + dx, y + dy)
def swing(pts, side, deg, reach=1.0):                      # rotate the arm about its shoulder; negative = forward (left) and down, positive = back and up; reach scales the arm
    sx, sy = P(f"blade_{side}_shoulder", pts); a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    for part in ("elbow", "wrist", "tip"):
        x, y = P(f"blade_{side}_{part}", rest); x -= P(f"blade_{side}_shoulder", rest)[0]; y -= P(f"blade_{side}_shoulder", rest)[1]
        put(pts, f"blade_{side}_{part}", sx + (x * c - y * s) * reach, sy + (x * s + y * c) * reach)
def stride(pts, ph, amp, lift):                           # legs alternate in pairs, phase ph (0-1)
    for leg, pair in {"front_near": 0, "hind_near": 0, "mid_near": 1, "hind_far": 1}.items():
        t = (ph + 0.5 * pair) % 1.0
        if t < 0.5: off = amp / 2 - amp * (t / 0.5); lf = 0.0
        else: u = (t - 0.5) / 0.5; off = -amp / 2 + amp * u; lf = lift * math.sin(math.pi * u)
        for part in ("foot", "toe"):
            n = f"{leg}_{part}"
            if n in idx: x, y = P(n, rest); put(pts, n, x - off, y - lf)
        x, y = P(f"{leg}_knee", rest); put(pts, f"{leg}_knee", x - off * 0.5, y - lf * 0.9)
def pose(crouch=0.0, lean=0.0, rear=0.0, near=0.0, far=0.0, nreach=1.0, freach=1.0, ph=None, amp=0.0, lift=0.0, bag=0.0, sway=0.0, feet_back=0.0):
    pts = copy.deepcopy(rest)
    move(pts, BODY, lean, crouch)
    if rear:                                              # rear up: head, neck, chest rise and tip back, hips sink
        move(pts, ["chest", "neck", "head", "blade_near_shoulder", "blade_far_shoulder"], rear * 0.5, -rear * 0.9); move(pts, ["neck", "head"], rear * 0.5, -rear * 0.5)
        move(pts, ["hips", "body_bag_root"], rear * 0.4, rear * 0.2)
        for leg in ("front_near",):
            for part in ("hip", "knee", "foot", "toe"): x, y = P(f"{leg}_{part}", rest); put(pts, f"{leg}_{part}", x + rear * 0.3, y - rear * (0.45 if part in ("foot", "toe") else 0.2))
    swing(pts, "near", near, nreach); swing(pts, "far", far, freach)
    if ph is not None: stride(pts, ph, amp, lift)
    for leg in ("front_near", "mid_near", "hind_near", "hind_far"):       # crouched = knees out
        for part in ("hip", "knee"): x, y = P(f"{leg}_{part}", pts); put(pts, f"{leg}_{part}", x + lean * 0.2, y + crouch * 0.3)
    if feet_back:
        for n in idx:
            if n.endswith("_foot") or n.endswith("_toe"): x, y = P(n, pts); put(pts, n, x + feet_back, y)
    if bag:
        for n, k in (("body_bag_mid", 0.5), ("body_bag_tip", 1.0)): x, y = P(n, pts); put(pts, n, x + bag * k, y - abs(bag) * 0.4 * k)
    if sway: move(pts, ["iv_pole_top", "iv_hook", "blood_bag_top", "blood_bag_bottom"], sway, 0)
    return topad(pts)
def topad(pts):
    out = copy.deepcopy(pts)
    for p in out: p["x"] = round(min(0.99, max(0.01, (p["x"] * W + OX) / PW)), 4); p["y"] = round(min(0.99, max(0.01, (p["y"] * H + OY) / PH)), 4)
    return out
def lerp(a, b, t): return a + (b - a) * t
CLIPS = {}
# all numbers are PIXELS of the 188x148 stand. saw rush: coil low with both blades back, burst forward with the legs at a run (stride 34 px, lift 16), blades held out ahead, then slide out
CLIPS["saw_rush"] = [pose(crouch=10, lean=8, near=65, far=55, feet_back=4), pose(crouch=14, lean=12, near=75, far=62, feet_back=7),
    pose(crouch=8, lean=-8, near=20, far=10, ph=0.0, amp=36, lift=16), pose(crouch=7, lean=-16, near=-25, far=-20, ph=0.25, amp=40, lift=18),
    pose(crouch=8, lean=-20, near=-45, far=-38, ph=0.5, amp=40, lift=18), pose(crouch=8, lean=-20, near=-50, far=-42, ph=0.75, amp=40, lift=18),
    pose(crouch=6, lean=-14, near=-35, far=-30, ph=0.0, amp=26, lift=12), pose(crouch=2, lean=-4, near=-10, far=-8, ph=0.4, amp=8, lift=3)]
# double saw: near blade up and over (a diagonal chop), the far blade then sweeps flat across, the body twisting after it
CLIPS["double_saw"] = [pose(lean=3, near=30, far=5), pose(lean=5, near=55, far=5, rear=8), pose(lean=-4, near=0, far=5, crouch=3),
    pose(lean=-10, crouch=5, near=-70, far=10), pose(lean=-4, near=-60, far=50, crouch=3), pose(lean=0, near=-40, far=30, rear=4),
    pose(lean=-12, crouch=5, near=-45, far=-60), pose(lean=-6, near=-20, far=-30, crouch=2)]
# tube lash: rears up high and tips back with the blades folded away, hangs there, then snaps forward low and the tubes whip
CLIPS["lash"] = [pose(rear=6, bag=3, near=40, far=40), pose(rear=14, bag=6, near=55, far=55), pose(rear=22, bag=10, near=65, far=65), pose(rear=24, bag=12, near=65, far=65, sway=2),
    pose(rear=4, lean=-18, crouch=8, bag=-18, near=40, far=40), pose(rear=0, lean=-24, crouch=10, bag=-26, near=30, far=30), pose(rear=6, lean=-12, crouch=4, bag=-10, near=40, far=40), pose(rear=2, lean=-3, bag=-3, near=20, far=20)]
# lamp tear: rises on the hind legs, one blade straight up over the head and hooked, wrenched down; the far blade stays tucked
CLIPS["lamp_tear"] = [pose(rear=6, near=30, far=40), pose(rear=14, near=55, far=40, nreach=1.05), pose(rear=22, near=78, far=40, nreach=1.15), pose(rear=24, near=82, far=40, nreach=1.2, lean=-2),
    pose(rear=24, near=82, far=40, nreach=1.2, lean=-3, sway=2), pose(rear=14, lean=-8, near=35, far=40, nreach=1.1, crouch=4), pose(rear=4, lean=-10, near=-30, far=40, crouch=8), pose(rear=0, near=-10, far=20, crouch=3)]
im0 = Image.open("stand_pad.png").convert("RGBA"); im = Image.new("RGBA", (PW, PH), (0, 0, 0, 0)); im.paste(im0, (OX, OY)); b = io.BytesIO(); im.save(b, "PNG")
DESC = {"saw_rush": "pale hunched insect-like creature made of a human patient with two bloody saw-blade arms, four thin legs, black body bag on its back, blood bag on an iv pole, it crouches then charges forward at speed slashing both blades through",
        "double_saw": "pale hunched insect-like creature made of a human patient with two bloody saw-blade arms, four thin legs, black body bag on its back; it chops down with one blade then sweeps the other blade sideways",
        "lash": "pale hunched insect-like creature made of a human patient with two bloody saw-blade arms, four thin legs, black body bag on its back; it rears up then lunges forward whipping its body forward",
        "lamp_tear": "pale hunched insect-like creature made of a human patient with two bloody saw-blade arms, four thin legs, black body bag on its back; it rises up and hooks a blade overhead then wrenches it down"}
for name, fr in CLIPS.items():
    assert len(fr) == N
    body = {"description": DESC[name], "first_frame": {"type": "base64", "base64": base64.b64encode(b.getvalue()).decode()}, "first_frame_keypoints": topad(rest), "keypoints": fr, "rig": rig, "action": name, "direction": "west", "view": "side", "seed": SEED, "no_background": True}
    json.dump(body, open(f"body_{name}_s{SEED}.json", "w"))
    # a stick preview so the poses can be checked before spending generations
    from PIL import ImageDraw
    sheet = Image.new("RGB", (PW * 4 * 2, PH * 2 * 2), (24, 24, 28))
    for k, pts in enumerate(fr):
        tile = Image.new("RGB", (PW, PH), (24, 24, 28)); d = ImageDraw.Draw(tile); tile.paste(im.convert("RGB"), (0, 0), im.split()[3].point(lambda v: 70 if v else 0))
        pt = lambda n: (pts[idx[n]]["x"] * PW, pts[idx[n]]["y"] * PH)
        for chain, col in ((["hips", "chest", "neck", "head"], (230, 230, 230)), (["blade_near_shoulder", "blade_near_elbow", "blade_near_wrist", "blade_near_tip"], (255, 80, 80)), (["blade_far_shoulder", "blade_far_elbow", "blade_far_wrist", "blade_far_tip"], (255, 160, 60)),
                           (["body_bag_root", "body_bag_mid", "body_bag_tip"], (120, 120, 255)), (["iv_pole_base", "iv_pole_top"], (80, 255, 80))):
            d.line([pt(n) for n in chain], fill=col, width=2)
        for leg in ("front_near", "mid_near", "hind_near", "hind_far"): d.line([pt(f"{leg}_{p}") for p in ("hip", "knee", "foot") ], fill=(80, 220, 255), width=2)
        sheet.paste(tile.resize((PW * 2, PH * 2), Image.NEAREST), ((k % 4) * PW * 2, (k // 4) * PH * 2))
    sheet.save(f"stick_{name}.png")
print("ok")
