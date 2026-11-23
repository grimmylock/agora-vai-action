// Story 5 (2026-11-18): Tier 1, part 2a - Pavel. The Engineer in the Flooded Bunker, the way back to the village, the Rust Culvert
// (Secret Cave Area) with the canister and the Blight Brute, and the reveal at the Medic's House. From STORY_DIALOGUE.md.
// Steps: "engineer" 1 = find P. in the bunker, 2 = Pavel walks with you (a story partner: Companion.JoinStory), done at the village;
//        "culvert" 1 = talk to Pavel, 2 = into the cave for the canister, 3 = the chase (the Brute wakes), 4 = bring the canister back, done.
// - The people are talk points at landmarks (like Yefim), not units: Pavel is at the bunker's Command Room, then the Medic's House.
// - Pavel as a partner can die. ponytail: STORY.md says "retry from a save"; here the step goes back to 1 so he can be found again (no soft lock).
// - The Brute is a real mutant put down at the Mutant Den when you enter the cave, asleep (not aggroed) until the canister is taken; the cutscene,
//   then it hunts you; it can't follow through the cave mouth. Its chase feel (speed against yours) is NOT tuned: never run in Unity.
// - Not in this batch: the Grey Robes watching the way back, the Husks, the night defence (Story 5b).
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static partial class Tier1Story
    {
        public const string Command = "Command Room", Medic = "Medic's House", Den = "Mutant Den", CaveMouth = "Cave Mouth", BunkerZone = "flooded_bunker", CaveZone = "secret_cave";
        const string Canister = "story_canister";
        public static string ZoneOverride;   // harness: pretend to be on another map
        static string Zone => ZoneOverride ?? TerrainMap.ZoneId;
        static Mutant _brute; static bool _wake; static float _wakeAt; public static Mutant TheBrute => _brute;   // (harness: the culvert Brute, not any other Blight Brute in the zone)

        public const int BruteHp = 10000;

        /// <summary>The free floor tile farthest from the cave mouth within 6 tiles of the den: the back of the lair.</summary>
        static (int x, int y) HoardSpot(LandmarkInstance den)
        {
            var mouth = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == CaveMouth);
            (int x, int y) best = TerrainMap.FindNearestEnterable(den.X, den.Y - 2); int bd = -1;
            for (int dy = -6; dy <= 6; dy++) for (int dx = -6; dx <= 6; dx++)
            {
                int x = den.X + dx, y = den.Y + dy;
                if (!TerrainMap.CanEnterTile(x, y) || System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) < 2) continue;
                int d = mouth == null ? -dy : System.Math.Abs(x - mouth.X) + System.Math.Abs(y - mouth.Y);
                if (d > bd) { bd = d; best = (x, y); }
            }
            return best;
        }

        /// <summary>Pavel died on one of his three walks (the bunker, north, the lake): the same rule everywhere - the auto-save made when he joined is loaded (STORY.md: "retry from a
        /// save"); with no save to go back to the mission goes back to its start. `reset` runs then.</summary>
        internal static void PavelLost(string where, System.Action reset)
        {
            GameLog.Event($"[ALERT] Pavel is dead. {where}");
            Story.Set("pavel_lost");
            GreyRobes.Release();
            Story.Fade(UnityEngine.Color.black, 1f, 0.6f);
            if (!SaveSystem.Load()) { reset(); Story.Fade(UnityEngine.Color.black, 0f, 0.6f); }
        }

        internal static bool PavelWithYou => Companion.Alive && Companion.Current.Name == "Pavel";

        static bool TryInteractPavel(Player p)
        {
            if (Zone == BunkerZone && Story.Step("engineer") == 1 && Story.Near(p, Command, 3)) { TalkPavelBunker(p); return true; }
            if (Zone == CaveZone && Story.Step("culvert") == 2 && Story.Near(p, Den, 3) && Holder(Canister) == null) { TakeCanister(p); return true; }
            if (OnTier1 && Story.Near(p, Medic, 3)) return TalkPavelVillage(p);
            return false;
        }

        // ---------------- the bunker ----------------

        static void TalkPavelBunker(Player p)
        {
            Story.Talk("Pavel", new[] { "Stop! I have a - I have a gun. I'll use it. Are you... are you with them?" },
                new[] { "With who?", "Yefim sent us." }, i =>
                {
                    Story.Talk("Pavel", new[] {
                        i == 0 ? "The grey ones. If you don't know who I mean, you're not with them. ...God. Okay. Okay."
                               : "I don't know any Yefim. But you're not wearing robes, so I'll take the chance.",
                        "(The barricade scrapes aside. A thin man, pale, a soaked bandage on his thigh, sitting on a steel crate.)",
                        "Pavel. I was an engineer. Before. I built things for people I shouldn't have.",
                        "This crate - I can't explain now. If they get it, a lot of people stop being people. Can you get me out?" },
                        new[] { "Can you walk?", "What's in it?" }, j =>
                        {
                            Story.Talk("Pavel", new[] { j == 0 ? "Badly. I'll keep up. I'll have to." : "Later. Please. When there are walls between us and the marsh, I'll tell you everything. Some of it." });
                            SaveSystem.Save();                                    // what is loaded if he dies on the way
                            Companion.JoinStory(p, "Pavel", 60);
                            Story.SetStep("engineer", 2);
                            Story.SetObjective("engineer", "Get Pavel back to Bonfire Village alive", Yefim);
                            Story.Journal("The Engineer: Pavel, a wounded defector, hid in the Flooded Bunker with a crate. Get him to Bonfire Village.");
                            GameLog.Event("Pavel is walking with you. Keep him alive.");
                        });
                });
        }

        // ---------------- the village ----------------

        static bool TalkPavelVillage(Player p)
        {
            int step = Story.Step("culvert");
            if (step == 1)
            {
                Story.Talk("Pavel", new[] {
                    "When I ran, I had two things. The crate, and a canister. I couldn't carry both through the marsh, so I hid the canister in the Rust Culvert - the drainage cave by the road.",
                    "Take this radio. I'll talk you through it. It's sealed, grey, about this big. Don't open it. Please don't open it." });
                MacroState.Current.DiscoveredHiddenNodes.Add(CaveZone);   // the brush at the culvert's mouth: Pavel showed you the way in
                Story.SetStep("culvert", 2);
                Story.SetObjective("culvert", "Fetch Pavel's canister from the Rust Culvert", "Rust Culvert");
                Story.Journal("The Culvert: Pavel hid a sealed canister deep in the Rust Culvert.");
                return true;
            }
            if (step == 4 && Holder(Canister) is Player h)
            {
                h.RemoveItem(Canister, 1);
                Story.Talk("Pavel", new[] {
                    "(Pavel opens the canister on the medic's table. Inside: a hard drive in foam, and a sheaf of printed logs.)",
                    "Field test logs. They've been testing it here. In this marsh.",
                    "It's a transmitter. It doesn't kill you. It switches you off. What's left still walks, still holds a gun, still remembers how to pull a trigger - badly. Nothing else.",
                    "Yefim: The fishermen who didn't come home. The courier.",
                    "I'm sorry. I'm so sorry. I helped build the first one." },
                    new[] { "Who are they? The ones in grey?", "How do we stop it?" }, i =>
                    {
                        Story.Talk("Pavel", new[] { i == 0
                            ? "I never learned their name. I'm not sure they have one. People who decided the Zone should be... quieter. Obedient."
                            : "I don't know yet. The drive has more. I need time, and somewhere safer than a village with a fence." });
                        Story.Journal("The Husks: \"those\" are testing a mind-switching transmitter in the marsh. The missing people are Husks now.");
                        Story.Set("husks_known");
                        Story.Finish("culvert");
                    });
                return true;
            }
            return false;
        }

        // ---------------- the cave ----------------

        /// <summary>StorySystem.OnEnterZone: the Brute sleeps in the Mutant Den while the canister is still in the cave.</summary>
        public static void OnEnterZone(string zoneId)
        {
            _brute = null; _wake = false;
            if (zoneId != CaveZone) return;
            bool quest = Story.Step("culvert") >= 2 && Story.Step("culvert") <= 3, rematch = Story.Step("brute2") == 1;
            var den = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == Den); if (den == null) return;
            if (!TerrainMap.InteractiveObjects.Any(o => o.Name == "The Brute's Hoard"))   // a bag of Tier 3 loot at the back of the lair, past where it sleeps
            {
                var (hx, hy) = HoardSpot(den);
                Stashes.Hoard(hx, hy, "The Brute's Hoard", 3, "Scraps and gear dragged back here by something big. Worth the trip.");
            }
            if (!quest && !rematch) return;
            var (x, y) = TerrainMap.FindNearestEnterable(den.X, den.Y + 1);
            _brute = EncounterGeneration.CreateMutantFromDb(MutantDatabase.Tiers["tier4"]["BlightBrute"], x, y);
            _brute.MaxHp = _brute.Hp = BruteHp;   // Yan: 10k HP - kill it if you can
            _brute.IsAggroed = false;
            EncounterState.Enemies.Add(_brute);
            if (quest)
            {
                Story.Radio("Pavel", "You're in? Good. It's further than you think. I was... I was panicking.");
                Story.Radio("Pavel", "There's a pool, and past it a shelf of rock. I wedged it behind the rocks. You should see it.");
            }
        }

        static void TakeCanister(Player p)
        {
            Give(Canister, "Sealed Canister");
            GameLog.Event("Grey, sealed, cold. Behind it the rock is scored with long scrapes.");
            Story.SetStep("culvert", 3);
            Story.SetObjective("culvert", "Get out of the cave", CaveMouth);
            if (_brute != null && _brute.IsAlive())
            {
                Story.Cutscene(_brute.X, _brute.Y, 3.2f);
                GameLog.Event("The dark at the far end of the cave moves. Something enormous stands up. Rock dust falls from its back.");
                Story.Radio("Pavel", "What was - what's that noise? Is that - that's not - get out. GET OUT. RUN!");
                _wake = true; _wakeAt = UnityEngine.Time.time + 3.2f;
            }
        }

        // ---------------- every tick ----------------

        static void TickPavel()
        {
            if (Story.Step("brute2") == 1 && _brute != null && !_brute.IsAlive())   // Yefim's rematch: it's down
            {
                Story.Finish("brute2"); Pay(1500, "the Blight Brute"); Story.Journal("The Blight Brute of the Rust Culvert is dead. Yefim's hunters can use the road again.");
                Story.Talk("Yefim (radio)", new[] { "The road's open again. My hunters are already out. I owe you." });
            }
            if (_wake && UnityEngine.Time.time >= _wakeAt) { _wake = false; if (_brute != null && _brute.IsAlive()) _brute.IsAggroed = true; }
            // Pavel walking with you died: back to the bunker (see the header)
            if (Story.Step("engineer") == 2 && !PavelWithYou)
            {
                PavelLost("The bunker's barricade is shut again.", () =>
                {
                    Story.SetStep("engineer", 1);
                    Story.SetObjective("engineer", "Pavel is down. Go back to the Flooded Bunker for him", "Flooded Bunker");
                });
            }
            // the walk back: "those" stop and watch from a distance, and at the lamps' edge they stand a while, then go
            if (Story.Step("engineer") == 2 && OnTier1 && PavelWithYou && !Story.Has("robes_watched") && Player.Current != null && !GreyRobes.Present)
            {
                var pl = Player.Current; var yf = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == Yefim);
                if (yf != null && System.Math.Max(System.Math.Abs(pl.X - yf.X), System.Math.Abs(pl.Y - yf.Y)) > 30)
                {
                    Story.Set("robes_watched");
                    GreyRobes.Place(pl.X + (yf.X < pl.X ? -20 : 20), pl.Y + 6, 2, true);
                    GameLog.Event("The robed figures stop and watch. They don't come closer.");
                    Story.Radio("Pavel", "That's them. Don't let them see me. Please.");
                }
            }
            // he's home: the village's lamps
            if (Story.Step("engineer") == 2 && OnTier1 && PavelWithYou && Story.AnyNear(Yefim, 8))
            {
                Story.Talk("Yefim", new[] { "Who is this? ...Never mind. Inside. Medic! Now!", "(Pavel, to you:) Thank you. I mean it. They'll know where I am now. But at least there's a fence." });
                Companion.Dismiss();
                if (GreyRobes.Present) { GameLog.Event("The robed figures stop at the edge of the guards' lamps. They stand there a long time. Then they're gone."); GreyRobes.Release(); }
                Story.Finish("engineer");
                Story.SetStep("culvert", 1);
                Story.SetObjective("culvert", "Talk to Pavel at the Medic's House", Medic);
                Story.Journal("Pavel is safe in Bonfire Village, at the Medic's House.");
            }
            // out of the cave with the canister: the Brute can't follow
            if (Story.Step("culvert") == 3 && OnTier1)
            {
                GameLog.Event("The thing slams into the pipe mouth behind you. Concrete cracks. It can't fit. It screams.");
                Story.Radio("Pavel", "Are you alive? Say something. ...Oh, thank God. That thing doesn't belong here. Nothing that big lives this far south. Something is pushing them.");
                Story.Journal("A Blight Brute lives in the Rust Culvert. Something is pushing the deep-Zone mutants south.");
                Story.SetStep("culvert", 4);
                Story.SetObjective("culvert", "Bring the canister to Pavel", Medic);
            }
        }
    }
}
