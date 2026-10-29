// Boss 1 (Yan, 2026-10-29: "build the secret lab hatchet ... bossfight ... to be used as baseline for the bossfights in this game,
// im a sucker for HYPE MOMENTS AND AURA stuff like metal gear rising revengeance, dragon ball, sekiro"):
// the boss framework, and the first boss on it.
// - A boss fights with telegraphed moves: a WHITE glint (and its tink) = parry it; a RED glint (and a low drone) = it can't be
//   parried, dodge it (the dive, or just get out of reach). Combos chain several hits.
// - "perfect parries stagger him for a second, normal parries just block the damage" (Parry.cs): while staggered he takes
//   everything; otherwise he swats bullets away in a shower of sparks (Yan left guns to me - this makes the parry the way in
//   and your gun the punish).
// - Phase 2 at half health: his aura flares, a sting, faster and nastier moves.
// - At 0 HP he doesn't die - he drops to a knee, broken: "when hp bar gets to 0 we can press a key to finish him off" - E next to
//   him: the finisher (slow motion, the camera on you both, a hit-stop, the blood). "after he's down and looted, a few seconds
//   later a smoke grenade goes off and he's gone".
// - The HUD (HudView.Boss.cs): his name card when the fight starts, his health bar, STAGGERED / EXECUTE.
// Bosses aren't nerfed for difficulty (rule 6): tune them by giving the player tools.
// Co-op: he fights the nearest player (Rt.TargetOf); any player can parry him, shoot him while he's open, or finish him.
// ponytail: the hit-stop is the host's time scale (fine solo; co-op clients don't share the host's clock yet).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Boss
    {
        public class Move
        {
            public string Name;
            public float Wind = 0.6f;      // the glint, then this long before the hit lands
            public bool Danger;            // red glint: can't be parried
            public int Hits = 1;           // a combo: each hit has its own glint
            public float Gap = 0.4f;       // between a combo's hits
            public float Lunge;            // > 0: he closes this far in one rush before the hit
            public float Mult = 1f;        // x the weapon's damage
            public int Weight = 1;         // how often he picks it
            public bool Phase2Only;
        }

        public class Def
        {
            public string Name, Title, Weapon, Armor;
            public int Hp;
            public float StepSeconds = 0.35f, Recover = 1.0f;
            public Move[] Moves;
            public string Phase2Line;
        }

        public class Fight
        {
            public Def Def; public HumanEnemy Unit;
            public int Phase = 1;
            public Move Doing; public int HitsLeft; public float StrikeAt = -1f, NextMoveAt, NextStep, StaggerUntil = -1f;
            public bool Broken, Finished, Gone; public float LootedAt = -1f;
            public float GlintAt = -99f; public bool GlintDanger;   // for the view
            public float Started;
        }

        public static readonly List<Fight> Fights = new List<Fight>();
        public static Fight Of(ICombatant u) => u == null ? null : Fights.FirstOrDefault(f => f.Unit == u && !f.Gone);
        public static bool Is(ICombatant u) => Of(u) != null;
        public static Fight Current => Fights.FirstOrDefault(f => !f.Gone && !f.Finished) ?? Fights.LastOrDefault(f => !f.Gone && Time.time - (f.LootedAt < 0 ? Time.time : f.LootedAt) < 4f);

        // the name card (HudView.Boss.cs reads it)
        public static string SplashTitle, SplashSub; public static float SplashAt = -99f, SplashSeconds = 3.2f;
        static void Splash(string title, string sub) { SplashTitle = title; SplashSub = sub; SplashAt = Time.unscaledTime; }

        public static void Reset() { Sleepers.Clear(); Fights.Clear(); _stopUntil = -1f; _slowUntil = -1f; _momentAt = -99f; Parry.Unparryable = false; SplashAt = -99f; }

        // ---------------- the first boss ----------------

        // Yan's hatchet fight (ACTION_DESIGN "Melee"): a corpse in black armour with a red visor, the Tactical Hatchet buried in
        // it; take the hatchet and he gets up - a nod to a certain unkillable courier, no names from that game. He draws a knife.
        /// <summary>Yan: "a black suit with a red visor and a gas mask" (ArtSource/make_unique_armor.py) - his alone, not in any loot table.</summary>
        public static readonly ArmorDef BlackVisorSuit = new ArmorDef { Name = "Black Visor Suit", PhysDR = 0.6f, AnomDR = 0.3f, BeltSlots = 2, ModSlots = 2, Mp = 4, WeightKg = 14f };
        static ArmorDef SuitOf(Def def) => def.Armor == BlackVisorSuit.Name ? BlackVisorSuit : ArmorDatabase.Get("tier5", def.Armor) ?? ArmorDatabase.Get("tier4", def.Armor);

        public static readonly Def Visor = new Def
        {
            Name = "The Visor", Title = "HE ALWAYS GETS UP", Weapon = "SerratedTrenchKnife", Armor = "Black Visor Suit", Hp = 2400,
            StepSeconds = 0.3f, Recover = 0.9f, Phase2Line = "The red visor flares. He stops holding back.",
            Moves = new[]
            {
                new Move { Name = "Slash", Wind = 0.6f, Weight = 3 },
                new Move { Name = "Flurry", Wind = 0.5f, Hits = 3, Gap = 0.38f, Mult = 0.7f, Weight = 2 },
                new Move { Name = "Lunge", Wind = 0.75f, Danger = true, Lunge = 5f, Mult = 1.4f, Weight = 1 },
                new Move { Name = "Feint flurry", Wind = 0.4f, Hits = 5, Gap = 0.3f, Mult = 0.6f, Weight = 2, Phase2Only = true },
                new Move { Name = "Execution", Wind = 0.9f, Danger = true, Lunge = 7f, Mult = 1.8f, Weight = 2, Phase2Only = true },
            },
        };

        /// <summary>Put a boss on the map at (x, y) and start the fight.</summary>
        public static Fight Spawn(Def def, int x, int y)
        {
            var w = WeaponDatabase.Categories["melee"].TryGetValue(def.Weapon, out var wd) ? wd : null;
            var a = SuitOf(def);
            var h = new HumanEnemy(def.Name, x, y, "Contractors", "Legend", w, a) { IsAggroed = true, MaxHp = def.Hp, Hp = def.Hp };
            EncounterState.Enemies.Add(h); EncounterState.InCombat = true;
            var f = new Fight { Def = def, Unit = h, Started = Time.time, NextMoveAt = Time.time + 1.2f };
            Fights.Add(f);
            Splash(def.Name.ToUpperInvariant(), def.Title);
            Sfx.Play("boss_sting", null, 1f, 0f);
            HitStop(0.4f);
            GameLog.Write($"{def.Name}: {def.Title}");
            return f;
        }

        // ---------------- every frame (Rt.WorldTick, instead of RtAi for this unit) ----------------

        public static void Tick(Fight f, Player p)
        {
            var h = f.Unit; float now = Time.time;
            if (f.Broken || f.Finished || !h.IsAlive() || p == null) return;
            if (now < f.StaggerUntil) return;
            if (f.Phase == 1 && h.Hp <= f.Def.Hp / 2)
            {
                f.Phase = 2; f.Doing = null; f.NextMoveAt = now + 1.4f;
                Splash("PHASE 2", f.Def.Phase2Line);
                Sfx.Play("boss_sting", null, 0.8f, 0f, 1.25f);
                CombatFx.Push("aura", h, null, h.EquippedWeapon).Now = true;
                HitStop(0.3f);
            }
            float speed = f.Phase == 2 ? 0.8f : 1f;
            int dist = CombatantUtil.Distance(h, p);

            if (f.Doing == null)
            {
                if (dist > 1 && now >= f.NextStep)
                {
                    f.NextStep = now + f.Def.StepSeconds * speed;
                    h.Mp = System.Math.Max(h.Mp, TurnLoop.WalkMp);
                    h.MoveToward(p.X, p.Y);
                }
                if (now < f.NextMoveAt) return;
                var options = f.Def.Moves.Where(m => (!m.Phase2Only || f.Phase == 2) && (m.Lunge > 0f ? dist >= 2 && dist <= m.Lunge : dist <= 1)).ToList();
                if (options.Count == 0) return;
                int roll = Random.Range(0, options.Sum(m => m.Weight));
                foreach (var m in options) { if (roll < m.Weight) { Begin(f, m, now, speed); break; } roll -= m.Weight; }
                return;
            }
            if (now < f.StrikeAt) return;
            Land(f, p);
            if (f.Doing == null) return;                                    // parried mid-combo: the stagger already cut it short
            if (--f.HitsLeft > 0) { f.StrikeAt = now + f.Doing.Gap * speed; Glint(f, f.Doing.Danger); return; }
            f.Doing = null; f.NextMoveAt = now + f.Def.Recover * speed * Random.Range(0.8f, 1.3f);
        }

        static void Begin(Fight f, Move m, float now, float speed)
        {
            f.Doing = m; f.HitsLeft = m.Hits; f.StrikeAt = now + m.Wind * speed;
            Glint(f, m.Danger);
            if (m.Danger) Banter.Say(f.Unit, m.Name.ToUpperInvariant() + "!", 1);
        }

        static void Glint(Fight f, bool danger)
        {
            f.GlintAt = Time.time; f.GlintDanger = danger;
            var fx = CombatFx.Push("glint", f.Unit, null, f.Unit.EquippedWeapon); fx.Now = true; fx.Psi = danger;
        }

        /// <summary>The hit lands now: a lunge rushes in first; the swing takes whoever is in its arc (parry, block or blood).</summary>
        static void Land(Fight f, Player p)
        {
            var h = f.Unit; var m = f.Doing;
            if (m.Lunge > 0f)
            {
                int sx = System.Math.Sign(h.X - p.X), sy = System.Math.Sign(h.Y - p.Y);
                var (x, y) = TerrainMap.FindNearestEnterable(p.X + sx, p.Y + sy);
                if (CombatantUtil.Distance(p, x, y) <= 1) { h.X = x; h.Y = y; }   // Rt.Glide turns the jump into a rush
            }
            var (hx, hy) = Ballistics.Pos(h); var (px, py) = Ballistics.Pos(p);
            float dx = px - hx, dy = py - hy, len = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy));
            var anim = CombatFx.Push("melee", h, p, h.EquippedWeapon); anim.Now = true;
            if (m.Danger && Rt.Of(p).Diving) { GameLog.Write("DODGED!"); CombatFx.Push("dodge", p, h, null).Now = true; return; }   // the dive goes under it
            if (h.EquippedWeapon == null) return;
            Parry.Unparryable = m.Danger;
            try { Melee.Swing(h, h.EquippedWeapon, dx / len, dy / len, 0.45f, m.Mult); }
            finally { Parry.Unparryable = false; }
        }

        // ---------------- what the rest of the game asks ----------------

        /// <summary>A perfect parry (or anything else that staggers): open for `seconds`. A plain enemy just loses that long.</summary>
        public static void Stagger(ICombatant u, float seconds)
        {
            var f = Of(u);
            if (f == null) { if (u is HumanEnemy he) RtAi.Hold(he, seconds); return; }
            if (f.Broken) return;
            f.StaggerUntil = Time.time + seconds; f.Doing = null; f.NextMoveAt = f.StaggerUntil + 0.3f;
            CombatFx.Push("stagger", u, null, null).Now = true;
        }

        public static bool Staggered(ICombatant u) => Of(u) is Fight f && Time.time < f.StaggerUntil;

        /// <summary>Rounds glance off him unless he's staggered (or already broken).</summary>
        public static bool Deflects(ICombatant u) => Of(u) is Fight f && !f.Broken && !f.Finished && Time.time >= f.StaggerUntil;

        /// <summary>After any damage (Gunplay.Resolve, Melee.Strike): a boss at 0 doesn't die, he breaks.</summary>
        public static void OnDamaged(ICombatant u)
        {
            var f = Of(u);
            if (f == null || f.Finished || u.Hp > 0) return;
            u.Hp = 1;
            if (f.Broken) return;
            f.Broken = true; f.Doing = null;
            Splash("EXECUTE", "E - next to him");
            CombatFx.Push("stagger", u, null, null).Now = true;
            HitStop(0.35f);
            GameLog.Write($"{f.Def.Name} drops to one knee. E next to him: execute.");
        }

        public static Fight BrokenNear(Player p) => Fights.FirstOrDefault(f => f.Broken && !f.Finished && CombatantUtil.Distance(p, f.Unit) <= 2);

        /// <summary>E next to a broken boss: the finisher.</summary>
        public static bool TryFinish(Player p)
        {
            var f = BrokenNear(p);
            if (f == null) return false;
            f.Finished = true;
            var (x, y) = Ballistics.Pos(f.Unit);
            Story.Cutscene(x, y, 1.6f);
            HitStop(0.45f);
            var fx = CombatFx.Push("finisher", p, f.Unit, p.EquippedWeapon); fx.Now = true; fx.Hit = true; fx.Killed = true;
            f.Unit.Hp = 0;
            Weapon.OnTargetKilled(p, f.Unit);
            Splash(f.Def.Name.ToUpperInvariant(), "DEFEATED");
            Rt.AddFocus(p, Rt.FocusMax);                                           // Yan: "executing gives slow-mo energy" - Focus full
            GameLog.Write($"You execute {f.Def.Name}. Your Focus surges.");
            if (f.Def == Visor) Story.Set("visor_done");
            return true;
        }

        // ---------------- the Visor's lab: Secret Lab Beta's secret wing (ArtSource/plans/lab_plans.py visor_wing) ----------------
        // ACTION_DESIGN "Melee": "found in a secret room of a Tier 4-5 lab: break a wall, walk a long corridor; it's stuck in a
        // corpse in black armour with a red visor"; Yan: "the ... corpse gets up when you take the hatchet and fights you in melee".

        public const string VisorLab = "secret_lab_beta", VisorMark = "The Black Visor", CrackedWall = "Cracked Wall";
        static readonly HashSet<HumanEnemy> Sleepers = new HashSet<HumanEnemy>();

        /// <summary>A map was entered: the body lies in its room until the fight is won.</summary>
        public static void OnEnterZone(string zoneId)
        {
            Sleepers.Clear();
            if (zoneId != VisorLab || Story.Has("visor_done")) return;
            var l = TerrainMap.Landmarks?.FirstOrDefault(x => x.Name == VisorMark);
            if (l == null) return;
            var body = new HumanEnemy("Body in black armour", l.X, l.Y, "Contractors", "Legend", null, BlackVisorSuit) { Hp = 0 };
            EncounterState.Enemies.Add(body); Sleepers.Add(body);
        }

        /// <summary>E at the cracked wall: it gives - behind it, a corridor nobody was meant to find.</summary>
        public static bool TryBreakWall(Player p)
        {
            var l = TerrainMap.Landmarks?.FirstOrDefault(x => x.Name == CrackedWall && CombatantUtil.Distance(p, x.X, x.Y) <= 1);
            if (l == null || TerrainMap.GetTile(l.X, l.Y).Type != TileType.Solid) return false;
            TerrainMap.SetTileType(l.X, l.Y, TileType.Difficult, "rubble");
            Sfx.Play("door_break", TacticalGridView.GridToWorld(l.X, l.Y));
            Noise.Make(l.X, l.Y, 10, p);
            GameLog.Write("The plaster was hollow. It caves in - behind it, a long dark corridor that isn't on any plan.");
            return true;
        }

        /// <summary>E on the body (before it's looted like any other): you pull the hatchet out, and he gets up.</summary>
        public static bool TryWake(Player p, HumanEnemy corpse)
        {
            if (corpse == null || !Sleepers.Remove(corpse)) return false;
            EncounterState.Enemies.Remove(corpse);
            WakeVisor(p, corpse.X, corpse.Y);
            return true;
        }

        /// <summary>The hatchet comes free in your hand; the visor lights up red; he rises (the Testing Ground calls this too).</summary>
        public static Fight WakeVisor(Player p, int x, int y)
        {
            var d = ShopDatabase.Items.FirstOrDefault(i => i.Type == "weapon" && i.Category == "melee" && i.Key == "TacticalHatchet");
            if (d != null) { p.AddItem(TestingGround.ItemOf(d)); p.EquipWeaponFromInventory(d.Id); }
            GameLog.Write("You work the hatchet loose from the black armour. It comes free with a wet sound.");
            GameLog.Write("Behind the visor, a red light comes on.");
            Story.Cutscene(x + 0.5f, y + 0.5f, 2.4f);
            var f = Spawn(Visor, x, y);
            f.NextMoveAt = Time.time + 2.6f;                                       // he gets up slowly. Then he doesn't do anything slowly.
            return f;
        }

        /// <summary>His body was opened for looting (CorpseLootUI): the clock to the smoke grenade starts.</summary>
        public static void Looted(HumanEnemy body) { var f = Of(body); if (f != null && f.Finished && f.LootedAt < 0f) f.LootedAt = Time.time; }

        public const float SmokeAfter = 3f;

        /// <summary>Every frame: the hit-stop wears off; a looted boss's smoke grenade.</summary>
        public static void TickAll()
        {
            if (_stopUntil >= 0f && Time.unscaledTime >= _stopUntil)
            {
                _stopUntil = -1f;
                if (!Rt.Paused) Time.timeScale = Time.unscaledTime < _slowUntil ? Mathf.Min(MomentScale, Rt.SlowMo ? Rt.SlowScale : 1f) : Rt.SlowMo ? Rt.SlowScale : 1f;
            }
            else if (_stopUntil < 0f && _slowUntil >= 0f && Time.unscaledTime >= _slowUntil)
            {
                _slowUntil = -1f;
                if (!Rt.Paused) Time.timeScale = Rt.SlowMo ? Rt.SlowScale : 1f;
            }
            foreach (var f in Fights.Where(f => f.Finished && !f.Gone && f.LootedAt >= 0f && Time.time - f.LootedAt >= SmokeAfter).ToList())
            {
                f.Gone = true;
                CombatFx.Push("smoke", f.Unit, null, null).Now = true;
                EncounterState.Enemies.Remove(f.Unit);
                GameLog.Write("A smoke grenade hisses out of nowhere. When it clears, the body is gone.");
            }
        }

        // ---------------- hit-stop and the perfect parry's moment ----------------

        static float _stopUntil = -1f, _slowUntil = -1f, _momentAt = -99f;
        public const float MomentStop = 0.14f, MomentSlow = 1.0f, MomentScale = 0.25f;

        /// <summary>A moment of near-frozen time (real seconds): the weight on a parry, a phase change, the finisher.</summary>
        public static void HitStop(float seconds)
        {
            if (Rt.Paused) return;
            Time.timeScale = 0.05f;
            _stopUntil = Mathf.Max(_stopUntil, Time.unscaledTime + seconds);
        }

        /// <summary>A perfect parry: a hit-stop, then a second of slow motion while the camera leans in and a cold edge rings
        /// the screen (Punch drives the zoom and HudView's vignette).</summary>
        public static void ParryMoment()
        {
            HitStop(MomentStop);
            _momentAt = Time.unscaledTime;
            _slowUntil = Time.unscaledTime + MomentStop + MomentSlow;
        }

        /// <summary>0..1: how far into the parry moment's lean-in we are (fast in, held, eased out).</summary>
        public static float Punch
        {
            get
            {
                float t = Time.unscaledTime - _momentAt, len = MomentStop + MomentSlow;
                if (t < 0f || t > len) return 0f;
                return Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((len - t) / 0.45f);
            }
        }
    }
}
