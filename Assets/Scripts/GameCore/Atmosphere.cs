// Batch 31: time of day and weather (Yan: "time of day mechanic, fog, rain, morning, night, heavy rain").
// One clock (MacroState.Hour) and one weather state per visit. The three art directions all live
// here as moods: overcast Zone realism by day, golden decay at dusk, the dark-wood flashlight at night.
//   Clock: walking a tile / a combat round = a minute; a zone crossing = 8 hours on the road (so 3
//          crossings are about a day, as file 10 has it); sleeping wakes you at 6:30. Passing
//          midnight starts a new Day (WorldDays.NewDay).
//   Weather: rolled on every zone entry (zone-flavoured odds, fog likelier around dawn), drifts a
//          step every couple of game hours while you stay.
//   Look(): what it looks like right now - colour wash, darkness, fog, rain, lightning, how far you
//          see (Vision) and how far enemies see you (SightFactor). AtmosphereView draws it.
using UnityEngine;

namespace GameCore
{
    public enum Weather { Clear, Overcast, Fog, Rain, Storm }

    public struct AtmosphereLook
    {
        public Color Wash; public float WashA;     // colour laid over the world (overcast grey, golden, night blue)
        public float Night;                       // 0..1 darkness outside your light
        public float Fog;                         // 0..1 drifting fog layer
        public float Rain;                        // 0 none, 0.5 rain, 1 storm
        public bool Lightning;
        public float Vision;                      // tiles you see all around
        public float Cone;                        // flashlight reach in tiles (0 = no flashlight)
        public string Phase;                      // "NIGHT", "DAWN", "MORNING", "DAY", "DUSK"
        // Batch 35: the colour grade (ColorGrade puts it on a URP Volume), after concept.py's post()
        public float Saturation;                  // 1 = as drawn; never below 0.65 so faction colours stay readable
        public Color Filter;                      // multiplies every colour: warm at 18:00, cold in the rain
        public float Haze;                        // lifts the darks toward grey, in display terms (0.04 = concept A)
        public float Vignette;                    // URP vignette intensity
    }

    public static class Atmosphere
    {
        public const float MinutesPerStep = 1f, CrossingHours = 8f, WakeHour = 6.5f;

        static MacroState M => MacroState.Current;

        public static Weather Current
        {
            get => System.Enum.TryParse<Weather>(M.Weather, out var w) ? w : Weather.Overcast;
            set => M.Weather = value.ToString();
        }

        /// <summary>Time passes. Crossing midnight starts a new Day.</summary>
        public static void Advance(float minutes)
        {
            float before = M.Hour;
            M.Hour += minutes / 60f;
            int hours = (int)Mathf.Floor(M.Hour) - (int)Mathf.Floor(before);      // Batch 36: A-Life ticks once per game hour
            if (M.Hour >= 24f) { M.Hour -= 24f; WorldDays.NewDay(); }
            for (int i = 0; i < hours; i++) SquadManager.TickHour();
            // weather drifts every 2 game hours
            if (Mathf.Floor(before / 2f) != Mathf.Floor(M.Hour / 2f) || M.Hour < before) Drift();
        }

        public static void Sleep()
        {
            M.Hour = WakeHour;            // NewDay is started by the caller (WorldDays.Rest)
            Current = Random.value < 0.45f ? Weather.Fog : Weather.Overcast;     // misty mornings
        }

        /// <summary>A zone entry: fresh weather for the place, its odds by zone and time of day.</summary>
        public static void RollFor(string zoneId)
        {
            if (ZoneLayoutRecipes.IsInterior(zoneId)) return;                    // underground: weather doesn't matter
            float fog = 0.12f, rain = 0.18f, storm = 0.06f, clear = 0.34f;   // Batch 94: clear skies were 22%
            if (zoneId == "marshes" || zoneId == "black_valley") fog += 0.25f;
            if (zoneId == "dried_lake" || zoneId == "quarry_basin") { rain -= 0.1f; clear += 0.2f; }
            if (M.Hour >= 4.5f && M.Hour < 9f) fog += 0.25f;
            float r = Random.value * (fog + rain + storm + clear + 0.4f);
            Current = (r -= fog) < 0 ? Weather.Fog : (r -= rain) < 0 ? Weather.Rain : (r -= storm) < 0 ? Weather.Storm : (r -= clear) < 0 ? Weather.Clear : Weather.Overcast;
        }

