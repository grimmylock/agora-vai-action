// Husks (STORY.md "Names", Yan 2026-11-18): zombified stalkers, the work of "those"'s transmitter. They appear in every tier, as strong as their armour and
// weapons make them, but: they walk slowly (they drag themselves), shoot very badly, always close in on the target whatever they hold, never flee or surrender,
// can't radio for help, walk in packs of 4 to 8, and everything they carry is Broken. Faction "Husks": hostile to every human faction and to you.
// Art: ArtSource/make_husks.py (desaturated, hunched, grey eyes, wounds) -> Sprites/husk_*_x2.png, loaded by UnitSprites.
// ponytail: no roaming husk packs in the A-Life yet (they are placed by the story: the village defence); a Husk deck for the squad system comes later.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Husk
    {
        public const string Faction = "Husks";
        public const float StepMul = 1.8f, AimSpread = 3.2f;   // a step takes 1.8x as long; extra spread (NpcAim)
        public static bool Is(ICombatant u) => u is HumanEnemy h && h.FactionName == Faction;

        static readonly string[] Sources = { "Wanderers", "Raiders", "Wanderers" };

        /// <summary>One Husk with a tier's kit (the gear any stalker of that tier would carry).</summary>
        public static HumanEnemy Make(int tier, bool first = false)
        {
            string rank = tier <= 1 ? "Rookie" : tier <= 3 ? "Experienced" : tier <= 5 ? "Professional" : "Veteran";
            string from = Sources[UnityEngine.Random.Range(0, Sources.Length)];
            var (w, a) = EncounterGeneration.PickFactionLoadout(from, rank, System.Math.Max(1, tier), first);
            var h = new HumanEnemy("Husk", 0, 0, Faction, rank, w, a);
            h.Morale = "Steady";
            return h;
        }

        /// <summary>A pack of `n` (4 to 8 when you pass nothing).</summary>
        public static List<Mutant> Pack(int tier, int n = -1)
        {
            if (n < 0) n = UnityEngine.Random.Range(4, 9);
            return Enumerable.Range(0, n).Select(i => (Mutant)Make(tier, i == 0)).ToList();
        }
    }
}
