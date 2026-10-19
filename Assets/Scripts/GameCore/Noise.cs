// Batch 47: gunfire and explosions are heard. Before this nothing alerted anyone but sight (TurnLoop vision check),
// so you could fight one squad with another standing 15 tiles away behind a wall. Now every NPC on the map within
// earshot who isn't already fighting walks over to look (its wander target becomes the noise); whether it then joins
// in is down to the usual vision check and NpcFights. Suppressed weapons carry a short way; rain and storms muffle.
namespace GameCore
{
    public static class Noise
    {
        public const int Shot = 20, Suppressed = 6, Blast = 30;

        public static void Make(int x, int y, int radius, ICombatant source)
        {
            var w = Atmosphere.Current;
            radius = (int)(radius * (w == Weather.Storm ? 0.6f : w == Weather.Rain ? 0.8f : 1f));
            if (source is Player p) Contact.Heard(x, y, radius, p);   // Batch 62: hunters hear where you are
            var spot = TerrainMap.FindNearestEnterable(x, y);
            foreach (var e in EncounterState.Enemies)
            {
                if (e == source || !e.IsAlive() || e.IsAggroed || System.Math.Max(System.Math.Abs(e.X - x), System.Math.Abs(e.Y - y)) > radius) continue;
                if (SquadManager.LeavingExit(e) != null) continue;      // already on its way out
                e.PatrolTarget = spot;
            }
        }
    }
}
