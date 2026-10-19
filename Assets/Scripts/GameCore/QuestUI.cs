// New (Batch 6): the quest journal panel - lists quests offered in the player's current zone
// (QuestSystem.AvailableInZone) with Accept buttons, and every currently active quest with its
// full source checklist text plus live progress on whichever objective is actually tracked
// mechanically (see QuestSystem.cs). Built at runtime under the existing Canvas, same convention
// as ShopUI/InventoryUI/DeathScreen - no scene wiring required. Added automatically by
// GameBootstrap, toggled with J (PlayerController).
using System.Linq;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class QuestUI : MonoBehaviour
    {
        private const int MaxRowsShown = 12;
        private const float RowHeight = 30f;

        private static QuestUI _instance;

        private GameObject _panel;
        private Transform _availableList;
        private Transform _activeList;
        private bool _open;

        private void Awake() => _instance = this;

        public static void Toggle() => _instance?.ToggleInternal();

        private void ToggleInternal()
        {
            if (_open) { Close(); return; }
            BuildPanelIfNeeded();
            if (_panel == null) return;

            _open = true;
            GameState.Current.ModalOpen = true;
            _panel.SetActive(true);
            Rebuild();
        }

        private void Close()
        {
            _open = false;
            GameState.Current.ModalOpen = false;
            if (_panel != null) _panel.SetActive(false);
        }

        private void BuildPanelIfNeeded()
        {
            if (_panel != null) return;
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("QuestUI: no Canvas found in the scene - can't show the quest panel.");
                return;
            }

            _panel = UIBuildUtil.CreatePanel(canvas.transform, "QuestPanel", new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f),
                new Color(0.08f, 0.08f, 0.08f, 0.95f));

            UIBuildUtil.CreateButton(_panel.transform, "CloseButton", new Vector2(0.86f, 0.92f), new Vector2(0.98f, 0.99f),
                "Close", Close);
            UIBuildUtil.CreateText(_panel.transform, "AvailableHeader", new Vector2(0.02f, 0.85f), new Vector2(0.48f, 0.91f),
                "AVAILABLE HERE", 18, Color.white, TextAlignmentOptions.Left);
            UIBuildUtil.CreateText(_panel.transform, "ActiveHeader", new Vector2(0.52f, 0.85f), new Vector2(0.98f, 0.91f),
                "ACTIVE QUESTS", 18, Color.white, TextAlignmentOptions.Left);

            _availableList = CreateList(_panel.transform, "AvailableList", new Vector2(0.02f, 0.02f), new Vector2(0.48f, 0.84f));
            _activeList = CreateList(_panel.transform, "ActiveList", new Vector2(0.52f, 0.02f), new Vector2(0.98f, 0.84f));
        }

        private static Transform CreateList(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 3;
            return go.transform;
        }

        private void Rebuild()
        {
            var player = Player.Current;
            string zoneId = MacroState.Current.CurrentZoneId;

            foreach (Transform child in _availableList) Destroy(child.gameObject);
            var available = QuestSystem.AvailableInZone(zoneId);
            foreach (var q in available.Take(MaxRowsShown))
            {
                var row = UIBuildUtil.CreateRow(_availableList, $"Avail_{q.Id}", RowHeight * 1.6f, new Color(1f, 1f, 1f, 0.05f));
                UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f),
                    $"{q.Hook}\n({q.Category}, Tier {q.ZoneTier})", 12, Color.white, TextAlignmentOptions.Left);
                UIBuildUtil.CreateButton(row.transform, "AcceptBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f),
                    "Accept", () => { QuestSystem.AcceptQuest(q.Id); Rebuild(); });
            }
            if (available.Count == 0)
            {
                var emptyRow = UIBuildUtil.CreateRow(_availableList, "AvailEmpty", RowHeight, Color.clear);
                UIBuildUtil.CreateText(emptyRow.transform, "Label", Vector2.zero, Vector2.one,
                    "Nothing on offer here right now.", 12, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Left);
            }

            foreach (Transform child in _activeList) Destroy(child.gameObject);
            foreach (var aq in player.ActiveQuests.Take(MaxRowsShown))
            {
                var q = QuestDatabase.Find(aq.QuestId);
                var row = UIBuildUtil.CreateRow(_activeList, $"Active_{aq.QuestId}", RowHeight * (1.9f + aq.DisplayText.Length * 0.5f), new Color(1f, 1f, 1f, 0.05f));
                string checklist = string.Join("\n", aq.DisplayText.Select(t => $"[{(aq.AllDone ? "x" : " ")}] {t}"));
                string progress = string.Join(", ", aq.Objectives.Select(ObjectiveProgressText));
                UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.98f, 1f),
                    $"{q?.Hook ?? aq.QuestId} - {aq.ZoneId}\n{checklist}\nProgress: {progress}\nComplication: {QuestSystem.ComplicationText(aq.Complication)}{(q != null ? $"  |  For: {QuestSystem.Sides(q, aq.ZoneId).giver} or {QuestSystem.Sides(q, aq.ZoneId).alt}" : "")}", 11, Color.white, TextAlignmentOptions.Left);
            }
            if (player.ActiveQuests.Count == 0)
            {
                var emptyRow = UIBuildUtil.CreateRow(_activeList, "ActiveEmpty", RowHeight, Color.clear);
                UIBuildUtil.CreateText(emptyRow.transform, "Label", Vector2.zero, Vector2.one,
                    "No active quests.", 12, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Left);
            }
        }

        private static string ObjectiveProgressText(QuestObjectiveRuntime o) => o.Kind switch
        {
            "Kill" => $"Kills {o.CurrentCount}/{o.NeededCount}",
            "SkillCheck" => o.Done ? "Investigated" : "Search the zone (F points the way)",
            "ClearZone" => o.Done ? "Zone cleared" : "Clear all hostiles in-zone",
            _ => o.Done ? "Done" : "Pending",
        };
    }
}
