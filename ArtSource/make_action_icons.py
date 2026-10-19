"""Batch 72: the action-bar icons (HudView.ActionBar / CombatActions), 16x16 in the locked agora-vai style:
flat 2-3 tones per material, 1px near-black outline #141617 added round every silhouette, shown at 2x in the HUD.

    python ArtSource/make_action_icons.py            # preview sheet + PNGs into ArtSource/_out/icons
    python ArtSource/make_action_icons.py --install  # also copies them to Assets/Resources/Sprites/icon_act_<name>.png
"""
import os, shutil, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '_out', 'icons')
DEST = os.path.join(HERE, '..', 'Assets', 'Resources', 'Sprites')


def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


PAL = {
    # UI cream / bone
    'W': hexc('e6dcc0'), 'w': hexc('b3a98f'), 'v': hexc('7d7a70'),
    # gunmetal ramp (weapon palette 1-5)
    '1': hexc('a9b0b5'), '2': hexc('727980'), '3': hexc('4c5258'), '4': hexc('33373b'),
    # olive / brass / copper
    'o': hexc('7a8256'), 'O': hexc('555c3a'), 'e': hexc('3c4128'),
    'Y': hexc('c9a63e'), 'y': hexc('f0d878'), 'c': hexc('a0602f'),
    # straw sandbags
    'S': hexc('a89a6a'), 's': hexc('c7b98a'), 'T': hexc('7c704b'),
    # accents (HUD colours)
    'R': hexc('c8443a'), 'r': hexc('e0675a'), 'Q': hexc('8a2a24'),
    'C': hexc('4fb6c9'), 'k': hexc('9fe0ec'), 'L': hexc('1c2a30'),
    'G': hexc('e8c547'), 'P': hexc('9d7bd8'), 'p': hexc('c4aef0'),
    # skin / leather
    'H': hexc('c8916a'), 'h': hexc('9a6a4c'), 'm': hexc('5a3f2b'), 'n': hexc('7d5a3e'),
}
OUTLINE = (20, 22, 23, 255)

