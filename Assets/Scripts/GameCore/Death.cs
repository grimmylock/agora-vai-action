// Yan (2026-10-20): three death states when playing together.
//  1. Downed (Body.cs): crawl about, a friend holding E gets you up.
//  2. Dead: you bleed out (or are finished off) while a friend is still standing. You lose reputation and one equipped item
//     (primary, secondary or armor) wears one step; a friend can bring you back with E, but only out of combat.
//  3. DEAD DEAD: the whole squad is dead. Everyone comes back at the last camp - a bonfire you made camp at (E at the fire), or
//     Bonfire Village by default - each with their armor one step more worn, half the pack gone, and the reputation penalty.
// Solo play still ends at the death screen, as before (a one-man squad wipes the moment you die). Yan: deaths cost rank XP
// (Player.LoseXp: never a rank, never below 0) - no reputation; what leaves your pack lies where you fell (DropPack).
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Death
    {
        public static bool Coop => Player.All.Count > 1;
        public const int DeadXp = 50, WipeXp = 100;                                  // knobs: rank XP lost (toward the next rank)
        public const float ReviveHp = 0.25f;                                         // a dead friend comes back on a quarter of their HP

        /// <summary>Every frame: a player who just died (co-op, friends still up) takes the dead penalty once.</summary>
        public static void Tick()
        {
            if (!Coop) return;
            foreach (var p in Player.All)
            {
                var s = Rt.Of(p);
                if (p.IsAlive()) { s.Dead = false; continue; }
                if (s.Dead) continue;
                s.Dead = true;
                GameLog.Write("A squadmate is dead. Get them back on their feet once the fighting's over (hold E over them).");
                p.LoseXp(DeadXp);
                WearOne(p);
            }
        }

        /// <summary>Out of combat, a friend holding E brings a dead player back (Rt.PlayerTick).</summary>
        public static void Revive(Player p)
        {
            Body.Forget(p);
            p.StatusEffects.Bleeding = false;
            p.HealPermanent(Mathf.Max(1, Mathf.RoundToInt(p.MaxHp * ReviveHp)));
            Rt.Of(p).Dead = false;
            GameLog.Write("Your squadmate gasps back to life.");
        }

        /// <summary>The whole squad is dead: everyone back at the last camp, with the wipe penalties.</summary>
        public static void Wipe()
        {
            var m = MacroState.Current;
            string zone = m.CampZone ?? "bonfire_village";
            GameLog.Write("[SQUAD WIPED] The Zone takes its toll. You come to by the camp fire.");
            foreach (var p in Player.All)
            {
                if (p.EquippedArmor != null && !ItemCondition.IsBroken(p.EquippedArmorCondition)) p.EquippedArmorCondition = ItemCondition.Worse(p.EquippedArmorCondition);
                var pack = p.Inventory.Items.ToList();
                DropPack(p, pack.OrderBy(_ => Random.value).Take(pack.Count / 2).ToList());   // half the pack, where you fell
                p.LoseXp(WipeXp);
                Body.Forget(p); p.StatusEffects.Bleeding = false; p.HealPermanent(p.MaxHp * 2);
                var s = Rt.Of(p); s.Dead = false; s.DiveUntil = s.ProneUntil = s.VaultFrom = -1f; s.UsingId = null;
                p.ApplyCarryEffects();
            }
            Rt.SetSlow(false);
            EncounterState.InCombat = false; EncounterState.ResetFight();
            if (!MacroMapDatabase.SameMap(zone, TerrainMap.ZoneId)) ZoneTravel.EnterZone(zone);
            foreach (var e in EncounterState.Enemies) e.IsAggroed = false;
            var (cx, cy) = m.CampX >= 0 ? (m.CampX, m.CampY) : TerrainMap.ArrivalAnchor(zone) ?? (Player.Current.X, Player.Current.Y);
            int i = 0;
            foreach (var p in Player.All)                                            // round the fire
            {
                var spot = TerrainMap.FindNearestEnterable(cx + (i % 3) - 1, cy + 1 + i / 3); i++;
                p.X = spot.x; p.Y = spot.y; Rt.SetOffset(p, 0f, 0f);
            }
            GameState.Current.CurrentTurn = Turn.Player;
        }

        /// <summary>E at a bonfire: the squad comes back here if it wipes. True if there was a fire to camp at.</summary>
        public static bool TryCamp(Player p)
        {
            bool fire = TerrainMap.GetTile(p.X, p.Y).Look == "campfire"
                || TerrainMap.GetAdjacentCoords(p.X, p.Y).Any(a => TerrainMap.GetTile(a.x, a.y).Look == "campfire");
            if (!fire) return false;
            var m = MacroState.Current;
            m.CampZone = m.CurrentZoneId; m.CampX = p.X; m.CampY = p.Y;
            GameLog.Write("You make camp by the fire. If the whole squad falls, this is where you'll wake.");
            return true;
        }

        // ---- what you drop on a wipe lies where you fell; people passing by help themselves ----
        public class Pack { public int X, Y; public string Zone; public System.Collections.Generic.List<GroundItem> Items = new System.Collections.Generic.List<GroundItem>(); }
        public static readonly System.Collections.Generic.List<Pack> Packs = new System.Collections.Generic.List<Pack>();
        public const int LootSight = 10;                                             // tiles: a passer-by notices a dropped pack

        static void DropPack(Player p, System.Collections.Generic.List<InventoryItem> items)
        {
            if (items.Count == 0) return;
            var pack = new Pack { X = p.X, Y = p.Y, Zone = TerrainMap.ZoneId };
            foreach (var it in items)
            {
                p.Inventory.Items.Remove(it);
                var g = new GroundItem { X = p.X, Y = p.Y, Item = it };
                TerrainMap.GroundItems.Add(g); pack.Items.Add(g);
            }
            Packs.Add(pack);
            GameLog.Write($"Some of your gear is left lying where you fell ({items.Count} item{(items.Count == 1 ? "" : "s")}).");
        }

        /// <summary>Yan: "if enemies are nearby they can loot it, so the player needs to kill them to get them back". Each minute
        /// (TurnLoop.TickAwareness): the nearest hostile person within 10 tiles who isn't fighting walks to a dropped pack and,
        /// on reaching it, takes what's still there into his own inventory - kill him and it's on his body. Mutants don't loot.</summary>
        public static void TickPacks()
        {
            foreach (var pack in Packs.ToList())
            {
                pack.Items.RemoveAll(g => !TerrainMap.GroundItems.Contains(g));      // picked up already (by you or a friend)
                if (pack.Items.Count == 0 || pack.Zone != TerrainMap.ZoneId) { if (pack.Items.Count == 0) Packs.Remove(pack); continue; }
                var taker = EncounterState.Enemies.OfType<HumanEnemy>()
                    .Where(h => h.IsAlive() && !h.IsAggroed && !Companion.Is(h) && !EnemyAI.FriendlyToPlayer(h) && !TestingGround.IsDummy(h)
                        && System.Math.Max(System.Math.Abs(h.X - pack.X), System.Math.Abs(h.Y - pack.Y)) <= LootSight)
                    .OrderBy(h => System.Math.Max(System.Math.Abs(h.X - pack.X), System.Math.Abs(h.Y - pack.Y))).FirstOrDefault();
                if (taker == null) continue;
                if (System.Math.Max(System.Math.Abs(taker.X - pack.X), System.Math.Abs(taker.Y - pack.Y)) > 1) { taker.PatrolTarget = (pack.X, pack.Y); continue; }
                foreach (var g in pack.Items) { TerrainMap.GroundItems.Remove(g); taker.Inventory.Items.Add(g.Item); }
                GameLog.Write($"{taker.Name} goes through the gear you left behind and pockets it.");
                Packs.Remove(pack);
            }
        }

        static void WearOne(Player p)
        {
            var picks = new System.Collections.Generic.List<System.Action>();
            if (p.EquippedWeapon != null && !ItemCondition.IsBroken(p.EquippedWeapon.Condition)) picks.Add(() => p.EquippedWeapon.Condition = ItemCondition.Worse(p.EquippedWeapon.Condition));
            if (p.SecondaryWeapon != null && !ItemCondition.IsBroken(p.SecondaryWeapon.Condition)) picks.Add(() => p.SecondaryWeapon.Condition = ItemCondition.Worse(p.SecondaryWeapon.Condition));
            if (p.EquippedArmor != null && !ItemCondition.IsBroken(p.EquippedArmorCondition)) picks.Add(() => p.EquippedArmorCondition = ItemCondition.Worse(p.EquippedArmorCondition));
            if (picks.Count > 0) picks[Random.Range(0, picks.Count)]();
        }
    }
}
