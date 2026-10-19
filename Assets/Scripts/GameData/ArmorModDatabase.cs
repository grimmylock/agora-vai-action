// Ported from database.js: ARMOR_MOD_DB
using System.Collections.Generic;

namespace GameData
{
    public class ArmorModDef
    {
        public string Name;
        public float? PhysDRPercentAdd;
        public float? AnomDRPercentAdd;
        public string Target;          // e.g. "mutant"
        public int? MpModifier;
        public int? BeltSlotBonus;
        public int? CarryCapacityBonus;
        public string Description;
    }

    public static class ArmorModDatabase
    {
        public static readonly Dictionary<string, ArmorModDef> All = new Dictionary<string, ArmorModDef>
        {
            ["AramidWeave"] = new ArmorModDef { Name = "Aramid Weave", PhysDRPercentAdd = 0.10f, Description = "+10% Phys-DR" },
            ["CeramicPlates"] = new ArmorModDef { Name = "Ceramic Plates", PhysDRPercentAdd = 0.20f, Description = "+20% Phys-DR" },
            ["TreatedMutantHide"] = new ArmorModDef { Name = "Treated Mutant Hide", PhysDRPercentAdd = 0.15f, Target = "mutant", Description = "+15% Phys-DR vs mutants" },
            ["TitaniumGrid"] = new ArmorModDef { Name = "Titanium Grid", PhysDRPercentAdd = 0.15f, MpModifier = -1, Description = "+15% Phys-DR, -1 MP" },
            ["LeadLinedFabric"] = new ArmorModDef { Name = "Lead-Lined Fabric", AnomDRPercentAdd = 0.30f, Description = "+30% Anom-DR" },
            ["AsbestosCoating"] = new ArmorModDef { Name = "Asbestos Coating", AnomDRPercentAdd = 0.30f, Description = "+30% Anom-DR Burn" },
            ["FaradayMesh"] = new ArmorModDef { Name = "Faraday Mesh", AnomDRPercentAdd = 0.30f, Description = "+30% Anom-DR Livewire" },
            ["PsiStimulatorEarpiece"] = new ArmorModDef { Name = "Psi-Stimulator Earpiece", AnomDRPercentAdd = 0.20f, Description = "+20% Anom-DR Psi" },
            ["ArtifactPouch"] = new ArmorModDef { Name = "Artifact Pouch", BeltSlotBonus = 1, Description = "+1 Belt Slot" },
            ["Servomotors"] = new ArmorModDef { Name = "Servomotors", CarryCapacityBonus = 10, Description = "+10kg carry capacity" },
            ["AdvancedHydraulics"] = new ArmorModDef { Name = "Advanced Hydraulics", CarryCapacityBonus = 20, Description = "+20kg carry capacity, cancels heavy armor MP penalty" },
            ["AutoInjector"] = new ArmorModDef { Name = "Auto-Injector", Description = "Auto-Injector effect" },
            ["ClosedBreathing"] = new ArmorModDef { Name = "Closed-Breathing", Description = "Closed-Breathing system" },
        };
    }
}
