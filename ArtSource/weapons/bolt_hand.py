"""Bolt guns by hand. M91Trekhlineyka: the steel bolt strip + its upright handle slide back out of the receiver (the slot behind it is filled dark).
Sever98: the bolt handle (stem + knob) is its own part, swung up and back about its root (style "lever", negative degrees); the stock behind it is filled from its neighbours."""
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
D = 'Assets/Resources/Sprites/Weapons/'
def load(k): return np.array(Image.open(D + f'weapon_{k}_hd.png').convert('RGBA'))
def save(k, body, part):
    Image.fromarray(body).save(D + f'weapon_{k}_body.png'); Image.fromarray(part).save(D + f'weapon_{k}_part.png')
def mosin():
    k = 'M91Trekhlineyka'; im = load(k); a = im[..., 3] > 0; H, W = a.shape; yy, xx = np.mgrid[0:H, 0:W]
    r, g, b = (im[..., i].astype(int) for i in range(3)); wood = (r > b + 7) & (r + g + b > 50)
    m = a & ~wood & (((xx >= 69) & (xx <= 100) & (yy >= 12) & (yy <= 15)) | ((xx >= 82) & (xx <= 91) & (yy <= 11)))
    body = im.copy(); body[m] = 0
    slot = np.zeros_like(m); slot[12:16, 69:101] = True; body[slot] = [26, 28, 34, 255]       # the receiver's dark slot where the bolt was
    body[11, 82:92] = [14, 14, 18, 255]                                                       # the top rail the handle stood on
    part = np.zeros_like(im); part[m] = im[m]; save(k, body, part); print(k, int(m.sum()), 'px bolt')
def sever():
    k = 'Sever98'; im = load(k); a = im[..., 3] > 0; H, W = a.shape; yy, xx = np.mgrid[0:H, 0:W]
    m = a & (xx >= 88) & (xx <= 97) & (yy >= 12) & (yy <= 25)                                 # the handle: stem and knob under the receiver's rail
    body = im.copy(); body[m] = 0
    # fill the hole from the nearest kept pixel (stock behind it)
    idx = ndi.distance_transform_edt(m, return_distances=False, return_indices=True)
    keep = a & ~m; ys, xs = np.where(keep)
    d = np.zeros(m.shape, bool); d = m
    iy, ix = ndi.distance_transform_edt(~keep, return_distances=False, return_indices=True)
    fill = m & (yy >= 14); body[fill] = im[iy[fill], ix[fill]]
    part = np.zeros_like(im); part[m] = im[m]; save(k, body, part); print(k, int(m.sum()), 'px handle')
mosin(); sever()
