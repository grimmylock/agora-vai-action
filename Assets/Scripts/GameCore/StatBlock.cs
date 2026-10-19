// Ported from script.js: this.stats = { S: 10, P: 10, A: 10, I: 10 } on Player.
// Field names kept as the original single-letter keys (Strength/Perception/Agility/Intelligence)
// so they line up with getStatModifier(unit, 'S') style lookups and with the stat-check plumbing
// (rollSkillCheck accepts a stat key or array of them) ported later in the combat step.
namespace GameCore
{
    public class StatBlock
    {
        public int S = 10; // Strength
        public int P = 10; // Perception
        public int A = 10; // Agility
        public int I = 10; // Intelligence

        /// <summary>Looks up a stat by its JS-style single-letter key ("S","P","A","I"). Returns 0 if unknown.</summary>
        public int Get(string key)
        {
            switch (key)
            {
                case "S": return S;
                case "P": return P;
                case "A": return A;
                case "I": return I;
                default: return 0;
            }
        }
    }
}
