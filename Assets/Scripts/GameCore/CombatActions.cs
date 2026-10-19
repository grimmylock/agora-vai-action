// Batch 72 (Yan: "buttons for stuff in the bottom of the screen where the player can choose which action to take similar
// to xcom, with icons"): every player action as one entry - its icon, hotkey, AP cost, when it can be used and what it
// does. HudView.ActionBar draws them as buttons; the hotkeys in PlayerController still work the same.
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public class ActionDef
    {
        public string Id, Icon, Key, Name, Desc;
        public bool CombatOnly, ExploreOnly;
        public Func<Player, bool> Show = p => true;           // hidden entirely (a perk you don't have)
        public Func<Player, int> Ap = p => 0;
        public Func<Player, string> Blocked = p => null;      // why it can't be used right now, or null
        public Func<Player, bool> On = p => false;            // a toggle or stance that is active
        public Action<Player> Run;
    }

    public static class CombatActions
    {
        static bool Fight => !PlayerController.Exploring();
        static bool MyTurn => GameState.Current.CurrentTurn == Turn.Player;

        static string NeedAp(Player p, int ap) => p.Ap < ap ? $"Needs {ap} AP." : null;

        static int ShotAp(Player p)
        {
            var w = p.EquippedWeapon;
            if (w == null) return 2;
            bool burst = p.FireMode == "Burst" && CombatRules.CanBurst(w);
            int ap = burst ? CombatRules.BurstApCost(w) : CombatRules.SingleShotAp(w);
            if (!burst && !CombatRules.IsMelee(w) && p.AimedPart != null) ap += 1;
            return ap + (p.IsDepleted() ? 1 : 0);
        }

        /// <summary>The enemy the Fire button shoots: a hostile you can see and reach, the ones already fighting you first, then the nearest.</summary>
        public static Mutant FireTarget(Player p)
        {
            var w = p.EquippedWeapon;
            int range = w == null ? 1 : w.MaxReach;
            var grid = GameServices.Grid;
            return EncounterState.Enemies
                .Where(e => e.IsAlive() && EncounterState.IsActive(e) && !Companion.Is(e) && !EnemyAI.FriendlyToPlayer(e) && Vision.Shows(e)
                    && CombatantUtil.Distance(p, e) <= range && (grid == null || grid.CheckLineOfSight(p.X, p.Y, e.X, e.Y)))
                .OrderByDescending(e => e.IsAggroed).ThenBy(e => CombatantUtil.Distance(p, e)).FirstOrDefault();
        }

        static List<(int x, int y)> Next(Player p) => TerrainMap.GetAdjacentCoords(p.X, p.Y).Where(c => Props.Movable(c.x, c.y)).ToList();

        static string GunBlocked(Player p)
        {
            var w = p.EquippedWeapon;
            if (w == null) return "No weapon in your hands.";
            if (ItemCondition.IsBroken(w.Condition)) return $"{w.Name} is broken.";
            if (w.IsJammed) return "Jammed: reload (R) clears it.";
            if (!CombatRules.IsMelee(w) && !w.IsLoaded) return "Empty: reload (R).";
            return null;
        }

        public static readonly List<ActionDef> All = new List<ActionDef>
        {
            new ActionDef { Id = "fire", Icon = "fire", Key = "LMB", Name = "Fire", CombatOnly = true,
                Desc = "Target the nearest enemy you can see in range (or click one on the map): click a body part, or SHOOT for a plain shot.",
                Ap = ShotAp,
                Blocked = p => GunBlocked(p) ?? NeedAp(p, ShotAp(p)) ?? (FireTarget(p) == null ? "No enemy in sight and in range." : null),
                Run = p => { var t = FireTarget(p); if (t != null && !HudView.OpenTargeting(t)) { p.Attack(t, null); TurnLoop.CheckCombatEnd(); } } },   // Batch 77: opens the targeting panel
            new ActionDef { Id = "aim", Icon = "aim", Key = "A", Name = "Aim", CombatOnly = true,
                Desc = "+2 to hit on your next shot this turn.", Ap = p => 1,
                Blocked = p => p.StatusEffects.AimBonus > 0 ? "Already aiming." : NeedAp(p, 1),
                On = p => p.StatusEffects.AimBonus > 0, Run = p => p.Aim() },
            new ActionDef { Id = "part", Icon = "part", Key = "L", Name = "Aimed shot",
                Desc = "Pick the body part single shots go for: legs, arms, body, head, armor, grenade pouch, or off. +1 AP a shot.",
                On = p => p.AimedPart != null, Run = p => p.CycleAimedPart() },
            new ActionDef { Id = "mode", Icon = "mode", Key = "X", Name = "Fire mode",
                Desc = "Switch between single shots and bursts.",
                Blocked = p => CombatRules.CanBurst(p.EquippedWeapon) ? null : "This gun only fires single shots.",
                On = p => p.FireMode == "Burst", Run = p => p.ToggleFireMode() },
            new ActionDef { Id = "reload", Icon = "reload", Key = "R", Name = "Reload",
                Desc = "Reload, or clear a jam.", Ap = p => p.EquippedWeapon != null && p.EquippedWeapon.IsJammed ? 1 : p.EquippedWeapon?.Db?.ReloadApCost ?? 1,
                Blocked = p => p.EquippedWeapon == null || CombatRules.IsMelee(p.EquippedWeapon) ? "Nothing to reload."
                    : !p.EquippedWeapon.IsJammed && p.EquippedWeapon.CurrentMagazine >= p.EquippedWeapon.MagCapacity ? "Already full." : null,
                Run = p => { if (p.EquippedWeapon.IsJammed) p.ClearWeaponJam(); else p.ReloadWeapon(); } },
            new ActionDef { Id = "cover", Icon = "cover", Key = "C", Name = "Take cover", CombatOnly = true,
                Desc = "+2 AC on top of the cover you're behind, until your next turn.", Ap = p => 2,
                Blocked = p => !p.NearCover() ? "No cover next to you." : NeedAp(p, 2),
                On = p => p.StatusEffects.TakeCoverAc > 0 && !p.StatusEffects.EvasiveMelee, Run = p => p.TakeCover() },
            new ActionDef { Id = "hunker", Icon = "hunker", Key = "K", Name = "Hunker down", CombatOnly = true,
                Desc = "Spend the rest of your AP and end your turn: your cover counts from every side and its AC doubles (light +2, hard +4).",
                Ap = p => Math.Max(1, p.Ap), Blocked = p => p.CanHunker(out var why) ? null : why,
                On = p => p.StatusEffects.Hunkered, Run = p => p.HunkerDown() },
            new ActionDef { Id = "overwatch", Icon = "overwatch", Key = "O", Name = "Overwatch", CombatOnly = true,
                Desc = "Spend the rest of your AP (a shot's worth or more) and end your turn: you shoot the first enemy that moves where you can see it.",
                Ap = p => Math.Max(CombatRules.SingleShotAp(p.EquippedWeapon), p.Ap), Blocked = p => p.CanOverwatch(out var why) ? null : why,
                On = p => p.OverwatchHeld, Run = p => p.Overwatch() },
            new ActionDef { Id = "evasive", Icon = "evasive", Key = "V", Name = "Evasive", CombatOnly = true,
                Desc = "+2 AC against melee until your next turn.", Ap = p => 2, Blocked = p => NeedAp(p, 2),
                On = p => p.StatusEffects.EvasiveMelee, Run = p => p.EvasiveManeuvers() },
            new ActionDef { Id = "grenade", Icon = "grenade", Key = "G", Name = "Grenade", CombatOnly = true,
                Desc = "Ready your next grenade, then click a tile within 8 to throw it. Right-click cancels.", Ap = p => 2,
                Blocked = p => !p.Inventory.Items.Any(i => GameData.ConsumableDatabase.All.TryGetValue(i.Id, out var d) && d.Throwable != null) ? "No grenades." : NeedAp(p, 2),
                On = p => p.ArmedThrowable != null, Run = p => p.CycleThrowable() },
            new ActionDef { Id = "intimidate", Icon = "intimidate", Key = "T", Name = "Intimidate", CombatOnly = true,
                Desc = "Bark at the nearest human enemy within 8: shakes or breaks their nerve.", Ap = p => 2, Blocked = p => NeedAp(p, 2),
                Run = p => p.Intimidate() },
            new ActionDef { Id = "suppress", Icon = "suppress", Key = "S", Name = "Suppress", CombatOnly = true,
                Desc = "Hose a tile with a burst: everyone within 2 (4 with Suppressive Fire) is pinned, -2 to hit, and may lose 2 AP. Click the tile after.", Ap = p => 3,
                Show = p => CombatRules.CanBurst(p.EquippedWeapon),   // Batch 127: every automatic gun, the perk widens it
                Blocked = p => NeedAp(p, 3),
                On = p => p.ArmedAction == "suppress", Run = p => p.ArmSuppressiveFire() },
            new ActionDef { Id = "rungun", Icon = "rungun", Key = "H", Name = "Run and gun", CombatOnly = true,
                Desc = "Once a turn: 1 AP for +2 MP.", Ap = p => 1, Show = p => CombatantUtil.HasPerk(p, "RunAndGun"),
                Blocked = p => p.RunAndGunUsed ? "Used this turn." : NeedAp(p, 1), Run = p => p.RunAndGun() },
            new ActionDef { Id = "shove", Icon = "shove", Key = "Click", Name = "Shove",   // Batch 103
                Desc = "Click a desk, barrel or crate next to you (1 AP in a fight): a desk tips onto its side as cover from the far side, a barrel rolls away until something stops it (and knocks down whoever it hits), a crate slides a tile.",
                Ap = p => Fight ? Props.ShoveAp : 0, Show = p => Next(p).Count > 0,   // only when something is next to you
                Blocked = p => Next(p).Count == 0 ? "Nothing to shove next to you." : Fight ? NeedAp(p, Props.ShoveAp) : null,
                Run = p => { var n = Next(p); if (n.Count == 1) Props.Shove(p, n[0].x, n[0].y); else GameLog.Write("Click the one to shove."); } },
            new ActionDef { Id = "sneak", Icon = "sneak", Key = "N", Name = "Sneak",
                Desc = "Move at half speed; enemies spot you at half range. Sneak up behind someone unaware and click them for a takedown.",
                On = p => p.Sneaking, Run = p => p.ToggleSneak() },
            new ActionDef { Id = "light", Icon = "light", Key = "U", Name = "Flashlight",
                Desc = "Switch your flashlight. Off, you see only what lamps light, but in the dark enemies spot you at 60% range. Lamp light shows you either way.",
                On = p => p.FlashlightOn, Run = p => p.ToggleFlashlight() },
            new ActionDef { Id = "swap", Icon = "swap", Key = "Y", Name = "Swap weapon",
                Desc = "Draw your second weapon (Second in the pack). A pistol or a blade is free; anything else 1 AP.",
                Ap = p => p.SwapAp, Blocked = p => p.SecondaryWeapon == null ? "No second weapon." : NeedAp(p, p.SwapAp),
                Run = p => p.SwapWeapons() },
            new ActionDef { Id = "ammo", Icon = "ammo", Key = "Q", Name = "Ammo type",
                Desc = "Load the next kind of ammo you carry.",
                Blocked = p => p.EquippedWeapon == null || CombatRules.IsMelee(p.EquippedWeapon) ? "Nothing to load." : null,
                Run = p => p.CycleAmmoType() },
            new ActionDef { Id = "interact", Icon = "interact", Key = "E", Name = "Use", ExploreOnly = true,
                Desc = "Open, loot, talk or use what's next to you.", Run = p => InteractionEngine.AttemptInteract(p) },
            new ActionDef { Id = "search", Icon = "search", Key = "F", Name = "Search", ExploreOnly = true,
                Desc = "Search the area for hidden ways, clues, or an artifact in a calmed anomaly.",
                Run = p => { if (AnomalyEngine.TrySearchAnomaly(p)) return; ZoneTravel.SearchArea(); QuestSystem.TryInvestigate(); } },
            new ActionDef { Id = "rest", Icon = "rest", Key = "Z", Name = "Rest", ExploreOnly = true,
                Desc = "Rest to heal and recover (not with enemies around).", Run = p => WorldDays.Rest() },
        };

        public static ActionDef Find(string id) => All.FirstOrDefault(a => a.Id == id);

        /// <summary>The buttons to show now: combat actions in a fight, exploring actions out of one, perks you own.</summary>
        public static List<ActionDef> Visible(Player p)
        {
            bool fight = Fight;
            return All.Where(a => a.Show(p) && !(a.CombatOnly && !fight) && !(a.ExploreOnly && fight)).ToList();
        }

        /// <summary>Why the button is greyed out, or null if it can be pressed.</summary>
        public static string WhyNot(ActionDef a, Player p)
        {
            if (GameState.Current.ModalOpen) return "";
            if (!MyTurn) return "Not your turn.";
            return a.Blocked(p);
        }

        /// <summary>Press a button: runs the action if it's usable, otherwise says why not.</summary>
        public static bool Press(string id, Player p)
        {
            var a = Find(id);
            if (a == null || p == null) return false;
            var why = WhyNot(a, p);
            if (why != null) { if (why.Length > 0) GameLog.Write(why); return false; }
            a.Run(p);
            return true;
        }
    }
}
