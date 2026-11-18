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
        public const string King = "Scrap King's Nest", Dock = "Loading Dock", Fort = "Tire Fort", Tag = "story_dogtag";
        static string Map2 => MacroMapDatabase.MapOf("scrapyard");
        public static string ZoneOverride;   // harness: pretend to be on the Tier 2 map
        static bool OnTier2 => (ZoneOverride ?? TerrainMap.ZoneId) == Map2;

        static void CompleteArc(string npcId, int stage) { foreach (var p in Player.All) if (!p.NpcArcStage.TryGetValue(npcId, out var s) || s <= stage) p.NpcArcStage[npcId] = stage + 1; }

        public static bool TryInteract(Player p)
        {
            if (OnTier2 && Story.Near(p, King, 3) && TalkKing()) return true;
            if (OnTier2 && (Story.Step("bogdan") == 1 || Story.Step("bogdan") == 2)) { var b = Bogdan(); if (b != null && CombatantUtil.Distance(p, b) <= 3) { TalkBogdan(b, p); return true; } }
            return false;
        }

        // ---------------- the Scrap King ----------------

        static bool TalkKing()
        {
            if (Story.Step("kingcall") != 2) return false;
            Story.Talk("The Scrap King", new[] { "The Dock is mine again. You're the first thing that came out of the Basin useful.", "Here. Don't spend it all in one place. It's the only place there is." });
            Tier1Story.Pay(900, "the Loading Dock");
            foreach (var pl in Player.All) pl.AdjustReputation("Raiders", Player.RepPoint);
            CompleteArc("scrapyard", 1);
            Story.Journal("The Scrap King: the Loading Dock is his again; he'll want more.");
            Story.Finish("kingcall");
            return true;
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

        // ---------------- every game minute ----------------

        public static void Tick()
        {
            if (Net.Mode == Net.Role.Client || Player.Current == null) return;
            if (Story.IsDone("taken") && Story.Step("taken_day") == 0) Story.SetStep("taken_day", MacroState.Current.Day);
            // clear squads
            if (Story.Step("kingcall") == 1 && Tier1Story.Cleared("dock")) { Story.SetStep("kingcall", 2); Story.SetObjective("kingcall", "Tell the Scrap King the Dock is clear", King); }
            if (Story.Step("bogdan") >= 1 && Story.Step("bogdan") <= 2 && Tier1Story.Cleared("bogdan")) { Story.SetStep("bogdan", 3); Story.SetObjective("bogdan", "Tell Yefim about Bogdan", Tier1Story.Yefim); }
            if (!OnTier2 || EncounterState.InCombat || ChoiceUI.IsOpen) return;
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
                list.Add(new HumanEnemy(i == 0 ? "Bogdan" : "Bogdan's Raider", 0, 0, "Raiders", i == 0 ? "Expert" : "Experienced", w, a));
            }
            Tier1Story.Place("bogdan", Fort, "Raiders", list);
        }
    }
}
