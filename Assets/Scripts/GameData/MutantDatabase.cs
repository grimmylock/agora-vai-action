// Ported from database.js: MUTANT_DB
using System.Collections.Generic;
using System.Linq;

namespace GameData
{
    public class MutantDef
    {
        public string Name;
        public int Hp;
        public float PhysDR;
        public float AnomDR;
        public int Ap;
        public int Mp;
        public int Ac;
        public int AttackDamage;
        public int AgilityMod;
        public string Behavior;        // pack, swarm, ambush, ranged, berserk
        public int Tier;               // mirrors the Tiers dict key this entry lives under - used by loot/reward scaling
        public bool PackTactics;
        public bool ToxicBite;
        public bool ActiveCamouflage;
        public float? ThroatLacerationChance;
        public float? PanicThreshold;
        public float? ArmorPierce;
        public bool PsiBlast;
        public bool GroundSlam;
        public bool Leap; // Circle & Leap (Dread Prowler) - replaces the old "TriplePounce" flavor-only flag
        public bool NeuralOverload;
        public bool Ward;              // 2026-11-22: a placed room mutant (the Hospital Morgue's): never rolled by the random encounter pools
    }

    public static class MutantDatabase
    {
        // Keyed exactly like MUTANT_DB in the JS: Tiers["tier1"]["BlindMaw"]
        /// <summary>2026-11-22 (Yan's sheet): the Hospital Morgue's room mutants. Placed by DungeonLairs by name; not in Tiers, so no random pool ever picks them. Melee only (the Wretch and the Crawler also leap).</summary>
        public static readonly Dictionary<string, MutantDef> Wards = new Dictionary<string, MutantDef>
        {
            ["Ward Wretch"] = new MutantDef { Name = "Ward Wretch", Tier = 5, Hp = 700, PhysDR = 0.0f, AnomDR = 0.1f, Ap = 4, Mp = 4, Ac = 12, AttackDamage = 340, AgilityMod = 1, Behavior = "berserk", PanicThreshold = 0f, Ward = true },
            ["Ward Hulk"] = new MutantDef { Name = "Ward Hulk", Tier = 5, Hp = 1500, PhysDR = 0.2f, AnomDR = 0.2f, Ap = 3, Mp = 2, Ac = 11, AttackDamage = 520, AgilityMod = -1, Behavior = "berserk", PanicThreshold = 0f, Ward = true },
            ["Ward Crawler"] = new MutantDef { Name = "Ward Crawler", Tier = 5, Hp = 600, PhysDR = 0.0f, AnomDR = 0.1f, Ap = 4, Mp = 5, Ac = 13, AttackDamage = 300, AgilityMod = 2, Behavior = "berserk", PanicThreshold = 0f, Ward = true },
        };

