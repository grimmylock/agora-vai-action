# Batch 131b (Yan: "does not look good, we'll need to draw frame by frame, the dogs bite without moving their mouth, the brute doesnt
# use his big arm for stuff, their legs bug a lot"). Frame-by-frame: the approved body and head stay, every leg is redrawn per frame
# from hand-keyed joints (hip/shoulder, knee/elbow, hock/wrist, paw - feet planted on the ground), the lower jaw is cut free and
# hinged so a bite opens and snaps shut. Pilot: the Pallid Prowler (run 6 frames, bite 6 frames).
#   python3 make_mutant_frames.py <out_dir>
import math, os, sys
from PIL import Image
import make_mutants_hd as hd
import mutant_from_ref as m

def limb(img, pts, widths, col, lit, dark):
    """A leg as joined segments, each `widths[i]` px thick: lit on its top/left pixels, shaded on its bottom/right."""
    p = img.load(); W, H = img.size; drawn = set()
    for (x0, y0), (x1, y1), w in zip(pts, pts[1:], widths):
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(int(n * 2) + 1):
            x, y = x0 + (x1 - x0) * i / (n * 2), y0 + (y1 - y0) * i / (n * 2)
            for dx in range(w):
                for dy in range(w):
                    xx, yy = int(round(x - (w - 1) / 2 + dx)), int(round(y - (w - 1) / 2 + dy))
                    if 0 <= xx < W and 0 <= yy < H: drawn.add((xx, yy))
    for (x, y) in drawn:
        c = col
        if (x - 1, y) not in drawn or (x, y - 1) not in drawn: c = lit
        if (x + 1, y) not in drawn and (x, y + 1) not in drawn: c = dark
        p[x, y] = c

def cut(im, fn):
    part = Image.new("RGBA", im.size, (0, 0, 0, 0)); rest = im.copy(); a, p, r = im.load(), part.load(), rest.load()
    for y in range(im.height):
        for x in range(im.width):
            if a[x, y][3] and fn(x, y): p[x, y] = a[x, y]; r[x, y] = (0, 0, 0, 0)
    return part, rest

# ------------------------------------------------------------------ Pallid Prowler
PALE = dict(col=(204, 199, 182, 255), lit=(224, 221, 206, 255), dark=(170, 161, 140, 255))
FAR = dict(col=(140, 135, 118, 255), lit=(160, 154, 136, 255), dark=(98, 95, 84, 255))
HIP, SHOULDER = (12, 21), (27, 21)
# run: per frame (knee, hock, paw) for the near hind leg and (elbow, wrist, paw) for the near foreleg; far legs run 3 frames behind
HIND = [((8, 25), (4, 28), (1, 31)), ((11, 26), (8, 28), (8, 31)), ((15, 25), (13, 28), (15, 31)),
        ((15, 26), (12, 29), (14, 31)), ((12, 26), (8, 29), (6, 31)), ((10, 26), (5, 29), (3, 31))]
FORE = [((29, 25), (33, 28), (36, 30)), ((28, 25), (30, 29), (31, 31)), ((26, 25), (25, 29), (24, 31)),
        ((25, 25), (22, 28), (19, 30)), ((26, 24), (25, 27), (27, 28)), ((28, 24), (31, 26), (34, 28))]
BOB = [0, 0, -1, -1, 0, 0]

def pallid_parts():
    base = [it[2] for it in hd.sprites(outlined=False) if it[1] == "pallid_prowler"][0]
    a = base.load()
    acid = lambda x, y: a[x, y][1] > a[x, y][0] + 40                # the green drool stays wherever it hangs
    _, body = cut(base, lambda x, y: y >= 23 and not acid(x, y))  # every old leg pixel goes (one reached under the head)
    jaw, body = cut(body, lambda x, y: x >= 40 and y >= 21)       # lower jaw (with its drool), hinged at the back of the mouth
    return body, jaw

def legs(img, i, oy=0, hip=HIP, sh=SHOULDER, plant=None):
    j = (i + 3) % 6
    def pts(hp, table, k, far):
        dx = 3 if far else 0
        return [(hp[0] + dx, hp[1] + oy)] + [(x + dx, y + (0 if y >= 31 else oy)) for x, y in table[k]]
    limb(img, pts(hip, HIND, j, True), (3, 2, 2), **FAR); limb(img, pts(sh, FORE, j, True), (3, 2, 2), **FAR)
    if plant: return
    limb(img, pts(hip, HIND, i, False), (3, 2, 2), **PALE); limb(img, pts(sh, FORE, i, False), (3, 2, 2), **PALE)

