# The small Mind Weaver (2026-11-20; Yan: the SpriteCook one is 9 tiles tall, too big for a Tier 3 mutant - "draw a small regular Tier 3 version by hand in the Gaunt's style", and "we need sprite variations").
# The design is make_concepts_v2.mind_weaver(), redrawn by running the same drawing code through a scaling layer (every shape, width, line and pixel scaled by SCALE) with three colour sets, so the shapes and the detail are placed by the same hand.
#   python3 ArtSource/make_mind_weaver_small.py  -> ArtSource/mutants_anim/concepts_new/mind_weaver_small_{a,b,c}.png + mind_weaver_small_sheet.png
import os, sys, re, inspect, math
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_concepts_v2 as v
import pxart
SCALE = 0.68
class SMask(pxart.Mask):
    def __init__(self, size): super().__init__(size)
    def ell(self, cx, cy, rx, ry, rot=0.0): return super().ell(cx * SCALE, cy * SCALE, rx * SCALE, ry * SCALE, rot)
    def poly(self, pts): return super().poly([(x * SCALE, y * SCALE) for x, y in pts])
    def tube(self, pts, widths): return super().tube([(x * SCALE, y * SCALE) for x, y in pts], [max(1.1, w * SCALE) for w in widths])
def spx(im, pts, col): pxart.px(im, [(int(round(x * SCALE)), int(round(y * SCALE))) for x, y in pts], col)
def sline(im, x0, y0, x1, y1, col): pxart.line(im, round(x0 * SCALE), round(y0 * SCALE), round(x1 * SCALE), round(y1 * SCALE), col)
SETS = {
 'a': {},   # the concept: grey skin, pink brain, violet thread
 'b': {'SKIN': 'ramp("#1a2220", "#303f3a", "#4f6359", "#7b9487", "#abc0b2")', 'BRAIN': 'ramp("#10261f", "#1f4a3c", "#2f7a60", "#52ad8a", "#9be6c4")', 'NERVE': 'ramp("#10304a", "#1f5f8f", "#3a95d0", "#7ac8f5", "#e0f6ff")'},    # the drowned one: sea-green skin, a green brain, cold blue thread
 'c': {'SKIN': 'ramp("#241a1a", "#3f2e2c", "#69504a", "#9a7c70", "#c8ac9c")', 'BRAIN': 'ramp("#2a0a0a", "#5a1414", "#8f2424", "#c24a3a", "#f0907a")', 'NERVE': 'ramp("#40201a", "#7a3a2a", "#c0613a", "#e8a070", "#fff0d8")'},    # the flayed one: raw-red brain, rust thread
}
def build(key):
    import importlib; importlib.reload(v)
    src = inspect.getsource(v.mind_weaver)
    src = src.replace('S = (116, 156)', 'S = (int(116 * %s) + 2, int(156 * %s) + 2)' % (SCALE, SCALE))
    for name, rep in SETS[key].items(): src = re.sub(r'(\n    %s = )ramp\([^\n]*\)' % name, lambda m: m.group(1) + rep, src, count=1)
    old = (v.Mask, v.px, v.line); v.Mask, v.px, v.line = SMask, spx, sline      # mk() (the part helper) lives in the module, so patch the module itself
    try:
        ns = v.__dict__; exec(src, ns); return ns['mind_weaver']()
    finally: v.Mask, v.px, v.line = old; exec(inspect.getsource(v.mind_weaver) if False else '', {})
if __name__ == '__main__':
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'concepts_new'); ims = {}
    for k in 'abc':
        ims[k] = build(k); ims[k].save(os.path.join(out, 'mind_weaver_small_%s.png' % k))
    K = 5; pad = 30; W = sum(i.width * K for i in ims.values()) + pad * 4; H = max(i.height for i in ims.values()) * K + pad * 2
    sh = Image.new('RGBA', (W, H), (24, 22, 28, 255)); x = pad
    for k, im in ims.items(): sh.alpha_composite(im.resize((im.width * K, im.height * K), Image.NEAREST), (x, H - im.height * K - pad)); x += im.width * K + pad
    sh.convert('RGB').save(os.path.join(out, 'mind_weaver_small_sheet.png')); print({k: i.size for k, i in ims.items()})