ICONS = {
    # crosshair, red centre
    'fire': [
        '................',
        '.......WW.......',
        '.....WWWWWW.....',
        '....W..WW..W....',
        '...W...WW...W...',
        '..W....WW....W..',
        '..W..........W..',
        '.WWWWW.RR.WWWWW.',
        '.WWWWW.RR.WWWWW.',
        '..W..........W..',
        '..W....WW....W..',
        '...W...WW...W...',
        '....W..WW..W....',
        '.....WWWWWW.....',
        '.......WW.......',
        '................',
    ],
    # rifle scope, side view, cyan lenses
    'aim': [
        '................',
        '................',
        '................',
        '.....3333.......',
        '.....2222.......',
        '.111111111111...',
        'kC11111111111Ck.',
        'kC22222222222Ck.',
        'LC33333333333CL.',
        '.444444444444...',
        '.....3443.......',
        '.....4..4.......',
        '................',
        '................',
        '................',
        '................',
    ],
    # head-and-shoulders target, red mark on the head
    'part': [
        '................',
        '......wwww......',
        '.....wWWWWw.....',
        '.....WWRRWW.....',
        '.....WWRRWW.....',
        '.....wWWWWw.....',
        '......wwww......',
        '.......ww.......',
        '...wwWWWWWWww...',
        '..wWWWWWWWWWWw..',
        '..WWWWWWWWWWWW..',
        '..WWWWWWWWWWWW..',
        '..WWWWWWWWWWWW..',
        '..wWWWWWWWWWWw..',
        '..wwwwwwwwwwww..',
        '................',
    ],
    # three rounds side by side (burst / single)
    'mode': [
        '................',
        '..c....c....c...',
        '.ccc..ccc..ccc..',
        '.ccc..ccc..ccc..',
        '.yYY..yYY..yYY..',
        '.yYY..yYY..yYY..',
        '.yYY..yYY..yYY..',
        '.yYY..yYY..yYY..',
        '.yYY..yYY..yYY..',
        '.yYY..yYY..yYY..',
        '.yYY..yYY..yYY..',
        '.YYY..YYY..YYY..',
        '.ccc..ccc..ccc..',
        '................',
        '................',
        '................',
    ],
    # curved box magazine, a round on top
    'reload': [
        '................',
        '......cyy.......',
        '.....1YYYY......',
        '.....122223.....',
        '.....122223.....',
        '.....142243.....',
        '.....122223.....',
        '.....142243.....',
        '......122223....',
        '......142243....',
        '......122223....',
        '.......122223...',
        '.......444444...',
        '.......444444...',
        '................',
        '................',
    ],
    # a plated shield
    'cover': [
        '................',
        '..111111111111..',
        '..1222222222233.',
        '..1222WWWW22233.',
        '..122WWWWWW2233.',
        '..122WWWWWW2233.',
        '..1222WWWW22233.',
        '..1222222222233.',
        '...12222222233..',
        '...12222222233..',
        '....122222233...',
        '.....1222233....',
        '......12233.....',
        '.......133......',
        '................',
        '................',
    ],
    # a helmet ducked behind sandbags
    'hunker': [
        '................',
        '................',
        '.....oooooo.....',
        '....ooooooOO....',
        '...oooooooOOO...',
        '..eeeeeeeeeeee..',
        '................',
        '.sssSS.sssSSSs..',
        '.SSSSST.SSSSST..',
        '.TTTTT.TTTTTTT..',
        '..sssSS.ssSSs...',
        '.SSSSSST.SSSSST.',
        '.TTTTTTT.TTTTTT.',
        '................',
        '................',
        '................',
    ],
    # a watching eye with a yellow pupil
    'overwatch': [
        '................',
        '................',
        '................',
        '......WWWW......',
        '....WWWWWWWW....',
        '...WWW.GG.WWW...',
        '..WWW.GGGG.WWW..',
        '.WWWW.GLLG.WWWW.',
        '.WWWW.GLLG.WWWW.',
        '..WWW.GGGG.WWW..',
        '...WWW.GG.WWW...',
        '....WWWWWWWW....',
        '......WWWW......',
        '................',
        '................',
        '................',
    ],
    # zig-zag dodge arrow
    'evasive': [
        '................',
        '................',
        '.....kkkkk......',
        '...kkCCCCCkk....',
        '..kCC.....CCk...',
        '..CC.......CC...',
        '.kC.........C...',
        '.CC.......CCCCC.',
        '.CC........CCC..',
        '.CC.........C...',
        '................',
        '.vvv....vvv.....',
        '................',
        '................',
        '................',
        '................',
    ],
    # round grenade, lever and pin ring
    'grenade': [
        '................',
        '..........11....',
        '.........1..1...',
        '.....2221.11....',
        '.....12223......',
        '.....3333.......',
        '....oooooO......',
        '...ooeoooeOO....',
        '..oooooooooOO...',
        '..ooeoooeooOe...',
        '..oooooooooOe...',
        '..ooeoooeoOOe...',
        '...ooooooOOe....',
        '....OOOOOee.....',
        '................',
        '................',
    ],
    # a shout: speech bubble with a red "!"
    'intimidate': [
        '................',
        '..wWWWWWWWWWWw..',
        '.wWWWWWRRWWWWWw.',
        '.WWWWWWRRWWWWWW.',
        '.WWWWWWRRWWWWWW.',
        '.WWWWWWRRWWWWWW.',
        '.WWWWWWRRWWWWWW.',
        '.WWWWWWWWWWWWWW.',
        '.wWWWWWRRWWWWWw.',
        '..wWWWWWWWWWWw..',
        '....WWww........',
        '....WWw.........',
        '....Ww..........',
        '....w...........',
        '................',
        '................',
    ],
    # tracer lines hosing a spot
    'suppress': [
        '................',
        '................',
        '..YYYYYYYyy.....',
        '................',
        '....YYYYYYYYyy..',
        '................',
        '.YYYYYYYYYYYYyy.',
        '................',
        '....YYYYYYYYyy..',
        '................',
        '..YYYYYYYyy.....',
        '................',
        '................',
        '................',
        '................',
        '................',
    ],
    # a running boot with speed lines
    'rungun': [
        '................',
        '................',
        '........nnnn....',
        '........nmmn....',
        '........nmmn....',
        '..vvv...nmmn....',
        '........nmmnn...',
        '.vvvv...nmmmnn..',
        '.......nmmmmmnn.',
        '..vvv..nmmmmmmn.',
        '.......44444444.',
        '................',
        '................',
        '................',
        '................',
        '................',
    ],
    # two footprints
    'sneak': [
        '................',
        '.........ww.....',
        '........wwww....',
        '........wwww....',
        '..ww....wwww....',
        '.wwww....ww.....',
        '.wwww...........',
        '.wwww....ww.....',
        '..ww.....ww.....',
        '................',
        '..ww............',
        '..ww............',
        '................',
        '................',
        '................',
        '................',
    ],
    # one big cartridge
    'ammo': [
        '................',
        '.......cc.......',
        '......cccc......',
        '......cccc......',
        '.....cccccc.....',
        '.....yYYYYY.....',
        '.....yYYYYY.....',
        '.....yYYYYY.....',
        '.....yYYYYY.....',
        '.....yYYYYY.....',
        '.....yYYYYY.....',
        '.....yYYYYY.....',
        '.....YYYYYY.....',
        '....YccccccY....',
        '................',
        '................',
    ],
    # an open hand
    'interact': [
        '................',
        '.....H.H.H......',
        '.....H.H.H.H....',
        '.....H.H.H.H....',
        '.....HHHHH.H....',
        '.H...HHHHHHH....',
        '.HH..HHHHHHH....',
        '..HH.HHHHHHh....',
        '...HHHHHHHHh....',
        '....HHHHHHh.....',
        '.....HHHHHh.....',
        '......hhhh......',
        '................',
        '................',
        '................',
        '................',
    ],
    # magnifying glass
    'search': [
        '................',
        '....1111........',
        '...1kkCC2.......',
        '..1kCCCCC2......',
        '..1CCCCCC2......',
        '..1CCCCCL2......',
        '..2CCCCLL3......',
        '...2CCLL3.......',
        '....2233nn......',
        '.........nn.....',
        '.........mnn....',
        '..........mnn...',
        '...........mm...',
        '................',
        '................',
        '................',
    ],
    # Zz
    'rest': [
        '................',
        '..PPPPPPP.......',
        '..pppppPP.......',
        '......PP........',
        '.....PP.........',
        '....PP..........',
        '...PP...........',
        '..PPPPPPP.......',
        '..ppppppp.PPPPP.',
        '..........pppPP.',
        '............PP..',
        '...........PP...',
        '..........PPPPP.',
        '..........ppppp.',
        '................',
        '................',
    ],
    # Batch 80: swap weapon - a pistol over a rifle, cream swap arrows
    'swap': [
        '................',
        '..W.............',
        '.WWW..2222222...',
        'W.W...1111111...',
        '..W...33.4......',
        '..W...4.........',
        '..W.............',
        '..W.........W...',
        '..W.........W...',
        '............W...',
        '.11111111111W...',
        '.222222222222...',
        '..33.43...W.W.W.',
        '..4...4....WWW..',
        '............W...',
        '................',
    ],
    # Batch 92: flashlight - a gunmetal torch, a yellow beam fanning right
    'light': [
        '................',
        '..........y.....',
        '.........yy...y.',
        '........yyy.yy..',
        '..................'[:16],
        '.3333222Gyyyyyyy',
        '.4322111GGyyyyyy',
        '.4322111GGyyyyyy',
        '.3333222Gyyyyyyy',
        '................',
        '........yyy.yy..',
        '.........yy...y.',
        '..........y.....',
        '................',
        '................',
        '................',
    ],
    # Batch 103: shove - a desk tipped on its side, legs out, an arrow pushing it
    'shove': [
        '................',
        '................',
        '..........cm....',
        '.....nnnnncm....',
        '.....n....cm....',
        '..........cm....',
        '...W......cm....',
        'WWWWW.....cm....',
        '...W......cm....',
        '..........cm....',
        '..........cm....',
        '.....n....cm....',
        '.....nnnnncm....',
        '..........cm....',
        '................',
        '................',
    ],
}


