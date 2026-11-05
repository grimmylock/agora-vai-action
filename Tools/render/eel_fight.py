"""Boss 3 preview: the eel against a bot, from the harness (the real EelBoss.cs code), drawn on the real Depths renders with the real strips.
Level 1: lurk, wind-up, WRAP, SHOCK when the coils are touched, STRIKE; then the Sump: REVEAL (swallows the body), the rear-up fight (snap, lunge, slam,
sweep, smash when enraged, wounds at 75/50/25 %), VOMIT and RETREAT. Every frame comes from a harness tick; the labels are EelBoss.Label.
    Tools/compile_check/runtest.sh ../../Assets/Scripts ; dotnet /tmp/test.dll eelframes frames.json
    python3 Tools/render/eel_fight.py frames.json maps/renders/boss_eel/eel_fight.mp4
Placeholders: the platform STRIKE reuses the rear-up snap strip (no strike art yet); the swim for a lurking eel is its shadow strip; the body is the armour
sprite lying down (the game's corpse is a ragdoll); no sound, no HUD, no camera."""
import sys, os, json, math, tempfile
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from eel_common import *
d = json.load(open(sys.argv[1])); out = sys.argv[2]
VW, VH = 1152, 704
man = player(); tmp = tempfile.mkdtemp(); n = 0
MAXHP = 24000; WIND = 0.6
def save(c):
    global n
    c.convert('RGB').save(os.path.join(tmp, f'{n:05d}.png')); n += 1
def hud(c, f, seg, title):
    dr = ImageDraw.Draw(c, 'RGBA'); bx, by, bw = VW // 2 - 260, 14, 520
    dr.rectangle((bx - 2, by - 2, bx + bw + 2, by + 18), fill=(0, 0, 0, 190)); w = bw * max(0, f['hp']) / MAXHP
    dr.rectangle((bx, by, bx + w, by + 16), fill=(150, 40, 40, 255) if f['enraged'] else (120, 150, 60, 255))
    for q in (0.75, 0.5, 0.25): dr.line((bx + bw * q, by - 2, bx + bw * q, by + 18), fill=(255, 255, 255, 200), width=2)
    dr.text((bx + 6, by + 2), f"THE DROWNER (placeholder name)  stage {f['phase']}" + ("  ENRAGED" if f['enraged'] else "") + (f"  wraps {f['wraps']}" if seg == 0 else ""), font=font(13), fill=(255, 245, 225))
    dr.rectangle((14, VH - 44, 214, VH - 28), fill=(0, 0, 0, 190)); dr.rectangle((16, VH - 42, 16 + 196 * f['php'] / 1000, VH - 30), fill=(80, 170, 90, 255))
    dr.text((20, VH - 42), f"player {f['php']}", font=font(12), fill=(255, 255, 255))
    banner(c, title, VH - 28, size=14, fill=(210, 205, 190), bg=(0, 0, 0, 150), x=230)
def popup(c, text, x, y, col): banner(c, text, y, size=16, fill=col, bg=(0, 0, 0, 0), x=x)
def label_banner(c, f):
    lab = f['label']; key = lab.split(':')[0].split(' - ')[0].strip()
    hot = any(k in lab for k in ('WIND-UP', 'wind-up')); big = any(k in lab for k in ('WRAP', 'SHOCK', 'STRIKE', 'SNAP', 'LUNGE', 'SLAM', 'SWEEP', 'SMASH', 'REVEAL', 'VOMIT'))
    col = (255, 120, 90) if hot else (130, 220, 255) if 'SHOCK' in lab or 'WRAP' in lab else (255, 230, 150) if big else (225, 225, 215)
    banner(c, lab, 52, fill=col, size=26)
def ring(c, x, y, r, col, w=3, squash=1.0):
    g = Image.new('RGBA', c.size, (0, 0, 0, 0)); dg = ImageDraw.Draw(g); dg.ellipse((x - r, y - r * squash, x + r, y + r * squash), fill=col[:3] + (60,), outline=col, width=w); c.alpha_composite(g)

