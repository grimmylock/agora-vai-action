// Ported from script.js: playerEndTurn(). This is the master enemy-AI turn loop — vision/aggro
// checks, ambient patrol wandering, per-behavior tactics (ranged/ambush/berserk), morale/fleeing,
// and finally handing the turn back to the player. Calls to updateUI()/render() from the original
// are omitted (see the UI wiring notes at the end of this response) — this is the pure game-state
// version; a MonoBehaviour can call TurnLoop.PlayerEndTurn() and then refresh its own UI/visuals
// afterward.
using System.Linq;
using GameData;

namespace GameCore
{
    public static class TurnLoop
    {
        /// <summary>Ported from playerEndTurn().</summary>
        public static void PlayerEndTurn()
        {
            if (GameState.Current.CurrentTurn != Turn.Player) return;

            GameState.Current.CurrentTurn = Turn.Mutant;
            GameLog.Write("Player ends turn. Mutant turn begins.");
            if (EncounterState.InCombat) Atmosphere.Advance(Atmosphere.MinutesPerStep);   // a combat round is a minute too

            var player = Player.Current;
            player.EndTurn();
            var grid = GameServices.RequireGrid();
            EnemyAI.BeginEnemyRound(player);
            GarrisonRaid.Round(player);   // Batch 87
            if (EncounterState.Surprised) GameLog.Write("SURPRISE! They never saw you coming and lose their turn.");
            try
            {

                foreach (var enemy in EncounterState.Enemies.ToList())
                {
                    if (!enemy.IsAlive()) continue;
                    if (Companion.Is(enemy)) { Companion.Act(player, grid, true); continue; }   // Batch 69: your partner

                    var target = TargetSelection.GetNearestHostile(enemy, new[] { player });
                    if (target == null) { NpcFights.Act(enemy, grid); continue; }   // Batch 39: friendly to you, maybe not to others

                    // 1 & 2. Vision/aggro check + ambient wander - shared with TickAwareness() below,
                    // which runs this same check once per tile during free exploration.
                    CheckVisionAndWander(enemy, player, grid);
                    if (!enemy.IsAggroed) continue; // wandered instead of fighting this round
                    if (EncounterState.Surprised) continue;   // Batch 69: caught off guard, they only take cover in their heads

                    if (Companion.TakesFire(enemy, player, grid)) continue;   // Batch 69: your partner is the closer target

                    // 3. Combat (file 04 roles, morale, mutant behaviors) - see EnemyAI.
                    EnemyAI.TakeTurn(enemy, player);
                    if (!player.IsAlive()) break;
                    Perks.TryAdrenaline(player);
                }

            }
            catch (System.Exception e)
            {
                // one broken hook mustn't leave CurrentTurn stuck on the enemy - log it and carry on
                UnityEngine.Debug.LogException(e);
                GameLog.Write("[ERROR] Something went wrong during the enemy turn - see the Console.");
            }
            EncounterState.Surprised = false;
            if (EncounterState.InCombat && player.IsAlive()) Contact.EndRound(player);   // Batch 62
            CheckCombatEnd();
            if (GameState.Current.CurrentTurn == Turn.CombatOver) return;
            if (EncounterState.InCombat) RadioForHelp();

            player.StartTurn();
            AnomalyEngine.ProcessTurnStart(player);
            if (!player.IsAlive()) { CheckCombatEnd(); return; }   // bled out at the start of the turn
            GameState.Current.CurrentTurn = Turn.Player;
            GameLog.Write("Player turn begins. AP and MP refreshed.");
        }