def pallid_run(body, jaw):
    frames = []
    for i in range(6):
        f = Image.new("RGBA", (56, 36), (0, 0, 0, 0)); L, T = 4, 4
        layer = Image.new("RGBA", (48, 32), (0, 0, 0, 0)); legs(layer, i, 0)   # far legs behind the body
        far = layer
        near = Image.new("RGBA", (48, 32), (0, 0, 0, 0))
        j = (i + 3) % 6
        limb(near, [(HIP[0], HIP[1] + BOB[i])] + [(x, y if y >= 31 else y + BOB[i]) for x, y in HIND[i]], (3, 2, 2), **PALE)
        limb(near, [(SHOULDER[0], SHOULDER[1] + BOB[i])] + [(x, y if y >= 31 else y + BOB[i]) for x, y in FORE[i]], (3, 2, 2), **PALE)
        farl = Image.new("RGBA", (48, 32), (0, 0, 0, 0))
        limb(farl, [(HIP[0] + 3, HIP[1] + BOB[i])] + [(x + 3, y if y >= 31 else y + BOB[i]) for x, y in HIND[j]], (3, 2, 2), **FAR)
        limb(farl, [(SHOULDER[0] + 3, SHOULDER[1] + BOB[i])] + [(x + 3, y if y >= 31 else y + BOB[i]) for x, y in FORE[j]], (3, 2, 2), **FAR)
        f.alpha_composite(farl, (L, T)); f.alpha_composite(body, (L, T + BOB[i])); f.alpha_composite(jaw, (L, T + BOB[i]))
        f.alpha_composite(near, (L, T))
        frames.append(m.outline(f))
    return frames

def open_jaw(f, jaw, ox, oy, deg):
    """Hinge the lower jaw open by deg (clockwise = down) about (40, 21); dark red mouth fills the gap."""
    hinge = (40, 21)
    if deg:
        p = f.load(); t = math.radians(deg)
        for x in range(41, 46):                                 # the inside of the mouth: a wedge under the upper jaw, behind the snout
            depth = min(4, int((x - 40) * math.tan(t)) + 1)
            for k in range(depth):
                xx, yy = ox + x, oy + 21 + k
                if 0 <= xx < f.width and 0 <= yy < f.height and not p[xx, yy][3]:
                    # Batch 131d (Yan: "a few white parts inside his mouth to simulate teeth"): fangs along the top and bottom
                    tooth = (k == 0 and x % 2 == 0) or (depth >= 2 and k == depth - 1 and x % 2 == 1)
                    p[xx, yy] = (240, 234, 216, 255) if tooth else (70, 20, 22, 255)
        jaw = jaw.rotate(-deg, resample=Image.NEAREST, center=hinge)
    f.alpha_composite(jaw, (ox, oy))

def pallid_bite(body, jaw):
    keys = [(0, 0, 0), (-1, 0, 16), (2, -1, 32), (4, 0, 0), (2, 0, 0), (0, 0, 0)]   # (head dx, dy, jaw open degrees)
    frames = []
    for i, (hx, hy, deg) in enumerate(keys):
        f = Image.new("RGBA", (56, 36), (0, 0, 0, 0)); L, T = 4, 4
        bx = max(0, hx - 1)                                        # the body follows the lunge a little
        planted = 1 if i in (2, 3, 4) else 0                       # forelegs braced forward for the lunge
        farl = Image.new("RGBA", (48, 32), (0, 0, 0, 0)); near = Image.new("RGBA", (48, 32), (0, 0, 0, 0))
        hk, fk = (3, 1) if planted else (2, 2)
        limb(farl, [(HIP[0] + 3 + bx, HIP[1])] + [(x + 3, y) for x, y in HIND[(hk + 3) % 6]], (3, 2, 2), **FAR)
        limb(farl, [(SHOULDER[0] + 3 + bx, SHOULDER[1])] + [(x + 3 + bx, y) for x, y in FORE[(fk + 3) % 6]], (3, 2, 2), **FAR)
        limb(near, [(HIP[0] + bx, HIP[1])] + list(HIND[hk]), (3, 2, 2), **PALE)
        limb(near, [(SHOULDER[0] + bx, SHOULDER[1])] + [(x + bx + (2 if planted else 0), y) for x, y in FORE[0 if planted else fk]], (3, 2, 2), **PALE)
        head, torso = cut(body, lambda x, y: x >= 37)
        f.alpha_composite(farl, (L, T)); f.alpha_composite(torso, (L + bx, T))
        for k in range(bx, hx): f.alpha_composite(head, (L + k, T + hy))   # neck stretches to the head
        f.alpha_composite(head, (L + hx, T + hy))
        open_jaw(f, jaw, L + hx, T + hy, deg)
        f.alpha_composite(near, (L, T))
        f = m.outline(f)
        if i == 3:                                                 # the snap: a white bite flash at the teeth
            p = f.load()
            for x, y in ((52, 21), (53, 23), (52, 26), (54, 25), (53, 20)):
                if not p[x, y][3]: p[x, y] = (245, 240, 225, 200)
        frames.append(f)
    return frames

