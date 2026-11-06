"""The Sump fight, drawn from the REAL boss code (EelSump.cs / EelBoss.cs): the harness runs the whole fight (dotnet /tmp/test.dll eelsump frames.json) and writes, every frame, the eel's spine in 3D,
the players (with their height and spin when thrown), the telegraph zones, the impacts and hits, the flood and the tail's HP. This only DRAWS them: Yan's drawing bent along the spine by the real painter,
on the real Sump render (four pools), with the impact treatment of the storyboards (hit-stop, shake, zoom punch, rings, splashes, debris, cracks, afterimages, shadows). No move is scripted here.
    Tools/compile_check/runtest.sh ../../Assets/Scripts ; dotnet /tmp/test.dll eelsump frames.json ; Tools/render/render.sh sub_station_depths_3
    python3 Tools/render/eel_real_clip.py frames.json maps/renders/boss_eel/eel_real.mp4        (--probe 3.0,12.0 for a contact sheet)
Placeholders: the body that is swallowed and brought back is the armour sprite lying down; the thrown/hurled players are bots at fixed spots; no sound; no Unity view exists yet (this is a preview)."""
import sys, os, json, math, tempfile, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import eel_storyboard as SB
from eel_storyboard import pieces, shadow, stamp, SPLASH, RIPPLES, WHIRL, MAN, FNT, WW, WH, VW, VH, PAD, NS, T, OX, OY, sp, c01
from eel_common import render, plan, banner, video, dead_body, font
import eel_poses as E
random.seed(3)
d = json.load(open(sys.argv[1])); out = sys.argv[2]; FR = d['frames']; FPS = 15
W3 = render('sub_station_depths_3').crop((OX, OY, OX + WW, OY + WH)).convert('RGBA'); PLAN = plan('sub_station_depths_3')
POOLS = [(p[1], p[2], p[3], p[4], p[0], p[5]) for p in d['pools']]; SLAB = d['slab']; BODY = dead_body(d['armor']); TAILMAX, MAXHP = d['tailmax'], d['maxhp']
SHALLOW = [(x, y, min(math.hypot(x + 0.5 - px, y + 0.5 - py) - (rx + ry) / 2 for px, py, rx, ry, nm, bg in POOLS)) for y in range(9, 31) for x in range(5, 55) if PLAN[y][x] == 'w']   # the tiles the flood can take (the game's rule)
tex = W3.crop((int(30.5 * T - OX) - 150, int(24.5 * T - OY) - 50, int(30.5 * T - OX) + 150, int(24.5 * T - OY) + 50)).convert('RGBA'); WATER = Image.new('RGBA', (WW + 300, WH + 100))
for ty in range(0, WATER.height, tex.height):
    for tx in range(0, WATER.width, tex.width): WATER.paste(tex, (tx, ty))
WATER = WATER.crop((0, 0, WW, WH))
ISL = [(8, 12, 6, 4), (46, 12, 6, 4), (21, 19, 4, 3), (36, 19, 4, 3), (28, 18, 5, 2)]
def interp(spine):
    a = np.array(spine, float).reshape(-1, 3); n = len(a); idx = np.linspace(0, n - 1, NS); i0 = np.floor(idx).astype(int).clip(0, n - 2); u = (idx - i0)[:, None]; return a[i0] * (1 - u) + a[i0 + 1] * u
def flood_mask(r):
    m = Image.new('L', (WW, WH), 0); dr = ImageDraw.Draw(m)
    for x, y, dd in SHALLOW:
        if dd <= r: x0, y0 = sp(x, y); dr.rectangle((x0, y0, x0 + T - 1, y0 + T - 1), fill=255)
    return m
def red(c, fn, alpha):
    ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); fn(ImageDraw.Draw(ov), (255, 50, 35, int(alpha))); c.alpha_composite(ov)
def draw_player(c, q, flash):
    x, y, z, rot, hp, prone = q
    if z <= -1.0: return                                                                        # under the water, in the tail's grip
    im = MAN.rotate(-rot, expand=True, resample=Image.NEAREST) if rot % 360 > 1 else MAN
    if prone and z <= 0.05: im = MAN.rotate(80, expand=True, resample=Image.NEAREST)
    if flash > 0.05:
        im = im.copy(); px = im.load()
        for a in range(im.width):
            for b in range(im.height):
                r, g, bl, al = px[a, b]
                if al > 0: px[a, b] = (255, int(g * (1 - flash) + 80 * flash), int(bl * (1 - flash)), al)
    sx, sy = sp(x, y, max(z, 0)); c.alpha_composite(im, (int(sx - im.width / 2), int(sy - im.height / 2 - 6)))