        /// <summary>New: resolved once, the instant an NPC's morale actually breaks (see
        /// CheckMorale's doc comment - "parting shot, drop-and-run"). First a free parting shot if
        /// the player still has a loaded ranged weapon in range (no AP cost - the NPC is turning
        /// to run, not standing still to trade), then - if they're still alive after that - a
        /// chance they drop their weapon and any loose gear in the panic (their worn armor stays
        /// on their back; they're still alive and running, not a corpse - that's the
        /// includeWornArmor:false HumanLootSystem already exposes for exactly this case).</summary>
        public static void ResolveFleeReactions(HumanEnemy npc, Player player)
        {
            var weapon = player.EquippedWeapon;
            bool isRanged = weapon?.Db != null && !string.IsNullOrEmpty(weapon.Db.AmmoType);
            int range = weapon != null ? weapon.MaxReach : 1;

            // player.Ap >= 1 isn't spent by this shot (it's free) - it's a readiness gate: only a
            // player who didn't dump every last point of AP on their own turn still has a round
            // "in the chamber" to react with here.
            if (player.Ap >= 1 && weapon != null && weapon.IsLoaded && isRanged && CombatantUtil.Distance(player, npc) <= range)
            {
                GameLog.Write($"{npc.Name} breaks and runs - free parting shot!");
                weapon.Fire(npc, "Single", (int)player.AgilityMod + CombatRules.RankMod(player.Rank));
            }

            if (!npc.IsAlive()) return; // the parting shot may have finished them

            // Batch 48: no d20. They throw down what slows them: a heavy weapon, an empty one, or everything if they're
            // badly hurt. It lands on the ground where they stood (walk over it to pick it up), not in your pack.
            var w = npc.EquippedWeapon;
            bool heavy = CombatRules.Category(w?.Db) == "heavy", empty = w != null && !CombatRules.IsMelee(w) && !w.IsLoaded, hurt = npc.Hp * 4 <= npc.MaxHp;
            if (w != null && (heavy || empty || hurt))
            {
                GameLog.Write($"{npc.Name} throws down their gear to run faster!");
                HumanLootSystem.DropOnGround(npc);
            }
        }

        /// <summary>Vision/aggro check + idle wander for one enemy - no AP/MP refresh, no combat
        /// resolution. Shared by PlayerEndTurn's full round and by TickAwareness's lighter
        /// per-tile check.</summary>
        public const int MutualSpotExempt = 3;
        static readonly System.Collections.Generic.HashSet<Mutant> Unaware = new System.Collections.Generic.HashSet<Mutant>();   // squads you've been told about (once each)

        /// <summary>MP an NPC gets for one walking step out of a fight: a diagonal through a bush or a door costs 3 since
        /// Batch 102 (it was 2, and walkers froze in brush and marsh).</summary>
        public const float WalkMp = 6f;   // Batch 125: enough for any one step - the dearest is a diagonal climb through brush, (2 + 2 high) x 1.5 (a Blind Maw stood stuck at a bank, retrying every minute)