        public static void Drift()
        {
            float r = Random.value;
            switch (Current)
            {
                case Weather.Clear: if (r < 0.3f) Current = Weather.Overcast; break;
                case Weather.Overcast: Current = r < 0.2f ? Weather.Rain : r < 0.5f ? Weather.Clear : Weather.Overcast; break;   // Batch 94: clears up more often
                case Weather.Fog: if (M.Hour > 9f && r < 0.6f || r < 0.25f) Current = Weather.Overcast; break;   // burns off in the morning
                case Weather.Rain: Current = r < 0.2f ? Weather.Storm : r < 0.5f ? Weather.Overcast : Weather.Rain; break;
                case Weather.Storm: if (r < 0.5f) Current = Weather.Rain; break;
            }
        }

        // ------------------------------------------------------------------ the look
        struct Key { public float H; public Color C; public float A, Night; public string Phase; public float Sat; public Color F; public float Haze, Vig; }
        static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
        // Batch 35: 18:00 was very orange (wash 255,168,72 at .17); the warmth now comes mostly from the grade's filter
        static readonly Key[] Day =
        {
            new Key { H = 0f,    C = C(12, 16, 32),    A = 0.30f, Night = 0.80f, Phase = "NIGHT",   Sat = 0.68f, F = new Color(0.88f, 0.92f, 1.04f), Haze = 0f,    Vig = 0.32f },
            new Key { H = 4.5f,  C = C(12, 16, 32),    A = 0.30f, Night = 0.78f, Phase = "NIGHT",   Sat = 0.68f, F = new Color(0.88f, 0.92f, 1.04f), Haze = 0f,    Vig = 0.32f },
            new Key { H = 5.5f,  C = C(70, 66, 92),    A = 0.30f, Night = 0.40f, Phase = "DAWN",    Sat = 0.72f, F = new Color(0.96f, 0.93f, 1.02f), Haze = 0.03f, Vig = 0.28f },
            new Key { H = 7f,    C = C(214, 196, 168), A = 0.12f, Night = 0f,    Phase = "MORNING", Sat = 0.84f, F = new Color(1.04f, 1.00f, 0.94f), Haze = 0.04f, Vig = 0.22f },
            new Key { H = 9.5f,  C = C(150, 152, 142), A = 0.08f, Night = 0f,    Phase = "DAY",     Sat = 0.82f, F = new Color(1.00f, 1.00f, 0.98f), Haze = 0.04f, Vig = 0.22f },
            new Key { H = 16f,   C = C(150, 152, 142), A = 0.08f, Night = 0f,    Phase = "DAY",     Sat = 0.82f, F = new Color(1.00f, 1.00f, 0.98f), Haze = 0.04f, Vig = 0.22f },
            new Key { H = 18f,   C = C(226, 172, 112), A = 0.11f, Night = 0f,    Phase = "DUSK",    Sat = 0.95f, F = new Color(1.10f, 1.02f, 0.88f), Haze = 0.06f, Vig = 0.20f },
            new Key { H = 19.5f, C = C(96, 58, 92),    A = 0.28f, Night = 0.35f, Phase = "DUSK",    Sat = 0.76f, F = new Color(0.94f, 0.90f, 1.00f), Haze = 0.02f, Vig = 0.30f },
            new Key { H = 21f,   C = C(12, 16, 32),    A = 0.30f, Night = 0.78f, Phase = "NIGHT",   Sat = 0.68f, F = new Color(0.88f, 0.92f, 1.04f), Haze = 0f,    Vig = 0.32f },
            new Key { H = 24f,   C = C(12, 16, 32),    A = 0.30f, Night = 0.80f, Phase = "NIGHT",   Sat = 0.68f, F = new Color(0.88f, 0.92f, 1.04f), Haze = 0f,    Vig = 0.32f },
        };

        public static AtmosphereLook Look()
        {
            if (!ZoneLayoutRecipes.IsUpstairs(M.CurrentZoneId)) return Look(M.Hour, Current, ZoneLayoutRecipes.IsInterior(M.CurrentZoneId));
            var l = Look(M.Hour, Weather.Overcast, false);   // Batch 120: upstairs in a building - the day's light through the windows, dimmer, no rain inside
            l.Night = Mathf.Max(l.Night, 0.3f); l.Vision = Mathf.Min(l.Vision, 14f);
            return l;
        }

