// Batch 129 (Yan: "Games like bg3, Stalker, Death road to canada, have npc's having little talks that add to the world ... you're
// scouting a squad or a friendly squad passes by and you can see little words above their heads ... or they acknowledge the player
// with a 'hey'"; and "when an enemy detects you they can shout something like 'SOMEONE'S HERE'"): short lines over people's heads
// (HudView.Barks draws them). Squads you can see trade two-line exchanges now and then, a friendly squad passing close greets you,
// an enemy noticing something says so, and one that spots you shouts. Only what you can see talks; it's flavour, not noise.
// Four voices: gangs (Raiders, Cartel, Outcasts), soldiers (Garrison, Sentinels, Contractors, IZC), the Obelisk, and drifters (the rest).
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public static class Banter
    {
        public class Bark { public Mutant Who; public string Text; public float Start, Until; public int Kind; }   // Kind: 0 talk, 1 notice, 2 shout
        public static readonly List<Bark> Active = new List<Bark>();
        public const int HearRange = 14, GreetRange = 5;
        public const float LineSeconds = 2.8f;
        static readonly Dictionary<OfflineSquad, float> _next = new Dictionary<OfflineSquad, float>();   // real time a squad may talk again
        static readonly HashSet<OfflineSquad> _greeted = new HashSet<OfflineSquad>();
        static readonly System.Random _rng = new System.Random();
        public static float Now = -1f;   // the harness sets a clock; the game uses Time.time
        static float T => Now >= 0f ? Now : UnityEngine.Time.time;

        public static void Reset() { Active.Clear(); _next.Clear(); _greeted.Clear(); }

        static string Voice(string faction)
        {
            switch (faction)
            {
                case "Raiders": case "Cartel": case "Outcasts": return "gang";
                case "Garrison": case "Sentinels": case "Contractors": case "IZC": return "army";
                case "Obelisk": return "zealot";
                default: return "drifter";
            }
        }

        static T1 Pick<T1>(IList<T1> list) => list[_rng.Next(list.Count)];

        public static void Say(Mutant who, string text, int kind = 0, float delay = 0f)
        {
            if (who == null || string.IsNullOrEmpty(text)) return;
            Active.RemoveAll(b => b.Who == who && b.Start <= T + delay);
            float start = T + delay;
            Active.Add(new Bark { Who = who, Text = text, Start = start, Until = start + LineSeconds + text.Length * 0.03f, Kind = kind });
        }

        /// <summary>An enemy's alert started filling (Contact.Notice): a muttered "huh?". Humans only.</summary>
        public static void Notice(Mutant e)
        {
            if (e is HumanEnemy && !Active.Any(b => b.Who == e)) Say(e, Pick(Huh), 1);
        }

        /// <summary>It spotted you (TurnLoop.CheckVisionAndWander): one shout per squad, the first who saw you.</summary>
        public static void Spotted(Mutant e)
        {
            if (!(e is HumanEnemy h)) return;
            if (Active.Any(b => b.Kind == 2 && b.Who is HumanEnemy o && o.FactionName == h.FactionName && CombatantUtil.Distance(o, h) <= 12 && b.Until > T)) return;
            Say(e, Pick(Shouts[Voice(h.FactionName)]), 2);
        }

        /// <summary>Every game minute (TurnLoop.TickAwareness): greetings and squad chatter, for squads you can see.</summary>
        public static void Tick(Player p)
        {
            Active.RemoveAll(b => b.Until < T || b.Who == null || !b.Who.IsAlive());
            if (p == null || EncounterState.InCombat || GameState.Current.ModalOpen) return;
            foreach (var sq in MacroState.Current.OfflineSquads)
            {
                var near = sq.Members.OfType<HumanEnemy>().Where(m => m.IsAlive() && !m.IsAggroed && !Companion.Is(m) && m.Morale == "Steady"
                    && EncounterState.Enemies.Contains(m) && Vision.Shows(m) && CombatantUtil.Distance(p, m) <= HearRange).ToList();
                if (near.Count == 0) continue;
                if (Active.Any(b => near.Contains(b.Who as HumanEnemy))) continue;   // still talking
                var closest = near.OrderBy(m => CombatantUtil.Distance(p, m)).First();
                if (EnemyAI.FriendlyToPlayer(closest) && CombatantUtil.Distance(p, closest) <= GreetRange && _greeted.Add(sq))
                {
                    closest.FaceToward(p.X, p.Y);
                    Say(closest, Pick(p.GetRepLevel(closest.FactionName) == "Friendly" ? Greet[Voice(closest.FactionName)] : Nod[Voice(closest.FactionName)]));
                    _next[sq] = T + 8f;
                    continue;
                }
                if (near.Count < 2 || (_next.TryGetValue(sq, out var at) && T < at)) continue;
                var pair = Exchange(Voice(sq.Faction), near[0].FactionName);
                var a = near[_rng.Next(near.Count)]; var b2 = near.Where(m => m != a).OrderBy(m => CombatantUtil.Distance(m, a)).First();
                a.FaceToward(b2.X, b2.Y); b2.FaceToward(a.X, a.Y);
                Say(a, pair.Item1);
                Say(b2, pair.Item2, 0, LineSeconds + 0.4f);
                _next[sq] = T + 14f + (float)_rng.NextDouble() * 16f;
            }
        }

        static (string, string) Exchange(string voice, string faction)
        {
            var pool = new List<(string, string)>(Talk[voice]);
            float h = MacroState.Current.Hour;
            if (h >= 21f || h < 5f) pool.AddRange(Night);
            if (Atmosphere.Look().Rain > 0f) pool.AddRange(Rain);
            return Pick(pool);
        }

        // ------------------------------------------------------------------ lines (original writing)
        static readonly string[] Huh = { "Huh?", "...You hear that?", "Hold on.", "Something moved.", "Who's there?", "Hm." };

        static readonly Dictionary<string, string[]> Shouts = new Dictionary<string, string[]>
        {
            ["gang"] = new[] { "SOMEONE'S HERE!", "Fresh meat!", "Over there! Get 'em!", "We got a visitor!", "Look alive, we got company!" },
            ["army"] = new[] { "CONTACT!", "Hostile spotted!", "Movement! Weapons up!", "Contact front!", "Intruder! Engage!" },
            ["zealot"] = new[] { "AN UNBELIEVER!", "The Pillar shows us a sinner!", "Cleanse it!", "Trespasser on holy ground!" },
            ["drifter"] = new[] { "SOMEONE'S HERE!", "Heads up, we're not alone!", "Over there!", "Watch it, someone's coming!" },
        };
        static readonly Dictionary<string, string[]> Greet = new Dictionary<string, string[]>
        {
            ["gang"] = new[] { "Heh. Our friend.", "You again? Good.", "Easy, it's one of ours.", "Brought us anything?" },
            ["army"] = new[] { "Carry on.", "Good to see you.", "Stay sharp out there.", "Friendly. Let them through." },
            ["zealot"] = new[] { "Walk in its light.", "The Pillar knows you.", "Peace, faithful one." },
            ["drifter"] = new[] { "Hey.", "Hey there.", "Good to see a friendly face.", "Stay safe out there.", "Hey, hunter." },
        };
        static readonly Dictionary<string, string[]> Nod = new Dictionary<string, string[]>
        {
            ["gang"] = new[] { "Keep walking.", "What are you looking at?", "Don't get ideas.", "Move along." },
            ["army"] = new[] { "Keep moving.", "Eyes front, civilian.", "Don't loiter.", "Hands where we can see them." },
            ["zealot"] = new[] { "...", "You are watched.", "Go." },
            ["drifter"] = new[] { "Hey.", "Watch yourself, rookie.", "Mind the road.", "Evening." },
        };
        static readonly Dictionary<string, (string, string)[]> Talk = new Dictionary<string, (string, string)[]>
        {
            ["gang"] = new[] {
                ("You see the boots on that last one?", "Too small. Story of my life."),
                ("How much for the rifle?", "More than you've got."),
                ("Boss says we move at dawn.", "Boss says a lot of things."),
                ("Who ate the last can?", "...Rats. Big rats."),
                ("I'm telling you, the toll's too low.", "Then you go ask them nicer."),
                ("Heard there's a stash by the culvert.", "Heard that last week too."),
                ("Quiet day.", "Don't jinx it."),
                ("My knee's been off since the rain.", "Your knee's been off since you were born."),
                ("If the Sentinels come through here...", "Then we run. Like always."),
                ("Gimme a smoke.", "Buy your own."),
                ("Ever think about leaving?", "Every morning. Then I wake up."),
                ("That rookie had a nice watch.", "Had."),
            },
            ["army"] = new[] {
                ("Radio check.", "Static. Same as always."),
                ("How long till relief?", "Six hours. Stop asking."),
                ("Sector's quiet.", "Sector's never quiet."),
                ("You clean your weapon?", "Twice. It still jams."),
                ("Command wants a report by dusk.", "Command wants a lot of things."),
                ("Those tracks back there, fresh?", "Fresh enough. Keep your eyes open."),
                ("I signed up for six months.", "We all did."),
                ("Rations again.", "Beats what the scavengers eat."),
                ("Did you hear the old tower last night?", "That wasn't the tower."),
                ("Keep it tight, people.", "Copy."),
                ("Any word from the second team?", "Nothing since noon."),
                ("Boots are falling apart.", "File a request. In triplicate."),
            },
            ["zealot"] = new[] {
                ("Do you hear it singing?", "Always. Louder at night."),
                ("The pulse came early today.", "Then we are close."),
                ("A brother walked into the light.", "He did not come back. Good."),
                ("The others call us mad.", "The others are afraid."),
                ("Keep the prayer steady.", "It is steady."),
                ("The ground is warm here.", "Blessed."),
            },
            ["drifter"] = new[] {
                ("Got any bread?", "Got a bolt. Want a bolt?"),
                ("How's the leg?", "Still attached."),
                ("Saw a light out by the marsh last night.", "Then don't go to the marsh."),
                ("Yefim's raising prices again.", "Yefim's always raising prices."),
                ("One more haul and I'm out.", "That's what you said last spring."),
                ("Heard a guy found a whole crate of filters.", "Heard a guy. Sure."),
                ("My counter's been clicking all day.", "Then stop standing on that spot."),
                ("Remember when the road was safe?", "No. Neither do you."),
                ("You smell that?", "Wet dog. Hope it's a dog."),
                ("Let's rest a bit.", "We've been resting an hour."),
                ("Think anyone ever leaves this place?", "Sure. In a bag."),
                ("Nice day for it.", "For what?"),
            },
        };
        static readonly (string, string)[] Night = {
            ("Cold one tonight.", "Keep the fire small."),
            ("Can't see a thing.", "Good. Neither can they."),
            ("Your watch.", "It's always my watch."),
        };
        static readonly (string, string)[] Rain = {
            ("This rain won't quit.", "Keeps the dogs in their holes, at least."),
            ("Everything I own is wet.", "Everything you own is stolen."),
        };
    }
}
