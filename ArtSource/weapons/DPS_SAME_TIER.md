# Weapon DPS vs the median armor of the same tier (2026-11-19)

Made by `RTBAL=10 bash Tools/compile_check/runtest.sh ../../Assets/Scripts` (`DpsVsSameTierReport` in `test/Program.cs`). Every round a torso hit at 3 tiles (no fall-off, spread or misses); automatics at their rpm, everything else at most 4 shots/s; sustained DPS includes reloads; the target is a 1000 HP standing human. The AP columns (tier 3 and up) use the gun's armor-piercing load.

| gun | cat | tier | dmg/round after armor | shots/s | DPS (no reload) | sustained DPS | mag | seconds to kill 1000 HP | AP: dmg/round | AP: sustained DPS | AP: seconds to kill (tier 3+ only) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| VKS-74U | assault | 1 | 37 | 9.3 | 345 | 217 | 30 | 2.9 | - | - | - |
| Obrez-66 | shotgun | 1 | 268 | 3.3 | 893 | 206 | 2 | 2.9 | - | - | - |
| Halden Service Revolver | sidearm | 1 | 171 | 2.5 | 428 | 205 | 6 | 2.0 | - | - | - |
| PSh-18 Pchela | sidearm | 1 | 89 | 3.3 | 297 | 187 | 8 | 4.7 | - | - | - |
| Vosa vz. 62 | smg | 1 | 40 | 11.3 | 453 | 245 | 20 | 3.6 | - | - | - |
| Smerch-3 | assault | 2 | 39 | 10.7 | 416 | 254 | 30 | 2.3 | - | - | - |
| VKM | assault | 2 | 44 | 7.0 | 308 | 207 | 30 | 3.1 | - | - | - |
| VK-74 | assault | 2 | 37 | 7.2 | 268 | 178 | 30 | 3.7 | - | - | - |
| PL-62 Kobza | heavy | 2 | 35 | 7.0 | 246 | 179 | 75 | 4.0 | - | - | - |
| Halden Lever Shotgun | shotgun | 2 | 203 | 1.2 | 237 | 179 | 8 | 3.4 | - | - | - |
| Ranger P8 | shotgun | 2 | 200 | 1.3 | 267 | 174 | 7 | 3.0 | - | - | - |
| Hound 13 | shotgun | 2 | 200 | 1.3 | 250 | 162 | 6 | 3.2 | - | - | - |
| Halden .45 Service | sidearm | 2 | 100 | 4.0 | 400 | 230 | 7 | 3.6 | - | - | - |
| Walden P9 | sidearm | 2 | 78 | 4.0 | 312 | 223 | 16 | 3.0 | - | - | - |
| PPZ Zubr | smg | 2 | 32 | 9.1 | 290 | 212 | 64 | 3.4 | - | - | - |
| SKN Karabin | sniper | 2 | 118 | 2.0 | 236 | 159 | 10 | 4.0 | - | - | - |
| TS-9 Tishina | assault | 3 | 43 | 9.2 | 396 | 206 | 20 | 4.5 | 50 | 240 | 2.1 |
| VK-103 | assault | 3 | 41 | 6.5 | 267 | 183 | 30 | 3.7 | 49 | 219 | 3.1 |
| Halden HR36K | assault | 3 | 35 | 7.8 | 273 | 180 | 30 | 3.6 | 42 | 216 | 2.9 |
| TR-30 | assault | 3 | 35 | 7.6 | 265 | 176 | 30 | 3.7 | 42 | 212 | 3.0 |
| Corvan LMG | heavy | 3 | 29 | 8.1 | 236 | 199 | 200 | 4.2 | 34 | 234 | 3.6 |
| RL-44 | heavy | 3 | 35 | 6.5 | 228 | 179 | 100 | 4.3 | 42 | 214 | 3.5 |
| Sever-34 O/U | shotgun | 3 | 269 | 2.5 | 673 | 179 | 2 | 3.4 | 307 | 205 | 3.4 |
| Karusel-5 | shotgun | 3 | 162 | 2.2 | 351 | 172 | 5 | 5.2 | 185 | 196 | 4.7 |
| Bayou Pump | shotgun | 3 | 160 | 1.3 | 200 | 136 | 8 | 4.8 | 182 | 155 | 4.0 |
| SR-21 Gadyuka | sidearm | 3 | 77 | 4.0 | 308 | 239 | 18 | 3.0 | 85 | 264 | 2.8 |
| Black Harrier | sidearm | 3 | 89 | 4.0 | 356 | 214 | 9 | 4.3 | 103 | 247 | 3.8 |
| Corvan Wasp-9 | smg | 3 | 34 | 10.7 | 363 | 221 | 30 | 2.7 | 38 | 247 | 2.4 |
| Corvan Hornet .45 | smg | 3 | 37 | 8.0 | 296 | 184 | 25 | 5.3 | 42 | 209 | 2.9 |
| VSN Shepot | sniper | 3 | 91 | 2.5 | 228 | 147 | 10 | 6.2 | 109 | 176 | 3.6 |
| Frontier Lever Rifle | sniper | 3 | 161 | 1.2 | 188 | 128 | 8 | 5.1 | 187 | 149 | 4.3 |
| M91 Trekhlineyka | sniper | 3 | 232 | 0.7 | 155 | 106 | 5 | 6.0 | 279 | 128 | 4.5 |
| NA-94 Yenisei | assault | 4 | 39 | 6.0 | 234 | 163 | 30 | 4.2 | 52 | 217 | 3.2 |
| VK-74 Obelisk | assault | 4 | 35 | 6.5 | 228 | 159 | 30 | 4.3 | 47 | 213 | 3.2 |
| Corvan Battle Rifle | assault | 4 | 45 | 6.0 | 270 | 157 | 20 | 6.1 | 59 | 206 | 2.7 |
| BR-86 Bulldog | assault | 4 | 28 | 6.5 | 182 | 116 | 30 | 8.0 | 37 | 154 | 4.2 |
| PL-74P Polovets | heavy | 4 | 48 | 6.5 | 312 | 249 | 100 | 3.1 | 65 | 337 | 2.3 |
| PL-74 | heavy | 4 | 34 | 6.5 | 221 | 171 | 100 | 4.5 | 46 | 231 | 3.2 |
| Sayan-12 | shotgun | 4 | 101 | 3.0 | 303 | 183 | 10 | 3.0 | 128 | 231 | 2.3 |
| Kaban-12 | shotgun | 4 | 90 | 3.3 | 300 | 167 | 10 | 5.7 | 114 | 211 | 2.4 |
| Tambour-12 | shotgun | 4 | 101 | 3.0 | 303 | 152 | 12 | 3.0 | 128 | 192 | 2.3 |
| SP-14 Warden | shotgun | 4 | 101 | 3.3 | 337 | 144 | 8 | 5.9 | 128 | 183 | 2.1 |
| Steppe Eagle | sidearm | 4 | 109 | 3.3 | 363 | 236 | 8 | 4.0 | 136 | 294 | 2.1 |
| Corvan 5.7 | sidearm | 4 | 67 | 4.0 | 268 | 216 | 20 | 3.5 | 75 | 242 | 3.3 |
| Rinaldi R6 | sidearm | 4 | 134 | 3.2 | 424 | 187 | 6 | 4.6 | 166 | 232 | 4.3 |
| Sperling S-96 | sidearm | 4 | 73 | 4.0 | 292 | 166 | 10 | 5.2 | 89 | 202 | 4.7 |
| Corvan Gnat PDW | smg | 4 | 30 | 12.7 | 380 | 247 | 40 | 2.6 | 34 | 280 | 2.3 |
| Corvan Marksman | sniper | 4 | 117 | 1.8 | 215 | 175 | 20 | 4.4 | 157 | 234 | 3.3 |
| Sever-98 | sniper | 4 | 268 | 0.8 | 201 | 160 | 10 | 4.0 | 358 | 214 | 2.7 |
| Nightwalker .45-70 | sniper | 4 | 216 | 0.9 | 198 | 136 | 6 | 4.4 | 216 | 136 | 4.4 |
| SV-2 Strela | sniper | 4 | 116 | 1.7 | 193 | 135 | 10 | 4.8 | 156 | 181 | 3.6 |
| Corvan Bullpup | assault | 5 | 28 | 8.1 | 228 | 140 | 30 | 6.6 | 52 | 260 | 2.3 |
| PL-74P Polovets Zulu | heavy | 5 | 31 | 6.5 | 202 | 161 | 100 | 4.9 | 56 | 290 | 2.6 |
| RK-12 Kuvalda | sidearm | 5 | 275 | 1.3 | 344 | 196 | 5 | 2.4 | 379 | 271 | 1.6 |
| G-7 Rail Prototype | sniper | 5 | 664 | 0.7 | 443 | 369 | 10 | 1.5 | 664 | 369 | 1.5 |
| Halden AM-50 Sledge | sniper | 5 | 314 | 1.2 | 366 | 262 | 10 | 2.6 | 397 | 332 | 1.7 |
| VK-127 Grom | sniper | 5 | 423 | 0.5 | 212 | 163 | 5 | 4.0 | 546 | 210 | 2.0 |
