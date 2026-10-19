// Ported from script.js: attemptMove(), checkLineOfSight(), getMoveCost() (as the IGridService
// implementation). This is the concrete class you assign to GameServices.Grid once it's
// instantiated (e.g. from a bootstrap MonoBehaviour), which is what lights up Player.Move(),
// Mutant.MoveToward(), Weapon.Fire()'s line-of-sight check, etc. from the previous port step.
namespace GameCore
{
    public class TacticalGridSystem : IGridService
    {
        public bool CanEnterTile(int x, int y) => TerrainMap.CanEnterTile(x, y);

        /// <summary>Ported from getMoveCost(unit, x, y), extended with a real climb cost: moving
        /// to a higher tile costs +1 MP per point of elevation gained (descending is free).
        /// Difficult terrain costs 2 (file 00 §5); crossing cover costs 2, or 1 with Vault; a
        /// closed door costs 2 (1 to open it, 1 to step through).</summary>
        public float GetMoveCost(ICombatant unit, int x, int y) => StepCost(unit, unit.X, unit.Y, x, y);

        /// <summary>Batch 102 (Yan: free movement, diagonals): movement is paid by distance, so a diagonal step costs 1.5x
        /// (about its real length); squeezing diagonally between two blocked tiles' corners isn't a step at all.</summary>
        public const float Diagonal = 1.5f;
        public float StepCost(ICombatant unit, int fx, int fy, int tx, int ty)
        {
            bool diag = fx != tx && fy != ty;
            if (diag && (!CanEnterTile(tx, fy) || !CanEnterTile(fx, ty))) return float.PositiveInfinity;
            return BaseCost(unit, fx, fy, tx, ty) * (diag ? Diagonal : 1f);
        }

        private int BaseCost(ICombatant unit, int fx, int fy, int x, int y)
        {
            int baseCost;
            switch (TerrainMap.GetTile(x, y).Type)
            {
                case TileType.ScatterCover:
                case TileType.HardCover:
                    baseCost = CombatantUtil.HasPerk(unit, "Vault") ? 1 : 2; break;
                case TileType.Difficult:
                case TileType.Door:
                    baseCost = 2; break;
                default:
                    baseCost = 1; break;
            }
            int climb = System.Math.Max(0, TerrainMap.GetHeight(x, y) - TerrainMap.GetHeight(fx, fy));
            return (baseCost + climb) * (unit is Player sneak && sneak.Sneaking && EncounterState.InCombat ? 2 : 1);   // Batch 66: sneaking in a fight
        }

        /// <summary>Ported from attemptMove(unit, newX, newY).</summary>
        public bool AttemptMove(ICombatant unit, int newX, int newY)
        {
            bool isPlayer = unit is Player;
            string unitLabel = isPlayer ? "Player" : unit.Name;

            if (!CanEnterTile(newX, newY))
            {
                GameLog.Write($"{unitLabel} cannot move: blocked by solid obstacle.");
                return false;
            }

            if (TerrainMap.UnitAt(newX, newY, unit) != null)
                return false;                       // someone's standing there

            float cost = GetMoveCost(unit, newX, newY);
            if (float.IsInfinity(cost)) return false;   // a diagonal through a wall corner
            if (unit.Mp < cost)
            {
                GameLog.Write($"{unitLabel} cannot move: not enough MP to enter tile.");
                return false;
            }

            if (TerrainMap.GetTile(newX, newY).Type == TileType.Door)
            {
                TerrainMap.SetTileType(newX, newY, TileType.DoorOpen);
                Sfx.Play("door_open", TacticalGridView.GridToWorld(newX, newY), 0.8f);
                GameLog.Write($"{unitLabel} opens a door.");
            }

            if (unit is Mutant walker) walker.Facing = (newX - unit.X, newY - unit.Y);   // Batch 66: they look where they walk
            unit.X = newX;
            unit.Y = newY;
            CombatFx.Step(unit, newX, newY);
            unit.Mp -= cost;
            if (unit is Player mover) mover.MovedThisTurn = true;

            GameServices.AnomalyTrigger?.TriggerAnomalyIfPresent(unit);
            Perks.OnStep(unit);   // Batch 72: Overwatch fires on the first hostile step it sees
            return true;
        }

        /// <summary>Ported from checkLineOfSight(startX, startY, targetX, targetY), extended with
        /// real height-interpolated blocking (walls and closed doors block; cover never does): walk the Bresenham path and linearly interpolate
        /// elevation between the two endpoints' heights - a tile that pokes up above that
        /// interpolated sightline blocks the shot, same as a solid obstacle would.</summary>
        public bool CheckLineOfSight(int startX, int startY, int targetX, int targetY)
        {
            var path = Pathfinding.GetStraightLinePath(startX, startY, targetX, targetY);
            int startHeight = TerrainMap.GetHeight(startX, startY);
            int endHeight = TerrainMap.GetHeight(targetX, targetY);
            int steps = path.Count - 1;

            for (int i = 1; i < steps; i++)
            {
                var p = path[i];
                if (TerrainMap.BlocksSight(p.x, p.y))
                    return false;

                float t = steps > 0 ? (float)i / steps : 0f;
                float sightlineHeight = startHeight + (endHeight - startHeight) * t;
                // +0.5 so a tile level with the sightline doesn't self-block on rounding.
                if (TerrainMap.GetHeight(p.x, p.y) > sightlineHeight + 0.5f)
                    return false;
            }
            return true;
        }

        public System.Collections.Generic.List<(int x, int y)> FindPath(int startX, int startY, int endX, int endY, ICombatant unit)
            => Pathfinding.FindPath(startX, startY, endX, endY, unit);
    }
}
