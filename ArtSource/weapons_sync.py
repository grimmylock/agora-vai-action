# Syncs GameData/WeaponDatabase.cs stats to 01_Weapons_and_Ammo.md (damage, ammo, capacity,
# range, weight, tier, Ignores X% Phys-DR, High Recoil, suppressor, heavy MP cap) and adds missing rows.
import re, sys
doc = open(sys.argv[1]).read(); path = sys.argv[2]; cs = open(path).read()
docw = {}
section = None
for line in doc.split("\n"):
    if line.startswith("## "): section = line[3:].strip()
    if not line.startswith("|") or line.startswith("|-") or line.startswith("|Weapon"): continue
    c = [x.strip() for x in line.strip("|").split("|")]
    if section in ("Melee",) and len(c) == 5: docw[c[0]] = ("melee", c)
    elif len(c) == 8 and section in ("Sidearms", "SMGs", "Shotguns", "Assault Rifles", "Sniper Rifles", "Heavy Weapons"): docw[c[0]] = (section, c)

def fmt(v):
    if isinstance(v, bool): return "true" if v else "false"
    if isinstance(v, float): return f"{v:g}f"
    if isinstance(v, int): return str(v)
    return f'"{v}"'

ORDER = ["Name", "BaseDamage", "AmmoType", "MagCapacity", "CylinderCapacity", "Range", "WeightKg", "Tier", "IgnorePhysDRPercent",
         "HighRecoil", "BurstCapable", "BurstFireApCost", "ActionType", "ReloadApCost", "MagFed", "InnateSuppressor", "PreAttached",
         "MaxMpWhileHeld", "ImmuneToBurstJams"]
changes = 0
def rebuild(m):
    global changes
    key, body = m.group(1), m.group(2)
    fields = dict(re.findall(r'(\w+) = ("[^"]*"|new\[\] \{[^}]*\}|[\d.]+f?|true|false)', body))
    name = fields["Name"].strip('"')
    if name not in docw: return m.group(0)
    sec, c = docw[name]
    f = {k: v for k, v in fields.items()}
    if sec == "melee":
        f["BaseDamage"] = c[1]; f["WeightKg"] = fmt(float(c[2].rstrip("kg"))); f["Tier"] = c[3]; traits = c[4]
    else:
        f["BaseDamage"] = c[1]; f["AmmoType"] = fmt(c[2])
        cap = int(re.match(r"\d+", c[3]).group(0))
        if "CylinderCapacity" in f: f["CylinderCapacity"] = str(cap)
        else: f["MagCapacity"] = str(cap)
        f["Range"] = fmt(f"{int(c[4]) * 2.5:g}cm")
        f["WeightKg"] = fmt(float(c[5].rstrip("kg"))); f["Tier"] = c[6]; traits = c[7]
        if re.search(r"high recoil", traits, re.I): f["HighRecoil"] = "true"
        if re.search(r"suppressor", traits, re.I): f["InnateSuppressor"] = "true"
        if sec == "Heavy Weapons": f["MaxMpWhileHeld"] = "2"
    ig = re.search(r"ignores (\d+)% Phys-DR", traits, re.I)
    if ig: f["IgnorePhysDRPercent"] = fmt(int(ig.group(1)) / 100)
    out = ", ".join(f"{k} = {f[k]}" for k in ORDER if k in f) + "".join(f", {k} = {v}" for k, v in f.items() if k not in ORDER)
    new = f'["{key}"] = new WeaponDef {{ {out} }}'
    if new != m.group(0): changes += 1
    return new
cs = re.sub(r'\["(\w+)"\] = new WeaponDef \{ (.*?) \}(?=,\s*$)', rebuild, cs, flags=re.M)
# missing melee weapons
add = {"KizlyarMachete": "Kizlyar Machete", "SerratedTrenchKnife": "Serrated Trench Knife", "MonolithRitualBlade": "Monolith Ritual Blade"}
anchor = re.search(r'(\["SpetsnazSwitchblade"\] = new WeaponDef \{[^}]*\},)', cs).group(1)
lines = []
for key, name in add.items():
    if f'"{name}"' in cs: continue
    _, c = docw[name]
    ig = re.search(r"ignores (\d+)% Phys-DR", c[4], re.I)
    extra = f", IgnorePhysDRPercent = {int(ig.group(1)) / 100:g}f" if ig else ""
    lines.append(f'                ["{key}"] = new WeaponDef {{ Name = "{name}", BaseDamage = {c[1]}, Range = "Adjacent", WeightKg = {float(c[2].rstrip("kg")):g}f, Tier = {c[3]}{extra} }},')
if lines: cs = cs.replace(anchor, anchor + "\n" + "\n".join(lines))
open(path, "w").write(cs)
print("rebuilt", changes, "entries; added", len(lines))
