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

    public static class GameLog
    {
        public static IGameLogTarget Target;
        /// <summary>Batch 36: while true, lines go to the Console only (the hourly A-Life tick).</summary>
        public static bool Quiet;

        /// <summary>Direct port of log(message) in script.js.</summary>
        public static void Write(string message)
        {
            if (Target != null && !Quiet)
                Target.AppendLine(message);
            else
                Debug.Log(message);
        }
    }
}