        public static readonly Dictionary<string, Dictionary<string, MutantDef>> Tiers =
            new Dictionary<string, Dictionary<string, MutantDef>>
        {
            ["tier1"] = new Dictionary<string, MutantDef>
            {
                // Synced to 04_Mutants_and_Factions.md: attack damage and panic % are the doc's
                // (armor Phys-DR now actually reduces mutant hits - see Mutant.Attack - so the old
                // lowered damage numbers that compensated for armor being ignored are gone).
                // Phys-DR rebalance (confirmed against actual weapon damage, not just applied blind):
                // CombatResolver.CalculateDamage halves Hollow Point's raw damage against ANY
                // nonzero Phys-DR target (and it's the *only* ammo type that gets punished this
                // way - Standard/AP/Dart don't care). With the old per-mutant Phys-DR, Hollow
                // Point was only ever good against Swarm (0%) and a straight loss against
                // every other mutant here - not a meaningful ammo choice, a trap. Stripping Phys-DR
                // from everything except Blight Brute fixes that project-wide, not just for these
                // three. HP below is the reference file's own compensation table; unchanged here
                // since these three already had it (5%/10%/0% was small enough this database's HP
                // already matched the reference's post-rebalance numbers).
                ["BlindMaw"] = new MutantDef { Name = "Blind Maw", Tier = 1, Hp = 400, PhysDR = 0.0f, AnomDR = 0.05f, Ap = 4, Mp = 5, Ac = 11, AttackDamage = 80, AgilityMod = 1, Behavior = "pack", PackTactics = true, PanicThreshold = 0.4f },
                ["PallidProwler"] = new MutantDef { Name = "Pallid Prowler", Tier = 1, Hp = 450, PhysDR = 0.0f, AnomDR = 0.1f, Ap = 4, Mp = 4, Ac = 11, AttackDamage = 90, AgilityMod = 0, Behavior = "pack", PackTactics = true, ToxicBite = true, PanicThreshold = 0.35f },
                ["Swarm"] = new MutantDef { Name = "Swarm", Tier = 1, Hp = 50, PhysDR = 0.0f, AnomDR = 0.1f, Ap = 4, Mp = 6, Ac = 10, AttackDamage = 50, AgilityMod = 2, Behavior = "swarm", PackTactics = true, PanicThreshold = 0.5f },
            },
            ["tier2"] = new Dictionary<string, MutantDef>
            {
                // Was PhysDR 15%/Hp 60 - Hp bumped to compensate for the removed Phys-DR (see
                // Tier 1 note above for why it's being removed at all).
                ["VeilProwler"] = new MutantDef { Name = "Veil Prowler", Tier = 2, Hp = 700, PhysDR = 0.0f, AnomDR = 0.2f, Ap = 4, Mp = 5, Ac = 12, AttackDamage = 200, AgilityMod = 1, Behavior = "ambush", ActiveCamouflage = true, ThroatLacerationChance = 0.25f, PanicThreshold = 0.3f },
            },
            ["tier3"] = new Dictionary<string, MutantDef>
            {
                // Was PhysDR 25%/Hp 85 and PhysDR 20%/Hp 85 respectively - both bumped to compensate.
                ["Gaunt"] = new MutantDef { Name = "Gaunt", Tier = 3, Hp = 1000, PhysDR = 0.0f, AnomDR = 0.25f, Ap = 5, Mp = 5, Ac = 13, AttackDamage = 300, AgilityMod = 2, Behavior = "ambush", ActiveCamouflage = false,   // 2026-11-16: no cloak any more - the wendigo walks out of the dark in plain sight and screams (Gaunt.cs)
                 ThroatLacerationChance = 0.4f, ArmorPierce = 0.2f, PanicThreshold = 0.2f },
                ["MindWeaver"] = new MutantDef { Name = "Mind Weaver", Tier = 3, Hp = 900, PhysDR = 0.0f, AnomDR = 0.3f, Ap = 5, Mp = 4, Ac = 13, AttackDamage = 350, AgilityMod = 2, Behavior = "ranged", PsiBlast = true, PanicThreshold = 0.2f },
            },
            ["tier4"] = new Dictionary<string, MutantDef>
            {
                // Blight Brute keeps its Phys-DR on purpose - it's the one Brute-archetype mutant
                // meant to punish Hollow Point and reward Armor Piercing/Dart instead.
                ["BlightBrute"] = new MutantDef { Name = "Blight Brute", Tier = 4, Hp = 1200, PhysDR = 0.4f, AnomDR = 0.25f, Ap = 4, Mp = 3, Ac = 14, AttackDamage = 600, AgilityMod = -1, Behavior = "berserk", GroundSlam = true, PanicThreshold = 0.1f },
                // Berserk mutants "never panic" per the Mutants & Factions reference - set explicitly
                // to 0f rather than leaving it unset, since Mutant's ctor fallback (?? 0.25f) would
                // otherwise silently give Dread Prowler a 25% panic threshold it isn't supposed to have.
                // Was PhysDR 30%/Hp 130 - Hp bumped to compensate.
                ["DreadProwler"] = new MutantDef { Name = "Dread Prowler", Tier = 4, Hp = 1550, PhysDR = 0.0f, AnomDR = 0.3f, Ap = 6, Mp = 6, Ac = 14, AttackDamage = 450, AgilityMod = 2, Behavior = "berserk", Leap = true, PanicThreshold = 0f },
            },
            ["tier5"] = new Dictionary<string, MutantDef>
            {
                // Void Weaver "never panics" too - see Dread Prowler's note above. Was PhysDR
                // 30%/Hp 140 - Hp bumped to compensate.
                ["VoidWeaver"] = new MutantDef { Name = "Void Weaver", Tier = 5, Hp = 1700, PhysDR = 0.0f, AnomDR = 0.6f, Ap = 6, Mp = 5, Ac = 15, AttackDamage = 500, AgilityMod = 3, Behavior = "ranged", ActiveCamouflage = true, NeuralOverload = true, PanicThreshold = 0f },
            },
        };

        // New: highest tier actually present in the table above (currently 5 - tier6/7 are
        // unpopulated). EncounterGeneration/SquadManager used to hardcode "3" as the tier cap when
        // scaling mutant difficulty to zone tier, which meant Tier 4-7 zones never saw anything
        // above a tier-3 mutant outside the rare boss roll. Computed instead of hardcoded so this
        // stays correct if tier6/7 entries are ever added.
        public static readonly int MaxTier = Tiers.Keys.Select(ParseTierKey).DefaultIfEmpty(1).Max();

        private static int ParseTierKey(string tierKey)
        {
            var digits = new string(tierKey.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var t) ? t : 1;
        }

        /// <summary>Mirrors JS's MUTANT_DB[tier][key] lookup pattern. Returns null if not found.</summary>
        public static MutantDef Get(string tier, string key)
        {
            if (Tiers.TryGetValue(tier, out var group) && group.TryGetValue(key, out var mutant))
                return mutant;
            return null;
        }
    }
}
