// Ports the global `log(message)` function from script.js, which appended a line to the
// #combatLog div. In Unity this needs to write into whatever UI element replaces that div, so
// it's a swappable interface (set GameLog.Target once you have that UI piece wired up) rather
// than a hardcoded UnityEngine.Debug.Log call. Until Target is assigned, messages fall back to
// Debug.Log so nothing is silently lost during development.
using UnityEngine;

namespace GameCore
{
    public interface IGameLogTarget
    {
        void AppendLine(string message);
    }

    /// <summary>A log target that also wants to know whether a line is an EVENT (dialogue, story, world news) - the on-screen log in real time shows only those.</summary>
    public interface IGameLogEvents { void Append(string message, bool isEvent); }

    public static class GameLog
    {
        /// <summary>Invisible marker GameLog.Event puts in front of a line so the co-op friends' logs know it is an event too.</summary>
        public const string EventMark = "\u2063";
        static readonly string[] EventTags = { "[JOURNAL]", "[RADIO]", "[ALERT]", "[SQUAD]", "[SAFE ZONE]", "[SURVIVAL]", "[NEWS]", "[SURGE]" };
        /// <summary>Yan, 2026-11-17: "message log only for dialogue and world events". Story, quest, journal, radio and world-news lines are written with Event; everything else (hits, reloads, steps, pick-ups, rolls)
        /// still goes to the Console but not to the on-screen log in real time.</summary>
        public static void Event(string message) => Write(EventMark + message);
        static bool IsEvent(string m) { if (m.StartsWith(EventMark, System.StringComparison.Ordinal)) return true; foreach (var t in EventTags) if (m.StartsWith(t, System.StringComparison.Ordinal)) return true; return false; }

        public static IGameLogTarget Target;
        /// <summary>Batch 36: while true, lines go to the Console only (the hourly A-Life tick).</summary>
        public static bool Quiet;

        /// <summary>Direct port of log(message) in script.js.</summary>
        public static void Write(string message)
        {
            NetWorld.TapLog(message);                                        // co-op: the host's lines reach the friends
            bool ev = IsEvent(message); if (message.StartsWith(EventMark, System.StringComparison.Ordinal)) message = message.Substring(EventMark.Length);   // (decided on the English, before the translation changes the tag)
            if (Loc.Pt) { string tr = Loc.T(message); if (tr == message) Loc.Miss(message); message = tr; }                                // pt-BR: the log in Portuguese (a line with no entry stays English)
            if (Target != null && !Quiet)
            {
                if (Target is IGameLogEvents t) t.Append(message, ev); else Target.AppendLine(message);
            }
            else
                Debug.Log(message);
        }
    }
}
