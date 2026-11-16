#!/usr/bin/env python3
"""Stills of Tier 2 beat 1 "Taken" (2026-11-16): the real map renders (Tools/render/render.sh taken_tunnels scrapyard) with the game's own armour, weapon and mutant sprites placed on them, in the beat's four moments.
NOT Unity captures: no lighting, shadows, camera or UI; the dialogue box and the alarm tint are drawn here to show the moment. Units stand where Tier2Story puts them (soldiers behind you, the horde at the corridor's east end).
    python3 Tools/render/taken_stills.py outdir"""
import sys, os, math, random
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import squad_scene as S
SP = S.SP
def mapcrop(zone, cx, cy, tw, th):                        # tile centre -> a 32 px/tile crop (the render is 16 px a tile, doubled like the game's view)
    m = Image.open(os.path.join(HERE, '_render', zone + '.png')).convert('RGBA'); x0, y0 = int((cx - tw / 2) * 16), int((cy - th / 2) * 16)
    return m.crop((x0, y0, x0 + tw * 16, y0 + th * 16)).resize((tw * 32, th * 32), Image.NEAREST)
def tile(scene, tw, th, cx, cy, x, y): return (int((x - (cx - tw / 2)) * 32 + 16), int((y - (cy - th / 2)) * 32 + 30))    # feet position of a unit standing on tile (x, y)
def person(scene, foot, armor, weapon=None, cat='assault', ang=0, walk=None, kneel=False, shoot=False):
    S.shadow(scene, foot)
    if weapon: S.soldier(scene, foot, armor, weapon, cat, ang, walk, shoot=shoot)
    else:
        atlas = 'armor_pose_kneel' if kneel else 'armor_atlas'; body = S.img(atlas + '_x2.png').crop((armor % 16 * 32, armor // 16 * 32, armor % 16 * 32 + 32, armor // 16 * 32 + 32))
        scene.alpha_composite(body, (int(foot[0] - 16), int(foot[1] - 30)))
def mutant(scene, foot, name, flip=False):
    im = S.img('Mutants/mutant_hd_%s.png' % name); im = im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im
    S.shadow(scene, foot); scene.alpha_composite(im, (int(foot[0] - im.width / 2), int(foot[1] - im.height + 16)))
def rat(scene, foot, flip=False):
    a = S.img('mutant_atlas.png'); c = a.crop((2 * 16, 0, 3 * 16, 16)).resize((32, 32), Image.NEAREST); c = c.transpose(Image.FLIP_LEFT_RIGHT) if flip else c
    scene.alpha_composite(c, (int(foot[0] - 16), int(foot[1] - 28)))
def font(n):
    for p in ('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', '/usr/share/fonts/dejavu/DejaVuSans.ttf'):
        if os.path.exists(p): return ImageFont.truetype(p, n)
    return ImageFont.load_default()
def caption(scene, text):
    d = ImageDraw.Draw(scene, 'RGBA'); f = font(15); w = scene.width
    d.rectangle((0, scene.height - 26, w, scene.height), fill=(0, 0, 0, 170)); d.text((8, scene.height - 22), text, fill=(235, 225, 200, 255), font=f)
def dim(scene, k):                                         # a dark, low-lit feel (flat multiply: the game's lighting is not modelled)
    sh = Image.new('RGBA', scene.size, (8, 10, 14, int(255 * k))); scene.alpha_composite(sh)
def dialogue(scene, who, lines, options):
    d = ImageDraw.Draw(scene, 'RGBA'); w, h = scene.size; bh = 150
    d.rectangle((24, h - bh - 40, w - 24, h - 36), fill=(14, 14, 18, 225), outline=(150, 130, 90, 255), width=2)
    d.text((40, h - bh - 30), who, fill=(224, 192, 96, 255), font=font(18)); y = h - bh - 4
    import textwrap
    for ln in [l2 for l in lines for l2 in textwrap.wrap(l, 78)]:
        d.text((40, y), ln, fill=(235, 225, 200, 255), font=font(15)); y += 20
    y += 4
    for i, o in enumerate(options): d.text((40, y), '%d. %s' % (i + 1, o), fill=(180, 200, 160, 255), font=font(15)); y += 20
def main(out):
    os.makedirs(out, exist_ok=True)
    # 1. the interrogation: the Loading Dock, the hatch open in the floor, the squad round the player
    tw, th, cx, cy = 24, 19, 40, 164; sc = mapcrop('scrapyard', cx, cy, tw, th)
    person(sc, tile(sc, tw, th, cx, cy, 40, 164), 45, kneel=True)
    for i, (x, y, a, w, c, ang) in enumerate([(37, 161, 63, 'BR86Bulldog', 'assault', 0), (43, 161, 64, 'Corvan57', 'assault', 180), (38, 165, 63, 'BayouPump', 'shotgun', 40), (42, 165, 63, 'BlackHarrier', 'smg', 140), (40, 159, 64, 'Corvan57', 'assault', -90)]):
        person(sc, tile(sc, tw, th, cx, cy, x, y), a, w, c, ang)
    dialogue(sc, 'Garrison Sergeant', ['"You came up this road with something that points at a place that is not on our maps. So. What is down there?"'], ['I don\'t know.', 'A lab. Old research, I think.', 'Nothing you\'d want.'])
    caption(sc, 'Interrogation at the Loading Dock (dialogue box drawn here, not the real UI)'); sc.convert('RGB').save(os.path.join(out, 'taken_1_interrogation.png'))
    plan = [l.strip().strip('",') for l in open(os.path.join(HERE, '..', '..', 'Assets/Scripts/GameCore/TierDungeons.TakenPlan.cs')) if l.strip().startswith('"') and len(l.strip()) > 50]
    floor = lambda x, y: plan[y][x] in '.,EFB'
    # 2. down the tunnels: the ladder in the south-east cave, the squad behind you
    tw, th, cx, cy = 22, 12, 70, 50; sc = mapcrop('taken_tunnels', cx, cy, tw, th)
    person(sc, tile(sc, tw, th, cx, cy, 70, 49), 45)
    for i, (x, y, a, w, c, ang) in enumerate([(73, 52, 63, 'BR86Bulldog', 'assault', 150), (74, 51, 64, 'Corvan57', 'assault', 160), (73, 50, 63, 'BayouPump', 'shotgun', 170), (74, 53, 63, 'BlackHarrier', 'smg', 150)]):
        person(sc, tile(sc, tw, th, cx, cy, x, y), a, w, c, ang, 'a' if i % 2 else None)
    dim(sc, 0.38); caption(sc, 'Down the hatch: the cave under the dock, the squad behind the player'); sc.convert('RGB').save(os.path.join(out, 'taken_2_going_down.png'))
    # 3. the door: the 6-wide corridor, the badge-reader door in the north wall, the alarm
    tw, th, cx, cy = 24, 10, 40, 8; sc = mapcrop('taken_tunnels', cx, cy, tw, th)
    person(sc, tile(sc, tw, th, cx, cy, 40, 8), 45)
    for i, (x, y, a, w, c, ang) in enumerate([(37, 8, 63, 'BR86Bulldog', 'assault', 0), (35, 9, 64, 'Corvan57', 'assault', 10), (34, 7, 63, 'BayouPump', 'shotgun', 0), (33, 10, 63, 'BlackHarrier', 'smg', 10), (31, 8, 63, 'BR86Bulldog', 'assault', 0)]):
        person(sc, tile(sc, tw, th, cx, cy, x, y), a, w, c, ang)
    dim(sc, 0.3); red = Image.new('RGBA', sc.size, (190, 20, 14, 70)); sc.alpha_composite(red)
    caption(sc, 'The lab door (badge reader) in the corridor wall: the card fails, the alarm (tint drawn here)'); sc.convert('RGB').save(os.path.join(out, 'taken_3_the_door.png'))
    # 4. the horde against the soldiers: the broken wall at the corridor's east end, the dark tunnel behind it
    tw, th, cx, cy = 30, 14, 78, 14; sc = mapcrop('taken_tunnels', cx, cy, tw, th); rnd = random.Random(4)
    for i, (x, y, a, w, c, ang) in enumerate([(70, 7, 63, 'BR86Bulldog', 'assault', 0), (69, 9, 64, 'Corvan57', 'assault', 0), (71, 10, 63, 'BayouPump', 'shotgun', 0), (68, 8, 63, 'BlackHarrier', 'smg', 0)]):
        person(sc, tile(sc, tw, th, cx, cy, x, y), a, w, c, ang, shoot=True)
    spots = [(x, y) for x in range(72, 92) for y in range(6, 26) if floor(x, y)]; rnd.shuffle(spots)
    big = [(n, spots.pop()) for n in ['pallid_prowler', 'blind_maw'] * 4]
    for n, (x, y) in sorted(big, key=lambda t: t[1][1]): mutant(sc, tile(sc, tw, th, cx, cy, x, y), n, flip=True)
    for (x, y) in sorted(spots[:40], key=lambda t: t[1]): rat(sc, tile(sc, tw, th, cx, cy, x + rnd.random() * .6, y), flip=True)
    dim(sc, 0.3); caption(sc, 'The broken wall at the corridor\'s end: the horde (8 big + 40 rats shown) against the soldiers'); sc.convert('RGB').save(os.path.join(out, 'taken_4_horde_vs_soldiers.png'))
if __name__ == '__main__': main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..', '..', 'maps', 'renders', 'taken'))
