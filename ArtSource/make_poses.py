# Poses prototype (Yan, 2026-10-29: "besides the front and back sprites we added some 'poses' to characters, like how old mortal
# kombat games where basically a lot of png's that played based on what button you presses ... sitting down, crouching, leaning
# foward, leaning to the side to use the cover mechanics, and add a few in between frames"; "just this for now": two suits, a
# preview sheet to approve or redline). Nothing goes into the game from here yet.
# Each pose is made from the finished standing sprite (make_armor_avatars.build / make_unique_armor), cut into bands - the upper
# body (head to belt) and the legs - then moved: lowered, shifted sideways, legs bent, spread or folded, and re-outlined. So any
# suit gets every pose for free, front and back.
#   python3 ArtSource/make_poses.py  -> ArtSource/_out/poses_preview.png
import os, sys
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import make_armor_avatars as av
import make_unique_armor as ua

K = av.hexc("#141617")

def bands(heavy):
    """Rows: the upper body ends at the belt; legs under it; row 15 is the boots' bottom outline."""
    belt = 10 if heavy else 11
    return belt, list(range(belt + 1, 15))

def row(img, y): return [img.getpixel((x, y)) for x in range(16)]
def blank(): return [(0, 0, 0, 0)] * 16
def shift(r, s): return [r[x - s] if 0 <= x - s < 16 else (0, 0, 0, 0) for x in range(16)]
def spread(r, s):
    """Left half out by s, right half out by s (knees apart); the gap between stays empty."""
    out = blank()
    for x in range(8):
        if 0 <= x - s: out[x - s] = r[x]
    for x in range(8, 16):
        if x + s < 16: out[x + s] = r[x]
    return out
def halves(left, right): return left[:8] + right[8:]
def tone(r, f):
    return [(min(255, int(p[0] * f)), min(255, int(p[1] * f)), min(255, int(p[2] * f)), p[3]) if p[3] and p[:3] != K[:3] else p for p in r]

def assemble(upper, legs, down=0, lean=(0, 0)):
    """upper: rows 0..belt; legs: rows laid under it, bottom row at 15 (the outline row is added). down: lower the upper body.
    lean: (head px, torso px) sideways shift."""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0)); px = img.load()
    head_end = len(upper) - 5
    rows = []
    for y, r in enumerate(upper):
        s = lean[0] if y < head_end else lean[1] if y < len(upper) - 1 else int(lean[1] / 2)   # the belt follows halfway, no break
        rows.append(shift(r, s))
    rows += legs
    top = 16 - 1 - len(rows)                 # leave row 15 for the outline under the boots
    top = max(0, min(top, down + (16 - 1 - len(upper) - len(legs))))
    for i, r in enumerate(rows):
        y = top + i
        if 0 <= y < 15:
            for x in range(16):
                if r[x][3]: px[x, y] = r[x]
    clean = Image.new("RGBA", (16, 16), (0, 0, 0, 0)); cp = clean.load()
    for y in range(16):                       # drop the old outline, then outline the new silhouette (inner outline lines stay)
        for x in range(16):
            p = px[x, y]
            if p[3] and not (p[:3] == K[:3] and _edge(px, x, y)): cp[x, y] = p
    return av._outline(clean)

def _edge(px, x, y):
    """An outline pixel on the silhouette's edge (it touches nothing-ness) - those get redrawn."""
    return any(not (0 <= x + dx < 16 and 0 <= y + dy < 16) or px[x + dx, y + dy][3] == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))

