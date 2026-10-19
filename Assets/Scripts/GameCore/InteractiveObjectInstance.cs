// Ported from script.js: the plain {x,y,name,stat,dc,reward,description} objects pushed into
// the `interactiveObjects` global array by spawnInteractiveObjects().
using GameData;

namespace GameCore
{
    public class InteractiveObjectInstance
    {
        public int X;
        public int Y;
        public string Name;
        public string[] Stat;
        public int Dc;
        public InteractiveReward Reward;
        public string Description;
        public bool Opened; // set by attemptInteract() on a successful "openPath" reward (combat/interaction step)
        public string Lock, Code, Opens;   // Batch 126: "key" / "code" locks, a code note and the stash it opens (Stashes)
        public bool Rolled;                // Batch 126: put down by Stashes.Roll (moves every 7 days), not a hand-placed find
        public string GrantsKey; // the map overhaul: a stash that also holds a key (TerrainMap.StashKeys -> MacroState.Keys)
    }
}
