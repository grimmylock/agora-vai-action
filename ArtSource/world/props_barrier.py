# The Barrier's steel deck and watchtowers (Yan, 2026-11-18: "looks good" on the raised-deck what-if; "2 raised concrete watchtowers with the machineguns"). Props for Tier 4's Barrier (Barrier.cs):
#   deck_face  - the deck's south face seen from the yard (a steel skirt with rivets, a hard drop shadow under it): one tile, placed on the ground row just south of the deck
#   deck_rail  - a railing along the deck's south edge (two rails, a post at each end): placed on the deck's last row
#   deck_steps - three steps down to the yard, instead of the face
#   tower_conc - a concrete watchtower: a chamfered base, a shaft with slit windows and a ladder, a railed top with a tripod machine gun behind a shield (about 2 tiles wide, 4 tall)
# Same interface as props.py (Prop(im, pivot, shadow, tall)); hooked in at the end of props.py.
from PIL import ImageDraw
from props import Prop
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
    c = P["concrete"]; s = P["steel"]; W, H = 40, 72; im = blank(W, H); d = ImageDraw.Draw(im); r = rng(seed)
    cx = W // 2
    def box(x0, y0, x1, y1, lit, mid, dark):                                              # a lit left face strip, a mid face, a dark right edge
        d.rectangle([x0, y0, x1, y1], fill=mid); d.rectangle([x0, y0, x0 + 2, y1], fill=lit); d.rectangle([x1 - 1, y0, x1, y1], fill=dark)
    # the chamfered base, the shaft, the top slab
    box(4, 58, 35, 70, c[3], c[2], c[0]); d.polygon([(4, 58), (8, 54), (31, 54), (35, 58)], fill=c[3])
    box(9, 22, 30, 56, c[3], c[2], c[0])
    for y in range(26, 54, 9):
        d.rectangle([17, y, 22, y + 3], fill=c[0]); d.rectangle([17, y, 22, y], fill=c[1])   # slit windows
    for y in range(24, 56, 4):
        for x in range(10, 30): 
            if r.random() < 0.05: _px(im, x, y, c[1])                                      # board marks and chips
    for y in range(34, 70, 3):
        d.line([13, y, 16, y], fill=s[3]); d.line([13, y + 1, 16, y + 1], fill=s[1])       # the ladder's rungs up the front (left)
    d.line([12, 34, 12, 69], fill=s[2]); d.line([17, 34, 17, 69], fill=s[2])
    # the top: a wide slab with a parapet, and the gun behind its shield
    box(3, 16, 36, 22, c[4], c[3], c[1])
    d.rectangle([3, 8, 36, 15], fill=c[2]); d.rectangle([3, 8, 36, 9], fill=c[4]); d.rectangle([3, 8, 5, 15], fill=c[3]); d.rectangle([35, 8, 36, 15], fill=c[0])
    for x in range(8, 33, 6): d.rectangle([x, 11, x + 2, 13], fill=c[0])                  # firing slots in the parapet
    # the machine gun: tripod legs, the body, the barrel poking out over the parapet to the right, a shield
    d.line([16, 8, 12, 4], fill=s[1]); d.line([22, 8, 26, 4], fill=s[1]); d.line([19, 8, 19, 2], fill=s[2])
    d.rectangle([13, 0, 26, 4], fill=s[2]); d.rectangle([13, 0, 26, 1], fill=s[4]); d.rectangle([26, 1, 36, 2], fill=s[1]); d.rectangle([26, 1, 36, 1], fill=s[3])
    d.rectangle([11, 0, 12, 6], fill=s[3])                                                  # the shield's edge
    im = outline_sel(pad(im, 1)); return Prop(im, (cx + 1, H + 0), (34, 8, 0, -2), True)

NEW = {"deck_face": deck_face, "deck_rail": deck_rail, "deck_steps": deck_steps, "tower_conc": tower_conc}
VARIANTS = {"deck_face": 3}
