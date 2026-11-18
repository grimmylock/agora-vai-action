// Story polish (2026-11-18): the small talk of the story's world (Banter.cs style: short lines over heads, flavour not noise).
// - Exchanges squads you can see trade: once the fence job is done they mention "those" (robes in the reeds, the grey ones); after the canister, the Husks; after the Sub-Station
//   reveal, the pulse under the lake. Added to Banter's pools (Banter.Exchange asks `Pool`).
// - The story's people (StoryNpcs units) say a line now and then when you walk past (every 50-110 s each, within 9 tiles, not in a fight).
// - Pavel, walking with you, has lines: nervous on the road, "down, down, down" when robed figures are near, "Leg's fine" when hurt (STORY_DIALOGUE.md "The Search").
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class StoryBarks
    {
        static readonly (string, string)[] Those = {
            ("Saw them again by the marsh road. Robes. Grey.", "Didn't say a word?"),
            ("Those ones were standing in the reeds an hour, just... looking at the ground.", "Don't wave at them."),
            ("Last guy who waved at them came back wrong.", "Wrong how?"),
            ("Somebody cut the fence at Watchtower 3.", "Somebody in grey, I heard."),
        };
        static readonly (string, string)[] Husks = {
            ("Saw one of the fishermen on the road. Walking. Bleeding.", "Did he stop when you called?"),
            ("Don't trust anything that moves slow out there.", "I don't trust anything that moves at all."),
            ("They drag their feet. You hear them before you see them.", "That's the idea."),
            ("A whole pack of them, pale as candles.", "Then I'm not going near the marsh."),
        };
        static readonly (string, string)[] Pulse = {
            ("The Sub-Station says the ground pulses at night.", "Everything in the Zone pulses."),
            ("My counter ticks in time with something under the lake.", "Then stop listening to your counter."),
        };

        /// <summary>Extra Banter exchanges the story has unlocked (drifters and soldiers; gangs and zealots keep their own).</summary>
        public static IEnumerable<(string, string)> Pool(string voice)
        {
            if (voice != "drifter" && voice != "army") yield break;
            if (Story.IsDone("fence")) foreach (var l in Those) yield return l;
            if (Story.Has("husks_known")) foreach (var l in Husks) yield return l;
            if (Story.Has("tier3_reveal")) foreach (var l in Pulse) yield return l;
        }

        static readonly Dictionary<string, string[]> Idle = new Dictionary<string, string[]>
        {
            ["Yefim"] = new[] { "Prices are going up again. Don't ask me why.", "I listen. Mostly I listen.", "Walk slow. Shoot fast." },
            ["Stas"] = new[] { "Quiet work. I like quiet.", "Drink's real. Mostly." },
            ["Headman Petro"] = new[] { "The nets are ours again. Barely.", "My father's key. Gone." },
            ["The Runner"] = new[] { "Every road I find, someone owns.", "I want my own routes." },
            ["The Scrap King"] = new[] { "Whoever sits on the Dock eats.", "I like people who don't make me explain things twice." },
            ["Pavel"] = new[] { "(muttering) ...part numbers, batch sizes...", "I built things for people I shouldn't have.", "Walls. I like walls." },
            ["Lev Marchuk"] = new[] { "...", "Nine years. Nine years, and still the noise.", "Sit or leave." },
            ["Dr. Priya Osei"] = new[] { "Spikes again. Regular. Too regular.", "Bedrock doesn't spike." },
            ["Emil Rask"] = new[] { "Coffee? It's terrible, but it's hot.", "Everyone's so tense." },
            ["Konrad Hale"] = new[] { "Supplies. It's always supplies.", "Tomorrow. No, not tonight." },
        };
        static readonly Dictionary<HumanEnemy, float> _next = new Dictionary<HumanEnemy, float>();
        static float _pavelAt;
        static readonly string[] PavelRoad = { "I used to drive this road. In a car. With heating.", "Is that... is that one of them on the ridge?", "Keep an eye on the reeds. Please.", "How much further?" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { _next.Clear(); _pavelAt = 0f; }

        /// <summary>Every frame (Rt.WorldTick).</summary>
        public static void Tick()
        {
            var p = Player.Current; if (p == null || EncounterState.InCombat || GameState.Current.ModalOpen) return;
            float now = Time.time;
            foreach (var n in EncounterState.Enemies.OfType<HumanEnemy>())
            {
                if (!StoryNpcs.Holds(n) || !n.IsAlive() || !Idle.TryGetValue(n.Name, out var lines) || CombatantUtil.Distance(p, n) > 9) continue;
                if (!_next.TryGetValue(n, out var at)) { _next[n] = now + Random.Range(4f, 30f); continue; }
                if (now < at) continue;
                _next[n] = now + Random.Range(50f, 110f);
                Banter.Say(n, lines[Random.Range(0, lines.Length)]);
            }
            var pav = Companion.Alive && Companion.Current.Name == "Pavel" ? Companion.Current : null;
            if (pav != null && now >= _pavelAt)
            {
                _pavelAt = now + Random.Range(35f, 80f);
                bool robes = EncounterState.Enemies.Any(e => GreyRobes.Is(e) && CombatantUtil.Distance(p, e) <= 28);
                string line = robes ? (Random.value < 0.5f ? "That's them. Don't let them see me. Please." : "Down, down, down...")
                    : pav.Hp * 2 < pav.MaxHp ? "Leg's... fine. It's fine. Keep going." : PavelRoad[Random.Range(0, PavelRoad.Length)];
                Banter.Say(pav, line);
            }
        }
    }
}