def poses(img, heavy):
    belt, legs = bands(heavy)
    upper = [row(img, y) for y in range(0, belt + 1)]
    L = [row(img, y) for y in legs]            # thigh .. boots
    thigh, boots = L[0], L[-1]
    n = len(L)
    P = {}
    P["stand"] = img
    # crouch: knees out, the body down; the in-between halfway
    P["crouch 1/2"] = assemble(upper, [spread(thigh, 1)] + L[2:], down=1)
    P["crouch"] = assemble(upper, [spread(thigh, 1), spread(boots, 1)], down=n - 2)
    # kneel: down on the right knee, the left foot planted
    knee = halves(blank(), tone(thigh, 1.25))
    P["kneel 1/2"] = assemble(upper, [spread(thigh, 1), halves(spread(boots, 1), tone(thigh, 1.2))], down=n - 2)
    P["kneel"] = assemble(upper, [halves(spread(thigh, 1), blank()), halves(spread(boots, 1), tone(thigh, 1.25))], down=n - 1)
    # sit: on the ground facing you, thighs coming at you, feet out wide
    P["sit 1/2"] = assemble(upper, [spread(thigh, 1), spread(boots, 2)], down=n - 2)
    P["sit"] = assemble(upper, [spread(boots, 3)], down=n)              # on the seat of his pants, only the boots show
    # leaning out from cover: head and shoulders over, legs stay
    for side, s in (("left", -1), ("right", 1)):
        P[f"lean {side} 1/2"] = assemble(upper, L, lean=(2 * s, s))
        P[f"lean {side}"] = assemble(upper, L, lean=(3 * s, 2 * s))
    # lunge: the body forward and down, the front leg out, the back leg trailing
    P["lunge 1/2"] = assemble(upper, [halves(shift(thigh, 0), shift(thigh, 1))] + [halves(shift(r, 0), shift(r, 1)) for r in L[1:]], down=1, lean=(1, 1))
    P["lunge"] = assemble(upper, [halves(shift(thigh, -1), shift(thigh, 2)), halves(shift(boots, -1), shift(boots, 3))], down=n - 2, lean=(2, 2))
    # stagger: rocked back, feet apart to catch himself
    P["stagger 1/2"] = assemble(upper, [spread(r, 1) for r in L], lean=(-1, 0))
    P["stagger"] = assemble(upper, [spread(thigh, 1), spread(boots, 1)], down=n - 2 - 1 if n > 3 else 0, lean=(-2, -1))
    # lying down: on the back, head to the left
    P["lying"] = img.rotate(90, expand=True)
    return P

SUITS = [("Leather Jacket (Scavenger start)", "Leather Jacket", False), ("Black Visor Suit (the Visor)", "Black Visor Suit", True)]

def build(name, back=False):
    if name in ua.UNIQUE:
        L = ua.UNIQUE[name]
        return ua.front(name, L) if not back else av.build(name, 0.2, 2, L, back=True)
    tier, phys, anom, belt = next((a[1], a[2], a[3], a[4]) for a in av.ARMORS if a[0] == name)
    return av.build(name, anom, belt, back=back)

if __name__ == "__main__":
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 12)
    bold = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
    S = 6; CELL = 16 * S; GAP = 14
    sets = []
    for label, name, heavy in SUITS:
        for back in (False, True):
            sets.append((f"{label} - {'back' if back else 'front'}", poses(build(name, back), heavy)))
    names = list(sets[0][1].keys())
    W = 20 + len(names) * (CELL + GAP); Hh = 50 + len(sets) * (CELL + 46) + 150
    sheet = Image.new("RGBA", (W, Hh), av.hexc("#1b1d1f")); d = ImageDraw.Draw(sheet)
    d.text((20, 14), "Poses prototype - 2 suits, front and back (x6). Not in the game yet: approve or redline each.", font=bold, fill=av.hexc("#e6e6e6"))
    y = 50
    for title, P in sets:
        d.text((20, y), title, font=bold, fill=av.hexc("#b8bcc0")); y += 22
        for i, n in enumerate(names):
            x = 20 + i * (CELL + GAP); im = P[n]
            d.rectangle((x - 2, y - 2, x + CELL + 2, y + CELL + 2), fill=av.hexc("#2a2620"))
            big = im.resize((im.width * S, im.height * S), Image.NEAREST)
            sheet.alpha_composite(big, (x + (CELL - big.width) // 2, y + (CELL - big.height)))
            if y < 80: pass
        for i, n in enumerate(names):
            d.text((20 + i * (CELL + GAP), y + CELL + 4), n, font=font, fill=av.hexc("#b8bcc0"))
        y += CELL + 24
    # true size (x1) and 1080p (x5) for the Scavenger's front
    d.text((20, y + 6), "true size x1 and 1080p (x5), Scavenger front:", font=font, fill=av.hexc("#b8bcc0")); y += 26
    for i, n in enumerate(names):
        im = sets[0][1][n]; x = 20 + i * (CELL + GAP)
        sheet.alpha_composite(im, (x, y))
        b5 = im.resize((im.width * 5, im.height * 5), Image.NEAREST)
        sheet.alpha_composite(b5, (x + 18, y + 80 - b5.height + 10))
    out = os.path.join(HERE, "_out", "poses_preview.png"); sheet.save(out); print("wrote", out, len(names), "poses")
