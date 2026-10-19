// Blast damage shared by explosive barrels now (and frag grenades later - same rules, file 01):
// everyone within the radius takes physical damage (armor Phys-DR applies, cover doesn't, Anom-DR
// never does) and rolls Agility DC 13 - success = half damage but knocked Prone. The blast also
// wrecks the terrain: light cover, doors and barrels in it are destroyed (barrels chain), hard
// cover loses 3 Integrity.
// ponytail: radius is a ruling (the docs only say "everyone in the blast") - 1 tile = the 3x3
// around the impact; bump BarrelRadius if barrels feel weak.
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Explosions
    {
        public const int BarrelRadius = 1;
        public const int BarrelDamage = 400;
        public const int SaveDc = 13;

        public const int ThrowRadius = 1;   // 3x3 (file 01)
        public const int ThrowDc = 12;

        /// <summary>Resolves a thrown grenade: accuracy roll, scatter on a miss, then the effect.</summary>
        public static void Throw(ICombatant thrower, ConsumableDef def, int x, int y)
        {
            int roll = Random.Range(1, 21) + (int)thrower.AgilityMod;
            string who = thrower is Player ? "You" : thrower.Name;
            if (roll < ThrowDc)
            {
                int dist = Random.Range(1, 5);
                int dir = Random.Range(0, 8);                                  // 0 = north, clockwise
                int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 }, dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
                x = Mathf.Clamp(x + dx[dir] * dist, 0, TerrainMap.Width - 1);
                y = Mathf.Clamp(y + dy[dir] * dist, 0, TerrainMap.Height - 1);
                GameLog.Write($"{who} throw{(thrower is Player ? "" : "s")} the {def.Name} wide (Agility {roll} vs {ThrowDc}) - it lands at ({x}, {y}).");
            }
            else GameLog.Write($"{who} throw{(thrower is Player ? "" : "s")} the {def.Name} right on target (Agility {roll} vs {ThrowDc}).");

            switch (def.Throwable)
            {
                case "frag": Blast(x, y, ThrowRadius, 400, thrower, "frag blast"); break;
                case "smoke": TerrainMap.AddSmoke(x, y, ThrowRadius, 3); GameLog.Write("Thick smoke blocks sight through the area."); break;
                case "flash": Flash(x, y, ThrowRadius); break;
            }
        }

        /// <summary>Flashbang: everyone in the blast rolls Agility DC 13 or loses 2 AP at the start
        /// of their next turn. No damage.</summary>
        public static void Flash(int cx, int cy, int radius)
        {
            var units = new List<ICombatant>();
            if (Player.Current != null && Player.Current.IsAlive()) units.Add(Player.Current);
            foreach (var e in EncounterState.Enemies) if (e.IsAlive()) units.Add(e);
            foreach (var u in units)
            {
                if (System.Math.Max(System.Math.Abs(u.X - cx), System.Math.Abs(u.Y - cy)) > radius) continue;
                int roll = Random.Range(1, 21) + (int)u.AgilityMod;
                string who = u is Player ? "Player" : u.Name;
                if (roll >= SaveDc) { GameLog.Write($"{who} shields their eyes from the flash (Agility {roll})."); continue; }
                u.PendingApDrain += 2;
                GameLog.Write($"{who} is blinded by the flash (Agility {roll} vs {SaveDc}): -2 AP next turn.");
            }
        }

        public static void Blast(int cx, int cy, int radius, int damage, ICombatant attacker, string label)
        {
            Sfx.Play("explosion", TacticalGridView.GridToWorld(cx, cy), 1f, 0.08f);
            Noise.Make(cx, cy, Noise.Blast, attacker);   // Batch 47
            var victims = new List<ICombatant>();
            if (Player.Current != null && Player.Current.IsAlive()) victims.Add(Player.Current);
            foreach (var e in EncounterState.Enemies) if (e.IsAlive()) victims.Add(e);

            var combat = GameServices.RequireCombat();
            foreach (var v in victims)
            {
                if (System.Math.Max(System.Math.Abs(v.X - cx), System.Math.Abs(v.Y - cy)) > radius) continue;
                int roll = Random.Range(1, 21) + (int)v.AgilityMod;
                bool saved = roll >= SaveDc;
                float raw = saved ? damage / 2f : damage;
                int dealt = (int)combat.CalculateDamage(raw, "Standard", v.PhysDR, 0f);
                v.Hp = System.Math.Max(0, v.Hp - dealt);
                if (v is Mutant m && !(v is HumanEnemy)) m.DamageTakenThisRound += dealt;
                string who = v is Player ? "Player" : v.Name;
                if (saved)
                {
                    v.StatusEffects.Prone = true;
                    GameLog.Write($"{who} dives clear of the {label} (Agility {roll} vs {SaveDc}) - {dealt} damage, knocked prone.");
                }
                else GameLog.Write($"{who} is caught in the {label} (Agility {roll} vs {SaveDc}) - {dealt} damage.");
                CombatRules.CheckWound(v, dealt, false);   // "Can cause Wounds" (file 01)
                if (!v.IsAlive())
                {
                    var k = CombatFx.Push("kill", null, v);   // Batch 24: blown outward from the blast, in pieces
                    k.FromX = cx; k.FromY = cy; k.Killed = true; k.Overkill = 99;
                    Weapon.OnTargetKilled(attacker, v);
                }
            }

            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    var t = TerrainMap.GetTile(x, y);
                    int max = TileRules.MaxIntegrity(t);
                    if (max <= 0) continue;
                    TerrainMap.DamageTile(x, y, t.Type == TileType.HardCover ? 3 : max, attacker);
                }
            Props.BlastShove(cx, cy, radius);   // Batch 103
        }
    }
}