# ------------------------------------------------------------------ Batch 131c (Yan: "the walk animation is still not that great, it
# bugs a lot, also his legs look like stubs, he has no paws like the initial sprite"): legs solved with fixed bone lengths (2-bone IK)
# so they never stretch; each foot is planted and slides back at a steady speed while it bears weight, then swings forward in an arc;
# a 4-beat walk (legs a quarter cycle apart) in 8 frames; legs taper from thigh to shin and end in long pale toes along the ground;
# no body bob (the jitter).
GROUND = 31

def ik(a, b, l1, l2, bend):
    """Joint between a and b with bones l1, l2; bend = +1 puts the joint forward (+x side of the a->b line), -1 back."""
    dx, dy = b[0] - a[0], b[1] - a[1]; d = max(1e-3, min(l1 + l2 - 1e-3, math.hypot(dx, dy)))
    ca = (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d); ang = math.atan2(dy, dx) + bend * math.acos(max(-1, min(1, ca)))
    return a[0] + l1 * math.cos(ang), a[1] + l1 * math.sin(ang)

def foot_at(phase, front, back, lift=3.0, stance=0.6):
    """Foot path over one cycle: planted and sliding front->back for `stance` of it, then an arc back to the front."""
    if phase < stance:
        t = phase / stance; return front + (back - front) * t, GROUND
    t = (phase - stance) / (1 - stance); return back + (front - back) * t, GROUND - lift * math.sin(math.pi * t)

def toes(img, x, y, n, col, tip):
    p = img.load()
    for k in range(n):
        xx, yy = int(round(x + 1 + k)), int(round(y))
        if 0 <= xx < img.width and 0 <= yy < img.height: p[xx, yy] = tip if k == n - 1 else col

def hind_leg(img, hip, foot, pal, front_toe=3):
    fx, fy = foot; hock = (fx - 1, fy - 3)                         # metatarsus: short, nearly upright, heel a bit back
    knee = ik(hip, hock, 5.5, 4.5, -1)                              # stifle forward, hock back (screen y down: -1 = forward here)
    limb(img, [hip, knee], (3,), **pal); limb(img, [knee, hock], (2,), **pal); limb(img, [hock, (fx, fy - 1)], (2,), **pal)
    toes(img, fx, fy, front_toe, pal["col"], pal["lit"])

def fore_leg(img, sh, foot, pal, front_toe=3):
    fx, fy = foot; wrist = (fx, fy - 2)
    elbow = ik(sh, wrist, 5.0, 4.5, 1)                              # elbow back
    limb(img, [sh, elbow], (3,), **pal); limb(img, [elbow, wrist], (2,), **pal); limb(img, [wrist, (fx, fy - 1)], (2,), **pal)
    toes(img, fx, fy, front_toe, pal["col"], pal["lit"])

# gait: 4-beat walk, left hind 0, left fore 0.25, right hind 0.5, right fore 0.75; near = left (the side we see)
HIND_RANGE, FORE_RANGE = (19, 2), (41, 25)   # long strides: legs reach out on diagonals like the reference

