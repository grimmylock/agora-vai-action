# Weapon tuning in real time (2026-11-17)

Yan: every gun individually, not one global knob; a 1-against-1 where every round lands should take about 2-4 s when the gun's tier matches the armor's tier (tier 1 vs tier 1 ... tier 5 vs tier 5); some guns are better up close, some at range, in every class.

**How it works** (`Gunplay.Tune`, one row per gun: `(rate, vsHuman)`): `rate` scales the old rounds-per-minute of automatics (600-950 rpm made a rifle kill in half a second); `vsHuman` scales the damage a round does to players and human enemies only. Mutants and bosses keep their old balance (a gun does `1/rate` times its old damage to them, so its damage per second against mutants is unchanged). A human dies when his torso pool (80% of HP) is gone, so "kill" = 800 torso damage after armor. Retune a gun by editing its row, then run the harness (it checks every gun lands in 1.6-4.8 s).

**Range roles** come from each gun's own handling (`Gunplay.Guns`: steadied spread, kick, steady-up time) together with its target time: close guns have a wide spread and a short kill time (they win at 6 tiles and fall off), long guns a tight spread and a longer kill time (they only pay off at range). The "hit%" columns are measured with real rounds at 6, 15 and 30 tiles (stationary shooter, best of hold/burst).

| gun | class | tier | rate | vsHuman | rounds to kill (same-tier armor) | all-hits kill time s | steady deg | kick | hit% 6t | hit% 15t | hit% 30t |
|-|-|-|-|-|-|-|-|-|-|-|-|
| PSh-18 Pchela | sidearm | 1 | 1.00 | 0.870 | 9 | 3.8 | 1.40 | 2.2 | 100 | 79 | 43 |
| Walden P9 | sidearm | 2 | 1.00 | 0.693 | 11 | 2.5 | 1.10 | 2.2 | 100 | 91 | 55 |
| Halden .45 Service | sidearm | 2 | 1.00 | 0.830 | 8 | 3.1 | 1.00 | 3.0 | 100 | 90 | 58 |
| Halden Service Revolver | sidearm | 1 | 1.00 | 1.117 | 5 | 1.6 | 1.00 | 3.2 | 100 | 90 | 58 |
| SR-21 Gadyuka | sidearm | 3 | 1.00 | 0.620 | 11 | 2.5 | 1.10 | 2.6 | 100 | 91 | 55 |
| Black Harrier | sidearm | 3 | 1.00 | 0.620 | 9 | 2.0 | 0.72 | 3.2 | 100 | 98 | 77 |
| Corvan 5.7 | sidearm | 4 | 1.00 | 0.660 | 13 | 3.0 | 1.00 | 1.8 | 100 | 90 | 58 |
| Sperling S-96 | sidearm | 4 | 1.00 | 0.690 | 11 | 4.4 | 0.95 | 3.0 | 100 | 94 | 60 |
| Rinaldi R6 | sidearm | 4 | 1.00 | 1.210 | 6 | 1.6 | 0.80 | 3.4 | 100 | 98 | 72 |
| Steppe Eagle | sidearm | 4 | 1.00 | 0.792 | 8 | 2.1 | 0.90 | 6.0 | 87 | 43 | 15 |
| RK-12 Kuvalda | sidearm | 5 | 1.00 | 1.680 | 4 | 2.4 | 0.80 | 7.5 | 96 | 97 | 72 |
| Vosa vz. 62 | smg | 1 | 0.80 | 0.480 | 20 | 1.7 | 2.20 | 1.1 | 96 | 55 | 19 |
| PPZ Zubr | smg | 2 | 0.80 | 0.404 | 25 | 2.6 | 1.80 | 1.2 | 98 | 66 | 33 |
| Corvan Wasp-9 | smg | 3 | 0.80 | 0.430 | 24 | 2.2 | 1.40 | 1.2 | 100 | 80 | 43 |
| Corvan Hornet .45 | smg | 3 | 0.80 | 0.440 | 22 | 2.6 | 1.60 | 2.0 | 99 | 72 | 33 |
| Corvan Gnat PDW | smg | 4 | 0.80 | 0.440 | 29 | 2.2 | 1.50 | 1.0 | 91 | 75 | 28 |
| VKS-74U | assault | 1 | 0.80 | 0.240 | 22 | 2.3 | 1.10 | 2.2 | 94 | 62 | 28 |
| Smerch-3 | assault | 2 | 0.80 | 0.192 | 22 | 2.0 | 0.90 | 2.3 | 91 | 61 | 32 |
| VK-74 | assault | 2 | 0.70 | 0.220 | 22 | 2.9 | 0.70 | 1.7 | 100 | 96 | 69 |
| VKM | assault | 2 | 0.70 | 0.230 | 19 | 2.6 | 0.80 | 2.4 | 97 | 74 | 42 |
| TS-9 Tishina | assault | 3 | 0.65 | 0.220 | 20 | 2.1 | 0.75 | 2.0 | 97 | 72 | 43 |
| TR-30 | assault | 3 | 0.65 | 0.220 | 23 | 2.9 | 0.60 | 1.7 | 100 | 96 | 70 |
| Halden HR36K | assault | 3 | 0.65 | 0.220 | 23 | 2.8 | 0.65 | 1.6 | 100 | 97 | 69 |
| VK-103 | assault | 3 | 0.65 | 0.240 | 20 | 2.9 | 0.70 | 2.3 | 99 | 80 | 48 |
| BR-86 Bulldog | assault | 4 | 0.60 | 0.185 | 29 | 4.3 | 0.55 | 1.5 | 100 | 100 | 91 |
| VK-74 Obelisk | assault | 4 | 0.60 | 0.250 | 23 | 3.4 | 0.50 | 1.6 | 100 | 100 | 90 |
| NA-94 Yenisei | assault | 4 | 0.60 | 0.320 | 21 | 3.3 | 0.55 | 1.4 | 100 | 100 | 91 |
| Corvan Bullpup | assault | 5 | 0.65 | 0.212 | 32 | 6.1 | 0.55 | 1.6 | 60 | 96 | 71 |
| Corvan Battle Rifle | assault | 4 | 0.60 | 0.267 | 18 | 2.8 | 0.50 | 3.0 | 95 | 71 | 45 |
| Obrez-66 | shotgun | 1 | 1.00 | 0.700 | 3 | 2.6 | 1.80 | 8.0 | 85 | 39 | 7 |
| Halden Lever Shotgun | shotgun | 2 | 1.00 | 0.580 | 4 | 2.6 | 1.40 | 6.0 | 100 | 77 | 35 |
| Hound 13 | shotgun | 2 | 1.00 | 0.590 | 4 | 2.4 | 1.30 | 6.0 | 100 | 90 | 45 |
| Ranger P8 | shotgun | 2 | 1.00 | 0.590 | 4 | 2.3 | 1.20 | 6.0 | 100 | 91 | 43 |
| Karusel-5 | shotgun | 3 | 1.00 | 0.570 | 5 | 1.8 | 1.30 | 6.0 | 98 | 68 | 25 |
| Bayou Pump | shotgun | 3 | 1.00 | 0.510 | 5 | 3.2 | 1.20 | 6.0 | 100 | 91 | 43 |
| Sever-34 O/U | shotgun | 3 | 1.00 | 0.810 | 3 | 3.0 | 1.00 | 7.0 | 94 | 62 | 23 |
| Tambour-12 | shotgun | 4 | 1.00 | 0.380 | 8 | 2.3 | 1.50 | 6.5 | 88 | 50 | 15 |
| SP-14 Warden | shotgun | 4 | 1.00 | 0.370 | 8 | 2.1 | 1.30 | 5.5 | 90 | 47 | 14 |
| Sayan-12 | shotgun | 4 | 1.00 | 0.380 | 8 | 2.3 | 1.50 | 6.0 | 88 | 50 | 15 |
| Kaban-12 | shotgun | 4 | 1.00 | 0.330 | 9 | 2.4 | 1.30 | 5.5 | 90 | 47 | 14 |
| SKN Karabin | sniper | 2 | 1.00 | 0.290 | 7 | 3.0 | 0.23 | 3.4 | 82 | 100 | 100 |
| Frontier Lever Rifle | sniper | 3 | 1.00 | 0.430 | 5 | 3.4 | 0.25 | 5.0 | 52 | 100 | 45 |
| VSN Shepot | sniper | 3 | 1.00 | 0.220 | 9 | 3.2 | 0.24 | 3.5 | 100 | 100 | 99 |
| M91 Trekhlineyka | sniper | 3 | 1.00 | 0.494 | 4 | 4.5 | 0.10 | 7.0 | 84 | 100 | 100 |
| Nightwalker .45-70 | sniper | 4 | 1.00 | 0.530 | 4 | 3.3 | 0.20 | 8.0 | 100 | 100 | 100 |
| Corvan Marksman | sniper | 4 | 1.00 | 0.310 | 7 | 3.3 | 0.12 | 5.0 | 95 | 86 | 51 |
| SV-2 Strela | sniper | 4 | 1.00 | 0.260 | 7 | 3.6 | 0.11 | 4.5 | 82 | 100 | 100 |
| Sever-98 | sniper | 4 | 1.00 | 0.450 | 4 | 5.0 | 0.08 | 7.0 | 86 | 100 | 100 |
| VK-127 Grom | sniper | 5 | 1.00 | 0.580 | 4 | 6.0 | 0.10 | 9.0 | 100 | 100 | 100 |
| Halden AM-50 Sledge | sniper | 5 | 1.00 | 0.430 | 5 | 3.4 | 0.10 | 13.5 | 100 | 100 | 100 |
| G-7 Rail Prototype | sniper | 5 | 1.00 | 0.530 | 3 | 3.0 | 0.05 | 4.0 | 100 | 100 | 100 |
| PL-62 Kobza | heavy | 2 | 0.65 | 0.230 | 23 | 3.1 | 1.40 | 2.2 | 50 | 21 | 5 |
| Corvan LMG | heavy | 3 | 0.65 | 0.180 | 28 | 3.3 | 1.30 | 1.9 | 93 | 56 | 22 |
| RL-44 | heavy | 3 | 0.65 | 0.220 | 23 | 3.4 | 1.20 | 2.4 | 50 | 22 | 5 |
| PL-74 | heavy | 4 | 0.60 | 0.220 | 24 | 3.5 | 0.88 | 2.6 | 53 | 26 | 6 |
| PL-74P Polovets | heavy | 4 | 0.60 | 0.311 | 17 | 2.5 | 0.80 | 2.5 | 55 | 26 | 6 |
| PL-74P Polovets Zulu | heavy | 5 | 0.60 | 0.310 | 26 | 3.8 | 0.80 | 2.4 | 55 | 28 | 9 |

