// Perks, straight from file 05. One at character creation, more at Professional, Expert and
// Legend (file 00 §13). Group is only used to lay the picker out.
using System.Collections.Generic;

namespace GameData
{
    public class PerkDef
    {
        public string Name;
        public string Group;
        public string Description;
        public string Requires;   // Batch 80: a perk key you must already have
    }

    public static class PerkDatabase
    {
        private static PerkDef P(string group, string name, string desc) => new PerkDef { Group = group, Name = name, Description = desc };

        public static readonly Dictionary<string, PerkDef> All = new Dictionary<string, PerkDef>
        {
            ["MarksmansFocus"] = P("Sniper", "Marksman's Focus", "+2 to attack rolls with a sniper rifle."),
            ["HighVelocityImpact"] = P("Sniper", "High-Velocity Impact", "Sniper shots at 12+ tiles ignore an extra 20% of Phys-DR."),
            ["Overwatch"] = P("Sniper", "Overwatch", "End your turn with 3+ AP and a sniper rifle: the first enemy that moves in your sights eats a free shot."),
            ["RecoilControl"] = P("Assault", "Recoil Control", "No burst penalty with assault rifles."),
            ["ControlledTaps"] = P("Assault", "Controlled Taps", "Once per turn, an attack die of 15+ refunds 1 AP."),
            ["SuppressiveFire"] = P("Assault", "Suppressive Fire", "Suppressing fire (S) reaches 4 tiles round the spot instead of 2."),
            ["BulletHose"] = P("SMG", "Bullet Hose", "SMG bursts have no penalty within 4 tiles."),
            ["RunAndGun"] = P("SMG", "Run and Gun", "H: once per turn, 1 AP for +2 MP."),
            ["HitAndRun"] = P("SMG", "Hit and Run", "After your first attack each turn, +2 MP."),
            ["Entrenched"] = P("Heavy", "Entrenched", "+20% Phys-DR on turns you didn't move."),
            ["Shredder"] = P("Heavy", "Shredder", "Heavy bursts roll with advantage but use double ammo."),
            ["WalkingTank"] = P("Heavy", "Walking Tank", "Heavy weapons no longer cap your MP at 2."),
            ["Breacher"] = P("Shotgun", "Breacher", "+4 shotgun damage within 4 tiles."),
            ["BuckshotSpread"] = P("Shotgun", "Buckshot Spread", "A shotgun roll can also hit one enemy next to your target."),
            ["StaggeringBlast"] = P("Shotgun", "Staggering Blast", "Shotgun hits within 4 tiles drain 2 MP from the target's next turn."),
            ["Gunslinger"] = new PerkDef { Group = "Sidearm", Name = "Gunslinger", Requires = "PointBlankExecution", Description = "Needs Point-Blank Execution. Two sidearms (Off-hand button in the pack): single shots fire both, 2 AP, -1 each; reload both for +1 AP. Not with Double Tap." },   // Batch 80 (Yan)
            ["DoubleTap"] = P("Sidearm", "Double Tap", "Your second sidearm shot each turn costs 1 AP."),
            ["PointBlankExecution"] = P("Sidearm", "Point-Blank Execution", "Adjacent sidearm shots ignore cover and get +2 to hit."),
            ["BladeDancer"] = P("Melee", "Blade Dancer", "Melee attacks cost 2, then 1, then 1 AP - max 3 per turn."),
            ["BullRush"] = P("Melee", "Bull Rush", "With a melee weapon in hand your MP doubles. Swap to a gun mid-turn and the extra MP is lost."),   // Batch 76
            ["DesperateParry"] = P("Melee", "Desperate Parry", "End your turn with 1+ AP: the first melee hit on you gets an Agility DC 14 to negate it."),
            ["CombatMedic"] = P("Survival", "Combat Medic", "Medkits and stims heal +10 HP."),
            ["QuickHands"] = P("Survival", "Quick Hands", "Reloads cost 0 AP in combat."),
            ["Gunsmith"] = P("Survival", "Gunsmith", "Clearing a jam costs 0 AP."),
            ["Sprinter"] = P("Survival", "Sprinter", "+2 MP per turn."),
            ["TacticalReposition"] = P("Survival", "Tactical Reposition", "+1 MP per turn; end your turn in cover for +1 AC."),
            ["AdrenalineSurge"] = P("Survival", "Adrenaline Surge", "First time you drop below 30 HP in a fight: +20 temp HP and +2 AP next turn."),
            ["Vault"] = P("Survival", "Vault", "Moving across cover tiles costs no extra MP."),
            ["PackMule"] = P("Survival", "Pack Mule", "Base carry capacity 50 kg."),
            ["IronStomach"] = P("Survival", "Iron Stomach", "Food and water restore double."),
            ["AnomalyWhisperer"] = P("Survival", "Anomaly Whisperer", "+2 to anomaly avoidance."),
            ["Scavenger"] = P("Survival", "Scavenger", "Loot counts are rolled twice, keep the higher."),
            ["IronWill"] = P("Survival", "Iron Will", "Temporary HP lasts 8 turns."),
            ["ArtifactHandler"] = P("Survival", "Artifact Handler", "Artifact Anom-DR penalties are halved."),
            ["Haggler"] = P("Survival", "Haggler", "Buy 15% cheaper, sell 15% higher."),
            ["Tumbler"] = P("Survival", "Tumbler", "After a dive you're back on your feet faster (0.35 s prone instead of 0.6)."),   // real-time fork
            ["ColdBlood"] = P("Survival", "Cold Blood", "Rounds that just miss you fill twice the Focus."),   // real-time fork
        };
    }
}
