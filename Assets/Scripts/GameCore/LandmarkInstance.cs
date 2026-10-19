// New (Batch 7): a placed instance of a GameData.LandmarkDef on the tactical grid - mirrors
// InteractiveObjectInstance's shape (db-def fields copied onto a positioned runtime instance).
// Unlike InteractiveObjectInstance, landmarks are never consumed/removed - they're permanent
// place-names, not one-time loot.
namespace GameCore
{
    public class LandmarkInstance
    {
        public int X;
        public int Y;
        public string Name;
        public string Description;
        public string HiddenNodeId;
    }
}
