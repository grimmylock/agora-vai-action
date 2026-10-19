// Ported from script.js: equipArtifact(inventoryIndex, beltSlotIndex), unequipArtifact(beltSlotIndex).
// Split into a separate partial-class file (rather than folded into Player.cs from step 2) so
// each port step's additions stay easy to trace against the original file. The original ends
// each of these with updateUI() — that's a no-op here until the UI step wires up a listener.
using GameData;

namespace GameCore
{
    public partial class Player
    {
        /// <summary>Ported from unequipArtifact(beltSlotIndex).</summary>
        public void UnequipArtifact(int beltSlotIndex)
        {
            string artifactId = ArtifactSlots[beltSlotIndex];
            if (string.IsNullOrEmpty(artifactId))
            {
                GameLog.Write("That belt slot is already empty.");
                return;
            }

            ArtifactDatabase.All.TryGetValue(artifactId, out var def);
            ArtifactSlots[beltSlotIndex] = null;
            AddItem(new InventoryItem
            {
                Id = artifactId,
                Name = def != null ? def.Name : artifactId,
                WeightKg = def != null ? def.WeightKg : 0.5f,
                Type = "artifact",
                Qty = 1,
                Price = def?.Type == "Legendary" ? 20000 : Economy.ValueOf(new InventoryItem { Type = "artifact", Name = def?.Name ?? artifactId }),
            });
            GameLog.Write($"Unequipped {(def != null ? def.Name : artifactId)} from belt slot {beltSlotIndex + 1}.");
            ApplyArtifactStatBonuses();
            ApplyCarryEffects();
        }

        /// <summary>
        /// Safely equips an artifact into a belt slot and enforces the single Legendary limit.
        /// </summary>
        public bool TryEquipArtifact(int slotIndex, string artifactId)
        {
            if (slotIndex < 0 || slotIndex >= ArtifactSlots.Length)
            {
                GameLog.Write("Invalid artifact slot.");
                return false;
            }

            if (!ArtifactDatabase.All.TryGetValue(artifactId, out var newArt))
            {
                GameLog.Write("Invalid artifact selected.");
                return false;
            }

            // (The old one-legendary limit is gone: file 03 makes each legendary its own family.)

            ArtifactSlots[slotIndex] = artifactId;
            ApplyArtifactStatBonuses();
            ApplyCarryEffects();
            return true;
        }

        /// <summary>Ported from equipArtifact(inventoryIndex, beltSlotIndex).</summary>
        public void EquipArtifact(int inventoryIndex, int beltSlotIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= Inventory.Items.Count)
            {
                GameLog.Write("Selected item is not an artifact.");
                return;
            }
            var item = Inventory.Items[inventoryIndex];
            if (item == null || item.Type != "artifact")
            {
                GameLog.Write("Selected item is not an artifact.");
                return;
            }

            int maxBeltSlots = EquippedArmor != null ? TotalBeltSlots : 1;
            if (beltSlotIndex >= maxBeltSlots)
            {
                GameLog.Write("Your current armor does not have enough belt slots unlocked.");
                return;
            }

            string currentArtifactInSlot = ArtifactSlots[beltSlotIndex];
            if (!TryEquipArtifact(beltSlotIndex, item.Id))
                return;

            RemoveItem(item.Id, 1);
            if (!string.IsNullOrEmpty(currentArtifactInSlot))
            {
                ArtifactDatabase.All.TryGetValue(currentArtifactInSlot, out var swappedDef);
                AddItem(new InventoryItem
                {
                    Id = currentArtifactInSlot,
                    Name = swappedDef != null ? swappedDef.Name : currentArtifactInSlot,
                    WeightKg = swappedDef != null ? swappedDef.WeightKg : 0.5f,
                    Type = "artifact",
                    Qty = 1,
                });
            }

            GameLog.Write($"Equipped {item.Name} to belt slot {beltSlotIndex + 1}.");
        }
    }
}