# ---------------------------------------------------------------------------- segment 0: Level 1
S1 = render('sub_station_depths_1'); P1 = plan('sub_station_depths_1'); wm = tile_mask(P1, S1.size, lambda ch: ch in 'W0'); nonwater = tile_mask(P1, S1.size, lambda ch: ch not in 'W0#')
lamps = [(x, y) for y, r in enumerate(P1) for x, ch in enumerate(r) if ch == 'e']
seg = d['segs'][0]; fr = seg['frames']; xs = [f['p'][0] for f in fr] + [fr[0]['plat'][0] or 56, fr[0]['shadow'][0]]; ys = [f['p'][1] for f in fr]
plat = next((x, y) for y, r in enumerate(P1) for x, ch in enumerate(r) if ch == '7')
cx, cy = (min(xs + [plat[0]]) + max(xs + [plat[0]])) / 2, (min(ys + [plat[1]]) + max(ys + [plat[1]])) / 2
cx = max(VW / T / 2, min(64 - VW / T / 2, cx)); cy = max(VH / T / 2, min(40 - VH / T / 2, cy)); X0, Y0 = int(cx * T - VW / 2), int(cy * T - VH / 2)
prev = None; php_prev = fr[0]['php']; shock_t = -99; head = -1; hit_pop = []; hold = 0; t_start = None
def title_card(text, sub):
    c = Image.new('RGBA', (VW, VH), (8, 12, 12, 255)); banner(c, text, VH // 2 - 30, size=34, bg=(0, 0, 0, 0)); banner(c, sub, VH // 2 + 20, size=18, bg=(0, 0, 0, 0), fill=(200, 200, 190))
    for k in range(30): save(c)
title_card('LEVEL 1 - the raised catwalks', 'a bot walks to the strike platform; EelBoss.cs plays the eel. Damage here carries over, capped at 20 % of its HP.')
wrap_t0 = None; lastSB = None
for k, f in enumerate(fr):
    c = S1.crop((X0, Y0, X0 + VW, Y0 + VH)).copy(); m = wm.crop((X0, Y0, X0 + VW, Y0 + VH)); st = f['stage']; age = f['age']; px, py, dv = f['p']
    pcx, pcy = (plat[0] + 0.5) * T - X0, (plat[1] + 0.5) * T - Y0
    sx, sy = f['shadow']; moving_left = True
    if prev is not None and f['shadow'][0] != prev['shadow'][0]: head = -1 if f['shadow'][0] < prev['shadow'][0] else 1
    if head == 0: head = -1
    facing_right = head == 1
    if st in ('lurk', 'recover', 'done', 'wind', 'release') and not (st == 'release'):
        im, an = frame('swim_shadow', k % 16); put(c, im, an, (sx * T - X0, sy * T - Y0), facing_right, 0.95, m)
        im, an = frame('swim', k % 16); put(c, im, an, (sx * T - X0, sy * T - Y0), facing_right, 0.4, m)
    if st == 'wind':                                                               # the lamps flicker red, rings swell on the water over the platform
        for lx, ly in lamps:
            if abs(lx - plat[0]) < 9 and abs(ly - plat[1]) < 9 and (k // 2) % 2 == 0: ring(c, (lx + 0.5) * T - X0, (ly + 0.5) * T - Y0, 20, (255, 40, 30, 220), 2)
        for j in range(3): ring(c, pcx, pcy, 40 + (age * 40 + j * 38) % 114, (200, 230, 240, int(200 * (1 - ((age * 40 + j * 38) % 114) / 114))), 2, 0.8)
    if st in ('wrap', 'release') or (st == 'wind' and age > 1.2):
        if wrap_t0 is None or st == 'wind': wrap_t0 = None
        if st == 'wrap' and wrap_t0 is None: wrap_t0 = k
        i = (min(7, int(age * 15)) if st == 'wrap' and age < 8 / 15 else 8 + (int(age * 15) - 8) % 6 if st == 'wrap' else 14 + min(7, int(age * 15)))
        bk, an = frame('wrap_back', i); ft, _ = frame('wrap_front', i)
        put(c, bk, an, (pcx, pcy)); c.paste(S1.crop((X0, Y0, X0 + VW, Y0 + VH)), (0, 0), nonwater.crop((X0, Y0, X0 + VW, Y0 + VH)))
        c.alpha_composite(man, (int(px * T - X0 - 16), int(py * T - Y0 - 22))); put(c, ft, an, (pcx, pcy))
        player_drawn = True
    else: player_drawn = False
    if st == 'wrap' and f['zones']:                                                # the touch ring
        z = f['zones'][0]; ring(c, z[0] * T - X0, z[1] * T - Y0, 3.1 * T, (130, 200, 255, 90), 2); ring(c, z[0] * T - X0, z[1] * T - Y0, 1.9 * T, (130, 200, 255, 70), 2)
    if st == 'strike' and f['zones']:
        z = f['zones'][0]; zx, zy = z[0] * T - X0, z[1] * T - Y0; live = age >= 0.55
        ring(c, zx, zy, z[2] * T, (255, 255, 255, 230) if live else (255, 60, 40, 140 + 80 * ((k // 2) % 2)), 3)
        if age >= 0.45:
            i = min(9, int((age - 0.45) * 15)); ox = (-6.25 if True else 0) * T; right = px > cx  # placeholder strike: the rear-up snap strip, anchored so the head lands on the zone
            im, an = frame('snap', i); fl = px > (f['plat'][0]); off = 6.25 * T if fl else -6.25 * T
            put(c, im, an, (zx - off, zy + 2.4 * T), fl)
    if not player_drawn:
        u = man if not dv else man.rotate(-70)
        c.alpha_composite(u, (int(px * T - X0 - 16), int(py * T - Y0 - 22)))
    dmg = php_prev - f['php']
    if dmg > 0:
        hit_pop.append([k, dmg, px * T - X0, py * T - Y0, f['label']])
    php_prev = f['php']
    if st == 'wrap' and any(h[0] > k - 6 and 'coils' in str(h[4]).lower() or h[0] > k - 6 and 'SHOCK' in h[4] for h in hit_pop):
        dr = ImageDraw.Draw(c, 'RGBA'); z = f['zones'][0]
        for j in range(6): a = j / 6 * 2 * math.pi + k; zap_line(dr, (z[0] * T - X0 + math.cos(a) * 2.4 * T, z[1] * T - Y0 + math.sin(a) * 2.2 * T), (px * T - X0, py * T - Y0 - 10), k * 7 + j)
        shock_t = k
    for h in hit_pop:
        if k - h[0] < 14: popup(c, f"-{h[1]}", h[2] - 10, h[3] - 50 - (k - h[0]) * 2, (255, 120, 100))
    lab = f['label']
    if st == 'wrap' and k - shock_t < 8 and shock_t > 0: lab = 'SHOCK: you touched the coils'
    f2 = dict(f); f2['label'] = lab; label_banner(c, f2); hud(c, f, 0, 'Level 1 | bot-played through EelBoss.cs | placeholder: strike uses the rear-up snap strip, no sound')
    prev = f; save(c)
# ---------------------------------------------------------------------------- segment 1: the Sump
title_card('LEVEL 3 - the Sump: the final phase', 'reveal, rear-up fight, wounds at 75 / 50 / 25 %, enrage from 50 % (smash = three slams), then it vomits the body up and retreats.')
S3 = render('sub_station_depths_3'); seg = d['segs'][1]; fr = seg['frames']; body = dead_body(seg['armor'])
CXs, CYs = 30.5, 24.5; X0, Y0 = int(30.5 * T - VW / 2), int(22.5 * T - VH / 2); slab = (30.5 * T, 19.5 * T)
php_prev = fr[0]['php']; hit_pop = []; vm = ANIMS['vomit']['mouth']; rm = ANIMS['reveal']['mouth']
for k, f in enumerate(fr):
    c = S3.crop((X0, Y0, X0 + VW, Y0 + VH)).copy(); st = f['stage']; age = f['age']; px, py, dv = f['p']; side = f['side']; flip = side > 0; dmgst = f['phase']
    ap = (CXs * T - X0, CYs * T - Y0)
    if st in ('hidden', 'reveal') and f['body'][0] != -1:                          # the body on the slab
        c.alpha_composite(body, (int(slab[0] - X0 - body.width / 2), int(slab[1] - Y0 - body.height / 2)))
    if st == 'hidden':
        for j in range(2): ring(c, ap[0], ap[1], 20 + ((k * 1.5 + j * 40) % 80), (180, 220, 230, 90), 2, 0.5)
    elif st == 'reveal': im, an = frame('reveal', min(25, int(age * 15)), dmgst); put(c, im, an, ap, flip)
    elif st in ('rear', 'hurt') or (st == 'move' and age < WIND):
        nm, i = ('hurt', min(7, int(age * 15))) if st == 'hurt' else ('rear_idle', k % 12)
        im, an = frame(nm, i, dmgst); put(c, im, an, ap, flip)
    elif st == 'move':
        mv = f['move']; i = int((age - WIND) * 15); im, an = frame(mv, i, dmgst); fl = (not flip) if mv == 'sweep' else flip; put(c, im, an, ap, fl)
    elif st == 'vomit': im, an = frame('vomit', min(23, int(age * 15)), dmgst); put(c, im, an, ap, flip)
    elif st == 'retreat': im, an = frame('retreat', min(13, int(age * 15)), dmgst); put(c, im, an, ap, flip)
    if st in ('vomit', 'retreat') and (st == 'retreat' or age >= 13 / 15):         # the body flies from the mouth to the slab, then lies there
        t = min(1.0, (age - 13 / 15) / 0.4) if st == 'vomit' else 1.0; mi = min(23, int(age * 15)) if st == 'vomit' else 23; mx, my = vm[min(mi, len(vm) - 1)]
        mx = -mx if flip else mx; sx_, sy_ = ap[0] + mx, ap[1] + my; bx = sx_ + (slab[0] - X0 - sx_) * t; by = sy_ + (slab[1] - Y0 - sy_) * t - 60 * math.sin(math.pi * t)
        c.alpha_composite(body, (int(bx - body.width / 2), int(by - body.height / 2)))
    if st == 'reveal' and 8 / 15 <= age < 13 / 15 + 0.01: pass
    for z in f['zones']:                                                           # where it will land / is landing
        zx, zy = z[0] * T - X0, z[1] * T - Y0
        if z[3] == 'sweep': ring(c, zx, zy, z[2] * T, (255, 255, 255, 220) if z[4] else (255, 70, 50, 160), 3)
        else: ring(c, zx, zy, z[2] * T, (255, 255, 255, 230) if z[4] else (255, 60, 40, 130 + 90 * ((k // 2) % 2)), 3)
    if st == 'move' and f['move'] == 'sweep' and age < WIND:                       # the arc the head will take
        g = Image.new('RGBA', c.size, (0, 0, 0, 0)); dg = ImageDraw.Draw(g)
        for j in range(24):
            th = math.pi + 0.5 + (math.pi - 1.0) * j / 23; x1 = ap[0] + math.cos(th) * 215 * (-1 if flip else 1); y1 = ap[1] + math.sin(th) * 215
            dg.ellipse((x1 - 5, y1 - 5, x1 + 5, y1 + 5), fill=(255, 70, 50, 150))
        c.alpha_composite(g)
    u = man if not dv else man.rotate(-70); c.alpha_composite(u, (int(px * T - X0 - 16), int(py * T - Y0 - 22)))
    dmg = php_prev - f['php']; php_prev = f['php']
    if dmg > 0: hit_pop.append([k, dmg, px * T - X0, py * T - Y0])
    for h in hit_pop:
        if k - h[0] < 14: popup(c, f"-{h[1]}", h[2] - 10, h[3] - 50 - (k - h[0]) * 2, (255, 120, 100))
    if f['chain'] > 0 and st == 'move': f = dict(f, label=f['label'] + f"  [{3 - f['chain'] + 1}/3]")
    label_banner(c, f); hud(c, f, 1, 'Level 3 | bot-played through EelBoss.cs | placeholders: body is the armour sprite lying down, no sound; the key is a to-do')
    if f['label'].startswith('wounds'):
        banner(c, f['label'].replace('wounds open', 'DAMAGE STAGE').upper(), 90, size=20, fill=(255, 150, 120))
    save(c)
c = Image.new('RGBA', (VW, VH), (8, 12, 12, 255)); banner(c, 'It is gone. The body stays on the slab: tier-4 armour, stash-like loot, a story key (to do).', VH // 2 - 14, size=20, bg=(0, 0, 0, 0))
for k in range(30): save(c)
video(tmp, out); print(out, n, 'frames', 'bot hits', d['hits'], 'dives', d['dives'])
