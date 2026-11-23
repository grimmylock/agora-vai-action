// Story 5b (2026-11-18): "Defend Bonfire Village" - Tier 1's last story mission (STORY.md, STORY_DIALOGUE.md "the final night").
// Step "defense": 0 = not yet, 1 = the night is running, done = dawn. Starts at dusk (19:00-23:00) once the canister is read (`husks_known`) and you are in the village.
// - Four waves of Husk packs (4, 5, 6, 8) are put down at the edge of the map round the village, one every WaveGap seconds, and walk at whoever is nearest. Husks never fall back.
//   The village's own guards and four extra Wanderers hold the fence (real squads: they fight the Husks for real, NpcFights).
// - Pavel is behind the Medic's House door (a talk point, not a unit): each Husk within two tiles of the house wears it down. At 0 the mission is lost: the auto-save made at dusk is loaded
//   (STORY.md: "lost only if you die or the Engineer dies -> retry from a save before it"; like Tier 2's shot).
// - After the last wave dies: dawn, the talk, +900 Ru, the road north (journal). Rewards go to every player.
// - ponytail: the win rate was NOT measured with a probe (STORY.md asked for one); the waves are a first guess. Husks hold no kit worth stealing (Broken loot). Not run in Unity.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static partial class Tier1Story
    {
        public const string DefenceMission = "defense";
        public static readonly int[] WaveSizes = { 4, 5, 6, 8 };
        public const float WaveGap = 70f, DoorHp = 100f, DoorWearPerSecond = 2.5f;
        static int _wave; static float _nextWave = -1f, _doorHp, _doorSay = 100f;
        static readonly List<Mutant> _husks = new List<Mutant>();

        static bool Dusk(float h) => h >= 19f && h < 23f;
        static bool InVillage(Player p) => TerrainMap.RegionAt(p.X, p.Y) == "bonfire_village";

        /// <summary>Per game minute (Tick): the evening it begins.</summary>
        static void TickDefenceStart()
        {
            if (Story.Step(DefenceMission) != 0 || !Story.IsDone("culvert") || !Story.Has("husks_known") || !Dusk(MacroState.Current.Hour)) return;
            if (!Player.All.Any(p => p.IsAlive() && InVillage(p))) return;
            SaveSystem.Save();                                             // "retry from a save before it"
            Story.Talk("Guard", new[] { "Yefim... Yefim! The marsh is moving!",
                "Yefim: Everyone with a gun, to the fence! Pavel, the Medic's House - lock the door and don't open it for anyone you can't name!",
                "Yefim: (to you) You brought him here. Now help us keep him." });
            Story.SetStep(DefenceMission, 1);
            Story.SetObjective(DefenceMission, "Defend Bonfire Village. Keep the Medic's House door shut", Medic);
            Story.Journal("The Husk horde is coming out of the marsh at night. Hold Bonfire Village.");
            Raiders0();
            _wave = 0; _nextWave = Time.time + 20f; _doorHp = DoorHp; _doorSay = 100f; _husks.Clear();
        }

        static void Raiders0()   // four extra Wanderers hold the fence beside the guards
        {
            var list = new List<Mutant>();
            for (int i = 0; i < 4; i++)
            {
                var (w, a) = EncounterGeneration.PickFactionLoadout("Wanderers", "Experienced", 1, i == 0);
                list.Add(new HumanEnemy("Village Guard", 0, 0, "Wanderers", "Experienced", w, a));
            }
            Place("defense_guards", Yefim, "Wanderers", list);
        }

        /// <summary>Every frame (Rt.WorldTick): the waves, the door, the end.</summary>
        public static void TickRt(float dt)
        {
            if (Net.Mode == Net.Role.Client || Story.Step(DefenceMission) != 1 || Story.IsDone(DefenceMission)) { if (Story.Step(DefenceMission) != 1) _husks.Clear(); return; }
            float now = Time.time;
            _husks.RemoveAll(m => !m.IsAlive());
            if (_wave < WaveSizes.Length && now >= _nextWave) { Release(_wave); _wave++; _nextWave = now + WaveGap; }
            foreach (var m in _husks) m.IsAggroed = true;                    // they never lose you
            var med = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == Medic);
            if (med != null)
            {
                int at = _husks.Count(m => System.Math.Max(System.Math.Abs(m.X - med.X), System.Math.Abs(m.Y - med.Y)) <= 2);
                _doorHp -= at * DoorWearPerSecond * dt;
                float pct = 100f * _doorHp / DoorHp;
                if (pct < _doorSay - 25f && pct > 0f) { _doorSay = pct; Story.Radio("Pavel", pct > 50f ? "Something's at the door. Scratching. Please hurry." : pct > 25f ? "The wood is splitting. HURRY." : "It's - the hinges - please!"); }
                if (_doorHp <= 0f) { Lose(); return; }
            }
            if (_wave >= WaveSizes.Length && _husks.Count == 0) Win();
        }

        static void Release(int i)
        {
            var med = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == Medic); if (med == null) return;
            // a side of the map: east, north, west-ish, south-ish (the marsh is east; Yan: "waves from several sides")
            var dirs = new[] { (1, 0), (0, -1), (-1, 0), (1, 1) }; var (dx, dy) = dirs[i % dirs.Length];
            int w = WaveSizes[i];
            var pack = Husk.Pack(1, w);
            SquadManager.PlaceQuestSquad("bonfire_village", Husk.Faction, pack, "story:defense_wave" + i);
            int k = 0;
            foreach (var m in pack)
            {
                int ex = Mathf.Clamp(med.X + dx * 38 + (k % 4) * 2 - 3, 3, TerrainMap.Width - 4), ey = Mathf.Clamp(med.Y + dy * 30 + (k / 4) * 2, 3, TerrainMap.Height - 4);
                var (x, y) = TerrainMap.FindNearestEnterable(ex, ey, 10);
                m.X = x; m.Y = y; m.IsAggroed = true; k++;
                if (!EncounterState.Enemies.Contains(m)) EncounterState.Enemies.Add(m);
                _husks.Add(m);
            }
            GameLog.Event(i == 0 ? "Husks on the road! They're not stopping!" : i == 2 ? "More of them. Some carry rifles. Some carry nothing but their hands." : $"Wave {i + 1}: the marsh gives up more of them.");
        }

        static void Lose()
        {
            Story.SetStep(DefenceMission, 0); _wave = 0; _husks.Clear();
            GameLog.Event("[ALERT] The door gives. Pavel is dead. Back to the dusk before it.");
            Story.Fade(Color.black, 1f, 0.6f);
            if (!SaveSystem.Load()) { Story.SetObjective(DefenceMission, null); Story.Fade(Color.black, 0f, 0.6f); }
        }

        static void Win()
        {
            Story.SetObjective(DefenceMission, null);
            MacroState.Current.Hour = 6f;
            Story.Talk("Yefim", new[] { "Still here. All of us that matter. ...Most of us.",
                "They'll come again. Not tonight. But Pavel can't stay. North is where the answers are, and north is where you're going.",
                "Pavel: There's a scientist. Osei. At the Dried Lake, past the Scavenger's Hub. If anyone can read this drive, she can. But I can't walk through two more regions of that. Not yet. Find the road first. Then come for me." });
            Pay(900, "the night the village held");
            Story.Journal("The Road North: find a route north for Pavel. Tier 2 lies past the Causeway and the Cutting.");
            Story.Finish(DefenceMission);
            Story.Set("tier1_done");
        }
    }
}
