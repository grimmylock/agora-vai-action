// Ported from script.js: rollAttack() and calculateDamage() (as the ICombatResolver
// implementation). Assign `new CombatResolver()` to GameServices.Combat (alongside
// GameServices.Grid from the previous step) to fully light up Weapon.Fire(), Mutant.Attack(),
// and HumanEnemy.Attack().
namespace GameCore
{
    public class CombatResolver : ICombatResolver
    {
        /// <summary>Ported from rollAttack(advantageState, modifier, targetAC).</summary>
        public AttackRollResult RollAttack(string advantageState, int modifier, int targetAc, string breakdown = null)
        {
            var roll = DiceRoller.Roll2D10(advantageState);
            int total = roll.Chosen + modifier;
            bool hit = total >= targetAc;
            GameLog.Write(DiceRoller.FormatRollLog(roll, modifier, targetAc, breakdown));

            return new AttackRollResult
            {
                Hit = hit,
                ChosenRoll = roll.Chosen,
                Jammed = false,
                NoAmmo = false,
                NoLineOfSight = false,
            };
        }

        /// <summary>Ported from calculateDamage(baseDamage, ammoType, targetPhysDR, targetAnomDR).</summary>
        public float CalculateDamage(float baseDamage, string ammoType, float targetPhysDR, float targetAnomDR)
        {
            // Physical damage on the design's rules (CombatRules): ammo vs capped Phys-DR. Anom-DR
            // never applies to bullets, blades or blasts (file 00 §6) - psi/anomaly damage goes
            // through CombatRules.AnomalousDamage instead.
            return CombatRules.PhysicalDamage(baseDamage, ammoType, targetPhysDR);
        }

        /// <summary>Ported from getCoverBonus(attacker, target, weapon) — delegates to CoverUtil (grid step).</summary>
        public CoverResult GetCover(ICombatant attacker, ICombatant target, Weapon weapon) =>
            CoverUtil.GetCover(attacker, target, weapon);

        /// <summary>Ported from isMeleeAttack(attacker, target, weapon) — delegates to CoverUtil (grid step).</summary>
        public bool IsMeleeAttack(ICombatant attacker, ICombatant target, Weapon weapon) =>
            CoverUtil.IsMeleeAttack(attacker, target, weapon);
    }
}
