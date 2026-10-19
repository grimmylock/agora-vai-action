// RT 7a: melee in real time (Yan, ACTION_DESIGN.md "Melee"; the Tactical Hatchet is the test run).
// - Click swings: the nearest enemy inside the weapon's reach and arc in front of you takes the hit; the crosshair's height on
//   them picks the part, as with guns. Blades make the target bleed, blunt weapons concuss.
// - Aim (right mouse) and click throws: the weapon flies (travel time, spin, max range) and hits through the same body rules.
//   A blade sticks in wood, tin, brush or cloth cover (E pulls it out) or in a body; stone, concrete and steel bounce it onto
//   the floor; blunt weapons never stick. A human pulls a blade out after a few seconds (2.5 s doing nothing else) and drops it;
//   on a mutant it falls off as it moves; on a corpse it goes into the loot (mutants: onto the floor).
// - Whatever lies on the floor is an ordinary ground item (walk onto it, E); with empty hands you equip it at once.
// Positions are in tile space (x right, y down; a unit stands at X + 0.5 + offset), like Ballistics.
// ponytail: Bull Rush, parry, takedowns and the Flying Steel perk come next (ACTION_DESIGN.md); NPCs don't throw yet.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Melee
    {
        public class Stats
        {
            public float Swing = 0.5f;          // seconds between swings
            public float Reach = 1.3f;          // tiles from your middle
            public float Arc = 100f;            // degrees in front of you
            public float ThrowSpeed = 12f;      // tiles a second
            public float ThrowRange = 8f;       // tiles
            public float WindUp;                // seconds before a throw leaves the hand (two-handed weapons: 1)
            public bool Blade = true, TwoHanded;
            public float StuckX = float.NaN, StuckY;   // RT 7d: where the buried blade's cut line is on weapon_<Key>_stuck.png
        }

        // per weapon (by name); the rest get stats from their weight: the smaller, the faster it swings and flies
        public static readonly Dictionary<string, Stats> Table = new Dictionary<string, Stats>
        {
            ["Tactical Hatchet"] = new Stats { Swing = 0.65f, Reach = 1.35f, Arc = 110f, ThrowSpeed = 13f, ThrowRange = 9f, StuckX = 23.5f, StuckY = 8.5f },
        };
        public static Stats For(Weapon w)
        {
            if (w?.Db != null && Table.TryGetValue(w.Db.Name, out var s)) return s;
            float kg = w?.Db?.WeightKg ?? 0.6f;
            return new Stats { Swing = 0.3f + 0.3f * kg, ThrowSpeed = Mathf.Max(8f, 17f - 6f * kg), ThrowRange = Mathf.Max(5f, 11f - 4f * kg) };
        }

        public const float PullAfter = 2.5f, PullTime = 2.5f, FallsOff = 3f;
        public const float ThrowHeight = 0.45f;                    // chest high unless the crosshair is on a body

        public class Flying { public Weapon W; public InventoryItem Item; public ICombatant By; public float X, Y, Dx, Dy, Left, Speed, H, Deg, Spin; public bool Blade; }
        public class Lodged { public InventoryItem Item; public int X, Y; public float Wx, Wy, Deg; }          // in cover: E pulls it out
        public class Stuck { public InventoryItem Item; public ICombatant In; public float Since, Deg, Side; public bool Pulling; }   // Side: +1 = thrown from the left
        public class Lying { public GroundItem G; public float Wx, Wy, Deg; }                                // on the floor (a ground item)
        public static readonly List<Flying> Air = new List<Flying>();
        public static readonly List<Lodged> InCover = new List<Lodged>();
        public static readonly List<Stuck> InBodies = new List<Stuck>();
        public static readonly List<Lying> OnFloor = new List<Lying>();
        static readonly Dictionary<Player, (float at, Weapon w, float dx, float dy, float h, float power)> Winding = new Dictionary<Player, (float, Weapon, float, float, float, float)>();

        // RT 7e (Yan: "left click swings it, holding left click charges the swing for higher damage and can be held indefinitely,
        // right click 'aims' the throw and starts charging it, letting go throws it"; and a throw wind-up "like lifting it over the
        // player's head"). Per combatant: when each button went down, when the last swing started (for its pose), when it threw.
        public class Charge { public float SwingFrom = -1f, ThrowFrom = -1f, SwungAt = -99f, ThrewAt = -99f; }
        static readonly Dictionary<ICombatant, Charge> Charges = new Dictionary<ICombatant, Charge>();
        public static Charge ChargeOf(ICombatant u) { if (!Charges.TryGetValue(u, out var c)) Charges[u] = c = new Charge(); return c; }
        public const float FullSwingCharge = 1f, MaxSwingBonus = 0.75f;   // held a second past the wind-up: x1.75 damage
        public const float FullThrowCharge = 0.6f, MinThrowHold = 0.2f;  // a throw charged 0.6 s flies full speed and range
        public const float ThrowWhip = 0.06f;                              // it leaves the hand this far into the whip forward
        static readonly Dictionary<ICombatant, float> NextSwing = new Dictionary<ICombatant, float>();
        // RT 7c: a swing lands at the chop, after the wind-up (HeldPoseMath.Chop), not on the click
        static readonly List<(float at, ICombatant by, Weapon w, float dx, float dy, float h, float mult)> Pending = new List<(float, ICombatant, Weapon, float, float, float, float)>();
        public static float HitDelay => HeldPoseMath.WindUp + HeldPoseMath.ChopTime;
        public static void Reset() { Air.Clear(); InCover.Clear(); InBodies.Clear(); OnFloor.Clear(); Winding.Clear(); NextSwing.Clear(); Pending.Clear(); Charges.Clear(); }

        public static float Now = -1f;                             // the harness sets it; the game uses Time.time
        static float T => Now >= 0f ? Now : Time.time;

        /// <summary>A human busy pulling a blade out of himself does nothing else (RtAi.Tick asks).</summary>
        public static bool Busy(ICombatant u) => InBodies.Any(s => s.In == u && s.Pulling);

        // ---------------- the player ----------------

        /// <summary>RT 7e: the mouse buttons with a melee weapon, every frame. Left: press winds up, release swings (held past the
        /// wind-up it charges, up to x1.75 damage). Right: press lifts it over your head and charges the throw, release throws it
        /// (let go before MinThrowHold - or a two-handed weapon's wind-up - and you just lower it). at = the crosshair, tile space.</summary>
        public static void PlayerInput(Player p, Weapon w, bool left, bool right, (float x, float y)? at)
        {
            var c = ChargeOf(p); float now = T;
            if (Winding.ContainsKey(p)) return;                                           // mid-throw
            if (left && c.SwingFrom < 0f && c.ThrowFrom < 0f && !(NextSwing.TryGetValue(p, out var ns) && now < ns)) c.SwingFrom = now;
            if (!left && c.SwingFrom >= 0f)
            {
                float held = now - c.SwingFrom; c.SwingFrom = -1f;
                float wind = Mathf.Min(held, HeldPoseMath.WindUp);
                float charge = Mathf.Clamp01((held - HeldPoseMath.WindUp) / FullSwingCharge);
                c.SwungAt = now - wind;                                                   // the pose carries on from where the wind-up is
                if (at.HasValue) Release(p, w, at.Value, HeldPoseMath.WindUp - wind + HeldPoseMath.ChopTime, 1f + MaxSwingBonus * charge);
            }
            if (right && c.ThrowFrom < 0f && c.SwingFrom < 0f) c.ThrowFrom = now;
            if (!right && c.ThrowFrom >= 0f)
            {
                float held = now - c.ThrowFrom; c.ThrowFrom = -1f;
                if (held < Mathf.Max(MinThrowHold, For(w).WindUp) || !at.HasValue) return;  // not wound up: lowered, no throw
                var (dx, dy, h) = Aim(p, at.Value);
                if (dx == 0f && dy == 0f) return;
                c.ThrewAt = now;
                Winding[p] = (now + ThrowWhip, w, dx, dy, h, Mathf.Clamp01(held / FullThrowCharge));
            }
        }

        static (float dx, float dy, float h) Aim(ICombatant by, (float x, float y) at)
        {
            var (px, py) = Ballistics.Pos(by);
            var over = Rt.UnitUnder(at.x, at.y);
            var (tx, ty) = over != null ? Ballistics.Pos(over) : at;                       // over a body: at its footprint, as guns aim
            float dx = tx - px, dy = ty - py, len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.05f) return (0f, 0f, 0f);
            return (dx / len, dy / len, over != null ? Ballistics.AimHeight(over, at.y) : ThrowHeight);
        }

        /// <summary>The swing leaves the wind-up: the hit lands `delay` s later (at the chop), `mult` x damage.</summary>
        static void Release(Player p, Weapon w, (float x, float y) at, float delay, float mult)
        {
            var (dx, dy, h) = Aim(p, at);
            if (dx == 0f && dy == 0f) return;
            float now = T;
            NextSwing[p] = now + delay + HeldPoseMath.Follow + HeldPoseMath.Recover;
            var anim = CombatFx.Push("melee", p, null, w); anim.Now = true;                  // the whoosh
            anim.ToX = p.X + Mathf.RoundToInt(dx * 2f); anim.ToY = p.Y + Mathf.RoundToInt(dy * 2f);
            Pending.Add((now + delay, p, w, dx, dy, h, mult));
            if (mult > 1.05f) GameLog.Write($"A charged swing (x{mult:0.00}).");
        }

        /// <summary>RT 7a (kept for tests and NPCs): click with a melee weapon - aiming throws it, otherwise it swings.</summary>
        public static void PlayerAttack(Player p, Weapon w, (float x, float y) at, bool aiming)
        {
            var st = For(w);
            float now = T;
            if (Winding.ContainsKey(p) || NextSwing.TryGetValue(p, out var t) && now < t) return;
            var (dx, dy, h) = Aim(p, at);
            if (dx == 0f && dy == 0f) return;
            if (aiming)
            {
                if (st.WindUp > 0f) { Winding[p] = (now + st.WindUp, w, dx, dy, h, 1f); GameLog.Write($"You wind up the {w.Name}..."); }
                else Throw(p, w, dx, dy, h);
                return;
            }
            NextSwing[p] = now + Mathf.Max(st.Swing, HeldPoseMath.SwingLength);
            var anim = CombatFx.Push("melee", p, null, w); anim.Now = true;                      // the wind-up starts now
            anim.ToX = p.X + Mathf.RoundToInt(dx * 2f); anim.ToY = p.Y + Mathf.RoundToInt(dy * 2f);
            Pending.Add((now + HitDelay, p, w, dx, dy, h, 1f));
            ChargeOf(p).SwungAt = now;
        }

        /// <summary>One swing: the nearest enemy within reach and inside the arc in front takes it.</summary>
        public static ICombatant Swing(ICombatant by, Weapon w, float dx, float dy, float h, float mult = 1f)
        {
            var st = For(w);
            var (px, py) = Ballistics.Pos(by);
            ICombatant best = null; float bestD = float.MaxValue;
            foreach (var u in Gunplay.Bodies())
            {
                if (u == null || u == by || !u.IsAlive() || Body.Down(u)) continue;
                var (ux, uy) = Ballistics.Pos(u);
                float ex = ux - px, ey = uy - py, d = Mathf.Sqrt(ex * ex + ey * ey);
                if (d > st.Reach || d < 0.01f) continue;
                if ((ex * dx + ey * dy) / d < Mathf.Cos(st.Arc * 0.5f * Mathf.Deg2Rad)) continue;   // outside the arc
                if (d < bestD) { best = u; bestD = d; }
            }
            if (best == null) return null;
            var fx = CombatFx.Push("impact", by, best, w); fx.Now = true;                       // blood, the flash, the knock-back
            // the body part: a line straight at them at the crosshair's height
            var (bx, by2) = Ballistics.Pos(best);
            float ox = bx - px, oy = by2 - py, ol = Mathf.Max(0.01f, Mathf.Sqrt(ox * ox + oy * oy));
            var hit = Ballistics.Trace(by, px, py, ox / ol, oy / ol, st.Reach + 0.3f, h, new[] { best });
            if (hit.Unit == null) { hit.Unit = best; hit.Part = "Torso"; }
            fx.Hit = true;
            Strike(by, w, hit.Unit, hit.Part, hit.Side, fx, mult);
            return best;
        }

        /// <summary>Damage from a swing or a throw that lands: the old armor math, limb health, bleeding or a concussion.</summary>
        public static void Strike(ICombatant by, Weapon w, ICombatant target, string part, float side, FxEvent fx = null, float mult = 1f)
        {
            var st = For(w);
            float raw = (w.BaseDamage + (w.Upgrades.Contains("Reinforced") ? 30 : 0)) * Ballistics.PartMult(part ?? "Torso") * mult;
            if (target is Mutant tm0 && !tm0.IsAggroed) raw *= 1.3f;                      // they never saw it coming
            if (ItemCondition.IsDamaged(w.Condition)) raw *= 0.75f;
            if (by is Player) raw *= CombatRules.PlayerDamage;
            raw *= CombatRules.Lethality;
            float dr = part == "Head" ? HeadGear.Of(HeadGear.ArmorOf(target)).dr : target.PhysDR * CombatRules.ArmorScale(target);
            int dmg = CombatRules.PhysicalDamage(raw, "Standard", dr, w.Db?.IgnorePhysDRPercent ?? 0f);
            int before = target.Hp;
            target.Hp = System.Math.Max(0, target.Hp - dmg);
            if (fx != null) { fx.Killed = !target.IsAlive(); fx.Overkill = dmg - before; }   // the view plays blood, the fall, gibs
            GameLog.Write($"{(by is Player ? "You hit" : by.Name + " hits")} {(target is Player ? "you" : target.Name)} in the {(part ?? "body").ToLowerInvariant()} for {dmg}.");
            TestingGround.Note(target, dmg, part ?? "Torso");
            if (target is Mutant tm && !TestingGround.IsDummy(tm)) { tm.IsAggroed = true; if (!(target is HumanEnemy)) tm.DamageTakenThisRound += dmg; }
            if (by is Player && target is HumanEnemy he && !TestingGround.IsDummy(he)) foreach (var o in EnemyAI.Squad(he)) if (o.IsAlive()) o.IsAggroed = true;
            if (target.IsAlive())
            {
                Body.Hit(target, part ?? "Torso", side, dmg, "Standard");
                if (st.Blade && !target.StatusEffects.Bleeding) { target.StatusEffects.Bleeding = true; GameLog.Write($"{(target is Player ? "You are" : target.Name + " is")} bleeding."); }
                if (!st.Blade) Body.Concuss(target, 4f);
            }
            if (by is Player) Rt.AddFocus(target.IsAlive() ? 4f : 25f);
            if (!target.IsAlive()) Weapon.OnTargetKilled(by, target);
        }

        /// <summary>Let go of the weapon: it leaves your hands and flies.</summary>
        public static void Throw(Player p, Weapon w, float dx, float dy, float h, float power = 1f)
        {
            var st = For(w);
            var (px, py) = Ballistics.Pos(p);
            float k = 0.6f + 0.4f * Mathf.Clamp01(power);                                        // RT 7e: a half-charged throw is slower and shorter
            Air.Add(new Flying { W = w, Item = ItemOf(w), By = p, X = px, Y = py, Dx = dx, Dy = dy, H = h, Left = st.ThrowRange * k,
                Speed = st.ThrowSpeed * k, Blade = st.Blade, Deg = Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg, Spin = dx >= 0f ? -900f : 900f });
            if (p.EquippedWeapon == w) { p.EquippedWeapon = null; p.EquippedWeaponCategory = null; p.EquippedWeaponKey = null; }
            GameLog.Write($"You throw the {w.Name}.");
            Sfx.Play("swing", TacticalGridView.GridToWorld(p.X, p.Y));
            if (!EncounterState.InCombat) Contact.ShotFrom(p);
        }

        static InventoryItem ItemOf(Weapon w)
        {
            var (cat, key) = WeaponArt.Identify(w.Db);
            var def = ShopDatabase.Items.FirstOrDefault(i => i.Type == "weapon" && i.Category == cat && i.Key == key);
            return new InventoryItem { Id = def?.Id ?? $"weapon_{cat}_{key}", Name = w.Db?.Name ?? w.Name, WeightKg = w.Db?.WeightKg ?? 0.6f, Qty = 1,
                Type = "weapon", Price = def?.Price, Category = cat, Key = key, Condition = w.Condition,
                Upgrades = w.Upgrades != null && w.Upgrades.Count > 0 ? Upgrades.Join(w.Upgrades) : null };
        }

        // ---------------- every frame ----------------

        public static void Tick(float dt)
        {
            float now = T;
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                var q = Pending[i];
                if (now < q.at) continue;
                Pending.RemoveAt(i);
                if (q.by.IsAlive() && (!(q.by is Player pp) || pp.EquippedWeapon == q.w)) Swing(q.by, q.w, q.dx, q.dy, q.h, q.mult);
            }
            foreach (var kv in Winding.ToList())
                if (now >= kv.Value.at) { Winding.Remove(kv.Key); if (kv.Key.EquippedWeapon == kv.Value.w) Throw(kv.Key, kv.Value.w, kv.Value.dx, kv.Value.dy, kv.Value.h, kv.Value.power); }

            for (int i = Air.Count - 1; i >= 0; i--)
            {
                var f = Air[i];
                float step = Mathf.Min(f.Speed * dt, f.Left);
                var hit = Ballistics.Trace(f.By, f.X - f.Dx * 0.1f, f.Y - f.Dy * 0.1f, f.Dx, f.Dy, step + 0.1f, f.H, Gunplay.Bodies().Where(u => u != f.By));   // from a bit back: Trace skips bodies under 0.05 from the start
                f.Deg += f.Spin * dt;
                if (hit.Unit != null)
                {
                    Air.RemoveAt(i);
                    var fx = CombatFx.Push("impact", f.By, hit.Unit, f.W); fx.Now = true; fx.Hit = true;   // blood, as for a round
                    Strike(f.By, f.W, hit.Unit, hit.Part, hit.Side, fx);
                    if (f.Blade && hit.Unit.IsAlive()) InBodies.Add(new Stuck { Item = f.Item, In = hit.Unit, Since = now, Deg = f.Deg, Side = f.Dx >= 0f ? 1f : -1f });
                    else if (f.Blade && hit.Unit is HumanEnemy dead) dead.Inventory.Items.Add(f.Item);                  // in the corpse: loot it back
                    else { var (ux, uy) = Ballistics.Pos(hit.Unit); Drop(f.Item, ux, uy, f.Deg); }
                    continue;
                }
                if (hit.Wall || hit.Cover)
                {
                    Air.RemoveAt(i);
                    var mat = hit.CoverX >= 0 ? Materials.At(hit.CoverX, hit.CoverY) : Material.Concrete;
                    if (f.Blade && hit.CoverX >= 0 && Materials.Pierced(mat))
                    {
                        InCover.Add(new Lodged { Item = f.Item, X = hit.CoverX, Y = hit.CoverY, Wx = hit.EndX, Wy = hit.EndY, Deg = f.Deg });
                        Sfx.Play("hit_wood", TacticalGridView.GridToWorld(hit.CoverX, hit.CoverY));
                    }
                    else
                    {
                        Drop(f.Item, hit.EndX - f.Dx * 0.6f, hit.EndY - f.Dy * 0.6f, f.Deg);                                  // bounced back onto the floor
                        Sfx.Play(mat == Material.Wood ? "hit_wood" : mat == Material.Tin ? "hit_tin" : "hit_concrete", TacticalGridView.GridToWorld(hit.CoverX >= 0 ? hit.CoverX : (int)hit.EndX, hit.CoverY >= 0 ? hit.CoverY : (int)hit.EndY));
                    }
                    continue;
                }
                f.X += f.Dx * step; f.Y += f.Dy * step; f.Left -= step;
                if (f.Left <= 0.001f) { Air.RemoveAt(i); Drop(f.Item, f.X, f.Y, f.Deg); }
            }

            for (int i = InBodies.Count - 1; i >= 0; i--)
            {
                var s = InBodies[i];
                var (ux, uy) = Ballistics.Pos(s.In);
                if (!s.In.IsAlive())
                {
                    InBodies.RemoveAt(i);
                    if (s.In is HumanEnemy dead) dead.Inventory.Items.Add(s.Item); else Drop(s.Item, ux, uy, s.Deg);
                    continue;
                }
                if (!(s.In is HumanEnemy))                                                         // a mutant shakes it off running
                {
                    if (now >= s.Since + FallsOff) { InBodies.RemoveAt(i); Drop(s.Item, ux, uy + 0.3f, s.Deg); }
                    continue;
                }
                if (!s.Pulling && now >= s.Since + PullAfter) { s.Pulling = true; GameLog.Write($"{s.In.Name} wrenches the {s.Item.Name} out."); }
                if (s.Pulling && now >= s.Since + PullAfter + PullTime) { InBodies.RemoveAt(i); Drop(s.Item, ux + 0.3f, uy + 0.3f, 90f); }
            }

            OnFloor.RemoveAll(l => !TerrainMap.GroundItems.Contains(l.G));                          // picked up
        }

        /// <summary>Onto the floor as an ordinary ground item (tile under the point), drawn where it fell.</summary>
        public static void Drop(InventoryItem item, float x, float y, float deg)
        {
            int tx = Mathf.Clamp(Mathf.FloorToInt(x), 0, TerrainMap.Width - 1), ty = Mathf.Clamp(Mathf.FloorToInt(y), 0, TerrainMap.Height - 1);
            if (!Open(tx, ty))                                                                    // never inside a wall or cover
                foreach (var (ax, ay) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (-1, 1), (1, -1), (-1, -1) })
                    if (Open(tx + ax, ty + ay)) { tx += ax; ty += ay; x = tx + 0.5f; y = ty + 0.5f; break; }
            var g = new GroundItem { X = tx, Y = ty, Item = item };
            TerrainMap.GroundItems.Add(g);
            OnFloor.Add(new Lying { G = g, Wx = x, Wy = y, Deg = (Mathf.Cos(deg * Mathf.Deg2Rad) >= 0f ? 0f : 180f) + 15f });   // lying flat, a little askew
        }

        /// <summary>RT 7d: how a blade sits in a body - the cut line of its "stuck" sprite on the chest, a little toward the thrower,
        /// the edge in and the handle up and back toward them. (degrees, flip the sprite left-right, offset from the body's middle
        /// in character px, x right / y down).</summary>
        public static (float deg, bool flipX, float ox, float oy) StuckPose(float side) =>
            side >= 0f ? (140f, true, -0.5f, -0.5f) : (-140f, false, 0.5f, -0.5f);

        static bool Open(int x, int y) => x >= 0 && y >= 0 && x < TerrainMap.Width && y < TerrainMap.Height
            && !TileRules.BlocksMovement(TerrainMap.GetTile(x, y).Type) && TerrainMap.CoverAcAt(x, y) == 0;

        /// <summary>E next to a blade stuck in cover: pull it out (Flying Steel will make this quicker).</summary>
        public static bool PullLodged(Player p)
        {
            var l = InCover.FirstOrDefault(c => System.Math.Max(System.Math.Abs(c.X - p.X), System.Math.Abs(c.Y - p.Y)) <= 1);
            if (l == null) return false;
            InCover.Remove(l);
            p.AddItem(l.Item);
            GameLog.Write($"You pull the {l.Item.Name} out.");
            Sfx.Play("pickup");
            EquipIfEmptyHanded(p, l.Item);
            return true;
        }

        /// <summary>Picked a weapon up with nothing in your hands: it goes straight into them.</summary>
        public static void EquipIfEmptyHanded(Player p, InventoryItem item)
        {
            if (p.EquippedWeapon == null && item?.Type == "weapon" && p.Inventory.Items.Any(i => i.Id == item.Id)) p.EquipWeaponFromInventory(item.Id);
        }
    }
}
