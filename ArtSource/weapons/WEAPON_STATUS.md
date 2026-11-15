# Weapon pictures: reworked vs old (generated, do not edit by hand)

Regenerate with `python3 ArtSource/weapons/make_weapon_status.py`. **Reworked** = the gun is listed in `WeaponArt.Dense` (`Assets/Scripts/GameCore/HeldWeapon.cs`): its picture is the DOUBLE-SIZE one (160x64 canvas) made by `make_gun_x2.py`, used for the menus AND in the hand.
**Old** = the hand-pixelled 64x32 menu picture and the small in-hand picture from before 2026-11-13. How to rework one: `WEAPON_SPRITE_RULES.md`.

**Guns: 52 reworked, 5 old** (melee weapons are a separate hand-pixelled set, 12 of them, not part of this rework).

## Reworked

| Key | In-game name | Category | Real-world pattern | Real length (cm) | Picture length (px) | Reference photo (source, licence) | Density k |
|-|-|-|-|-|-|-|-|
| WaldenP9 | Walden P9 | sidearm | Walker P9m | 18.4 | 90 | https://commons.wikimedia.org/wiki/File:Walther_P99_9x19mm.png (CC BY 2.5) | 5.38 |
| Halden45Service | Halden .45 Service | sidearm | Colt M1911 | 21.9 | 94 | https://commons.wikimedia.org/wiki/File:M1911A1.png (CC BY-SA 3.0) | 5.71 |
| HaldenServiceRevolver | Halden Service Revolver | sidearm | M1917 Revolver | 27.3 | 100 | https://commons.wikimedia.org/wiki/File:US_Colt_45_M1917_Revolver_noBG.jpg (CC BY-SA 4.0) | 5.26 |
| Corvan57 | Corvan 5.7 | sidearm | FN Five-seveN | 20.8 | 93 | https://commons.wikimedia.org/wiki/File:FN_Five_Seven.jpg (Public domain) | 4.14 |
| SperlingS96 | Sperling S-96 | sidearm | Mauser C96 ("Red 9", 9x19) | 30 | 103 | none on file | 5.00 |
| RinaldiR6 | Rinaldi R6 | sidearm | Chiappa Rhino | 29 | 102 | https://commons.wikimedia.org/wiki/File:Chiappa_Rhino_DS_60_Stormhunter.jpg (CC0) | 7.09 |
| SteppeEagle | Steppe Eagle | sidearm |  | 26.4 | 99 | https://commons.wikimedia.org/wiki/File:Desert_Eagle_Chrome.JPG (CC BY-SA 3.0) | 6.26 |
| RK12Kuvalda | RK-12 Kuvalda | sidearm | RSh-12 | 33 | 106 | none on file | 7.17 |
| VosaVz62 | Vosa vz. 62 | smg | Skorpion vz. 61 | 27 | 100 | https://commons.wikimedia.org/wiki/File:Sa_vz._61_%C5%A0korpion.jpg (CC0) | 8.92 |
| PPZZubr | PPZ Zubr | smg | PP-19 Bizon | 55 | 122 | https://commons.wikimedia.org/wiki/File:PP-19_Bizon_right_view.jpg (CC BY-SA 3.0) | 7.97 |
| CorvanWasp9 | Corvan Wasp-9 | smg | Viper 5 | 55 | 122 | https://commons.wikimedia.org/wiki/File:Heckler_%26_Koch_MP5-1_(cropped).jpg (CC BY-SA 3.0) | 6.91 |
| CorvanHornet45 | Corvan Hornet .45 | smg | UMP-45 | 69 | 130 | https://commons.wikimedia.org/wiki/File:HKUMP45.JPG (CC BY-SA 3.0) | 6.94 |
| CorvanGnatPDW | Corvan Gnat PDW | smg | MP7 | 34 | 107 | https://commons.wikimedia.org/wiki/File:Heckler_%26_Koch_MP7A1.png (CC BY-SA 2.5) | 6.79 |
| Obrez66 | Obrez-66 | shotgun | Sawed-off TOZ-66 | 45 | 115 | https://commons.wikimedia.org/wiki/File:Weapon_made_from_a_shotgun.png (Public domain) | 6.65 |
| HaldenLeverShotgun | Halden Lever Shotgun | shotgun | Winchester 1887 | 85 | 138 | https://commons.wikimedia.org/wiki/File:M1887_LH_noBG.png (CC BY-SA 4.0) | 6.32 |
| Hound13 | Hound 13 | shotgun | Chaser 13 | 100 | 144 | https://commons.wikimedia.org/wiki/File:Mossberg_Maverick_88.png (CC BY-SA 4.0) | 6.00 |
| RangerP8 | Ranger P8 | shotgun | Remington 870 | 106 | 147 | https://commons.wikimedia.org/wiki/File:Remington_870_Wmaster.jpg (CC BY-SA 4.0) | 5.95 |
| Karusel5 | Karusel-5 | shotgun | Rossi Circuit Judge (revolver carbine, shotgun rounds) | ? | None | none on file | 6.61 |
| BayouPump | Bayou Pump | shotgun | Mossberg 590 | 100 | 144 | https://commons.wikimedia.org/wiki/File:M590A1.png (CC BY-SA 3.0) | 5.90 |
| Sever34 | Sever-34 O/U | shotgun | TOZ-34 | 112 | 149 | https://commons.wikimedia.org/wiki/File:TOZ-34_Shotgun.jpg (CC BY-SA 4.0) | 5.55 |
| Tambour12 | Tambour-12 | shotgun | Protecta | 79 | 135 | https://commons.wikimedia.org/wiki/File:Armsel_Striker_no_background.png (CC BY-SA 2.0) | 4.97 |
| SP14Warden | SP-14 Warden | shotgun | SPSA14 | 80 | 136 | https://commons.wikimedia.org/wiki/File:Franchi_SPAS_12-P5120256-white.jpg (CC BY-SA 3.0 fr) | 6.00 |
| Sayan12 | Sayan-12 | shotgun | Saiga-12 | 105 | 146 | https://commons.wikimedia.org/wiki/File:Saiga_12_black_stock.jpg (CC BY-SA 4.0) | 4.71 |
| Kaban12 | Kaban-12 | shotgun | Vepr-12 | 95 | 142 | https://commons.wikimedia.org/wiki/File:Vepr-12_ARMS_%26_Hunting_2012_01.jpg (CC BY-SA 4.0) | 6.03 |
| VKS74U | VKS-74U | assault | AKS-74U | 73 | 132 | https://commons.wikimedia.org/wiki/File:AKS74U_(noBG).jpg (CC BY-SA 4.0) | 6.85 |
| VK74 | VK-74 | assault | AK-74 | 94 | 142 | https://commons.wikimedia.org/wiki/File:Ak74l.png (CC BY-SA 3.0) | 5.79 |
| VKM | VKM | assault | AKM | 88 | 139 | https://commons.wikimedia.org/wiki/File:AKM-JH01.jpg (Public domain) | 5.76 |
| TS9Tishina | TS-9 Tishina | assault | VLA | 88.6 | 139 | https://commons.wikimedia.org/wiki/File:AS_Val_(541-03).jpg (CC BY-SA 4.0) | 6.58 |
| TR30 | TR-30 | assault | TRs-301 | 84 | 137 | https://commons.wikimedia.org/wiki/File:LR_300.JPG (CC BY-SA 3.0) | 6.05 |
| HaldenHR36K | Halden HR36K | assault | G36K | 86 | 138 | https://commons.wikimedia.org/wiki/File:Gewehr_G36K_noBG_02.png (CC BY-SA 4.0) | 6.49 |
| VK103 | VK-103 | assault | AK-103 | 94 | 142 | https://commons.wikimedia.org/wiki/File:AK-103.JPG (Public domain) | 5.55 |
| BR86Bulldog | BR-86 Bulldog | assault | IL86 | 78.5 | 135 | https://commons.wikimedia.org/wiki/File:Enfield_L85A1-IWM_PR_17-IMG_6557-white.jpg (CC BY-SA 3.0 fr) | 6.54 |
| VK74Obelisk | VK-74 Obelisk | assault | AK-74 OBELISK | 94 | 142 | https://commons.wikimedia.org/wiki/File:Ak74l.png (CC BY-SA 3.0) | 5.74 |
| NA94Yenisei | NA-94 Yenisei | assault | AN-94 Abakan | 94.3 | 142 | https://commons.wikimedia.org/wiki/File:Rifle_AN-94.jpg (Public domain) | 5.74 |
| CorvanBullpup | Corvan Bullpup | assault | FT-200M | 69 | 130 | https://commons.wikimedia.org/wiki/File:FN_F2000S.JPG (Public domain) | 6.00 |
| CorvanBattleRifle | Corvan Battle Rifle | assault | FN FAL | 110 | 148 | https://commons.wikimedia.org/wiki/File:FN-FAL_belgian_noBG.png (CC BY-SA 4.0) | 5.64 |
| SKNKarabin | SKN Karabin | sniper | SKS | 102 | 145 | https://commons.wikimedia.org/wiki/File:Simonov-SKS-45.JPG (Public domain) | 5.67 |
| FrontierLeverRifle | Frontier Lever Rifle | sniper | Winchester 1886/1892 | 95 | 142 | https://commons.wikimedia.org/wiki/File:Winchester_92_short_rifle_transparent.png (CC0) | 6.00 |
| VSNShepot | VSN Shepot | sniper | Vintar BC | 89 | 140 | https://commons.wikimedia.org/wiki/File:Vss_vintorez_01.jpeg (CC BY-SA 3.0) | 4.75 |
| M91Trekhlineyka | M91 Trekhlineyka | sniper | Mosin Nagant | 123.5 | 153 | https://commons.wikimedia.org/wiki/File:Mosin-Nagant_m91-30_sniper_noBG.png (CC BY-SA 3.0) | 5.22 |
| Nightwalker4570 | Nightwalker .45-70 | sniper | Marlin Dark Series | 95 | 142 | photo supplied by Yan 2026-11-13 (black polymer lever rifle; source unknown, drawing reference only) (licence unknown) | 6.00 |
| CorvanMarksman | Corvan Marksman | sniper | SR-25 | 112 | 149 | https://commons.wikimedia.org/wiki/File:SR-25_pic02.jpg (CC BY 2.0) | 5.27 |
| SV2Strela | SV-2 Strela | sniper | SVDm-2 | 122 | 153 | https://commons.wikimedia.org/wiki/File:SVD_Dragunov.jpg (CC BY-SA 4.0) | 5.09 |
| Sever98 | Sever-98 | sniper | SV-98 | 118 | 151 | https://commons.wikimedia.org/wiki/File:SV-98-05.jpg (CC BY-SA 4.0) | 6.64 |
| VK127Grom | VK-127 Grom | sniper | VSSK Vychlop | 92 | 141 | https://commons.wikimedia.org/wiki/File:VKS_sniper_rifle_at_Engineering_Technologies_2012.jpg (CC BY-SA 3.0) | 5.41 |
| HaldenAM50Sledge | Halden AM-50 Sledge | sniper | Barrett M82 | 145 | 156 | https://commons.wikimedia.org/wiki/File:Barrett-M82A1-Independence-Day-2017-IZE-048-white.jpg (CC BY 4.0) | 4.88 |
| PL62Kobza | PL-62 Kobza | heavy |  | 104 | 146 | none on file | 5.46 |
| CorvanLMG | Corvan LMG | heavy | M249 SAW | 104 | 146 | https://commons.wikimedia.org/wiki/File:PEO_M249_Para_ACOG.jpg (Public domain) | 5.49 |
| RL44 | RL-44 | heavy | RPD | 104 | 146 | https://commons.wikimedia.org/wiki/File:LMG-RPD-44_noBG.png (CC BY-SA 3.0) | 5.52 |
| PL74 | PL-74 | heavy | RP-74 | 116 | 150 | https://commons.wikimedia.org/wiki/File:PKM_DD-ST-85-01257.JPEG (Public domain) | 5.31 |
| PL74PPolovets | PL-74P Polovets | heavy | PKP Pecheneg | 121 | 152 | https://commons.wikimedia.org/wiki/File:Pecheneg_machine_gun-06.jpg (CC BY-SA 3.0) | 6.16 |
| PL74PPolovetsZulu | PL-74P Polovets Zulu | heavy | PKP Pecheneg Zulu | 121 | 152 | https://commons.wikimedia.org/wiki/File:Pecheneg_machine_gun-06.jpg (CC BY-SA 3.0) | 6.16 |

