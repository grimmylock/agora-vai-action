"""Lever guns, the whole lever: the big loop with its thick steel root swings on an arc about its hinge (the hinge is where the loop meets the receiver).
lever = opaque pixels that are not stock wood, inside x <= XL and y >= YT, plus the ring round the loop's hole; body = the rest, with the stock's cut edge outlined.
Writes _body/_part (same canvas as _hd) and prints the HammerPivot hinge."""
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
D = 'Assets/Resources/Sprites/Weapons/'
# gun: seed in the loop hole, XL, YT, hinge (x, y), wood rule
G = {'FrontierLeverRifle': dict(seed=(78, 35), XL=88, YT=26, hinge=(87, 29), XM=69, GY=999, wood='brown'),
     'Nightwalker4570': dict(seed=(82, 40), XL=0, YT=999, hinge=(93, 31), XM=0, GY=0, wood='none'),
     'HaldenLeverShotgun': dict(seed=(80, 44), XL=92, YT=34, hinge=(90, 36), XM=69, GY=41, wood='red')}
def run(k, c):
    im = np.array(Image.open(D + f'weapon_{k}_hd.png').convert('RGBA')); a = im[..., 3] > 0; H, W = a.shape
    r, g, b = (im[..., i].astype(int) for i in range(3)); yy, xx = np.mgrid[0:H, 0:W]
    lab, _ = ndi.label(~a); hole = lab == lab[c['seed'][1], c['seed'][0]]
    near = a & ndi.binary_dilation(hole, ndi.generate_binary_structure(2, 1), iterations=3)
    wood = a & (r > b + 7) & (r + g + b > 50)
    near &= ~(wood & (yy < c['GY']))                      # the stock's own wood stays; only the lever's grip (below GY) comes along
    lever = (a & ~wood & (xx <= c['XL']) & (xx >= c['XM']) & (yy >= c['YT'])) | near
    body = im.copy(); body[lever] = 0
    gone = lever & ~a * 0 | lever
    # outline the stock where the lever was cut away from it
    edge = a & ~lever & ndi.binary_dilation(lever, ndi.generate_binary_structure(2, 1)) & wood
    body[edge] = [14, 14, 18, 255]
    part = np.zeros_like(im); part[lever] = im[lever]
    Image.fromarray(body).save(D + f'weapon_{k}_body.png'); Image.fromarray(part).save(D + f'weapon_{k}_part.png')
    print(k, 'lever px', int(lever.sum()), 'hinge', c['hinge'])
if False: pass
if __name__ == '__main__':
    for k, c in G.items(): run(k, c)
