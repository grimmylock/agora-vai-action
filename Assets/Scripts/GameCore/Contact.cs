// Batch 62: breaking contact. Before this every enemy in a fight knew where you were each round, through walls and
// at any range, so a fight only ended when one side was dead or ran. Now they hunt what they last saw:
// - anyone who sees you (or hears you shoot) tells the rest; out of sight, they walk to where you were and fan out;
// - pack mutants follow your real trail for a few rounds; a bleeding player is followed exactly until bandaged;
// - Raiders give up after 3 rounds, Garrison, Sentinels and Obelisk after 8, everyone else after 5;
// - contact breaks when nobody has seen you for 3 rounds and you're 15+ tiles from every hunter. The fight ends
//   and whoever gave up stays on watch for an hour, seeing half again as far.
using System.Linq;

namespace GameCore
{
    public static class Contact
    {
        public const int BreakRounds = 3, BreakDistance = 15, PackTrailRounds = 3;

        public static (int x, int y) LastSeen;
        public static int UnseenRounds;
        static bool _seen;

        static float Now => MacroState.Current.Day * 24f + MacroState.Current.Hour;

        public static void Spot(Player p) { LastSeen = (p.X, p.Y); UnseenRounds = 0; _seen = true; }

        /// <summary>Batch 64: you opened fire on someone who hadn't seen you - they know where the shot came from, not where you go next.</summary>
        public static void ShotFrom(Player p) { LastSeen = (p.X, p.Y); UnseenRounds = 0; }

        /// <summary>A hunter looking for you sees twice its idle range, cut by night and weather (and halved if you sneak).</summary>
        public static bool Sees(Mutant e, Player p) =>
            CombatantUtil.Distance(e, p) <= e.VisionRange * 2 * Atmosphere.SightFactor() * LightMap.Exposure(p) * (p.Sneaking ? 0.5f : 1f)
            && Mutual(e, p);

        /// <summary>Line of sight both ways: a tile line from A to B and the one from B to A can pass different sides of a corner;
        /// the enemy only gets you when you'd get him too (the player's own view traces from the player).</summary>
        public static bool Mutual(ICombatant e, Player p)
        {
            var g = GameServices.RequireGrid();
            return g.CheckLineOfSight(e.X, e.Y, p.X, p.Y) && g.CheckLineOfSight(p.X, p.Y, e.X, e.Y);
        }

        public const float RearSight = 3f;

        /// <summary>Batch 66: how far an enemy that hasn't spotted you sees you - its range (longer on watch), cut by
        /// night and weather, halved if you sneak, and only 3 tiles outside the 120-degree cone it's facing.</summary>
        public static float IdleSight(Mutant e, Player p)
        {
            float sight = e.VisionRange * SightMult(e) * Atmosphere.SightFactor() * LightMap.Exposure(p) * (p.Sneaking ? 0.5f : 1f);
            return InFront(e, p.X, p.Y) ? sight : System.Math.Min(sight, RearSight);
        }

        /// <summary>Batch 127c (Yan: "a detection system for enemies based on line of sight, depending on which way they're moving"):
        /// spotting takes a moment. Each look (a game minute exploring, a round in a fight) an enemy that can see you fills its
        /// Alert: 0.1-0.5 by how close you are inside its sight, at once within 3 tiles. While it fills, the enemy stops and turns
        /// to look (so it sees you in full); out of its sight it drains. True = spotted now, or nothing to notice (carry on).</summary>
        public static bool Notice(Mutant e, Player p, bool sees, int dist, float sight)
        {
            if (!sees) { e.Alert = System.Math.Max(0f, e.Alert - 0.05f); return e.Alert <= 0f; }
            if (e.Alert <= 0f && dist > TurnLoop.MutualSpotExempt) Banter.Notice(e);   // Batch 129: "Huh?"
            e.Alert = dist <= TurnLoop.MutualSpotExempt ? 1f : System.Math.Min(1f, e.Alert + 0.1f + 0.4f * (1f - dist / System.Math.Max(1f, sight)));
            if (e.Alert >= 1f) return true;
            e.FaceToward(p.X, p.Y);
            return false;
        }

        public static bool InFront(Mutant e, int x, int y)
        {
            float vx = x - e.X, vy = y - e.Y, fx = e.Facing.x, fy = e.Facing.y;
            float vl = (float)System.Math.Sqrt(vx * vx + vy * vy), fl = (float)System.Math.Sqrt(fx * fx + fy * fy);
            return vl == 0 || fl == 0 || (vx * fx + vy * fy) / (vl * fl) >= 0.5f;
        }

        /// <summary>Does this enemy know where you are right now? Seeing you (or a blood trail) spots you for everyone.</summary>
        public static bool Knows(Mutant e, Player p)
        {
            if (Sees(e, p) || p.StatusEffects.Bleeding || p.StatusEffects.ToxicBleed) Spot(p);
            if (_seen) return true;
            var b = e.Db?.Behavior;
            return !(e is HumanEnemy) && (b == "pack" || b == "swarm") && UnseenRounds < PackTrailRounds;
        }

        public static int SearchRounds(Mutant e) => !(e is HumanEnemy h) ? 4
            : h.FactionName == "Raiders" ? 3
            : h.FactionName == "Garrison" || h.FactionName == "Sentinels" || h.FactionName == "Obelisk" ? 8 : 5;

        /// <summary>Idle vision, longer for an hour after losing you.</summary>
        public static float SightMult(Mutant e) => e.AlertUntil > Now ? 1.5f : 1f;

        /// <summary>Your gunfire or a blast: any hunter within earshot knows where you are.</summary>
        public static void Heard(int x, int y, int radius, Player p)
        {
            if (EncounterState.InCombat && EncounterState.Enemies.Exists(e => e.IsAggroed && e.IsAlive()
                && System.Math.Max(System.Math.Abs(e.X - x), System.Math.Abs(e.Y - y)) <= radius)) Spot(p);
        }

        /// <summary>End of the enemy round: count it, send searchers home, maybe end the fight.</summary>
        public static void EndRound(Player p)
        {
            if (!_seen) UnseenRounds++;
            _seen = false;
            if (UnseenRounds == 0) return;
            var hunters = EncounterState.Enemies.Where(e => EncounterState.IsActive(e) && e.IsAggroed).ToList();
            if (hunters.Count == 0) return;
            bool broken = UnseenRounds >= BreakRounds && hunters.All(e => CombatantUtil.Distance(e, p) >= BreakDistance);
            foreach (var e in hunters)
                if (broken || UnseenRounds > SearchRounds(e))
                {
                    e.IsAggroed = false;
                    e.AlertUntil = Now + 1f;
                    e.PatrolTarget = LastSeen;          // they go have a look
                    GameLog.Write($"{e.Name} gives up the hunt.");
                }
            if (EncounterState.AnyActive()) return;
            EncounterState.InCombat = false;
            EncounterState.ResetFight();
            GameLog.Write("[ALERT] You've broken contact. They're still out there, watching.");
        }
    }
}
