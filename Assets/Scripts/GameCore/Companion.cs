// Batch 69: a hired Wanderer who walks and fights beside you. Yan chose "Player tools" for early fights: a fresh
// rookie against a Raider gang of 4 is 2 shots against 8 every round; a partner makes it 4 against 8.
// Hired at a Wanderers trader (ShopUI). Lives in EncounterState.Enemies while on the map like any unit, but never
// aggroes on you: out of a fight it follows you and holds fire (so sneaking still works); in a fight it shoots
// whoever is fighting you, and enemies shoot it when it's the closer target. It crosses zones with you, saves
// with you, and when it dies it's gone - loot the body.
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Companion
    {
        public const int HireCost = 600;
        public static HumanEnemy Current;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        public static bool Is(ICombatant c) => c != null && c == Current;
        public static bool Alive => Current != null && Current.IsAlive();

        static readonly string[] Names = { "Lyosha", "Tomas", "Dima", "Kostya", "Yarek", "Pavlo", "Senka", "Borya" };

        public static void Hire(Player p)
        {
            if (Alive) { GameLog.Write($"{Current.Name} is already walking with you."); return; }
            if (p.Ru < HireCost) { GameLog.Write($"Hiring a Wanderer costs {HireCost} RU."); return; }
            string rank = CombatRules.RankIndex(p.Rank) >= CombatRules.RankIndex("Experienced") ? "Experienced" : "Rookie";
            var (w, a) = EncounterGeneration.PickFactionLoadout("Wanderers", rank, 1);
            p.Ru -= HireCost;
            Spawn(Names[UnityEngine.Random.Range(0, Names.Length)], rank, w, a, -1, p);
            GameLog.Write($"{Current.Name} ({rank}, {Current.EquippedWeapon?.Name ?? "bare hands"}) shoulders a pack: \"Lead the way.\"");
        }

        public static void Dismiss()
        {
            if (Current == null) return;
            GameLog.Write($"{Current.Name} heads back to camp.");
            EncounterState.Enemies.Remove(Current);
            Current = null;
        }

        static void Spawn(string name, string rank, WeaponDef w, ArmorDef a, int hp, Player p)
        {
            var spot = FreeSpotNear(p);
            Current = new HumanEnemy(name, spot.x, spot.y, "Wanderers", rank, w, a) { IsAggroed = false };
            if (hp > 0) Current.Hp = System.Math.Min(hp, Current.MaxHp);
            if (!EncounterState.Enemies.Contains(Current)) EncounterState.Enemies.Add(Current);
        }

        /// <summary>A new map: your partner arrives right beside you.</summary>
        public static void OnEnterZone(Player p)
        {
            if (!Alive) { Current = null; return; }
            var spot = FreeSpotNear(p);
            Current.X = spot.x; Current.Y = spot.y;
            Current.IsAggroed = false;
            if (!EncounterState.Enemies.Contains(Current)) EncounterState.Enemies.Add(Current);
        }

        static (int x, int y) FreeSpotNear(Player p)
        {
            foreach (var c in TerrainMap.GetAdjacentCoords(p.X, p.Y))
                if (TerrainMap.CanEnterTile(c.x, c.y) && !EncounterState.Enemies.Any(e => e.IsAlive() && e.X == c.x && e.Y == c.y)) return c;
            return (p.X, p.Y);
        }

        /// <summary>Its beat: each tile you walk, and each enemy phase (with a full turn's AP).</summary>
        public static void Act(Player p, IGridService grid, bool fullTurn)
        {
            var c = Current;
            if (c == null || !c.IsAlive()) return;
            c.IsAggroed = false;
            c.Mp = System.Math.Max(c.Mp, TurnLoop.WalkMp);
            var foe = EncounterState.InCombat
                ? EncounterState.Enemies.Where(o => o != c && o.IsAlive() && o.IsAggroed && !(o is HumanEnemy h && EnemyAI.IsOut(h))
                        && CombatantUtil.Distance(c, o) <= c.VisionRange * 2 && grid.CheckLineOfSight(c.X, c.Y, o.X, o.Y))
                    .OrderBy(o => CombatantUtil.Distance(c, o)).FirstOrDefault()
                : null;
            if (foe == null)
            {
                for (int i = 0; i < 2 && CombatantUtil.Distance(c, p) > 2; i++) if (!c.MoveToward(p.X, p.Y)) break;
                return;
            }
            if (fullTurn) c.Ap = System.Math.Max(c.Ap, 4);
            Engage(c, foe, grid);
        }

        /// <summary>An enemy phase: an enemy fighting you shoots your partner instead when it's the closer one it can see.
        /// True if it did (so it doesn't also take its turn against you).</summary>
        public static bool TakesFire(Mutant enemy, Player p, IGridService grid)
        {
            var c = Current;
            if (c == null || !c.IsAlive() || !enemy.IsAggroed || enemy == c) return false;
            if (!FactionDatabase.AreHostile(enemy.Faction, c.Faction)) return false;
            if (CombatantUtil.Distance(enemy, c) >= CombatantUtil.Distance(enemy, p) || !grid.CheckLineOfSight(enemy.X, enemy.Y, c.X, c.Y)) return false;
            if (enemy is HumanEnemy h && EnemyAI.IsOut(h)) return false;
            Engage(enemy, c, grid);
            return true;
        }

        // ponytail: plain shoot-or-close like NpcFights - no cover seeking or grenades for or against the partner.
        static void Engage(Mutant u, ICombatant foe, IGridService grid)
        {
            var h = u as HumanEnemy;
            int range = h?.EquippedWeapon != null && h.EquippedWeapon.RangeTiles > 0 ? h.EquippedWeapon.RangeTiles : 1;
            for (int i = 0; i < 2 && CombatantUtil.Distance(u, foe) > range; i++) if (!u.MoveToward(foe.X, foe.Y)) break;
            if (CombatantUtil.Distance(u, foe) > range || !grid.CheckLineOfSight(u.X, u.Y, foe.X, foe.Y)) return;
            u.Ap = System.Math.Max(u.Ap, 2);
            for (int shot = 0; shot < 2 && u.Ap >= 2 && foe.IsAlive(); shot++)
            {
                if (h != null) { if (h.EquippedWeapon != null && !h.EquippedWeapon.IsLoaded) h.EquippedWeapon.Reload(); h.Attack(foe, "Single"); }
                else u.Attack(foe);
            }
        }

        public static void OnKilled(ICombatant dead)
        {
            if (!Is(dead)) return;
            GameLog.Write($"[ALERT] {Current.Name} is down. Your partner is gone.");
            Current = null;
        }

        // ---- save / load (Player.SaveLoad) ----
        public static string Capture()
        {
            if (!Alive) return null;
            var c = Current;
            string wk = Find(WeaponDatabase.Categories, c.EquippedWeapon?.Db), ak = Find(ArmorDatabase.Tiers, c.EquippedArmor);
            return string.Join("|", c.Name, c.Rank, wk ?? "", ak ?? "", c.Hp);
        }

        public static void Restore(string s, Player p)
        {
            Current = null;
            if (string.IsNullOrEmpty(s)) return;
            var f = s.Split('|');
            if (f.Length < 5) return;
            WeaponDef w = null; ArmorDef a = null;
            var wk = f[2].Split('/'); var ak = f[3].Split('/');
            if (wk.Length == 2) w = WeaponDatabase.Get(wk[0], wk[1]);
            if (ak.Length == 2) a = ArmorDatabase.Get(ak[0], ak[1]);
            Spawn(f[0], f[1], w, a, int.TryParse(f[4], out var hp) ? hp : -1, p);
        }

        static string Find<T>(System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, T>> db, T item) where T : class
        {
            if (item == null) return null;
            foreach (var g in db) foreach (var kv in g.Value) if (kv.Value == item) return g.Key + "/" + kv.Key;
            return null;
        }
    }
}
