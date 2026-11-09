"""Shared by eel_art.py and eel_fight.py: the eel's strips (ArtSource/bosses/eel_anim.py -> Assets/Resources/Sprites/Fx/boss_eel_*.png + boss_eel_anims.json)
composited on the real Depths renders (Tools/render/render.sh sub_station_depths_1 sub_station_depths_3) at 2x, so one tile = 32 px like the strips."""
import os, json, subprocess, tempfile, math
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')
R = lambda *p: os.path.join(ROOT, *p)
T = 32
FXD = R('Assets/Resources/Sprites/Fx')
ANIMS = json.load(open(os.path.join(FXD, 'boss_eel_anims.json')))
_strips = {}
def strip(name):
    if name not in _strips: _strips[name] = Image.open(os.path.join(FXD, f'boss_eel_{name}.png')).convert('RGBA')
    return _strips[name]
def frame(name, i, dmg=0):
    """frame i of animation `name`, with the damage overlay of stage `dmg` (0 = clean) on top; returns (image, anchor)"""
    a = ANIMS[name]; w, h = a['frame']; i = max(0, min(a['frames'] - 1, int(i)))
    im = strip(name).crop((i * w, 0, i * w + w, h)).copy()
    if dmg: im.alpha_composite(strip(f'{name}_d{dmg}').crop((i * w, 0, i * w + w, h)))
    return im, tuple(a['anchor'])
def put(canvas, im, anchor, at, flip=False, alpha=1.0, mask=None):
    """put a frame so its anchor lands on pixel `at` (flip = the eel faces right: the strips face left as drawn)"""
    ax, ay = anchor
    if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT); ax = im.width - 1 - ax
    if alpha < 1: im = im.copy(); im.putalpha(im.getchannel('A').point(lambda a: int(a * alpha)))
    if mask is not None:                      # only where the mask (same size as the canvas) is on
        layer = Image.new('RGBA', canvas.size, (0, 0, 0, 0)); layer.alpha_composite(im, (int(at[0] - ax), int(at[1] - ay)))
        layer.putalpha(Image.composite(layer.getchannel('A'), Image.new('L', canvas.size, 0), mask)); canvas.alpha_composite(layer)
    else: canvas.alpha_composite(im, (int(at[0] - ax), int(at[1] - ay)))
def render(zone):
    im = Image.open(R(f'Tools/render/_render/{zone}.png')).convert('RGBA'); return im.resize((im.width * 2, im.height * 2), Image.NEAREST)
def plan(zone): return open(R(f'ArtSource/plans/{zone}.txt')).read().split('\n')
def tile_mask(rows, size, pred):
    m = Image.new('L', size, 0); d = ImageDraw.Draw(m)
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            if pred(c): d.rectangle((x * T, y * T, x * T + T - 1, y * T + T - 1), fill=255)
    return m
def player():
    i = int(os.environ.get('PLAYER_ARMOR', 0))          # armour atlas index (SpriteAtlasIndex.Armor order); default 0 = the first suit
    return Image.open(R('Assets/Resources/Sprites/armor_atlas.png')).convert('RGBA').crop((i % 16 * 16, i // 16 * 16, i % 16 * 16 + 16, i // 16 * 16 + 16)).resize((32, 32), Image.NEAREST)
def font(sz): return ImageFont.load_default(size=sz)
def dead_body(armor_idx):
    """the swallowed body: a human in that armour, lying down and darkened (the game's corpse is a ragdoll; this is the stand-in)"""
    at = Image.open(R('Assets/Resources/Sprites/armor_atlas.png')).convert('RGBA'); c = armor_idx % 16, armor_idx // 16
    s = at.crop((c[0] * 16, c[1] * 16, c[0] * 16 + 16, c[1] * 16 + 16)).resize((32, 32), Image.NEAREST).rotate(80, expand=True)
    px = s.load()
    for y in range(s.height):
        for x in range(s.width):
            r, g, b, a = px[x, y]; px[x, y] = (int(r * .55), int(g * .5), int(b * .5), a)
    return s
def banner(c, text, y, fill=(255, 235, 200), size=22, bg=(0, 0, 0, 175), x=None):
    d = ImageDraw.Draw(c, 'RGBA'); f = font(size); w = d.textlength(text, font=f)
    x = (c.width - w) / 2 if x is None else x; d.rectangle((x - 8, y - 3, x + w + 8, y + size + 5), fill=bg); d.text((x, y), text, font=f, fill=fill)
def video(tmp, out, fps=15):
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-framerate', str(fps), '-i', os.path.join(tmp, '%05d.png'), '-c:v', 'libx264', '-pix_fmt', 'yuv420p',
                    '-crf', '19', '-movflags', '+faststart', out], check=True)
def zap_line(d, a, b, seed, width=3):
    """a jagged electric line from a to b"""
    import random; rnd = random.Random(seed); n = 7; pts = [a]
    for i in range(1, n):
        t = i / n; pts.append((a[0] + (b[0] - a[0]) * t + rnd.uniform(-9, 9), a[1] + (b[1] - a[1]) * t + rnd.uniform(-9, 9)))
    pts.append(b); d.line(pts, fill=(120, 200, 255, 120), width=width + 4); d.line(pts, fill=(235, 250, 255, 255), width=width)
