using System.Linq;

namespace GameCore
{
    /// <summary>2026-11-19: the one reader for papers on the map (Stashes "doc" objects, E to read). Tier 2's points of interest (Tier2Poi.DocTexts) are plain pages; the Annex's papers have their own effects (Tier3Annex.ReadDoc).</summary>
    public static class Docs
    {
        public static void Read(string id)
        {
            if (id != null && Tier2Poi.DocTexts.TryGetValue(id, out var d))
            {
                Story.Talk(d.title, d.pages);
                if (!Story.Has("doc_" + id)) { Story.Set("doc_" + id); Story.Journal(d.journal); }
                return;
            }
            Tier3Annex.ReadDoc(id);
        }
    }
}
