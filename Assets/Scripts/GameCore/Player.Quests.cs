// New (Batch 6): quest progress state, split into its own partial-class file matching the existing
// Player.Equipment.cs/Player.Artifacts.cs convention. See QuestSystem.cs for the engine that reads
// and writes all of this.
using System.Collections.Generic;

namespace GameCore
{
    public partial class Player
    {
        /// <summary>Quests currently in progress, with their runtime objective state.</summary>
        public List<ActiveQuest> ActiveQuests = new List<ActiveQuest>();

        /// <summary>Standalone quest ids that have paid out already - never offered again.</summary>
        public HashSet<string> CompletedQuestIds = new HashSet<string>();

        /// <summary>Per-NPC storyline progress: Quartermaster zone id -> next stage to offer (1-3).
        /// Missing entry means "hasn't started" (offer Stage 1). A completed arc sits at 4 (no
        /// Stage 4 exists in QuestDatabase, so QuestSystem.AvailableInZone naturally stops
        /// offering anything further).</summary>
        public Dictionary<string, int> NpcArcStage = new Dictionary<string, int>();
    }
}
