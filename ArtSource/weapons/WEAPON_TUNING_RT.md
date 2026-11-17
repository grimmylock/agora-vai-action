# Weapon tuning in real time (2026-11-17)

Yan: every gun individually, not one global knob; a 1-against-1 where every round lands should take about 2-4 s when the gun's tier matches the armor's tier (tier 1 vs tier 1 ... tier 5 vs tier 5); some guns are better up close, some at range, in every class.

**How it works** (`Gunplay.Tune`, one row per gun: `(rate, vsHuman)`): `rate` scales the old rounds-per-minute of automatics (600-950 rpm made a rifle kill in half a second); `vsHuman` scales the damage a round does to players and human enemies only. Mutants and bosses keep their old balance (a gun does `1/rate` times its old damage to them, so its damage per second against mutants is unchanged). A human dies when his torso pool (80% of HP) is gone, so "kill" = 800 torso damage after armor. Retune a gun by editing its row, then run the harness (it checks every gun lands in 1.6-4.8 s).

**Range roles** come from each gun's own handling (`Gunplay.Guns`: steadied spread, kick, steady-up time) together with its target time: close guns have a wide spread and a short kill time (they win at 6 tiles and fall off), long guns a tight spread and a longer kill time (they only pay off at range). The "hit%" columns are measured with real rounds at 6, 15 and 30 tiles (stationary shooter, best of hold/burst).

