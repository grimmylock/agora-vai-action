"""After cut_parts.py: wood/stock pixels the lever rectangle caught go back to the body (the loop keeps only steel + its grip)."""
import numpy as np
from PIL import Image
D = 'Assets/Resources/Sprites/Weapons/'
for k, rule in (('FrontierLeverRifle', 'wood'), ('HaldenLeverShotgun', 'wood'), ('Nightwalker4570', 'grey')):
    b = np.array(Image.open(D + f'weapon_{k}_body.png').convert('RGBA')); p = np.array(Image.open(D + f'weapon_{k}_part.png').convert('RGBA'))
    a = p[..., 3] > 0; r, g, bl = (p[..., i].astype(int) for i in range(3)); m = (r + g + bl) / 3
    if rule == 'wood': stock = a & (r > bl + 14) & (m > 40)
    else: stock = a & (m > 48)
    if k == 'HaldenLeverShotgun': stock &= ~((r > bl + 14) & (np.arange(p.shape[1])[None, :] > 74) & (np.arange(p.shape[0])[:, None] > 41))  # the lever's wooden grip stays on the loop
    b[stock] = p[stock]; p[stock] = 0
    Image.fromarray(b).save(D + f'weapon_{k}_body.png'); Image.fromarray(p).save(D + f'weapon_{k}_part.png'); print(k, int(stock.sum()), 'px back to body')