def run(probe=None):
    tmp = tempfile.mkdtemp(); fi = 0; n = 0; shown = -1; pops = []; hitat = {}; sheet = []; probes = sorted(probe) if probe else None; prev = [None, None]; prevhead = None
    while fi < len(FR):
        f = FR[fi]; t = f['t']; adv = SB.frozen == 0
        if SB.frozen > 0: SB.frozen -= 1
        if fi != shown:                                                                      # the events of this frame, once
            shown = fi
            for e in f['ev']:
                if e[0] == 'impact': SB.impact(t, e[1], e[2], e[3], bool(e[4]), stop=2, crackn=7); SB.frozen = min(SB.frozen, 2)
                elif e[0] == 'hit': pops.append((t, e[5], e[6], e[7])); hitat[e[5]] = t
        step = 1.0 / FPS if adv else 0.0
        c = W3.copy(); S = interp(f['spine']); fl = f['flood']
        if 'swell' in f['label'] or ('wind-up' in f['label'] and f['move'] in ('arch', 'dash', 'snare', 'combo')) and False:
            pass
        if 'swell' in f['label']:
            for px, py, rx, ry, nm, bg in POOLS: stamp(c, WHIRL[int(t * 15) % len(WHIRL)], px, py, 2.6 if bg else 1.9, 0.6)
            ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); dr = ImageDraw.Draw(ov)
            for x, y, w, h in ISL: dr.rectangle((*sp(x - 0.3, y - 0.3), *sp(x + w + 0.3, y + h + 0.3)), outline=(255, 255, 255, int(150 + 90 * math.sin(n * 0.5))), width=3)
            c.alpha_composite(ov)
        if fl > 0.05:                                                                         # the flood: the game's own tiles, the pool's water over them, foam at the edge
            m = flood_mask(fl); wl = WATER.copy(); wl.putalpha(m.point(lambda v: int(v * 0.94))); c.alpha_composite(wl)
            edge = Image.fromarray(np.clip(np.array(m).astype(int) - np.array(m.filter(ImageFilter.MinFilter(7))).astype(int), 0, 255).astype(np.uint8)); fo = Image.new('RGBA', c.size, (210, 230, 235, 0)); fo.putalpha(edge.point(lambda v: int(v * 0.65))); c.alpha_composite(fo)
            for k in range(14):
                gx = 8 + (k * 37 % 46); gy = 11 + (k * 53 % 19)
                if m.getpixel((int(sp(gx, gy)[0]), int(sp(gx, gy)[1]))) > 200: stamp(c, RIPPLES[(int(t * 15) + k * 3) % len(RIPPLES)], gx, gy, 0.8, 0.45)
        for z in f['zones']:                                                                  # telegraphs (red) and live zones (white), exactly where the code will hurt
            x, y = sp(z[0], z[1]); live = z[4]; col = (255, 255, 255, 235) if live else (255, 60, 40, 150 + 80 * (n // 2 % 2))
            if z[3] == 'band' and len(z) > 5:
                pts = [sp(z[5][i], z[5][i + 1]) for i in range(0, len(z[5]), 2)]; red(c, lambda dr, co: dr.line(pts, fill=co[:3] + (80,), width=96 if f['move'] == 'arch' else 128), 255)
                if f['move'] == 'arch': red(c, lambda dr, co: dr.ellipse((x - z[2] * T, y - z[2] * T, x + z[2] * T, y + z[2] * T), outline=co, width=3), 220)
            elif z[3] == 'sweep': red(c, lambda dr, co: dr.ellipse((x - z[2] * T, y - z[2] * T, x + z[2] * T, y + z[2] * T), outline=co, width=3), 160 if not live else 230)
            else:
                ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); ImageDraw.Draw(ov).ellipse((x - z[2] * T, y - z[2] * T, x + z[2] * T, y + z[2] * T), fill=col[:3] + (60,), outline=col, width=3); c.alpha_composite(ov)
        for r_ in list(SB.rings):
            age = t - r_['t0']
            if age > r_['life']: SB.rings.remove(r_); continue
            if age < 0: continue
            k = age / r_['life']; rad = r_['r1'] * T * (1 - (1 - k) ** 2.2); x, y = sp(r_['x'], r_['y']); a = int(235 * (1 - k)); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); dr = ImageDraw.Draw(ov)
            dr.ellipse((x - rad, y - rad * 0.8, x + rad, y + rad * 0.8), outline=(235, 240, 240, a), width=max(2, int(r_['w'] * (1 - k)) + 1)); dr.ellipse((x - rad * 0.92, y - rad * 0.74, x + rad * 0.92, y + rad * 0.74), outline=(120, 160, 175, a // 2), width=3); c.alpha_composite(ov)
        ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); dr = ImageDraw.Draw(ov)
        for (qx, qy, qr) in SB.scorch:
            gx, gy = sp(qx, qy)
            for kk, al in ((1.0, 40), (0.7, 55), (0.4, 70)): dr.ellipse((gx - qr * T * kk, gy - qr * T * kk * 0.8, gx + qr * T * kk, gy + qr * T * kk * 0.8), fill=(18, 20, 18, al))
        for cr in SB.cracks: dr.line([sp(*q) for q in cr['pts']], fill=(22, 24, 22, 215), width=3)
        c.alpha_composite(ov); c.alpha_composite(shadow(S))
        if not f['swallowed'] and f['stage'] not in ('vomit',) or f['ejected']:
            c.alpha_composite(BODY, (int(sp(SLAB[0], SLAB[1])[0] - BODY.width / 2), int(sp(SLAB[0], SLAB[1])[1] - BODY.height / 2)))
        pl = f['pl']
        for i, q in enumerate(pl):
            if q[2] <= 0.3: draw_player(c, q, 0.9 * c01(1 - (t - hitat.get(i, -9)) / 0.5))
        face = -1 if S[min(8, NS - 1), 0] > S[0, 0] else 1                                    # the head looks away from its neck
        flooding = fl > 0.5 and f['move'] == 'flood'
        if flooding:
            Sr = S.copy(); Sr[:, 2] = np.where(S[:, 2] > 0.5, S[:, 2], -1.0); layers, P, vis = pieces(Sr, face)
            Ss = S.copy(); Ss[:, 2] = np.where(S[:, 2] > -0.2, 0.02, -1.0); Ss[40:, 2] = -1.0
            if S[0, 2] > 0.5: Ss[:, 2] = -1.0   # rearing/bursting: the rest of the body is the shadow no more
            # the swim is a shadow of the first 10 tiles only (a 35-tile smear cut by the dry islands read as two eels)
            lsh, _, _ = pieces(Ss, face); mf = flood_mask(fl)
            for a, ly in lsh:
                sh = Image.fromarray(E.shadow(ly)); im2 = Image.fromarray(ly)
                sh.putalpha(Image.fromarray(np.minimum(np.array(sh.getchannel('A')), (np.array(mf) * 0.9).astype(np.uint8)))); im2.putalpha(Image.fromarray(np.minimum(np.array(im2.getchannel('A')), (np.array(mf) * 0.35).astype(np.uint8)))); c.alpha_composite(sh); c.alpha_composite(im2)
        else: layers, P, vis = pieces(S, face)
        cur = np.zeros((WH, WW, 4), np.uint8)
        for a, ly in layers: mk = ly[..., 3] > 0; cur[mk] = ly[mk]
        head = S[0]; hv = 0.0 if prevhead is None else math.hypot(head[0] - prevhead[0], head[1] - prevhead[1]) * FPS; prevhead = head.copy() if adv else prevhead
        if hv > 14 and prev[0] is not None:                                                    # afterimages while it moves fast
            for g, alpha in ((prev[0], 0.16), (prev[1], 0.3)):
                if g is not None: gi = Image.fromarray(g); gi.putalpha(gi.getchannel('A').point(lambda v, al=alpha: int(v * al))); c.alpha_composite(gi)
        if adv: prev = [prev[1], cur]
        for a, ly in sorted(layers, key=lambda z: -z[0]): c.alpha_composite(Image.fromarray(ly))
        for i in range(NS - 1):                                                                # foam and ripples where the body goes through the water: always a pool's centre
            if (S[i, 2] > 0.02) != (S[i + 1, 2] > 0.02) and not (fl > 0.05): stamp(c, RIPPLES[int(t * 15) % len(RIPPLES)], S[i, 0], S[i, 1], 0.9, 0.85)
        for s_ in list(SB.splashes):
            idx = int((t - s_['t0']) * 15)
            if idx < 0: continue
            if idx >= len(s_['fr']): SB.splashes.remove(s_); continue
            stamp(c, s_['fr'][idx], s_['x'], s_['y'], s_['s'], s_.get('a', 1.0), up=20 * s_['s'])
        if f['move'] == 'dash' and 'DASH!' in f['label'] and adv:
            for q in range(0, NS, 90):
                if 0.02 < S[q, 2] < 0.9 and random.random() < 0.5: SB.dust(S[q, 0], S[q, 1] + 0.3, 1, 2.0)
        SB.draw_particles(c, t, step)
        if f['tout'] and f['move'] in ('snare', 'combo'):                                       # the tail is a target: its own bar, and the tracer when a teammate shoots it
            tx, ty, tz = S[NS - 1]; bx, by = sp(tx, ty, max(tz, 0) + 1.6); od = ImageDraw.Draw(c, 'RGBA'); hp = c01(f['thp'] / TAILMAX)
            od.rectangle((bx - 46, by - 22, bx + 46, by - 8), fill=(0, 0, 0, 190)); od.rectangle((bx - 44, by - 20, bx - 44 + 88 * hp, by - 10), fill=(205, 80, 60, 255)); od.text((bx - 44, by - 38), 'TAIL - shoot it', font=FNT, fill=(255, 235, 200, 255))
            if f['shoot'] and len(pl) > 1: x0_, y0_ = sp(pl[1][0], pl[1][1], 0.7); x1_, y1_ = sp(tx, ty, max(tz, 0) + 0.3); od.line((x0_, y0_, x1_, y1_), fill=(255, 235, 150, 230), width=3); od.rectangle((x1_ - 4, y1_ - 4, x1_ + 4, y1_ + 4), fill=(255, 255, 255, 255))
        for i, q in enumerate(pl):
            if q[2] > 0.3: draw_player(c, q, 0.9 * c01(1 - (t - hitat.get(i, -9)) / 0.5))
        for (tp_, who, dmg, txt) in pops:
            if 0 <= t - tp_ < 0.9 and who < len(pl):
                q = pl[who]; x, y = sp(q[0], q[1], 2.0 + (t - tp_) * 2.2); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); ImageDraw.Draw(ov).text((x - 55, y), f'-{dmg}  {txt}', font=FNT, fill=(255, 120, 100, int(255 * (1 - (t - tp_) / 0.9)))); c.alpha_composite(ov)
        SB.flash = min(SB.flash, 0.18)
        if SB.flash > 0.02: c.alpha_composite(Image.new('RGBA', c.size, (255, 255, 255, int(255 * SB.flash))))
        sx_ = sy_ = 0
        if SB.shake_amp > 0.3: sx_ = int(random.uniform(-1, 1) * SB.shake_amp); sy_ = int(random.uniform(-1, 1) * SB.shake_amp)
        z_ = 1.0 + SB.zoom; w_, h_ = int(VW / z_), int(VH / z_); cx_, cy_ = PAD + sx_ + VW // 2, PAD + sy_ + VH // 2
        fr = c.crop((cx_ - w_ // 2, cy_ - h_ // 2, cx_ - w_ // 2 + w_, cy_ - h_ // 2 + h_)); fr = fr.resize((VW, VH), Image.NEAREST) if z_ > 1.001 else fr
        ttl = {'combo': '1  COMBO: head strike + tail sweep', 'arch': '2  ARCH SLAM', 'dash': '3  SNAKE DASH', 'snare': '4  TAIL SNARE (' + f['var'] + ')', 'flood': '5  FLOOD SURGE'}.get(f['move'], 'REVEAL' if f['stage'] == 'reveal' else 'DEATH: it brings the body back up' if f['stage'] == 'vomit' else '')
        if ttl: banner(fr, ttl, 12, size=24)
        lab = f['label']; hot = 'wind-up' in lab or 'swell' in lab
        banner(fr, lab, 50, size=22, fill=(255, 140, 110) if hot else (255, 235, 150) if any(k in lab for k in ('STRIKE', 'SLAM', 'SWEEP', 'DASH!', 'CAUGHT', 'DRAGGED', 'THROWN', 'HURLED', 'BURSTS', 'TURNS')) else (225, 225, 215))
        od = ImageDraw.Draw(fr, 'RGBA'); od.rectangle((VW // 2 - 262, VH - 56, VW // 2 + 262, VH - 40), fill=(0, 0, 0, 190)); od.rectangle((VW // 2 - 260, VH - 54, VW // 2 - 260 + 520 * c01(f['hp'] / MAXHP), VH - 42), fill=(150, 40, 40, 255))
        od.text((VW // 2 - 256, VH - 54), f"THE DROWNER (placeholder name)  head {f['hp']}   tail {f['thp']}", font=font(12), fill=(255, 245, 225))
        banner(fr, 'drawn from the real fight code (EelSump.cs) by the harness | preview only: no Unity view, no sound | players are bots', VH - 30, size=14, fill=(210, 205, 190), bg=(0, 0, 0, 150))
        SB.shake_amp *= 0.67; SB.flash *= 0.4; SB.zoom *= 0.67
        if probe:
            while probes and t >= probes[0]: sheet.append((probes.pop(0), fr.copy()))
            if not probes: break
        else: fr.convert('RGB').save(os.path.join(tmp, f'{n:05d}.png'))
        n += 1
        if adv: fi += 1
    if probe:
        w, h, cols = 640, 360, 3; rows = (len(sheet) + cols - 1) // cols; sh = Image.new('RGB', (cols * w, rows * h))
        for k, (pt, im) in enumerate(sheet): sh.paste(im.resize((w, h)).convert('RGB'), ((k % cols) * w, (k // cols) * h))
        sh.save(out); print(out, len(sheet), 'moments')
    else: video(tmp, out, FPS); print(out, n, 'frames', round(n / FPS, 1), 's')

if __name__ == '__main__':
    pr = [float(v) for v in sys.argv[sys.argv.index('--probe') + 1].split(',')] if '--probe' in sys.argv else None
    run(pr)
