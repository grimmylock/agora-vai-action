# The Barrier's steel deck and watchtowers (Yan, 2026-11-18: "looks good" on the raised-deck what-if; "2 raised concrete watchtowers with the machineguns"). Props for Tier 4's Barrier (Barrier.cs):
#   deck_face  - the deck's south face seen from the yard (a steel skirt with rivets, a hard drop shadow under it): one tile, placed on the ground row just south of the deck
#   deck_rail  - a railing along the deck's south edge (two rails, a post at each end): placed on the deck's last row
#   deck_steps - three steps down to the yard, instead of the face
#   tower_conc - a concrete watchtower: a chamfered base, a shaft with slit windows and a ladder, a railed top with a tripod machine gun behind a shield (about 2 tiles wide, 4 tall)
# Same interface as props.py (Prop(im, pivot, shadow, tall)); hooked in at the end of props.py.
from PIL import ImageDraw
from props import Prop, finish
from worldlib import *

def _px(im, x, y, c):
    if 0 <= x < im.width and 0 <= y < im.height: im.putpixel((x, y), c)

def deck_face(seed):
    s = P["steel"]; im = blank(16, 16); r = rng(seed)
    for x in range(16): _px(im, x, 0, s[4]); _px(im, x, 1, s[3])                       # the lit top edge
    for y in range(2, 8):
        for x in range(16): _px(im, x, y, s[2] if y < 6 else s[1])
    for x in range(16): _px(im, x, 4, s[1])                                              # a stiffening rib
    for x in (2, 13): _px(im, x, 3, s[0]); _px(im, x, 6, s[0])                           # rivets
    if seed % 3 == 0:
        for x in range(5, 11): _px(im, x, 5, P["rust"][1])                               # a rusty streak
    for y in range(8, 14):                                                               # the shadow it throws on the ground
        a = int(150 * (1 - (y - 8) / 6.0))
        for x in range(16): _px(im, x, y, SHADOW + (a,))
    return Prop(im, (8, 13), None, False)

def deck_rail(seed):
    s = P["steel"]; im = blank(16, 13)
    for x in range(16): _px(im, x, 2, s[4]); _px(im, x, 3, s[2]); _px(im, x, 7, s[3]); _px(im, x, 8, s[1])
    for x in (0, 1, 14, 15):
        for y in range(1, 13): _px(im, x, y, s[3] if x in (0, 14) else s[1])
    return Prop(im, (8, 12), None, False)

def deck_steps(seed):
    s = P["steel"]; im = blank(16, 16)
    for k, (y0, inset) in enumerate(((0, 1), (4, 0), (8, 0))):
        for x in range(inset, 16 - inset):
            _px(im, x, y0, s[4]); _px(im, x, y0 + 1, s[3])                                # the tread, lit
            _px(im, x, y0 + 2, s[1]); _px(im, x, y0 + 3, s[0])                            # the riser, in shade
    for y in range(0, 12):
        _px(im, 0, y, s[2]); _px(im, 15, y, s[1])                                          # the stringers
    for y in range(12, 16):
        a = int(150 * (1 - (y - 12) / 4.0))
        for x in range(16): _px(im, x, y, SHADOW + (a,))
    return Prop(im, (8, 13), None, False)

