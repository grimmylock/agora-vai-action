// Boss: the Specimen, grown (Tier 4, Deep Caverns: the Crystal Pool island; BOSS_CONCEPTS_V3.md 5, BOSS_ROSTER.md 5): the rematch with the brain that escaped Secret Lab Alpha (BrainBoss.Escaped).
// 2026-11-21 v2 (Yan: the first version "doesn't read as the Brain escaped, it's bigger and doesn't have any of the aspects the brain had like the big eye tentacle or the laser attacks"):
// it is drawn from the vat brain's own art (ArtSource/mutants_anim/bosses_v3/specimen_v2: the brain cut from the vat sheet, grown and freed by Pixel Lab, the giant eye behind it; BossArt key
// "specimen_v2") and it fights with the vat's attacks, now free to move:
//  - the Laser (red): the giant eye fires a beam at where you stood when it charged (a dive goes under it, a wall stops it); phase 2: the Sweep, a beam that swings across 70 degrees;
//  - the Orbs: a ring of the vat's psychic orbs (BrainBoss.Orbs: same drawing, same flight, cover and walls stop them), with an aimed bolt; more in phase 2;
//  - the Mind-Lash (red): its eyes flare: everyone it can see has their aim torn away and is dazed (break its line of sight);
//  - the Drift (red): it rushes through the air at you.
// It keeps its distance (Def.KeepAt) like the caster it is. It only comes if it escaped the lab. Co-op: the nearest player; the orbs, the beams and the mind-lash catch everyone. Host-side
// only (the snapshot carries the boss's clip, not its beams and orbs yet).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class SpecimenGrown
    {
        public const string Lair = "Crystal Pool", Zone = "deep_caverns", Done = "specimen_grown_done", Trophy = "Specimen Tissue";
        public const float WakeRange = 12f, MindRange = 11f, BeamReach = 14f, BeamHalfWidth = 0.55f, BeamFor = 0.7f, SweepFor = 1.3f, SweepArc = 70f, EyeHeight = 1.6f;
        public const int LaserDamage = 160, SweepHitsGap = 1;

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "The Specimen", Title = "IT REMEMBERS YOU", Weapon = "SpecimenLegs", Armor = null, Hp = 8000, Faction = "Mutants", Rank = "Master", DoneFlag = Done, Vanishes = false,
            Art = "specimen_v2", Creature = true, KeepAt = 5, StepSeconds = 0.3f, Recover = 1.0f, Phase2Line = "The giant eye opens all the way. Every eye on the brain opens with it.",
            Moves = new[]
            {
                new Boss.Move { Name = "Laser", Wind = 1.1f, Danger = true, MaxDist = BeamReach, Effects = new[] { "laser" }, Weight = 3, Open = 1.3f, Clip = "laser" },
                new Boss.Move { Name = "Orbs", Wind = 0.9f, MaxDist = 12f, Effects = new[] { "orbs" }, Weight = 3, Open = 1.0f, Clip = "orbs" },
                new Boss.Move { Name = "Mind-Lash", Wind = 1.0f, Danger = true, MaxDist = MindRange, Effects = new[] { "mind" }, Weight = 2, Open = 1.3f, Clip = "mind" },
                new Boss.Move { Name = "Drift", Wind = 0.8f, Danger = true, Lunge = 6f, Mult = 1.1f, Weight = 1, Open = 1.6f, Clip = "walk" },
                new Boss.Move { Name = "Sweep", Wind = 1.3f, Danger = true, MaxDist = BeamReach, Effects = new[] { "laser" }, Weight = 3, Open = 1.8f, Phase2Only = true, Clip = "laser" },
                new Boss.Move { Name = "Storm", Wind = 0.8f, Hits = 2, Gap = 0.9f, MaxDist = 12f, Effects = new[] { "orbs", "orbs" }, Weight = 2, Open = 1.4f, Phase2Only = true, Clip = "orbs" },
            },
        };
        static SpecimenGrown() { Def.Defeated = Defeated; }

        /// <summary>Its beams now (drawn by CombatFxView from Center), and the sweep in progress.</summary>
        public static readonly List<BrainBoss.Beam> Beams = new List<BrainBoss.Beam>();
        static float _sweepFrom, _sweepTo, _sweepT0 = -1f; static Boss.Fight _sweeping; static readonly Dictionary<Player, float> _hitAt = new Dictionary<Player, float>();
        public static Boss.Fight Current => Boss.Fights.FirstOrDefault(f => f.Def == Def && !f.Gone && !f.Mirror);
        public static bool Active => Current != null;
        /// <summary>The giant eye, where its beams start (tile space, y down).</summary>
        public static (float x, float y) Center(Boss.Fight f) { var (x, y) = Ballistics.Pos(f.Unit); return (x, y - EyeHeight); }

        static bool _spawnedHere;
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; Beams.Clear(); _sweeping = null; }

        public static void Tick(float dt)
        {
            var cur = Current;
            if (cur != null && BrainBoss.Current == null)                                                   // its orbs fly here (the vat's tick flies them while the vat is in the lab)
            {
                var players = Player.All.Where(p => p.IsAlive() && !Body.Down(p)).ToList();
                BrainBoss.FlyOrbs(dt, players); TickBeams(cur, players);
            }
            else if (cur == null) { Beams.Clear(); _sweeping = null; }
            if (Net.Mode == Net.Role.Client || TerrainMap.ZoneId != Zone || Story.Has(Done) || !Story.Has(BrainBoss.Escaped) || Player.Current == null) return;
            Boss.Fights.RemoveAll(f => f.Def == Def && !f.Finished && !EncounterState.Enemies.Contains(f.Unit));
            if (_spawnedHere || Boss.Fights.Any(f => f.Def == Def && !f.Gone)) return;
            var l = TerrainMap.Landmarks?.FirstOrDefault(m => m.Name == Lair); if (l == null) return;
            if (!Player.All.Any(p => p.IsAlive() && Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)) <= WakeRange)) return;
            var (x, y) = TerrainMap.FindNearestEnterable(l.X, l.Y);
            Spawn(x, y, true); _spawnedHere = true;
        }

        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Def, x, y); Rt.SetOffset(f.Unit, 0f, 0f);
            f.NextMoveAt = Time.time + (intro ? 2.8f : 1.6f);
            if (intro)
            {
                GameLog.Event("Above the glowing pool something hangs in the air: the brain from the lab's vat, grown, its violet eyes open, and behind it the giant eye you saw before it vanished. It remembers you.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.2f);
            }
            return f;
        }

        /// <summary>How far a beam from (x, y) at `deg` runs before a wall stops it (tiles, at most BeamReach).</summary>
        static float BeamLength(float x, float y, float deg)
        {
            float r = deg * Mathf.Deg2Rad, dx = Mathf.Cos(r), dy = Mathf.Sin(r);
            for (float t = 0.5f; t < BeamReach; t += 0.25f)
            {
                var tile = TerrainMap.GetTile(Mathf.FloorToInt(x + dx * t), Mathf.FloorToInt(y + dy * t));
                if (tile == null || tile.Type == TileType.Solid || tile.Type == TileType.Door) return t;
            }
            return BeamReach;
        }

        /// <summary>The Laser's blow: a beam from the giant eye at where its target stood when it charged (Fight.AimX/AimY); in phase 2 (the Sweep) it swings across SweepArc.</summary>
        public static void Laser(Boss.Fight f, Boss.Move m, int hi)
        {
            var (bx, by) = Ballistics.Pos(f.Unit); float deg = Mathf.Atan2(f.AimY + 0.5f - by, f.AimX + 0.5f - bx) * Mathf.Rad2Deg;   // the beam runs over the ground from its feet toward where the target stood (drawn from the eye)
            Sfx.Play("psi", TacticalGridView.GridToWorld(f.Unit.X, f.Unit.Y), 1f, 0.03f, 0.7f); _hitAt.Clear();
            if (m.Name == "Sweep") { _sweeping = f; _sweepT0 = Time.time; _sweepFrom = deg - SweepArc / 2f; _sweepTo = deg + SweepArc / 2f; Beams.Clear(); Beams.Add(new BrainBoss.Beam { Angle = _sweepFrom, Length = BeamLength(bx, by, _sweepFrom), Until = Time.time + SweepFor }); }
            else { Beams.Clear(); Beams.Add(new BrainBoss.Beam { Angle = deg, Length = BeamLength(bx, by, deg), Until = Time.time + BeamFor }); }
            TickBeams(f, Player.All.Where(p => p.IsAlive() && !Body.Down(p)).ToList());
        }

        /// <summary>The beams: the sweep turns; anyone within BeamHalfWidth of a live beam (over the ground, from the boss's feet) and not diving is burned, once a second at most.</summary>
        static void TickBeams(Boss.Fight f, List<Player> players)
        {
            float now = Time.time; var (bx, by) = Ballistics.Pos(f.Unit);
            if (_sweeping == f && _sweepT0 >= 0f && Beams.Count > 0)
            {
                float k = Mathf.Clamp01((now - _sweepT0) / SweepFor); float a = Mathf.Lerp(_sweepFrom, _sweepTo, k); Beams[0].Angle = a; Beams[0].Length = BeamLength(bx, by, a);
                if (k >= 1f) _sweeping = null;
            }
            Beams.RemoveAll(b => now > b.Until);
            foreach (var b in Beams)
            {
                float r = b.Angle * Mathf.Deg2Rad, dx = Mathf.Cos(r), dy = Mathf.Sin(r);
                foreach (var p in players)
                {
                    var (px, py) = Ballistics.Pos(p); float t = (px - bx) * dx + (py - by) * dy; if (t < 0f || t > b.Length) continue;
                    float off = Mathf.Abs((px - bx) * dy - (py - by) * dx); if (off > BeamHalfWidth || Rt.Untouchable(p)) continue;
                    if (_hitAt.TryGetValue(p, out var at) && now - at < SweepHitsGap) continue;
                    _hitAt[p] = now; BrainBoss.Hurt(p, LaserDamage, "The giant eye's beam burns you");
                }
            }
        }

        /// <summary>The Orbs' blow: a ring of the vat's psychic orbs (8, 12 in phase 2) and a bolt aimed at where the target stood.</summary>
        public static void Orbs(Boss.Fight f)
        {
            var (bx, by) = Ballistics.Pos(f.Unit); int n = f.Phase == 2 ? 12 : 8; float start = Random.Range(0f, 360f / n);
            for (int k = 0; k < n; k++) BrainBoss.Shoot(bx, by, start + k * 360f / n, BrainBoss.OrbSpeed, "ring");
            BrainBoss.Shoot(bx, by, Mathf.Atan2(f.AimY + 0.5f - by, f.AimX + 0.5f - bx) * Mathf.Rad2Deg, BrainBoss.BoltSpeed, "bolt");
            Sfx.Play("psi", TacticalGridView.GridToWorld(f.Unit.X, f.Unit.Y), 0.8f, 0.05f, 1.2f);
        }

        /// <summary>The Mind-Lash's blow: every player within MindRange it can see has their aim torn away and is dazed for 1.5 s. Breaking its line of sight is the only cover.</summary>
        public static void Mind(Boss.Fight f)
        {
            var h = f.Unit; Sfx.Play("psi", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.02f, 0.8f); CombatFx.Push("stagger", h, null, null).Now = true; int n = 0;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                if (CombatantUtil.Distance(h, q) > MindRange) continue;
                if (GameServices.Grid != null && !GameServices.Grid.CheckLineOfSight(h.X, h.Y, q.X, q.Y)) continue;
                var aim = Gunplay.AimOf(q); aim.Steady = 0f; aim.Bloom = Gunplay.PlayerMaxBloom; Body.Concuss(q, 1.5f); n++;
            }
            GameLog.Write(n > 0 ? "Its eyes find you, and for a moment you forget which way your gun points." : "Its eyes search for you through the rock.");
        }

        static void Defeated(Player p, HumanEnemy h)
        {
            Beams.Clear(); _sweeping = null; BrainBoss.Orbs.Clear();
            h.Inventory.Items.Add(new InventoryItem { Id = "specimen_tissue", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 1.5f, Price = 3200 });
            Story.Journal("The Specimen: the brain that fled Secret Lab Alpha fed on the Crystal Pool in the Deep Caverns and came back stronger. It is dead. A piece of it is worth a great deal to a lab.");
            GameLog.Event("Its eyes go dark one by one. The giant eye closes, and the brain drops into the shallows of the pool and lies still.");
        }
    }
}
