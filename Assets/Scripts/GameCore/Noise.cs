// Batch 47: gunfire and explosions are heard. Before this nothing alerted anyone but sight (TurnLoop vision check),
// so you could fight one squad with another standing 15 tiles away behind a wall. Now every NPC on the map within
// earshot who isn't already fighting walks over to look (its wander target becomes the noise); whether it then joins
// in is down to the usual vision check and NpcFights. Suppressed weapons carry a short way; rain and storms muffle.
using System.Linq;

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
                if (!(e is HumanEnemy)) continue;                     // mutants don't come to gunfire - to blood (Smell), when they like their odds
                if (SquadManager.LeavingExit(e) != null) continue;      // already on its way out
                e.PatrolTarget = spot;
            }
        }

        public const int SmellRange = 25;

        /// <summary>Yan: "make it so mutants only come over if someone dies (they smell the blood) and only if they think they can win
        /// the fight". A death at (x, y): each unaware pack within 25 tiles weighs itself (its members' HP) against everyone still
        /// standing within 10 tiles of the body (you included); if it's the stronger, it heads for the body.
        /// ponytail: HP against HP, no armor or guns counted - a pack will happily walk into a well-armed squad.</summary>
        public static void Smell(int x, int y)
        {
            var spot = TerrainMap.FindNearestEnterable(x, y);
            int D(ICombatant u, int ax, int ay) => System.Math.Max(System.Math.Abs(u.X - ax), System.Math.Abs(u.Y - ay));
            var standing = Gunplay.Bodies().Where(u => u.IsAlive() && !(u is Mutant m0 && !(m0 is HumanEnemy)) && D(u, x, y) <= 10).Sum(u => u.Hp);
            var noses = EncounterState.Enemies.Where(e => !(e is HumanEnemy) && e.IsAlive() && !e.IsAggroed && D(e, x, y) <= SmellRange).ToList();
            foreach (var e in noses)
            {
                int pack = noses.Where(o => D(o, e.X, e.Y) <= 6).Sum(o => o.Hp);
                if (pack > standing) e.PatrolTarget = spot;
            }
        }
    }
}
