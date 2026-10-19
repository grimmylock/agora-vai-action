// Ported from script.js: this.statusEffects = { bleeding, toxicBleed, irradiated, prone }
namespace GameCore
{
    public class StatusEffects
    {
        public bool Bleeding;
        public bool ToxicBleed;
        public bool Irradiated;
        public bool Prone;

        // New (file 00 firefight rules)
        public string Wound;          // null, "Arm", "Leg", "Torso", "Head" - one at a time, a Bandage treats it
        public bool Suppressed;       // -2 to attack rolls on your next turn
        public int AimBonus;          // banked by the Aim action, spent on the next attack this turn
        public int TakeCoverAc;       // Take Cover / Evasive: +AC until the start of your next turn
        public bool EvasiveMelee;     // Evasive Maneuvers: the bonus only counts vs melee
        public bool Hunkered;         // Batch 72: Hunker Down - cover from every side, doubled, until your next turn
        public bool HalfMpNextTurn;   // Batch 130: hit by the Blight Brute's boulder - the player starts the next turn on half MP
        public int PendingMpDrain;    // Staggering Blast: MP lost at the start of the next turn
    }
}