## Old (still to do)

| Key | In-game name | Category | Tier | Real-world pattern | Real length (cm) | Planned picture length (px) | Reference photo on file (source, licence) |
|-|-|-|-|-|-|-|-|
| Smerch3 | Smerch-3 | assault | 2 | SR-3M Vikhr | 63 | 127 | https://commons.wikimedia.org/wiki/File:Oboronexpo2014part4-26.jpg (CC BY-SA 4.0) |
| PSh18Pchela | PSh-18 Pchela | sidearm | 1 | PMm | 16.1 | 87 | https://commons.wikimedia.org/wiki/File:%D0%9F%D0%B8%D1%81%D1%82%D0%BE%D0%BB%D0%B5%D1%82_%D0%9F%D0%9C%D0%9C_-_%D0%9E%D0%A1%D0%9D_%D0%A1%D0%B0%D1%82%D1%83%D1%80%D0%BD_01.jpg (CC BY-SA 4.0) |
| BlackHarrier | Black Harrier | sidearm | 3 | Black Kite | 29 | 102 | https://commons.wikimedia.org/wiki/File:Wildey_IMG_6827-8_right.jpg (CC BY-SA 2.0 fr) |
| SR21Gadyuka | SR-21 Gadyuka | sidearm | 3 | SP-21 Gyurza | 19.5 | 91 | https://commons.wikimedia.org/wiki/File:9mm_SR1PM_pistol_TVM2012_016.jpg (CC BY-SA 4.0) |
| G7RailPrototype | G-7 Rail Prototype | sniper | 5 | Item 62 Gauss Rifle | 100 | 144 | https://commons.wikimedia.org/wiki/File:Single_stage_coilgun.jpg (CC BY 3.0) |

## Melee (separate set, not part of this rework)

Hunting Knife (`HuntingKnife`), Spetsnaz Switchblade (`SpetsnazSwitchblade`), Steppe Machete (`SteppeMachete`), Serrated Trench Knife (`SerratedTrenchKnife`), Obelisk Ritual Blade (`ObeliskRitualBlade`), Hatchet (`Hatchet`), Wooden Baseball Bat (`WoodenBaseballBat`), Hammer (`Hammer`), Fire Axe (`FireAxe`), Sledgehammer (`Sledgehammer`), Barbed Wire Bat (`BarbedWireBat`), Tactical Hatchet (`TacticalHatchet`)