## Yan's sheet edits (2026-11-17)

Yan took `WEAPON_TIERLIST_RT.xlsx`, changed numbers and added a column of notes per gun (his file is kept as `WEAPON_TIERLIST_RT_YAN.xlsx`). Applied in `Gunplay.cs`:
- **Reach** (class tables): sidearms 22 -> 40 tiles, SMGs 24 -> 50, shotguns 14 -> 30, snipers 70 -> 100 (rifles and heavy MGs stay 40).
- **Fire rate** (sidearms): Corvan 5.7 380 -> 300, SR-21 340 -> 290, Walden P9 360 -> 250, Sperling S-96 330 -> 300, Halden .45 300 -> 290, Rinaldi R6 130 -> 190, PSh-18 300 -> 200, Black Harrier 260 -> 250, Halden Service Revolver 120 -> 150.
- **Reload / magazine**: Walden P9 1.2 -> 1.6 s, PPZ Zubr 2.4 -> 2.6, PL-74P Polovets and Zulu 4.5 -> 3.9, Halden Lever Shotgun 3.0 -> 2.2 s and magazine 5 -> 8.
- **Damage per round** (`vsHuman` rows): Walden P9 98 -> 104, Halden Service Revolver 157 -> 201, PPZ Zubr 46 -> 42, Corvan Bullpup 89 -> 70, BR-86 Bulldog 68 -> 50, Smerch-3 44 -> 50, Corvan Battle Rifle 72 -> 80, PL-74P Polovets 62 -> 87.
- **His notes**: "tighten/tighter spread" (my numbers: steadied spread -20% for Black Harrier, VSN Shepot, Corvan Marksman, PL-74, PL-74P Polovets; -25% spread and -25% kick for SV-2 Strela and SKN Karabin: "less bloom when firing"); VK-127 Grom "slower fire-rate and bullet velocity" (40 -> 30 rpm, 105 -> 80 tiles/s); Halden AM-50 Sledge "raise recoil" (kick 11 -> 13.5); Sever-98 "longer reload and slower fire-rate" (2.8 -> 3.4 s, 45 -> 36 rpm); G-7 "keep as is" (only the penetration).
- **Penetration (new mechanic, `Gunplay.Pierce`)**: a round passes through bodies and keeps flying, each body after the first taking 75% of what the one before did (G-7: 90%); walls, cover and a deflecting boss still stop it. RK-12 Kuvalda and Steppe Eagle 2 targets (3 with armor-piercing); SKN Karabin, VSN Shepot, Frontier Lever Rifle, M91, Nightwalker, SV-2 Strela 2; Corvan Marksman, Sever-98, VK-127 Grom, AM-50 Sledge 3; PL-74P Polovets Zulu 2; G-7 all. It applies to enemies with those guns too.
- **Lever guns (`Gunplay.HipRate`)**: Nightwalker .45-70, Frontier Lever Rifle and Halden Lever Shotgun fire three times as fast when not aiming ("can be levered when hip-fired, hip fire speed +200%").
- His edits push four guns outside the 2-4 s same-tier target (Corvan Bullpup 6.1 s, VK-127 Grom 6.0, Sever-98 5.0, Rinaldi R6 1.6): left as he set them; the harness band is now 1.5-6.5 s.
