// Real lighting in the game (Yan, 2026-11-04: "real lighting ... shadows reacting to light sources, similar to how octopath traveler
// looks" - "lets follow with it"). The offline prototype (WorldArt.Lights.cs) made the look; this runs it live:
//   - the map's placed lights (lamp posts, campfires, ceiling and emergency lights) are baked once each against what stands round them
//     (WorldArt.BakeLight: the shadows they throw, what every sprite pixel is) - at most one new bake a frame, and a bake is dropped
//     when a prop moves or a wall breaks near it (WorldArt.Refresh -> Invalidate)
//   - every frame the baked lights are added into a field over the view (8 samples a tile) - cheap
//   - AtmosphereView turns that field into the night overlay: lit where a lamp reaches, dark in the shadows, warm round the flame
// F7 (editor) switches it off to compare with the old overlay; anything that throws turns it off for the session and logs why.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class Lighting
    {
        public static bool Enabled = Load();
        public static bool Failed;
        static bool Load() { try { return PlayerPrefs.GetInt("lighting_v2", 1) == 1; } catch { return true; } }

        public static void Toggle()
        {
            Enabled = !Enabled;
            try { PlayerPrefs.SetInt("lighting_v2", Enabled ? 1 : 0); PlayerPrefs.Save(); } catch { }
            GameLog.Write($"[DEV] Lighting v2 {(Enabled ? "on" : "off")}.");
        }

        public static void Fail(System.Exception e)
        {
            if (Failed) return;
            Failed = true;
            GameLog.Write("[DEV] Lighting v2 turned itself off: " + e.Message);
            Debug.LogException(e);
        }

        static List<WorldArt.PointLight> _lights;
        static WorldArt.LightBake[] _bakes; static float[] _bakedAt; static bool[] _stale;   // 2026-11-18: a light whose surroundings changed keeps its old bake for RebakeEvery seconds, then looks again (one bake a frame at most)
        const float RebakeEvery = 2.5f;
        // Editor only (2026-11-18): what the lighting costs, logged to the Console every 5 s while the fine overlay runs - "[light] ..." lines Yan can copy back when it is still slow
        public static double OverlayMs, BakeMs, AddMs; public static int OverlayN, BakeN, LampN; static float _reportAt;
        public static void Report()
        {
#if UNITY_EDITOR
            if (Time.realtimeSinceStartup < _reportAt) return; _reportAt = Time.realtimeSinceStartup + 5f;
            if (OverlayN + BakeN == 0) return;
            Debug.Log($"[light] last 5 s: overlay redrawn {OverlayN}x, {(OverlayN > 0 ? OverlayMs / OverlayN : 0):0.0} ms each ({OverlayMs:0} ms total), of which the lamp field {(OverlayN > 0 ? AddMs / OverlayN : 0):0.0} ms each ({LampN / System.Math.Max(1, OverlayN)} lamps in view); {BakeN} lamp bakes, {(BakeN > 0 ? BakeMs / BakeN : 0):0.0} ms each");
            OverlayMs = BakeMs = AddMs = 0; OverlayN = BakeN = LampN = 0;
#endif
        }
        static string _zone; static int _gen = -1;
        static float[] _lr, _lg, _lb;

        public static bool Active => _lights != null;
        /// <summary>The map's placed lights (the 2D lights test turns each into a Light2D).</summary>
        public static IReadOnlyList<WorldArt.PointLight> Placed() => Ensure() ? (IReadOnlyList<WorldArt.PointLight>)_lights : System.Array.Empty<WorldArt.PointLight>();
        public static void Reset() { _lights = null; _bakes = null; _bakedAt = null; _stale = null; _zone = null; }

        static bool Ensure()
        {
            if (!WorldArt.Ready || TerrainMap.Tiles == null) return false;
            if (_lights != null && _zone == TerrainMap.ZoneId && _gen == WorldArt.Generation) return true;
            _lights = WorldArt.PlacedLights(0, 0, TerrainMap.Width - 1, TerrainMap.Height - 1);
            _bakes = new WorldArt.LightBake[_lights.Count]; _bakedAt = new float[_lights.Count]; _stale = new bool[_lights.Count];
            _zone = TerrainMap.ZoneId; _gen = WorldArt.Generation;
            return true;
        }

        /// <summary>A tile's art changed: the baked lights that reach it must look again.</summary>
        public static void Invalidate(int cx, int cy)
        {
            if (_lights == null) return;
            for (int i = 0; i < _lights.Count; i++)
            {
                var l = _lights[i];
                if (_bakes[i] != null && System.Math.Abs(l.X / WorldArt.Px - cx) <= l.R / WorldArt.Px + 3f && System.Math.Abs(l.Y / WorldArt.Px - cy) <= l.R / WorldArt.Px + 5f) _stale[i] = true;   // (was: dropped at once, so a firefight that chipped cover rebaked the lamps every frame)
            }
        }

        /// <summary>The summed light from every placed light, over map pixels (gx0, gy0)..(gx0 + rw, gy0 + rh), one sample per `step` pixels (the arrays are
        /// (rw / step) wide), colour-weighted; false when none reaches the view. Lights not baked yet join the field a frame at a time.</summary>
        public static bool Field(int gx0, int gy0, int rw, int rh, int step, float time, out float[] lr, out float[] lg, out float[] lb)
        {
            lr = lg = lb = null;
            if (!Enabled || Failed || !Ensure() || _lights.Count == 0) return false;
            int n = (rw / step) * (rh / step);
            if (_lr == null || _lr.Length < n) { _lr = new float[n]; _lg = new float[n]; _lb = new float[n]; }
            bool any = false; int baked = 0;
            for (int i = 0; i < _lights.Count; i++)
            {
                var l = _lights[i];
                if (l.X + l.R < gx0 || l.X - l.R > gx0 + rw || l.Y + l.R < gy0 || l.Y - l.R - l.H > gy0 + rh) continue;   // out of view (the head hangs above the foot)
                if (_bakes[i] == null || _stale[i] && Time.time - _bakedAt[i] > RebakeEvery) { if (baked++ >= 1) { if (_bakes[i] == null) continue; } else { var sw0 = System.Diagnostics.Stopwatch.StartNew(); _bakes[i] = WorldArt.BakeLight(l); _bakedAt[i] = Time.time; _stale[i] = false; BakeMs += sw0.Elapsed.TotalMilliseconds; BakeN++; } }
                if (!any) { System.Array.Clear(_lr, 0, n); System.Array.Clear(_lg, 0, n); System.Array.Clear(_lb, 0, n); any = true; }
                var sw1 = System.Diagnostics.Stopwatch.StartNew(); WorldArt.AddLight(_bakes[i], time, _lr, _lg, _lb, gx0, gy0, rw, rh, step); AddMs += sw1.Elapsed.TotalMilliseconds; LampN++;
            }
            if (any) { lr = _lr; lg = _lg; lb = _lb; }
            return any;
        }
    }
}