        private static void CheckVisionAndWander(Mutant enemy, Player player, IGridService grid)
        {
            if (Shakedown.Approaching(enemy)) return;   // Batch 73: walking over to ask for the toll (Shakedown.Tick moves them)
            if (!enemy.IsAggroed)
            {
                int distToPlayer = CombatantUtil.Distance(enemy, player);
                // Batch 74 (Yan: "raiders spotting me from outside of the screen"): seeing is mutual - an enemy only spots
                // you from somewhere you can see it too (in your sight and light), or from right next to you. The camera then
                // eases out to show it (TacticalGridView.FollowCamera). Nobody opens a fight from the dark or off screen.
                bool mutual = Vision.Shows(enemy) || distToPlayer <= MutualSpotExempt;
                if (Vision.Shows(enemy) && enemy is HumanEnemy hh && !EnemyAI.FriendlyToPlayer(enemy) && Unaware.Add(enemy))
                {
                    // Batch 127 (Yan: "i have yet to win a player vs human enemies fight"): the surprise round was there but nothing said
                    // it was on offer. One line when you first see a squad that hasn't seen you.
                    foreach (var o in EncounterState.Enemies.OfType<HumanEnemy>()) if (o.FactionName == hh.FactionName && CombatantUtil.Distance(o, enemy) <= 12) Unaware.Add(o);
                    GameLog.Write($"{hh.FactionName} ahead. They haven't seen you - shoot first for a surprise round, or sneak (N) round them.");
                    PlayerController.StopWalk();   // the route stops here: walking on would walk into their sight
                }
                float sight = Contact.IdleSight(enemy, player);
                bool sees = mutual && distToPlayer <= sight && !EnemyAI.FriendlyToPlayer(enemy) && grid.CheckLineOfSight(enemy.X, enemy.Y, player.X, player.Y);
                if (!Contact.Notice(enemy, player, sees, distToPlayer, sight)) return;   // Batch 127c: not yet - it stops and looks your way
                if (sees)
                {
                    if (Shakedown.TryStart(enemy, player)) return;   // Batch 72: a Tier 1 Raider gang wants its toll first
                    enemy.IsAggroed = true;
                    EncounterState.InCombat = true;
                    Contact.Spot(player);
                    GameLog.Write($"[ALERT] {enemy.Name} has spotted the Player!");
                    enemy.SpottedAt = UnityEngine.Time.time;
                    Banter.Spotted(enemy);   // Batch 129: "SOMEONE'S HERE!"
                }
            }
            if (!enemy.IsAggroed && NpcFights.Act(enemy, grid)) return;   // Batch 39: busy fighting another faction
            if (enemy.IsAggroed)
            {
                // aggroed on spawn (Hazard 4+) or still angry from your last visit: that's a fight
                if (EncounterState.IsActive(enemy) && !EncounterState.InCombat) { EncounterState.InCombat = true; Contact.Spot(player); }
                return;
            }

            // Batch 67 (Yan: "Npc's just move erratica around the map and do not seem to have a purpose, also waaaay too fast"):
            // each unit used to pick its own random spot within 5 tiles and walk 2 tiles a minute, twice your pace. Now a squad
            // moves as one: its leader walks to a spot 8-20 tiles off at half your pace, rests there 20-90 minutes, then moves
            // on; the others keep a loose ring around the leader. A squad leaving the zone still walks to its exit.
            var M = MacroState.Current;
            int minute = (int)((M.Day * 24f + M.Hour) * 60f);
            var exitTo = SquadManager.LeavingExit(enemy);        // Batch 37: a squad on its way out walks to its exit
            var squad = M.OfflineSquads.FirstOrDefault(q => q.Members.Contains(enemy));
            var lead = squad?.Members.FirstOrDefault(m => m.IsAlive() && EncounterState.Enemies.Contains(m)) ?? enemy;
            bool follower = lead != enemy && exitTo == null;
            if (exitTo != null) enemy.PatrolTarget = exitTo;
            else if (follower)
            {
                int slot = squad.Members.IndexOf(enemy);
                double a = slot * 2.4;                            // spread round the leader
                if (CombatantUtil.Distance(enemy, lead) <= 2) return;   // close enough: stand about
                enemy.PatrolTarget = TerrainMap.FindNearestEnterable(
                    System.Math.Min(System.Math.Max(0, lead.X + (int)System.Math.Round(System.Math.Cos(a) * 2)), TerrainMap.Width - 1),
                    System.Math.Min(System.Math.Max(0, lead.Y + (int)System.Math.Round(System.Math.Sin(a) * 2)), TerrainMap.Height - 1));
            }
            else if (enemy.PatrolTarget == null || CombatantUtil.Distance(enemy, enemy.PatrolTarget.Value.x, enemy.PatrolTarget.Value.y) == 0)
            {
                if (enemy.RestUntil == 0) { enemy.RestUntil = minute + enemy.RestFor; enemy.PatrolTarget = null; }
                if (minute < enemy.RestUntil) return;             // resting where it got to
                enemy.RestUntil = 0;
                // Batch 110: the squad's task picks the next stop (its home post, a patrol loop, a spot to scavenge, a post to take);
                // a map without landmarks keeps the old stroll 8-20 tiles off
                var stop = Posts.NextStop(squad, enemy, minute, out enemy.RestFor);
                int d = UnityEngine.Random.Range(8, 21); double ang = UnityEngine.Random.value * 6.283;
                int tx = stop?.x ?? enemy.X + (int)(System.Math.Cos(ang) * d), ty = stop?.y ?? enemy.Y + (int)(System.Math.Sin(ang) * d);
                // a walkable tile - a target inside a wall was never reached, so never replaced
                enemy.PatrolTarget = TerrainMap.FindNearestEnterable(
                    System.Math.Min(System.Math.Max(0, tx), TerrainMap.Width - 1), System.Math.Min(System.Math.Max(0, ty), TerrainMap.Height - 1));
                // the map overhaul: nobody wanders into a Safe Zone that isn't theirs (Bonfire Village has Wanderers only)
                var pt = enemy.PatrolTarget.Value;
                if (!SquadManager.MayWalkInto(squad?.Faction, pt.x, pt.y)) { enemy.PatrolTarget = null; enemy.RestUntil = minute + 30; return; }
            }
            // the leader walks at half your pace; real time: the followers too - the clock ticks 4.2 times a second there, so a tile
            // every tick was 4.2 tiles/s, faster than you walk (2.6) and twice their leader (Yan: "running REEEEALLY fast")
            if ((!follower || Rt.On) && exitTo == null && (minute + CombatantUtil.StableId(enemy)) % 2 != 0) return;

            // Ambient wander gets its own small top-up rather than relying on the combat Mp pool,
            // which only ever gets refreshed for enemies that reach the aggroed combat branch.
            enemy.Mp = System.Math.Max(enemy.Mp, WalkMp);   // enough for a bush or a door; still one step
            if (enemy.PatrolTarget != null && CombatantUtil.Distance(enemy, enemy.PatrolTarget.Value.x, enemy.PatrolTarget.Value.y) > 0
                && !enemy.MoveToward(enemy.PatrolTarget.Value.x, enemy.PatrolTarget.Value.y)) enemy.PatrolTarget = null;   // one tile a minute at most
            if (exitTo != null && CombatantUtil.Distance(enemy, exitTo.Value.x, exitTo.Value.y) <= 1) SquadManager.StepOut(enemy);
        }