def walk_legs(i, n=8, dx=0, dy=0):
    far = Image.new("RGBA", (48, 32), (0, 0, 0, 0)); near = Image.new("RGBA", (48, 32), (0, 0, 0, 0))
    for layer, off, pal, shift in ((far, 0.5, FAR, 3), (near, 0.0, PALE, 0)):
        ph = (i / n + off) % 1.0
        hind_leg(layer, (HIP[0] + shift + dx, HIP[1] + 1 + dy), (foot_at(ph, *HIND_RANGE)[0] + shift + dx, foot_at(ph, *HIND_RANGE)[1]), pal)
        ph2 = (i / n + off + 0.25) % 1.0
        fx, fy = foot_at(ph2, *FORE_RANGE)
        fore_leg(layer, (SHOULDER[0] + shift + dx, SHOULDER[1] + 1 + dy), (fx + shift + dx, fy), pal)
    return far, near

WALK_BOB = [0, 0, 1, 1, 0, 0, 1, 1]

def pallid_walk(body, jaw, n=8):
    frames = []
    for i in range(n):
        f = Image.new("RGBA", (56, 36), (0, 0, 0, 0)); L, T = 4, 4
        b = WALK_BOB[i * len(WALK_BOB) // n]                      # Batch 131d (Yan: "make his body bob around a little bit"):
        far, near = walk_legs(i, n, dy=b)                          # the body dips a pixel twice a stride, hips and shoulders with it
        f.alpha_composite(far, (L, T)); f.alpha_composite(body, (L, T + b)); f.alpha_composite(jaw, (L, T + b)); f.alpha_composite(near, (L, T))
        frames.append(m.outline(f))
    return frames

def pallid_bite2(body, jaw):
    keys = [(0, 0, 0), (-1, 0, 16), (2, -1, 32), (4, 0, 0), (2, 0, 0), (0, 0, 0)]   # (head dx, dy, jaw open degrees)
    frames = []
    head, torso = cut(body, lambda x, y: x >= 37)
    for i, (hx, hy, deg) in enumerate(keys):
        f = Image.new("RGBA", (56, 36), (0, 0, 0, 0)); L, T = 4, 4
        bx = max(0, hx - 1)
        far = Image.new("RGBA", (48, 32), (0, 0, 0, 0)); near = Image.new("RGBA", (48, 32), (0, 0, 0, 0))
        brace = hx > 0                                                # forelegs thrown forward, hind legs pushing
        # near and far legs on clearly different spots, or they fuse into one blob
        hind_leg(far, (HIP[0] + 3 + bx, HIP[1] + 1), (16 if brace else 15, GROUND), FAR)
        fore_leg(far, (SHOULDER[0] + 3 + bx, SHOULDER[1] + 1), (30 + bx if brace else 28, GROUND), FAR)
        hind_leg(near, (HIP[0] + bx, HIP[1] + 1), (5 if brace else 8, GROUND), PALE)
        fore_leg(near, (SHOULDER[0] + bx, SHOULDER[1] + 1), (39 + bx if brace else 35, GROUND), PALE)
        f.alpha_composite(far, (L, T)); f.alpha_composite(torso, (L + bx, T))
        for k in range(bx, hx): f.alpha_composite(head, (L + k, T + hy))
        f.alpha_composite(head, (L + hx, T + hy))
        open_jaw(f, jaw, L + hx, T + hy, deg)
        f.alpha_composite(near, (L, T))
        f = m.outline(f)
        if i == 3:
            p = f.load()
            for x, y in ((52, 21), (53, 23), (52, 26), (54, 25), (53, 20)):
                if not p[x, y][3]: p[x, y] = (245, 240, 225, 200)
        frames.append(f)
    return frames

# ------------------------------------------------------------------ Batch 131e (Yan: "i don't like how much the legs changed ... the initial
# sprites ... had a lot more detail in the paws and legs, the new ones make them kind of squish together and lose a lot of detail").
# Back to the approved legs: each of the four is cut out of the sprite on its own, split at the knee/elbow, and bent there - the
# upper part turns about the hip/shoulder, the lower part rides on the moved knee and flexes as the leg swings forward. The pixels
# are the original ones throughout (paws, toes, shading). Body bob and the toothy bite stay.
# per leg: (x range of its pixels below the belly, hip, knee, behind the body?)
PALLID_LEGS = {
    "near_hind": ((0, 13), (11, 22), (6, 25), False),
    "far_hind":  ((14, 21), (17, 22), (15, 26), True),
    "near_fore": ((22, 31), (26, 22), (27, 26), False),
    "far_fore":  ((32, 43), (33, 22), (35, 26), True),
}
PHASE = {"near_hind": 0.0, "near_fore": 0.25, "far_hind": 0.5, "far_fore": 0.75}   # 4-beat walk

def seg_dist(p, a, b):
    ax, ay = a; bx, by = b; px, py = p; vx, vy = bx - ax, by - ay
    t = max(0, min(1, ((px - ax) * vx + (py - ay) * vy) / max(1e-6, vx * vx + vy * vy)))
    return math.hypot(px - ax - t * vx, py - ay - t * vy)

def pallid_rig():
    base = [it[2] for it in hd.sprites(outlined=False) if it[1] == "pallid_prowler"][0]
    a = base.load()
    acid = lambda x, y: a[x, y][1] > a[x, y][0] + 40
    legs = {}
    for name, ((x0, x1), hip, knee, far) in PALLID_LEGS.items():
        up = Image.new("RGBA", base.size, (0, 0, 0, 0)); lo = Image.new("RGBA", base.size, (0, 0, 0, 0)); pu, pl = up.load(), lo.load()
        for y in range(22, base.height):
            for x in range(x0, x1 + 1):
                if not a[x, y][3] or acid(x, y): continue
                # upper part: above the knee, nearer the hip-knee line than the knee's own level
                (pu if y <= knee[1] else pl)[x, y] = a[x, y]
        legs[name] = (up, lo, hip, knee, far)
    _, body = cut(base, lambda x, y: y >= 22 and not acid(x, y))
    jaw, body = cut(body, lambda x, y: x >= 40 and y >= 21)
    return body, jaw, legs

def pose_leg(f, leg, ang_u, ang_l, ox, oy, lift=0):
    up, lo, hip, knee, far = leg
    t = math.radians(ang_u); kx, ky = knee[0] - hip[0], knee[1] - hip[1]
    nkx = hip[0] + kx * math.cos(t) + ky * math.sin(t); nky = hip[1] - kx * math.sin(t) + ky * math.cos(t)   # knee after the turn
    lo2 = lo.rotate(ang_u + ang_l, resample=Image.NEAREST, center=knee)
    f.alpha_composite(lo2, (int(round(ox + nkx - knee[0])), int(round(oy + nky - knee[1] - lift))))
    f.alpha_composite(up.rotate(ang_u, resample=Image.NEAREST, center=hip), (ox, oy - lift))

def leg_angles(name, i, n=8, swing=13, flex=22):
    ph = 2 * math.pi * (i / n + PHASE[name])
    fwd = math.cos(ph)                                   # > 0 while the leg is swinging forward (off the ground)
    return swing * math.sin(ph), -flex * max(0.0, fwd), 1 if fwd > 0.5 else 0

def pallid_walk3(body, jaw, legs, n=8):
    frames = []
    for i in range(n):
        f = Image.new("RGBA", (56, 36), (0, 0, 0, 0)); L, T = 4, 4; b = WALK_BOB[i]
        for name in ("far_hind", "far_fore"):
            u, l, lift = leg_angles(name, i, n); pose_leg(f, legs[name], u, l, L, T + b, lift)
        f.alpha_composite(body, (L, T + b)); f.alpha_composite(jaw, (L, T + b))
        for name in ("near_hind", "near_fore"):
            u, l, lift = leg_angles(name, i, n); pose_leg(f, legs[name], u, l, L, T + b, lift)
        frames.append(m.outline(f))
    return frames

def pallid_bite3(body, jaw, legs):
    keys = [(0, 0, 0), (-1, 0, 16), (2, -1, 32), (4, 0, 0), (2, 0, 0), (0, 0, 0)]   # (head dx, dy, jaw open degrees)
    brace = {"near_hind": -8, "far_hind": -6, "near_fore": 10, "far_fore": 8}         # lunge: forelegs reach, hind legs push
    head, torso = cut(body, lambda x, y: x >= 37)
    frames = []
    for i, (hx, hy, deg) in enumerate(keys):
        f = Image.new("RGBA", (56, 36), (0, 0, 0, 0)); L, T = 4, 4
        bx = max(0, hx - 1); k = 1.0 if hx >= 2 else 0.4 if hx else 0.0
        for name in ("far_hind", "far_fore"): pose_leg(f, legs[name], brace[name] * k, 0, L + bx, T)
        f.alpha_composite(torso, (L + bx, T))
        for kk in range(bx, hx): f.alpha_composite(head, (L + kk, T + hy))
        f.alpha_composite(head, (L + hx, T + hy))
        open_jaw(f, jaw, L + hx, T + hy, deg)
        for name in ("near_hind", "near_fore"): pose_leg(f, legs[name], brace[name] * k, 0, L + bx, T)
        f = m.outline(f)
        if i == 3:
            p = f.load()
            for x, y in ((52, 21), (53, 23), (52, 26), (54, 25), (53, 20)):
                if not p[x, y][3]: p[x, y] = (245, 240, 225, 200)
        frames.append(f)
    return frames

# ------------------------------------------------------------------ the Blight Brute (Batch 131f)
# Yan: "he drags his arm around with him and the rock throw and a melee swipe" / "the brute doesnt use his big arm for stuff".
# Same idea as the hounds' approved legs: the original pixels, cut into the rock-crusted club arm, the claw arm and the two legs,
# turned at their joints per frame. The club does the work: it drags on the ground while walking, chops overhead in the swipe,
# and digs a boulder out of the ground and hurls it in the throw. The shoulder stays on the body so a raised arm leaves no hole.
B_SHOULDER, B_TIP = (17, 31), (8, 60)        # club pivot and the club's end resting on the ground (sprite coords)
B_CLAW_SH = (55, 30)
BL, BT, BW, BH = 12, 30, 100, 94             # frame: room behind for the drag dust, above for the raised club and boulder

def brute_rig():
    base = [it[2] for it in hd.sprites(outlined=False) if it[1] == "blight_brute"][0]
    edge = lambda y: 21 if y < 52 else 21 - (y - 52) * 0.6        # the club's right side, leaning left where it meets the leg
    club, body = cut(base, lambda x, y: y >= 30 and x < edge(y))
    keep, _ = cut(club, lambda x, y: math.hypot(x - B_SHOULDER[0], y - B_SHOULDER[1]) < 6)
    body.alpha_composite(keep)                                    # the shoulder stays put
    claw, body = cut(body, lambda x, y: x >= 52 and y >= 34)
    legL, body = cut(body, lambda x, y: y >= 55 and x < 37)
    legR, body = cut(body, lambda x, y: y >= 55 and x >= 37)
    return dict(club=club, claw=claw, legL=legL, legR=legR, body=body)

def rot_pt(p, pivot, deg):
    t = math.radians(deg); vx, vy = p[0] - pivot[0], p[1] - pivot[1]
    return pivot[0] + vx * math.cos(t) + vy * math.sin(t), pivot[1] - vx * math.sin(t) + vy * math.cos(t)

def boulder(w=14, h=11):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); p = im.load()
    for y in range(h):
        for x in range(w):
            dx, dy = (x - (w - 1) / 2) / (w / 2), (y - (h - 1) / 2) / (h / 2)
            d = dx * dx + dy * dy + 0.15 * math.sin(x * 1.7 + y)
            if d < 1: p[x, y] = (110, 102, 90, 255) if dx + dy < -0.3 else (78, 72, 62, 255) if dx + dy < 0.5 else (56, 51, 44, 255)
    for x, y in ((4, 6), (8, 3), (9, 7), (3, 4)):
        if p[x, y][3]: p[x, y] = (46, 42, 36, 255)
    p[5, 3] = (197, 203, 72, 255)                                  # a crystal in it, like the club's
    for x, y in ((2, h - 2), (6, h - 1), (10, h - 2)):             # clods of earth hanging off the bottom
        if p[x, y - 1][3]: p[x, y] = (72, 58, 40, 255)
    return m.outline(im)

