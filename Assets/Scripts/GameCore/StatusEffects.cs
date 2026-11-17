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
    }
}
