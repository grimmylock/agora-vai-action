"""Hand points (2026-11-16): grip and support rows for the double-size pictures, read off Tools/render/hand_points_sheet.py (grid in virtual px).
Rules: the support hand's centre sits ~0.8 virtual px above the underside of the fore-end/handguard (the glove wraps it); the grip hand on the pistol grip's upper-middle (stock wrist for lever/bolt/pump guns).
Usage: python3 ArtSource/weapons/apply_hand_points.py   (rewrites the first four numbers of each listed row in HeldWeapon.cs; muzzle untouched)"""
import re
P = 'Assets/Scripts/GameCore/HeldWeapon.cs'
NEW = {  # key: (grip x, grip y, sup x, sup y)  (sup None = one-hander)
 'HaldenLeverShotgun': (11.4, 5.5, 25.6, 2.6), 'FrontierLeverRifle': (11.2, 4.8, 25.5, 2.6), 'Nightwalker4570': (11.2, 5.0, 27.0, 3.2),
 'VK74': (14.2, 9.0, 28.5, 4.4), 'RangerP8': (12.0, 5.2, 29.5, 3.6), 'SKNKarabin': (13.0, 5.6, 28.0, 4.1), 'CorvanWasp9': (14.2, 9.6, 26.5, 5.4),
 'G7RailPrototype': (12.8, 7.0, 30.0, 4.8), 'Smerch3': (13.0, 5.9, 24.5, 2.9), 'PL74PPolovetsZulu': (12.8, 8.3, 30.0, 5.2), 'PL74PPolovets': (13.0, 7.2, 29.5, 4.2),
 'BR86Bulldog': (15.2, 9.6, 26.5, 5.9), 'Sever98': (13.0, 5.6, 23.0, 3.3), 'PPZZubr': (9.8, 7.0, 19.0, 5.2), 'TR30': (11.7, 7.5, 26.5, 4.6),
 'VKS74U': (12.4, 7.0, 26.5, 3.7), 'PL62Kobza': (15.0, 7.4, 31.0, 5.5), 'Obrez66': (4.8, 4.0, 17.0, 2.6), 'RK12Kuvalda': (4.4, 8.8, None, None),
 'VK74Obelisk': (13.4, 7.4, 28.5, 3.3), 'Hound13': (12.7, 4.0, 28.5, 2.7), 'SP14Warden': (12.0, 4.7, 28.0, 4.0), 'HaldenHR36K': (11.9, 8.4, 28.6, 4.1),
 'M91Trekhlineyka': (13.3, 5.7, 30.0, 3.2), 'Kaban12': (11.9, 5.8, 27.0, 3.3), 'Karusel5': (11.4, 4.6, 23.5, 2.2), 'VK127Grom': (15.8, 5.9, 25.9, 7.0),
 'Tambour12': (16.6, 6.0, 33.2, 5.0), 'VSNShepot': (14.6, 5.4, 28.0, 2.7), 'Sayan12': (15.2, 7.2, 27.0, 3.0), 'TS9Tishina': (13.0, 6.6, 27.0, 2.2),
 'CorvanHornet45': (13.0, 7.4, 28.5, 4.4), 'VosaVz62': (10.9, 6.2, None, None), 'Sever34': (13.8, 4.4, 25.0, 2.4), 'HaldenAM50Sledge': (12.3, 7.5, 29.8, 8.2),
 'SV2Strela': (11.3, 5.8, 27.5, 3.2), 'CorvanMarksman': (12.5, 7.4, 28.8, 7.4), 'CorvanLMG': (14.0, 10.8, 28.0, 7.7), 'PL74': (13.2, 8.0, 34.3, 8.2),
 'VK103': (13.8, 7.3, 29.0, 3.6), 'CorvanBullpup': (12.0, 8.2, 23.0, 7.2), 'RL44': (12.3, 7.0, 25.0, 4.6), 'NA94Yenisei': (13.2, 8.0, 28.0, 4.3),
 'VKM': (14.0, 7.2, 28.0, 3.7), 'CorvanBattleRifle': (13.6, 7.2, 28.0, 4.2), 'CorvanGnatPDW': (17.6, 7.2, 25.3, 6.2),
}
s = open(P).read(); n = 0
def f(v): return 'float.NaN' if v is None else (f'{v:g}' if v != int(v) else f'{int(v)}') + 'f'
for k, (gx, gy, sx, sy) in NEW.items():
    m = re.search(r'(\["%s"\] = new\[\] \{ )([^}]*)(\})' % k, s)
    if not m: print('MISSING', k); continue
    parts = [p.strip() for p in m.group(2).split(',')]
    parts[0], parts[1] = f(gx), f(gy)
    if sx is not None: parts[2], parts[3] = f(sx), f(sy)
    s = s[:m.start(2)] + ', '.join(parts) + ' ' + s[m.end(2):]; n += 1
open(P, 'w').write(s); print(n, 'rows rewritten')
