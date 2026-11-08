"""The eel's procedural body (EelRig.cs) in motion: the harness dumps the head and segment chain every frame (dotnet /tmp/test.dll eelframes frames.json), and
Yan's own drawing is bent along that chain with the real painter (ArtSource/bosses/eel_poses.py), over the real Depths renders. Excerpts: the wrap (Level 1), the Sump attacks.
    python3 Tools/render/eel_rig_clip.py frames.json maps/renders/boss_eel/eel_rig.mp4"""
import sys, os, json, math, tempfile
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__))); sys.path.insert(0, R_ := os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'ArtSource', 'bosses'))
from eel_common import *
import eel_poses as E, make_eel as M
d = json.load(open(sys.argv[1])); out = sys.argv[2]; VW, VH = 1152, 704; man = player(); tmp = tempfile.mkdtemp(); n = 0
def save(c):
    global n
    c.convert('RGB').save(os.path.join(tmp, f'{n:05d}.png')); n += 1
def smooth(P, it=3):
    P = np.array(P, float)
    for _ in range(it): Q = np.empty((len(P) * 2 - 2, 2)); Q[0::2] = P[:-1] * 0.75 + P[1:] * 0.25; Q[1::2] = P[:-1] * 0.25 + P[1:] * 0.75; P = np.vstack([P[:1], Q, P[-1:]])
    return P
def eel_from_rig(rig, nvis, face, X0, Y0, stage):
    """Yan's pixels bent along the chain: the head at the chain's start (a profile, never rotated), a short straight lead behind it, the chain smoothed and resampled to 1 px"""
    if E.CUR_STAGE != stage: E.set_stage(stage); E.CUR_STAGE = stage
    pts = [((rig[2 * i] * T - X0), (rig[2 * i + 1] * T - Y0)) for i in range(0, nvis + 1)]
    h = pts[0]; back = 1 if face < 0 else -1; lead = [(h[0] + back * k, h[1]) for k in range(E.LEAD)]
    body = [p for p in pts[1:] if math.hypot(p[0] - h[0], p[1] - h[1]) > E.LEAD + 10]
    P = M.resample([tuple(q) for q in smooth(lead[-1:] + body, 3)]); P = lead + [tuple(q) for q in P[1:]]
    return E.eel_layer(VW, VH, P, face)
E.CUR_STAGE = 0
def label(c, text, y=14, size=24, fill=(255, 235, 200)): banner(c, text, y, size=size, fill=fill)
def ring(c, x, y, r, col, w=3):
    g = Image.new('RGBA', c.size, (0, 0, 0, 0)); ImageDraw.Draw(g).ellipse((x - r, y - r, x + r, y + r), fill=col[:3] + (55,), outline=col, width=w); c.alpha_composite(g)

