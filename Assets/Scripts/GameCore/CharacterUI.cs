// New (file 00 §4 and §13): character creation (6 stat points, loadout, starting perk), the
// perk pick at Professional/Expert/Legend, and the character sheet (P) with rank milestones.
// Built at runtime under the Canvas like the other panels; the whole panel is rebuilt on every
// click, which keeps it simple.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class CharacterSpec
    {
        public int S = 10, P = 10, A = 10, I = 10;
        public string Loadout = "Scavenger";
        public string Perk;
    }

    public class CharacterUI : MonoBehaviour
    {
        private static CharacterUI _instance;
        private GameObject _panel;
        private string _mode;                    // "create" | "perk" | "sheet"
        private CharacterSpec _spec;
        private string _perk;

        public static readonly string[] Loadouts = { "Scavenger", "Ex-Military", "Freelance Trader" };
        private static readonly Dictionary<string, string> LoadoutText = new Dictionary<string, string>
        {
            ["Scavenger"] = "Hunting Knife, PSh-18 Pchela + 30 rds, Leather Jacket, 2 Basic Medkits, 1 Bandage, +200 Ru",
            ["Ex-Military"] = "VKS-74U + 60 rds, Spetsnaz Switchblade, Raider Jacket, 1 Basic Medkit, 1 Basic Stim, +100 Ru",
            ["Freelance Trader"] = "PSh-18 Pchela + 30 rds, Scrap-Plated Vest, Small Canvas Sack, 1 Basic Medkit, +500 Ru",
        };

        public static bool IsOpen => _instance != null && _instance._panel != null;

        private void Awake() => _instance = this;

        public static void ShowCreation()
        {
            if (_instance == null) { GameBootstrap.StartNewGame(); return; }
            GameState.Current = new GameState();   // clears a death screen's CombatOver
            _instance._spec = new CharacterSpec();
            _instance._perk = null;
            _instance.Open("create");
        }

        public static void ShowPerkPick()
        {
            if (_instance == null || Player.Current == null || Player.Current.PendingPerkPicks <= 0) return;
            _instance._perk = null;
            _instance.Open("perk");
        }

        public static void ShowSheet() => _instance?.Open("sheet");

        private bool _prevModal;

        private void Open(string mode)
        {
            if (_panel == null) _prevModal = GameState.Current.ModalOpen && !ChoiceUI.IsOpen;
            _mode = mode;
            GameState.Current.ModalOpen = true;
            Rebuild();
        }

        private void Close()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
            GameState.Current.ModalOpen = _prevModal || ChoiceUI.IsOpen;
        }

        private void Rebuild()
        {
            if (_panel != null) Destroy(_panel);
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            bool full = _mode == "create";
            _panel = UIBuildUtil.CreatePanel(canvas.transform, "CharacterPanel", full ? Vector2.zero : new Vector2(0.1f, 0.06f),
                full ? Vector2.one : new Vector2(0.9f, 0.94f), new Color(0.06f, 0.06f, 0.05f, 0.98f));
            _panel.transform.SetAsLastSibling();
            if (_mode == "create") BuildCreation();
            else if (_mode == "perk") BuildPerkPick();
            else BuildSheet();
        }

        // ------------------------------------------------------------------ creation

        private void BuildCreation()
        {
            var t = _panel.transform;
            Text(t, new Vector2(0.03f, 0.9f), new Vector2(0.6f, 0.98f), "NEW SCAVENGER", 30);
            int left = 6 - (_spec.S + _spec.P + _spec.A + _spec.I - 40);
            Text(t, new Vector2(0.03f, 0.84f), new Vector2(0.45f, 0.9f), $"Stats - {left} point{(left == 1 ? "" : "s")} left (6-16 each, lower one to raise another)", 15);
            string[] names = { "Strength", "Perception", "Agility", "Intelligence" };
            string[] hints = { "doors, heavy objects", "ambushes, anomalies, searching", "attacks, anomaly avoidance", "electronics, terminals, lore" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                float y0 = 0.76f - i * 0.075f, y1 = y0 + 0.06f;
                int v = Get(idx);
                Text(t, new Vector2(0.03f, y0), new Vector2(0.2f, y1), names[i], 18);
                Btn(t, new Vector2(0.2f, y0), new Vector2(0.24f, y1), "-", () => { if (Get(idx) > 6) { Set(idx, Get(idx) - 1); Rebuild(); } });
                Text(t, new Vector2(0.245f, y0), new Vector2(0.31f, y1), $"{v} ({CombatantUtil.GetStatModifier(v):+0;-0;0})", 18, TextAlignmentOptions.Center);
                Btn(t, new Vector2(0.31f, y0), new Vector2(0.35f, y1), "+", () => { if (Get(idx) < 16 && 6 - (_spec.S + _spec.P + _spec.A + _spec.I - 40) > 0) { Set(idx, Get(idx) + 1); Rebuild(); } });
                Text(t, new Vector2(0.36f, y0), new Vector2(0.5f, y1), hints[i], 12);
            }

            Text(t, new Vector2(0.03f, 0.43f), new Vector2(0.45f, 0.48f), "Loadout", 18);
            for (int i = 0; i < Loadouts.Length; i++)
            {
                string l = Loadouts[i];
                float y0 = 0.35f - i * 0.09f, y1 = y0 + 0.075f;
                var b = Btn(t, new Vector2(0.03f, y0), new Vector2(0.17f, y1), l, () => { _spec.Loadout = l; Rebuild(); });
                if (_spec.Loadout == l) Highlight(b);
                Text(t, new Vector2(0.18f, y0), new Vector2(0.5f, y1), LoadoutText[l], 12);
            }

            PerkGrid(t, new Vector2(0.52f, 0.14f), new Vector2(0.98f, 0.9f), null);
            Text(t, new Vector2(0.52f, 0.9f), new Vector2(0.98f, 0.96f), "Starting perk", 18);

            bool ready = _perk != null && left >= 0;
            var start = Btn(t, new Vector2(0.8f, 0.03f), new Vector2(0.97f, 0.1f), ready ? "Into the Zone" : "Pick a perk", () =>
            {
                if (_perk == null) return;
                _spec.Perk = _perk;
                Close();
                GameBootstrap.StartNewGame(_spec);
            });
            if (ready) Highlight(start);
            Btn(t, new Vector2(0.6f, 0.03f), new Vector2(0.77f, 0.1f), "Back", () => { Close(); MainMenuUI.Show(); });
        }

        private int Get(int i) => i == 0 ? _spec.S : i == 1 ? _spec.P : i == 2 ? _spec.A : _spec.I;
        private void Set(int i, int v) { if (i == 0) _spec.S = v; else if (i == 1) _spec.P = v; else if (i == 2) _spec.A = v; else _spec.I = v; }

        // ------------------------------------------------------------------ perk pick / sheet

        private void BuildPerkPick()
        {
            var t = _panel.transform;
            var p = Player.Current;
            Text(t, new Vector2(0.03f, 0.9f), new Vector2(0.7f, 0.98f), $"{p.Rank.ToUpperInvariant()} - PICK A PERK", 26);
            PerkGrid(t, new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.88f), p.ActivePerks);
            var ok = Btn(t, new Vector2(0.78f, 0.03f), new Vector2(0.97f, 0.1f), _perk == null ? "Pick one" : "Take it", () =>
            {
                if (_perk == null || !p.TakePerk(_perk)) return;
                p.PendingPerkPicks--;
                if (p.PendingPerkPicks > 0) { _perk = null; Rebuild(); } else Close();
            });
            if (_perk != null) Highlight(ok);
            Btn(t, new Vector2(0.58f, 0.03f), new Vector2(0.76f, 0.1f), "Later (P)", Close);
        }

        private void BuildSheet()
        {
            var t = _panel.transform;
            var p = Player.Current;
            if (p == null) { Close(); return; }
            var sb = new StringBuilder();
            sb.AppendLine($"<size=24>{p.Rank.ToUpperInvariant()}</size>   Ru {p.Ru}   AP {p.MaxAp}   AC {p.Ac}   HP {p.Hp}/{p.MaxHp}");
            sb.AppendLine($"S {p.Stats.S}  P {p.Stats.P}  A {p.Stats.A}  I {p.Stats.I}");
            sb.AppendLine();
            sb.AppendLine("<color=#e0c060>REPUTATION</color>  (-3 Hostile, +3 Friendly)");
            sb.AppendLine("  " + string.Join("   ", FactionDatabase.Factions.Keys.Where(f => f != "Mutant")
                .Select(f => $"{f} {p.RepPoints(f):+0;-0;0}{(p.GetRepLevel(f) == "Friendly" ? "*" : p.GetRepLevel(f) == "Hostile" ? "!" : "")}")));
            sb.AppendLine();
            sb.AppendLine("<color=#e0c060>PERKS</color>");
            foreach (var k in p.ActivePerks)
                sb.AppendLine(PerkDatabase.All.TryGetValue(k, out var d) ? $"  {d.Name} - {d.Description}" : "  " + k);
            sb.AppendLine();
            var next = p.NextRank;
            if (next != null)
            {
                sb.AppendLine($"<color=#e0c060>NEXT RANK: {next.ToUpperInvariant()}</color>");
                foreach (var (text, met) in p.MilestonesFor(next))
                    sb.AppendLine($"  {(met ? "<color=#80d080>[x]</color>" : "[ ]")} {text}");
            }
            sb.AppendLine();
            sb.AppendLine($"Quests done {p.QuestsCompleted} | deepest tier {p.MaxTierReached} | toughest mutant killed T{p.MaxMutantTierKilled} | storylines finished {p.ArcsFinished}");
            Text(t, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.97f), sb.ToString(), 16, TextAlignmentOptions.TopLeft);
            Btn(t, new Vector2(0.8f, 0.03f), new Vector2(0.97f, 0.1f), "Close", Close);
            if (p.PendingPerkPicks > 0)
                Highlight(Btn(t, new Vector2(0.55f, 0.03f), new Vector2(0.78f, 0.1f), $"Pick perk ({p.PendingPerkPicks})", () => { _perk = null; _mode = "perk"; Rebuild(); }));
        }

        /// <summary>Every perk as a button grid, grouped by weapon family; a description of the
        /// selected one underneath.</summary>
        private void PerkGrid(Transform t, Vector2 min, Vector2 max, List<string> owned)
        {
            var perks = PerkDatabase.All.ToList();
            int cols = 3, rows = (perks.Count + cols - 1) / cols;
            float descH = 0.1f;
            float w = (max.x - min.x) / cols, h = (max.y - min.y - descH) / rows;
            for (int i = 0; i < perks.Count; i++)
            {
                var kv = perks[i];
                int c = i / rows, r = i % rows;
                var a = new Vector2(min.x + c * w + 0.003f, max.y - (r + 1) * h + 0.002f);
                var b = new Vector2(min.x + (c + 1) * w - 0.003f, max.y - r * h - 0.002f);
                bool has = owned != null && owned.Contains(kv.Key);
                bool locked = kv.Value.Requires != null && (owned == null || !owned.Contains(kv.Value.Requires));   // Batch 80
                var btn = Btn(t, a, b, $"{kv.Value.Name} <size=10><color=#999>{(locked ? "needs " + PerkDatabase.All[kv.Value.Requires].Name : kv.Value.Group)}</color></size>", () => { if (!has && !locked) { _perk = kv.Key; Rebuild(); } });
                btn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 13;
                if (has) btn.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.15f, 1f);
                else if (locked) btn.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
                if (_perk == kv.Key) Highlight(btn);
            }
            string desc = _perk != null ? $"<b>{PerkDatabase.All[_perk].Name}</b>: {PerkDatabase.All[_perk].Description}" : "Click a perk to read it.";
            Text(t, new Vector2(min.x, min.y), new Vector2(max.x, min.y + descH - 0.01f), desc, 14);
        }

        private static TextMeshProUGUI Text(Transform t, Vector2 a, Vector2 b, string s, int size, TextAlignmentOptions align = TextAlignmentOptions.Left)
            => UIBuildUtil.CreateText(t, "Text", a, b, s, size, Color.white, align);

        private static Button Btn(Transform t, Vector2 a, Vector2 b, string label, System.Action onClick)
            => UIBuildUtil.CreateButton(t, "Btn", a, b, label, onClick);

        private static void Highlight(Button b) => b.GetComponent<Image>().color = new Color(0.55f, 0.42f, 0.12f, 1f);
    }
}
