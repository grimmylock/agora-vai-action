// Story 4 (Yan, 2026-10-29: "start building the tier 1 missions"): Tier 1's missions from STORY.md and STORY_DIALOGUE.md, on the
// Story.cs pieces. This batch: the first morning, the courier's pack (Yefim), the Raider camp (Stas), the cut fence (Yefim), the
// distress loop, and the Headman's key. Pavel, "those", the Culvert, the Husks and the defence come next.
// - Talking: E next to the person's place (Yefim at The Bonfire Camp, Stas at the Bunkhouse, the Headman at his house). They aren't
//   units on the map yet, so the panel shows their name without a portrait. ponytail: Stas stands in for the bar's barman at the
//   Bunkhouse until the village's bar is built (the starting-area rework).
// - The courier's body, the Raider camp and the distress loop are per-world spots (Story.Spot), never a place another mission uses.
// - Mission enemies are real squads placed at their spot when the job is taken (the tutorial's fixed packs, ACTION_DESIGN), holding
//   it like any quest crew. A job is done when its squad has nobody alive. ponytail: a squad that retreats off its spot alive keeps
//   its job open until someone finishes it.
// - Rewards go to every player (co-op).
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Tier1Story
    {
        public const string Yefim = "The Bonfire Camp", Stas = "Bunkhouse", Headman = "Headman's House", Fence = "Watchtower 3";
        const string Pack = "story_courier_pack", Note = "story_p_note";

        // the places missions already use: never a per-world spot
        static readonly string[] Used = { Fence, "Rust Culvert", "Flooded Bunker", "Fishing Village", Headman, "Fisherman's Shack", "Sunken Tanker",
            "Reed Blind", "Blind Curve", "Checkpoint Gate", "Chapel Crypt", "Pumping Station", "Pump Room", "Rail Depot" };

        static string SpotFor(string key) => Story.Spot(key, (TerrainMap.Landmarks ?? new List<LandmarkInstance>())
            .Where(l => TerrainMap.RegionAt(l.X, l.Y) == "bonfire_village" || l.Name.EndsWith("Checkpoint")).Select(l => l.Name).Concat(Used).ToArray());

        static bool OnTier1 => TerrainMap.ZoneId == "perimeter";

        // ---------------- the squads a mission puts down ----------------

        static void Place(string mission, string landmark, string faction, List<Mutant> members)
        {
            var l = TerrainMap.Landmarks.FirstOrDefault(x => x.Name == landmark);
            if (l == null) return;
            SquadManager.PlaceQuestSquad(TerrainMap.RegionAt(l.X, l.Y) ?? "perimeter", faction, members, "story:" + mission);
            int k = 0;
            foreach (var m in members)   // round the spot (Posts makes the nearest landmark their home, and quest crews hold it)
            {
                var (x, y) = TerrainMap.FindNearestEnterable(l.X + (k % 3) - 1, l.Y + (k / 3) + 1);
                m.X = x; m.Y = y; k++;
            }
        }

        static void Mutants(string mission, string landmark, string key, int n)
        {
            var def = MutantDatabase.Tiers["tier1"][key];
            Place(mission, landmark, "Mutant", Enumerable.Range(0, n).Select(_ => EncounterGeneration.CreateMutantFromDb(def, 0, 0)).ToList());
        }

        static void Raiders(string mission, string landmark, int n)
        {
            var list = new List<Mutant>();
            for (int i = 0; i < n; i++)
            {
                var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1, isLeader: i == 0);
                var h = new HumanEnemy("Raider", 0, 0, "Raiders", "Rookie", w, a);
                HumanLootSystem.RollSpawnStash(h, 1);
                list.Add(h);
            }
            Place(mission, landmark, "Raiders", list);
        }

        /// <summary>Has the mission's squad been put down, and is everyone in it dead?</summary>
        static bool Cleared(string mission)
        {
            var qs = MacroState.Current.OfflineSquads.Where(q => q.QuestId == "story:" + mission).ToList();
            return !qs.SelectMany(q => q.Members).Any(m => m.IsAlive());
        }

        static void Pay(int ru, string why)
        {
            foreach (var p in Player.All) p.Ru += ru;
            GameLog.Write($"+{ru} Ru ({why}).");
        }

        static void Give(string id, string name) =>
            Player.Current.AddItem(new InventoryItem { Id = id, Name = name, Type = "key", Qty = 1, WeightKg = 0.5f, Price = 0 });

        static Player Holder(string id) => Player.All.FirstOrDefault(p => p.Inventory.Items.Any(i => i.Id == id));

        // ---------------- E next to someone ----------------

        /// <summary>E pressed: a mission's conversation or action here? True if the story took the key press.</summary>
        public static bool TryInteract(Player p)
        {
            if (!OnTier1) return false;
            if (Story.Near(p, Yefim, 3)) return TalkYefim();
            if (Story.Near(p, Stas, 2)) return TalkStas();
            if (Story.Near(p, Headman, 2)) return TalkHeadman(p);
            if (Story.Step("courier") == 1 && Story.Near(p, Story.Spot("courier"), 2) && Holder(Pack) == null)
            {
                Give(Pack, "Courier's Pack");
                GameLog.Write("The pack is still on him. Something has been chewing the straps.");
                Story.SetObjective("courier", "Bring the courier's pack to Yefim", Yefim);
                return true;
            }
            if ((Story.Step("fence") == 1 || Story.Step("fence") == 2) && !Story.Has("fence_inspected") && Story.Near(p, Fence, 3))
            {
                Story.Set("fence_inspected");
                GameLog.Write("The wire isn't torn. It's cut - clean, square ends. Bolt cutters.");
                GameLog.Write("A scrap of dark grey cloth is caught on a barb.");
                Story.Journal("The fence at Watchtower 3 was cut with bolt cutters. A scrap of grey cloth was caught on the wire.");
                return true;
            }
            if (Story.Step("burst") == 2 && Story.Near(p, Story.Spot("distress"), 2))
            {
                Give(Note, "Note signed \"P.\"");
                GameLog.Write("A battered radio wired to a car battery, still looping. Under it, a note.");
                Story.Talk("Note", new[] { "\"Bunker under the marsh. Key with the fishermen - the Headman knows. Grey ones are looking for me. If you read this and you're not one of them, come. - P.\"" });
                Story.Journal("A note from \"P.\": a bunker under the marsh, the key with the fishing village's Headman, grey ones hunting him.");
                Story.Finish("burst");
                Story.SetStep("key", 1);
                Story.SetObjective("key", "Ask the Headman in the fishing village for the bunker's key", Headman);
                return true;
            }
            return false;
        }

        static bool TalkYefim()
        {
            if (!Story.IsDone("intro"))
            {
                Story.Talk("Yefim", new[] {
                    "Awake? Good. The Zone doesn't wait for anyone to finish their dreams.",
                    "I'm Yefim. I sell, I buy, I listen. Mostly I listen.",
                    "A courier was bringing me batteries and medicine. He was due two days ago.",
                    $"His last call came from {SpotFor("courier")}. If he's dead, his pack isn't. Bring it to me and I'll pay - better than the crows." },
                    new[] { "I'll get it.", "What killed him?" }, i =>
                    {
                        if (i == 1) Story.Talk("Yefim", new[] { "Maws, if I had to bet. Blind things, all teeth. They hear you before you see them. Walk slow. Shoot fast." });
                        StartCourier();
                    });
                Story.Finish("intro");
                return true;
            }
            if (Story.Step("courier") == 1 && Holder(Pack) is Player h)
            {
                h.RemoveItem(Pack, 1);
                Story.Talk("Yefim", new[] { "Medicine, batteries... and blood on all of it. Here. You earned this. Now go eat something - you look like the courier." });
                Pay(400, "the courier's pack");
                Story.Finish("courier");
                return true;
            }
            if (Story.IsDone("courier") && Story.Step("fence") == 0)
            {
                Story.Talk("Yefim", new[] {
                    "Something went through the fence at Watchtower 3 last night. Guards found the wire torn open.",
                    "Probably a beast. Probably. Go and see what we're dealing with before it comes back hungry." });
                Story.SetStep("fence", 1);
                Story.SetObjective("fence", "Find what broke through the fence", Fence);
                Mutants("fence", Fence, "PallidProwler", 1);
                return true;
            }
            if (Story.Step("fence") == 2)
            {
                if (Story.Has("fence_inspected"))
                {
                    Story.Talk("Yefim", new[] {
                        "Bolt cutters... A Prowler doesn't carry bolt cutters. And this cloth - nobody here wears grey.",
                        "Someone came into my village and walked out again, and the guards saw a beast. I don't like that at all.",
                        "Take this. Extra. For keeping your eyes open when everyone else closed theirs." });
                    Pay(600, "the cut fence, and the closer look");
                }
                else
                {
                    Story.Talk("Yefim", new[] { "A Prowler. Of course. They test the fence every spring. Good work - here." });
                    Pay(300, "the fence");
                }
                Story.Finish("fence");
                return true;
            }
            if (Story.Step("burst") == 1)
            {
                Story.Talk("Yefim", new[] {
                    "Three hours it's been saying that. Same words, same place. A loop.",
                    "Whoever set it running might be dead. Might not.",
                    $"It's coming from {SpotFor("distress")}. If it's a trap, come back. If it's a man, bring him back." });
                Story.SetStep("burst", 2);
                Story.SetObjective("burst", "Find where the distress loop comes from", Story.Spot("distress"));
                return true;
            }
            return false;   // nothing to say: E falls through (the camp, the bonfire)
        }

        static void StartCourier()
        {
            Story.SetStep("courier", 1);
            Story.SetObjective("courier", "Find Yefim's courier and his pack", SpotFor("courier"));
            Mutants("courier", Story.Spot("courier"), "BlindMaw", 3);
            Story.Journal("Yefim's courier went quiet near " + Story.Spot("courier") + ". Find his pack.");
        }

        static bool TalkStas()
        {
            if (Story.IsDone("intro") && Story.Step("raiders") == 0)
            {
                Story.Talk("Stas", new[] {
                    "You. Yes, you, with the face.",
                    $"Three Raiders set up camp at {SpotFor("raiders")}. They've been stopping our people on the road, taking \"tolls\".",
                    "The guards won't leave the fence. I'm not asking you to start a war. I'm asking you to end a small one.",
                    "Don't fight three. Fight one, then another, then another. They sleep in shifts. Stay low, stay behind them. A knife is quieter than a rifle." },
                    new[] { "Three of them. Fine.", "What's in it for me?" }, i =>
                    {
                        if (i == 1) Story.Talk("Stas", new[] { "Their stash, my gratitude, and a drink that isn't made from radiator fluid. Mostly the stash." });
                    });
                Story.SetStep("raiders", 1);
                Story.SetObjective("raiders", "Clear the Raider camp", Story.Spot("raiders"));
                Raiders("raiders", Story.Spot("raiders"), 3);
                return true;
            }
            if (Story.Step("raiders") == 2)
            {
                Story.Talk("Stas", new[] { "Quiet work. I like quiet. Here - and the drink's real. Mostly." });
                Pay(500, "the Raider camp");
                Story.Finish("raiders");
                return true;
            }
            return false;
        }

        static bool TalkHeadman(Player p)
        {
            int step = Story.Step("key");
            if (step == 1)
            {
                Story.Talk("Headman Petro", new[] {
                    "The bunker? Nobody goes in the bunker. The water came up and took half of it, and what lives in the other half doesn't like visitors.",
                    "I have the key. My father had it before me. Why would I give it to you?" },
                    new[] { "A man's dying in there.", "I'll pay. (1500 Ru)", "I'm taking it." }, i =>
                    {
                        if (i == 0)
                        {
                            Story.Talk("Headman Petro", new[] { "Men die everywhere. ...But. Prowlers nest by the Fisherman's Shack and take our nets, and our boys. Clear them, and the key is yours." });
                            Story.SetStep("key", 2);
                            Story.SetObjective("key", "Clear the Prowlers at the Fisherman's Shack", "Fisherman's Shack");
                            Mutants("key", "Fisherman's Shack", "PallidProwler", 2);
                        }
                        else if (i == 1 && p.Ru >= 1500) { p.Ru -= 1500; Story.Talk("Headman Petro", new[] { "Then pay well. The key doesn't care who holds it. I do, a little." }); GiveKey(); }
                        else if (i == 1) Story.Talk("Headman Petro", new[] { "That isn't 1500. Come back when it is." });
                        else
                        {
                            Story.Talk("Headman Petro", new[] { "Then take it, and take my curse with it. The village will remember your face." });
                            Story.Set("petro_robbed");
                            foreach (var pl in Player.All) pl.AdjustReputation("Wanderers", -Player.RepPoint);
                            GiveKey();
                        }
                    });
                return true;
            }
            if (step == 3)
            {
                Story.Talk("Headman Petro", new[] { "The nets are ours again. Here. My father's key. Whatever's down there - don't let it follow you back." });
                GiveKey();
                return true;
            }
            return false;
        }

        static void GiveKey()
        {
            MacroState.Current.Keys.Add("flooded_bunker");
            GameLog.Write("You have the Flooded Bunker's key.");
            Story.Finish("key");
            Story.SetStep("engineer", 1);
            Story.SetObjective("engineer", "Find \"P.\" in the Flooded Bunker", "Flooded Bunker");
        }

        // ---------------- every game minute ----------------

        /// <summary>TurnLoop.TickAwareness: jobs that finish on their own (a squad wiped out), and the distress loop at night.</summary>
        public static void Tick()
        {
            if (!OnTier1) return;
            if (Story.Step("intro") == 0) { Story.SetStep("intro", 1); Story.SetObjective("intro", "Talk to Yefim by the fire", Yefim); }
            if (Story.Step("fence") == 1 && Cleared("fence")) { Story.SetStep("fence", 2); Story.SetObjective("fence", "Tell Yefim what you found (or look closer at the fence first)", Yefim); }
            if (Story.Step("raiders") == 1 && Cleared("raiders")) { Story.SetStep("raiders", 2); Story.SetObjective("raiders", "Tell Stas the camp is clear", Stas); }
            if (Story.Step("key") == 2 && Cleared("key")) { Story.SetStep("key", 3); Story.SetObjective("key", "Back to the Headman", Headman); }
            float h = MacroState.Current.Hour;
            if (Story.Step("burst") == 0 && Story.IsDone("fence") && Story.IsDone("raiders") && (h >= 21f || h < 5f))
            {
                Story.Radio("Yefim's radio", "...anyone... this is... I have the... I can't carry it any... please...");
                Story.SetStep("burst", 1);
                Story.SetObjective("burst", "Ask Yefim about the voice on his radio", Yefim);
            }
        }
    }
}