def grid_to_img(rows):
    assert len(rows) == 16, len(rows)
    img = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        assert len(row) == 16, (y, row)
        for x, ch in enumerate(row):
            if ch != '.':
                img.putpixel((x, y), PAL[ch])
    return img


def outline(im, col=OUTLINE):
    out = im.copy()
    a = im.load()
    o = out.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            if not a[x, y][3] and any(0 <= x + dx < w and 0 <= y + dy < h and a[x + dx, y + dy][3]
                                      for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = col
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    icons = {}
    for name, rows in ICONS.items():
        im = outline(grid_to_img(rows))
        icons[name] = im
        im.save(os.path.join(OUT, f'icon_act_{name}.png'))

    # preview: each icon as the HUD shows it (2x in a 46px button), plus 6x
    n = len(icons)
    cell = 120
    sheet = Image.new('RGBA', (cell * 6, ((n + 5) // 6) * (cell + 20)), hexc('1b1d1f'))
    d = ImageDraw.Draw(sheet)
    for i, (name, im) in enumerate(icons.items()):
        cx, cy = (i % 6) * cell, (i // 6) * (cell + 20)
        big = im.resize((96, 96), Image.NEAREST)
        sheet.paste(big, (cx + 4, cy + 4), big)
        d.rectangle([cx + 72, cy + 70, cx + 72 + 45, cy + 70 + 45], fill=hexc('3a3e41'))
        d.rectangle([cx + 74, cy + 72, cx + 72 + 43, cy + 70 + 43], fill=hexc('15171a'))
        x2 = im.resize((32, 32), Image.NEAREST)
        sheet.paste(x2, (cx + 79, cy + 76), x2)
        d.text((cx + 4, cy + cell + 2), name, fill=hexc('b8bcc0'))
    sheet.save(os.path.join(OUT, 'preview_action_icons.png'))

    if '--install' in sys.argv:
        for name in icons:
            shutil.copy(os.path.join(OUT, f'icon_act_{name}.png'), os.path.join(DEST, f'icon_act_{name}.png'))
        print('installed', len(icons), 'icons to', os.path.normpath(DEST))


if __name__ == '__main__':
    main()
