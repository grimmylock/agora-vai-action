// 2026-11-24 (Yan: survival mode "stutters A LOT, even with nothing happening on screen"): a hitch finder, since nothing here can be profiled without Unity. Wrap a part of the frame in `using (Perf.Sec("name"))`;
// any section that takes longer than SlowMs prints one line to the Console (UnityEngine.Debug.Log, "[PERF] name 14.2 ms") and the game log's tail, at most a few a second. Costs a Stopwatch read per section.
using System.Diagnostics;

namespace GameCore
{
    public static class Perf
    {
        public const double SlowMs = 6.0;
        static readonly Stopwatch Clock = Stopwatch.StartNew();
        static double _quietUntil;
        public static bool On = true;

        public readonly struct Scope : System.IDisposable
        {
            readonly string _name; readonly double _t0;
            public Scope(string name) { _name = name; _t0 = Clock.Elapsed.TotalMilliseconds; }
            public void Dispose()
            {
                if (!On) return;
                double now = Clock.Elapsed.TotalMilliseconds, ms = now - _t0;
                if (ms < SlowMs || now < _quietUntil) return;
                _quietUntil = now + 250.0;                                   // a few lines a second at most
                UnityEngine.Debug.Log("[PERF] " + _name + " " + ms.ToString("F1") + " ms");
            }
        }
        public static Scope Sec(string name) => new Scope(name);
    }
}