def tower_conc(seed):
    """A concrete guard tower on four piers, drawn to sit beside props.watchtower (the timber one: same size, same 3/4 view, same outline): refs/watchtower (the prison guard tower: a wide
    platform on a column; the square towers) and refs/bld_bunker (board-marked concrete, stains, rebar). Four heavy piers with two ring beams and steel cross-bracing, a zig-zag steel stair,
    a concrete platform with a notched parapet, a flat roof slab on posts, and under it the gun: a tripod machine gun behind a shield with an ammo box and a floodlight. 3 tiles wide, ~6 tall;
    the map gives it a 3x2 footprint with one open tile (the gun nook) behind the barrier (Emplacement.cs)."""
    c = P["concrete"]; s = P["steel"]; ru = P["rust"]; W, H = 52, 104; im = blank(W, H); d = ImageDraw.Draw(im); r = rng(seed)
    def line(x0, y0, x1, y1, col, t=1): d.line([x0, y0, x1, y1], fill=col, width=t)
    def box(x0, y0, x1, y1, lit, mid, dark):                                                       # a pier or a beam: lit left edge, shaded right edge
        d.rectangle([x0, y0, x1, y1], fill=mid); d.line([x0, y0, x0, y1], fill=lit); d.line([x1, y0, x1, y1], fill=dark)
    # back piers (further, darker) and the steel cross-bracing between the piers
    for x0 in (11, 38): box(x0, 56, x0 + 3, 99, c[2], c[1], c[0])
    for y0, y1 in ((96, 70), (70, 56)):
        line(15, y0, 37, y1, ru[1]); line(37, y0, 15, y1, ru[1])
    # ring beams behind (shade), then the front piers with their footings
    for y in (68, 90): d.rectangle([6, y, 46, y + 2], fill=c[1])
    for x0 in (3, 43):
        box(x0, 52, x0 + 5, 98, c[4], c[3], c[1])                                                   # the pier
        for y in range(56, 96, 6): line(x0 + 1, y, x0 + 4, y, c[2])                                  # board-marked formwork seams
        d.rectangle([x0 - 1, 96, x0 + 6, 101], fill=c[3]); line(x0 - 1, 96, x0 + 6, 96, c[4]); line(x0 - 1, 101, x0 + 6, 101, c[0])   # the footing
    # front ring beams, one with the barrier's hazard stripes
    box(3, 66, 48, 69, c[4], c[3], c[1]); line(3, 66, 48, 66, c[4]); line(4, 69, 47, 69, c[0])
    box(3, 88, 48, 91, c[4], c[3], c[1]); line(4, 91, 47, 91, c[0])
    for x in range(10, 42):
        for y in (89, 90): px(im, x, y, hx("#c9a63e") if (x + y) % 6 < 3 else hx("#2a2a28"))
    # the steel stair: two flights zig-zagging up the front, a landing between
    def flight(x0, y0, x1, y1):
        line(x0, y0 + 1, x1, y1 + 1, s[1], 2)
        n = max(3, abs(y1 - y0) // 3)
        for i in range(n + 1):
            t = i / n; sx, sy = int(x0 + (x1 - x0) * t), int(y0 + (y1 - y0) * t)
            line(sx - 3, sy, sx + 3, sy, s[4]); line(sx - 3, sy + 1, sx + 3, sy + 1, s[2])
        line(x0, y0 - 6, x1, y1 - 6, s[3])
    flight(38, 99, 14, 74); d.rectangle([10, 72, 19, 74], fill=s[2]); line(10, 72, 19, 72, s[4]); flight(16, 72, 38, 55)
    # the platform: a slab seen from the front, a chamfered underside, the notched parapet
    d.rectangle([1, 49, 50, 53], fill=c[3]); line(1, 49, 50, 49, c[4]); line(1, 53, 50, 53, c[1]); d.polygon([(4, 54), (47, 54), (44, 57), (7, 57)], fill=c[0])
    d.rectangle([1, 40, 50, 48], fill=c[2]); d.rectangle([1, 40, 3, 48], fill=c[3]); d.rectangle([48, 40, 50, 48], fill=c[1])
    for x in range(2, 50, 9): d.rectangle([x, 36, x + 5, 39], fill=c[3]); line(x, 36, x + 5, 36, c[4]); line(x + 5, 37, x + 5, 39, c[1])   # the merlons
    for x in range(8, 46, 9): d.rectangle([x + 1, 43, x + 5, 45], fill=c[0])                       # firing slits
    # the roof: a flat slab on four posts, its top lit, a floodlight on a bracket
    for x0 in (4, 46): box(x0, 12, x0 + 1, 36, s[3], s[2], s[0])
    for x0 in (14, 36): box(x0, 16, x0 + 1, 36, s[2], s[1], s[0])
    d.rectangle([5, 17, 46, 35], fill=c[0]); d.rectangle([5, 17, 46, 22], fill=c[1])                # the shadow under the roof: the back wall's inner face
    d.rectangle([2, 6, 49, 11], fill=c[4]); d.rectangle([2, 6, 49, 8], fill=c[4]); line(2, 6, 49, 6, c[4]); d.rectangle([1, 12, 50, 16], fill=c[2]); line(1, 12, 50, 12, c[3]); line(1, 16, 50, 16, c[0])
    for x in range(4, 48, 7): line(x, 8, x + 3, 8, c[3])                                            # roof board marks
    line(46, 17, 46, 20, s[3]); d.rectangle([44, 20, 48, 22], fill=s[1]); line(44, 20, 48, 20, s[4]); px(im, 46, 23, hx("#e8d890"))   # the lamp
    # the machine gun: a tripod, the body, a long barrel, a shield plate and the ammo box
    line(24, 36, 19, 30, s[1]); line(28, 36, 33, 30, s[1]); line(26, 36, 26, 28, s[2])
    d.rectangle([17, 24, 33, 28], fill=s[2]); d.rectangle([17, 24, 33, 25], fill=s[4]); d.rectangle([33, 25, 46, 26], fill=s[1]); line(33, 25, 46, 25, s[3]); d.rectangle([14, 25, 16, 27], fill=s[1])
    d.rectangle([20, 29, 24, 33], fill=P["olive"][2]); line(20, 29, 24, 29, P["olive"][4])
    line(31, 20, 31, 31, s[3]); line(32, 20, 32, 31, s[4]); line(33, 21, 33, 30, s[2])            # the shield
    # weathering: rain streaks down from every edge, moss at the footings, rust and chips with rebar, a stain from the roof
    for _ in range(16):
        x = r.randrange(3, 49); y0 = r.choice((16, 54, 70, 92)); col = c[1] if y0 < 60 else c[2]
        for y in range(y0, y0 + r.randint(5, 12)): px(im, x, y, col)
    for x0 in (3, 43):
        for _ in range(14): px(im, r.randrange(x0 - 1, x0 + 6), r.randrange(92, 101), P["moss"][r.randint(0, 1)])
    for k in range(5): px(im, 44 + k % 2, 70 + k * 3, ru[1 + k % 3]); px(im, 5 + k % 2, 74 + k * 2, ru[1 + k % 3])
    for _ in range(18): px(im, r.randrange(2, 50), r.randrange(40, 100), c[1] if r.random() < .6 else c[4])
    return finish(im, (W // 2, 101), shadow=(50, 9, 0, -2), tall=True)

NEW = {"deck_face": deck_face, "deck_rail": deck_rail, "deck_steps": deck_steps, "tower_conc": tower_conc}
VARIANTS = {"deck_face": 3}