        public static AtmosphereLook Look(float hour, Weather w, bool underground)
        {
            var l = new AtmosphereLook();
            if (underground)
            {
                // no sky: always dark beyond your light, a cold wash
                l.Wash = C(20, 26, 30); l.WashA = 0.18f; l.Night = 0.85f; l.Vision = 2.5f; l.Cone = 10f;   // Batch 92: darker, the flashlight and lamps do the seeing l.Phase = "UNDERGROUND";
                l.Saturation = 0.7f; l.Filter = new Color(0.92f, 0.98f, 1.0f); l.Vignette = 0.34f;
                return l;
            }
            hour = Mathf.Repeat(hour, 24f);
            int i = 0; while (i < Day.Length - 2 && hour >= Day[i + 1].H) i++;
            var a = Day[i]; var b = Day[i + 1];
            float t = Mathf.Clamp01((hour - a.H) / Mathf.Max(0.01f, b.H - a.H));
            l.Wash = Color.Lerp(a.C, b.C, t); l.WashA = Mathf.Lerp(a.A, b.A, t); l.Night = Mathf.Lerp(a.Night, b.Night, t);
            l.Phase = t < 0.5f ? a.Phase : b.Phase;
            l.Saturation = Mathf.Lerp(a.Sat, b.Sat, t); l.Filter = Color.Lerp(a.F, b.F, t); l.Haze = Mathf.Lerp(a.Haze, b.Haze, t); l.Vignette = Mathf.Lerp(a.Vig, b.Vig, t);
            l.Vision = Mathf.Lerp(20f, 8f, l.Night / 0.8f);   // Batch 65: was 13 to 5, before the zoom-out
            l.Cone = 11f * Mathf.Clamp01((l.Night - 0.15f) / 0.2f);   // Batch 67: the flashlight beam grows in with the dark instead of switching on
            switch (w)
            {
                case Weather.Overcast: l.Wash = Color.Lerp(l.Wash, C(120, 126, 126), 0.5f); l.WashA += 0.06f; l.Saturation *= 0.92f; l.Filter = Color.Lerp(l.Filter, new Color(0.97f, 1f, 1f), 0.5f); break;
                case Weather.Fog: l.Wash = Color.Lerp(l.Wash, C(150, 156, 150), 0.7f); l.WashA += 0.22f; l.Fog = 1f; l.Vision = Mathf.Min(l.Vision, 11f); l.Cone = Mathf.Min(l.Cone, 11f); l.Saturation *= 0.86f; l.Haze += 0.03f; l.Filter = Color.Lerp(l.Filter, Color.white, 0.6f); break;   // fog stays grey
                case Weather.Rain: l.Wash = Color.Lerp(l.Wash, C(70, 82, 96), 0.6f); l.WashA += 0.14f; l.Rain = 0.5f; l.Vision -= 2f; l.Saturation *= 0.86f; l.Filter *= new Color(0.86f, 0.89f, 0.96f); break;   // concept "rain": wet, cold, darker
                case Weather.Storm: l.Wash = Color.Lerp(l.Wash, C(38, 44, 60), 0.7f); l.WashA += 0.26f; l.Night = Mathf.Max(l.Night, 0.25f); l.Rain = 1f; l.Lightning = true; l.Vision -= 4f; l.Saturation *= 0.82f; l.Filter *= new Color(0.80f, 0.83f, 0.92f); l.Vignette += 0.06f; break;
            }
            l.Vision = Mathf.Max(6f, l.Vision);
            l.Saturation = Mathf.Max(0.65f, l.Saturation);                       // Yan: faction colours must stay readable
            return l;
        }

        /// <summary>Batch 67 (Yan: "time of day change needs to be gradual, same with weathers"): what's on screen eases
        /// toward the real look, so a weather change or an hour skipped (sleep, F10) fades over a few seconds
        /// instead of popping. k = 0..1, how far to move this frame.</summary>
        public const float FadeSeconds = 2.5f;
        public static AtmosphereLook Ease(AtmosphereLook a, AtmosphereLook b, float k)
        {
            float L(float x, float y) => Mathf.Lerp(x, y, k);
            b.Wash = Color.Lerp(a.Wash, b.Wash, k); b.WashA = L(a.WashA, b.WashA); b.Night = L(a.Night, b.Night);
            b.Fog = L(a.Fog, b.Fog); b.Rain = L(a.Rain, b.Rain); b.Lightning &= b.Rain > 0.75f;
            b.Vision = L(a.Vision, b.Vision); b.Cone = L(a.Cone, b.Cone);
            b.Saturation = L(a.Saturation, b.Saturation); b.Filter = Color.Lerp(a.Filter, b.Filter, k); b.Haze = L(a.Haze, b.Haze); b.Vignette = L(a.Vignette, b.Vignette);
            return b;
        }

        /// <summary>How far enemies see you, as a fraction of their usual range.</summary>
        public static float SightFactor()
        {
            var l = Look();
            return Mathf.Clamp(l.Vision / 20f, 0.35f, 1f);
        }

        public static string Clock() => $"{(int)M.Hour:00}:{(int)((M.Hour % 1f) * 60f):00}";
        public static string Label(Weather w) => w switch { Weather.Clear => "CLEAR", Weather.Overcast => "OVERCAST", Weather.Fog => "FOG", Weather.Rain => "RAIN", _ => "STORM" };
    }
}
