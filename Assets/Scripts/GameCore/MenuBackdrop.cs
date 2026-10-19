// Batch 123 (Yan: "a main menu screen of someplace in the map showcasing the passage of time mechanic, so if you stay in
// the main menu long enough you see it turn to night or raining"): behind the menu, Bonfire Village by its fire, drawn
// by the real map view (TacticalGridView backdrop mode) under the real sky (Atmosphere, AtmosphereView). Its clock runs
// fast - a game day in three minutes - and the weather drifts every two game hours as it does in play. Nothing else
// runs: no squads, no A-Life, no save. The state it uses is the boot-time MacroState, which New Game and Load replace.
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class MenuBackdrop
    {
        public const string Region = "bonfire_village";
        public const int FireX = 79, FireY = 166;          // the camp's bonfire (TierMaps, Bonfire Village)
        public const float GameMinutesPerSecond = 8f;      // 24 h in 3 real minutes
        public const float StartHour = 16.5f;              // late afternoon: dusk, then night, come round soon

        public static bool Ready { get; private set; }

        /// <summary>Loads the village's map into the boot-time state (once).</summary>
        public static void Prepare()
        {
            var m = MacroState.Current;
            m.CurrentZoneId = Region;
            m.Hour = StartHour;
            Atmosphere.Current = Weather.Clear;
            TerrainMap.BuildTerrainForZone(Region);
            Ready = true;
        }

        public static void Reset() => Ready = false;

        /// <summary>The menu's clock: `seconds` of real time. The weather drifts every two game hours (Atmosphere.Drift).</summary>
        public static void Advance(float seconds)
        {
            var m = MacroState.Current;
            float before = m.Hour;
            m.Hour += seconds * GameMinutesPerSecond / 60f;
            if (m.Hour >= 24f) { m.Hour -= 24f; m.Day++; }
            if (Mathf.Floor(before / 2f) != Mathf.Floor(m.Hour / 2f) || m.Hour < before) Atmosphere.Drift();
        }

        public static string Label() => $"DAY {MacroState.Current.Day}  ·  {Atmosphere.Clock()}  ·  {Atmosphere.Label(Atmosphere.Current)}";
    }
}
