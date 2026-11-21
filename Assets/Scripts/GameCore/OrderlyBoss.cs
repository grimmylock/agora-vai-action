// Boss: the Orderly (Tier 6, the Hospital Morgue; BOSS_CONCEPTS_V3.md 4, BOSS_ROSTER.md 11). REDONE 2026-11-21 from Yan's own ChatGPT drawing (ArtSource/mutants_anim/boss_concepts/gpt/orderly_native.png): an insect made of a hospital patient -
// a long pale torso stretched into a thorax, both arms grown into bone-saw blades held up like a mantis, a black body bag fused over its back like wing cases, six thin legs of forearms, a face on its belly, an IV pole
// with a bag of dark blood growing out of its back. Side on. Drawn from Pixel Lab clips (BossArt key "orderly"). The fight is a heavy duel on Boss.cs:
//  - the Saw (white: parry the sweep), the Chop (red: both blades down: dive or step out), the Grab (red: caught, slammed down and pulled to it);
//  - the Drip: it squeezes the IV bag and mends DripHeal of its health, unless you hurt it by DripBreak of its health while it does it (the bag bursts: no heal, and it bleeds).
//    It can drip DripMax times a fight. A tool for the player (rule 6): stop it, and it is the weaker for the rest of the fight;
//  - phase 2: the Double Saw (white, white) and the Saw Rush (red, a lunge).
// 2026-11-21 (Yan: "a kind of stalker type enemy, that roams the halls of the dungeon and has to be avoided until the final confrontation", "stays in the morgue"): in the Hospital Morgue it is a STALKER until the boiler
// power is back on. It spawns far from you, quietly, and roams the morgue's rooms (patrol: walks slowly, listens at each stop). Yellow "!" over it while its notice fills, red when it has found you. It notices
// you by sight (line of sight, nearer and brighter = faster: crouching and standing in the dark help) and by sound (shots, running footsteps near it); it hunts a little faster than you walk, slower than you sprint,
// loses you after a few seconds without sight, goes to the last place it saw or heard you, and gives up. It takes NO damage and has no boss bar. Caught, it grabs you (the Catch: heavy, slams you down) and then
// retreats for a while. When the boiler switch brings the power back it drops the roaming: the Orderly proper (killable, its moves above) comes for you wherever you are. Co-op: it notices the most visible player.
// NOT built from the brief yet: the lights going out (the cave darkness), the bag as a separate target on its back.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class OrderlyBoss
    {
        public const string Lair = "Autopsy Theatre", Done = "orderly_done", Trophy = "Ward Name Tag";
        public const float WakeRange = 10f, DripHeal = 0.06f, DripBreak = 0.03f; public const int DripMax = 3;
        static readonly Dictionary<Boss.Fight, (int hp, int used, bool burst)> _drip = new Dictionary<Boss.Fight, (int, int, bool)>();

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "The Orderly", Title = "HE IS STILL ON SHIFT", Weapon = "OrderlySaw", Armor = null, Hp = 12000, Faction = "Mutants", Rank = "Legend", DoneFlag = Done, Vanishes = false,
            Art = "orderly", Creature = true, StepSeconds = 0.45f, Recover = 1.0f, Phase2Line = "It tears the drip out of its back. What runs out of the tube is black.",
            Moves = new[]
            {
                new Boss.Move { Name = "Saw", Wind = 0.9f, MaxDist = 2f, Mult = 1.0f, Weight = 3, Poses = new[] { 1 }, Open = 1.4f, Clip = "saw" },
                new Boss.Move { Name = "Chop", Wind = 1.1f, Danger = true, MaxDist = 2f, Mult = 1.6f, Weight = 2, Poses = new[] { 0 }, Open = 2.0f, Clip = "chop" },
                new Boss.Move { Name = "Grab", Wind = 1.0f, Danger = true, MaxDist = 2f, Mult = 1.1f, Effects = new[] { "slab" }, Weight = 2, Open = 1.8f, Clip = "grab" },
                new Boss.Move { Name = "Drip", Wind = 1.6f, MaxDist = 20f, Effects = new[] { "drip" }, Weight = 1, Open = 0.8f, Phase1Only = true, Clip = "drip" },
                new Boss.Move { Name = "Double Saw", Wind = 0.8f, Hits = 2, Gap = 0.6f, MaxDist = 2f, Mult = 0.8f, Weight = 3, Poses = new[] { 1, 1 }, Open = 1.8f, Phase2Only = true, Clip = "saw" },
                new Boss.Move { Name = "Saw Rush", Wind = 1.0f, Danger = true, Lunge = 6f, Mult = 1.4f, Weight = 2, Poses = new[] { 1 }, Open = 2.2f, Phase2Only = true, Clip = "saw" },
            },
        };
        static OrderlyBoss() { Def.Defeated = Defeated; }

        static bool _spawnedHere; static string _map;
        public const string MorgueZone = "hospital_morgue";
        public const float PatrolStep = 0.62f, HuntStep = 0.29f, Sight = 18f, FootstepRange = 6f, GiveUpAfter = 6f, RetreatFor = 9f;   // seconds per tile (walk 2.6 t/s = 0.38, sprint 4.4 = 0.23)
        public static readonly Boss.Move CatchMove = new Boss.Move { Name = "Catch", Wind = 0.7f, Danger = true, MaxDist = 2f, Mult = 0.7f, Effects = new[] { "slab" }, Weight = 0, Open = 1.2f, Clip = "grab" };
        class St { public int Mode; public (int x, int y) Wp; public float NextStep, WaitUntil, StuckAt, LoseAt, RetreatUntil, CatchAt; public (int x, int y)? Heard; public Player Target; public bool Catching; public (int x, int y) LastSeen; public bool HasWp; public int Stuck; }
        // modes: 0 patrol, 1 investigate, 2 hunt, 3 retreat
        static readonly Dictionary<Boss.Fight, St> _st = new Dictionary<Boss.Fight, St>();
        static readonly List<(int x, int y)> _wps = new List<(int, int)>();
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; _drip.Clear(); _st.Clear(); _wps.Clear(); }

        static bool PowerOn() { var k = LabLocks.Key("power"); return k != null && MacroState.Current != null && MacroState.Current.Keys.Contains(k); }

        public static void Tick(float dt)
        {
            if (Net.Mode == Net.Role.Client || Story.Has(Done) || Player.Current == null) return;
            if (_map != TerrainMap.ZoneId) { _map = TerrainMap.ZoneId; _spawnedHere = false; _st.Clear(); _wps.Clear(); }
            Boss.Fights.RemoveAll(f => f.Def == Def && !f.Finished && !EncounterState.Enemies.Contains(f.Unit));
            foreach (var f in Boss.Fights.Where(f => f.Def == Def && f.Stalking && !f.Gone).ToList()) if (PowerOn()) EndStalk(f);   // the lamps come on: the finale
            if (TerrainMap.ZoneId != MorgueZone || _spawnedHere || Boss.Fights.Any(f => f.Def == Def && !f.Gone)) return;
            if (!Player.All.Any(p => p.IsAlive())) return;
            BuildWaypoints();
            if (PowerOn()) { var l = TerrainMap.Landmarks?.FirstOrDefault(m => m.Name == Lair); if (l == null) return; var (x, y) = TerrainMap.FindNearestEnterable(l.X, l.Y); Spawn(x, y, true); _spawnedHere = true; return; }   // power already on: the Orderly proper, at its tables
            SpawnStalker(); _spawnedHere = true;
        }

        /// <summary>The roaming points: the morgue's named rooms (and a few tiles round them), each a walkable tile.</summary>
        static void BuildWaypoints()
        {
            _wps.Clear();
            foreach (var n in new[] { "Morgue Landing", "Cold Room", Lair, "Boiler Room", "Records Room" })
            {
                var l = TerrainMap.Landmarks?.FirstOrDefault(m => m.Name == n); if (l == null) continue;
                var pt = TerrainMap.FindNearestEnterable(l.X, l.Y); if (!_wps.Contains(pt)) _wps.Add(pt);
            }
        }
        /// <summary>For the Testing Ground (no morgue): six walkable points on a ring round (cx, cy).</summary>
        public static void RingWaypoints(int cx, int cy, int r)
        {
            _wps.Clear();
            for (int i = 0; i < 6; i++) { double a = i * Mathf.PI / 3; _wps.Add(TerrainMap.FindNearestEnterable(cx + (int)(Mathf.Cos((float)a) * r), cy + (int)(Mathf.Sin((float)a) * r))); }
        }

        public static Boss.Fight SpawnStalker()
        {
            var p = Player.Current; (int x, int y) best = _wps.Count > 0 ? _wps[0] : (p.X + 12, p.Y);
            int bd = -1; foreach (var w in _wps) { int d = CombatantUtil.Distance(p, w.x, w.y); if (d > bd) { bd = d; best = w; } }   // the farthest room from you
            var (x2, y2) = TerrainMap.FindNearestEnterable(best.x, best.y);
            var f = Boss.Spawn(Def, x2, y2, true); f.Stalking = true; Rt.SetOffset(f.Unit, 0f, 0f); f.Unit.Alert = 0f;
            _st[f] = new St { Mode = 0, WaitUntil = Time.time + 6f, NextStep = Time.time };
            return f;
        }

        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Def, x, y); Rt.SetOffset(f.Unit, 0f, 0f); _drip.Remove(f);
            f.NextMoveAt = Time.time + (intro ? 3.0f : 1.8f);
            if (intro)
            {
                GameLog.Event("Between the two steel tables something unfolds from the ceiling: a patient stretched into an insect, both arms grown into saw blades, a black body bag split open on its back. A bag of dark blood swings on a pole out of its spine, and the blades scream on the tiles.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.4f); Sfx.Play("saw_scrape", TacticalGridView.GridToWorld(x, y), 1f, 0f);
            }
            return f;
        }

        public static string DebugState(Boss.Fight f) { var st = State(f); return $"mode {st.Mode} wp {st.Wp} has {st.HasWp} stuck {st.Stuck} wait {st.WaitUntil - Time.time:0.0} wps {_wps.Count}"; }
        static St State(Boss.Fight f) { if (!_st.TryGetValue(f, out var st)) { st = new St { WaitUntil = Time.time + 3f }; _st[f] = st; } return st; }

        /// <summary>The finale: it stops roaming. The Orderly proper (killable, its own moves) comes for the nearest player.</summary>
        public static void EndStalk(Boss.Fight f)
        {
            f.Stalking = false; f.Unit.IsAggroed = true; f.Unit.Alert = 1f; f.NextMoveAt = Time.time + 2.0f; f.Doing = null; _st.Remove(f);
            Boss.NameCard(Def.Name.ToUpperInvariant(), Def.Title); Sfx.Play("boss_sting", null, 1f, 0f);
            GameLog.Event("The boiler takes. Somewhere in the dark the sawing stops. Then, down the corridor, something starts walking toward you, and it is not slow any more.");
        }

        /// <summary>Noise.Make: a shot or blast. A stalker within hearing goes to look (unless it is hunting or retreating).</summary>
        public static void Heard(int x, int y, int radius, ICombatant source)
        {
            if (!(source is Player)) return;
            foreach (var f in Boss.Fights) { if (f.Def != Def || !f.Stalking || f.Gone) continue; var st = State(f); if (st.Mode >= 2) continue; if (CombatantUtil.Distance(f.Unit, x, y) > radius) continue; st.Mode = 1; st.Heard = TerrainMap.FindNearestEnterable(x, y); st.HasWp = false; f.Unit.Alert = Mathf.Max(f.Unit.Alert, 0.55f); }
        }

        /// <summary>Boss.Tick while it roams and nothing is in progress: senses, then patrol / investigate / hunt / retreat.</summary>
        public static void StalkTick(Boss.Fight f)
        {
            var h = f.Unit; float now = Time.time; if (f.Broken || f.Finished || !h.IsAlive()) return; var st = State(f);
            if (now < f.StaggerUntil) return;
            if (st.Catching) { st.Catching = false; st.Mode = 3; st.RetreatUntil = now + RetreatFor; st.HasWp = false; h.Alert = 0f; h.IsAggroed = false; st.Target = null; }
            // ---- senses
            Player seen = null; float bestGain = 0f; bool sees = false;
            foreach (var p in Player.All)
            {
                if (!p.IsAlive()) continue; int d = CombatantUtil.Distance(h, p); float gain = 0f;
                bool los = d <= Sight && GameServices.Grid != null && GameServices.Grid.CheckLineOfSight(h.X, h.Y, p.X, p.Y);
                if (los)
                {
                    float light = Mathf.Clamp01(Vision.Light(p.X, p.Y)); gain = (1f - d / Sight) * (0.3f + 0.7f * light) * (p.Sneaking ? 0.5f : 1f) * (Rt.Of(p).Moving > 0 ? (Rt.Of(p).Moving > 1 ? 1.3f : 1f) : 0.6f); sees = true;
                    if (d <= 3) gain = Mathf.Max(gain, 0.8f);                                       // at arm's length it knows
                }
                else if (d <= FootstepRange && Rt.Of(p).Moving > 1 && !p.Sneaking) gain = 0.3f;          // running footsteps through a wall
                if (gain > bestGain) { bestGain = gain; seen = p; }
            }
            if (st.Mode == 3 && now < st.RetreatUntil) { }                                          // retreating: it does not notice for a while
            else if (st.Mode == 2) { if (sees && seen != null) { st.Target = seen; st.LoseAt = now + GiveUpAfter; st.LastSeen = (seen.X, seen.Y); } }
            else
            {
                if (bestGain > 0f) { h.Alert = Mathf.Min(1f, h.Alert + bestGain * 1.1f * Time.deltaTime); if (seen != null) st.LastSeen = (seen.X, seen.Y); }
                else h.Alert = Mathf.Max(0f, h.Alert - 0.25f * Time.deltaTime);
                if (h.Alert >= 1f && seen != null) { st.Mode = 2; st.Target = seen; st.LoseAt = now + GiveUpAfter; h.IsAggroed = true; h.SpottedAt = now; Sfx.Play("boss_sting", null, 0.7f, 0f, 1.2f); GameLog.Write("It has found you."); }
            }
            if (st.Mode == 3 && now >= st.RetreatUntil) { st.Mode = 0; st.HasWp = false; st.WaitUntil = now + 2f; }
            // ---- movement / the catch
            if (st.Mode == 2)
            {
                var t = st.Target; if (t == null || !t.IsAlive()) { t = Player.All.Where(p => p.IsAlive()).OrderBy(p => CombatantUtil.Distance(h, p)).FirstOrDefault(); st.Target = t; if (t == null) return; }
                h.SpottedAt = now;
                int dist = CombatantUtil.Distance(h, t);
                if (dist <= 1 && now >= st.CatchAt) { st.CatchAt = now + 4f; st.Catching = true; Boss.Begin(f, CatchMove, now, 1f); return; }
                if (now >= st.LoseAt) { st.Mode = 1; st.Heard = st.LastSeen; st.HasWp = false; h.IsAggroed = false; h.Alert = 0.6f; return; }   // lost you: go to where it last saw you
                if (now >= st.NextStep && dist > 1) { st.NextStep = now + HuntStep; h.MoveToward(t.X, t.Y); }
                return;
            }
            h.IsAggroed = false;
            if (st.Mode == 1 && st.Heard != null)
            {
                var hp = st.Heard.Value; int dd = CombatantUtil.Distance(h, hp.x, hp.y);
                if (dd <= 1) { st.Heard = null; st.WaitUntil = now + 3f; }
                else if (now >= st.NextStep) { st.NextStep = now + PatrolStep * 0.8f; h.MoveToward(hp.x, hp.y); }
                return;
            }
            if (st.Mode == 1) { if (now >= st.WaitUntil) st.Mode = 0; return; }
            // patrol / retreat: walk to a waypoint, listen there, pick the next
            if (now < st.WaitUntil) return;
            if (!st.HasWp || CombatantUtil.Distance(h, st.Wp.x, st.Wp.y) <= 1 || st.Stuck > 12)
            {
                if (st.HasWp && st.Stuck <= 12) st.WaitUntil = now + Random.Range(2f, 4.5f);
                st.Stuck = 0; if (_wps.Count == 0) BuildWaypoints(); if (_wps.Count == 0) return;
                var cands = _wps.Where(w => CombatantUtil.Distance(h, w.x, w.y) > 3).ToList(); if (cands.Count == 0) cands = _wps;
                if (st.Mode == 3) st.Wp = cands.OrderByDescending(w => Player.All.Where(p => p.IsAlive()).Select(p => CombatantUtil.Distance(p, w.x, w.y)).DefaultIfEmpty(0).Min()).First();   // away from you
                else st.Wp = cands[Random.Range(0, cands.Count)];
                st.HasWp = true; if (now < st.WaitUntil) return;
            }
            if (now >= st.NextStep) { st.NextStep = now + PatrolStep; int x0 = h.X, y0 = h.Y; h.MoveToward(st.Wp.x, st.Wp.y); if (h.X == x0 && h.Y == y0) st.Stuck++; else st.Stuck = 0; }
        }

        /// <summary>Boss.Begin of the Drip: remember his health, to see whether the bag bursts.</summary>
        public static void DripBegins(Boss.Fight f) { _drip.TryGetValue(f, out var d); _drip[f] = (f.Unit.Hp, d.used, d.burst); }
        public static int DripsUsed(Boss.Fight f) => _drip.TryGetValue(f, out var d) ? d.used : 0;
        public static bool Burst(Boss.Fight f) => _drip.TryGetValue(f, out var d) && d.burst;
        /// <summary>The Drip's blow: he mends DripHeal of his health, unless he lost DripBreak of it since he began (the bag burst), or he has used it DripMax times.</summary>
        public static void Drip(Boss.Fight f)
        {
            var h = f.Unit; _drip.TryGetValue(f, out var d);
            if (d.burst || d.used >= DripMax) { GameLog.Write("It squeezes the bag. Nothing comes out of it."); return; }
            if (d.hp - h.Hp >= Def.Hp * DripBreak) { _drip[f] = (h.Hp, d.used, true); h.StatusEffects.Bleeding = true; GameLog.Event("The IV bag bursts. Black fluid sprays over the tiles, and it bleeds."); CombatFx.PxAt("acid_splash", h.X, h.Y); return; }
            Sfx.Play("drip_squeeze", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.05f);
            int heal = Mathf.RoundToInt(Def.Hp * DripHeal); h.Hp = Mathf.Min(h.MaxHp, h.Hp + heal); _drip[f] = (h.Hp, d.used + 1, false);
            GameLog.Write("It squeezes the bag on its pole. Dark fluid runs down the tube into it, and it straightens.");
        }

        /// <summary>The Grab's blow: whoever is in front of him is caught, slammed down (knocked down 1.2 s) and pulled to his feet.</summary>
        public static void Slab(Boss.Fight f, Player p, Boss.Move m, int hi)
        {
            var h = f.Unit; if (p == null || h.EquippedWeapon == null || CombatantUtil.Distance(h, p) > 2) return;
            if (Rt.Of(p).Diving || Rt.Untouchable(p)) { GameLog.Write("DODGED!"); CombatFx.Push("dodge", p, h, null).Now = true; return; }
            int hp0 = p.Hp; Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, p, Boss.MultOf(m, hi)); } finally { Parry.Unparryable = false; }
            if (p.Hp < hp0) { Rt.Knockdown(p, 1.2f); Boss.DragTo(h, p, 1.5f); Sfx.Play("hit_hard", null, 1f, 0.03f); GameLog.Write("Thin grey legs close on you, lift you and slam you down on the tiles."); }
        }

        static void Defeated(Player p, HumanEnemy h)
        {
            h.Inventory.Items.Add(new InventoryItem { Id = "orderly_tag", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 0.1f, Price = 4200 });
            Story.Journal("The Orderly: something that had been a patient and had become an insect kept the Hospital Morgue. It is down. Its ward name tag reads a name nobody has said in years.");
            GameLog.Event("Its legs fold one by one. The IV pole tips over and rings on the tiles, and the face on its belly goes on screaming without a sound.");
        }
    }
}
