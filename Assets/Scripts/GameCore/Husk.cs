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

        public const int MaxPacks = 4;
        public const float PackChancePerHour = 0.10f;

        /// <summary>Hourly (SquadManager.TickHour): once the canister has been read (flag husks_known) the transmitter's work shows up in every tier: now and then a pack of Husks
        /// appears in a wild region off your map (never a Safe Zone, a dungeon or an interior), of the tier you are in or one above, and wanders between regions like any squad (they
        /// may walk anywhere that isn't a Safe Zone). At most MaxPacks at once in the world. They are the story's, not a faction's: no deck, no home, no post.</summary>
        public static void Roam(string hereMap)
        {
            if (!Story.Has("husks_known") || Survival.On) return;
            var m = MacroState.Current;
            if (m.OfflineSquads.Count(s => s.Faction == Faction && s.Members.Any(x => x.IsAlive())) >= MaxPacks || UnityEngine.Random.value > PackChancePerHour) return;
            int tier = MacroMapDatabase.Find(m.CurrentZoneId)?.Tier ?? 1;
            var zones = MacroMapDatabase.Zones.Where(z => !z.IsSafeZone && !z.IsDungeon && z.AllowedFactions.Count > 0 && !ZoneLayoutRecipes.IsInterior(z.Id)
                && z.Tier <= tier + 1 && (hereMap == null || !SquadManager.OnMap(z.Id, hereMap))).ToList();
            if (zones.Count == 0) return;
            var zone = zones[UnityEngine.Random.Range(0, zones.Count)];
            var pack = Pack(zone.Tier);
            m.OfflineSquads.Add(new OfflineSquad { ZoneId = zone.Id, Faction = Faction, Members = pack, PersistFullState = true, IsHostileToPlayer = true, LastUpdatedTick = m.WorldTickCounter });
            WorldEvents.News('M', zone.Id, $"Something is walking the roads of {zone.Name}: pale people in a pack, dragging their feet.");
        }

        /// <summary>A pack of `n` (4 to 8 when you pass nothing).</summary>
        public static List<Mutant> Pack(int tier, int n = -1)
        {
            if (n < 0) n = UnityEngine.Random.Range(4, 9);
            return Enumerable.Range(0, n).Select(i => (Mutant)Make(tier, i == 0)).ToList();
        }
    }
}
