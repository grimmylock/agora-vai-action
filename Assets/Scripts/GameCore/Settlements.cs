// 2026-11-24 (Yan: "fix all of the safe zones and faction bases ... create an actual world with actual stuff in it, the bars, safe zones have a medic with medical stuff, places to sleep ... I want it to NOT feel empty"):
// the people of the settled places. Each Def is a crew (a Wanderer / Sentinel / Liberator ... squad on a story post, so it holds its landmark and is fought for through the normal squad skirmish) made once per game, host only,
// when the map that has its landmark loads (StoryNpcs.OnEnterZone), before the first frame - nothing pops in. Services: E at a medic's landmark patches you up for RU, E at a bar buys a drink and the news (rumours).
// Sleeping is the existing Safe Zone rest (WorldDays.Rest), the cots are props.
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public static class Settlements
    {
        class Def { public string Tag, Landmark, Faction, Who, Rank; public int N; }
        static Def D(string tag, string lm, string faction, string who, string rank, int n) => new Def { Tag = tag, Landmark = lm, Faction = faction, Who = who, Rank = rank, N = n };

        static readonly List<Def> Defs = new List<Def>
        {
            // Bonfire Village (Safe Zone, Tier 1)
            D("village_guards", "The Bonfire Camp", "Wanderers", "Village Guard", "Experienced", 3),
            D("village_medic", "Medic's House", "Wanderers", "Medic", "Rookie", 2),
            D("village_bench", "Repair Bench", "Wanderers", "Villager", "Rookie", 2),
            D("village_bunk", "Bunkhouse", "Wanderers", "Villager", "Rookie", 2),
            D("village_bar", "The Smoking Barrel", "Wanderers", "Barman", "Rookie", 3),
            D("village_stall", "Yefim's Footlocker", "Wanderers", "Villager", "Rookie", 1),
            // Scavengers' Hub (Safe Zone, Tier 3)
            D("hub_bar", "The Rusty Anchor", "Wanderers", "Barman", "Professional", 4),
            D("hub_bazaar", "The Bazaar", "Wanderers", "Trader", "Professional", 4),
            D("hub_clinic", "Hub Clinic", "Wanderers", "Medic", "Professional", 2),
            D("hub_bunk", "Hub Bunkhouse", "Wanderers", "Lodger", "Professional", 2),
            D("hub_yard", "Repair Yard", "Wanderers", "Mechanic", "Professional", 2),
            D("hub_barracks", "Sentinel Barracks", "Sentinels", "Sentinel", "Veteran", 3),
            D("hub_gate", "Sentinel Checkpoint", "Sentinels", "Sentinel", "Veteran", 3),
            // Vanguard Barracks (Safe Zone, Tier 4)
            D("van_post", "Command Post", "Liberators", "Liberator", "Expert", 3),
            D("van_row", "Barracks Row", "Liberators", "Liberator", "Expert", 4),
            D("van_aid", "Aid Post", "Liberators", "Medic", "Expert", 2),
            D("van_parade", "Parade Ground", "Liberators", "Liberator", "Expert", 4),
            D("van_motor", "Motor Pool", "Liberators", "Mechanic", "Expert", 2),
            D("van_sentinels", "Sentinel Firebase", "Sentinels", "Sentinel", "Expert", 3),
            // faction bases on the open maps
            D("base_army_camp", "Army Field Camp", "Garrison", "Garrison Soldier", "Professional", 3),
            D("base_gatehouse", "Gatehouse", "Garrison", "Garrison Soldier", "Professional", 3),
            D("base_outpost", "Sentinel Outpost", "Sentinels", "Sentinel", "Veteran", 3),
            D("base_barricade", "Garrison Barricade", "Garrison", "Garrison Soldier", "Veteran", 3),
            D("base_armory", "Garrison Armory", "Garrison", "Garrison Soldier", "Veteran", 3),
            D("base_shrine", "Obelisk Shrine", "Obelisk", "Obelisk Acolyte", "Expert", 3),
        };

        public static readonly string[] Medics = { "Medic's House", "Hub Clinic", "Aid Post" };
        public static readonly string[] Bars = { "The Smoking Barrel", "The Rusty Anchor" };
        public const int TreatCost = 30, DrinkCost = 15;

        static int TierAt(LandmarkInstance l) => GameData.MacroMapDatabase.Find(TerrainMap.RegionAt(l.X, l.Y))?.Tier ?? 1;

        /// <summary>A map loaded: every crew whose landmark is on it and has not been made yet is made. Host only (a joined friend mirrors the world).</summary>
        public static void Populate()
        {
            if (Net.Mode == Net.Role.Client || TerrainMap.Landmarks == null) return;
            foreach (var d in Defs)
            {
                if (Story.Has("settled_" + d.Tag)) continue;
                var l = TerrainMap.Landmarks.FirstOrDefault(x => x.Name == d.Landmark);
                if (l == null) continue;
                Story.Set("settled_" + d.Tag);
                var list = new List<Mutant>(); int tier = TierAt(l);
                for (int i = 0; i < d.N; i++)
                {
                    var (w, a) = EncounterGeneration.PickFactionLoadout(d.Faction, d.Rank, tier, i == 0);
                    if (w == null && a == null) break;
                    list.Add(new HumanEnemy(d.Who, 0, 0, d.Faction, d.Rank, w, a));
                }
                if (list.Count > 0) Tier1Story.Place(d.Tag, d.Landmark, d.Faction, list);
            }
        }

        static LandmarkInstance Near(Player p, IEnumerable<string> names) =>
            TerrainMap.Landmarks.FirstOrDefault(l => names.Contains(l.Name) && System.Math.Max(System.Math.Abs(l.X - p.X), System.Math.Abs(l.Y - p.Y)) <= 3);

        public static string PromptAt(Player p) => Near(p, Medics) != null ? "Ask the medic for help" : Near(p, Bars) != null ? "Talk to the barman" : null;

        /// <summary>E at a medic's or a bar: true if handled.</summary>
        public static bool TryInteract(Player p)
        {
            if (p == null || EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed && CombatantUtil.Distance(p, e) <= 12)) return false;
            if (Near(p, Medics) != null)
            {
                bool hurt = p.Hp < p.MaxHp || p.StatusEffects.Bleeding;
                ChoiceUI.Show("MEDIC", hurt ? $"\"Sit. Let me see.\" Patching you up costs {TreatCost} RU." : "\"You look fine to me. Come back when you are not.\"",
                    hurt ? new List<string> { $"Patch me up ({TreatCost} RU)", "Not now" } : new List<string> { "Thanks" }, i =>
                    {
                        if (!hurt || i != 0) return;
                        if (p.Ru < TreatCost) { GameLog.Write("You cannot pay the medic."); return; }
                        p.Ru -= TreatCost; p.HealPermanent(p.MaxHp); p.StatusEffects.Bleeding = false; Body.Heal(p, 100000); while (Body.Splint(p)) { }
                        GameLog.Write($"The medic cleans you up and binds you (-{TreatCost} RU).");
                    });
                return true;
            }
            if (Near(p, Bars) != null)
            {
                ChoiceUI.Show("BARMAN", $"\"What'll it be?\" A drink and whatever the room is saying costs {DrinkCost} RU.", new List<string> { $"A drink ({DrinkCost} RU)", "Not now" }, i =>
                {
                    if (i != 0) return;
                    if (p.Ru < DrinkCost) { GameLog.Write("You cannot pay for a drink."); return; }
                    p.Ru -= DrinkCost; p.Tiredness = System.Math.Max(0, p.Tiredness - 1);
                    GameLog.Write($"You drink and listen (-{DrinkCost} RU).");
                    WorldEvents.TellRumors();
                });
                return true;
            }
            return false;
        }
    }
}
