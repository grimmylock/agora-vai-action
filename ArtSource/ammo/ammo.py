"""Ammunition sprites (2026-11-16, Yan: "add actual sprites for the ammo in the game, so we don't have the red and yellow pixels flying out of realistic looking guns").
Spent cases (thrown from the ejection port, dumped by a revolver, lying on the floor) and live rounds (carried in the hand on a tube/clip reload) per ammunition class, drawn in code at 4 texture px per world pixel,
base to the left, mouth/bullet to the right. Classes (AmmoType -> class is CombatFx.CaseKindOf): pistol, magnum, rifle, rifle_steel (lacquered steel: 5.45x39, 7.62x39), rifleL, heavy (12.7, .50), shell12.
Writes Assets/Resources/Sprites/Ammo/ammo_case_<kind>.png and ammo_round_<kind>.png.  Run: python3 ArtSource/ammo/ammo.py"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Sprites', 'Ammo')
OL = (46, 32, 16)
BRASS = [(255, 232, 160), (236, 200, 100), (208, 164, 66), (158, 116, 42), (104, 74, 28)]
STEEL = [(176, 170, 140), (146, 142, 114), (116, 114, 92), (84, 84, 68), (56, 56, 46)]       # lacquered steel case
COPPER = [(224, 160, 104), (196, 124, 72), (160, 96, 54), (118, 70, 40), (82, 50, 30)]
RED = [(236, 110, 96), (206, 62, 52), (170, 40, 36), (124, 28, 28), (84, 20, 20)]
LEAD = [(176, 184, 198), (146, 154, 170), (118, 126, 142), (88, 94, 108), (62, 66, 78)]
def shade(t, pal):                                                                           # a cylinder lit from above-left: t = 0 (top) .. 1 (bottom)
    return pal[0] if t < 0.16 else pal[1] if t < 0.38 else pal[2] if t < 0.66 else pal[3] if t < 0.88 else pal[4]
def draw(profile, W, H, spent, pals, mouth_dark=True):
    """profile: list of (x0, x1, half_height_start, half_height_end, palette index); one column band per entry."""
    a = np.zeros((H, W, 4), np.uint8); cy = H / 2.0
    for (x0, x1, h0, h1, pi) in profile:
        for x in range(x0, x1):
            hh = h0 + (h1 - h0) * (x - x0) / max(1, x1 - x0 - 1)
            for y in range(int(np.floor(cy - hh)), int(np.ceil(cy + hh))):
                if y < 0 or y >= H: continue
                t = (y + 0.5 - (cy - hh)) / (2 * hh)
                if 0 <= t <= 1: a[y, x] = (*shade(t, pals[pi]), 255)
    return a
def finish(a, name):
    m = a[..., 3] > 0; ring = ndi.binary_dilation(m, structure=np.ones((3, 3))) & ~m; pad = np.zeros((a.shape[0] + 2, a.shape[1] + 2, 4), np.uint8); pad[1:-1, 1:-1] = a
    mp = np.pad(m, 1); rp = np.pad(ring, 1); full = ndi.binary_dilation(mp, structure=np.ones((3, 3))) & ~mp; pad[full] = (*OL, 255)
    Image.fromarray(pad).save(os.path.join(OUT, name)); return pad.shape[1], pad.shape[0]
def case(kind):
    # (total length, case body half height, neck half height, neck start, shoulder length, rim half height, rimmed)
    spec = {'pistol': (22, 4.0, 4.0, 22, 0, 4.6, False), 'magnum': (26, 4.5, 4.5, 26, 0, 5.4, True), 'rifle': (30, 4.4, 2.8, 20, 6, 4.6, False), 'rifle_steel': (30, 4.4, 2.8, 20, 6, 4.6, False),
            'rifleL': (38, 5.2, 3.5, 25, 8, 5.4, False), 'heavy': (46, 6.6, 4.4, 30, 9, 7.2, False)}[kind]
    L, hb, hn, ns, sl, hr, rimmed = spec; pals = [STEEL if kind == 'rifle_steel' else BRASS]
    prof = [(0, 2, hr, hr, 0), (2, 4, hb - 0.4, hb - 0.4, 0), (4, ns, hb, hb, 0)]                    # rim, extractor groove, body
    if sl: prof.append((ns, ns + sl, hb, hn, 0))                                                      # shoulder
    prof.append((ns + sl, L, hn, hn, 0))                                                             # neck / the rest
    return prof, L, int(2 * hr) + 2, pals
def make_case(kind):
    prof, L, H, pals = case(kind); a = draw(prof, L, H, True, pals); cy = H // 2
    hn = prof[-1][2]
    for y in range(int(cy - hn + 1), int(cy + hn - 1)): a[y, L - 2:L] = (22, 16, 8, 255)             # the open mouth of a spent case
    a[int(cy) - 1:int(cy) + 1, 0:2] = (*pals[0][3], 255)                                              # primer pocket shading
    return finish(a, f'ammo_case_{kind}.png')
def make_round(kind):
    prof, L, H, pals = case(kind); tip = {'pistol': 9, 'magnum': 11, 'rifle': 12, 'rifle_steel': 12, 'rifleL': 15, 'heavy': 20}[kind]; hn = prof[-1][2]
    pal_b = COPPER if kind not in ('rifle_steel',) else LEAD
    ogive = [(L, L + tip // 2, hn, hn * 0.86, 1), (L + tip // 2, L + tip, hn * 0.86, 0.6, 1)] if True else []
    full = prof + [(x0, x1, h0, h1, 1) for (x0, x1, h0, h1, _) in ogive]; W = L + tip; a = draw(full, W, H, False, pals + [pal_b])
    return finish(a, f'ammo_round_{kind}.png')
def make_shell(live):
    W, Hh = 34, 13; a = np.zeros((Hh, W, 4), np.uint8); cy = Hh / 2.0
    for x in range(W):
        if x < 2: hh = 6.0
        elif x < 11: hh = 5.6                                                                         # the brass head
        else: hh = 5.3                                                                                # the plastic hull
        for y in range(int(np.floor(cy - hh)), int(np.ceil(cy + hh))):
            t = (y + 0.5 - (cy - hh)) / (2 * hh)
            if 0 <= t <= 1: a[y, x] = (*shade(t, BRASS if x < 11 else RED), 255)
    a[int(cy - 5):int(cy + 5), 11] = (*RED[3], 255)                                                    # the seam between head and hull
    if live: a[int(cy - 4):int(cy + 4), W - 3:W] = (*RED[3], 255); a[int(cy) - 1:int(cy) + 1, W - 3:W] = (*RED[0], 255)   # a crimped top
    else:
        for y in range(int(cy - 4), int(cy + 4)): a[y, W - 3:W] = (22, 14, 12, 255)                  # a split, burnt mouth
        a[int(cy) - 2:int(cy) + 2, W - 4] = (*RED[4], 255)
    return finish(a, f"ammo_{'round' if live else 'case'}_shell12.png")
if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for k in ('pistol', 'magnum', 'rifle', 'rifle_steel', 'rifleL', 'heavy'): print('case', k, make_case(k), 'round', make_round(k))
    print('shell12', make_shell(False), make_shell(True))
    names = [f'ammo_case_{k}.png' for k in ('pistol', 'magnum', 'rifle', 'rifle_steel', 'rifleL', 'heavy', 'shell12')] + [f'ammo_round_{k}.png' for k in ('pistol', 'magnum', 'rifle', 'rifle_steel', 'rifleL', 'heavy', 'shell12')]
    sheet = Image.new('RGB', (1000, 300), (84, 92, 80)); x = 8; y = 8
    for i, n in enumerate(names):
        im = Image.open(os.path.join(OUT, n)).convert('RGBA'); b = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
        if i == 7: x = 8; y = 150
        sheet.paste(b, (x, y), b); x += b.width + 10
    sheet.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ammo_preview.png'))
