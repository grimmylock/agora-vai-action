# Batch 131 (Yan: "the 5 you did look great, lets try animating them - for the dogs, a run animation cycle and bite/claw swipe;
# For the brute, he drags his arm around with him and the rock throw and a melee swipe"). The double-resolution sprites
# (make_mutants_hd.py) cut into parts - hind and fore legs swinging from their hips, the head, the Brute's club arm, claw arm and
# legs - posed per frame, then outlined. A boulder is drawn for the throw.
#   python3 make_mutant_anims.py <out_dir>    -> mutant_<key>_<anim>_strip.png (frames left to right) + <key>_<anim>.gif previews
import math, os, sys
from PIL import Image
import make_mutants_hd as hd
import mutant_from_ref as m
from make_mutants_v2 import Canvas, render

# per hound: belly line (legs below it), the x that splits hind from fore legs, hip pivots, where the head starts, frame padding
RIGS = {
    "pallid_prowler": dict(belly=23, split=21, hind=(13, 22), fore=(30, 23), head=38, pad=(6, 10, 4)),
    "blind_maw":      dict(belly=22, split=23, hind=(16, 21), fore=(30, 21), head=33, pad=(6, 10, 4), drop_shadow=29),
    "veil_prowler":   dict(belly=20, split=26, hind=(16, 19), fore=(32, 19), head=36, pad=(6, 10, 4)),
    "dread_prowler":  dict(belly=33, split=30, hind=(22, 32), fore=(42, 32), head=50, pad=(6, 12, 4)),
}

def parts_of(im, mask_fn):
    """Split im into (part, rest) by a predicate on pixel coordinates."""
    part = Image.new("RGBA", im.size, (0, 0, 0, 0)); rest = im.copy()
    a, p, r = im.load(), part.load(), rest.load()
    for y in range(im.height):
        for x in range(im.width):
            if a[x, y][3] and mask_fn(x, y): p[x, y] = a[x, y]; r[x, y] = (0, 0, 0, 0)
    return part, rest

def place(frame, layer, dx=0, dy=0, angle=0.0, pivot=(0, 0)):
    """layer (same size as the base) rotated about pivot (degrees, + = counter-clockwise on screen), then offset into frame."""
    if angle: layer = layer.rotate(angle, resample=Image.NEAREST, center=pivot)
    frame.alpha_composite(layer, (dx, dy))

def blank(base, pad):
    l, r, t = pad
    return Image.new("RGBA", (base.width + l + r, base.height + t), (0, 0, 0, 0))

def smear(frame, cx, cy, r, a0, a1, col=(232, 226, 210)):
    """A short white swipe arc (the claw or bite), alpha falling off along it."""
    p = frame.load(); n = 14
    for i in range(n):
        a = math.radians(a0 + (a1 - a0) * i / (n - 1))
        for dr in (0, 1):
            x, y = int(round(cx + (r + dr) * math.cos(a))), int(round(cy - (r + dr) * math.sin(a)))
            if 0 <= x < frame.width and 0 <= y < frame.height and not p[x, y][3]: p[x, y] = col + (int(90 + 140 * i / n),)

