// New (file 10): the World panel - Day, today's zone event, surge warning, faction goal
// tracks, rumors heard and named NPCs. One text block, rebuilt on open. Toggled with W.
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace GameCore
{
    public class WorldUI : MonoBehaviour
    {
        private static WorldUI _instance;
        private GameObject _panel;
        private TextMeshProUGUI _body;

        private void Awake() => _instance = this;

        public static void Toggle() => _instance?.ToggleInternal();

        private void ToggleInternal()
        {
            if (_panel != null && _panel.activeSelf) { Close(); return; }
            if (_panel == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null) return;
                _panel = UIBuildUtil.CreatePanel(canvas.transform, "WorldPanel", new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f),
                    new Color(0.08f, 0.08f, 0.08f, 0.95f));
                UIBuildUtil.CreateButton(_panel.transform, "CloseButton", new Vector2(0.86f, 0.92f), new Vector2(0.98f, 0.99f), "Close", Close);
                _body = UIBuildUtil.CreateText(_panel.transform, "Body", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.91f),
                    "", 15, Color.white, TextAlignmentOptions.TopLeft);
            }
            _body.text = Describe();
            _panel.SetActive(true);
            GameState.Current.ModalOpen = true;
        }

        private void Close()
        {
            if (_panel != null) _panel.SetActive(false);
            GameState.Current.ModalOpen = false;
        }

        public static string Describe()
        {
            var m = MacroState.Current;
            var sb = new StringBuilder();
            sb.AppendLine($"<size=20>THE ZONE - DAY {m.Day}</size>");
            if (!string.IsNullOrEmpty(m.EventOfDay)) sb.AppendLine($"Today: {m.EventOfDay}");
            if (m.SurgeRounds > 0) sb.AppendLine($"<color=#ff5040>SURGE in {m.SurgeRounds} round(s) - get indoors!</color>");
            sb.AppendLine($"Crossings since rest: {m.CrossingsSinceRest}/3");
            sb.AppendLine();
            sb.AppendLine($"<color=#e0c060>MAIN STORY - ACT {System.Math.Min(5, StorySystem.Act)}</color>   Fragments: {StorySystem.Fragments}");
            sb.AppendLine("  " + StorySystem.Objective);
            foreach (var o in Story.Objectives) sb.AppendLine($"  {o.text}{(string.IsNullOrEmpty(o.landmark) ? "" : $" ({o.landmark})")}");   // Story 3
            sb.AppendLine();
            sb.AppendLine("<color=#e0c060>FACTION GOALS</color>");
            foreach (var g in WorldEvents.Goals)
            {
                bool lost = g.Faction == "Sentinels" && WorldEvents.Done("Liberators") || g.Faction == "Liberators" && WorldEvents.Done("Sentinels");
                sb.AppendLine(WorldEvents.Done(g.Faction) ? $"  {g.Faction}: DONE - {g.Result}"
                    : lost ? $"  {g.Faction}: lost the barracks"
                    : $"  {g.Faction} {WorldEvents.Track(g.Faction)} {g.Goal}");
            }
            sb.AppendLine();
            sb.AppendLine("<color=#e0c060>RUMORS</color>");
            if (m.Rumors.Count == 0) sb.AppendLine("  Nothing yet. Talk to people in a Safe Zone.");
            foreach (var r in m.Rumors.Take(6)) sb.AppendLine("  - " + r);
            sb.AppendLine();
            sb.AppendLine("<color=#e0c060>PEOPLE YOU KNOW</color>");
            if (m.NamedNpcs.Count == 0) sb.AppendLine("  Nobody yet.");
            foreach (var n in m.NamedNpcs) sb.AppendLine("  " + n);
            return sb.ToString();
        }
    }
}
