using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.StaticData.Data
{
    public class ResearchData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Lænken: Hvilken research skal være færdig før denne kan startes?
        public string? ParentId { get; set; }

        // Krav til bygninger (typisk Academy level)
        public int RequiredAcademyLevel { get; set; }

        // Pris og tid
        public int WoodCost { get; set; }
        public int StoneCost { get; set; }
        public int MetalCost { get; set; }
        public int ResearchTimeInSeconds { get; set; }

        // Bonussen (Dette skal din motor læse når den beregner produktion/kamp)
        public List<ModifierData> Modifiers { get; set; } = new();
    }
}
