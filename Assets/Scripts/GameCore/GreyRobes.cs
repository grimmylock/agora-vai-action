// "Those" (STORY.md, Yan 2026-10-28/11-18): the robed figures. Tier 1 rules: they are never hostile and never killed; seen from a distance on the marsh roads with a hand-held
// device; they turn away when you come close (they don't hurry, then are gone); shot at, a smoke charge bursts and they are gone (a hit only staggers them); during Pavel's walk back
// they stop and watch from a distance, at the village edge they stand at the edge of the lamps, then leave.
// Faction "Those": hostile to nobody. The unit is a HumanEnemy in the Grey Robe suit (UnitSprites unique suit, ArtSource/make_robes.py), Unkillable, never aggroed.
// ponytail: a team stands put until you come near (they don't wander); sightings are rolled per game minute, one team at a time. They are not in the A-Life (no squad, no home).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class GreyRobes
    {
        public const string Faction = "Those", Suit = "Grey Robe";
        public static readonly ArmorDef RobeSuit = new ArmorDef { Name = Suit, PhysDR = 0.3f, AnomDR = 0.6f, BeltSlots = 1, ModSlots = 0, Mp = 4, WeightKg = 3f };
        public const float TurnAwayRange = 9f, GoneAfter = 9f, WalkStep = 0.6f;

        public static bool Is(ICombatant u) => u is HumanEnemy h && h.FactionName == Faction;

        class Watcher { public HumanEnemy Unit; public bool Hold; public float LeavingSince = -1f, NextStep; }
        static readonly List<Watcher> Team = new List<Watcher>();
        static bool _announced; static float _nextSight = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Team.Clear(); _announced = false; _nextSight = 0f; }

        public static bool Present => Team.Any(w => w.Unit.IsAlive() && EncounterState.Enemies.Contains(w.Unit));

        /// <summary>`n` robed figures round (x, y) (the nearest walkable tiles). hold = they stay until Release (Pavel's walk).</summary>
        public static void Place(int x, int y, int n, bool hold)
        {
            for (int i = 0; i < n; i++)
            {
                var (px, py) = TerrainMap.FindNearestEnterable(x + (i % 2) * 2, y + i, 8);
                var h = new HumanEnemy("Robed One", px, py, Faction, "Legend", null, RobeSuit) { IsAggroed = false, Unkillable = true };
                h.MaxHp = h.Hp = 300; h.Morale = "Steady";
                EncounterState.Enemies.Add(h);
                Team.Add(new Watcher { Unit = h, Hold = hold });
            }
        }

        static Player Nearest(Mutant m) => Player.All.Where(p => p.IsAlive()).OrderBy(p => CombatantUtil.Distance(p, m)).FirstOrDefault();

        /// <summary>From Boss.OnDamaged: any hit and the smoke goes up.</summary>
        public static void OnDamaged(ICombatant u)
        {
            if (!Is(u)) return;
            var w = Team.FirstOrDefault(t => t.Unit == u); if (w == null) return;
            if (u.Hp < w.Unit.MaxHp) { GameLog.Event("A grey cloud bursts where they stood. When it clears, they're gone."); VanishAll(true); }
        }

        static void VanishAll(bool smoke)
        {
            foreach (var w in Team.ToList())
            {
                if (smoke) CombatFx.Push("smoke", w.Unit, null, null).Now = true;
                EncounterState.Enemies.Remove(w.Unit);
            }
            Team.Clear();
        }

        /// <summary>Let the ones holding (Pavel's walk) go: they leave quietly.</summary>
        public static void Release() { foreach (var w in Team) w.Hold = false; foreach (var w in Team) w.LeavingSince = Time.time - GoneAfter + 3f; }

        /// <summary>Every frame (Rt.WorldTick).</summary>
        public static void Tick(float dt)
        {
            if (Team.Count == 0) return;
            float now = Time.time;
            foreach (var w in Team.ToList())
            {
                var u = w.Unit;
                if (!u.IsAlive() || !EncounterState.Enemies.Contains(u)) { Team.Remove(w); continue; }
                u.IsAggroed = false;
                if (u.Hp < u.MaxHp) { OnDamaged(u); break; }
                var p = Nearest(u); if (p == null) continue;
                float d = CombatantUtil.Distance(p, u);
                if (!w.Hold && w.LeavingSince < 0f && d <= TurnAwayRange)
                {
                    w.LeavingSince = now;
                    if (Team.All(t => t.LeavingSince >= 0f)) GameLog.Write("They turn away. They don't hurry. They just leave.");
                }
                if (w.LeavingSince >= 0f)
                {
                    if (now >= w.NextStep) { w.NextStep = now + WalkStep; u.MoveAwayFrom(p); }
                    if (now - w.LeavingSince >= GoneAfter || d > 26f) { EncounterState.Enemies.Remove(u); Team.Remove(w); }
                }
            }
        }

        /// <summary>Per game minute (Tier1Story.Tick): now and then a team on the marsh road, far off, once the fence has shown there is someone about.</summary>
        public static void TickSight(Player p)
        {
            if (Present || p == null || Net.Mode == Net.Role.Client) return;
            if (!Story.IsDone("fence") || Story.IsDone("defense") || Story.Step("defense") == 1) return;
            if (Time.time < _nextSight) return;
            _nextSight = Time.time + Random.Range(240f, 600f);
            if (Random.value > 0.55f) return;
            for (int t = 0; t < 12; t++)
            {
                float ang = Random.value * 6.2832f, r = Random.Range(22f, 34f);
                var (x, y) = TerrainMap.FindNearestEnterable(p.X + Mathf.RoundToInt(Mathf.Cos(ang) * r), p.Y + Mathf.RoundToInt(Mathf.Sin(ang) * r), 4);
                if (TerrainMap.RegionAt(x, y) == "bonfire_village" || System.Math.Max(System.Math.Abs(x - p.X), System.Math.Abs(y - p.Y)) < 18) continue;
                Place(x, y, Random.value < 0.5f ? 2 : 3, false);
                if (!_announced) { _announced = true; GameLog.Write("Robed figures, far off. One is holding something up to the ground, like a detector."); }
                return;
            }
        }
    }
}
