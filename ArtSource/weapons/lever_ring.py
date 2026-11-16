"""Lever loops, exact: the loop is the opaque ring round the enclosed see-through hole of the big loop (the trigger guard's hole is a different one).
body = the picture with the ring removed except its top edge (that stays as the frame's underside), part = the ring. Writes _body/_part for each lever gun."""
import numpy as np, sys
from PIL import Image
from scipy import ndimage as ndi
D = 'Assets/Resources/Sprites/Weapons/'
# gun: a point inside the big loop's hole, thickness of the ring (px)
G = {'FrontierLeverRifle': ((78, 35), 2), 'HaldenLeverShotgun': ((80, 44), 2), 'Nightwalker4570': ((82, 40), 2)}
for k, (seed, th) in G.items():
    im = np.array(Image.open(D + f'weapon_{k}_hd.png').convert('RGBA')); a = im[..., 3] > 0
    lab, n = ndi.label(~a); hole = lab == lab[seed[1], seed[0]]
    assert lab[seed[1], seed[0]] != 0 and not hole[0, :].any() and not hole[:, 0].any(), k + ': seed not in an enclosed hole'
    ring = a & ndi.binary_dilation(hole, structure=ndi.generate_binary_structure(2, 1), iterations=th)
    top = np.where(hole.any(1))[0].min()
    keep = ring & (np.arange(im.shape[0])[:, None] <= top + 1)       # the ring's top edge stays on the body too
    body = im.copy(); body[ring & ~keep] = 0
    part = np.zeros_like(im); part[ring] = im[ring]
    Image.fromarray(body).save(D + f'weapon_{k}_body.png'); Image.fromarray(part).save(D + f'weapon_{k}_part.png')
    ys, xs = np.where(ring); print(k, 'hole', int(hole.sum()), 'px, ring', int(ring.sum()), 'px, box', xs.min(), ys.min(), xs.max(), ys.max())
