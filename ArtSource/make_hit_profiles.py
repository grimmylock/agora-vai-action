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
def union_profile(k, name, ppt):
    stand = np.array(Image.open(f'{R}/Mutants/mutant_hd_{k}.png').convert('RGBA'))[..., 3] > 0
    best, _ = profile(stand, ppt); best = list(best)
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
        p, _ = profile(a, ppt)
        for i, v in enumerate(p):
            if i < len(best): best[i] = max(best[i], v)
            else: best.append(v)
    return np.array(best), ppt
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
L = ['// GENERATED by ArtSource/make_hit_profiles.py - do not edit. A body\'s hit silhouette: half-width (tiles) per pixel row from the feet up, taken from its sprite.',
     'using System.Collections.Generic;', '', 'namespace GameData', '{', '    public static class HitProfiles', '    {',
     '        /// <summary>Pixels a tile in the sprite the profile was cut from (32 for the double-resolution art, 16 for the old atlas cells).</summary>', '        public static readonly Dictionary<string, (int ppt, float[] half)> All = new Dictionary<string, (int, float[])>', '        {']
for n, (p, ppt) in E_.items(): L.append(f'            ["{n}"] = ({ppt}, new[] {{ ' + ', '.join(f'{v:.3f}f' for v in p) + ' }),')
L += ['        };', '', '        /// <summary>The silhouette\'s half-width at `height` tiles above the feet, or -1 above the crown or for a body without a profile.</summary>',
      '        public static float Half(string key, float height)', '        {', '            if (key == null || !All.TryGetValue(key, out var p) || height < 0f) return -1f;',
      '            int row = (int)(height * p.ppt); if (row >= p.half.Length) return -1f;', '            return p.half[row];', '        }', '',
      '        /// <summary>How deep a body is front to back as a cap on its half-width (tiles): a hound is long on screen but thin, so a round that comes along its length sees only this much of it; 9 = no cap.</summary>',
      '        public static float DepthCap(string key) => key == "Pallid Prowler" || key == "Blind Maw" || key == "Veil Prowler" || key == "Dread Prowler" ? 0.38f : key == "Blight Brute" ? 0.6f : key == "boss:mother_hollow" ? 0.7f : key == "boss:excavator" ? 1.6f : 9f;', '',
      '        /// <summary>The eel\'s body: the radius (tiles) of its hit column at each of the 140 spine samples, from the thickness of its drawn body (so it tapers to the tail); the head zone\'s radius.</summary>',
      '        public static readonly float[] EelBody = new[] { ' + ', '.join(f'{v:.3f}f' for v in EEL) + ' };', f'        public const float EelHeadHalf = {EELHEAD:.3f}f;', '',
      '        /// <summary>The silhouette\'s height in tiles (feet to crown), 0 for a body without one.</summary>', '        public static float Height(string key) => key != null && All.TryGetValue(key, out var p) ? p.half.Length / (float)p.ppt : 0f;', '    }', '}', '']
open(os.path.join(ROOT, 'Assets/Scripts/GameData/HitProfiles.cs'), 'w').write('\n'.join(L))
for n, (p, ppt) in E_.items(): print(f'{n:16s} {len(p)} rows ({len(p)/ppt:.2f} tiles) half {p.min():.2f}..{p.max():.2f}')
