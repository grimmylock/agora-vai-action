"""Art preview of the eel (Yan's drawing, bent along paths) in the real Depths renders, a player beside it for scale; built like brain_floating.mp4:
the strips composited on the real map render, frame by frame.
    Tools/render/render.sh sub_station_depths_1 sub_station_depths_3 ; python3 ArtSource/bosses/eel_anim.py <sump.png> <l1.png>
    python3 Tools/render/eel_art.py maps/renders/boss_eel/eel_art.mp4
Segments: idle rear-up (Sump) / the three damage stages / swimming under the catwalks (Level 1) / wrapping a platform (Level 1) / the rear-up moves."""
import sys, os, tempfile, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from eel_common import *
out = sys.argv[1]
S3, S1 = render('sub_station_depths_3'), render('sub_station_depths_1'); P3, P1 = plan('sub_station_depths_3'), plan('sub_station_depths_1')
man = player(); tmp = tempfile.mkdtemp(); n = 0; VW, VH = 960, 640
def view(bg, cx, cy):
    x0, y0 = int(cx * T - VW / 2), int(cy * T - VH / 2); return x0, y0, bg.crop((x0, y0, x0 + VW, y0 + VH)).copy()
def save(c, caption, sub=''):
    global n
    banner(c, caption, 10, size=24)
    if sub: banner(c, sub, VH - 34, size=15, fill=(210, 205, 190), bg=(0, 0, 0, 150))
    c.convert('RGB').save(os.path.join(tmp, f'{n:05d}.png')); n += 1
# the Sump
CX, CY = 30.5 * T, 24.5 * T; x0, y0, _ = view(S3, 30.5, 22.5); PL = (22.5 * T, 21.5 * T)
def sump(fr, dmg=0, flip=False, who=PL, after=None):
    _, _, c = view(S3, 30.5, 22.5); im, an = frame(*fr, dmg) if isinstance(fr, tuple) else frame(fr, 0, dmg)
    put(c, im, an, (CX - x0, CY - y0), flip); c.alpha_composite(man, (int(who[0] - x0 - 16), int(who[1] - y0 - 22)))
    return c
for loop in range(3):                                                            # 1. idle
    for i in range(12): save(sump(('rear_idle', i)), 'IDLE - rearing half out of the water', 'The Sump. The player is 1 tile tall: the eel is about 9 tiles of neck and coil above the pool.')
for st in range(4):                                                              # 2. damage marks
    for i in range(12): save(sump(('rear_idle', i), st), ['DAMAGE 0 - full health', 'DAMAGE 1 - 75% HP: gashes, a torn fin', 'DAMAGE 2 - 50% HP: flesh and blood on the flank', 'DAMAGE 3 - 25% HP: open wounds, bone, drips'][st], 'The wounds are overlay strips over the same frames (boss_eel_<anim>_d1..d3).')
# 3. swimming under the catwalks (Level 1): the lurking shadow, masked to the water so the catwalks pass over it
wm = tile_mask(P1, S1.size, lambda ch: ch in 'W0'); sw, swa = frame('swim_shadow', 0)
row = 28; px = 46                # a catwalk crossing the row: the player stands on it, the eel passes under
for k in range(110):
    head = (64 - k * 0.3) * T; hx = head; cx_ = 44
    x0_, y0_, c = view(S1, cx_, 25); m = wm.crop((x0_, y0_, x0_ + VW, y0_ + VH))
    im, an = frame('swim', k % 16); put(c, im, an, (hx - x0_, (row + 0.5) * T - y0_), False, 0.42, m)         # murky: the body seen through the water
    im, an = frame('swim_shadow', k % 16); put(c, im, an, (hx - x0_, (row + 0.5) * T - y0_), False, 0.9, m)
    c.alpha_composite(man, (int((px + 0.5) * T - x0_ - 16), int(row * T - y0_ - 12)))
    save(c, 'SWIM - below the catwalks (shadow + the body through the water)', 'Level 1. Only drawn over deep water, so the catwalks pass over it. The game shows just the shadow; the faint body is for judging the art.')
# 4. the wrap round a platform (Level 1): back part under the platform and catwalks, the player on it, the front part over
plat = next((x, y) for y, r in enumerate(P1) for x, ch in enumerate(r) if ch == '7'); pcx, pcy = (plat[0] + 0.5) * T, (plat[1] + 0.5) * T
nonwater = tile_mask(P1, S1.size, lambda ch: ch not in 'W0#')
order = list(range(0, 8)) + list(range(8, 14)) * 2 + list(range(14, 22))
for k, i in enumerate(order):
    x0_, y0_, c = view(S1, plat[0] - 8, plat[1] - 1); bk, an = frame('wrap_back', i); ft, _ = frame('wrap_front', i)
    put(c, bk, an, (pcx - x0_, pcy - y0_)); c.paste(S1.crop((x0_, y0_, x0_ + VW, y0_ + VH)), (0, 0), nonwater.crop((x0_, y0_, x0_ + VW, y0_ + VH)))
    c.alpha_composite(man, (int(pcx - x0_ - 16), int(pcy - y0_ - 22))); put(c, ft, an, (pcx - x0_, pcy - y0_))
    save(c, 'WRAP - ' + ('bursts out round the platform' if i < 8 else 'holds, electricity on the coils' if i < 14 else 'lets go'), 'Level 1. The player stands on the 3x3 platform; the electricity sparks are baked into the hold frames.')
# 5. the rear-up moves (Sump)
for mv, flip, lab in (('lunge', False, 'LUNGE - the long dart'), ('snap', False, 'SNAP - the short bite'), ('slam', False, 'SLAM - crashes down in front'), ('hurt', False, 'HURT - a flinch'), ('sweep', True, 'SWEEP - the head swings round the pool')):
    for i in range(ANIMS[mv]['frames']):
        who = (24.5 * T, 21.5 * T) if mv != 'sweep' else (31 * T, 13.5 * T)
        save(sump((mv, i), 0, flip, who), lab, 'The Sump. The strips face left as drawn; the game flips them to face the player.')
video(tmp, out); print(out, n, 'frames')
