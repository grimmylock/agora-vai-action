// Story polish (2026-11-18, Yan: "polish tier 1 to 3, add the missing npcs"): the story's people as real units on the map instead of bare talk points. Each stands at his landmark
// while the story says he is there (a predicate), unarmed, never aggroed, hostile to nobody (faction "Folk"), unkillable (Hp never below 1), and held in place (TurnLoop never wanders
// them, Holds). The talk points still work exactly as before: E near the landmark; the unit is only who you see and who turns to face you (Story.Talk finds him by name).
// Re-evaluated every game minute and when a map loads; Pavel is a unit only while he's NOT walking with you. ponytail: they are the player's friends - no dialogue about being shot,
// a hit just fails to hurt; they have no daily routine and don't sit or sleep (the A-Life's barks and the Banter lines don't include them).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class StoryNpcs
    {
        public const string Faction = "Folk";

        class Def { public string Name, Landmark, Zone, Look, Suit, Fixed; public int Dx, Dy; public System.Func<bool> When; }
        static string P(string zone) => MacroMapDatabase.MapOf(zone);
        static bool PavelFree => !Tier1Story.PavelWithYou;
        static readonly List<Def> Defs = new List<Def>
        {
            new Def { Name = "Yefim", Suit = "Yefim",  Landmark = "The Bonfire Camp", Zone = "perimeter", Look = "Wanderers", Dx = 2, Dy = 0, When = () => true },
            new Def { Name = "Stas", Fixed = "Wanderer Canvas Jacket",  Landmark = "Bunkhouse", Zone = "perimeter", Look = "Wanderers", Dx = 1, Dy = 1, When = () => true },
            new Def { Name = "Headman Petro", Fixed = "Wanderer Padded Coat",  Landmark = "Headman's House", Zone = "perimeter", Look = "Wanderers", Dx = 1, Dy = 1, When = () => true },
            new Def { Name = "The Runner", Suit = "The Runner",  Landmark = "The Cutting", Zone = "perimeter", Look = "Wanderers", Dx = 2, Dy = 1, When = () => Story.Step("runner") >= 1 || Story.IsDone("runner") },
            // Pavel: the bunker, then the Medic's House, then the Hub's bunkhouse, then the Sub-Station
            new Def { Name = "Pavel", Suit = "Pavel",  Landmark = "Command Room", Zone = "flooded_bunker", Look = "Surveyors", Dx = 1, Dy = 1, When = () => Story.Step("engineer") == 1 && PavelFree },
            new Def { Name = "Pavel", Suit = "Pavel",  Landmark = "Medic's House", Zone = "perimeter", Look = "Surveyors", Dx = 1, Dy = 2, When = () => Story.IsDone("engineer") && Story.Step("escort") < 2 && !Story.IsDone("escort") && PavelFree },
            new Def { Name = "Pavel", Suit = "Pavel",  Landmark = "Hub Bunkhouse", Zone = "scavengers_hub", Look = "Surveyors", Dx = 1, Dy = 1, When = () => Story.IsDone("escort") && Story.Step("lake") == 0 && PavelFree },
            new Def { Name = "Pavel", Suit = "Pavel",  Landmark = "Sub-Station House", Zone = "scavengers_hub", Look = "Surveyors", Dx = 3, Dy = 1, When = () => Story.Has("tier3_reveal") && PavelFree },
            new Def { Name = "The Scrap King", Suit = "The Scrap King",  Landmark = "Scrap King's Nest", Zone = "scrapyard", Look = "Raiders", Dx = 1, Dy = 1, When = () => !Story.Has("king_exiled") },
            new Def { Name = "Spool", Suit = "Barbwire Coat",  Landmark = "Dog Kennels", Zone = "scrapyard", Look = "Outcasts", Dx = 1, Dy = 1, When = () => (Story.Has("spool_spared") || Story.Has("rooke_sided") && Story.Step("spool") == 0) && Story.Step("crown") < 3 && !Story.IsDone("crown") },   // the ally at the Kennels (CrownJob.cs)
            new Def { Name = "Spool", Suit = "Barbwire Coat",  Landmark = "Scrap King's Nest", Zone = "scrapyard", Look = "Outcasts", Dx = -2, Dy = 1, When = () => Story.Step("crown") >= 5 || Story.Has("king_exiled") },   // at the Nest for the duel, and after it: he keeps the Nest and the trade
            new Def { Name = "Voss", Suit = "Voss",  Landmark = "Roundhouse", Zone = "scavengers_hub", Look = "Contractors", Dx = 2, Dy = 1, When = () => true },
            new Def { Name = "Quill", Fixed = "Contractor Light",  Landmark = "Signal Tower", Zone = "scavengers_hub", Look = "Contractors", Dx = 1, Dy = 2, When = () => Story.Step("leak") == 2 },
            // Tier 4 (Story 8)
            new Def { Name = "Commander Miro", Landmark = "Command Post", Zone = "vanguard_barracks", Look = "Liberators", Fixed = "Liberator Guard Suit II", Dx = 2, Dy = 1, When = () => true },
            new Def { Name = "Tolik Brodsky", Landmark = "The Barrier", Zone = "vanguard_barracks", Look = "Liberators", Fixed = "Liberator Exoskeleton", Dx = 3, Dy = 2, When = () => Story.Step("barrier") < 2 || Story.IsDone("barrier") },
            new Def { Name = "Commander Dovbush", Landmark = "Sentinel Firebase", Zone = "vanguard_barracks", Look = "Sentinels", Fixed = "Sentinel Armored Suit", Dx = 1, Dy = 1, When = () => !(Story.Step("summit") >= 3) },
            new Def { Name = "Commander Dovbush", Landmark = "Command Post", Zone = "vanguard_barracks", Look = "Sentinels", Fixed = "Sentinel Armored Suit", Dx = -2, Dy = 1, When = () => Story.Step("summit") >= 3 },
            new Def { Name = "Hunter", Landmark = "Motor Pool", Zone = "vanguard_barracks", Look = "Wanderers", Fixed = "Wanderer Field Armor", Dx = 1, Dy = 1, When = () => true },
            new Def { Name = "Lev Marchuk", Suit = "Lev Marchuk",  Landmark = "The Rusty Anchor", Zone = "scavengers_hub", Look = "Wanderers", Dx = 1, Dy = 1, When = () => true },
            new Def { Name = "Dr. Priya Osei", Suit = "Dr. Priya Osei",  Landmark = "Sub-Station House", Zone = "scavengers_hub", Look = "Surveyors", Dx = 0, Dy = 2, When = () => Story.Step("lake") == 3 || Story.IsDone("lake") },
            new Def { Name = "Emil Rask", Suit = "Emil Rask",  Landmark = "Sub-Station House", Zone = "scavengers_hub", Look = "Surveyors", Dx = 2, Dy = 2, When = () => Story.Step("lake") == 3 || Story.IsDone("lake") },
            new Def { Name = "Konrad Hale", Suit = "Konrad Hale",  Landmark = "Sub-Station House", Zone = "scavengers_hub", Look = "Surveyors", Dx = -2, Dy = 2, When = () => Story.Step("lake") == 3 || Story.IsDone("lake") },
        };

        // ---- looks (Yan, 2026-11-18): the people of the story have a unique suit sprite each (ArtSource/make_npc_looks.py; UnitSprites.UniqueSuits); the minor ones wear a fixed suit of the game's own ----
        static readonly Dictionary<string, ArmorDef> Suits = new Dictionary<string, ArmorDef>();
        /// <summary>A unique look as an armour (the stats are a light suit's; the name picks the sprite).</summary>
        public static ArmorDef SuitOf(string name, float phys = 0.2f)
        {
            if (!Suits.TryGetValue(name, out var a)) Suits[name] = a = new ArmorDef { Name = name, PhysDR = phys, AnomDR = 0.1f, BeltSlots = 2, ModSlots = 0, Mp = 4, WeightKg = 4f };
            return a;
        }
        /// <summary>One of the game's own suits by name (null if there is none).</summary>
        public static ArmorDef Fixed(string name) => ArmorDatabase.Tiers.Values.Select(t => t.Values.FirstOrDefault(x => x.Name == name)).FirstOrDefault(x => x != null);
        static ArmorDef ArmorOf(Def d) => d.Suit != null ? SuitOf(d.Suit) : (d.Fixed != null ? Fixed(d.Fixed) : null) ?? EncounterGeneration.PickFactionLoadout(d.Look, "Experienced", 1).Item2;

        static readonly Dictionary<Def, HumanEnemy> Live = new Dictionary<Def, HumanEnemy>();
        public static string ZoneOverride;   // harness
        static string Here => ZoneOverride ?? TerrainMap.ZoneId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => Live.Clear();

        public static bool Holds(ICombatant u) => u is HumanEnemy h && Live.ContainsValue(h);

        /// <summary>The unit for a speaker's name ("Yefim"), or null: radio voices ("Yefim (radio)") and unknown names have none.</summary>
        public static HumanEnemy Find(string who)
        {
            if (string.IsNullOrEmpty(who) || who.Contains("(radio)")) return null;
            foreach (var kv in Live) if (kv.Value.IsAlive() && string.Equals(kv.Key.Name, who, System.StringComparison.OrdinalIgnoreCase) && EncounterState.Enemies.Contains(kv.Value)) return kv.Value;
            return null;
        }

        /// <summary>A map loaded: the old units went with it.</summary>
        public static void OnEnterZone() { Live.Clear(); Tick(); }

        public static void Tick()
        {
            if (Net.Mode == Net.Role.Client || TerrainMap.Landmarks == null) return;
            string here = Here;
            foreach (var d in Defs)
            {
                bool want = P(d.Zone) == here && d.When() || d.Zone == here && d.When();
                Live.TryGetValue(d, out var u);
                bool alive = u != null && u.IsAlive() && EncounterState.Enemies.Contains(u);
                if (!want) { if (u != null) { EncounterState.Enemies.Remove(u); Live.Remove(d); } continue; }
                if (alive) continue;
                var l = TerrainMap.Landmarks.FirstOrDefault(x => x.Name == d.Landmark); if (l == null) continue;
                var (x2, y2) = TerrainMap.FindNearestEnterable(l.X + d.Dx, l.Y + d.Dy, 4);
                if (EncounterState.Enemies.Any(e => e.IsAlive() && e.X == x2 && e.Y == y2) || Player.All.Any(p => p.X == x2 && p.Y == y2)) continue;
                var a = ArmorOf(d);
                var h = new HumanEnemy(d.Name, x2, y2, Faction, "Experienced", null, a) { IsAggroed = false, Unkillable = true };
                h.MaxHp = h.Hp = 300; h.Morale = "Steady";
                EncounterState.Enemies.Add(h); Live[d] = h;
            }
        }
    }
}
