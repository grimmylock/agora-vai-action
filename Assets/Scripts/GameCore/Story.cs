// Story 3 (Yan, 2026-10-29: "you can start by building the story systems"): the pieces every mission in STORY.md uses.
// - Talk: a conversation - lines one after another in the dialogue panel (ChoiceUI), the answers on the last line.
// - Radio: a line over the radio, in the log while you keep playing.
// - Journal: entries the J panel lists, oldest first.
// - Step / Objective: where each mission is, and what it wants next; an objective's landmark gets the map's objective marker.
// - Spot: a landmark picked once per world for a mission (Yan: "a different POI not connected to any current mission, chosen
//   when the world is created, adds some replayability").
// - Cutscene: the camera frames a point and you stand still for a few seconds while the world runs.
// Everything is kept in MacroState.StoryFlags ("m:", "o:", "j:", "spot:" prefixes), so it's saved with the world as it is.
// The missions themselves come next (Tier 1, STORY.md); the old acts in StorySystem.cs stay until each is replaced.
// Co-op: the story belongs to the host's world. ponytail: conversations, radio and cutscenes show on the host only - clients
// see them when the snapshot carries story events (with the enemies and shots, Net.cs).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class Story
    {
        static List<string> Flags => MacroState.Current.StoryFlags;

        static string Get(string prefix) => Flags.FirstOrDefault(f => f.StartsWith(prefix))?.Substring(prefix.Length);
        static void Put(string prefix, string value) { Flags.RemoveAll(f => f.StartsWith(prefix)); if (value != null) Flags.Add(prefix + value); }

        // ---------------- missions ----------------

        /// <summary>A mission's step: 0 = not started; each mission numbers its own steps; Done = finished.</summary>
        public static int Step(string mission) => int.TryParse(Get($"m:{mission}="), out int s) ? s : 0;
        public static void SetStep(string mission, int step) => Put($"m:{mission}=", step.ToString());
        public const int Done = 999;
        public static bool IsDone(string mission) => Step(mission) >= Done;
        public static void Finish(string mission) { SetStep(mission, Done); SetObjective(mission, null); }

        /// <summary>What the mission wants now ("Find the courier's pack"), and where (a landmark name, or null). null text clears it.</summary>
        public static void SetObjective(string mission, string text, string landmark = null) =>
            Put($"o:{mission}=", text == null ? null : text + "|" + (landmark ?? ""));

        public static IEnumerable<(string mission, string text, string landmark)> Objectives =>
            Flags.Where(f => f.StartsWith("o:")).Select(f =>
            {
                int eq = f.IndexOf('='), bar = f.LastIndexOf('|');
                return (f.Substring(2, eq - 2), f.Substring(eq + 1, bar - eq - 1), bar + 1 < f.Length ? f.Substring(bar + 1) : null);
            });

        /// <summary>Is this landmark (on the map you're on) one a mission wants you at? Drives the map's objective marker.</summary>
        public static bool IsObjective(string landmark) => Objectives.Any(o => o.landmark == landmark);

        // ---------------- the journal ----------------

        /// <summary>Add a journal entry once (the same text twice is one entry).</summary>
        public static void Journal(string text) { if (!Flags.Contains("j:" + text)) { Flags.Add("j:" + text); GameLog.Write("[JOURNAL] " + text); } }
        public static IEnumerable<string> JournalEntries => Flags.Where(f => f.StartsWith("j:")).Select(f => f.Substring(2));

        // ---------------- per-world spots ----------------

        /// <summary>The landmark this world picked for `key` - picked the first time it's asked, from the map you're on, never one
        /// in `reserved` (the places missions already use) and never another spot's.</summary>
        public static string Spot(string key, params string[] reserved)
        {
            var had = Get($"spot:{key}=");
            if (had != null) return had;
            var taken = new HashSet<string>(Flags.Where(f => f.StartsWith("spot:")).Select(f => f.Substring(f.IndexOf('=') + 1)).Concat(reserved));
            var free = (TerrainMap.Landmarks ?? new List<LandmarkInstance>())
                .Where(l => !l.Name.Contains(":") && !taken.Contains(l.Name)).Select(l => l.Name).Distinct().ToList();
            if (free.Count == 0) return null;
            var pick = free[UnityEngine.Random.Range(0, free.Count)];
            Put($"spot:{key}=", pick);
            return pick;
        }

        // ---------------- talking ----------------

        /// <summary>A conversation: each line in the dialogue panel, "..." to go on; the last line shows `answers` (or one "..."),
        /// and `then` gets the answer picked. `who` is the name on the panel; `unit` is them on the map, if they're there.</summary>
        public static void Talk(string who, string[] lines, string[] answers = null, Action<int> then = null, ICombatant unit = null)
        {
            void Line(int i)
            {
                bool last = i == lines.Length - 1;
                var opts = last && answers != null && answers.Length > 0 ? answers.ToList() : new List<string> { "..." };
                ChoiceUI.Show(who, lines[i], opts, pick => { if (last) then?.Invoke(pick); else Line(i + 1); }, unit);
            }
            if (lines == null || lines.Length == 0) { then?.Invoke(0); return; }
            Line(0);
        }

        /// <summary>A line over the radio: you keep playing.</summary>
        public static void Radio(string who, string text) { GameLog.Write($"[RADIO] {who}: {text}"); Sfx.Play("ui_click"); }   // ponytail: no radio crackle sound yet

        // ---------------- cutscenes ----------------

        public static (float x, float y)? LookAt;   // tile space; the camera frames you and this point
        public static float CutsceneUntil = -1f;
        public static bool InCutscene => Time.time < CutsceneUntil;

        /// <summary>The camera frames you and (x, y) for `seconds` (game time); you stand still, the world keeps running.</summary>
        public static void Cutscene(float x, float y, float seconds) { LookAt = (x, y); CutsceneUntil = Time.time + seconds; }

        public static void Reset() { LookAt = null; CutsceneUntil = -1f; }
    }
}