        /// <summary>New: lighter-weight than a full PlayerEndTurn - just the vision/aggro + idle-
        /// wander pass for every enemy in the zone, no AP/MP refresh and no combat resolution.
        /// Called once per tile the player walks during free exploration (see
        /// PlayerController.WalkToward) so enemies can notice you - or wander past you - in near
        /// real time instead of only reacting in a batch whenever your MP happens to run out.
        /// Getting spotted here doesn't hand the enemy a free attack; it still needs an actual
        /// PlayerEndTurn() (Space, or running out of MP) to act on it.</summary>
        public static void TickAwareness()
        {
            if (GameState.Current.CurrentTurn != Turn.Player) return;
            var player = Player.Current;
            var grid = GameServices.Grid;
            if (player == null || grid == null) return;
            Stashes.Tick();   // Batch 126
            Banter.Tick(player);   // Batch 129: squads you can see talk, friends say hi: stashes move every 7 days; a locked chest's key finds a carrier

            foreach (var enemy in EncounterState.Enemies.ToList())
            {
                if (Companion.Is(enemy)) Companion.Act(player, grid, false);   // Batch 69: follows you, holds fire while you sneak
                else if (enemy.IsAlive() && !TestingGround.IsDummy(enemy)) CheckVisionAndWander(enemy, Rt.TargetOf(enemy, player), grid);   // co-op: the nearest player
            }

            Shakedown.Tick(player);                         // Batch 73
            Death.TickPacks();                              // co-op: passers-by loot a dropped pack
            AnomalyEngine.DetectorSweep(player);
            ZoneTravel.SenseHiddenWays();                   // Batch 50
            QuestSystem.SenseClues();                       // Batch 50
            Atmosphere.Advance(Atmosphere.MinutesPerStep * (player.Sneaking && EncounterState.InCombat ? 2 : 1));   // Batch 31: a step is a minute (Batch 66: two sneaking; Batch 67: exploring, this is the clock's minute and sneaking costs time by walking slower)
            // Out of combat there are no turns (Batch 59) - a full move's worth of tiles counts as a round for the
            // Surge and for your own upkeep (bleeding, healing over time, smoke); enemies already react tile by tile above.
            if (!EncounterState.InCombat && ++_exploreTiles >= System.Math.Max(1, player.MaxMp))
            {
                _exploreTiles = 0;
                if (MacroState.Current.SurgeRounds > 0) WorldEvents.TickSurge();
                player.EndTurn(); player.StartTurn();
                AnomalyEngine.ProcessTurnStart(player);
                if (!player.IsAlive()) CheckCombatEnd();
            }
        }

        private static int _exploreTiles;

