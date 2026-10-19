// Days (file 10 §1): a new Day starts when the player rests (Safe Zone or at a campfire) or after
// 3 zone crossings without resting. Each new Day, established traders restock (file 07).
// Zone Events and Faction Goals hook in here too (see WorldEvents once added).
using System.Linq;

namespace GameCore
{
    public static class WorldDays
    {
        public static void OnZoneCrossed()
        {
            // Batch 31: the road takes 8 hours (3 crossings ~ a day, as before); midnight starts the new Day
            var m = MacroState.Current;
            m.CrossingsSinceRest++;
            GameLog.Write("Time passes on the road.");
            Atmosphere.Advance(Atmosphere.CrossingHours * 60f);
        }

        /// <summary>Z: a full rest - in a Safe Zone, or next to a campfire, with nobody hostile
        /// around. Tiredness back to 10, and a new Day begins.</summary>
        public static void Rest()
        {
            var p = Player.Current;
            if (p == null || GameState.Current.CurrentTurn != Turn.Player) return;
            if (EncounterState.AnyActive() && EncounterState.Enemies.Any(e => EncounterState.IsActive(e) && e.IsAggroed))
            {
                GameLog.Write("You can't rest with enemies around.");
                return;
            }
            var zone = GameData.MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            bool fire = TerrainMap.GetTile(p.X, p.Y).Look == "campfire"
                || TerrainMap.GetAdjacentCoords(p.X, p.Y).Any(a => TerrainMap.GetTile(a.x, a.y).Look == "campfire");
            if (zone == null || !(zone.IsSafeZone || fire))
            {
                GameLog.Write("Nowhere safe to sleep here. Rest in a Safe Zone or by a campfire.");
                return;
            }
            p.Tiredness = 10;
            if (p.Hp < p.MaxHp) { p.HealPermanent(p.MaxHp); p.StatusEffects.Bleeding = false; }   // Batch 69: a night's sleep somewhere safe patches you up
            GameLog.Write("You sleep through the night and wake up patched up.");
            Atmosphere.Sleep();
            NewDay();
        }

        public static void NewDay()
        {
            var m = MacroState.Current;
            m.Day++;
            m.CrossingsSinceRest = 0;
            m.RolledShopStock.Clear();            // established traders restock
            GameLog.Write($"=== Day {m.Day} ===");
            WorldEvents.OnNewDay();
        }
    }
}
