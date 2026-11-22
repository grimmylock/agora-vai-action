#!/usr/bin/env python3
"""Writes Assets/Scripts/GameData/HitProfiles.cs: each body's hit SILHOUETTE as a half-width per pixel row, from the sprite's own opaque pixels (Yan, 2026-11-14: "hitboxes that actually match the sprites").
Row 0 = the bottom opaque row (the feet / ground line); the half-width of a row = how far its opaque pixels reach from the sprite's centre (the unit's own position), in tiles, so a hound that
faces left or right gets the same silhouette; a one-row gap or thin row is filled from its neighbours (a running max over +-1 row). 'human' = armor_atlas_x2.png cell 0 (every suit shares the body);
mutants: the 32 px a tile mutant_hd_*.png, and the 16 px a tile cells of mutant_atlas.png (order = SpriteAtlasIndex.Mutants). Re-run after a sprite is redrawn."""
import os, numpy as np
from PIL import Image
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'); R = os.path.join(ROOT, 'Assets', 'Resources', 'Sprites')
def profile(a, ppt):
    h, w = a.shape; ys = np.where(a.any(1))[0]; top, bot = ys.min(), ys.max(); cx = w / 2.0; out = []
    for y in range(bot, top - 1, -1):
        xs = np.where(a[y])[0]; out.append(max(cx - xs.min(), xs.max() + 1 - cx) / ppt if len(xs) else 0.0)
    out = np.array(out); sm = np.maximum.reduce([out, np.append(out[1:], 0), np.insert(out[:-1], 0, 0)]); return sm, ppt
import re as _re2
E_ = {}
# 2026-11-22 (Yan: "the hitboxes do not match the sprite for a lot of the mutants, every part of the body must be damageable"): the silhouette is the UNION (per row, the widest reach) of the stand sprite, the 8 walk pictures, the 8 run pictures and the
# contact frame of each melee clip, each aligned by its own ground row and centre: a limb or a head that swings out while it walks or strikes is no longer outside the hit shape.
_ATT = open(os.path.join(ROOT, 'Assets/Scripts/GameData/MutantAttackData.cs')).read()
def profile_lr(a, ppt):
    """(left, right) reach of each row from the sprite's centre, bottom row first, smoothed over +-1 row like profile()."""
    ys = np.where(a.any(1))[0]; top, bot = ys.min(), ys.max(); cx = a.shape[1] / 2.0; L = []; Rr = []
    for y in range(bot, top - 1, -1):
        xs = np.where(a[y])[0]; L.append((cx - xs.min()) / ppt if len(xs) else 0.0); Rr.append((xs.max() + 1 - cx) / ppt if len(xs) else 0.0)
    def sm(o): o = np.array(o); return np.maximum.reduce([o, np.append(o[1:], 0), np.insert(o[:-1], 0, 0)])
    return sm(L), sm(Rr)
CENTRE = {}

# 2026-11-22 (Yan: "the hitboxes only consider the top of the mutants as the head, not the actual head from the sprite"): where the HEAD is, per body, as a box in fractions of the stand sprite's opaque bounding box (x 0 = its left edge .. 1 = its right edge in the picture facing right,
# y 0 = the feet .. 1 = the crown). Read off the pictures (maps/renders/hitboxes). Bosses: a height range only (they are not stored per side). Tiles come out of it: HitProfiles.Head[key] = (x0, x1, yLo, yHi), x from the unit's centre, y above the feet.
HEAD_FRAC = {'Pallid Prowler': (0.80, 1.0, 0.23, 0.68), 'Blind Maw': (0.65, 1.0, 0.20, 0.78), 'Veil Prowler': (0.82, 1.0, 0.35, 0.72), 'Dread Prowler': (0.78, 1.0, 0.36, 0.74), 'Blight Brute': (0.43, 0.72, 0.32, 0.63),
             'Gaunt': (0.60, 1.0, 0.56, 1.0), 'Elder Gaunt': (0.65, 1.0, 0.64, 1.0), 'Ward Wretch': (0.30, 0.72, 0.83, 1.0), 'Ward Hulk': (0.35, 0.78, 0.72, 1.0), 'Ward Crawler': (0.48, 0.85, 0.60, 1.0)}   # (the Wards: stored mirrored, they are drawn facing left)
