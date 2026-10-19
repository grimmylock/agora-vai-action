// These interfaces describe the grid and combat functions that script.js's Player/Mutant/Weapon
// classes call into (attemptMove, checkLineOfSight, getMoveCost, rollAttack, calculateDamage,
// getCoverBonus, isMeleeAttack). They aren't implemented yet — that happens in the "grid/movement"
// and "combat resolution" steps of the port. Declaring the shape now means Player/Mutant/Weapon
// below can be ported with their real method bodies intact (not stubbed out), and will start
// working the moment real implementations are assigned to GameServices.Grid / GameServices.Combat.
using System.Collections.Generic;

namespace GameCore
{
    public interface IGridService
    {
        bool CanEnterTile(int x, int y);
        float GetMoveCost(ICombatant unit, int x, int y);
        /// <summary>Batch 102: the cost of one step from (fx,fy) to (tx,ty); a diagonal costs 1.5x and can't cut a wall corner (infinity).</summary>
        float StepCost(ICombatant unit, int fx, int fy, int tx, int ty);

        /// <summary>
        /// Ports attemptMove(unit, newX, newY): validates the tile is enterable and the unit has
        /// enough MP, then moves the unit and deducts MP cost. Returns whether the move succeeded.
        /// </summary>
        bool AttemptMove(ICombatant unit, int newX, int newY);

        bool CheckLineOfSight(int startX, int startY, int targetX, int targetY);

        /// <summary>Ports findPath(start, end, unit) — A* pathfinding for AI movement.</summary>
        List<(int x, int y)> FindPath(int startX, int startY, int endX, int endY, ICombatant unit);
    }

    public struct AttackRollResult
    {
        public bool Hit;
        public int ChosenRoll;      // the d20 result actually used (post advantage/disadvantage)
        public bool Jammed;
        public bool NoAmmo;
        public bool NoLineOfSight;
    }

    public interface ICombatResolver
    {
        /// <summary>Ports calculateDamage(baseDamage, ammoType, physDR, anomDR).</summary>
        float CalculateDamage(float baseDamage, string ammoType, float physDR, float anomDR);

        /// <summary>Ports rollAttack(advantageState, modifier, targetAc). advantageState is "standard" | "advantage" | "disadvantage".</summary>
        AttackRollResult RollAttack(string advantageState, int modifier, int targetAc, string breakdown = null);

        /// <summary>Ports getCoverBonus(attacker, target, weapon) - now AC-based, see CoverUtil.</summary>
        CoverResult GetCover(ICombatant attacker, ICombatant target, Weapon weapon);

        /// <summary>Ports isMeleeAttack(attacker, target, weapon).</summary>
        bool IsMeleeAttack(ICombatant attacker, ICombatant target, Weapon weapon);
    }

    /// <summary>
    /// Simple service locator mirroring how script.js just called its grid/combat functions as
    /// globals. Assign Grid/Combat once those steps of the port exist. Calling into a Player/
    /// Mutant method that needs one of these before it's assigned throws a clear exception rather
    /// than silently doing nothing.
    /// </summary>
    /// <summary>
    /// Ports triggerAnomalyIfPresent(unit) from script.js, called by AttemptMove after a
    /// successful step. Unlike Grid/Combat this is optional: until the anomaly-spawning system
    /// is ported, movement simply doesn't trigger anomalies rather than throwing.
    /// </summary>
    public interface IAnomalyTrigger
    {
        void TriggerAnomalyIfPresent(ICombatant unit);
    }

    public static class GameServices
    {
        public static IGridService Grid;
        public static ICombatResolver Combat;
        public static IAnomalyTrigger AnomalyTrigger; // optional — left null until the anomaly step

        public static IGridService RequireGrid()
        {
            if (Grid == null)
                throw new System.InvalidOperationException("GameServices.Grid is not set yet — implemented in the grid/movement port step.");
            return Grid;
        }

        public static ICombatResolver RequireCombat()
        {
            if (Combat == null)
                throw new System.InvalidOperationException("GameServices.Combat is not set yet — implemented in the combat resolution port step.");
            return Combat;
        }
    }
}