def dust(f, pts, col=(150, 140, 120, 160)):
    p = f.load()
    for x, y in pts:
        x, y = int(round(x)), int(round(y))
        if 0 <= x < f.width and 0 <= y < f.height and not p[x, y][3]: p[x, y] = col

def puff(cx, cy, r, n, seed):
    import random
    rnd = random.Random(seed)
    return [(cx + rnd.uniform(-r, r), cy - abs(rnd.gauss(0, r / 2.5))) for _ in range(n)]

def brute_pose(R, club=0.0, claw=0.0, dx=0, dy=0, lL=0, lR=0, fL=0, fR=0, rock=None, fly=None, puffs=(), arc=None, reach=(0, 0), bury=False):
    """One frame. club/claw: degrees counter-clockwise on screen (the club hangs down at 0; -180 is straight up, -270 forward).
    dx, dy: the body's lean; lL/lR: a foot lifted, fL/fR: moved forward. rock: the boulder on the club's end.
    reach: the shoulder thrown forward (the body twists into a swing); bury: the club's end sinks into the ground (cut at it)."""
    f = Image.new("RGBA", (BW, BH), (0, 0, 0, 0)); ox, oy = BL + dx, BT + dy
    for leg, lift, fwd in ((R["legR"], lR, fR), (R["legL"], lL, fL)):
        for k in range(min(0, dy), 1): f.alpha_composite(leg, (BL + fwd, BT - lift + k))   # stretch up to a raised body
    f.alpha_composite(R["claw"].rotate(claw, resample=Image.NEAREST, center=B_CLAW_SH), (ox, oy)) if claw > 30 else None
    f.alpha_composite(R["body"], (ox, oy))
    if claw <= 30: f.alpha_composite(R["claw"].rotate(claw, resample=Image.NEAREST, center=B_CLAW_SH), (ox, oy))
    cl = R["club"].rotate(club, resample=Image.NEAREST, center=B_SHOULDER)
    if bury:
        cl = cl.copy(); cp = cl.load()
        for y in range(BT + 63 - oy - reach[1], cl.height):
            for x in range(cl.width): cp[x, y] = (0, 0, 0, 0)
    if abs(club) > 20 or reach[0]:                                # crossing the body: its own outline keeps it readable
        lit = Image.eval(cl, lambda v: v)                             # nearer the eye than the body: a touch lighter
        lp = lit.load()
        for y in range(lit.height):
            for x in range(lit.width):
                c = lp[x, y]
                if c[3]: lp[x, y] = tuple(min(255, int(v * 1.18 + 10)) for v in c[:3]) + (255,)
        f = m.outline(f); f.alpha_composite(m.outline(lit), (ox + reach[0], oy + reach[1]))
    else:
        f.alpha_composite(cl, (ox + reach[0], oy + reach[1])); f = m.outline(f)
    if arc: smear(f, *arc)
    if rock:
        tx, ty = rot_pt(B_TIP, B_SHOULDER, club); b = boulder(); tx += reach[0]; ty += reach[1]
        f.alpha_composite(b, (int(ox + tx - b.width / 2 + rock[0]), int(oy + ty - b.height / 2 + rock[1])))
    if fly:
        b = boulder(); f.alpha_composite(b, fly)
    dust(f, puffs)
    return f