# ---- Level 1: the wrap excerpt
S1 = render('sub_station_depths_1'); P1 = plan('sub_station_depths_1'); nonwater = tile_mask(P1, S1.size, lambda ch: ch not in 'W0#'); wm = tile_mask(P1, S1.size, lambda ch: ch in 'W0')
seg = d['segs'][0]['frames']; plat = next((x, y) for y, r in enumerate(P1) for x, ch in enumerate(r) if ch == '7')
t0 = next(f['t'] for f in seg if f['stage'] == 'wind'); t1 = next(f['t'] for f in seg if f['stage'] == 'recover')
X0, Y0 = int(min(plat[0] - 14, 64 - VW / T) * T), int((plat[1] - 9) * T)         # the view stays on the map
for f in seg:
    if not (t0 - 0.6 <= f['t'] <= t1 + 0.4): continue
    c = S1.crop((X0, Y0, X0 + VW, Y0 + VH)).copy(); st = f['stage']; px, py, dv = f['p']; pcx, pcy = (plat[0] + 0.5) * T - X0, (plat[1] + 0.5) * T - Y0
    lay = eel_from_rig(f['rig'], f['nvis'], f['face'], X0, Y0, f['phase'])
    if f['under']:
        m = wm.crop((X0, Y0, X0 + VW, Y0 + VH)); a = Image.fromarray(E.shadow(lay)); a.putalpha(Image.composite(a.getchannel('A').point(lambda v: int(v * .9)), Image.new('L', a.size, 0), m)); c.alpha_composite(a)
        b = Image.fromarray(lay); b.putalpha(Image.composite(b.getchannel('A').point(lambda v: int(v * .4)), Image.new('L', b.size, 0), m)); c.alpha_composite(b)
        c.alpha_composite(man, (int(px * T - X0 - 16), int(py * T - Y0 - 22)))
    elif st in ('wrap', 'release'):
        im = Image.fromarray(lay); back = im.copy(); fr = im.copy(); ba = np.array(back); fa = np.array(fr); cut = int(pcy); ba[cut:] = 0; fa[:cut] = 0
        c.alpha_composite(Image.fromarray(ba)); c.paste(S1.crop((X0, Y0, X0 + VW, Y0 + VH)), (0, 0), nonwater.crop((X0, Y0, X0 + VW, Y0 + VH)))
        c.alpha_composite(man, (int(px * T - X0 - 16), int(py * T - Y0 - 22))); c.alpha_composite(Image.fromarray(fa))
    else:
        c.alpha_composite(Image.fromarray(lay)); c.alpha_composite(man, (int(px * T - X0 - 16), int(py * T - Y0 - 22)))
    for z in f['zones']:
        if z[3] == 'strike': ring(c, z[0] * T - X0, z[1] * T - Y0, z[2] * T, (255, 255, 255, 230) if z[4] else (255, 60, 40, 170))
    if f['disch']: 
        dr = ImageDraw.Draw(c, 'RGBA'); rig = f['rig']
        for j in range(0, f['nvis'], 3): zap_line(dr, (rig[2 * j] * T - X0, rig[2 * j + 1] * T - Y0), (rig[2 * ((j + 2) % f['nvis'])] * T - X0 + math.sin(n + j) * 10, rig[2 * ((j + 2) % f['nvis']) + 1] * T - Y0), n * 5 + j, 2)
    lab = f['label']; label(c, lab, fill=(130, 220, 255) if f['disch'] else (255, 120, 90) if 'WIND' in lab else (255, 235, 200))
    banner(c, 'procedural body: head path + 56-segment chain | Yan\'s drawing bent along it | placeholders: no sound, strike is not drawn from the water', VH - 30, size=14, fill=(210, 205, 190), bg=(0, 0, 0, 150))
    save(c)
# ---- Level 3: the Sump attacks excerpt
S3 = render('sub_station_depths_3'); seg = d['segs'][1]['frames']; X0, Y0 = int(30.5 * T - VW / 2), int(22.5 * T - VH / 2)
ts = [f['t'] for f in seg if f['stage'] == 'move']; start = ts[0] - 0.8
wins = [(start, start + 11.0)]; sm = [f['t'] for f in seg if f['stage'] == 'move' and f['chain'] > 0]
if sm: wins.append((sm[0] - 0.8, sm[0] + 3.4))
for w0, w1 in wins:
    for f in seg:
        if not (w0 <= f['t'] <= w1): continue
        c = S3.crop((X0, Y0, X0 + VW, Y0 + VH)).copy(); px, py, dv = f['p']
        face = f['side'] if not (f['move'] == 'sweep') else f['face']
        lay = eel_from_rig(f['rig'], 56, face, X0, Y0, f['phase']); c.alpha_composite(Image.fromarray(lay))
        for z in f['zones']: ring(c, z[0] * T - X0, z[1] * T - Y0, z[2] * T, (255, 255, 255, 230) if z[4] else (255, 60, 40, 150 + 80 * (n // 2 % 2)))
        c.alpha_composite(man if not dv else man.rotate(-70), (int(px * T - X0 - 16), int(py * T - Y0 - 22)))
        lab = f['label'] + (f"  [{3 - f['chain'] + 1}/3]" if f['chain'] and f['stage'] == 'move' else ''); label(c, lab, fill=(255, 120, 90) if 'wind-up' in lab else (255, 235, 200))
        banner(c, 'procedural body | Yan\'s drawing bent along the chain | head: ease-in-back wind-up (0.6 s), ease-out-expo strike, sweep on a quadratic Bezier', VH - 30, size=14, fill=(210, 205, 190), bg=(0, 0, 0, 150))
        save(c)
video(tmp, out); print(out, n, 'frames')