# ------------------------------------------------------------------ hounds
def hound(key, base):
    R = RIGS[key]; l, rpad, t = R["pad"]
    if R.get("drop_shadow"):   # the reference's ground shadow would swing with the legs: drop the darkest pixels at the feet
        a = base.load()
        for y in range(R["drop_shadow"], base.height):
            for x in range(base.width):
                if a[x, y][3] and sum(a[x, y][:3]) < 60: a[x, y] = (0, 0, 0, 0)
    hind, rest = parts_of(base, lambda x, y: y >= R["belly"] and x < R["split"])
    fore, rest = parts_of(rest, lambda x, y: y >= R["belly"] and x >= R["split"])
    head, body = parts_of(rest, lambda x, y: x >= R["head"])
    def pose(dx=0, dy=0, hind_a=0.0, fore_a=0.0, head_dx=0, head_dy=0, fore_dx=0):
        f = blank(base, R["pad"]); ox, oy = l + dx, t + dy
        place(f, hind, ox, oy, hind_a, R["hind"]); place(f, fore, ox + fore_dx, oy, fore_a, R["fore"])
        place(f, body, ox, oy)
        for k in range(min(0, head_dx), max(0, head_dx)): place(f, head, ox + k, oy)   # fill the neck as the head moves out
        place(f, head, ox + head_dx, oy + head_dy)
        return f
    run = []
    for i in range(6):   # legs scissor (hind and fore in opposite phase), body bobs, head counter-bobs
        s = math.sin(i / 6 * 2 * math.pi)
        run.append(pose(dy=-1 if i in (1, 4) else 0, hind_a=-26 * s, fore_a=26 * s, head_dy=1 if i in (1, 4) else 0))
    bite = [pose(),
            pose(dx=-1, dy=1, hind_a=6, fore_a=-6, head_dx=-1, head_dy=-1),           # gathers itself
            pose(dx=2, hind_a=-14, fore_a=24, head_dx=2, fore_dx=1),                  # lunges, forelegs reaching
            pose(dx=4, hind_a=-18, fore_a=34, head_dx=3, head_dy=1, fore_dx=2),       # bite and claw
            pose(dx=2, hind_a=-8, fore_a=10, head_dx=1),
            pose()]
    run = [m.outline(f) for f in run]; bite = [m.outline(f) for f in bite]
    hx = l + base.width + 1; hy = t + R["belly"] - 6
    smear(bite[3], hx - 8, hy + 2, 8, 80, -70, (245, 240, 225))                       # the swipe in front of it
    smear(bite[3], hx - 8, hy + 2, 10, 70, -60, (245, 240, 225))
    return {"run": run, "attack": bite}

# ------------------------------------------------------------------ the Blight Brute
def boulder():
    c = Canvas(12, 10); c.ell(5.6, 5, 5.0, 3.8, "R")
    for x, y in ((3, 3), (7, 4), (5, 6), (8, 6)): c.px(x, y, "r")
    c.px(4, 2, "c")
    return render(c, {"R": "#6e665a", "r": "#4a443a", "c": "#c5cb48"}, "rc")

def brute(base):
    L, RP, T = 6, 22, 22                                  # room for the drag dust, the swipe and the boulder overhead
    club, rest = parts_of(base, lambda x, y: x < 21 and y >= 22)
    claw, rest = parts_of(rest, lambda x, y: x >= 53 and y >= 28)
    legL, rest = parts_of(rest, lambda x, y: y >= 55 and x < 37)
    legR, body = parts_of(rest, lambda x, y: y >= 55 and x >= 37)
    rock_im = boulder()
    shoulder, hand = (55, 30), (58, 60)
    def hand_at(theta):   # where the claw hand is with the arm turned theta degrees (screen counter-clockwise = forward/up)
        t = math.radians(theta); vx, vy = hand[0] - shoulder[0], hand[1] - shoulder[1]
        return shoulder[0] + vx * math.cos(t) + vy * math.sin(t), shoulder[1] - vx * math.sin(t) + vy * math.cos(t)
    def pose(dy=0, club_dx=0, club_dy=0, claw_a=0.0, lL=0, lR=0, rock="", fly=None, dust=()):
        f = Image.new("RGBA", (base.width + L + RP, base.height + T), (0, 0, 0, 0)); ox, oy = L, T + dy
        place(f, legL, L, T - lL); place(f, legR, L, T - lR)
        place(f, body, ox, oy); place(f, club, ox + club_dx, oy + club_dy)
        place(f, claw, ox, oy, claw_a, shoulder)
        f = m.outline(f)
        if rock == "hand":                                  # the boulder in its fist
            hx, hy = hand_at(claw_a); f.alpha_composite(rock_im, (int(ox + hx - 6), int(oy + hy - 6)))
        if fly: f.alpha_composite(rock_im, fly)
        p = f.load()
        for x, y in dust:                                   # dust and dirt: the dragged arm, the torn-up ground
            if 0 <= x < f.width and 0 <= y < f.height and not p[x, y][3]: p[x, y] = (150, 140, 120, 150)
        return f
    foot = T + base.height - 2
    walk = [pose(lL=2, club_dx=0),
            pose(dy=-1, lL=1, club_dx=-1),
            pose(club_dx=-1, dust=((L + 1, foot), (L - 1, foot - 1), (L + 3, foot - 1))),
            pose(lR=2, club_dx=0),
            pose(dy=-1, lR=1, club_dx=-1),
            pose(club_dx=-1, dust=((L + 2, foot), (L, foot - 2), (L + 4, foot)))]
    hx0, hy0 = hand_at(20)
    rip = tuple((int(L + hx0 + dx), int(T + hy0 + dy)) for dx, dy in ((-5, 4), (5, 3), (-2, 6), (3, 7), (7, 5), (-7, 6)))
    throw = [pose(),
             pose(dy=2, claw_a=20),                                    # crouches, reaching down in front
             pose(dy=2, claw_a=20, rock="hand", dust=rip),             # rips a boulder out of the ground
             pose(dy=1, claw_a=80, rock="hand"),                       # heaves it up
             pose(dy=-1, claw_a=165, rock="hand"),                     # overhead
             pose(claw_a=110, fly=(L + 70, T + 6)),                    # hurls it
             pose(claw_a=60, fly=(L + 84, T + 12)),
             pose()]
    swipe = [pose(), pose(claw_a=-30, dy=1), pose(claw_a=60), pose(claw_a=95), pose(claw_a=30), pose()]
    sx, sy = L + shoulder[0], T + shoulder[1]
    smear(swipe[2], sx, sy, 30, -70, 10, (245, 240, 225)); smear(swipe[3], sx, sy, 31, -40, 40, (245, 240, 225))
    smear(swipe[3], sx, sy, 28, -30, 35, (245, 240, 225))
    return {"walk": walk, "throw": throw, "attack": swipe}

