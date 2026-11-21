# Rewrites the combo clip rows in VergerBoss.cs (between "// combos begin" and "// combos end") from ArtSource/mutants_anim/verger_beast/combos.json (made by make_beast_combos.py) and prints each combo's gaps for the Moves in Boss.cs.
import os, re, json
HERE = os.path.dirname(os.path.abspath(__file__)); V = os.path.join(HERE, "..", "Assets", "Scripts", "GameCore", "VergerBoss.cs")
info = json.load(open(os.path.join(HERE, "mutants_anim", "verger_beast", "combos.json")))
rows = "".join('            ["%s"] = new Clip { Name = "%s", Frames = %d, Ms = new float[] { %s }, Hit = %d, Hits = new[] { %s } },\n' % (n, n, d["frames"], ", ".join(str(int(x)) for x in d["ms"]), d["hits"][0], ", ".join(map(str, d["hits"]))) for n, d in info.items())
s = open(V).read(); a = s.index("// combos begin"); a = s.index("\n", a) + 1; b = s.index("            // combos end")
open(V, "w").write(s[:a] + rows + s[b:])
for n, d in info.items(): print(n, "gaps", d["gaps"], "pre-hit ms", d["pre_hit"])