        /// <summary>Batch 59: a fight that drags on gets the enemy on the radio. After 3 rounds per enemy it started
        /// with, the leader of the biggest human squad still fighting calls for help; if they're still up and in the
        /// fight at the end of the next round, the call goes out (SquadManager.CallReinforcements). Once a fight.</summary>
        public static void RadioForHelp()
        {
            var es = EncounterState.Enemies;
            if (EncounterState.FightRounds++ == 0) EncounterState.FightStartCount = es.Count(e => EncounterState.IsActive(e) && e.IsAggroed);
            if (EncounterState.HelpCalled) return;
            var caller = EncounterState.Caller;
            if (caller != null)
            {
                EncounterState.Caller = null;
                if (EnemyAI.IsOut(caller) || caller.Morale != "Steady") { GameLog.Write($"{caller.Name}'s call for help is cut off."); EncounterState.NextCall = EncounterState.FightRounds + 3; return; }   // someone else tries in 3 rounds
                EncounterState.HelpCalled = true;
                int n = SquadManager.CallReinforcements(caller.FactionName, MacroState.Current.CurrentZoneId, caller.X, caller.Y);
                GameLog.Write(n > 0 ? $"[RADIO] {caller.Name} got through - {caller.FactionName} backup is coming." : $"[RADIO] {caller.Name} calls for help, but nobody answers.");
                return;
            }
            if (EncounterState.FightRounds < System.Math.Max(EncounterState.NextCall, 3 * System.Math.Max(1, EncounterState.FightStartCount))) return;
            var side = es.OfType<HumanEnemy>().Where(h => !EnemyAI.IsOut(h) && h.IsAggroed && h.Morale == "Steady").GroupBy(h => h.FactionName).OrderByDescending(g => g.Count()).FirstOrDefault();
            if (side == null) return;
            var lead = EnemyAI.Leader(side.First());
            EncounterState.Caller = lead != null && lead.Morale == "Steady" ? lead : side.First();
            EncounterState.CallAt = UnityEngine.Time.time;
            GameLog.Write($"[ALERT] {EncounterState.Caller.Name} is yelling into the radio for backup!");
        }

        /// <summary>
        /// New: nothing previously ended an encounter on victory (only player death was handled,
        /// and only checked at end-of-turn) — a fight you won just sat there forever. Call this
        /// after anything that might have killed the last combatant on either side: here at
        /// end-of-turn, and from PlayerController right after the player's attack so a win is
        /// recognized immediately instead of requiring an extra End Turn press.
        /// </summary>
        /// <summary>Every player is dead (solo: you are).</summary>
        public static bool AllDead => Player.All.Count == 0 ? Player.Current == null || Player.Current.Hp <= 0 : Player.All.All(pl => pl.Hp <= 0);

        public static void CheckCombatEnd()
        {
            if (GameState.Current.Mode != GameMode.Tactical) return;
            if (GameState.Current.CurrentTurn == Turn.CombatOver) return;

            var player = Player.Current;
            if (player != null && AllDead && Death.Coop) { Death.Wipe(); return; }   // co-op: DEAD DEAD - everyone back at the camp
            if (player != null && AllDead)                           // co-op (Yan): "game over only if everyone dies"
            {
                GameLog.Write("Player has fallen. Combat ends.");
                GameState.Current.CurrentTurn = Turn.CombatOver;
                return;
            }

            // Victory: a fight was on and nobody hostile is left standing - dead, fled or surrendered.
            // Corpses (and surrendered NPCs) stay on the map so they can still be looted; anyone
            // who ran is gone.
            if (EncounterState.InCombat && !EncounterState.AnyActive())
            {
                EncounterState.InCombat = false;
                EncounterState.ResetFight();
                EncounterState.Enemies.RemoveAll(e => e.IsAlive() && e.IsFleeing());
                // Stay right here on the map and hand control straight back - there's no separate
                // "Overworld" to fall back to anymore, so unlike the player-death case above,
                // CombatOver can't be left standing (PlayerController blocks all input unless
                // CurrentTurn == Player, which would otherwise freeze exploration the instant you
                // win a fight). The defeated squad members are already marked dead in place (see
                // SquadManager), so re-entering this zone later won't respawn them.
                GameLog.Write("All hostiles down. Area secured.");
                player.StartTurn();
                GameState.Current.CurrentTurn = Turn.Player;
                QuestSystem.CheckClearZone();   // a fight can end by surrender or flight, not just a kill
            }
        }
    }
}