def smear(f, cx, cy, r, a0, a1, col=(240, 234, 216)):
    """A swipe arc behind the club's end, fading towards its tail (angles in degrees, math orientation)."""
    p = f.load(); n = 28
    for i in range(n):
        a = math.radians(a0 + (a1 - a0) * i / (n - 1))
        for dr in (-1, 0, 1):
            x, y = int(round(cx + (r + dr) * math.cos(a))), int(round(cy - (r + dr) * math.sin(a)))
            if 0 <= x < f.width and 0 <= y < f.height and not p[x, y][3]: p[x, y] = col + (int(40 + 190 * i / n),)

def brute_anims():
    R = brute_rig()
    sx, sy = BL + B_SHOULDER[0], BT + B_SHOULDER[1]
    tip = lambda a, dx=0, dy=0: tuple(v + o for v, o in zip(rot_pt(B_TIP, B_SHOULDER, a), (BL + dx, BT + dy)))
    g = BT + 63                                                    # ground line
    # walk: a heavy 8-beat plod; the club trails as the body steps forward and is dragged back in, scraping dust
    lag = [0, -3, -6, -8, -5, -2, 1, 1]
    walk = []
    for i in range(8):
        tx, ty = tip(lag[i])
        walk.append(brute_pose(R, club=lag[i], claw=4 * math.sin(i / 8 * 2 * math.pi), dy=(0, 1, 1, 0, 0, 1, 1, 0)[i],
                               lL=(1, 3, 2, 0, 0, 0, 0, 0)[i], fL=(0, 1, 2, 2, 0, 0, 0, 0)[i],
                               lR=(0, 0, 0, 0, 1, 3, 2, 0)[i], fR=(0, 0, 0, 0, 0, 1, 2, 2)[i],
                               puffs=puff(tx - 3, g, 4, 6 + 2 * (i % 3), i) if lag[i] < -2 else ()))
    # swipe: club back, up over the head, the body twists to throw the shoulder forward and chops it down in front, dust, rest
    keys = [(0, 0, 0, 0, 0), (-50, -1, 1, 4, 0), (-140, -1, -1, 6, 2), (-185, 0, -2, 8, 6), (-245, 2, 0, 10, 18), (-292, 3, 2, 2, 22),
            (-292, 3, 2, 0, 22), (-325, 2, 1, -2, 10), (-360, 0, 0, 0, 0)]
    swipe = []
    for i, (a, dx, dy, cl, rx) in enumerate(keys):
        arc = None
        if i == 4: arc = (sx + dx + rx, sy + dy, 31, 120, 15)
        if i == 5: arc = (sx + dx + rx, sy + dy, 31, 75, -20)
        tx, ty = tip(a, dx + rx, dy)
        a = a % 360 if a % 360 < 180 else a % 360 - 360
        swipe.append(brute_pose(R, club=a, claw=cl, dx=dx, dy=dy, reach=(rx, 0), fR=1 if dx > 1 else 0, arc=arc,
                                puffs=puff(tx, g, 6, 14, 50 + i) if i in (5, 6) else ()))
    # throw: crouch and drive the club into the ground at its side, rip a boulder out, haul it back and over, hurl it forward
    t0 = tip(0)
    T = [dict(club=0), dict(club=-6, dy=1, claw=6), dict(club=0, dy=4, claw=8, bury=True, puffs=puff(t0[0], g, 7, 26, 7)),
         dict(club=4, dy=3, bury=True, rock=(2, 6), puffs=puff(t0[0], g, 9, 34, 8)),
         dict(club=8, dy=1, rock=(1, 1), puffs=puff(t0[0], g - 1, 11, 30, 9)),
         dict(club=-70, dy=0, rock=(0, 0)), dict(club=-160, dy=-1, rock=(0, -2), claw=10, reach=(2, 0)),
         dict(club=-200, dx=1, dy=-1, rock=(1, -2), claw=12, reach=(6, 0)),
         dict(club=-245, dx=3, dy=0, fR=1, reach=(14, 0), fly=(BW - 30, BT - 4), arc=(sx + 17, sy, 31, 120, 40)),
         dict(club=-290, dx=3, dy=1, fR=1, reach=(14, 0), fly=(BW - 16, BT - 8)), dict(club=-330, dx=1, dy=0, reach=(6, 0)), dict(club=0)]
    throw = [brute_pose(R, **k) for k in T]
    return {"walk": walk, "attack": swipe, "throw": throw}

