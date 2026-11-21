// Boss: the Grinder (Tier 5, Quarry Basin: the Pit Floor; the class keeps its old name, ExcavatorBoss, and the flag "excavator_done", for saves). REDONE 2026-11-21 from Yan's own ChatGPT drawing (boss_concepts/gpt/grinder_native.png):
// the Excavator (art style rejected) is replaced by a huge blind segmented worm that ate a quarry crew and their digger: a toothed round mouth, rusted spikes, boots and hands between its rings. Side on. Pixel Lab clips (BossArt key "grinder").
// The fight is Boss.cs with effects of its own:
//  - the Bite (red: a long lunge of the head; dive or get outside its reach), the Burrow Rush (red: it drives straight at you, 8 tiles);
//  - the Dig-Slam (red): it rears and crashes down on where you stood when it rose: everyone within SlamRadius of that spot is hit and knocked down;
//  - the Scoop (red): it spits gravel that lands on three spots round where you stood: each RockRadius wide;
//  - phase 2 (it goes berserk): the Double Bite.
// It finds you when a player comes near the Pit Floor (a landmark of the Tier 5 map), once. Co-op: the nearest player; the slam and the gravel catch everyone in them. Host-side only.
// NOT built from the brief yet: the weak points (the exposed crew), burrowing under the ground.
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class ExcavatorBoss
    {
        public const string Lair = "Pit Floor", Done = "excavator_done", Trophy = "Grinder Tooth";
        public const float WakeRange = 14f, SlamRadius = 2.0f, RockRadius = 1.2f;

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "The Grinder", Title = "THE SHIFT NEVER ENDED", Weapon = "ExcavatorBucket", Armor = null, Hp = 11000, Faction = "Mutants", Rank = "Master", DoneFlag = Done, Vanishes = false,
            Art = "grinder", Creature = true, StepSeconds = 0.55f, Recover = 1.0f, Phase2Line = "The rings split along their seams. What is left of the crew starts to scream.",
            Moves = new[]
            {
                new Boss.Move { Name = "Bite", Wind = 1.1f, Danger = true, MaxDist = 3f, Mult = 1.3f, Weight = 3, Poses = new[] { 1 }, Open = 1.6f, Clip = "bite" },
                new Boss.Move { Name = "Dig-Slam", Wind = 1.2f, Danger = true, MaxDist = 6f, Mult = 1.6f, Effects = new[] { "digslam" }, Weight = 2, Open = 1.8f, Clip = "slam" },
                new Boss.Move { Name = "Scoop", Wind = 1.3f, Danger = true, MaxDist = 10f, Mult = 0.9f, Effects = new[] { "rocks" }, Weight = 2, Open = 1.8f, Clip = "spit" },
                new Boss.Move { Name = "Burrow Rush", Wind = 1.0f, Danger = true, Lunge = 8f, Mult = 1.5f, Weight = 2, Open = 2.2f, Clip = "walk" },
                new Boss.Move { Name = "Double Bite", Wind = 0.9f, Hits = 2, Gap = 0.9f, MaxDist = 3f, Mult = 1.1f, RedHits = new[] { true, true }, Weight = 3, Poses = new[] { 1, 1 }, Open = 2.0f, Phase2Only = true, Clip = "bite" },
            },
        };
        static ExcavatorBoss() { Def.Defeated = Defeated; }

        static bool _spawnedHere; static string _map;
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; }

        public static void Tick(float dt)
        {
            if (Net.Mode == Net.Role.Client || Story.Has(Done) || Player.Current == null) return;
            if (_map != TerrainMap.ZoneId) { _map = TerrainMap.ZoneId; _spawnedHere = false; }
            var l = TerrainMap.Landmarks?.FirstOrDefault(m => m.Name == Lair); if (l == null) return;
            Boss.Fights.RemoveAll(f => f.Def == Def && !f.Finished && !EncounterState.Enemies.Contains(f.Unit));
            if (_spawnedHere || Boss.Fights.Any(f => f.Def == Def && !f.Gone)) return;
            if (!Player.All.Any(p => p.IsAlive() && Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)) <= WakeRange)) return;
            var (x, y) = TerrainMap.FindNearestEnterable(l.X, l.Y);
            Spawn(x, y, true); _spawnedHere = true;
        }

        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Def, x, y); Rt.SetOffset(f.Unit, 0f, 0f);
            f.NextMoveAt = Time.time + (intro ? 3.0f : 1.8f);
            if (intro)
            {
                GameLog.Event("Down on the pit floor the gravel starts to slide. Something the width of a truck rises out of it, ringed with rusted teeth, and there are boots and hands between its rings that are still moving.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.4f); Sfx.Play("diesel", TacticalGridView.GridToWorld(x, y), 1f, 0f);
            }
            return f;
        }

        static void HitAround(Boss.Fight f, Boss.Move m, int hi, float cx, float cy, float radius, float knock)
        {
            var h = f.Unit; if (h.EquippedWeapon == null) return;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                var (qx, qy) = Ballistics.Pos(q); float dx = qx - cx, dy = qy - cy; if (dx * dx + dy * dy > radius * radius) continue;
                if (Rt.Of(q).Diving || Rt.Untouchable(q)) { CombatFx.Push("dodge", q, h, null).Now = true; continue; }
                Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, q, Boss.MultOf(m, hi)); } finally { Parry.Unparryable = false; }
                if (knock > 0f) Rt.Knockdown(q, knock);
            }
        }

        /// <summary>The Dig-Slam's blow: the bucket lands on the spot its target stood on when it rose (Fight.AimX/AimY).</summary>
        public static void DigSlam(Boss.Fight f, Boss.Move m, int hi)
        {
            CombatFx.PxAt("small_explosion", f.AimX, f.AimY); Sfx.Play("bucket_slam", TacticalGridView.GridToWorld(f.AimX, f.AimY), 1f, 0.03f); CombatFx.Push("stagger", f.Unit, null, null).Now = true; Boss.HitStopPublic(0.06f);
            HitAround(f, m, hi, f.AimX + 0.5f, f.AimY + 0.5f, SlamRadius, 0.8f);
        }

        /// <summary>The Scoop's blow: rubble comes down on three spots: where the target stood, and two tiles either side of it.</summary>
        public static void Rocks(Boss.Fight f, Boss.Move m, int hi)
        {
            var h = f.Unit; float dx = f.AimX - h.X, dy = f.AimY - h.Y, l = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy)); float sx = -dy / l * 2f, sy = dx / l * 2f;
            foreach (var (ox, oy) in new[] { (0f, 0f), (sx, sy), (-sx, -sy) })
            {
                float cx = f.AimX + 0.5f + ox, cy = f.AimY + 0.5f + oy;
                CombatFx.PxAt("blunt_hit", (int)cx, (int)cy); HitAround(f, m, hi, cx, cy, RockRadius, 0f);
            }
            Sfx.Play("explosion_0", TacticalGridView.GridToWorld(f.AimX, f.AimY), 0.6f, 0.05f, 1.3f);
        }

        static void Defeated(Player p, HumanEnemy h)
        {
            h.Inventory.Items.Add(new InventoryItem { Id = "grinder_tooth", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 2f, Price = 3600 });
            Story.Journal("The Grinder: a worm the width of a truck ate a quarry crew and their digger, and wore them. It is down on the Quarry Basin's pit floor. You kept one of its rusted teeth.");
            GameLog.Event("Its rings go slack one by one. The hands between them open, and close, and stop. The gravel settles over what is left.");
        }
    }
}
