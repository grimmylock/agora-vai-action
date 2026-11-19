// Story 7a (2026-11-18): Tier 3 - The Safe Haven (STORY_DIALOGUE.md Tier 3; STORY_OUTLINE_T4_T7.md "Tier 3 additions"). The Scavenger's Hub, the Dried Lake Sub-Station.
// - Lev Marchuk (The Rusty Anchor): E there. First a stranger's talk (+ journal). After the Sub-Station reveal he asks for his pills (a Medical Crate on the Stranded Ferry, in its
//   Passenger Lounge - the dungeon puts it down the first time you enter, once he has asked); hand-in: the Confession (he was one of "those"; flag `lev_confessed`).
// - Pavel waits at the Hub Bunkhouse after Tier 2. E there: he walks with you across the lakebed to the Sub-Station (a second escort: auto-save; if he dies it loads). Near the House Osei
//   radios: the turrets are locked down after a Prowler attack. Two packs come out of the Flats and the Tower Line; E at the Breaker Room (east wall) cuts the turrets and the door opens;
//   E at the House: the Reveal (production drawings; a counter-signal needs the old array's power and one of their cores), Hale and Rask meet you, Pavel stays with Osei. +1000 Ru.
// - After: E at the House for Hale (search his drawer: his radio is only a supply racket -> flag `hale_cleared`), Rask (coffee) and Osei's thread (the spikes below: the energy
//   pulses come at regular intervals, "like a heartbeat" - she calls it interference; flag `spikes_known`).
// Steps: "lake" 1 walking, 2 lockdown, 3 door open, done after the Reveal. "pills" 1 find the crate, 2 hand-in. "lev" 1 met.
// ponytail: Osei, Rask, Hale and Lev are talk points (no units). The breaker room is a new landmark at the Sub-Station apron's east edge. Not run in Unity.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Tier3Story
    {
        public const string Anchor = "The Rusty Anchor", Bunk = "Hub Bunkhouse", House = "Sub-Station House", Breaker = "Breaker Room", Ferry = "stranded_ferry", Lounge = "Passenger Lounge", Crate = "story_pills";
        public static string ZoneOverride;
        static string Map3 => MacroMapDatabase.MapOf("scavengers_hub");
        static bool OnTier3 => (ZoneOverride ?? TerrainMap.ZoneId) == Map3;
        static bool Pav => Tier1Story.PavelWithYou;

        public static bool TryInteract(Player p)
        {
            if (OnTier3 && Story.Near(p, Anchor, 3) && TalkLev()) return true;
            if (OnTier3 && Story.Near(p, Bunk, 3) && TalkPavelHub(p)) return true;
            if (OnTier3 && Story.Step("lake") == 2 && Story.Near(p, Breaker, 3)) { CutTurrets(); return true; }
            if (OnTier3 && Story.Near(p, House, 4) && TalkHouse()) return true;
            if (OnTier3 && Story.Near(p, "Roundhouse", 4) && TalkVoss()) return true;
            if (OnTier3 && Story.Step("leak") == 2 && Story.Near(p, "Signal Tower", 4)) { TalkQuill(); return true; }
            return false;
        }

        // ---------------- Lev ----------------

        static bool TalkLev()
        {
            if (Story.IsDone("pills")) return false;
            if (Story.Step("pills") == 2 && Tier1Story.Holder(Crate) is Player h)
            {
                h.RemoveItem(Crate, 1);
                Story.Talk("Lev Marchuk", new[] { "...Thank you. The noise is... quieter. Sit. I'll tell you something I haven't told anyone.",
                    "I was one of them.",
                    "Not born to it. Recruited. They find people who are good and angry and lonely, and they give them a reason. Order. A Zone that stops killing everyone. It sounds beautiful when you're tired.",
                    "Then I saw what \"order\" meant. The first field test. A village. They switched it off like a lamp.",
                    "I walked out. Took what I knew and made a name with it. Every legend you heard about me - I used their maps, their routes, their tricks. And I never once went back to stop them. Because I knew it wasn't enough. One man isn't enough." },
                    new[] { "It's not one man now.", "Why tell us?" }, i =>
                    Story.Talk("Lev Marchuk", new[] { i == 0 ? "...No. It isn't, is it." : "Because your engineer built their machine, and you saved him anyway. Either you're fools or you're the first good news I've had in nine years." }));
                Story.Set("lev_confessed"); Story.Journal("The Confession: Lev Marchuk was one of \"those\". He walked out after the first field test.");
                Tier1Story.Pay(800, "Lev's pills"); Story.Finish("pills"); return true;
            }
            if (Story.Step("pills") == 1) { Story.Talk("Lev Marchuk", new[] { "The pills. The crate on the ferry in the lake, if the things living in it haven't eaten the boxes." }); return true; }
            if (Story.IsDone("lake") && Story.Step("pills") == 0)
            {
                Story.Talk("Lev Marchuk", new[] { "You actually got him to the lake. Hm. Maybe you're luckier than I was.",
                    "My hands. Look at them. Shaking like a rookie's. There's a pill for it - for the noise in the head. I ran out a week ago.",
                    "The Bazaar won't have it. Nobody sells it. Osei had some once. Or there was a medical crate on that ferry in the lake - if the things living in it haven't eaten the boxes." },
                    new[] { "I'll find it." }, _ => Story.Talk("Lev Marchuk", new[] { "Don't do it for me. Do it so I'm useful. Useful is better than sorry." }));
                Story.SetStep("pills", 1); Story.SetObjective("pills", "Find Lev's medical crate on the Stranded Ferry", "Stranded Ferry");
                Story.Journal("The Pills: Lev needs medicine from a crate on the Stranded Ferry in the lake.");
                return true;
            }
            if (Story.Step("lev") == 0)
            {
                Story.Talk("Barman", new[] { "That's Marchuk. Yes, that Marchuk. Don't ask him for stories. He doesn't tell them any more." });
                Story.Talk("Lev Marchuk", new[] { "You're staring. Everyone stares. Sit or leave." }, new[] { "They say you've been everywhere.", "We brought someone from the south. An engineer. Running from people in grey robes.", "(Leave)" }, i =>
                {
                    if (i == 0) Story.Talk("Lev Marchuk", new[] { "They say a lot. I've been to enough places to know which ones you don't come back from. Same thing, mostly." });
                    else if (i == 1) Story.Talk("Lev Marchuk", new[] { "...Say that again.", "Grey. No marks. No faction. They don't talk on the road. They stand and look at the ground like it owes them money.", "Your engineer is a dead man. So are you, if you stand next to him long enough." });
                });
                Story.SetStep("lev", 1);
                Story.Journal("Lev Marchuk: a legend of the Zone, drinking himself quiet in the Rusty Anchor. He knows \"those\".");
                return true;
            }
            return false;
        }

        // ---------------- Pavel at the Hub, the walk to the lake ----------------

        static bool TalkPavelHub(Player p)
        {
            if (Story.Step("lake") != 0 || !Story.Has("tier2_done")) return false;
            Story.Talk("Pavel", new[] { "A lake with no water. Osei is on the far side of it, in the Sub-Station. If anyone can read this drive, she can.", "I'll try to keep up." });
            SaveSystem.Save();
            Companion.JoinStory(p, "Pavel", 100);
            Story.SetStep("lake", 1);
            Story.SetObjective("lake", "Take Pavel across the lakebed to the Sub-Station", House);
            Story.Journal("To the Sub-Station: Dr. Priya Osei, across the dry lakebed, is the one who can read Pavel's drive.");
            return true;
        }

        static void CutTurrets()
        {
            GameLog.Event("The turrets spin down. The door grinds open. Everyone inside. Now.");
            Story.SetStep("lake", 3);
            Story.SetObjective("lake", "Get inside the Sub-Station", House);
        }

        static bool TalkHouse()
        {
            int s = Story.Step("lake");
            if (s == 3 && Pav)
            {
                Story.Talk("Dr. Priya Osei", new[] { "(in person) Pavel. From the institute. I read your papers. I hated your papers.", "Pavel: Everyone did. They were right to.", "Osei: Show me what you brought." });
                Story.Talk("Dr. Priya Osei", new[] { "These aren't prototypes. These are production drawings. Tooling, part numbers, batch sizes.", "Pavel: They're not testing any more. They're manufacturing.",
                    "A counter-signal is possible. Cancel their frequency - a wide band, a big antenna. But the processing... nothing out here can do it. Except - (she taps the map) - the old array in the north. If it still has power.",
                    "Emil Rask: That's suicide, Priya. The north is crawling with Obelisk.", "Then we'll need someone suicidal. (looks at you) No offence." }, new[] { "None taken.", "Some taken." }, _ =>
                    Story.Talk("Dr. Priya Osei", new[] { "There's one more thing the counter-signal needs - a core, tuned to their frequency. We don't have one. They do." }));
                Companion.Dismiss();
                Story.Journal("The Reveal: \"those\" are making the device in numbers. A counter-signal needs the old array's power - and one of their own tuned cores.");
                Story.Set("osei_met"); Tier1Story.Pay(1000, "Pavel, delivered to Dr. Osei");
                Story.Finish("lake"); Story.Set("tier3_reveal");
                return true;
            }
            if (Story.IsDone("lake") && Tier3Annex.TalkOsei()) return true;                // 2026-11-19: Osei's ask about the lab door (Tier3Annex.cs)
            if (Story.IsDone("lake"))
            {
                bool pillsAsk = Story.Step("pills") == 1 && Tier1Story.Holder(Crate) == null;
                var ans = new List<string> { "Ask Osei about the lake", "Konrad Hale", "Emil Rask", "(Leave)" };
                if (pillsAsk) ans.Add("Ask Osei about Lev's pills");
                Story.Talk("Sub-Station", new[] { "Osei, Rask and Hale are at their stations." }, ans.ToArray(), i =>
                {
                    if (i == 4)
                    {
                        Story.Talk("Dr. Priya Osei", new[] { "Lev's tremor. I had a box, once. One left. You brought me an engineer I can finally use; take it, and tell him it's from me.", "(She gives you a small box.)" });
                        Player.Current.AddItem(new InventoryItem { Id = Crate, Name = "Medical Crate", Type = "key", Qty = 1, WeightKg = 1f, Price = 0 });
                    }
                    else if (i == 0)
                    {
                        Story.Talk("Dr. Priya Osei", new[] { "The researcher who died in the Depths - his station was measuring spikes from deeper underground. Bedrock doesn't spike.", "Something under the lake is drawing power. A lot of it. I want to know what.",
                            "They come at regular intervals. Almost like a pulse. I've been calling it interference." });
                        Story.Set("spikes_known"); Story.Journal("Osei's spikes: something deep under the lake draws power, on a regular pulse. She calls it interference.");
                    }
                    else if (i == 1) Story.Talk("Konrad Hale", new[] { "Supplies. People always think it's something else. It's always supplies.", "(He lowers a small radio into a drawer when he sees you. He doesn't lock it. It holds crate manifests and a price list: a supply racket, nothing worse.)" }, null, _ => { Story.Set("hale_cleared"); Story.Journal("Konrad Hale's hidden radio is only a supply racket."); });
                    else if (i == 2) { Story.Talk("Emil Rask", new[] { "Konrad? He's a good man. Bit nervous. Who wouldn't be, out here?", "Can I get you anything? Coffee? It's terrible, but it's hot." }); Story.Set("rask_talked"); }
                });
                return true;
            }
            return false;
        }

        // ---------------- the leak (Voss and the Contractors) ----------------
        // Someone on Voss's payroll sold a patrol route: the Contractor snipers on Sniper's Lip (Tier 2) were paid by "those". Quill, a deserter, is at the Signal Tower with a robed figure.

        static bool TalkVoss()
        {
            int s = Story.Step("leak");
            if (s == 3 && Tier1Story.Holder("story_ledger") is Player h)
            {
                h.RemoveItem("story_ledger", 1);
                bool spared = Story.Has("quill_spared");
                Story.Talk("Voss", new[] { "A ledger. Payments, dates, a drop at the Signal Tower - and a name for the payer I can't pronounce, because it isn't a name.",
                    spared ? "You let him walk? ...Fine. A man who talks gets to leave. That's the one rule I respect." : "You brought him in alive. He'll sing for me too. Good.",
                    "Whoever paid my snipers wanted a skinny engineer dead. That's not a contract. That's a hunt. The Contractors don't work for people like that. Not any more." });
                Tier1Story.Pay(1000, "the leak"); foreach (var pl in Player.All) pl.AdjustReputation("Contractors", (spared ? 1 : 2) * Player.RepPoint);
                Story.Journal("The Leak: \"those\" paid the Contractor snipers who hunted Pavel. Voss has the ledger.");
                Story.Finish("leak"); Story.Set("those_paid_snipers"); return true;
            }
            if (s == 0 && Story.Has("tier3_reveal"))
            {
                Story.Talk("Voss", new[] { "Contractors don't ask who pays. Mostly. Lately someone's paying my people to look the other way - and to shoot where they're told.",
                    "A sniper team on the north rim was paid by somebody who isn't on my books. One of mine sold the patrol schedule. I want to know who bought it." }, new[] { "I'll find out.", "Not my fight." }, i =>
                {
                    if (i == 1) return;
                    Story.SetStep("leak", 1); Story.SetObjective("leak", "Find who paid the snipers: the Signal Tower", "Signal Tower");
                    Story.Journal("The Leak: Voss wants to know who bought his patrol schedule. A trail leads to the Signal Tower.");
                });
                return true;
            }
            if (s == 1 || s == 2) { Story.Talk("Voss", new[] { "The Signal Tower. Quiet, high, nobody looks at it. Go and look at it." }); return true; }
            return false;
        }

        static void TalkQuill()
        {
            var q = StoryNpcs.Find("Quill");
            Story.Talk("Quill", new[] { "Don't. I'm - I'm just a man with a radio. They told me they'd leave me alone.", "A grey one comes, always at dusk. Pays in clean notes. Tells me where your people will be. Never says why.",
                "They said the skinny engineer was a thief. That the Zone would be quieter without him. I didn't ask more. I didn't want to know." }, new[] { "Give me the ledger. You can go.", "Give me the ledger. You're coming with me." }, i =>
                {
                    if (i == 0) Story.Set("quill_spared");
                    Tier1Story.Give("story_ledger", "Payment Ledger");
                    Story.SetStep("leak", 3); Story.SetObjective("leak", "Take the ledger to Voss at the Roundhouse", "Roundhouse");
                    if (q != null && i == 0) q.FaceToward(q.X + 5, q.Y);
                });
        }

        // ---------------- the lake ----------------

        static void Waves()
        {
            string[] spots = { "The Flats", "Tower Line" };
            for (int w = 0; w < 2; w++)
            {
                var list = new List<Mutant>();
                for (int i = 0; i < 5; i++)
                {
                    var def = EncounterGeneration.PickRandomMutantByTier(3, 3) ?? EncounterGeneration.PickRandomMutantByTier(1, 5);
                    var m = EncounterGeneration.CreateMutantFromDb(def, 0, 0); m.IsAggroed = true; list.Add(m);
                }
                Tier1Story.Place("lockdown" + w, spots[w], "Mutant", list);
                foreach (var m in list) m.IsAggroed = true;
            }
        }

        public static void OnEnterZone(string zoneId)
        {
            if (zoneId != Ferry || Story.Step("pills") != 1 || Story.Has("pills_placed")) return;
            var l = TerrainMap.Landmarks.FirstOrDefault(x => x.Name == Lounge); if (l == null) return;
            var (x, y) = TerrainMap.FindNearestEnterable(l.X + 1, l.Y + 1);
            TerrainMap.DropItem(x, y, new InventoryItem { Id = Crate, Name = "Medical Crate", Type = "key", Qty = 1, WeightKg = 2f, Price = 0 });
            Story.Set("pills_placed");
        }

        public static void Tick()
        {
            var p = Player.Current; if (p == null || Net.Mode == Net.Role.Client) return;
            if (Story.Step("leak") == 1 && OnTier3)
            {
                var st = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == "Signal Tower");
                if (st != null && System.Math.Max(System.Math.Abs(p.X - st.X), System.Math.Abs(p.Y - st.Y)) < 25)
                {
                    Story.SetStep("leak", 2); GreyRobes.Place(st.X + 3, st.Y + 3, 1, false);
                    GameLog.Event("A robed figure stands at the foot of the Signal Tower, talking to a man in Contractor colours. It turns away as you come.");
                    Story.SetObjective("leak", "Question the Contractor at the Signal Tower", "Signal Tower"); StoryNpcs.Tick();
                }
            }
            if (Story.Step("pills") == 1 && Tier1Story.Holder(Crate) != null) { Story.SetStep("pills", 2); Story.SetObjective("pills", "Take the medical crate to Lev", Anchor); }
            int s = Story.Step("lake");
            if (s < 1 || s >= 3 || Story.IsDone("lake")) return;
            if (!Pav)   // Pavel is down: the auto-save, or ask him again
            {
                Tier1Story.PavelLost("The lakebed was too much for him.", () => { Story.SetStep("lake", 0); Story.SetObjective("lake", null); });
                return;
            }
            if (!OnTier3) return;
            var h = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == House); if (h == null) return;
            int d = System.Math.Max(System.Math.Abs(p.X - h.X), System.Math.Abs(p.Y - h.Y));
            if (s == 1 && d < 60 && !Story.Has("osei_radio")) { Story.Set("osei_radio"); Story.Talk("Dr. Priya Osei (radio)", new[] { "This is the Sub-Station. If you're coming across the flats after dark, you're either brave or stupid, and I need people who are both. Who's with you?" }); }
            if (s == 1 && d < 22)
            {
                Story.SetStep("lake", 2);
                Story.Radio("Osei", "The turrets went into full lockdown when the Prowlers hit us. I can't override from in here. The breaker room is outside, east wall. Cut the turrets, and I can open the door - and then everything outside can come in too. So be quick.");
                Story.SetObjective("lake", "Cut the turrets at the Breaker Room (the east wall)", Breaker);
                Waves();
                Story.Radio("Pavel", "They're coming out of the reeds. Hurry!");
            }
        }
    }
}