def save(out):
    os.makedirs(out, exist_ok=True)
    body, jaw, legs = pallid_rig()
    sets = {"pallid_prowler": {"walk": pallid_walk3(body, jaw, legs), "attack": pallid_bite3(body, jaw, legs)},
            "blight_brute": brute_anims()}
    for key, anims in sets.items():
        for name, frames in anims.items():
            w, h = frames[0].size; strip = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
            for i, f in enumerate(frames): strip.alpha_composite(f, (i * w, 0))
            strip.save(os.path.join(out, f"mutant_{key}_{name}_strip.png"))
            sc = 6 if w < 80 else 4
            big = [Image.alpha_composite(Image.new("RGBA", f.size, (42, 38, 32, 255)), f).resize((w * sc, h * sc), Image.NEAREST).convert("P", palette=Image.ADAPTIVE) for f in frames]
            ms = 110 if name == "walk" else 130
            if key == "blight_brute": ms = 150 if name == "walk" else 120   # a heavy plod
            big[0].save(os.path.join(out, f"{key}_{name}.gif"), save_all=True, append_images=big[1:], duration=ms, loop=0)
    return sets

def brute_preview(path, anims=None):
    """The Brute's three animations side by side, 3x - the review GIF."""
    anims = anims or brute_anims(); n = 0
    for fr in anims.values(): n = max(n, len(fr))
    frames = []
    for t in range(n * 2):
        im = Image.new("RGBA", (BW * 3 * 3 + 40, BH * 3 + 20), (42, 38, 32, 255))
        for i, fr in enumerate(anims.values()):
            f = fr[t % len(fr)]; im.alpha_composite(f.resize((BW * 3, BH * 3), Image.NEAREST), (10 + i * (BW * 3 + 10), 10))
        frames.append(im.convert("P", palette=Image.ADAPTIVE))
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=130, loop=0)

if __name__ == "__main__":
    sets = save(sys.argv[1] if len(sys.argv) > 1 else "_out/mutant_frames")
    if len(sys.argv) > 2: brute_preview(sys.argv[2], sets["blight_brute"])
