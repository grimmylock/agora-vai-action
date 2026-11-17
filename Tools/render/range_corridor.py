#!/usr/bin/env python3
"""What 6 and 15 tiles look like (2026-11-17, Yan: "take the corridor from the gaunt cave map and place a character, another at 6 tiles and another at 15"): the real map render of the Hatch Tunnels
(Tools/render/render.sh taken_tunnels), the long corridor with the red lights, with the game's own body and weapon sprites standing where units stand. NOT a Unity capture: no lighting, no camera lean, no UI.
    python3 Tools/render/range_corridor.py outdir"""
import sys, os
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import squad_scene as S
import taken_stills as T
Y = 8                                                     # a row in the middle of the corridor (it is open from y 6 to 11, x 35 to 80)
def scene(x0, you_x, tw, th, scale, marks, title, note):
    cx, cy = you_x + (tw / 2 - 3), Y + 0.5                # framed so you stand 3 tiles from the left edge
    m = Image.open(os.path.join(HERE, '_render', 'taken_tunnels.png')).convert('RGBA'); px0, py0 = int((cx - tw / 2) * 16), int((cy - th / 2) * 16)
    sc = m.crop((px0, py0, px0 + tw * 16, py0 + th * 16)).resize((tw * 32, th * 32), Image.NEAREST)
    foot = lambda x: (int((x - (cx - tw / 2)) * 32 + 16), int((Y - (cy - th / 2)) * 32 + 30))
    d = ImageDraw.Draw(sc, 'RGBA')
    for t, col, lab in marks:                             # ground ticks and numbers
        fx, fy = foot(you_x + t); d.line([(fx, fy - 40), (fx, fy + 14)], fill=col, width=2); d.text((fx - 8, fy + 16), str(t), fill=col, font=T.font(15))
    T.person(sc, foot(you_x), 30, 'VK74', 'assault', 0)                                  # you, rifle up, facing right
    for t in (6, 15): T.person(sc, foot(you_x + t), 0, 'VK74', 'assault', 180)           # two raiders facing you
    sc = sc.resize((int(tw * 32 * scale), int(th * 32 * scale)), Image.NEAREST)
    out = Image.new('RGBA', (sc.width, sc.height + 64), (24, 26, 28, 255)); out.alpha_composite(sc, (0, 0)); dd = ImageDraw.Draw(out)
    dd.text((12, sc.height + 6), title, font=T.font(20), fill=(235, 235, 225)); dd.text((12, sc.height + 36), note, font=T.font(15), fill=(170, 170, 160))
    return out
def main(out):
    os.makedirs(out, exist_ok=True)
    # the game's default zoom: 7 tiles half-height (14 tall, 25 wide at 16:9)
    a = scene(0, 50, 25, 14, 1.0, [(5, (240, 220, 120, 255), ''), (6, (255, 255, 255, 255), ''), (10, (240, 170, 90, 255), ''), (15, (230, 90, 70, 255), '')],
              'Default camera zoom: you, a raider at 6 tiles, another at 15', 'Ticks on the floor every 5 tiles from you, white = 6, red = 15. Map render + the game\'s body and weapon sprites (not a Unity capture).')
    a.convert('RGB').save(os.path.join(out, 'range_corridor_default_zoom.png'))
    # zoomed all the way out: 28 tall, about 50 wide; you at x=36 so that 30 (pistol fall-off starts) and 40 (pistol reach) fit
    b = scene(0, 36, 50, 28, 0.9, [(6, (255, 255, 255, 255), ''), (10, (240, 170, 90, 255), ''), (15, (230, 90, 70, 255), ''), (20, (200, 200, 200, 255), ''), (30, (120, 200, 255, 255), ''), (40, (120, 255, 160, 255), '')],
              'Zoomed all the way out: 6 and 15 tiles, and 30 (pistol fall-off starts), 40 (pistol reach)', 'Ticks at 6, 10, 15, 20, 30 (blue) and 40 tiles (green). Rifles and heavy MGs start losing damage at 50, snipers at 60 (this corridor is about 45 tiles long, so those are off the picture).')
    b.convert('RGB').save(os.path.join(out, 'range_corridor_zoomed_out.png'))
    print(a.size, b.size)
if __name__ == '__main__': main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..', '..', 'maps', 'renders', 'range'))
