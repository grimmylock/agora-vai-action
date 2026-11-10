# Double-density (x2) side views for the 3/4 furniture: lockers, desk, filing cabinet seen from the end.
# Real proportions (Yan 2026-11-10: the 16 px side views were "too thin"): in this 3/4 view an object turned 90 degrees shows
#   its DEPTH as the new width, its old width foreshortened as the top surface above the end face (the reference
#   furniture pack's "TopSide" views show the same: a long top strip over a narrow end panel, wide as the object is deep).
# Colours are sampled from the object's own front sprite, so it stays the same object.
import numpy as np
from PIL import Image, ImageDraw
import props
from props import Prop, finish
from worldlib import blank, px

def _sample(base_name):
    f = props.PROPS[base_name](1000).im.convert("RGBA"); a = np.array(f)
    opaque = a[..., 3] > 0
    def med(y0, y1):
        reg = a[y0:y1][opaque[y0:y1]][:, :3]
        return tuple(int(v) for v in np.median(reg, axis=0)) if len(reg) else (90, 90, 90)
    h = f.height
    top = med(1, 4); face = med(h // 2 - 2, h // 2 + 4); dark = med(h - 4, h - 1)
    return top, face, dark

def _shade(c, k): return tuple(max(0, min(255, int(v * k))) for v in c) + (255,)

def _box(w, top_h, face_h, top, face, dark, over=0):
    """Oblique box at x2: top surface (w x top_h) over an end face (w x face_h). `over` = how far the top slab overhangs the face on each side."""
    W = w + 2 * over; im = Image.new("RGBA", (W, top_h + face_h), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W - 1, top_h - 1], fill=_shade(top, 1.0))                              # the top surface
    d.line([(0, 0), (W - 1, 0)], fill=_shade(top, 1.35)); d.line([(0, 0), (0, top_h - 1)], fill=_shade(top, 1.2))   # lit north and west edges
    d.line([(W - 1, 1), (W - 1, top_h - 1)], fill=_shade(top, 0.8))
    d.rectangle([over, top_h, over + w - 1, top_h + face_h - 1], fill=_shade(face, 1.0))        # the end face
    d.line([(over, top_h), (over + w - 1, top_h)], fill=_shade(top, 0.62))                      # the slab's front edge shadows the face
    d.line([(over, top_h + 1), (over, top_h + face_h - 1)], fill=_shade(face, 1.22))           # lit west edge
    d.line([(over + w - 1, top_h + 1), (over + w - 1, top_h + face_h - 1)], fill=_shade(face, 0.72))   # shaded east edge
    d.line([(over, top_h + face_h - 1), (over + w - 1, top_h + face_h - 1)], fill=_shade(dark, 0.6))  # the foot
    return im, d

def _lockers_e(seed):
    top, face, dark = _sample("lockers")
    im, d = _box(26, 16, 36, top, face, dark)
    W = im.width
    for y in (5, 10): d.line([(1, y), (W - 2, y)], fill=_shade(top, 0.78))                    # the seams between the three lockers' lids
    for yy in (4, 18, 30):                                                                    # stiffener ribs pressed into the end panel
        d.line([(3, 16 + yy), (W - 4, 16 + yy)], fill=_shade(face, 0.8)); d.line([(3, 17 + yy), (W - 4, 17 + yy)], fill=_shade(face, 1.15))
    for (x, y) in ((3, 19), (W - 4, 19), (3, 46), (W - 4, 46)): d.point((x, y), fill=_shade(face, 1.45))   # rivets
    d.rectangle([2, 49, 5, 51], fill=_shade(dark, 0.5)); d.rectangle([W - 6, 49, W - 3, 51], fill=_shade(dark, 0.5))   # feet
    p = finish(im, (W // 2 + 1, im.height), shadow=(13, 6, 1, -1), tall=True); p.scale = 2; return p

def _desk_e(seed):
    top, face, dark = _sample("desk")
    im, d = _box(20, 22, 12, top, face, dark, over=2)                                        # the slab overhangs the carcass by one map pixel each side
    W = im.width; paper = (200, 190, 160, 255)
    for x in range(3, W - 3, 3): d.line([(x, 1), (x, 21)], fill=_shade(top, 0.9))           # the grain of the planks, long way
    d.rectangle([6, 5, W - 7, 14], fill=_shade((70, 80, 70), 1.0)); d.line([(6, 5), (W - 7, 5)], fill=_shade((70, 80, 70), 1.4))   # blotter
    d.rectangle([8, 7, 13, 12], fill=paper); d.line([(8, 9), (13, 9)], fill=(150, 140, 120, 255)); d.line([(8, 11), (12, 11)], fill=(150, 140, 120, 255))   # papers
    d.line([(15, 17), (W - 6, 19)], fill=_shade(dark, 0.45))                                  # a pencil
    d.rectangle([4, 24, W - 5, 32], outline=_shade(face, 0.72), fill=_shade(face, 0.9))      # the end panel's frame
    d.line([(5, 25), (W - 6, 25)], fill=_shade(face, 1.2))
    d.rectangle([4, 33, 6, 33], fill=_shade(dark, 0.5)); d.rectangle([W - 7, 33, W - 5, 33], fill=_shade(dark, 0.5))
    p = finish(im, (W // 2 + 1, im.height), shadow=(12, 5, 1, -1), tall=False); p.scale = 2; return p

def _cabinet_e(seed):
    top, face, dark = _sample("cabinet")
    im, d = _box(22, 14, 38, top, face, dark)
    W = im.width
    d.rectangle([3, 3, W - 5, 10], fill=_shade(top, 0.86)); d.line([(3, 3), (W - 5, 3)], fill=_shade(top, 1.25))    # a stack of folders lying on the top
    d.line([(4, 6), (W - 6, 6)], fill=(205, 198, 170, 255)); d.line([(4, 8), (W - 6, 8)], fill=(190, 182, 156, 255))
    for yy in (14, 26, 38): d.line([(2, 14 + yy - 14), (W - 3, yy)], fill=_shade(face, 0.8))    # drawer seams carry round the end
    d.rectangle([W - 8, 22, W - 5, 24], fill=_shade(face, 1.3))                              # a label holder
    for (x, y) in ((3, 17), (W - 4, 17), (3, 48), (W - 4, 48)): d.point((x, y), fill=_shade(face, 1.45))
    d.rectangle([2, 50, 4, 51], fill=_shade(dark, 0.5)); d.rectangle([W - 5, 50, W - 3, 51], fill=_shade(dark, 0.5))
    p = finish(im, (W // 2 + 1, im.height), shadow=(11, 5, 1, -1), tall=True); p.scale = 2; return p

def _mirror_p(p):
    return Prop(p.im.transpose(Image.FLIP_LEFT_RIGHT), (p.im.width - 1 - p.pivot[0], p.pivot[1]), p.shadow, p.tall, scale=p.scale)

def install():
    import props_v2
    for base, fn in (("lockers", _lockers_e), ("desk", _desk_e), ("cabinet", _cabinet_e)):
        props.PROPS[f"{base}_fe"] = fn
        props.PROPS[f"{base}_fw"] = (lambda f: (lambda seed: _mirror_p(f(seed))))(fn)
        props.VARIANTS[f"{base}_fe"] = props.VARIANTS[f"{base}_fw"] = 1