BOSS_HEAD = {'boss:mother_hollow': (0.10, 0.60), 'boss:ferryman': (0.30, 0.78), 'boss:grinder': (0.55, 1.0), 'boss:orderly': (0.12, 0.42), 'boss:choir': (0.70, 1.0), 'boss:specimen_v2': (0.55, 1.0), 'boss:specimen_eye': (0.0, 1.0)}
HEAD = {}
def add_head(name, stand_alpha, ppt):
    if name not in HEAD_FRAC: return
    a = stand_alpha; ys = np.where(a.any(1))[0]; xs = np.where(a.any(0))[0]; top, bot = ys.min(), ys.max(); cx = a.shape[1] / 2.0
    x0f, x1f, y0f, y1f = HEAD_FRAC[name]; xmin, xmax = xs.min(), xs.max() + 1; ht = (bot - top + 1)
    x0 = (xmin + x0f * (xmax - xmin) - cx) / ppt; x1 = (xmin + x1f * (xmax - xmin) - cx) / ppt
    if name.startswith('Ward '): x0, x1 = -x1, -x0
    HEAD[name] = (x0, x1, y0f * ht / ppt, y1f * ht / ppt)
def union_profile(k, name, ppt):
    stand = np.array(Image.open(f'{R}/Mutants/mutant_hd_{k}.png').convert('RGBA'))[..., 3] > 0
    add_head(name, stand, ppt)
    bl, br = profile_lr(stand, ppt); bl, br = list(bl), list(br)
    cuts = []
    for kind in ('walk', 'run'):
        fn = f'{R}/Mutants/anim/{k}_{kind}.bytes'
        if os.path.exists(fn):
            st = np.array(Image.open(fn).convert('RGBA'))[..., 3] > 0
            if st.shape[1] % 8 == 0: cuts += [st[:, i * (st.shape[1] // 8):(i + 1) * (st.shape[1] // 8)] for i in range(8)]
    for kind in ('bite', 'melee', 'claw', 'swipe', 'slam'):
        fn = f'{R}/Mutants/anim/{k}_{kind}.bytes'
        m = _re2.search(r'\["%s"\] = new Dictionary.*?\["%s"\] = new AttackAnim \{ Frames = (\d+), W = \d+, H = \d+, Contact = (\d+)' % (_re2.escape(name), kind), _ATT, _re2.S)
        if os.path.exists(fn) and m:
            st = np.array(Image.open(fn).convert('RGBA'))[..., 3] > 0; n, c = int(m.group(1)), int(m.group(2))
            if st.shape[1] % n == 0: cuts.append(st[:, c * (st.shape[1] // n):(c + 1) * (st.shape[1] // n)])
    for a in cuts:
        if not a.any(): continue
        l, r = profile_lr(a, ppt)
        for i in range(len(l)):
            if i < len(bl): bl[i] = max(bl[i], l[i]); br[i] = max(br[i], r[i])
            else: bl.append(l[i]); br.append(r[i])
    bl, br = np.array(bl), np.array(br)
    if name.startswith('Ward '): bl, br = br, bl                       # drawn facing LEFT: stored as if facing right, like every other mutant (UnitSprites.DrawnLeft)
    CENTRE[name] = (br - bl) / 2.0                                        # + = the sprite reaches further to the right than to the left, in the unflipped (facing right) picture
    return (bl + br) / 2.0, ppt
at = np.array(Image.open(f'{R}/armor_atlas_x2.png').convert('RGBA'))[..., 3] > 0; E_['human'] = profile(at[0:32, 0:32], 32)
for k, n in [('pallid_prowler', 'Pallid Prowler'), ('blind_maw', 'Blind Maw'), ('veil_prowler', 'Veil Prowler'), ('dread_prowler', 'Dread Prowler'), ('blight_brute', 'Blight Brute'), ('gaunt', 'Gaunt'), ('elder_gaunt', 'Elder Gaunt'), ('ward_wretch', 'Ward Wretch'), ('ward_hulk', 'Ward Hulk'), ('ward_crawler', 'Ward Crawler')]:
    E_[n] = union_profile(k, n, 43 if k in ('gaunt', 'elder_gaunt') else 32)   # 2026-11-17: the Gaunts are drawn at 32/43 of their art (Gaunt.ArtScale)
ma = np.array(Image.open(f'{R}/mutant_atlas.png').convert('RGBA'))[..., 3] > 0
for i, n in {2: 'Swarm', 7: 'Mind Weaver', 10: 'Void Weaver'}.items(): E_[n] = profile(ma[:, i * 16:(i + 1) * 16], 16)
# bosses (2026-11-14): the brain in its vat (the sheet's first frame; the sprite's bottom is 10 px under the tile's middle, the model's ground line 16 px, so 6 base rows are added) and the eel's body: the thickness of
# the baked body strip (boss_eel_body_s0.png, 8 px a spine sample, 32 px a tile) per spine sample, half of it = the column's radius; the head zone is the head picture's height.
vat = np.array(Image.open(f'{R}/Mutants/boss_brain_vat.png').convert('RGBA'))[..., 3] > 0; p_, ppt_ = profile(vat[0:160, 0:128], 32); E_['The Specimen'] = (np.concatenate([np.full(6, p_[0]), p_]), ppt_)
# 2026-11-21: the creature bosses drawn from Pixel Lab clips (BossArt.cs): the idle clip's first frame, at the scale they are drawn (BossArt.Scales: 32 px a tile / scale); key "boss:<art key>"
import re as _re, io as _io
_ba = open(os.path.join(ROOT, 'Assets/Scripts/GameCore/BossArt.cs')).read(); _sc = dict((k, float(v)) for k, v in _re.findall(r'\["(\w+)"\] = ([0-9.]+)f', _ba[_ba.index('Scales'):_ba.index('Scales') + 400]))
for _k, _s in _sc.items():
    _m = _re.search(r'"boss_%s_idle", Frames = (\d+)' % _k, _ba); _f = os.path.join(R, 'Mutants', 'anim', 'boss_%s_idle.bytes' % _k)
    if not _m or not os.path.exists(_f): continue
    _im = np.array(Image.open(_f).convert('RGBA'))[..., 3] > 0; _w = _im.shape[1] // int(_m.group(1))
    E_['boss:' + _k] = profile(_im[:, 0:_w], int(round(32 / _s)))
eb = np.array(Image.open(f'{R}/Fx/boss_eel_body_s0.png').convert('RGBA'))[..., 3] > 0; EEL = [max(0.12, float(np.median(eb[:, i * 8:i * 8 + 8].sum(0))) / 2 / 32) for i in range(140)]
eh = np.array(Image.open(f'{R}/Fx/boss_eel_head_s0.png').convert('RGBA'))[..., 3] > 0; EELHEAD = float(eh.sum(0).max()) / 2 / 32
for _bk, (_lo, _hi) in BOSS_HEAD.items():
    if _bk in E_: _h = len(E_[_bk][0]) / E_[_bk][1]; HEAD[_bk] = (-99.0, 99.0, _lo * _h, _hi * _h)
L = ['// GENERATED by ArtSource/make_hit_profiles.py - do not edit. A body\'s hit silhouette: half-width (tiles) per pixel row from the feet up, taken from its sprite.',
     'using System.Collections.Generic;', '', 'namespace GameData', '{', '    public static class HitProfiles', '    {',
     '        /// <summary>Pixels a tile in the sprite the profile was cut from (32 for the double-resolution art, 16 for the old atlas cells).</summary>', '        public static readonly Dictionary<string, (int ppt, float[] half)> All = new Dictionary<string, (int, float[])>', '        {']
for n, (p, ppt) in E_.items(): L.append(f'            ["{n}"] = ({ppt}, new[] {{ ' + ', '.join(f'{v:.3f}f' for v in p) + ' }),')
L += ['        };', '', '        /// <summary>2026-11-22 (Yan: the hit shape follows which way the mutant faces): per row, how far the middle of the silhouette lies to the RIGHT of the unit\'s centre in the unflipped (facing right) picture; mirror it for a mutant drawn flipped.</summary>', '        public static readonly Dictionary<string, float[]> Centre = new Dictionary<string, float[]>', '        {'] + [f'            ["{n}"] = new[] {{ ' + ', '.join(f'{v:.3f}f' for v in c) + ' },' for n, c in CENTRE.items()] + ['        };', '', '        public static float CentreOf(string key, float height)', '        {', '            if (key == null || !Centre.TryGetValue(key, out var c) || !All.TryGetValue(key, out var p) || height < 0f) return 0f;', '            int row = (int)(height * p.ppt); return row >= c.Length ? 0f : c[row];', '        }', '', '        /// <summary>The silhouette\'s half-width at `height` tiles above the feet, or -1 above the crown or for a body without a profile.</summary>',
      '        public static float Half(string key, float height)', '        {', '            if (key == null || !All.TryGetValue(key, out var p) || height < 0f) return -1f;',
      '            int row = (int)(height * p.ppt); if (row >= p.half.Length) return -1f;', '            return p.half[row];', '        }', '',
      '        /// <summary>How deep a body is front to back as a cap on its half-width (tiles): a hound is long on screen but thin, so a round that comes along its length sees only this much of it; 9 = no cap.</summary>',
      '        public static float DepthCap(string key) => key == "Pallid Prowler" || key == "Blind Maw" || key == "Veil Prowler" || key == "Dread Prowler" ? 0.38f : key == "Blight Brute" ? 0.6f : key == "boss:mother_hollow" ? 0.7f : key == "boss:excavator" ? 1.6f : 9f;', '',
      '        /// <summary>The eel\'s body: the radius (tiles) of its hit column at each of the 140 spine samples, from the thickness of its drawn body (so it tapers to the tail); the head zone\'s radius.</summary>',
      '        public static readonly float[] EelBody = new[] { ' + ', '.join(f'{v:.3f}f' for v in EEL) + ' };', f'        public const float EelHeadHalf = {EELHEAD:.3f}f;', '',
      '        /// <summary>The silhouette\'s height in tiles (feet to crown), 0 for a body without one.</summary>', '        public static float Height(string key) => key != null && All.TryGetValue(key, out var p) ? p.half.Length / (float)p.ppt : 0f;', '    }', '}', '']
_txt = '\n'.join(L)
_extra = ['        /// <summary>2026-11-22 (Yan: the head is where the head is drawn, not the top 30%): (x0, x1, yLo, yHi) of the head, x from the unit\'s centre in the picture facing right (+-99 = any x), y above the feet, tiles. ArtSource/make_hit_profiles.py HEAD_FRAC.</summary>',
          '        public static readonly Dictionary<string, (float x0, float x1, float lo, float hi)> Head = new Dictionary<string, (float, float, float, float)>', '        {'] + [f'            ["{n}"] = ({v[0]:.3f}f, {v[1]:.3f}f, {v[2]:.3f}f, {v[3]:.3f}f),' for n, v in HEAD.items()] + ['        };', '',
          '        /// <summary>Is the point (x from the centre, height above the feet) on the head? `flipped`: the mutant is drawn mirrored. Bodies without a head box: the old rule, the top 30%.</summary>',
          '        public static bool HasHead(string key) => key != null && Head.ContainsKey(key);', '', '        public static bool OnHead(string key, bool flipped, float x, float height)', '        {',
          '            if (key == null || !Head.TryGetValue(key, out var h)) return false; if (flipped) x = -x;', '            return height >= h.lo && height <= h.hi && x >= h.x0 && x <= h.x1;', '        }',
          '        public static bool InHeadRows(string key, float height) => key != null && Head.TryGetValue(key, out var h) && height >= h.lo && height <= h.hi;', '']
_txt = _txt.replace("        /// <summary>The silhouette's height in tiles (feet to crown)", '\n'.join(_extra) + "\n        /// <summary>The silhouette's height in tiles (feet to crown)", 1)
open(os.path.join(ROOT, 'Assets/Scripts/GameData/HitProfiles.cs'), 'w').write(_txt)
for n, (p, ppt) in E_.items(): print(f'{n:16s} {len(p)} rows ({len(p)/ppt:.2f} tiles) half {p.min():.2f}..{p.max():.2f}')
