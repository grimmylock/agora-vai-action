// Batch 39: factions fight each other on your map. Yan: "the world needs to be alive by itself without
// the player". Until now the enemy AI only ever looked for the player, so hostile squads on the same map
// ignored each other. Now an NPC that isn't fighting you closes in on the nearest hostile NPC it can see
// and attacks it, one beat at a time: a beat is each tile you walk (TurnLoop.TickAwareness) and each enemy
// phase (TurnLoop.PlayerEndTurn). Kills go through the usual death hooks (loot stays on the body; no rep or
// quest credit for you). Only A-Life squad members take part, so quest targets, story bosses and named
// mutants are never killed by someone else.
using System.Linq;
using GameData;

namespace GameCore
{
    public static class NpcFights
    {
        /// <summary>One beat for an NPC that isn't fighting you. True if it fought (so it doesn't wander).</summary>
        public static bool Act(Mutant u, IGridService grid)
        {
            if (!InSquad(u) || Out(u)) return false;
            var foe = Foe(u, grid);
            if (foe == null) return false;
            u.PatrolTarget = null;
            u.Ap = System.Math.Max(u.Ap, 2); u.Mp = System.Math.Max(u.Mp, TurnLoop.WalkMp);   // the same small top-up the wander gets
            var h = u as HumanEnemy;
            int range = h?.EquippedWeapon != null && h.EquippedWeapon.RangeTiles > 0 ? h.EquippedWeapon.RangeTiles : 1;
            bool canHit = CombatantUtil.Distance(u, foe) <= range && grid.CheckLineOfSight(u.X, u.Y, foe.X, foe.Y);
            if (!canHit)
            {
                for (int i = 0; i < 2 && CombatantUtil.Distance(u, foe) > range; i++) if (!u.MoveToward(foe.X, foe.Y)) break;
                return true;
            }
            u.FaceToward(foe.X, foe.Y);   // Batch 128: turns to the fight (sprite and gun follow Facing)
            if (h != null) h.Attack(foe, "Single");
            else u.Attack(foe);
            return true;
        }

        /// <summary>The nearest hostile squad member this unit can see (either side's hostility counts).</summary>
        public static Mutant Foe(Mutant u, IGridService grid) =>
            EncounterState.Enemies
                .Where(o => o != u && o.IsAlive() && !Out(o) && InSquad(o) && Hostile(u, o)
                    && CombatantUtil.Distance(u, o) <= u.VisionRange * Atmosphere.SightFactor()
                    && grid.CheckLineOfSight(u.X, u.Y, o.X, o.Y))
                .OrderBy(o => CombatantUtil.Distance(u, o))
                .FirstOrDefault();

        static bool Hostile(Mutant a, Mutant b) => FactionDatabase.AreHostile(a.Faction, b.Faction) || FactionDatabase.AreHostile(b.Faction, a.Faction);
        static bool Out(Mutant u) => u.Named || (u is HumanEnemy h && (h.IsFleeing() || h.Morale == "Surrendered"));
        static bool InSquad(Mutant u) => MacroState.Current.OfflineSquads.Any(s => s.QuestId == null && s.Members.Contains(u));   // Batch 44: quest squads wait for you
    }
}
