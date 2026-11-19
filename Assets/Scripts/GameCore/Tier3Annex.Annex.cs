// The Loading Annex (Tier 3 beat 4, batch 4; STORY_GAUNT_LAB.md): what is inside - Tier 4 loot bags, mutants that got out of containment, and the story told Resident Evil style: papers on desks and in cells
// that E reads in the dialogue panel, one big discovery (the Elder Gaunt, "G-1", was the prototype of the Preacher: a person with a reworked throat, then replaced by a device), a vault code in two halves
// (cell-block note + the Handler's diary; 4817) and the calibration table in the vault (the quest item), handed to Osei.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static partial class Tier3Annex
    {
        public const string AnnexZone = "loading_annex", Files = "story_annex_files", VaultCode = "4817";

        static LandmarkInstance M2(string n) => Mark(n);

        /// <summary>Puts a thing on the first free enterable tile around (landmark + dx, dy): tries the offset, then a widening ring.</summary>
        static (int x, int y) Free(string mark, int dx, int dy)
        {
            var l = Mark(mark); if (l == null) return (-1, -1);
            for (int r = 0; r < 4; r++)
                for (int oy = -r; oy <= r; oy++) for (int ox = -r; ox <= r; ox++)
                {
                    int x = l.X + dx + ox, y = l.Y + dy + oy; if (!TerrainMap.CanEnterTile(x, y) || TerrainMap.InteractiveObjects.Any(o => o.X == x && o.Y == y)) continue;
                    if (TerrainMap.Landmarks.Any(m => m.X == x && m.Y == y)) continue; return (x, y);
                }
            return (-1, -1);
        }

        /// <summary>For Tools/render (ANNEX=1): puts down what the Annex holds without running the story.</summary>
        internal static void PlaceForRender() { _annexPlaced = false; PlaceAnnex(); }

        static void PlaceAnnex()
        {
            int extra = Mathf.Max(0, Player.All.Count - 1);
            // Tier 4 loot bags: the intake lockers, two cells, the control room, the office, the vault
            foreach (var (m, dx, dy, name, desc) in new[] {
                ("Intake Desk", -6, 6, "Staff Duffel", "A duffel under the intake benches, packed in a hurry and never collected."),
                ("Cell Block", -4, -3, "Cell Cache", "Somebody's things, left in a cell when the door was last closed."),
                ("Cell Block", 4, 4, "Cell Cache", "Hidden under a cot: the guards' own stash."),
                ("Control Room", 5, -3, "Technicians' Locker Bag", "A duffel of spare parts, tools and one forgotten ration case."),
                ("Handler's Office", 3, 2, "The Handler's Case", "A locked-looking case under the desk. It was not locked."),
                ("Vault", 0, 1, "Vault Stores", "Shelves of sealed crates: the Garrison's old research budget, in goods.") })
            { var (x, y) = Free(m, dx, dy); if (x >= 0) Stashes.Hoard(x, y, name, 4, desc); }
            // the papers
            Doc("Intake Desk", Desks, "Search the desk", "Intake Ledger", "A ledger in the top drawer, one page folded down.", "intake");
            Doc("Cell Block", new[] { "lockers" }, "Search the locker", "Cell Plate and Note", "A note pushed in behind the guards' coats.", "cell");
            Doc("Handler's Office", Desks, "Search the desk", "The Handler's Diary", "A black-covered diary at the back of the drawer.", "diary");
            Doc("Control Room", new[] { "console", "terminal", "desk" }, "Search the console", "Transmitter Log", "A log sheet clipped inside the console's hatch.", "transmit");
            Doc("Vault", new[] { "cabinet", "lockers" }, "Search the cabinet", "Calibration Table", "A grey folder, stamped CALIBRATION. The only thing on the middle shelf.", "calib", 4);
            // the ones that got out
            void Pack(string room, int dx, int dy, params string[] kinds)
            { int k = 0; foreach (var spec in kinds.Concat(Enumerable.Repeat(kinds[0], extra))) { var l = Mark(room); if (l == null) return; var m = Make(spec, l.X + dx + k % 3 * 2, l.Y + dy + k / 3 * 2); EncounterState.Enemies.Add(m); Annexers.Add(m); k++; } }
            Pack("Intake Desk", -4, 3, "tier2:VeilProwler", "tier2:VeilProwler");
            Pack("Intake Desk", 3, 5, "tier3:Gaunt");
            Pack("Cell Block", -3, -4, "tier4:DreadProwler", "tier2:VeilProwler");
            Pack("Cell Block", 2, 4, "tier3:Gaunt");
            Pack("Control Room", 3, -4, "tier4:DreadProwler", "tier2:VeilProwler", "tier2:VeilProwler");
            Pack("Handler's Office", 2, 2, "tier3:Gaunt");
        }
        static readonly List<Mutant> Annexers = new List<Mutant>();

        /// <summary>Yan, 2026-11-19: "the papers should be inside a desk": the nearest piece of furniture of the right kind to the landmark holds the paper (the prompt says what to search); no such piece within 12 tiles: it lies on the floor.</summary>
        static void Doc(string mark, string[] looks, string what, string name, string desc, string id, int r = 12)
        {
            var l = Mark(mark); if (l == null) return;
            (int x, int y, float d) best = (-1, -1, 1e9f);
            for (int y = l.Y - r; y <= l.Y + r; y++) for (int x = l.X - r; x <= l.X + r; x++)
            {
                if (x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height || !looks.Contains(TerrainMap.GetTile(x, y).Look ?? "")) continue;
                if (TerrainMap.InteractiveObjects.Any(o => o.X == x && o.Y == y)) continue;
                float d = (x - l.X) * (x - l.X) + (y - l.Y) * (y - l.Y); if (d < best.d) best = (x, y, d);
            }
            if (best.x >= 0 && Stashes.PaperIn(best.x, best.y, what, desc, id) != null) return;
            var (fx, fy) = Free(mark, 1, 0); if (fx >= 0) Stashes.Paper(fx, fy, name, desc, id);
        }
        static readonly string[] Desks = { "desk", "desk_n", "desk_s", "desk_e", "desk_w" };

        static void TickAnnex()
        {
            if (!TerrainMap.InteractiveObjects.Any(o => o.Lock == "doc") && !_annexPlaced && Mark("Intake Desk") != null) { _annexPlaced = true; PlaceAnnex(); }
            if (TerrainMap.InteractiveObjects.Any(o => o.Lock == "doc")) _annexPlaced = true;
        }
        static bool _annexPlaced;

        static void Both()
        {
            if (!Story.Has("doc_cell") || !Story.Has("doc_diary")) return;
            var key = LabLocks.Key("code"); if (key == null || MacroState.Current.Keys.Contains(key)) return;
            MacroState.Current.Keys.Add(key); LabLocks.Granted(key);
            GameLog.Event($"Two halves make the code: {VaultCode}. The vault's keypad will take it.");
            Story.Journal($"The Annex vault code is {VaultCode}: half from the cell block, half from the Handler's diary.");
        }

        /// <summary>E on a paper: the text in the dialogue panel, and what it changes.</summary>
        public static void ReadDoc(string id)
        {
            switch (id)
            {
                case "intake":
                    Story.Talk("INTAKE LEDGER", new[] { "ANNEX INTAKE. Entries in a clerk's hand, one a night, for a year. Most lines are crossed out in a different ink: TRANSFERRED, TRANSFERRED, TRANSFERRED.",
                        "G-1. Adult male. Volunteer. ORLOV, Matthias, 41. Former line technician, Loading Dock. Signed himself in. 'The pay is the pay.' Medical: sound. Voice: strong - a baritone. Remarks: this is the one the Handler asked for by name.",
                        "Below it, later, in another pen: G-1 is no longer fit for general intake. Move to Cell 6 for the duration of the programme. Do not use his name on the door." });
                    Story.Set("doc_intake"); Story.Journal("The Annex intake ledger: G-1 was Matthias Orlov, a loading-dock technician who volunteered. They called it a programme."); break;
                case "cell":
                    Story.Talk("CELL 6", new[] { "A steel plate on the last door: G-1. A strip of tape under it, the pen pressed hard: 'ORLOV. he can still hear us.' Inside, the floor is scratched in long parallel lines.",
                        "A second note, folded small and pushed under the plate: 'Vault - first two numbers: 4 8. Handler has the rest. If the cards stop working, it is because I stopped paying for the power. - R.'" });
                    Story.Set("doc_cell"); Story.Journal("Cell 6 held G-1 (Orlov). A guard's note gives the first half of the vault code: 4 8."); Both(); break;
                case "diary":
                    Story.Talk("THE HANDLER'S DIARY - 1", new[] { "Day 3. The surgeon is good and says little. They have reworked the throat. The call is not a sound, he says; it is a carrier. A thing the others can hear and answer. Orlov wakes up and tries to speak and it comes out wrong. Everyone in the corridor stops.",
                        "Day 9. First test, to the dogs in the yard. The first to answer was the one we named Bell. It came across the yard in a straight line and sat at his door. It did not want food. It wanted to be told what to do.",
                        "Day 31. He no longer answers to his name. When the call goes out, the cavern answers - I have heard them from here, in the walls, like weather. Those people say it is working. I wrote 'working' in the log. I do not know what else to write." ,
                        "Day 52. The new design arrives from upstream: a sphere, no throat, no man. 'A better amplifier than a throat.' They call the old one a prototype. They will not need him. I was told to code the vault for the last time. 1 7. If you find this, I was never here.",
                        "(Written large on the last page, not in his hand: 'G-1 WAS THE PROTOTYPE.')" });
                    Story.Set("doc_diary"); Story.Set("annex_discovery");
                    Story.Journal("The Handler's diary: the Elder Gaunt was the prototype of the leash. They rebuilt a man's throat so the pack would answer it, tested it on the dogs, then designed a sphere to replace him. The vault code's second half: 1 7.");
                    Both(); break;
                case "transmit":
                    Story.Talk("TRANSMITTER LOG", new[] { "Bench log, the last sheet. Test 41: carrier from G-1's cell, relayed through the old transmitter, answered at 1.4 km. Test 42: carrier from the sphere prototype, same relay. Answered at 6 km. Test 43: same, two spheres.",
                        "Remarks: 'The sphere does not tire, does not sleep, does not remember. It is a better amplifier than a throat. Request authority to move the programme to the Barrier.'" });
                    Story.Set("doc_transmit"); Story.Journal("Annex transmitter log: the first spheres worked at six kilometres. They asked to move the programme to the Barrier."); break;
                case "calib":
                    var h = Player.Current; if (h == null) return;
                    if (Tier1Story.Holder(Files) != null) { GameLog.Write("You already carry the folder."); break; }
                    Story.Talk("CALIBRATION TABLE", new[] { "The shape of the call, drawn as a table: frequency against response. Columns of numbers, a tidy hand. At the bottom, in a margin note:",
                        "'It cannot be cut, only drowned.'" }, null, _ =>
                    {
                        h.AddItem(new InventoryItem { Id = Files, Name = "Calibration Table", Type = "key", Qty = 1, WeightKg = 0.2f, Price = 0 });
                        Story.Set("annex_files_taken"); Story.SetObjective(Mission, "Take the calibration table to Dr. Osei (Sub-Station House)", "Sub-Station House");
                        Story.Journal("The calibration table: the call cannot be cut, only drowned - exactly what Osei's counter-signal does. Take it to her.");
                    });
                    break;
            }
        }

        /// <summary>E at Osei with the table: the hand-in.</summary>
        static bool AnnexHandIn()
        {
            if (!(Tier1Story.Holder(Files) is Player h)) return false;
            h.RemoveItem(Files, 1);
            Story.Talk("Dr. Priya Osei", new[] { "'It cannot be cut, only drowned.' ...Yes. That is the counter-signal. I was building a scissors for a rope that was never there.",
                "A prototype. A man. Pavel will want to know that part, and the Garrison thread: whoever the Handler answered to paid for this long before the Barrier. Crusher Row's pay notes carry the same name.",
                "One more thing. The pulses under the lake changed shape the hour the Elder fell. Something out there noticed. Go north - and take the file, I made a copy. Here is what I promised." });
            Tier1Story.Pay(1500, "the Annex");
            Story.Set("annex_files"); Story.Set("gaunt_was_those"); Story.Finish(Mission);
            Story.Journal("The Door Under the Dock: the calibration table is with Osei. The leash was built with a person, Orlov, and then replaced by a device. The pulses under the lake changed when the Elder fell.");
            return true;
        }
    }
}
