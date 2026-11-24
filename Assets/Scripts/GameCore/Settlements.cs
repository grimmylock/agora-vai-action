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
        class Def { public string Tag, Landmark, Faction, Who, Rank, Lead; public int N; }
        static Def D(string tag, string lm, string faction, string who, string rank, int n, string lead = null) => new Def { Tag = tag, Landmark = lm, Faction = faction, Who = who, Rank = rank, N = n, Lead = lead };

        static readonly List<Def> Defs = new List<Def>
        {
            // Bonfire Village (Safe Zone, Tier 1)
            D("village_guards", "The Bonfire Camp", "Wanderers", "Village Guard", "Experienced", 3),
            D("village_medic", "Medic's House", "Wanderers", "Medic", "Rookie", 2, "Nadia"),
            D("village_bench", "Repair Bench", "Wanderers", "Villager", "Rookie", 2),
            D("village_bunk", "Bunkhouse", "Wanderers", "Villager", "Rookie", 2),
            D("village_bar", "The Smoking Barrel", "Wanderers", "Barman", "Rookie", 3, "Grisha"),
            D("village_stall", "Yefim's Footlocker", "Wanderers", "Villager", "Rookie", 1),
            // Scavengers' Hub (Safe Zone, Tier 3)
            D("hub_bar", "The Rusty Anchor", "Wanderers", "Barman", "Professional", 4, "Marta"),
            D("hub_bazaar", "The Bazaar", "Wanderers", "Trader", "Professional", 4),
            D("hub_clinic", "Hub Clinic", "Wanderers", "Medic", "Professional", 2, "Doctor Ilko"),
            D("hub_bunk", "Hub Bunkhouse", "Wanderers", "Lodger", "Professional", 2),
            D("hub_yard", "Repair Yard", "Wanderers", "Mechanic", "Professional", 2),
            D("hub_barracks", "Sentinel Barracks", "Sentinels", "Sentinel", "Veteran", 3),
            D("hub_gate", "Sentinel Checkpoint", "Sentinels", "Sentinel", "Veteran", 3),
            // Vanguard Barracks (Safe Zone, Tier 4)
            D("van_post", "Command Post", "Liberators", "Liberator", "Expert", 3),
            D("van_row", "Barracks Row", "Liberators", "Liberator", "Expert", 4),
            D("van_aid", "Aid Post", "Liberators", "Medic", "Expert", 2, "Sergeant Orsa"),
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
                    list.Add(new HumanEnemy(i == 0 && d.Lead != null ? d.Lead : d.Who, 0, 0, d.Faction, d.Rank, w, a));
                }
                if (list.Count > 0) Tier1Story.Place(d.Tag, d.Landmark, d.Faction, list);
            }
        }

        static LandmarkInstance Near(Player p, IEnumerable<string> names) =>
            TerrainMap.Landmarks.FirstOrDefault(l => names.Contains(l.Name) && System.Math.Max(System.Math.Abs(l.X - p.X), System.Math.Abs(l.Y - p.Y)) <= 3);

        public static string PromptAt(Player p) => Near(p, Medics) != null ? "Talk to the medic" : Near(p, Bars) != null ? "Talk to the barman" : null;

        // ---- talk (Yan: "npc's should have dialogue, not just a press E to get healed"): each place has a named person, a greeting, topics to ask about, and then the service ----
        class Talk { public string Name, Greeting, Role; public (string ask, string[] reply)[] Topics; }
        static readonly Dictionary<string, Talk> Talks = new Dictionary<string, Talk>
        {
            ["Medic's House"] = new Talk { Name = "Nadia", Role = "medic", Greeting = "\"Door's open, boots off the mat. Who did this to you?\"", Topics = new[] {
                ("Who is hurt in the village?", new[] { "\"Everyone, a little. The ones who come in at night are the ones I worry about. Husks leave something in a cut that does not wash out.\"" }),
                ("Where did you learn this?", new[] { "\"A hospital that does not exist any more. Pavel taught me the rest, and Pavel is the kind of man who tells you the truth about your stitches.\"" }),
                ("Any advice?", new[] { "\"Keep a splint in the pack, and do not run on a broken leg. Sleep somewhere with a fire. I charge less than the Zone does.\"" }) } },
            ["Hub Clinic"] = new Talk { Name = "Doctor Ilko", Role = "medic", Greeting = "\"Sit. Do not touch the shelves. They are in an order only I understand.\"", Topics = new[] {
                ("Busy?", new[] { "\"Sentinels with burns, scavengers with broken wrists, the odd Contractor who thinks he is made of iron. A slow day is a good day.\"" }),
                ("What do you need?", new[] { "\"Clean bandages and silence. I can only supply one of those, and it is not the silence.\"" }),
                ("What happened to the lake?", new[] { "\"People who go out to the old pier come back quiet. I treat the body. The rest is somebody else's department.\"" }) } },
            ["Aid Post"] = new Talk { Name = "Sergeant Orsa", Role = "medic", Greeting = "\"State your wounds, soldier. Quickly. The line does not stop for you.\"", Topics = new[] {
                ("How bad is the Barrier?", new[] { "\"Worse each month. They send me boys who held it for a week and cannot hold a cup. We patch them and send them back. That is the job.\"" }),
                ("Who commands here?", new[] { "\"Commander Miro. Ask him anything you like. He will answer with a question about your intentions. I only ask about your wounds.\"" }),
                ("Anything I should carry?", new[] { "\"Field dressings, a splint, and an AP round for whatever stands in front of you. Do not count on the Sentinels to cover your flank.\"" }) } },
            ["The Smoking Barrel"] = new Talk { Name = "Grisha", Role = "bar", Greeting = "\"Sit anywhere. If it wobbles, it is a stool. If it does not, it is a table.\"", Topics = new[] {
                ("What is the barrel?", new[] { "\"The barrel is none of your business. It is also why the roof has not caught fire. Draw your own conclusions.\"" }),
                ("Who drinks here?", new[] { "\"Wanderers back from the marsh, guards coming off the fence, Stas when he thinks Yefim is not counting. Everybody talks. I just pour.\"" }),
                ("Heard anything?", new[] { "\"A truck went down by the Culvert and did not come up. The Raiders are quiet. Quiet is when I count the bottles twice.\"" }) } },
            ["The Rusty Anchor"] = new Talk { Name = "Marta", Role = "bar", Greeting = "\"Welcome to the Anchor. The soup is a rumour. The beer is real.\"", Topics = new[] {
                ("Who drinks here?", new[] { "\"Contractors on the clock, Sentinels off it, Wanderers who owe me money. Voss comes on Fridays, if the Zone has Fridays.\"" }),
                ("Any work going?", new[] { "\"The board by the door, and whatever the room is whispering. Pay attention to the quiet table. They are the ones with jobs.\"" }),
                ("Anything about the lake?", new[] { "\"Nobody fishes there now. A man came in last week, drank four, and left a note on the counter. It said 'do not'. Just that.\"" }) } },
        };

        static string TalkKey(IEnumerable<string> names, Player p) { var l = Near(p, names); return l?.Name; }

        /// <summary>E at a medic's or a bar: true if handled. A greeting, topics to ask about, and the service.</summary>
        public static bool TryInteract(Player p)
        {
            if (p == null || EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed && CombatantUtil.Distance(p, e) <= 12)) return false;
            string key = TalkKey(Medics, p) ?? TalkKey(Bars, p);
            if (key == null || !Talks.TryGetValue(key, out var t)) return false;
            var who = EncounterState.Enemies.OfType<HumanEnemy>().FirstOrDefault(h => h.IsAlive() && h.Name == t.Name);
            Menu(p, t, t.Greeting, who);
            return true;
        }

        static void Menu(Player p, Talk t, string said, HumanEnemy who)
        {
            bool medic = t.Role == "medic", hurt = p.Hp < p.MaxHp || p.StatusEffects.Bleeding;
            var opts = new List<string>();
            opts.Add(medic ? (hurt ? $"Patch me up ({TreatCost} RU)" : "I am fine, just talking") : $"A drink ({DrinkCost} RU)");
            foreach (var tp in t.Topics) opts.Add(tp.ask);
            if (!medic) opts.Add("What is the news? (rumours)");
            opts.Add("Goodbye");
            ChoiceUI.Show(t.Name.ToUpperInvariant(), said, opts, i =>
            {
                if (i == opts.Count - 1) { GameLog.Write(medic ? $"{t.Name} nods and goes back to work." : $"{t.Name} wipes the counter and nods."); return; }
                if (i == 0)
                {
                    if (medic && hurt)
                    {
                        if (p.Ru < TreatCost) { Menu(p, t, "\"I would like to, but I do not work for free. Come back with " + TreatCost + " RU.\"", who); return; }
                        p.Ru -= TreatCost; p.HealPermanent(p.MaxHp); p.StatusEffects.Bleeding = false; Body.Heal(p, 100000); while (Body.Splint(p)) { }
                        GameLog.Write($"{t.Name} cleans you up and binds you (-{TreatCost} RU).");
                        Menu(p, t, "\"There. Do not make me do it twice in one day.\"", who); return;
                    }
                    if (!medic)
                    {
                        if (p.Ru < DrinkCost) { Menu(p, t, "\"On credit? Ha. No.\"", who); return; }
                        p.Ru -= DrinkCost; p.Tiredness = System.Math.Max(0, p.Tiredness - 1);
                        GameLog.Write($"You drink with {t.Name} (-{DrinkCost} RU).");
                        Menu(p, t, "\"Good. Now you look less like a funeral.\"", who); return;
                    }
                    Menu(p, t, "\"Then sit a minute. Ask what you like.\"", who); return;
                }
                if (!medic && i == opts.Count - 2) { WorldEvents.TellRumors(); Menu(p, t, "\"That is all I have. If you hear more, I want to hear it.\"", who); return; }
                var topic = t.Topics[i - 1]; Menu(p, t, string.Join("\n", topic.reply), who);
            }, who);
        }
    }
}
