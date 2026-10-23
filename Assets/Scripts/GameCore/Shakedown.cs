// Batch 72 (Yan chose "Shake you down first"): a Tier 1 Raider gang that spots you doesn't open fire - it calls out
// and demands a road toll: Ru or the gun in your hands. Pay or hand it over and they let you walk (that gang leaves you
// alone for a day); refuse and they shoot first; or shoot first yourself and they lose their first turn (the Batch 69
// surprise round). Each gang asks once. The world still lives by itself: this only changes what a gang that walked
// into you on its own does, it never spawns anyone.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Shakedown
    {
        public const int TollPerMember = 50;
        public const int TruceMinutes = 24 * 60;

        /// <summary>Headless (harness): -1 = no shakedowns (no dialog to show), 0..3 = answer with that choice.</summary>
        public static int TestPick = -1;
        public static bool TestApproach;   // headless: walk over anyway (the dialog then takes its first answer)

        public const int Pay = 0, HandGun = 1, Refuse = 2, ShootFirst = 3;
        public const float SurpriseSeconds = 1.5f;   // RT 7z: real-time "they lose their first turn"

        static int Minute => (int)((MacroState.Current.Day * 24f + MacroState.Current.Hour) * 60f);

        public static OfflineSquad SquadOf(Mutant m) => MacroState.Current.OfflineSquads.FirstOrDefault(q => q.Members.Contains(m));

        /// <summary>A gang that took your toll leaves you alone for a day (EnemyAI.FriendlyToPlayer).</summary>
        public static bool InTruce(Mutant m)
        {
            if (!(m is HumanEnemy)) return false;
            var s = SquadOf(m);
            return s != null && s.TruceUntil > Minute;
        }

        /// <summary>Called the moment a hostile notices you (TurnLoop.CheckVisionAndWander). True = a shakedown started
        /// instead of a fight (the dialog decides what happens next).</summary>
        public static bool TryStart(Mutant spotter, Player p)
        {
            if (!(spotter is HumanEnemy h) || h.FactionName != "Raiders") return false;
            if ((MacroMapDatabase.Find(MacroState.Current.CurrentZoneId)?.Tier ?? 1) > 1) return false;
            if (EncounterState.InCombat || EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed)) return false;
            var squad = SquadOf(h);
            if (squad != null && squad == _pending) return true;   // the boss is still talking
            if (squad != null && squad == _approach) return true;  // already walking over
            if (squad == null || squad.ShakedownDone || !squad.IsHostileToPlayer) return false;
            if (!ChoiceUI.Available && TestPick < 0 && !TestApproach) return false;
            squad.ShakedownDone = true;
            if (TestPick >= 0) return Demand(squad, p);
            // Batch 73 (Yan: "they stayed basically outside of my view while asking me to pay up"): the gang calls out
            // and walks over first; the toll is asked face to face, once their boss is close and in your sight.
            var walkers = squad.Members.OfType<HumanEnemy>().Where(m => m.IsAlive() && EncounterState.Enemies.Contains(m)).ToList();
            if (walkers.Count == 0) return false;
            _approach = squad; _approachSince = Minute;
            GameLog.Write($"[ALERT] Someone shouts at you: \"Hey! Stay right there!\" {walkers.Count} Raiders are walking over.");
            Sfx.Play("alert");
            return true;
        }

        /// <summary>The gang that's walking over to you, or null.</summary>
        static OfflineSquad _approach;
        static int _approachSince;
        public const int TalkRange = 4, GiveUpMinutes = 25, GiveUpRange = 22;

        public static bool Approaching(Mutant m) => _approach != null && _approach.Members.Contains(m);

        /// <summary>Every exploring minute (TurnLoop.TickAwareness): the gang walks toward you, two tiles a minute; the
        /// boss asks once close and seen. Shoot first and it's a fight with their first turn lost (they weren't ready);
        /// get far enough away or wait them out and they give up.</summary>
        public static void Tick(Player p)
        {
            if (_approach == null || p == null) return;
            var gang = _approach.Members.OfType<HumanEnemy>().Where(m => m.IsAlive() && EncounterState.Enemies.Contains(m) && !EnemyAI.IsOut(m)).ToList();
            if (gang.Count == 0 || gang.Any(m => m.IsAggroed) || EncounterState.InCombat) { _approach = null; return; }   // you opened fire, or they're gone
            var boss = gang.OrderByDescending(m => CombatRules.RankIndex(m.Rank)).First();
            int d = CombatantUtil.Distance(boss, p);
            if (d > GiveUpRange || Minute - _approachSince > GiveUpMinutes)
            {
                GameLog.Write($"{boss.Name}'s gang gives up on you.");
                _approach = null; return;
            }
            // Batch 123: only when truly in view (sight worked out) and on screen
            if (d <= TalkRange && Vision.Ready && Vision.Shows(boss) && TacticalGridView.OnScreen(boss.X, boss.Y)) { var sq = _approach; _approach = null; Demand(sq, p); return; }
            foreach (var m in gang)
                for (int step = 0; step < 2 && CombatantUtil.Distance(m, p) > (m == boss ? 2 : 3); step++)
                {
                    m.Mp = System.Math.Max(m.Mp, TurnLoop.WalkMp);
                    if (!m.MoveToward(p)) break;
                }
        }

        /// <summary>The toll, face to face.</summary>
        static bool Demand(OfflineSquad squad, Player p)
        {

            var gang = squad.Members.OfType<HumanEnemy>().Where(m => m.IsAlive() && EncounterState.Enemies.Contains(m)).ToList();
            if (gang.Count == 0) return false;
            var boss = gang.OrderByDescending(m => CombatRules.RankIndex(m.Rank)).First();
            int toll = TollPerMember * gang.Count;
            var gun = p.EquippedWeapon;
            bool hasGun = gun != null && !CombatRules.IsMelee(gun);

            var opts = new List<string>();
            var map = new List<int>();
            if (p.Ru >= toll) { opts.Add($"Pay the toll ({toll} Ru)"); map.Add(Pay); }
            if (hasGun) { opts.Add($"Hand over your {gun.Name}"); map.Add(HandGun); }
            opts.Add("Refuse (they open fire)"); map.Add(Refuse);
            opts.Add(Rt.On ? "Shoot first (they're slow to react)" : "Shoot first (they lose their first turn)"); map.Add(ShootFirst);

            string others = gang.Count > 1 ? $" and {gang.Count - 1} more" : "";
            GameLog.Write($"{boss.Name}{others} stop in front of you, guns up. A shakedown.");
            string body = $"{boss.Name} ({boss.Rank}){others} have you covered.\n\n"
                + $"\"Easy, walker. This road has a toll. {toll} Ru{(hasGun ? $", or that {gun.Name} in your hands" : "")}, and you walk on breathing.\"\n\n"
                + $"You have {p.Ru} Ru.";
            _pending = squad;
            if (TestPick >= 0) { Resolve(map.Contains(TestPick) ? TestPick : Refuse, squad, gang, boss, toll, p); return true; }
            ChoiceUI.Show("ROAD TOLL", body, opts, i => Resolve(map[i], squad, gang, boss, toll, p), boss);
            return true;
        }

        static OfflineSquad _pending;
        public static void Reset() { _pending = null; _approach = null; TestPick = -1; TestApproach = false; }

        static void Resolve(int pick, OfflineSquad squad, List<HumanEnemy> gang, HumanEnemy boss, int toll, Player p)
        {
            _pending = null;
            switch (pick)
            {
                case Pay:
                    p.Ru -= toll;
                    LetGo(squad, gang, $"{boss.Name} counts the {toll} Ru and waves you on. \"Pleasure doing business.\"");
                    break;
                case HandGun:
                    var w = p.EquippedWeapon;
                    p.EquippedWeapon = null; p.EquippedWeaponCategory = null; p.EquippedWeaponKey = null;
                    if (w != null) { w.Owner = boss; boss.EquippedWeapon = w; }   // kill them later and it's yours again
                    p.ApplyCarryEffects();
                    LetGo(squad, gang, $"{boss.Name} takes your {w?.Name} and grins. \"Good choice.\"");
                    break;
                case Refuse:
                    Fight(gang, p);
                    GameLog.Write($"\"Wrong answer.\" {boss.Name}'s gang opens fire!");
                    if (!Rt.On && GameState.Current.CurrentTurn == Turn.Player) TurnLoop.PlayerEndTurn();   // they shoot first (real-time: they're already drawing)
                    break;
                default:
                    Fight(gang, p);
                    EncounterState.Surprised = true;   // Batch 69: they lose their first turn
                    if (Rt.On) foreach (var m in gang) RtAi.Hold(m, SurpriseSeconds);   // RT 7z: in real time "a turn" was never applied - they fired at once
                    p.Ap = System.Math.Max(p.Ap, p.MaxAp); p.Mp = System.Math.Max(p.Mp, p.MaxMp);
                    GameLog.Write($"You go for your gun before {boss.Name} finishes the sentence!{(Rt.On ? "" : " They lose their first turn.")}");
                    break;
            }
        }

        static void LetGo(OfflineSquad squad, List<HumanEnemy> gang, string line)
        {
            squad.TruceUntil = Minute + TruceMinutes;
            foreach (var m in gang) m.IsAggroed = false;
            GameLog.Write(line);
            GameLog.Write("That gang leaves you alone for a day.");
        }

        static void Fight(List<HumanEnemy> gang, Player p)
        {
            foreach (var m in gang) m.IsAggroed = true;
            EncounterState.InCombat = true;
            Contact.Spot(p);
        }
    }
}
