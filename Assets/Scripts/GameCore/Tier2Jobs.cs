// Story 6a (2026-11-18): Tier 2's jobs from STORY_DIALOGUE.md that follow "Taken" (Tier2Story.cs): "The Scrap King's Call" and "Yefim's Last Request: Bogdan".
// They replace the quartermaster arcs' SCRAPKING-1 and YEFIM-3 (QuestSystem.StoryJobs hides those two) and move the arcs on the same way (NpcArcStage).
// - Scrap King: a day after "Taken" he calls (a talk) and wants the rival crew gone from the Loading Dock (5 real Raiders put down there); back at his Nest: 900 Ru.
// - Bogdan: once Yefim's keepsake job is done (NpcArcStage bonfire_village >= 3) and Tier 2 is open, Yefim calls; Bogdan and four Raiders hold the Tire Fort. E next to Bogdan:
//   fight, or tell him Yefim remembers - he stands his crew down (a truce with that squad), gives you his brother's dog tag; Yefim (Tier 1, the fire) takes it: 900 Ru.
//   Or kill them all: Yefim is told: 700 Ru.
// - Steps: "kingcall" 0 not yet, 1 the talk done / dock crew out, 2 dock clear (go back), done. "bogdan" 1 go, 2 talking, 3 (killed) or 4 (tag) back to Yefim, done.
// ponytail: the crews hold their places like any quest squad; a gang that sees you first fights you (no shakedown above Tier 1), so the talk may come mid-fight. Rewards to every player.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Tier2Jobs
    {
        public const string Runner = "The Cutting", Bridge = "Chokepoint Bridge", King = "Scrap King's Nest", Dock = "Loading Dock", Fort = "Tire Fort", Tag = "story_dogtag";
        static string Map2 => MacroMapDatabase.MapOf("scrapyard");
        public static string ZoneOverride;   // harness: pretend to be on the Tier 2 map
        internal static bool OnTier2 => (ZoneOverride ?? TerrainMap.ZoneId) == Map2;

        internal static void CompleteArc(string npcId, int stage) { foreach (var p in Player.All) if (!p.NpcArcStage.TryGetValue(npcId, out var s) || s <= stage) p.NpcArcStage[npcId] = stage + 1; }

        public static bool TryInteract(Player p)
        {
            if (Tier2Escort.TryInteract(p)) return true;
            if (TerrainMap.ZoneId == "perimeter" && Story.Step("runner") == 1 && Story.Near(p, Runner, 3)) { TalkRunner(); return true; }
            if (OnTier2 && Story.Step("bridge") == 1) { var sg = Sergeant(); if (sg != null && CombatantUtil.Distance(p, sg) <= 3) { TalkBridge(sg, p); return true; } }
            if (OnTier2 && Story.Near(p, King, 3) && TalkKing()) return true;
            if (OnTier2 && (Story.Step("rooke") == 1 || Story.Step("rooke") == 2)) { var rk = EncounterState.Enemies.OfType<HumanEnemy>().FirstOrDefault(e => e.Name == "Rooke" && e.IsAlive()); if (rk != null && CombatantUtil.Distance(p, rk) <= 3) { TalkRooke(rk); return true; } }
            if (OnTier2 && Story.Step("crusher") == 2 && Story.Near(p, "Crusher Row", 4))
            {
                Story.Talk("Crusher Row", new[] { "In the crew boss's coat: a pay note, creased from folding. Sealed with a Garrison stamp." });
                Story.SetStep("crusher", 3); Story.SetObjective("crusher", "Show the Scrap King the note", King); return true;
            }
            if (OnTier2 && (Story.Step("bogdan") == 1 || Story.Step("bogdan") == 2)) { var b = Bogdan(); if (b != null && CombatantUtil.Distance(p, b) <= 3) { TalkBogdan(b, p); return true; } }
            return false;
        }

        // ---------------- the Scrap King ----------------

        static bool TalkKing()
        {
            if (Story.IsDone("kingcall") && TalkKingJobs()) return true;
            if (Story.Step("kingcall") != 2) return false;
            Story.Talk("The Scrap King", new[] { "The Dock is mine again. You're the first thing that came out of the Basin useful.", "Here. Don't spend it all in one place. It's the only place there is." });
            Tier1Story.Pay(900, "the Loading Dock");
            foreach (var pl in Player.All) pl.AdjustReputation("Raiders", Player.RepPoint);
            CompleteArc("scrapyard", 1);
            Story.Journal("The Scrap King: the Loading Dock is his again; he'll want more.");
            Story.Finish("kingcall");
            return true;
        }

        // ---------------- the King's other jobs: Crusher Row (the backer) and Rooke at the Maze ----------------
        // "crusher": 1 go (a rival scavenger crew of four at Crusher Row, and a Husk pack that came with them), 2 clear (E at Crusher Row: a Garrison-sealed pay note), 3 tell the King.
        // "rooke": 1 go (Rooke, Expert, and five enforcers at the Maze); E by Rooke: fight, or take his better offer (side with him); killed: tell the King. SCRAPKING-2 and -3 of the quartermaster arc.

        static bool TalkKingJobs()
        {
            int c = Story.Step("crusher"), r = Story.Step("rooke");
            if (c == 3)
            {
                Story.Talk("The Scrap King", new[] { "A note sealed with a Garrison stamp. Pay for a crew to bleed my yard. The Garrison. Of course.", "They don't want the Dock. They want the Basin quiet and everyone in it hungry. Someone is feeding them money - and orders." });
                Tier1Story.Pay(1000, "Crusher Row"); CompleteArc("scrapyard", 2); Story.Set("king_garrison_backer");
                Story.Journal("Crusher Row: the rival crew was paid by the Garrison. Someone above them gives the orders.");
                Story.Finish("crusher"); return true;
            }
            if (c == 0)
            {
                Story.Talk("The Scrap King", new[] { "A rival crew moved into Crusher Row. Scavengers, they say. Scavengers don't pay for good rifles. Find out who does." });
                Story.SetStep("crusher", 1); Story.SetObjective("crusher", "Find out who backs the crew in Crusher Row", "Crusher Row");
                Story.Journal("Crusher Row: someone is backing a rival crew on the Scrap King's yard.");
                var crew = new List<Mutant>();
                for (int i = 0; i < 4; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", "Experienced", 2, i == 0); crew.Add(new HumanEnemy(i == 0 ? "Crew Boss" : "Rival Scavenger", 0, 0, "Raiders", "Experienced", w, a)); }
                Tier1Story.Place("crusher", "Crusher Row", "Raiders", crew);
                if (Story.Has("husks_known")) Tier1Story.Place("crusher_husks", "Crusher Pit", Husk.Faction, Husk.Pack(2, 4));   // the pit is where they wander in from
                return true;
            }
            if (r == 3 || r == 4)
            {
                Story.Talk("The Scrap King", new[] { r == 3 ? "Rooke's dead? Good. A yard has room for one king." : "...You took Rooke's money. I won't forget it. Don't come to my Nest again." });
                if (r == 3) { Tier1Story.Pay(1500, "Rooke"); foreach (var pl in Player.All) pl.AdjustReputation("Raiders", Player.RepPoint); }
                CompleteArc("scrapyard", 3); Story.Journal(r == 3 ? "The Maze: Rooke is dead. The Scrap King holds the yard." : "The Maze: you sided with Rooke. The King is hostile now.");
                Story.Finish("rooke"); return true;
            }
            int sp = Story.Step("spool");                                     // Story 9 (2026-11-19): "The Exile" - Spool at the Dog Kennels (SpoolBoss.cs)
            if (sp == 2) { TalkSpoolDone(); return true; }
            if (sp == 1) { Story.Talk("The Scrap King", new[] { "The Dog Kennels, south-east, where the fence is. He will be on the crate with the bat. Don't talk to him for long." }); return true; }
            if (c >= 1 && c <= 2 || r == 1 || r == 2) { Story.Talk("The Scrap King", new[] { "Well? Go on." }); return true; }
            if (Story.IsDone("crusher") && r == 0)
            {
                Story.Talk("The Scrap King", new[] { "Rooke. He runs the Maze with six guns and a grudge. I want him gone. Kill him, or I'll find someone who will." });
                Story.SetStep("rooke", 1); Story.SetObjective("rooke", "Deal with Rooke and his enforcers at the Maze", "The Maze");
                Story.Journal("The Maze: the Scrap King wants Rooke and his enforcers dead.");
                var gang = new List<Mutant>();
                for (int i = 0; i < 6; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", i == 0 ? "Expert" : "Experienced", 2, i == 0); gang.Add(new HumanEnemy(i == 0 ? "Rooke" : "Rooke's Enforcer", 0, 0, "Raiders", i == 0 ? "Expert" : "Experienced", w, a)); }
                Tier1Story.Place("rooke", "The Maze", "Raiders", gang);
                return true;
            }
            if (Story.IsDone("rooke") && !Story.Has("rooke_sided") && sp == 0)
            {
                Story.Talk("The Scrap King", new[] { "Spool. You will have heard the name, or seen a caravan with its crates gone and its drivers laid out in the road.",
                    "He was mine once. My enforcer, my dogs, my kennels. Now he sits in my kennels with a bat wrapped in wire and calls the yard his.",
                    "Kill him. And bring me whatever he carries in that coat. Papers, mostly. A man like that keeps papers, and I would like them in my stove, not in a stranger's hands." },
                    new[] { "I'll go.", "What did he do?" }, i =>
                {
                    if (i == 1) Story.Talk("The Scrap King", new[] { "He talked. He talked about me, to anyone who would sit still. I put him out of the Nest and he took the dogs with him. That is all." });
                    Story.SetStep("spool", 1); Story.SetObjective("spool", "Deal with Spool at the Dog Kennels", SpoolBoss.Lair);
                    Story.Journal("The Exile: the Scrap King wants his old enforcer Spool dead and his papers brought to him. Spool holds the Dog Kennels with a barbed wire bat.");
                });
                return true;
            }
            return false;
        }

        /// <summary>E at the King with the Kennel Ledger (step 2): hand it over or say he had nothing.</summary>
        static void TalkSpoolDone()
        {
            if (!(Tier1Story.Holder(SpoolBoss.Ledger) is Player h)) { Story.Talk("The Scrap King", new[] { "You have nothing for me? Then he is not dead, or you are a liar. Go and look again." }); return; }
            Story.Talk("The Scrap King", new[] { "Well. Is he dead? ...Good. And the papers?" }, new[] { "Here. All of it.", "He had nothing on him." }, i => SpoolOutcome(h, i));
        }

        internal static void SpoolOutcome(Player h, int i)
        {
            if (i == 0)
            {
                h.RemoveItem(SpoolBoss.Ledger, 1);
                Story.Talk("The Scrap King", new[] { "(He reads one line, folds it, and feeds it to the stove.) Sick dogs bite. You did the right thing. Do not repeat what you read.", "Twelve hundred. It's more than the bat was worth." });
                Tier1Story.Pay(1200, "Spool"); foreach (var pl in Player.All) pl.AdjustReputation("Raiders", Player.RepPoint);
                Story.Set("spool_ledger_given"); Story.Journal("The Exile: you gave the King Spool's ledger and he burned it. He paid well, and he knows you read it.");
            }
            else
            {
                Story.Talk("The Scrap King", new[] { "...Nothing. In that coat. Of course. (A long look.) Six hundred, then. For the walk." });
                Tier1Story.Pay(600, "Spool");
                Story.Set("spool_ledger_kept"); Story.Journal("The Exile: you kept Spool's ledger. The King sold the Garrison the hatch under the Loading Dock, and he suspects you know. The ledger is worth something to the right people.");
            }
            CompleteArc("scrapyard", 4); Story.Finish("spool");
        }

        static void TalkRooke(HumanEnemy b)
        {
            Story.Talk("Rooke", new[] { "The King's dog. Sit. Whatever he's paying you, I'll double it. He's old, he's slow, and he's been wrong about the Basin since the day he climbed his heap." }, new[] { "(Fight)", "...Double? Let's talk." }, i =>
            {
                if (i == 0) { Story.SetStep("rooke", 2); return; }
                Story.Talk("Rooke", new[] { "Smart. Here - half now. Tell him the Maze took you in." });
                Tier1Story.Pay(1500, "Rooke's offer"); foreach (var pl in Player.All) pl.AdjustReputation("Raiders", -2 * Player.RepPoint);
                int until = (int)((MacroState.Current.Day * 24f + MacroState.Current.Hour) * 60f) + 3 * 24 * 60;
                var sq = Shakedown.SquadOf(b); if (sq != null) { sq.TruceUntil = until; foreach (var m in sq.Members) m.IsAggroed = false; }
                Story.Set("rooke_sided"); Story.SetStep("rooke", 4); Story.SetObjective("rooke", "Take the news to the Scrap King", King);
            });
            if (Story.Step("rooke") == 1) Story.SetStep("rooke", 2);
        }

        // ---------------- Bogdan ----------------

        static HumanEnemy Bogdan() => EncounterState.Enemies.OfType<HumanEnemy>().FirstOrDefault(e => e.Name == "Bogdan" && e.IsAlive());

        static void TalkBogdan(HumanEnemy b, Player p)
        {
            Story.Talk("Bogdan", new[] { "Yefim sent you? Ha. The old man still counts his dead. Tell him I count mine too." }, new[] { "(Fight)", "He wants you to know he remembers." }, i =>
            {
                if (i == 0) { Story.SetStep("bogdan", 2); return; }
                Story.Talk("Bogdan", new[] { "...Tell him I remember too. Tell him it was a bad year, and I was a bad man, and I'm still a bad man, but I'm tired.", "(He tosses over a dog tag.) His brother's. I kept it. Don't know why." });
                Give(Tag, "Dog Tag");
                int until = (int)((MacroState.Current.Day * 24f + MacroState.Current.Hour) * 60f) + 3 * 24 * 60;
                var sq = Shakedown.SquadOf(b);
                if (sq != null) { sq.TruceUntil = until; foreach (var m in sq.Members) m.IsAggroed = false; }
                Story.SetStep("bogdan", 4);
                Story.SetObjective("bogdan", "Take the dog tag back to Yefim", Tier1Story.Yefim);
            });
            if (Story.Step("bogdan") == 1) Story.SetStep("bogdan", 2);
        }

        static void Give(string id, string name) => Tier1Story.Give(id, name);

        /// <summary>Tier1Story.TryInteract: E by Yefim's fire with Bogdan's news. True if it took the key.</summary>
        public static bool TalkYefim(Player p)
        {
            int s = Story.Step("bogdan");
            if (s == 4 && Tier1Story.Holder(Tag) is Player h)
            {
                h.RemoveItem(Tag, 1);
                Story.Talk("Yefim", new[] { "...A dog tag. His. My brother's.", "Thank you. For bringing this back instead of him." });
                Tier1Story.Pay(900, "Bogdan, and the tag");
                foreach (var pl in Player.All) pl.AdjustReputation("Wanderers", Player.RepPoint);
                CompleteArc("bonfire_village", 3); Story.Journal("Yefim's brother's dog tag is home. Bogdan lives, and is tired."); Story.Finish("bogdan"); return true;
            }
            if (s == 3)
            {
                Story.Talk("Yefim", new[] { "He's dead, then. I thought I'd feel something.", "...Never mind. Here." });
                Tier1Story.Pay(700, "Bogdan");
                CompleteArc("bonfire_village", 3); Story.Journal("Bogdan is dead. Yefim feels nothing, or says so."); Story.Finish("bogdan"); return true;
            }
            return false;
        }

        // ---------------- the Runner's patron ----------------

        static void TalkRunner()
        {
            Story.Talk("The Runner", new[] { "You know who I really work for? The Scrap King. Every crate I move, he takes half. Every road I find, he owns.",
                "I want out. I want my own routes, my own name. But the King doesn't let people out." }, new[] { "I'll help you get free.", "I work for the King." }, i =>
                {
                    if (i == 0)
                    {
                        Story.Talk("The Runner", new[] { "Then watch your back. The King's men will come. Thank you." });
                        foreach (var pl in Player.All) { pl.AdjustReputation("Raiders", -3 * Player.RepPoint); pl.AdjustReputation("Wanderers", Player.RepPoint); }
                        SquadManager.SendQuestSquad(new[] { "Raiders" }, "smugglers_run", "story:kingmen");
                        Story.Set("runner_free"); Tier1Story.Pay(600, "the Runner's freedom");
                        Story.Journal("The Runner is free of the Scrap King. The King's men will be looking for you.");
                    }
                    else
                    {
                        Story.Talk("The Runner", new[] { "...I thought so. Fine. My routes are his. The tunnel stays open for you, at least." });
                        foreach (var pl in Player.All) pl.AdjustReputation("Raiders", Player.RepPoint);
                        Story.Set("runner_king"); Tier1Story.Pay(500, "the King's routes");
                        Story.Journal("The Runner's routes now belong to the Scrap King. The tunnel stays open for you.");
                    }
                    CompleteArc("smugglers_run", 3); Story.Finish("runner");
                });
        }

        // ---------------- the Liberators at the Bridge ----------------

        static HumanEnemy Sergeant() => EncounterState.Enemies.OfType<HumanEnemy>().FirstOrDefault(e => e.Name == "Liberator Sergeant" && e.IsAlive());

        static bool HighTech(InventoryItem i) => i.Type == "detector" || i.Type == "attachment" || (i.Name ?? "").Contains("Radio") || (i.Name ?? "").Contains("PDA");

        static void TalkBridge(HumanEnemy sg, Player p)
        {
            bool convoy = p.CompletedQuestIds.Contains("T2-03");
            var answers = new List<string> { "We're just passing.", "We'll take another crossing.", p.Ru >= 300 ? "(Pay 300 Ru)" : "(Pay - you can't afford it)" };
            if (convoy) answers.Add("We kept your convoy alive.");
            Story.Talk("Liberator Sergeant", new[] { "Halt. Bags open, all of you. We're free people here - free to search you, too.", "Anything with a blinking light, we keep. Anything that looks like it came from a lab, we really keep." }, answers.ToArray(), i =>
            {
                if (i == 0)
                {
                    var take = p.Inventory.Items.Where(HighTech).ToList();
                    foreach (var it in take) { p.Inventory.Items.Remove(it); sg.Inventory.Items.Add(it); }
                    GameLog.Event(take.Count == 0 ? "They go through your bags and find nothing they want." : $"They keep {take.Count} thing{(take.Count == 1 ? "" : "s")} with a blinking light. The Sergeant has them now.");
                    Story.Set("bridge_passed"); Story.SetStep("bridge", 2);
                }
                else if (i == 1) { Story.Talk("Liberator Sergeant", new[] { "The Ford's slow and the Rail Bridge is a scramble. Your funeral. Go." }); Story.SetStep("bridge", 2); }
                else if (i == 2 && p.Ru >= 300) { p.Ru -= 300; Story.Talk("Liberator Sergeant", new[] { "Freedom has a price. Today it's this one." }); Story.SetStep("bridge", 2); Story.Set("bridge_passed"); }
                else if (i == 3) { Story.Talk("Liberator Sergeant", new[] { "...You did. Go on through. Don't make me regret it." }); Story.SetStep("bridge", 2); Story.Set("bridge_passed"); }
            });
        }

        // ---------------- every game minute ----------------

        public static void Tick()
        {
            if (Net.Mode == Net.Role.Client || Player.Current == null) return;
            Tier2Escort.Tick();
            if (Story.IsDone("taken") && Story.Step("taken_day") == 0) Story.SetStep("taken_day", MacroState.Current.Day);
            // clear squads
            if (Story.Step("crusher") == 1 && Tier1Story.Cleared("crusher")) { Story.SetStep("crusher", 2); Story.SetObjective("crusher", "Search Crusher Row for who is paying them", "Crusher Row"); }
            if (Story.Step("rooke") >= 1 && Story.Step("rooke") <= 2 && Tier1Story.Cleared("rooke")) { Story.SetStep("rooke", 3); Story.SetObjective("rooke", "Tell the Scrap King Rooke is dead", King); }
            if (Story.Step("kingcall") == 1 && Tier1Story.Cleared("dock")) { Story.SetStep("kingcall", 2); Story.SetObjective("kingcall", "Tell the Scrap King the Dock is clear", King); }
            if (Story.Step("bogdan") >= 1 && Story.Step("bogdan") <= 2 && Tier1Story.Cleared("bogdan")) { Story.SetStep("bogdan", 3); Story.SetObjective("bogdan", "Tell Yefim about Bogdan", Tier1Story.Yefim); }
            // the Liberators remember a fight at the Bridge
            if (Story.Step("bridge") >= 1 && !Story.Has("bridge_fought") && MacroState.Current.OfflineSquads.Where(q => q.QuestId == "story:bridge").SelectMany(q => q.Members).Any(m => !m.IsAlive()))
            { Story.Set("bridge_fought"); GameLog.Event("The Liberators will remember this."); foreach (var pl in Player.All) pl.AdjustReputation("Liberators", -3 * Player.RepPoint); }
            // the Runner's patron: once the Runner's ambush job is done and the King's too, the Runner wants a word at the Cutting
            if (Story.Step("runner") == 0 && Story.IsDone("kingcall") && Player.All.Any(pl => pl.NpcArcStage.TryGetValue("smugglers_run", out var rs) && rs >= 3))
            { Story.SetStep("runner", 1); Story.SetObjective("runner", "The Runner wants a word. Find him at the Cutting", Runner); Story.Journal("The Runner's Patron: the Runner wants to talk about who he really works for."); }
            if (!OnTier2 || EncounterState.InCombat || ChoiceUI.IsOpen) return;
            if (Story.Step("bridge") == 0 && Story.IsDone("taken"))
            {
                Story.SetStep("bridge", 1);
                var libs = new List<Mutant>();
                for (int i = 0; i < 4; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Liberators", "Experienced", 2, i == 0); libs.Add(new HumanEnemy(i == 0 ? "Liberator Sergeant" : "Liberator", 0, 0, "Liberators", "Experienced", w, i == 0 ? StoryNpcs.Fixed("Liberator Guard Suit") ?? a : a)); }
                Tier1Story.Place("bridge", Bridge, "Liberators", libs);
            }
            int tdone = Story.Step("taken_day"); if (tdone == 0) return;
            if (Story.Step("kingcall") == 0 && MacroState.Current.Day >= tdone + 1) StartKing();
            else if (Story.Step("bogdan") == 0 && Story.IsDone("kingcall") && Player.All.Any(p => p.NpcArcStage.TryGetValue("bonfire_village", out var st) && st >= 3)) StartBogdan();
        }

        static void StartKing()
        {
            Story.SetStep("kingcall", 1);
            Story.Talk("The Scrap King (radio)", new[] { "You're the one who came up out of the Dock with all your fingers. Word travels.",
                "The soldiers are gone, and some fools from a rival crew have moved into my Dock. My Dock. Clear them out and you'll find the Scrap King generous. Fail and you'll find him... less generous." },
                new[] { "What's the Dock to you?", "Deal." }, i =>
                {
                    if (i == 0) Story.Talk("The Scrap King (radio)", new[] { "Everything that comes in or out of the Basin comes past it. Whoever sits on the Dock eats. I like to eat." });
                });
            Story.SetObjective("kingcall", "Clear the rival crew from the Loading Dock", Dock);
            Story.Journal("The Scrap King wants the rival crew gone from the Loading Dock.");
            var list = new List<Mutant>();
            for (int i = 0; i < 5; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", "Experienced", 2, i == 0); list.Add(new HumanEnemy(i == 0 ? "Rival Boss" : "Rival Raider", 0, 0, "Raiders", "Experienced", w, a)); }
            Tier1Story.Place("dock", Dock, "Raiders", list);
        }

        static void StartBogdan()
        {
            Story.SetStep("bogdan", 1);
            Story.Talk("Yefim (radio)", new[] { "The keepsake you brought back. There was a name on the back I didn't want to see. Bogdan.",
                "We rode together. Years ago. He sold three of us to the Garrison for a crate of rifles. One of them was my brother.",
                "He holds the Tire Fort now. I'm too old to go. You're not. I'm not asking you to kill him. I'm asking you to look him in the eye and decide." });
            Story.SetObjective("bogdan", "Find Bogdan at the Tire Fort", Fort);
            Story.Journal("Yefim's Last Request: Bogdan holds the Tire Fort. Look him in the eye and decide.");
            var list = new List<Mutant>();
            for (int i = 0; i < 5; i++)
            {
                var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", i == 0 ? "Expert" : "Experienced", 2, i == 0);
                list.Add(new HumanEnemy(i == 0 ? "Bogdan" : "Bogdan's Raider", 0, 0, "Raiders", i == 0 ? "Expert" : "Experienced", w, i == 0 ? StoryNpcs.SuitOf("Bogdan", 0.45f) : a));   // Bogdan has his own look (make_npc_looks.py)
            }
            Tier1Story.Place("bogdan", Fort, "Raiders", list);
        }
    }
}
