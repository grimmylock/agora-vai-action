# Rot Crawler: erase the two extra raised limbs by hand (Yan, 2026-11-20: "rot crawler seems to have some extra limbs", circled the far hind leg and the far arm over the back) and re-draw the outline where they were.
# python3 ArtSource/fix_rot_crawler.py [preview]  -> ArtSource/mutants_anim/concepts_new/spritecook/rot_crawler_sc_v1_fixed.png
import sys, os
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
D = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mutants_anim', 'concepts_new', 'spritecook')
im = Image.open(os.path.join(D, 'rot_crawler_sc_v1.png')).convert('RGBA'); im = im.crop(im.getbbox())
LEFT = [(8, 4), (29, 4), (29, 19), (23, 19), (17, 23), (14, 34), (13, 46), (11, 47), (11, 63), (0, 63), (0, 40), (6, 26)]   # far hind leg: the arch over the haunch, the thin shin behind the thigh and its foot
RIGHT = [(82, 2), (97, 2), (98, 14), (94, 20), (88, 22), (84, 14)]                                                              # far arm: the arch over the shoulder
m = Image.new('L', im.size, 0); d = ImageDraw.Draw(m)
for poly in (LEFT, RIGHT): d.polygon(poly, fill=255)
mask = np.array(m) > 0
if len(sys.argv) > 1:
    ov = im.copy(); a = np.array(ov); a[mask] = (a[mask] * 0.4 + np.array([255, 0, 0, 255]) * 0.6).astype(np.uint8); ov = Image.fromarray(a)
    bg = Image.new('RGBA', ov.size, (40, 36, 44, 255)); bg.alpha_composite(ov); bg.resize((ov.width * 8, ov.height * 8), Image.NEAREST).save('/tmp/rot_mask.png'); sys.exit()
a = np.array(im); a[mask] = 0
alpha = a[:, :, 3] > 0
grown = ndi.binary_dilation(alpha, structure=np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]])) & ~alpha & ndi.binary_dilation(mask, iterations=2)   # new outline only where the cut was
a[grown] = (14, 14, 12, 255)
out = Image.fromarray(a); out = out.crop(out.getbbox()); out.save(os.path.join(D, 'rot_crawler_sc_v1_fixed.png')); print(out.size)
bg = Image.new('RGBA', out.size, (40, 36, 44, 255)); bg.alpha_composite(out); bg.resize((out.width * 8, out.height * 8), Image.NEAREST).save('/tmp/rot_fixed8.png')
