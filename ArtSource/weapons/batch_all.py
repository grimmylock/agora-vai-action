import sys; sys.path.insert(0, '/home/claude/art/weapons')
from weapon_pixel import *
from batch_pistols import sheet
F = True
CFG = {
 "melee": {
  "HuntingKnife": dict(B=12, flip=F, rot=16, drop_dark=0.3), "SpetsnazSwitchblade": dict(B=12, flip=F, rot=13), "SteppeMachete": dict(B=18, flip=F, erase=[(1530, 380, 1920, 626)]),
  "SerratedTrenchKnife": dict(B=13), "ObeliskRitualBlade": dict(B=16, flip=F)},
 "sidearm": {
  "PSh18Pchela": dict(B=14), "WaldenP9": dict(B=14, olive_s=0.08), "Halden45Service": dict(B=15, flip=F),
  "HaldenServiceRevolver": dict(B=16, flip=F, force=[(9, 40, 0, 6, "metal")]), "SR21Gadyuka": dict(B=14, flip=F),
  "BlackHarrier": dict(B=17), "Corvan57": dict(B=15), "RinaldiR6": dict(B=16, flip=F, rot=14), "SteppeEagle": dict(B=16, flip=F, rot=27)},
 "smg": {
  "VosaVz62": dict(B=16, flip=F), "PPZZubr": dict(B=21, drop=(45, 100, 0.18)), "CorvanWasp9": dict(B=22, flip=F), "CorvanHornet45": dict(B=22, flip=F, bright=0.55), "CorvanGnatPDW": dict(B=20, flip=F, bright=0.45)},
 "shotgun": {
  "Obrez66": dict(B=17, mask="white"), "HaldenLeverShotgun": dict(B=26, flip=F), "Hound13": dict(B=27), "RangerP8": dict(B=28),
  "BayouPump": dict(B=28), "Sever34": dict(B=29, flip=F), "Tambour12": dict(B=26), "SP14Warden": dict(B=27, flip=F),
  "Sayan12": dict(B=28, flip=F, erase=[(260, 120, 370, 230)], cov=0.35), "Kaban12": dict(B=27, rot=-25)},
 "assault": {
  "VKS74U": dict(B=24), "Smerch3": dict(B=19), "VK74": dict(B=28), "VKM": dict(B=28, rot=-15), "TS9Tishina": dict(B=26),
  "TR30": dict(B=27, mask="white", recolor="dark"), "HaldenHR36K": dict(B=25), "VK103": dict(B=27, mask="white"), "BR86Bulldog": dict(B=25, flip=F, cov=0.35),
  "VK74Obelisk": dict(B=28), "NA94Yenisei": dict(B=28), "CorvanBullpup": dict(B=22, flip=F), "CorvanBattleRifle": dict(B=30, cov=0.35)},
 "sniper": {
  "SKNKarabin": dict(B=29), "FrontierLeverRifle": dict(B=27, flip=F), "VSNShepot": dict(B=27), "M91Trekhlineyka": dict(B=30),
  "Nightwalker4570": dict(B=27), "CorvanMarksman": dict(B=30, flip=F), "SV2Strela": dict(B=30, flip=F, drop=(28, 95, 0.1)), "Sever98": dict(B=29, flip=F),
  "VK127Grom": dict(B=28, rot=-6), "HaldenAM50Sledge": dict(B=30, flip=F), "G7RailPrototype": dict(B=26, flip=F)},
 "heavy": {
  "CorvanLMG": dict(B=29, bright=0.7), "RL44": dict(B=29), "PL74": dict(B=30, rot=-8), "PL74PPolovets": dict(B=30, rot=-9), "PL74PPolovetsZulu": dict(B=30, rot=-9)},
}
def _row(x0, y, txt): return [(x0 + i, y, c) for i, c in enumerate(txt) if c != " "]
ZULU = (_row(16, 3, "33 3333333333333 333") + _row(16, 4, "4l222222222222222Rl") + _row(16, 5, "44444444444444444ll")
        + _row(16, 6, "33   5       5   33") + _row(44, 10, "5555R") + _row(44, 11, "4444"))
OBELISK = _row(25, 6, " 444 ") + _row(25, 7, "4lRl4") + _row(25, 8, "33333") + _row(38, 13, "44444R") + _row(38, 14, "55555")
CFG["heavy"]["PL74PPolovetsZulu"]["after"] = ZULU
CFG["assault"]["VK74Obelisk"]["after"] = OBELISK
CFG["melee"]["SteppeMachete"]["after"] = [(23, 15, "."), (23, 19, ".")]
for cat in CFG.values():
    for c in cat.values(): c["A"] = min(60, 2 * c["B"])
if __name__ == '__main__':
    for cat, P in CFG.items():
        if len(sys.argv) > 1 and cat not in sys.argv[1:]: continue
        sheet(P, f'/tmp/claude-0/px_{cat}.png', f'{cat} v1')
