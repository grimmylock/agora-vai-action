// Boss: the Specimen, grown: the rematch with the brain that escaped Secret Lab Alpha (BrainBoss.Escaped), in SECRET LAB GAMMA's Signal Room.
// 2026-11-21 v3 (Yan: "the brain outside the vat should be at another lab not a cave, it should have new moves, it moves around during the fight ... also the eye behind him is
// connected through a cord and moves around and fires as well"; v2 before: "doesn't read as the Brain escaped ... the big eye tentacle or the laser attacks").
// TWO bodies:
//  - the BRAIN (a Boss.cs fight; drawn from the vat brain's own art, BossArt "specimen_v2", the giant eye cut out of it by ArtSource/specimen_cut_eye.py): it keeps its distance
//    (Def.KeepAt) and relocates: the Blink takes it through the void to another spot of the room. Its moves: the Orbs (a ring of the vat's orbs and a bolt; the Storm in phase 2),
//    the Pulse (red: a psychic ring that throws back everyone within PulseRadius; dive through it), the Pull (red: drags everyone within PullReach toward it), the Mind-Lash
//    (red: aim torn away and dazed in its sight; break its line of sight), the Blink.
//  - the EYE (a Mutant of its own, EyeDef, BossArt "specimen_eye"): tied to the brain by a cord (drawn by CombatFxView.SpecimenCord), it circles the brain at CordLength and
//    every EyeEvery seconds charges (its clip glows) and fires a beam at where its target stood when it began (a dive goes under it, a wall stops it); in phase 2 it fires more
//    often and sweeps. It can be shot: when it bursts, the cord goes slack and the beams stop for the rest of the fight (a tool for the player, CLAUDE.md rule 6).
// It only comes if it escaped the lab. Co-op: the nearest player; the orbs, beams, pulse and pull catch everyone. Host-side only (the snapshot carries the brain's clip, not the eye
// nor its beams yet).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class SpecimenGrown
    {
        public const string Lair = "Signal Room", Zone = "secret_lab_gamma", Done = "specimen_grown_done", Trophy = "Specimen Tissue", EyeName = "The Specimen's Eye";
        public const float WakeRange = 12f, MindRange = 11f, BeamReach = 14f, BeamHalfWidth = 0.55f, BeamFor = 0.6f, SweepFor = 1.2f, SweepArc = 60f;
        public const float CordLength = 3.2f, EyeOrbit = 45f, EyeLift = 1.1f, EyeCharge = 1.0f, EyeEvery = 5f, EyeEvery2 = 3.4f, PulseRadius = 6f, PullReach = 10f, PullTiles = 3f, BrainLift = 1.0f;
        public const int LaserDamage = 150, PulseDamage = 120, PullDamage = 60, EyeHp = 78700;

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "The Specimen", Title = "IT REMEMBERS YOU", Weapon = "SpecimenLegs", Armor = null, Hp = 262500, Faction = "Mutants", Rank = "Master", DoneFlag = Done, Vanishes = false,
            Art = "specimen_v2", Creature = true, KeepAt = 5, StepSeconds = 0.3f, Recover = 1.0f, Phase2Line = "The eye on its cord opens all the way. Every eye on the brain opens with it.",
            Moves = new[]
            {
                new Boss.Move { Name = "Orbs", Wind = 0.9f, MaxDist = 12f, Effects = new[] { "orbs" }, Weight = 3, Open = 1.0f, Clip = "orbs" },
                new Boss.Move { Name = "Pulse", Wind = 1.1f, Danger = true, MaxDist = PulseRadius, Effects = new[] { "pulse" }, Weight = 2, Open = 1.4f, Clip = "mind" },
                new Boss.Move { Name = "Pull", Wind = 1.0f, Danger = true, MaxDist = PullReach, Effects = new[] { "pull" }, Weight = 2, Open = 1.2f, Clip = "mind" },
                new Boss.Move { Name = "Mind-Lash", Wind = 1.0f, Danger = true, MaxDist = MindRange, Effects = new[] { "mind" }, Weight = 2, Open = 1.3f, Clip = "mind" },
                new Boss.Move { Name = "Blink", Wind = 0.6f, MaxDist = 20f, Effects = new[] { "blink" }, Weight = 2, Open = 0.6f, Clip = "mind" },
                new Boss.Move { Name = "Storm", Wind = 0.8f, Hits = 2, Gap = 0.9f, MaxDist = 12f, Effects = new[] { "orbs", "orbs" }, Weight = 3, Open = 1.4f, Phase2Only = true, Clip = "orbs" },
                new Boss.Move { Name = "Pull-Pulse", Wind = 1.0f, Hits = 2, Gap = 1.0f, MaxDist = PullReach, Effects = new[] { "pull", "pulse" }, RedHits = new[] { true, true }, Weight = 2, Open = 1.8f, Phase2Only = true, Clip = "mind" },   // hauled in, then thrown out
            },
        };
        public static readonly MutantDef EyeDef = new MutantDef { Name = EyeName, Tier = 5, Hp = EyeHp, PhysDR = 0.05f, AnomDR = 0.3f, Ap = 0, Mp = 0, Ac = 12, AttackDamage = 0, Behavior = "boss", PanicThreshold = 0f };
        static SpecimenGrown() { Def.Defeated = Defeated; }

        /// <summary>A beam now: from (X, Y) at Angle for Length tiles until Until; Lift = how high its start is drawn (the eye floats).</summary>
        public class EyeBeam { public float X, Y, Angle, Length, Until, Lift; }
        public static readonly List<EyeBeam> Beams = new List<EyeBeam>();
        public static Mutant Eye; static float _eyeAngle, _eyeNext = -1f, _eyeFireAt = -1f, _eyeAimX, _eyeAimY, _sweepFrom, _sweepTo, _sweepT0 = -1f; static bool _sweep;
        static readonly Dictionary<Player, float> _hitAt = new Dictionary<Player, float>();
        public static Boss.Fight Current => Boss.Fights.FirstOrDefault(f => f.Def == Def && !f.Gone && !f.Mirror);
        public static bool Active => Current != null;
        public static bool IsEye(ICombatant u) => u != null && u == Eye;
        public static bool EyeAlive => Eye != null && Eye.IsAlive() && Eye.Hp > 0;
        public static bool EyeCharging => EyeAlive && _eyeFireAt >= 0f;
        public static float EyeChargeT => _eyeFireAt < 0f ? 0f : Mathf.Clamp01(1f - (_eyeFireAt - Time.time) / EyeCharge);
        public static float EyeDiedAt = -99f;
        /// <summary>Where the brain's cord leaves it and where the eye hangs (tile space, y down: the drawing height already taken off).</summary>
        public static (float x, float y) BrainCord(Boss.Fight f) { var (x, y) = Ballistics.Pos(f.Unit); return (x + 0.3f, y - BrainLift); }
        public static (float x, float y) EyeAt() { if (Eye == null) return (0f, 0f); var (x, y) = Ballistics.Pos(Eye); return (x, y - EyeLift); }

        static bool _spawnedHere;
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; Beams.Clear(); Eye = null; _eyeFireAt = -1f; }

        public static void Tick(float dt)
        {
            var cur = Current;
            if (cur != null)
            {
                var players = Player.All.Where(p => p.IsAlive() && !Body.Down(p)).ToList();
                if (BrainBoss.Current == null) BrainBoss.FlyOrbs(dt, players);                  // its orbs fly here (the vat's tick flies them while the vat is in its lab)
                if (!cur.Broken && !cur.Finished) TickEye(cur, players, dt); else if (Eye != null && Eye.IsAlive()) { Eye.Hp = 0; EncounterState.Enemies.Remove(Eye); }
                TickBeams(players);
            }
            else { Beams.Clear(); if (Eye != null) { EncounterState.Enemies.Remove(Eye); Eye = null; } }
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
            var f = Boss.Spawn(Def, x, y); Rt.SetOffset(f.Unit, 0f, 0f); _home = (x, y);
            var (ex, ey) = TerrainMap.FindNearestEnterable(x + 2, y - 2);
            Eye = new Mutant(EyeName, ex, ey, EyeDef) { Hp = EyeHp, MaxHp = EyeHp, IsAggroed = true }; EncounterState.Enemies.Add(Eye); _eyeAngle = -45f; _eyeNext = Time.time + 3f; _eyeFireAt = -1f; Beams.Clear(); EyeDiedAt = -99f;
            f.NextMoveAt = Time.time + (intro ? 2.8f : 1.6f);
            if (intro)
            {
                GameLog.Event("Among the dead consoles something hangs in the air: the brain from Lab Alpha's vat, grown, its violet eyes open. On a cord of nerve beside it floats the giant eye, and it turns to look at you. It remembers you.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.2f);
            }
            return f;
        }
        static (int x, int y) _home;

        /// <summary>The eye: it circles the brain at the cord's length (floating: walls and furniture do not stop it), charges, fires; its death is the cord going slack.</summary>
        static void TickEye(Boss.Fight f, List<Player> players, float dt)
        {
            if (Eye == null) return;
            if (!EyeAlive) { if (EyeDiedAt < 0f) { EyeDiedAt = Time.time; Beams.Clear(); _eyeFireAt = -1f; GameLog.Event("The eye bursts on its cord. The cord goes slack, and the brain shrieks without a sound."); EncounterState.Enemies.Remove(Eye); } return; }
            float now = Time.time; var (bx, by) = Ballistics.Pos(f.Unit);
            if (_eyeFireAt < 0f) _eyeAngle += EyeOrbit * (f.Phase == 2 ? 1.5f : 1f) * dt;                  // it holds still while it charges
            float r = _eyeAngle * Mathf.Deg2Rad, tx = bx + Mathf.Cos(r) * CordLength, ty = by + Mathf.Sin(r) * CordLength * 0.8f;
            int ix = Mathf.Clamp(Mathf.FloorToInt(tx), 1, TerrainMap.Width - 2), iy = Mathf.Clamp(Mathf.FloorToInt(ty), 1, TerrainMap.Height - 2);
            Eye.X = ix; Eye.Y = iy; Rt.SetOffset(Eye, tx - ix - 0.5f, ty - iy - 0.5f);
            var target = players.OrderBy(p => CombatantUtil.Distance(Eye, p)).FirstOrDefault(); if (target == null) return;
            if (_eyeFireAt < 0f && now >= _eyeNext && CombatantUtil.Distance(Eye, target) <= BeamReach)
            {
                _eyeFireAt = now + EyeCharge; var (px, py) = Ballistics.Pos(target); _eyeAimX = px; _eyeAimY = py;      // where you stood when it began: move
                Sfx.Play("psi", TacticalGridView.GridToWorld(Eye.X, Eye.Y), 0.7f, 0.04f, 1.4f);
            }
            if (_eyeFireAt >= 0f && now >= _eyeFireAt)
            {
                _eyeFireAt = -1f; _eyeNext = now + (f.Phase == 2 ? EyeEvery2 : EyeEvery) * Random.Range(0.85f, 1.15f);
                var (ex, ey) = Ballistics.Pos(Eye); float deg = Mathf.Atan2(_eyeAimY - ey, _eyeAimX - ex) * Mathf.Rad2Deg; _hitAt.Clear();
                Sfx.Play("lantern_flare", TacticalGridView.GridToWorld(Eye.X, Eye.Y), 0.8f, 0.03f, 0.8f);
                if (f.Phase == 2 && Random.value < 0.5f) { _sweep = true; _sweepT0 = now; _sweepFrom = deg - SweepArc / 2f; _sweepTo = deg + SweepArc / 2f; Beams.Add(new EyeBeam { X = ex, Y = ey, Angle = _sweepFrom, Length = BeamLength(ex, ey, _sweepFrom), Until = now + SweepFor, Lift = EyeLift }); }
                else Beams.Add(new EyeBeam { X = ex, Y = ey, Angle = deg, Length = BeamLength(ex, ey, deg), Until = now + BeamFor, Lift = EyeLift });
            }
        }

        /// <summary>How far a beam from (x, y) at `deg` runs before a wall stops it (tiles, at most BeamReach). It runs over the ground (the eye's height is only the drawing).</summary>
        public static float BeamLength(float x, float y, float deg)
        {
            float r = deg * Mathf.Deg2Rad, dx = Mathf.Cos(r), dy = Mathf.Sin(r);
            for (float t = 0.5f; t < BeamReach; t += 0.25f)
            {
                var tile = TerrainMap.GetTile(Mathf.FloorToInt(x + dx * t), Mathf.FloorToInt(y + dy * t));
                if (tile == null || tile.Type == TileType.Solid || tile.Type == TileType.Door) return t;
            }
            return BeamReach;
        }

        /// <summary>The beams: a sweep turns (and its start follows the eye); anyone within BeamHalfWidth of a live beam and not diving is burned, once a second at most.</summary>
        static void TickBeams(List<Player> players)
        {
            float now = Time.time;
            if (_sweep && Beams.Count > 0 && _sweepT0 >= 0f)
            {
                float k = Mathf.Clamp01((now - _sweepT0) / SweepFor), a = Mathf.Lerp(_sweepFrom, _sweepTo, k); var b0 = Beams[Beams.Count - 1];
                if (EyeAlive) { var (ex, ey) = Ballistics.Pos(Eye); b0.X = ex; b0.Y = ey; }
                b0.Angle = a; b0.Length = BeamLength(b0.X, b0.Y, a); if (k >= 1f) _sweep = false;
            }
            Beams.RemoveAll(b => now > b.Until);
            foreach (var b in Beams)
            {
                float r = b.Angle * Mathf.Deg2Rad, dx = Mathf.Cos(r), dy = Mathf.Sin(r);
                foreach (var p in players)
                {
                    var (px, py) = Ballistics.Pos(p); float t = (px - b.X) * dx + (py - b.Y) * dy; if (t < 0f || t > b.Length) continue;
                    if (Mathf.Abs((px - b.X) * dy - (py - b.Y) * dx) > BeamHalfWidth || Rt.Untouchable(p)) continue;
                    if (_hitAt.TryGetValue(p, out var at) && now - at < 1f) continue;
                    _hitAt[p] = now; BrainBoss.Hurt(p, LaserDamage, "The eye's beam burns you");
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

        /// <summary>The Pulse's blow: a psychic ring bursts out PulseRadius tiles: everyone in it not diving is hurt and thrown outward.</summary>
        public static void Pulse(Boss.Fight f)
        {
            var (bx, by) = Ballistics.Pos(f.Unit); CombatFx.PxAt("thunderclap", f.Unit.X, f.Unit.Y); Sfx.Play("psi", TacticalGridView.GridToWorld(f.Unit.X, f.Unit.Y), 1f, 0.02f, 0.5f); Boss.HitStopPublic(0.05f);
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                var (qx, qy) = Ballistics.Pos(q); float dx = qx - bx, dy = qy - by, d = Mathf.Sqrt(dx * dx + dy * dy); if (d > PulseRadius) continue;
                if (Rt.Of(q).Diving || Rt.Untouchable(q)) { CombatFx.Push("dodge", q, f.Unit, null).Now = true; continue; }
                BrainBoss.Hurt(q, PulseDamage, "The pulse throws you back"); float l = Mathf.Max(0.01f, d); Rt.Shove(q, dx / l * 2.5f, dy / l * 2.5f, 0.35f); Rt.Knockdown(q, 0.6f);
            }
        }

        /// <summary>The Pull's blow: everyone within PullReach it can see and who is not diving is dragged PullTiles toward it (and stung a little).</summary>
        public static void Pull(Boss.Fight f)
        {
            Sfx.Play("psi", TacticalGridView.GridToWorld(f.Unit.X, f.Unit.Y), 1f, 0.02f, 0.4f); int n = 0;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                if (CombatantUtil.Distance(f.Unit, q) > PullReach || Rt.Of(q).Diving || Rt.Untouchable(q)) continue;
                if (GameServices.Grid != null && !GameServices.Grid.CheckLineOfSight(f.Unit.X, f.Unit.Y, q.X, q.Y)) continue;
                Boss.DragTo(f.Unit, q, PullTiles); BrainBoss.Hurt(q, PullDamage, "The void pulls at you"); n++;
            }
            if (n > 0) GameLog.Write("The air folds toward the brain and takes you with it.");
        }

        /// <summary>The Blink's blow: it goes through the void and comes out somewhere else in the room: an open tile 5-9 tiles from its target, within 12 of where the fight began.</summary>
        public static void Blink(Boss.Fight f, Player p)
        {
            var h = f.Unit; CombatFx.PxAt("smoke_bomb", h.X, h.Y);
            for (int tries = 0; tries < 40; tries++)
            {
                int x = _home.x + Random.Range(-12, 13), y = _home.y + Random.Range(-8, 9);
                if (!TerrainMap.CanEnterTile(x, y) || TerrainMap.UnitAt(x, y, h) != null) continue;
                int d = p == null ? 6 : System.Math.Max(System.Math.Abs(p.X - x), System.Math.Abs(p.Y - y)); if (d < 5 || d > 9) continue;
                h.X = x; h.Y = y; Rt.SetOffset(h, 0f, 0f); CombatFx.PxAt("smoke_bomb", x, y); Sfx.Play("psi", TacticalGridView.GridToWorld(x, y), 0.8f, 0.04f, 1.6f); return;
            }
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
            GameLog.Write(n > 0 ? "Its eyes find you, and for a moment you forget which way your gun points." : "Its eyes search for you through the walls.");
        }

        /// <summary>The eye's picture this frame: dead (its death clip, held), charging (the charge clip over the charge time), else its idle loop.</summary>
        public static Sprite EyeSprite()
        {
            var a = BossArt.Of("specimen_eye"); if (a == null) return null; float now = Time.time;
            if (!EyeAlive && a.Clips.TryGetValue("death", out var dc)) { var ds = BossArt.Frames(a, "death"); if (ds == null) return null; int i = dc.IndexAt(now - EyeDiedAt); return ds[i < 0 ? ds.Length - 1 : Mathf.Min(i, ds.Length - 1)]; }
            if (EyeCharging) { var cs = BossArt.Frames(a, "charge"); if (cs != null) return cs[Mathf.Clamp((int)(EyeChargeT * cs.Length), 0, cs.Length - 1)]; }
            var ids = BossArt.Frames(a, "idle"); if (ids == null) return null; var ic = a.Clips["idle"]; return ids[Mathf.Clamp(ic.IndexAt(now), 0, ids.Length - 1)];
        }

        static void Defeated(Player p, HumanEnemy h)
        {
            Beams.Clear(); BrainBoss.Orbs.Clear(); if (Eye != null) { Eye.Hp = 0; EncounterState.Enemies.Remove(Eye); }
            h.Inventory.Items.Add(new InventoryItem { Id = "specimen_tissue", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 1.5f, Price = 3200 });
            Story.Journal("The Specimen: the brain that fled Secret Lab Alpha turned up again in Secret Lab Gamma, with its eye on a cord. It is dead. A piece of it is worth a great deal to a lab.");
            GameLog.Event("Its eyes go dark one by one. The cord falls, the eye with it, and the brain drops onto the floor of the Signal Room and lies still.");
        }
    }
}