def all_anims():
    out = {}
    for name, key, im in hd.sprites(outlined=False):
        out[key] = brute(im) if key == "blight_brute" else hound(key, im)
    return out

def save(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for key, anims in all_anims().items():
        for anim, frames in anims.items():
            w, h = frames[0].size; strip = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
            for i, f in enumerate(frames): strip.alpha_composite(f, (i * w, 0))
            strip.save(os.path.join(out_dir, f"mutant_{key}_{anim}_strip.png"))
            ms = 110 if anim in ("run", "walk") else 130
            big = [Image.alpha_composite(Image.new("RGBA", f.size, (42, 38, 32, 255)), f).resize((w * 4, h * 4), Image.NEAREST).convert("P", palette=Image.ADAPTIVE) for f in frames]
            big[0].save(os.path.join(out_dir, f"{key}_{anim}.gif"), save_all=True, append_images=big[1:], duration=ms, loop=0)

def preview(path, anims=None):
    """Every animation looping side by side (3x, on ground colour) - the review GIF."""
    anims = anims or all_anims()
    cells = [(k, a, fr) for k, d in anims.items() for a, fr in d.items()]
    n = 24                                                 # 6- and 8-frame loops both fit
    cw = max(fr[0].width for _, _, fr in cells) * 3 + 16; chh = max(fr[0].height for _, _, fr in cells) * 3 + 16
    cols = 4; rows = (len(cells) + cols - 1) // cols
    frames = []
    for t in range(n):
        im = Image.new("RGBA", (cols * cw, rows * chh), (42, 38, 32, 255))
        for i, (k, a, fr) in enumerate(cells):
            f = fr[t % len(fr)]; big = f.resize((f.width * 3, f.height * 3), Image.NEAREST)
            im.alpha_composite(big, ((i % cols) * cw + 8, (i // cols) * chh + chh - 8 - big.height))
        frames.append(im.convert("P", palette=Image.ADAPTIVE))
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=120, loop=0)

if __name__ == "__main__":
    save(sys.argv[1] if len(sys.argv) > 1 else "_out/mutant_anims")
    if len(sys.argv) > 2: preview(sys.argv[2])
