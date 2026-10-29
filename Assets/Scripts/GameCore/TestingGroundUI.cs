// The Testing Ground's panel (TestingGround.cs): the armory pages (weapons, armor, supplies: take or equip, free) and the
// spawner pages (humans by faction, rank and count; mutants by kind), with ammo, heal and clear on every page. Built at
// runtime under the Canvas like CorpseLootUI. ponytail: pages of rows instead of a ScrollRect, like the shop.
using System.Collections.Generic;
using System.Linq;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class TestingGroundUI : MonoBehaviour
    {
        const int Rows = 14;
        static readonly string[] Tabs = { "Weapons", "Armor", "Supplies", "Humans", "Mutants" };
        static readonly int[] Counts = { 1, 3, 5, 8 };

        static TestingGroundUI _instance;
        GameObject _panel;
        Transform _body;
        int _tab, _page, _faction, _rank, _count = 1;
        bool _aware = true, _near;

        void Awake() => _instance = this;

        public static void Open(bool spawner) { if (_instance == null) return; _instance._tab = spawner ? 3 : 0; _instance._page = 0; _instance.Show(); }

        void Show()
        {
            if (_panel == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null) return;
                _panel = UIBuildUtil.CreatePanel(canvas.transform, "TestingGroundPanel", new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), new Color(0.06f, 0.065f, 0.07f, 0.96f));
                _body = UIBuildUtil.CreatePanel(_panel.transform, "Body", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0)).transform;
            }
            GameState.Current.ModalOpen = true;
            _panel.SetActive(true);
            Rebuild();
        }

        void Close() { GameState.Current.ModalOpen = false; if (_panel != null) _panel.SetActive(false); }

        static string[] Factions => FactionDatabase.Factions.Keys.Where(f => f != "Mutant").ToArray();

        /// <summary>Everything on one page as (label, buttons): rebuilt from scratch on every click.</summary>
        List<(string label, (string text, System.Action act)[] buttons)> Entries()
        {
            var p = Player.Current;
            var list = new List<(string, (string, System.Action)[])>();
            if (_tab <= 2)
            {
                var items = ShopDatabase.Items.Where(d => _tab == 0 ? d.Type == "weapon" : _tab == 1 ? d.Type == "armor" : d.Type != "weapon" && d.Type != "armor")
                    .OrderBy(d => d.Category).ThenBy(d => d.Tier ?? 0).ThenBy(d => d.Name);
                foreach (var d in items)
                {
                    string label = _tab == 2 ? d.Name : $"{d.Name}   <color=#8a8f94>{d.Category} - tier {d.Tier}</color>";
                    list.Add((label, _tab == 2 ? new (string, System.Action)[] { ("Take 5", () => TestingGround.Take(p, d, false)) }
                        : new (string, System.Action)[] { ("Take", () => TestingGround.Take(p, d, false)), ("Equip", () => TestingGround.Take(p, d, true)) }));
                }
            }
            else if (_tab == 3)
            {
                list.Add(("<color=#ff5a3a>BOSS</color> The Visor - hatchet duel (F parries)", new (string, System.Action)[] { ("Fight", () => TestingGround.VisorDuel(p)) }));   // Boss 1
                list.Add(("Target dummy, no armor", new (string, System.Action)[] { ("Dummy", () => TestingGround.SpawnDummy(p, null, null, _near)) }));
                foreach (var f in Factions)   // "Dummy": kitted as this faction at the chosen rank, but it never moves or fights
                    list.Add(($"{f}", new (string, System.Action)[] { ("Dummy", () => TestingGround.SpawnDummy(p, f, CombatRules.RankOrder[_rank], _near)),
                        ("Spawn", () => TestingGround.SpawnHumans(p, f, CombatRules.RankOrder[_rank], _count, _near, _aware)) }));
            }
            else
                foreach (var m in TestingGround.AllMutants())
                    list.Add(($"{m.Name}   <color=#8a8f94>tier {m.Tier}</color>", new (string, System.Action)[] { ("Spawn", () => TestingGround.SpawnMutants(p, m, _count, _near, _aware)) }));
            return list;
        }

        void Rebuild()
        {
            foreach (Transform ch in _body) Destroy(ch.gameObject);
            var p = Player.Current;
            UIBuildUtil.CreateText(_body, "Title", new Vector2(0.02f, 0.93f), new Vector2(0.7f, 0.99f), "TESTING GROUND", 22, HudView.Yellow, TextAlignmentOptions.Left);
            Btn(new Vector2(0.84f, 0.93f), new Vector2(0.98f, 0.99f), "Close", Close);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int t = i;
                var b = Btn(new Vector2(0.02f + i * 0.192f, 0.86f), new Vector2(0.2f + i * 0.192f, 0.92f), Tabs[i], () => { _tab = t; _page = 0; });
                if (i == _tab) b.GetComponent<Image>().color = new Color(0.45f, 0.38f, 0.12f, 1f);
            }
            // options for the spawner pages
            if (_tab >= 3)
            {
                float x = 0.02f;
                void Opt(float w, string text, System.Action act) { Btn(new Vector2(x, 0.79f), new Vector2(x + w, 0.85f), text, act); x += w + 0.01f; }
                if (_tab == 3) Opt(0.2f, "Rank: " + CombatRules.RankOrder[_rank], () => _rank = (_rank + 1) % CombatRules.RankOrder.Length);
                Opt(0.14f, "Count: " + _count, () => _count = Counts[(System.Array.IndexOf(Counts, _count) + 1) % Counts.Length]);
                Opt(0.2f, _aware ? "Aware of you" : "Unaware", () => _aware = !_aware);
                Opt(0.22f, _near ? "Near you" : "On the pad", () => _near = !_near);
            }
            // the rows
            var all = Entries();
            int pages = System.Math.Max(1, (all.Count + Rows - 1) / Rows);
            _page = System.Math.Min(_page, pages - 1);
            float top = _tab >= 3 ? 0.77f : 0.84f, rowH = (top - 0.1f) / Rows;
            for (int r = 0; r < Rows && _page * Rows + r < all.Count; r++)
            {
                var (label, buttons) = all[_page * Rows + r];
                float y1 = top - r * rowH, y0 = y1 - rowH + 0.004f;
                UIBuildUtil.CreatePanel(_body, "Row", new Vector2(0.02f, y0), new Vector2(0.98f, y1), new Color(1f, 1f, 1f, r % 2 == 0 ? 0.04f : 0.02f));
                UIBuildUtil.CreateText(_body, "Label", new Vector2(0.03f, y0), new Vector2(0.7f, y1), label, 15, HudView.Cream, TextAlignmentOptions.Left);
                for (int k = 0; k < buttons.Length; k++)
                {
                    var (text, act) = buttons[buttons.Length - 1 - k];
                    Btn(new Vector2(0.86f - k * 0.13f, y0 + 0.003f), new Vector2(0.98f - k * 0.13f, y1 - 0.003f), text, act);
                }
            }
            // footer: pages, and the always-there helpers
            Btn(new Vector2(0.02f, 0.02f), new Vector2(0.1f, 0.08f), "<", () => _page = (_page + pages - 1) % pages);
            UIBuildUtil.CreateText(_body, "Page", new Vector2(0.1f, 0.02f), new Vector2(0.2f, 0.08f), $"{_page + 1} / {pages}", 15, HudView.Cream, TextAlignmentOptions.Center);
            Btn(new Vector2(0.2f, 0.02f), new Vector2(0.28f, 0.08f), ">", () => _page = (_page + 1) % pages);
            Btn(new Vector2(0.29f, 0.02f), new Vector2(0.39f, 0.08f), "2 pistols", () => TestingGround.GunslingerKit(p));   // RT 7z-c
            Btn(new Vector2(0.4f, 0.02f), new Vector2(0.58f, 0.08f), "Refill ammo", () => TestingGround.RefillAmmo(p));
            Btn(new Vector2(0.6f, 0.02f), new Vector2(0.76f, 0.08f), "Heal", () => TestingGround.Heal(p));
            Btn(new Vector2(0.78f, 0.02f), new Vector2(0.98f, 0.08f), "Clear range", TestingGround.Clear);
        }

        /// <summary>A button that redraws the panel after it acts (so counts, pages and toggles show at once).</summary>
        Button Btn(Vector2 a, Vector2 b, string text, System.Action act) =>
            UIBuildUtil.CreateButton(_body, text + "Button", a, b, text, () => { act(); if (_panel.activeSelf) Rebuild(); });
    }
}
