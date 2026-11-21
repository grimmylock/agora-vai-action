// PARKED 2026-11-21 (Yan, after the painted-rectangle art and then a face-in-the-wall concept: "face in the wall, scrap it, not gonna work"): nothing spawns it (Rt.WorldTick no longer ticks it, no Testing Ground entry); the code and its harness checks stay.
// Boss: the Singer (Tier 2, Valley Mine: the warm wall at the head of the Main Gallery; BOSS_CONCEPTS_V3.md 2, BOSS_ROSTER.md 3a, Yan 2026-11-20: "amazing and disturbing, silent hill style").
// A wall of mine rock and pale flesh that bulges into one huge face, miners' hands on the floor in front of it, two helmet lamps still burning. It never moves (Def.Rooted).
// Drawn from Pixel Lab clips (BossArt key "singer"): a painted wall section 8 x 6 tiles, stood against the gallery's end.
// The fight is Boss.cs with effects of its own:
//  - the Note (red): the mouth opens and the note rolls down the gallery: everyone who can SEE it (line of sight) within NoteRange is hit, knocked down and deafened; get behind
//    the timbers or a cart (cover breaks the line), or dive under it;
//  - the Grasp (red): a hand shoots out and drags you toward the mouth (the Verger's drag, longer); dive;
//  - the Crush (white, close): the hands slam whoever stands at the wall: parry it;
//  - phase 2: the Scream (red, a cone down the gallery: the Howl's code) and the Chorus (two notes).
// Co-op: it aims at the nearest player, the note hits everyone in sight. Host-side only (boss fights are not synced yet).
// NOT built from the brief yet: the lamps as weak points (shoot one: that side goes blind), the face tearing open to a weak point in phase 2.
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class SingerBoss
    {
        public const string Lair = "Miners' Rest", Zone = "valley_mine", Done = "singer_done", Trophy = "Singing Stone";
        public const float WakeRange = 12f, NoteRange = 14f, GraspRange = 8f, GraspDrag = 4f;

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "The Singer", Title = "THE WARM WALL", Weapon = "SingerHands", Armor = null, Hp = 5200, Faction = "Mutants", Rank = "Veteran", DoneFlag = Done, Vanishes = false,
            Art = null, Creature = true, Rooted = true,   // (parked: its art was scrapped) StepSeconds = 9f, Recover = 1.0f, Phase2Line = "The face splits wider than a face can. The whole gallery hums.",
            Moves = new[]
            {
                new Boss.Move { Name = "Note", Wind = 1.2f, Danger = true, MaxDist = NoteRange, Mult = 0.8f, Effects = new[] { "note" }, Weight = 3, Open = 1.6f, Clip = "note" },
                new Boss.Move { Name = "Grasp", Wind = 1.0f, Danger = true, MaxDist = GraspRange, Mult = 0.9f, Effects = new[] { "grasp" }, Weight = 2, Open = 1.4f, Clip = "grasp" },
                new Boss.Move { Name = "Crush", Wind = 0.8f, MaxDist = 2f, Mult = 1.2f, Weight = 3, Open = 1.2f, Clip = "grasp" },
                new Boss.Move { Name = "Scream", Wind = 1.1f, Danger = true, MaxDist = 9f, Effects = new[] { "howl" }, Weight = 2, Open = 1.8f, Phase2Only = true, Clip = "scream" },
                new Boss.Move { Name = "Chorus", Wind = 1.0f, Hits = 2, Gap = 1.4f, MaxDist = NoteRange, Mult = 0.7f, Effects = new[] { "note", "note" }, RedHits = new[] { true, true }, Weight = 2, Open = 2.0f, Phase2Only = true, Clip = "note" },
            },
        };
        static SingerBoss() { Def.Defeated = Defeated; }

        static bool _spawnedHere;
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; }

        /// <summary>Rt.WorldTick: it starts singing when a player comes near the warm wall.</summary>
        public static void Tick(float dt)
        {
            if (Net.Mode == Net.Role.Client || TerrainMap.ZoneId != Zone || Story.Has(Done) || Player.Current == null) return;
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
            f.NextMoveAt = Time.time + (intro ? 3.0f : 1.8f);
            if (intro)
            {
                GameLog.Event("The note you have been hearing since the adit stops. At the head of the gallery the wall is warm, and it has a face.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.4f);
            }
            return f;
        }

        /// <summary>The Note's blow: everyone within NoteRange who can see the wall (line of sight) and is not diving is hit (unparryable), knocked down and deafened. Cover breaks the line.</summary>
        public static void Note(Boss.Fight f, Boss.Move m, int hi)
        {
            var h = f.Unit; CombatFx.PxAt("thunderclap", h.X, h.Y); Sfx.Play("singer_note", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.02f); CombatFx.Push("stagger", h, null, null).Now = true;
            if (h.EquippedWeapon == null) return; int n = 0;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                if (CombatantUtil.Distance(h, q) > NoteRange) continue;
                if (GameServices.Grid != null && !GameServices.Grid.CheckLineOfSight(h.X, h.Y, q.X, q.Y)) continue;          // behind the timbers: the note goes past
                if (Rt.Of(q).Diving || Rt.Untouchable(q)) { CombatFx.Push("dodge", q, h, null).Now = true; continue; }
                Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, q, Boss.MultOf(m, hi)); } finally { Parry.Unparryable = false; }
                Body.Concuss(q, 1.5f); Rt.Knockdown(q, CombatantUtil.HasPerk(q, "Tumbler") ? 0.4f : 0.8f); n++;
            }
            GameLog.Write(n > 0 ? "The note goes through you like a train. Get behind something." : "The note rolls down the gallery and over your cover.");
        }

        /// <summary>The Grasp's blow: a hand shoots out at the nearest player within GraspRange: unless diving, hit and dragged GraspDrag tiles toward the wall.</summary>
        public static void Grasp(Boss.Fight f, Player p, Boss.Move m, int hi)
        {
            var h = f.Unit; if (p == null || h.EquippedWeapon == null || CombatantUtil.Distance(h, p) > GraspRange) return;
            if (Rt.Of(p).Diving || Rt.Untouchable(p)) { GameLog.Write("DODGED!"); CombatFx.Push("dodge", p, h, null).Now = true; return; }
            int hp0 = p.Hp; Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, p, Boss.MultOf(m, hi)); } finally { Parry.Unparryable = false; }
            if (p.Hp < hp0) { Boss.DragTo(h, p, GraspDrag); GameLog.Write("A grey hand closes round your leg and hauls you toward the wall."); }
        }

        static void Defeated(Player p, HumanEnemy h)
        {
            h.Inventory.Items.Add(new InventoryItem { Id = "singer_stone", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 3f, Price = 2400 });
            Story.Journal("The Singer: the warm wall at the head of the Valley Mine's gallery had a face, and sang one note on the eleven-second beat. It is quiet now. A piece of it still hums in your hand.");
            GameLog.Event("The face goes slack. The note stops, and the gallery is only cold rock again. One of the stones by the mouth is still warm, and still humming.");
        }
    }
}