| gun | class | tier | rate | vsHuman | rounds to kill (same-tier armor) | all-hits kill time s | steady deg | kick | hit% 6t | hit% 15t | hit% 30t |
|-|-|-|-|-|-|-|-|-|-|-|-|
| PSh-18 Pchela | sidearm | 1 | 1.00 | 0.87 | 9 | 3.4 | 1.40 | 2.2 | 100 | 79 | 0 |
| Walden P9 | sidearm | 2 | 1.00 | 0.65 | 11 | 2.5 | 1.10 | 2.2 | 100 | 91 | 0 |
| Halden .45 Service | sidearm | 2 | 1.00 | 0.83 | 8 | 3.1 | 1.00 | 3.0 | 100 | 90 | 0 |
| Halden Service Revolver | sidearm | 1 | 1.00 | 0.87 | 6 | 2.5 | 1.00 | 3.2 | 100 | 90 | 0 |
| SR-21 Gadyuka | sidearm | 3 | 1.00 | 0.62 | 11 | 2.5 | 1.10 | 2.6 | 100 | 91 | 0 |
| Black Harrier | sidearm | 3 | 1.00 | 0.62 | 9 | 2.0 | 0.90 | 4.4 | 80 | 30 | 0 |
| Corvan 5.7 | sidearm | 4 | 1.00 | 0.66 | 13 | 3.0 | 1.00 | 1.8 | 100 | 90 | 0 |
| Sperling S-96 | sidearm | 4 | 1.00 | 0.69 | 11 | 4.4 | 0.95 | 3.0 | 100 | 94 | 0 |
| Rinaldi R6 | sidearm | 4 | 1.00 | 1.21 | 6 | 2.3 | 0.80 | 3.4 | 100 | 98 | 0 |
| Steppe Eagle | sidearm | 4 | 1.00 | 0.72 | 9 | 4.0 | 0.90 | 6.0 | 87 | 43 | 0 |
| RK-12 Kuvalda | sidearm | 5 | 1.00 | 1.68 | 4 | 2.4 | 0.80 | 7.5 | 96 | 97 | 0 |
| Vosa vz. 62 | smg | 1 | 0.80 | 0.48 | 20 | 1.7 | 2.20 | 1.1 | 96 | 55 | 0 |
| PPZ Zubr | smg | 2 | 0.80 | 0.44 | 23 | 2.4 | 1.80 | 1.2 | 98 | 66 | 0 |
| Corvan Wasp-9 | smg | 3 | 0.80 | 0.43 | 24 | 2.2 | 1.40 | 1.2 | 100 | 80 | 0 |
| Corvan Hornet .45 | smg | 3 | 0.80 | 0.44 | 22 | 2.6 | 1.60 | 2.0 | 99 | 72 | 0 |
| Corvan Gnat PDW | smg | 4 | 0.80 | 0.44 | 29 | 2.2 | 1.50 | 1.0 | 91 | 75 | 0 |
| VKS-74U | assault | 1 | 0.80 | 0.24 | 22 | 2.3 | 1.10 | 2.2 | 94 | 62 | 28 |
| Smerch-3 | assault | 2 | 0.80 | 0.17 | 24 | 2.2 | 0.90 | 2.3 | 91 | 61 | 32 |
| VK-74 | assault | 2 | 0.70 | 0.22 | 22 | 2.9 | 0.70 | 1.7 | 100 | 96 | 69 |
| VKM | assault | 2 | 0.70 | 0.23 | 19 | 2.6 | 0.80 | 2.4 | 97 | 74 | 42 |
| TS-9 Tishina | assault | 3 | 0.65 | 0.22 | 20 | 2.1 | 0.75 | 2.0 | 97 | 72 | 43 |
| TR-30 | assault | 3 | 0.65 | 0.22 | 23 | 2.9 | 0.60 | 1.7 | 100 | 96 | 70 |
| Halden HR36K | assault | 3 | 0.65 | 0.22 | 23 | 2.8 | 0.65 | 1.6 | 100 | 97 | 69 |
| VK-103 | assault | 3 | 0.65 | 0.24 | 20 | 2.9 | 0.70 | 2.3 | 99 | 80 | 48 |
| BR-86 Bulldog | assault | 4 | 0.60 | 0.25 | 22 | 3.2 | 0.55 | 1.5 | 100 | 100 | 91 |
| VK-74 Obelisk | assault | 4 | 0.60 | 0.25 | 23 | 3.4 | 0.50 | 1.6 | 100 | 100 | 90 |
| NA-94 Yenisei | assault | 4 | 0.60 | 0.32 | 21 | 3.3 | 0.55 | 1.4 | 100 | 100 | 91 |
| Corvan Bullpup | assault | 5 | 0.65 | 0.27 | 25 | 3.0 | 0.55 | 1.6 | 60 | 96 | 71 |
| Corvan Battle Rifle | assault | 4 | 0.60 | 0.24 | 20 | 3.2 | 0.50 | 3.0 | 95 | 71 | 45 |
| Obrez-66 | shotgun | 1 | 1.00 | 0.70 | 3 | 2.6 | 1.80 | 8.0 | 85 | 0 | 0 |
| Halden Lever Shotgun | shotgun | 2 | 1.00 | 0.58 | 4 | 2.6 | 1.40 | 6.0 | 100 | 0 | 0 |
| Hound 13 | shotgun | 2 | 1.00 | 0.59 | 4 | 2.4 | 1.30 | 6.0 | 100 | 0 | 0 |
| Ranger P8 | shotgun | 2 | 1.00 | 0.59 | 4 | 2.3 | 1.20 | 6.0 | 100 | 0 | 0 |
| Karusel-5 | shotgun | 3 | 1.00 | 0.57 | 5 | 1.8 | 1.30 | 6.0 | 98 | 0 | 0 |
| Bayou Pump | shotgun | 3 | 1.00 | 0.51 | 5 | 3.2 | 1.20 | 6.0 | 100 | 0 | 0 |
| Sever-34 O/U | shotgun | 3 | 1.00 | 0.81 | 3 | 3.0 | 1.00 | 7.0 | 94 | 0 | 0 |
| Tambour-12 | shotgun | 4 | 1.00 | 0.38 | 8 | 2.3 | 1.50 | 6.5 | 88 | 0 | 0 |
| SP-14 Warden | shotgun | 4 | 1.00 | 0.37 | 8 | 2.1 | 1.30 | 5.5 | 90 | 0 | 0 |
| Sayan-12 | shotgun | 4 | 1.00 | 0.38 | 8 | 2.3 | 1.50 | 6.0 | 88 | 0 | 0 |
| Kaban-12 | shotgun | 4 | 1.00 | 0.33 | 9 | 2.4 | 1.30 | 5.5 | 90 | 0 | 0 |
| SKN Karabin | sniper | 2 | 1.00 | 0.29 | 7 | 3.0 | 0.30 | 4.5 | 74 | 28 | 6 |
| Frontier Lever Rifle | sniper | 3 | 1.00 | 0.43 | 5 | 3.4 | 0.25 | 5.0 | 90 | 60 | 20 |
| VSN Shepot | sniper | 3 | 1.00 | 0.22 | 9 | 3.2 | 0.30 | 3.5 | 67 | 19 | 4 |
| M91 Trekhlineyka | sniper | 3 | 1.00 | 0.43 | 4 | 4.5 | 0.10 | 7.0 | 84 | 100 | 100 |
| Nightwalker .45-70 | sniper | 4 | 1.00 | 0.53 | 4 | 3.3 | 0.20 | 8.0 | 100 | 98 | 74 |
| Corvan Marksman | sniper | 4 | 1.00 | 0.31 | 7 | 3.3 | 0.15 | 5.0 | 82 | 30 | 8 |
| SV-2 Strela | sniper | 4 | 1.00 | 0.26 | 7 | 3.6 | 0.15 | 6.0 | 83 | 32 | 11 |
| Sever-98 | sniper | 4 | 1.00 | 0.45 | 4 | 4.0 | 0.08 | 7.0 | 86 | 100 | 100 |
| VK-127 Grom | sniper | 5 | 1.00 | 0.58 | 4 | 4.5 | 0.10 | 9.0 | 100 | 100 | 100 |
| Halden AM-50 Sledge | sniper | 5 | 1.00 | 0.43 | 5 | 3.4 | 0.10 | 11.0 | 98 | 65 | 31 |
| G-7 Rail Prototype | sniper | 5 | 1.00 | 0.53 | 3 | 3.0 | 0.05 | 4.0 | 100 | 100 | 100 |
| PL-62 Kobza | heavy | 2 | 0.65 | 0.23 | 23 | 3.1 | 1.40 | 2.2 | 50 | 21 | 5 |
| Corvan LMG | heavy | 3 | 0.65 | 0.18 | 28 | 3.3 | 1.30 | 1.9 | 93 | 56 | 22 |
| RL-44 | heavy | 3 | 0.65 | 0.22 | 23 | 3.4 | 1.20 | 2.4 | 50 | 22 | 5 |
| PL-74 | heavy | 4 | 0.60 | 0.22 | 24 | 3.5 | 1.10 | 2.6 | 50 | 22 | 6 |
| PL-74P Polovets | heavy | 4 | 0.60 | 0.22 | 24 | 3.5 | 1.00 | 2.5 | 52 | 26 | 6 |
| PL-74P Polovets Zulu | heavy | 5 | 0.60 | 0.31 | 26 | 3.8 | 0.80 | 2.4 | 55 | 28 | 9 |

Enemy side (same pass): a human enemy now reacts for about 0.95 s (Rookie) down to 0.35 s (Legend) before the first round at a target he can shoot at, his aim settles over the next 0.8 s, and the pause between bursts or pulls is 0.3-0.8 s (automatic) / 0.2-0.45 s (other guns) scaled by rank. Measured: a standing player dies to 4 Experienced raiders at 6 tiles in about 4 s (median), to 6 Veterans at 8 tiles in 2.7 s, to 3 Rookies in 5.3 s (`RTBAL=3`).
