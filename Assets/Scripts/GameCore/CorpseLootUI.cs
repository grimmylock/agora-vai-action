// New (Batch 10 - living-world NPCs): the loot screen for a dead HumanEnemy's own stash (HumanLootSystem.RollSpawnStash/TakeSpoils/GrantDrop populate npc.Inventory/npc.Ru - see that file). Opens when the
// player clicks a corpse (PlayerController). 2026-11-18 (Yan: "add the grid as the looting screen for the enemies stuff"): the old text list is gone - the body's things lie in a GRID next to your own pack
// (HudView.Pack.cs: LootStart / BuildLoot / the drag code), and this class is only the door: it opens and closes that screen and keeps ModalOpen.
using UnityEngine;

namespace GameCore
{
    public class CorpseLootUI : MonoBehaviour
    {
        private static CorpseLootUI _instance;
        private bool _open;
        private HumanEnemy _corpse;

        public static bool IsOpen => _instance != null && _instance._open;
        public static void CloseIfOpen() { if (IsOpen) _instance.Close(); }

        private void Awake() => _instance = this;

        /// <summary>Called from PlayerController when the player clicks a dead HumanEnemy.</summary>
        public static void Open(HumanEnemy corpse) => _instance?.OpenInternal(corpse);

        private void OpenInternal(HumanEnemy corpse)
        {
            if (corpse == null || HudView.Instance == null) return;
            Boss.Looted(corpse);   // Boss 1: a few seconds later, his smoke grenade
            _corpse = corpse; _open = true;
            GameState.Current.ModalOpen = true;
            HudView.Instance.LootStart(corpse);   // your pack on the left, the body's grid on the right
        }

        private void Close()
        {
            _open = false; _corpse = null;
            GameState.Current.ModalOpen = false;
            HudView.Instance?.LootStop();
        }
    }
}
