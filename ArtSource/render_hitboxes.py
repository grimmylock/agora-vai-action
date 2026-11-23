#!/usr/bin/env python3
"""2026-11-22 (Yan: 'make an audit of all the mutant hitboxes in game and show them to me'): draws every mutant's and creature boss's hit shape (GameData/HitProfiles.cs: per pixel row the half-width and where its middle sits, the shape a round and the
crosshair test) over its sprite: the stand picture facing right, the same facing left (the shape mirrors with it), and a walking and a striking frame. Orange = body, red = head (the top 30%). Output: maps/renders/hitboxes/*.png"""
import os, re, glob, numpy as np
from PIL import Image, ImageDraw
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'); R = os.path.join(ROOT, 'Assets/Resources/Sprites'); OUT = os.path.join(ROOT, 'maps/renders/hitboxes'); os.makedirs(OUT, exist_ok=True)
src = open(os.path.join(ROOT, 'Assets/Scripts/GameData/HitProfiles.cs')).read()
All = {n: (int(ppt), np.array([float(v.rstrip('f')) for v in vals.split(',')])) for n, ppt, vals in re.findall(r'\["([^"]+)"\] = \((\d+), new\[\] \{ ([^}]*) \}\)', src)}
cen_block = src[src.index('Centre = new'):src.index('CentreOf')]
Centre = {n: np.array([float(v.rstrip('f')) for v in vals.split(',')]) for n, vals in re.findall(r'\["([^"]+)"\] = new\[\] \{ ([^}]*) \}', cen_block)}
head_block = src[src.index('Head = new'):src.index('HasHead')]
HEADS = {n: tuple(float(v.rstrip('f')) for v in vals.split(',')) for n, vals in re.findall(r'\["([^"]+)"\] = \(([^)]*)\)', head_block)}
ATT = open(os.path.join(ROOT, 'Assets/Scripts/GameData/MutantAttackData.cs')).read()
BA = open(os.path.join(ROOT, 'Assets/Scripts/GameCore/BossArt.cs')).read()
ZOOM = 3
RUNS_SRC = src[src.index('Runs = new'):src.index('HasRuns')]
def _parse_runs():
    out = {}; cur = None
    for line in RUNS_SRC.splitlines():
        m = re.match(r'\s*\["([^"]+)"\] = new float\[\]\[\]', line)
        if m: cur = m.group(1); out[cur] = []; continue
        if cur and 'new[]' in line: out[cur].append([float(v.rstrip('f')) for v in re.findall(r'-?[0-9.]+f', line)])
        elif cur and 'new float[0]' in line: out[cur].append([])
    return out
RUNS = _parse_runs()
ATK_SRC = src[src.index('AttackRuns = new'):src.index('AttackRunsAt')]
def _parse_atk():
    out = {}; cur = None
    for line in ATK_SRC.splitlines():
        m = re.match(r'\s*\["([^"]+)"\] = new float\[\]\[\]', line)
        if m: cur = m.group(1); out[cur] = []; continue
        if cur and 'new[]' in line: out[cur].append([float(v.rstrip('f')) for v in re.findall(r'-?[0-9.]+f', line)])
        elif cur and 'new float[0]' in line: out[cur].append([])
    return out
