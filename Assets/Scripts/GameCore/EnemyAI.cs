// Enemy turns, on file 04's rules. TurnLoop handles vision/aggro; once an enemy is fighting,
// TakeTurn runs its whole turn here.
//
// Humans: morale (Steady > Shaken > Broken: flee or surrender), a Combat Role picked from their
// weapon (Anchor: assault/heavy/sniper; Rusher: shotgun/SMG; Flanker: sidearm/melee) and one
// Leader per squad (highest rank). Each turn they score the tiles they can reach - cover against
// the player, line of sight, range, flanking - walk to the best one, then shoot. Anchors favour
// bursts and aim with spare AP, Leaders command and rally, Professional+ carry a frag and throw it
// at a player who sits on the same cover for two rounds.
// Mutants: pack/swarm close in together (Pack Tactics), ambushers cloak/strike/re-cloak, ranged
// psi mutants keep 3-5 tiles away, berserkers charge; Dread Prowler leaps, Blight Brute slams.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class EnemyAI
    {
        // Shaken thresholds by rank (file 04); Broken is RankDef.MoraleHPPercent. Legend: never.
        private static readonly Dictionary<string, int> ShakenPercent = new Dictionary<string, int>
        {
            ["Rookie"] = 75, ["Experienced"] = 55, ["Professional"] = 50, ["Veteran"] = 45,
            ["Expert"] = 40, ["Master"] = 35, ["Legend"] = 0,
        };

        private static readonly HashSet<string> ExtraThrowableFactions = new HashSet<string> { "Garrison", "Sentinels", "Contractors", "IZC", "Obelisk" };

        // per-faction squad leader at the start of the fight, to notice when it goes down
        private static readonly Dictionary<string, HumanEnemy> _leaders = new Dictionary<string, HumanEnemy>();
        private static readonly HashSet<string> _leaderLost = new HashSet<string>();

        /// <summary>This squad's leader has gone down or fled this fight (Intimidate advantage).</summary>
        public static bool LeaderLost(HumanEnemy h) => _leaderLost.Contains(h.FactionName);
        // how many enemy rounds the player has sat on the same cover tile (frag trigger)
        private static (int x, int y) _playerSpot = (-1, -1);
        private static int _playerSpotRounds;

        public static void TakeTurn(Mutant enemy, Player player)
        {
            if (enemy is HumanEnemy h) HumanTurn(h, player);
            else MutantTurn(enemy, player);
        }

        /// <summary>Called once per enemy round (before anyone acts) to track the player's camping.</summary>
        public static void BeginEnemyRound(Player player)
        {
            if ((player.X, player.Y) == _playerSpot && IsCoverSpot(player.X, player.Y)) _playerSpotRounds++;
            else { _playerSpot = (player.X, player.Y); _playerSpotRounds = IsCoverSpot(player.X, player.Y) ? 1 : 0; }
        }

        private static bool IsCoverSpot(int x, int y)
        {
            if (TerrainMap.CoverAcAt(x, y) > 0) return true;
            foreach (var a in TerrainMap.GetAdjacentCoords(x, y)) if (TerrainMap.CoverAcAt(a.x, a.y) > 0) return true;
            return false;
        }

        // =============================================================== humans

        /// <summary>A surrendered NPC (file 04): take their gear, interrogate them (Intelligence or
        /// Strength DC 12, once - a stash lead or a true rumor), or let them go (+rep, except
        /// Raiders and Obelisk). Right-click is still an execution.</summary>
        public static void HandlePrisoner(HumanEnemy h, Player p)
        {
            var options = new List<string> { "Take their gear" };
            if (!h.Interrogated) options.Add("Interrogate (Int or Str, DC 12)");
            options.Add("Let them go");
            ChoiceUI.Show("Surrender", "\"Don't shoot! I'm done, I'm done.\" Hands up, weapon on the ground. (Right-click executes instead.)", options, i =>
            {
                string pick = options[i];
                if (pick.StartsWith("Take")) CorpseLootUI.Open(h);
                else if (pick.StartsWith("Interrogate")) Interrogate(h, p);
                else LetGo(h, p);
            }, h);
        }

        public static void Interrogate(HumanEnemy h, Player p)
        {
            h.Interrogated = true;
            int roll = Random.Range(1, 21) + System.Math.Max(p.IntelligenceMod, p.StrengthMod);
            GameLog.Write($"You lean on {h.Name}: {roll} vs DC 12.");
            if (roll < 12) { GameLog.Write($"{h.Name} gives you nothing but a sullen stare."); return; }
            var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            var hidden = zone?.HiddenConnections?.FirstOrDefault(id => !MacroState.Current.DiscoveredHiddenNodes.Contains(id));
            if (hidden != null && Random.value < 0.5f)
            {
                MacroState.Current.Rumors.Insert(0, $"Day {MacroState.Current.Day}: {h.Name} swore there's a way into {MacroMapDatabase.Find(hidden)?.Name} from {zone.Name}. (true)");
                GameLog.Write($"\"There's a way into {MacroMapDatabase.Find(hidden)?.Name} from here. I swear it.\" (F to search for it)");
                return;
            }
            // Batch 48: they give up their own people - where the nearest other squad of their faction is and what it
            // carries - instead of a stash item rolled out of nowhere. It goes in your rumors, marked true.
            var crew = SquadManager.NearestSquad(h.FactionName, MacroState.Current.CurrentZoneId, h);
            if (crew == null) { GameLog.Write($"\"I'm all that's left of us.\" {h.Name} has nothing more to give."); return; }
            var loot = crew.Members.OfType<HumanEnemy>().Where(x => x.IsAlive()).SelectMany(x => x.Inventory.Items).OrderByDescending(i => i.Price).FirstOrDefault();
            string where = MacroMapDatabase.Find(crew.ZoneId)?.Name ?? crew.ZoneId;
            string tip = $"{h.Name} says {crew.Members.Count(x => x.IsAlive())} of their {h.FactionName} are in {where}{(loot != null ? $", carrying a {loot.Name}" : "")}.";
            MacroState.Current.Rumors.Insert(0, $"Day {MacroState.Current.Day}: {tip} (true)");
            GameLog.Write(tip);
        }

        public static void LetGo(HumanEnemy h, Player p)
        {
            h.Morale = "Broken";
            h.State = "fleeing";
            h.IsAggroed = false;
            if (h.FactionName != "Raiders" && h.FactionName != "Obelisk") p.AdjustReputation(h.FactionName, Player.RepPoint);
            WorldEvents.NoteNpc($"{h.Name} ({h.FactionName} {h.Rank})", "Grateful", "you let them go");
            GameLog.Write($"You wave {h.Name} off. They don't wait for you to change your mind.");
        }

        /// <summary>File 00 §12: a Friendly faction gives safe passage - its people don't attack on sight.</summary>
        public static bool FriendlyToPlayer(Mutant e) =>
            e is HumanEnemy h && Player.Current != null
            && (Player.Current.GetRepLevel(h.FactionName) == "Friendly"
                || (!GameConstants.NeutralAttackOnSight && Player.Current.GetRepLevel(h.FactionName) == "Neutral")
                || Shakedown.InTruce(h));   // Batch 72: a gang you paid off

        public static bool IsOut(HumanEnemy h) => !h.IsAlive() || h.IsFleeing() || h.Morale == "Surrendered";

        public static IEnumerable<HumanEnemy> Squad(HumanEnemy h) =>
            EncounterState.Enemies.OfType<HumanEnemy>().Where(o => o.FactionName == h.FactionName
                && (o == h || o.IsAggroed == h.IsAggroed) && CombatantUtil.Distance(o, h) <= 12);   // ponytail: proximity stands in for real squad membership

        public static HumanEnemy Leader(HumanEnemy h)
        {
            var squad = Squad(h).ToList();
            if (squad.Count < 2) return null;
            return squad.Where(o => !IsOut(o)).OrderByDescending(o => CombatRules.RankIndex(o.Rank)).FirstOrDefault();
        }

        public static string Role(HumanEnemy h)
        {
            if (Leader(h) == h) return "Leader";
            switch (CombatRules.Category(h.EquippedWeapon?.Db))
            {
                case "assault": case "heavy": case "sniper": return "Anchor";
                case "shotgun": case "smg": return "Rusher";
                default: return "Flanker";
            }
        }

        /// <summary>An ally dying where a squadmate can see it shakes that squadmate (file 04). Batch 127 (Yan chose "tactics that break
        /// squads"): or within 4 tiles of it, seen or not - they hear it.</summary>
        public static void OnHumanKilled(HumanEnemy dead)
        {
            var grid = GameServices.Grid;
            if (grid == null) return;
            foreach (var o in Squad(dead))
                if (o != dead && !IsOut(o) && (CombatantUtil.Distance(o, dead) <= 4 || grid.CheckLineOfSight(o.X, o.Y, dead.X, dead.Y))) Shake(o, $"sees {dead.Name} go down");
        }

        /// <summary>Batch 57: a head-shot kill. Squadmates within 8 tiles are shaken, seen or not; anyone who
        /// was already shaken before the shot breaks.</summary>
        public static void Execution(HumanEnemy dead, List<HumanEnemy> shakenBefore, Player player)
        {
            foreach (var o in Squad(dead).ToList())
            {
                if (o == dead || IsOut(o) || CombatantUtil.Distance(o, dead) > 8) continue;
                if (shakenBefore.Contains(o)) Break(o, player);
                else Shake(o, $"hears {dead.Name} drop from a head shot");
            }
        }

        public static void Shake(HumanEnemy h, string why)
        {
            if (h.Rank == "Legend" || h.Morale != "Steady") return;
            h.Morale = "Shaken";
            GameLog.Write($"{h.Name} {why} and is SHAKEN (-2 to hit, -1 AP, falls back).");
        }

        /// <summary>Breaks an NPC. Batch 48: decided by the situation, not a d20 - it surrenders when it can't get away
        /// (you're within 3 tiles, or it's too hurt to run: a quarter of its HP or less) and runs otherwise. Legends
        /// always run.</summary>
        public static void Break(HumanEnemy h, Player player)
        {
            if (h.Morale == "Surrendered" || h.IsFleeing()) return;
            bool cornered = player != null && CombatantUtil.Distance(h, player) <= 3 || h.Hp * 4 <= h.MaxHp;
            if (h.Rank == "Legend" || !cornered)
            {
                h.Morale = "Broken";
                h.State = "fleeing";
                GameLog.Write($"{h.Name} ({h.FactionName} {h.Rank}) BREAKS and runs!");
                TurnLoop.ResolveFleeReactions(h, player);
                WorldEvents.NoteNpc($"{h.Name} ({h.FactionName} {h.Rank})", "Hostile", "fled from you");
            }
            else
            {
                h.Morale = "Surrendered";
                GameLog.Write($"{h.Name} ({h.FactionName} {h.Rank}) throws down their weapon and SURRENDERS. Click them to take their gear.");
                // their weapon, armor and pack go into the lootable stash, same as a corpse's
                var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
                HumanLootSystem.GrantDrop(h, player, zone?.Tier ?? 1, includeWornArmor: true);
                StashGrenades(h);
                h.EquippedWeapon = null;
                WorldEvents.NoteNpc($"{h.Name} ({h.FactionName} {h.Rank})", "Wary", "surrendered to you");
            }
        }

        /// <summary>Unused grenades drop as loot (file 04).</summary>
        public static void StashGrenades(HumanEnemy h)
        {
            if (!string.IsNullOrEmpty(h.ExtraThrowable) && ConsumableDatabase.All.TryGetValue(h.ExtraThrowable, out var extra))
                h.Inventory.Items.Add(new InventoryItem { Id = extra.Id, Name = extra.Name, Type = "consumable", Qty = 1, WeightKg = 0.5f, Price = extra.Price });
            h.ExtraThrowable = null;
            if (h.Frags <= 0) return;
            h.Inventory.Items.Add(new InventoryItem { Id = "fragGrenade", Name = "Frag Grenade", Type = "consumable", Qty = h.Frags, WeightKg = 0.5f, Price = 250 });
            h.Frags = 0;
        }

        private static void UpdateMorale(HumanEnemy h, Player player)
        {
            // leader down or fled -> the whole squad is shaken, once; the next in rank takes over
            string f = h.FactionName;
            if (!_leaders.TryGetValue(f, out var lead) || lead == null || !EncounterState.Enemies.Contains(lead))
            {
                _leaders[f] = Leader(h);                    // a new fight
                _leaderLost.Remove(f);
            }
            else if (IsOut(lead))
            {
                foreach (var o in Squad(h)) if (o != lead && !IsOut(o)) Shake(o, "loses their leader");
                _leaderLost.Add(f);
                _leaders[f] = Leader(h);
            }

            if (h.Morale == "Surrendered" || h.IsFleeing()) return;
            int pct = h.MaxHp > 0 ? h.Hp * 100 / h.MaxHp : 100;
            int broken = h.RankData?.MoraleHPPercent ?? 50;
            if (pct <= broken) { Break(h, player); return; }
            // crossing the Shaken line shakes them once; a Rally afterwards sticks
            if (!h.HpShaken && ShakenPercent.TryGetValue(h.Rank, out var shaken) && pct <= shaken)
            {
                h.HpShaken = true;
                Shake(h, "is badly hurt");
            }
        }

        private static void DrainMp(Mutant u)
        {
            if (u.StatusEffects.PendingMpDrain <= 0) return;
            u.Mp = System.Math.Max(0, u.Mp - u.StatusEffects.PendingMpDrain);
            GameLog.Write($"{u.Name} is still reeling: -{u.StatusEffects.PendingMpDrain} MP.");
            u.StatusEffects.PendingMpDrain = 0;
        }

        private static void HumanTurn(HumanEnemy h, Player player)
        {
            var grid = GameServices.RequireGrid();
            h.Ap = CombatRules.NpcAp(h);   // Batch 77: 4 for every rank; bosses follow the player's rule
            h.Mp = h.TurnMp();
            h.NewTurn();   // Batch 86: per-turn perk counters
            DrainMp(h);
            AnomalyEngine.ProcessTurnStart(h);
            if (!h.IsAlive()) return;

            UpdateMorale(h, player);
            if (h.Morale == "Surrendered") return;
            if (h.Morale == "Shaken") h.Ap = System.Math.Max(0, h.Ap - 1);   // Batch 127: keeping their head down costs them a shot
            if (h.IsFleeing())
            {
                while (h.Mp > 0 && h.IsAlive() && h.MoveAwayFrom(player)) { }
                return;
            }

            if (h.StatusEffects.Prone && h.Ap >= 1)
            {
                h.Ap -= 1;
                h.StatusEffects.Prone = false;
                GameLog.Write($"{h.Name} gets back up.");
            }

            string role = Role(h);
            var weapon = h.EquippedWeapon;
            int range = weapon != null ? weapon.MaxReach : 1;                 // Batch 74: can shoot out to twice its range (normal rolls only inside it)

            // Leader: Command (free, once a round, unless suppressed) and Rally (2 AP).
            if (role == "Leader")
            {
                var shaken = Squad(h).FirstOrDefault(o => o != h && !IsOut(o) && o.Morale == "Shaken" && CombatantUtil.Distance(h, o) <= 6);
                if (shaken != null && h.Ap >= 4)
                {
                    h.Ap -= 2;
                    shaken.Morale = "Steady";
                    GameLog.Write($"{h.Name} rallies {shaken.Name} - steady again.");
                }
                if (!h.StatusEffects.Suppressed)
                {
                    var ally = Squad(h).Where(o => o != h && !IsOut(o)).OrderBy(o => CombatantUtil.Distance(o, player)).FirstOrDefault();
                    if (ally != null && ally.StatusEffects.AimBonus == 0)
                    {
                        ally.StatusEffects.AimBonus = 2;
                        GameLog.Write($"{h.Name} commands {ally.Name}: +2 on their next shot.");
                    }
                }
            }

            // Frag: Professional+ carry one; thrown at a player who has sat on the same cover for
            // two enemy rounds (file 04), from outside the blast.
            if (h.Frags > 0 && h.Ap >= 2 && _playerSpotRounds >= 2 && CombatantUtil.Distance(h, player) <= 8
                && CombatantUtil.Distance(h, player) > Explosions.ThrowRadius + 1 && grid.CheckLineOfSight(h.X, h.Y, player.X, player.Y))
            {
                h.Frags--;
                h.Ap -= 2;
                _playerSpotRounds = 0;
                Explosions.Throw(h, ConsumableDatabase.All["fragGrenade"], player.X, player.Y);
                if (!player.IsAlive()) return;
            }

            // Extra throwable (file 04, disciplined factions): smoke to cover a Shaken retreat,
            // a flashbang at a player who's closed to within 4 tiles.
            if (h.ExtraThrowable != null && h.Ap >= 2 && grid.CheckLineOfSight(h.X, h.Y, player.X, player.Y))
            {
                int dist = CombatantUtil.Distance(h, player);
                if (h.ExtraThrowable == "smokeGrenade" && h.Morale == "Shaken")
                {
                    h.Ap -= 2;
                    Explosions.Throw(h, ConsumableDatabase.All["smokeGrenade"], h.X, h.Y);
                    h.ExtraThrowable = null;
                }
                else if (h.ExtraThrowable == "flashbang" && dist <= 4 && dist > Explosions.ThrowRadius + 1)
                {
                    h.Ap -= 2;
                    Explosions.Throw(h, ConsumableDatabase.All["flashbang"], player.X, player.Y);
                    h.ExtraThrowable = null;
                    if (!player.IsAlive()) return;
                }
            }

            // Batch 62: lost you? go look where you were last seen
            if (!Contact.Knows(h, player)) { Search(h); if (!Contact.Sees(h, player)) return; Contact.Spot(player); }

            // Move to the best tile this role can reach.
            var best = PickTile(h, player, role, range);
            WalkTo(h, best);
            if (!Props.KickAtFoe(h, player)) Props.FlipForCover(h, player);   // Batch 103: roll a barrel up to you to shoot, or tip a desk for cover

            // Shoot with what's left.
            bool canBurst = role != "Flanker" && CombatRules.CanBurst(weapon);
            int guard = 0;
            while (h.Ap >= 2 && h.IsAlive() && player.IsAlive() && guard++ < 6)
            {
                bool inRange = weapon != null && CombatantUtil.Distance(h, player) <= range;
                if (!inRange || !grid.CheckLineOfSight(h.X, h.Y, player.X, player.Y))
                {
                    if (weapon == null && CombatantUtil.IsAdjacent(h, player)) { h.Ap -= 2; h.MeleeAttack(player); continue; }
                    break;
                }
                // Anchor/Leader with spare AP: aim first (1 AP), then a single aimed shot.
                if ((role == "Anchor" || role == "Leader") && h.Ap == 3 && h.StatusEffects.AimBonus == 0)
                {
                    h.Ap -= 1;
                    h.StatusEffects.AimBonus = 2;
                    GameLog.Write($"{h.Name} takes aim.");
                }
                int before = h.Ap;
                bool burst = canBurst && h.Ap >= CombatRules.BurstApCost(weapon) && weapon.CurrentMagazine >= CombatRules.BurstRounds(weapon)
                    && (role == "Anchor" || CombatantUtil.Distance(h, player) <= 4);
                h.Attack(player, burst ? "Burst" : "Single");
                if (h.Ap == before) break;
            }
            h.StatusEffects.Suppressed = false;
        }

        // ------------------------------------------------------------- tile choice

        private static Dictionary<(int x, int y), float> Reachable(Mutant u)
        {
            var grid = GameServices.RequireGrid();
            var cost = new Dictionary<(int x, int y), float> { [(u.X, u.Y)] = 0 };
            var q = new Queue<(int x, int y)>();
            q.Enqueue((u.X, u.Y));
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var n in TerrainMap.GetAdjacentCoords(c.x, c.y))   // Batch 102: diagonals too
                {
                    if (!grid.CanEnterTile(n.x, n.y) || TerrainMap.UnitAt(n.x, n.y, u) != null) continue;
                    float nc = cost[c] + grid.StepCost(u, c.x, c.y, n.x, n.y);
                    if (nc > u.Mp) continue;
                    if (cost.TryGetValue(n, out var old) && old <= nc) continue;
                    cost[n] = nc;
                    q.Enqueue(n);
                }
            }
            return cost;
        }

        private static (int x, int y) PickTile(HumanEnemy h, Player player, string role, int range)
        {
            var grid = GameServices.RequireGrid();
            var options = Reachable(h);
            int curDist = CombatantUtil.Distance(h, player);
            (int x, int y) best = (h.X, h.Y);
            float bestScore = float.MinValue;
            foreach (var kv in options)
            {
                var t = kv.Key;
                int dist = System.Math.Max(System.Math.Abs(t.x - player.X), System.Math.Abs(t.y - player.Y));
                bool los = grid.CheckLineOfSight(t.x, t.y, player.X, player.Y);
                bool inRange = dist <= range;
                var myCover = CoverUtil.GetCoverAt(player.X, player.Y, t.x, t.y);          // me, if the player shoots back
                var theirCover = CoverUtil.GetCoverAt(t.x, t.y, player.X, player.Y);       // the player, from here
                bool flank = theirCover.AcBonus == 0;
                float s = -kv.Value * 0.3f + (los && dist <= System.Math.Max(1, h.EquippedWeapon?.RangeTiles ?? 1) ? 3 : 0);           // don't wander for nothing; Batch 74: inside its range it shoots straighter
                switch (h.Morale == "Shaken" ? "Shaken" : role)
                {
                    case "Shaken":                                                           // fall back to cover, never advance
                        if (dist < curDist) continue;
                        s += myCover.AcBonus * 5 + dist * 0.8f + (los ? 0 : 3);
                        break;
                    case "Rusher":
                        s += (inRange && los ? 12 : 0) + (flank && los ? 6 : 0) + myCover.AcBonus * 2 - dist * 0.6f;
                        break;
                    case "Flanker":
                        s += (inRange && los ? 8 : 0) + (flank && los ? 10 : 0) + myCover.AcBonus * 2 - dist * 0.3f;
                        break;
                    default:                                                                 // Anchor / Leader: hold good cover at range
                        s += (inRange && los ? 12 : 0) + myCover.AcBonus * (role == "Leader" ? 6 : 4) + (dist >= 4 ? 2 : -3);
                        if (!(inRange && los)) s -= dist * 0.5f;   // Batch 90: no shot from here - work closer (they used to sit out of reach in cover forever)
                        // cover nearly shot through? find fresh cover
                        if (t == (h.X, h.Y) && myCover.HasTile && TileRules.MaxIntegrity(TerrainMap.GetTile(myCover.TileX, myCover.TileY))
                            - TerrainMap.GetTile(myCover.TileX, myCover.TileY).Damage <= 1) s -= 8;
                        break;
                }
                if (dist <= 1 && role != "Flanker" && CombatRules.Category(h.EquippedWeapon?.Db) != "shotgun") s -= 4; // not point blank
                if (s > bestScore) { bestScore = s; best = t; }
            }
            return best;
        }

        /// <summary>Batch 62: walk to where the player was last seen, then sweep the tiles around it.</summary>
        private static void Search(Mutant u)
        {
            var ls = Contact.LastSeen;
            var t = u.PatrolTarget;
            if (t == null || (u.X, u.Y) == t.Value || System.Math.Max(System.Math.Abs(t.Value.x - ls.x), System.Math.Abs(t.Value.y - ls.y)) > 6)
                u.PatrolTarget = CombatantUtil.Distance(u, ls.x, ls.y) > 1 ? ls : TerrainMap.FindNearestEnterable(
                    System.Math.Min(System.Math.Max(0, ls.x + UnityEngine.Random.Range(-5, 6)), TerrainMap.Width - 1),
                    System.Math.Min(System.Math.Max(0, ls.y + UnityEngine.Random.Range(-5, 6)), TerrainMap.Height - 1));
            WalkTo(u, u.PatrolTarget.Value);
        }

        private static void WalkTo(Mutant u, (int x, int y) dest)
        {
            int guard = 0;
            while ((u.X, u.Y) != dest && u.Mp > 0 && u.IsAlive() && guard++ < 20)
                if (!u.MoveToward(dest.x, dest.y)) break;
        }

        // =============================================================== mutants

        private static void MutantTurn(Mutant m, Player player)
        {
            var grid = GameServices.RequireGrid();
            m.Ap = m.Db?.Ap ?? 4;
            m.Mp = m.Db?.Mp ?? 4;
            if (m.StatusEffects.Wound == "Leg") m.Mp = System.Math.Max(1, m.Mp - 2);   // Batch 57: aimed leg shots
            DrainMp(m);
            AnomalyEngine.ProcessTurnStart(m);
            if (!m.IsAlive()) return;
            m.CheckPanic();

            // Stagger: 40%+ of max HP lost since its last turn costs a non-berserk mutant this turn.
            bool staggered = m.Db?.Behavior != "berserk" && m.DamageTakenThisRound * 10 >= m.MaxHp * 4;
            m.DamageTakenThisRound = 0;
            if (staggered) { GameLog.Write($"{m.Name} is staggered and loses its turn."); return; }

            if (m.IsFleeing())
            {
                while (m.Mp > 0 && m.IsAlive() && m.MoveAwayFrom(player)) { }
                return;
            }
            if (m.StatusEffects.Prone) { m.StatusEffects.Prone = false; m.Ap = System.Math.Max(0, m.Ap - 1); }
            if (!Contact.Knows(m, player)) { Search(m); if (!Contact.Sees(m, player)) return; Contact.Spot(player); }   // Batch 62

            int dist = CombatantUtil.Distance(m, player);
            string behavior = m.Db?.Behavior ?? "pack";

            // Dread Prowler: circles at 3-5, then Leap Attack (2 AP + all MP) from up to MP tiles.
            if (m.Db != null && m.Db.Leap && m.StatusEffects.Wound != "Leg")   // Batch 57: a hobbled Prowler can't leap
            {
                if (dist >= 2 && dist <= m.Mp && m.Ap >= 2 && LeapAttack(m, player)) { m.StalkRounds = 0; return; }
                // Batch 61: circling only works if a leap line opens up; behind a wall or up a ledge it never does
                if (m.StalkRounds++ < 2) Circle(m, player, 3, 5);
                else Approach(m, player.X, player.Y, false);
                if (m.Ap >= 2 && m.IsInEngageRange(player)) { m.Ap -= 2; m.Attack(player); m.StalkRounds = 0; }
                return;
            }

            // Batch 130 (Yan: "if he can't reach the player in 3 turns he throws a boulder he rips of the ground, if the player is hit it
            // loses half his mp next turn"): three turns of charging without reaching you, and the fourth it throws instead.
            if (m.Db != null && m.Db.GroundSlam)
            {
                if (CombatantUtil.IsAdjacent(m, player)) m.ChaseTurns = 0;
                else if (m.ChaseTurns >= BoulderAfterTurns && m.Ap >= 2 && dist <= BoulderRange && grid.CheckLineOfSight(m.X, m.Y, player.X, player.Y))
                { ThrowBoulder(m, player); m.ChaseTurns = 0; return; }
                else m.ChaseTurns++;
            }

            // Blight Brute: Ground Slam once per encounter when it gets someone adjacent.
            if (m.Db != null && m.Db.GroundSlam && !m.SlamUsed)
            {
                Approach(m, player.X, player.Y, packMate: false);
                if (CombatantUtil.IsAdjacent(m, player) && m.Ap >= 2) { GroundSlam(m, player); return; }
            }

            switch (behavior)
            {
                case "ranged":
                    // keep 3-5 tiles away with line of sight, psi from up to 5
                    if (dist < 3 || dist > 5 || !grid.CheckLineOfSight(m.X, m.Y, player.X, player.Y)) Circle(m, player, 3, 5);
                    break;
                case "ambush":
                    // cloaked: close in only if it can reach striking distance this turn, otherwise
                    // stay 4+ tiles out (the cloak only holds there)
                    var adj = BestAdjacent(m, player, false);
                    if (adj.HasValue) { WalkTo(m, adj.Value); m.StalkRounds = 0; }
                    else if (m.IsCloaked && m.StalkRounds++ < 2) Circle(m, player, 4, 6);   // Batch 61: it used to circle forever when
                    else Approach(m, player.X, player.Y, false);                            // the only way to you was longer than its MP
                    break;
                default:
                    Approach(m, player.X, player.Y, behavior == "pack" || behavior == "swarm");
                    break;
            }

            if (m.Ap >= 2 && m.IsInEngageRange(player))
            {
                m.Ap -= 2;
                m.Attack(player);
            }

            // ambushers slip away and re-cloak if they can get 4+ tiles from everyone
            if (m.ActiveCamouflage && m.IsAlive() && !m.IsFleeing())
            {
                while (m.Mp > 0 && CombatantUtil.Distance(m, player) < 4 && m.MoveAwayFrom(player)) { }
                if (CombatantUtil.Distance(m, player) >= 4 && !m.IsCloaked)
                {
                    m.IsCloaked = true;
                    GameLog.Write($"{m.Name} shimmers and vanishes again.");
                }
            }
        }

        /// <summary>A free tile next to the target this mutant can reach with its MP (pack
        /// animals prefer one next to a packmate, for Pack Tactics).</summary>
        private static (int x, int y)? BestAdjacent(Mutant m, ICombatant target, bool packMate)
        {
            var options = Reachable(m);
            (int x, int y)? best = null;
            float bestScore = float.MinValue;
            foreach (var kv in options)
            {
                var t = kv.Key;
                if (System.Math.Max(System.Math.Abs(t.x - target.X), System.Math.Abs(t.y - target.Y)) != 1) continue;
                float s = -kv.Value;
                if (packMate && EncounterState.Enemies.Any(o => o != m && o.IsAlive() && System.Math.Max(System.Math.Abs(o.X - t.x), System.Math.Abs(o.Y - t.y)) <= 1)) s += 5;
                if (s > bestScore) { bestScore = s; best = t; }
            }
            return best;
        }

        private static void Approach(Mutant m, int tx, int ty, bool packMate)
        {
            if (CombatantUtil.IsAdjacent(m, tx, ty)) return;
            var adj = BestAdjacent(m, new Probe(tx, ty), packMate);
            if (adj.HasValue) { WalkTo(m, adj.Value); return; }
            int guard = 0;
            while (m.Mp > 0 && !CombatantUtil.IsAdjacent(m, tx, ty) && guard++ < 20)
                if (!m.MoveToward(tx, ty)) break;
        }

        /// <summary>Moves to a reachable tile between min and max tiles from the target, keeping LoS.</summary>
        private static void Circle(Mutant m, ICombatant target, int min, int max)
        {
            var grid = GameServices.RequireGrid();
            var options = Reachable(m);
            (int x, int y) best = (m.X, m.Y);
            float bestScore = float.MinValue;
            foreach (var kv in options)
            {
                var t = kv.Key;
                int d = System.Math.Max(System.Math.Abs(t.x - target.X), System.Math.Abs(t.y - target.Y));
                float s = -kv.Value * 0.2f - (d < min ? (min - d) * 4 : d > max ? (d - max) * 2 : 0);
                if (grid.CheckLineOfSight(t.x, t.y, target.X, target.Y)) s += 3;
                if (s > bestScore) { bestScore = s; best = t; }
            }
            WalkTo(m, best);
        }

        private static bool LeapAttack(Mutant m, Player player)
        {
            var spot = TerrainMap.GetAdjacentCoords(player.X, player.Y)
                .Where(p => TerrainMap.CanEnterTile(p.x, p.y) && TerrainMap.UnitAt(p.x, p.y, m) == null
                    && GameServices.RequireGrid().CheckLineOfSight(m.X, m.Y, p.x, p.y))
                .OrderBy(p => CombatantUtil.Distance(m, p.x, p.y)).Cast<(int x, int y)?>().FirstOrDefault();
            if (!spot.HasValue) return false;
            GameLog.Write($"{m.Name} crouches low... and LEAPS!");
            m.Ap -= 2;
            m.Mp = 0;
            m.X = spot.Value.x;
            m.Y = spot.Value.y;
            // Telegraph & dodge (file 04): with an AP to spare, the player throws themselves aside.
            if (player.Ap >= 1)
            {
                player.Ap -= 1;
                int roll = Random.Range(1, 21) + (int)player.AgilityMod;
                if (roll >= m.MutantCheckDC)
                {
                    GameLog.Write($"You dive aside (Agility {roll} vs {m.MutantCheckDC}) - the leap misses, and you'll have the advantage on it.");
                    player.HasLeapDodgeAdvantage = true;
                    return true;
                }
                GameLog.Write($"You try to dodge (Agility {roll} vs {m.MutantCheckDC}) - too slow.");
            }
            m.LeapAdvantage = true;
            m.Attack(player);
            m.LeapAdvantage = false;
            return true;
        }

        public const int BoulderAfterTurns = 3, BoulderRange = 10, BoulderDamage = 25;

        /// <summary>Batch 130: the Blight Brute rips a boulder out of the ground and hurls it (its whole turn). 2d10 + 3 against your AC
        /// (it arcs over cover); a hit does 25 and you start your next turn on half MP.</summary>
        public static void ThrowBoulder(Mutant m, Player player)
        {
            m.Ap = 0; m.FaceToward(player.X, player.Y);
            int a = Random.Range(1, 11), b = Random.Range(1, 11), total = a + b + 3;
            GameLog.Write($"{m.Name} rips a boulder out of the ground and HURLS it! (2d10 {a}+{b} +3 = {total} vs AC {player.Ac})");
            Sfx.Play("hit_concrete", null, 1f, 0.06f, 0.55f);   // a deep crunch: the concrete hit pitched way down
            if (total < player.Ac) { GameLog.Write("The boulder smashes down beside you."); return; }
            int dmg = CombatRules.PhysicalDamage(BoulderDamage, "Standard", player.PhysDR);
            player.Hp = System.Math.Max(0, player.Hp - dmg);
            player.StatusEffects.HalfMpNextTurn = true;
            GameLog.Write($"The boulder hits you for {dmg}. You'll be reeling: half MP next turn.");
            CombatRules.CheckWound(player, dmg, false);
        }

        private static void GroundSlam(Mutant m, Player player)
        {
            m.SlamUsed = true;
            m.Ap -= 2;
            GameLog.Write($"{m.Name} rears up and SLAMS the ground!");
            var victims = new List<ICombatant> { player };
            victims.AddRange(EncounterState.Enemies.OfType<HumanEnemy>().Where(e => e.IsAlive()));
            foreach (var v in victims)
            {
                if (!CombatantUtil.IsAdjacent(m, v) || !v.IsAlive()) continue;
                int dmg = CombatRules.PhysicalDamage(30, "Standard", v.PhysDR);
                v.Hp = System.Math.Max(0, v.Hp - dmg);
                int roll = Random.Range(1, 21) + (int)v.AgilityMod;
                string who = v is Player ? "Player" : v.Name;
                GameLog.Write($"{who} takes {dmg} from the slam.");
                if (roll < 13) { v.StatusEffects.Prone = true; GameLog.Write($"{who} is knocked prone (Agility {roll} vs 13)."); }
                CombatRules.CheckWound(v, dmg, false);
                if (!v.IsAlive() && v is HumanEnemy) Weapon.OnTargetKilled(m, v);
            }
        }

        /// <summary>Just a coordinate that looks like a unit, for helpers that take one.</summary>
        private class Probe : ICombatant
        {
            public Probe(int x, int y) { X = x; Y = y; }
            public string Name => "spot";
            public int X { get; set; }
            public int Y { get; set; }
            public int Ap { get; set; }
            public float Mp { get; set; }
            public int MaxMp { get; set; }
            public float PhysDR { get; set; }
            public float AnomDR { get; set; }
            public int Ac { get; set; }
            public int Hp { get; set; } = 1;
            public bool IsAlive() => true;
            public List<string> ActivePerks { get; } = new List<string>();
            public string Faction => "None";
            public StatusEffects StatusEffects { get; } = new StatusEffects();
            public float AgilityMod => 0;
            public int PendingApDrain { get; set; }
            public float ArtifactAvoidanceBonus => 0;
            public bool ArtifactAutoPassAnomalies => false;
            public object ActiveDetector => null;
        }
    }
}
