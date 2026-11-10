# Double-density (x2) props cut from imported CC0 art (ArtSource/external, see SOURCES.md) and recoloured toward the Zone.
# Each x2 sprite is 2x the pixel density of the old ones: scale=2 means one sprite pixel = half a map pixel.
# They are ADDED as extra variants of an existing family (p_tree_10.., p_redtree_6.. ...); the old variants stay.
import os, colorsys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
import props
from props import Prop

HERE = os.path.dirname(os.path.abspath(__file__))
EXT = os.path.join(HERE, "..", "external")

def _segments(path, min_px=200, dil=3):
    im = Image.open(path).convert("RGBA"); a = np.array(im)
    lab, n = ndi.label(ndi.binary_dilation(a[..., 3] > 0, iterations=dil))
    out = []
    for i, sl in enumerate(ndi.find_objects(lab)):
        if (a[sl][..., 3] > 0).sum() >= min_px:
            ys, xs = sl; out.append((i, im.crop((xs.start, ys.start, xs.stop, ys.stop))))
    return dict(out)

def recolour(im, sat=1.0, val=1.0, hue=None, hue_shift=0.0, osat=0.5, oval=0.6):
    """HSV recolour. Greens/yellow-greens (hue 45-175 deg): saturation x sat, value x val, hue set to `hue` (degrees) if given,
    else shifted by hue_shift. Everything else (trunks, roots, reds, oranges): saturation x osat, value x oval, hue shifted by hue_shift."""
    a = np.array(im.convert("RGBA")).astype(np.float32) / 255.0
    flat = a.reshape(-1, 4); res = np.empty_like(flat)
    for i, (r, g, b, al) in enumerate(flat):
        h, s, v = colorsys.rgb_to_hsv(r, g, b); deg = h * 360
        if 45 <= deg <= 205:
            if hue is not None: deg = hue + (deg - 110) * 0.15          # keep a little of the original hue spread (leaf shading)
            else: deg += hue_shift
            s = min(1.0, s * sat); v = min(1.0, v * val)
        else:
            deg += hue_shift + (14 if deg < 20 or deg > 330 else 0); s = min(1.0, s * osat); v = min(1.0, v * oval)
        r2, g2, b2 = colorsys.hsv_to_rgb((deg % 360) / 360.0, s, v); res[i] = (r2, g2, b2, al)
    return Image.fromarray((res.reshape(a.shape) * 255 + 0.5).astype(np.uint8), "RGBA")

def _trim(im, pad=1):
    bb = im.getbbox(); im = im.crop(bb)
    out = Image.new("RGBA", (im.width + 2 * pad, im.height + 2 * pad), (0, 0, 0, 0)); out.paste(im, (pad, pad)); return out

def _prop(im, shadow_w=None, tall=True):
    """Pivot = the foot of the trunk: bottom row's centre of mass, 3 sprite px up (the roots spread below it)."""
    im = _trim(im); a = np.array(im)[..., 3] > 0
    ys = np.where(a.any(axis=1))[0]; bottom = ys.max(); xs = np.where(a[max(0, bottom - 4):bottom + 1].any(axis=0))[0]
    px = int(round(xs.mean())); py = max(0, bottom - 3)
    sw = shadow_w or im.width // 2 * 0.9                      # shadow is in MAP pixels: half the sprite's width is the sprite in map px
    return Prop(im, (px, py), (int(sw), 5, 1, 0), tall, scale=2)

_WABSA = os.path.join(EXT, "to-be-altered", "wabsa_trees_sheet_1000x1056.png")
_cache = {}
def _tree_parts():
    if "w" not in _cache:
        seg = _segments(_WABSA) if os.path.exists(_WABSA) else {}
        _cache["w"] = seg
    return _cache["w"]

# (segment index in the Wabsa sheet, recolour) per family; recolours pull the fantasy brights toward olive/straw/rust
LIVE = [(7, dict(sat=0.55, val=0.62, hue=82)), (3, dict(sat=0.55, val=0.60, hue=78)), (6, dict(sat=0.50, val=0.62, hue=88)),
        (18, dict(sat=0.55, val=0.62, hue=76)), (22, dict(sat=0.52, val=0.60, hue=92)), (0, dict(sat=0.50, val=0.60, hue=86))]
RED = [(4, dict(osat=0.62, oval=0.66, hue_shift=6)), (29, dict(osat=0.58, oval=0.68, hue_shift=-4)), (15, dict(osat=0.58, oval=0.66, hue_shift=4)),
       (35, dict(osat=0.62, oval=0.66)), (23, dict(osat=0.60, oval=0.66, hue_shift=4))]
PINE = [(31, dict(sat=0.55, val=0.52, hue=130))]
DEAD = [(32, dict(osat=0.5, oval=0.78))]

def _builder(old, old_count, picks):
    def build(seed):
        v = (seed - 1000) // 77
        if v < old_count: return old(seed)
        idx, kw = picks[(v - old_count) % len(picks)]
        seg = _tree_parts().get(idx)
        return _prop(recolour(seg, **kw)) if seg is not None else old(seed)
    return build

def install():
    if not _tree_parts(): return
    for fam, picks in (("tree", LIVE), ("redtree", RED), ("pine", PINE), ("deadtree", DEAD)):
        old = props.PROPS[fam]; oc = props.VARIANTS.get(fam, 1)
        if getattr(old, "_ext", False): continue
        b = _builder(old, oc, picks); b._ext = True
        props.PROPS[fam] = b; props.VARIANTS[fam] = oc + len(picks)

install()
try:
    import furn_x2; furn_x2.install()      # x2 side views of lockers / desk / cabinet
except ImportError:
    pass