ATK = _parse_atk()
def overlay(img, key, ppt, flip, extra=None):
    """draws the OUTLINE (HitProfiles.Runs) the round and the crosshair test: img's bottom opaque row = row 0, its centre = the unit's centre; a flipped picture mirrors the runs."""
    a = np.array(img)[..., 3] > 0
    if not a.any() or key not in RUNS: return img
    ys = np.where(a.any(1))[0]; bot = ys.max(); w, h = img.size; cx = w / 2.0
    lay = Image.new('RGBA', img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    rows = RUNS[key]
    if extra and extra in ATK:
        n = max(len(rows), len(ATK[extra])); rows = [(rows[r] if r < len(rows) else []) + (ATK[extra][r] if r < len(ATK[extra]) else []) for r in range(n)]
    for r, runs in enumerate(rows):
        y = bot - r
        if y < 0: break
        for i in range(0, len(runs) - 1, 2):
            ra, rb = runs[i], runs[i + 1]
            if flip: ra, rb = -rb, -ra
            d.line([(cx + ra * ppt, y), (cx + rb * ppt, y)], fill=(255, 150, 30, 110))
    return Image.alpha_composite(img, lay)
def panel(frame, key, ppt, flip, zoom=None, extra=None):
    im = frame.transpose(Image.FLIP_LEFT_RIGHT) if flip else frame
    # the shape is computed for the picture AS DRAWN: a flipped picture mirrors the shape (done through `flip` above), not the pixels' own centre
    out = overlay(im, key, ppt, flip, extra)
    z = zoom or ZOOM; return out.resize((out.width * z, out.height * z), Image.NEAREST)
def strip_frame(fn, n, i):
    st = Image.open(fn).convert('RGBA'); w = st.width // n; return st.crop((i * w, 0, (i + 1) * w, st.height))
def sheet(items, title, fn):
    rows = [(label, ps, max(p.height for p in ps) + 24, sum(p.width + 8 for p in ps) + 8) for label, ps in items]
    W = max(max(r[3] for r in rows), 420); H = sum(r[2] for r in rows) + 26
    s = Image.new('RGB', (W, H), (34, 34, 40)); d = ImageDraw.Draw(s); d.text((6, 4), title, fill=(255, 230, 90)); y = 26
    for label, ps, rh, rw in rows:
        d.text((6, y + 2), label, fill=(230, 230, 230)); x = 8
        for p in ps: s.paste(p, (x, y + 20), p); x += p.width + 8
        y += rh
    s.save(fn); print('wrote', fn, s.size)
mut_items = []
for key, (ppt, half) in All.items():
    if key.startswith('boss:') or key in ('human', 'The Specimen') or key in ('Swarm', 'Mind Weaver', 'Void Weaver'): continue
    base = key.lower().replace(' ', '_'); stand = Image.open(f'{R}/Mutants/mutant_hd_{base}.png').convert('RGBA'); ps = [panel(stand, key, ppt, False), panel(stand, key, ppt, True)]
    walk = f'{R}/Mutants/anim/{base}_walk.bytes'
    if os.path.exists(walk): ps.append(panel(strip_frame(walk, 8, 3), key, ppt, False))
    for kind in ('bite', 'melee', 'claw', 'slam', 'swipe'):
        fn = f'{R}/Mutants/anim/{base}_{kind}.bytes'; m = re.search(r'\["%s"\] = new Dictionary.*?\["%s"\] = new AttackAnim \{ Frames = (\d+), W = \d+, H = \d+, Contact = (\d+)' % (re.escape(key), kind), ATT, re.S)
        if os.path.exists(fn) and m: ps.append(panel(strip_frame(fn, int(m.group(1)), int(m.group(2))), key, ppt, False, None, key + '|' + kind)); break
    mut_items.append((f'{key}  (stand facing right | facing left | walk | strike contact)', ps))
sheet(mut_items, 'MUTANTS: hit shape over the sprite (orange = the whole body: one part, no head zone)', os.path.join(OUT, 'mutants.png'))
atlas = Image.open(f'{R}/mutant_atlas.png').convert('RGBA'); items = []
for i, n in {2: 'Swarm', 7: 'Mind Weaver', 10: 'Void Weaver'}.items():
    cell = atlas.crop((i * 16, 0, (i + 1) * 16, atlas.height)); ppt = All[n][0]; items.append((n + '  (16 px a tile; never flipped)', [panel(cell, n, ppt, False, 6)]))
sheet(items, 'SMALL MUTANTS', os.path.join(OUT, 'small_mutants.png'))
b_items = []
for key, (ppt, half) in All.items():
    if not key.startswith('boss:'): continue
    art = key[5:]; idle = f'{R}/Mutants/anim/boss_{art}_idle.bytes'; m = re.search(r'"boss_%s_idle", Frames = (\d+)' % art, BA)
    if not os.path.exists(idle) or not m: continue
    n = int(m.group(1)); f0 = strip_frame(idle, n, 0); ps = [panel(f0, key, ppt, False, 1)]
    for ak in [k for k in ATK if k.startswith(key + '|')][:4]:                       # 2026-11-22: the attack clips: the picture at their middle frame, the outline with the clip's reach added
        kind = ak.split('|')[1]; mm = re.search(r'"boss_%s_%s", Frames = (\d+)' % (art, kind), BA); fn = f'{R}/Mutants/anim/boss_{art}_{kind}.bytes'
        if mm and os.path.exists(fn): nn = int(mm.group(1)); ps.append(panel(strip_frame(fn, nn, nn // 2 + 1), key, ppt, False, 1, ak))
    b_items.append((f'{key}  (idle, then attack clips with their reach added; drawn at {ppt} px a tile)', ps))
sheet(b_items, 'CREATURE BOSSES: hit shape over the idle picture', os.path.join(OUT, 'bosses.png'))

# 2026-11-23: humans: each suit's outline in its states (stand front, crouch, lying, a swing pose), the head / torso / legs bands coloured
HI_SRC = src[src.index('HumanIndex = new'):src.index('HumanShapes = new')]; HS_SRC = src[src.index('HumanShapes = new'):src.index('HumanRunsAt')]
HINDEX = dict(re.findall(r'\["([^"]+)"\] = "(h\d+)"', HI_SRC)); HSHAPES = {}; cur = None
for line in HS_SRC.splitlines():
    m = re.match(r'\s*\["(h\d+)"\] = new float', line)
    if m: cur = m.group(1); HSHAPES[cur] = []; continue
    if cur and 'new[]' in line: HSHAPES[cur].append([float(v.rstrip('f')) for v in re.findall(r'-?[0-9.]+f', line)])
    elif cur and 'new float[0]' in line: HSHAPES[cur].append([])
import numpy as _np
def human_panel(suit, state, cell_fn, col, row, zoom=6):
    a = Image.open(f'{R}/{cell_fn}.png').convert('RGBA'); cw = a.width // 16; cell = a.crop((col * cw, row * cw, (col + 1) * cw, (row + 1) * cw))
    al = _np.array(cell)[..., 3] > 0; ys = _np.where(al.any(1))[0]
    if not len(ys): return Image.new('RGBA', (cw * zoom, cw * zoom), (0, 0, 0, 0))
    bot = ys.max(); lay = Image.new('RGBA', cell.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay); rows = HSHAPES.get(HINDEX.get(suit + '|' + state), [])
    for r_, runs in enumerate(rows):
        y = bot - r_; hgt = (r_ + 0.5) / 32.0
        col_ = (255, 40, 40, 120) if hgt >= 0.61 else (255, 150, 30, 100) if hgt >= 0.23 else (60, 220, 90, 100)
        for i in range(0, len(runs) - 1, 2): d.line([(cw / 2.0 + runs[i] * 32, y), (cw / 2.0 + runs[i + 1] * 32, y)], fill=col_)
    out = Image.alpha_composite(cell, lay); return out.resize((out.width * zoom, out.height * zoom), Image.NEAREST)
ARM = [n for n in re.findall(r'"([^"]+)"', open(os.path.join(ROOT, 'Assets/Scripts/GameData/SpriteAtlasIndex.cs')).read().split('public static readonly string[] Armor')[1].split('};')[0])]
hitems = []
for suit in ['Raider Jacket', 'Sentinel Plate Carrier', 'Garrison Heavy Armor', 'IZC Containment Exo']:
    if suit not in ARM:
        cand = [n for n in ARM if n.split()[0] in suit.split()[:1]]; suit = cand[len(cand) // 2] if cand else ARM[0]
    i = ARM.index(suit); col, row = i % 16, i // 16
    ps = [human_panel(suit, 'stand', 'armor_atlas_x2', col, row), human_panel(suit, 'crouch', 'armor_pose_crouch_x2', col, row), human_panel(suit, 'lying', 'armor_pose_lying_x2', col, row), human_panel(suit, 'swing', 'armor_pose_lunge_r_x2', col, row)]
    hitems.append((suit + '  (stand | crouch picture | lying | a lunge pose; red = head, orange = torso, green = legs)', ps))
sheet(hitems, 'HUMANS: the suit outline in each state (a crouch uses the standing outline squashed to 70%, the picture is only 2 px lower)', os.path.join(OUT, 'humans.png'))
