// Story 6c (2026-11-18): the end of Tier 2 - "The trail north" and "The Escort" (STORY.md Tier 2 phases 4 and 7, STORY_DIALOGUE.md "The Escort").
// 1. The trail: Pavel (radio) has read more of the canister: "those" came down through Black Valley; only Dr. Osei at the Dried Lake can be trusted with the data. A
//    broadcast comes from the Radio Repeater on the north rim: Husks with Tier 2 gear (five) hold it; clear them, E at the repeater (robed figures are seen again, three of them
//    on the valley road). Steps "trail": 1 go, 2 cleared, done.
// 2. The escort: once the trail is done and the Liberators' bridge has been dealt with (`bridge` step 2), Pavel at the Medic's House asks you to take him north (an auto-save is made,
//    he walks with you as a story partner). Sniper's Lip: two Contractor snipers shoot at the road (they were paid to kill Pavel). The road cut: Raiders at the "Road Cut Checkpoint" ask a toll:
//    friends of the Scrap King (you sided with him, or are Friendly with the Raiders) pass; otherwise 400 Ru, or a fight ("and the skinny one stays"). The Scavenger's Hub gate: Pavel stays
//    there (Tier 3: he goes on to Osei). Then Yefim's call: the Blight Brute is back in the Rust Culvert (10,000 HP): "brute2" - kill it for 1500 Ru.
//    Steps "escort": 1 ask Pavel, 2 walking (the auto-save), done at the Hub. If Pavel dies the auto-save is loaded (STORY.md: "Pavel can die -> retry from a save"), or step 1 again with no save.
// ponytail: the road cut's toll talk and the snipers are placed squads (like every story squad); the snipers fire when they see you (no scripted "glint" cutscene yet).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Tier2Escort
    {
        public const string Repeater = "Radio Repeater", Lip = "Sniper's Lip", Cut = "Road Cut Checkpoint";
        static bool Pav => Tier1Story.PavelWithYou;
        static bool Near(string landmark, Player p, int r) => Story.Near(p, landmark, r);
        static float DistTo(Player p, string landmark) { var l = (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).FirstOrDefault(x => x.Name == landmark); return l == null ? 9999f : Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)); }

        public static bool TryInteract(Player p)
        {
            if (Tier2Jobs.OnTier2 && Story.Step("trail") == 2 && Near(Repeater, p, 3)) { Inspect(); return true; }
            if (TerrainMap.ZoneId == "perimeter" && Story.Step("escort") == 1 && Near(Tier1Story.Medic, p, 3)) { AskPavel(p); return true; }
            if (Tier2Jobs.OnTier2 && Story.Step("escort") == 2)
            {
                var r = EncounterState.Enemies.OfType<HumanEnemy>().Where(e => e.Name == "Checkpoint Raider" && e.IsAlive()).OrderBy(e => CombatantUtil.Distance(p, e)).FirstOrDefault();
                if (r != null && CombatantUtil.Distance(p, r) <= 3) { Toll(r, p); return true; }
            }
            return false;
        }

        // ---------------- the trail ----------------

        static void Inspect()
        {
            Story.Talk("Radio Repeater", new[] { "A dish bolted to the rock, a generator, and a loop of tone on a frequency that makes your fillings ache.",
                "Pavel (radio): That's it. That's the same signal as the canister. It isn't aimed at anyone yet. It's... listening for an answer." });
            GreyRobes.Place(Mathf.Clamp(Player.Current.X - 14, 3, TerrainMap.Width - 4), Mathf.Clamp(Player.Current.Y + 10, 3, TerrainMap.Height - 4), 3, false);
            GameLog.Event("Robed figures on the valley road, more of them this time. They don't come closer.");
            Story.Journal("The Radio Repeater on the north rim broadcasts the same signal as Pavel's canister. Robed figures are on the valley road.");
            Tier1Story.Pay(600, "the Radio Repeater");
            Story.Finish("trail");
        }

        static void StartTrail()
        {
            Story.SetStep("trail", 1);
            Story.Talk("Pavel (radio)", new[] { "I've read more of the drive. They came down through Black Valley - the ones in grey. They are broadcasting from somewhere on the north rim.",
                "Only Dr. Osei, at the Dried Lake, past the Scavenger's Hub, can be trusted with this. Find out what the broadcast is, and find me a road I can survive." });
            Story.SetObjective("trail", "Find the broadcast on the north rim: the Radio Repeater", Repeater);
            Story.Journal("The Trail North: a broadcast comes from the Radio Repeater on Black Valley's north rim.");
            var pack = Husk.Pack(2, 5);
            Tier1Story.Place("repeater", Repeater, Husk.Faction, pack);
        }

        // ---------------- the escort ----------------

        static void AskPavel(Player p)
        {
            Story.Talk("Pavel", new[] { "You found a road. I can see it in your face. You don't look sure, but you found one.", "I'll be slow. Don't wait for me if - no. Wait for me. Please wait for me." });
            SaveSystem.Save();                                              // the escort can be failed: this is what is loaded
            Companion.JoinStory(p, "Pavel", 100);
            Story.SetStep("escort", 2);
            Story.SetObjective("escort", "Take Pavel north to the Scavenger's Hub, alive", Tier2Jobs.Bridge);
            Story.Journal("The Escort: Pavel walks with you now, north to the Scavenger's Hub.");
        }

        static void Toll(HumanEnemy r, Player p)
        {
            bool friend = Story.Has("runner_king") || p.GetRepLevel("Raiders") == "Friendly";
            var sq = Shakedown.SquadOf(r);
            void Stand() { int until = (int)((MacroState.Current.Day * 24f + MacroState.Current.Hour) * 60f) + 2 * 24 * 60; if (sq != null) { sq.TruceUntil = until; foreach (var m in sq.Members) m.IsAggroed = false; } Story.Set("cut_passed"); }
            if (friend) { Story.Talk("Checkpoint Raider", new[] { "Toll. ...Wait, you're the King's friend. Go on." }); Stand(); return; }
            Story.Talk("Checkpoint Raider", new[] { "Toll. And the skinny one stays." }, new[] { p.Ru >= 400 ? "(Pay 400 Ru)" : "(Pay - you can't afford it)", "(Fight)" }, i =>
            {
                if (i == 0 && p.Ru >= 400) { p.Ru -= 400; Story.Talk("Checkpoint Raider", new[] { "Heh. Smart skinny one. Go on." }); Stand(); }
            });
        }

        static void Arrive()
        {
            Story.Talk("Sentinel Guard", new[] { "Weapons lowered inside the walls. Trade, drink, sleep. Start a fight and you leave in a bag.", "(Pavel:) Walls. Real walls. I could kiss them." });
            Companion.Dismiss();
            Story.Talk("Yefim (radio)", new[] { "Made it to the Hub? Good. Listen - that thing in the Culvert. It's still there. Scaring my hunters off the road.", "When you're stronger, come home and finish it." });
            Tier1Story.Pay(1200, "Pavel, safe at the Hub");
            Story.Journal("Pavel is safe at the Scavenger's Hub. Tier 3 lies ahead. Yefim wants the Blight Brute dead.");
            Story.Finish("escort"); Story.Set("tier2_done");
            Story.SetStep("brute2", 1);
            Story.SetObjective("brute2", "Kill the Blight Brute in the Rust Culvert (Yefim's request, any time)", "Rust Culvert");
        }

        // ---------------- every game minute ----------------

        public static void Tick()
        {
            var p = Player.Current; if (p == null) return;
            // the trail begins once you have a foothold
            if (Story.Step("trail") == 0 && Story.IsDone("kingcall") && Tier2Jobs.OnTier2 && !EncounterState.InCombat && !ChoiceUI.IsOpen) StartTrail();
            if (Story.Step("trail") == 1 && Tier1Story.Cleared("repeater")) { Story.SetStep("trail", 2); Story.SetObjective("trail", "Examine the Radio Repeater", Repeater); }
            if (Story.Step("escort") == 0 && Story.IsDone("trail") && Story.Step("bridge") >= 2) { Story.SetStep("escort", 1); Story.SetObjective("escort", "Pavel is waiting at the Medic's House: take him north", Tier1Story.Medic); }
            if (Story.Step("escort") != 2) return;
            if (!Pav)   // Pavel is down
            {
                Tier1Story.PavelLost("The road north was too much for him.", () => { Story.SetStep("escort", 1); Story.SetObjective("escort", "Pavel is waiting at the Medic's House: take him north", Tier1Story.Medic); });
                return;
            }
            if (Tier2Jobs.OnTier2)
            {
                if (!Story.Has("snipers_placed") && DistTo(p, Lip) < 45f)
                {
                    Story.Set("snipers_placed");
                    var gun = WeaponDatabase.Categories["sniper"].Values.FirstOrDefault();
                    var list = new List<Mutant>();
                    for (int i = 0; i < 2; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Contractors", "Veteran", 2, i == 0); list.Add(new HumanEnemy("Contractor Sniper", 0, 0, "Contractors", "Veteran", gun ?? w, a) { IsAggroed = true }); }
                    Tier1Story.Place("snipers", Lip, "Contractors", list);
                    foreach (var m in list) m.IsAggroed = true;
                    GameLog.Event("A glint on the rim. Then another.");
                    Story.Radio("Pavel", "Snipers! Who would pay snipers to kill me? ...Oh. Them. Of course them.");
                }
                if (!Story.Has("cut_placed") && DistTo(p, Cut) < 35f)
                {
                    Story.Set("cut_placed");
                    var list = new List<Mutant>();
                    for (int i = 0; i < 4; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", "Experienced", 2, i == 0); list.Add(new HumanEnemy("Checkpoint Raider", 0, 0, "Raiders", "Experienced", w, a)); }
                    Tier1Story.Place("cutpost", Cut, "Raiders", list);
                }
            }
            var hub = MacroMapDatabase.MapOf("scavengers_hub");
            if ((Tier1Story.ZoneOverride ?? TerrainMap.ZoneId) == hub && Pav) Arrive();
        }
    }
}
