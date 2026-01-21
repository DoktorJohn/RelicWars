using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.StaticData.Data
{
    public class ResearchData : IModifierProvider
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ResearchTypeEnum ResearchType { get; set; }

        // Lænken: Hvilken research skal være færdig før denne kan startes?
        public string? ParentId { get; set; }

        // Krav til bygninger (typisk University level)
        public double ResearchPointCost { get; set; }

        public int ResearchTimeInSeconds { get; set; }

        // Bonussen (Dette skal din motor læse når den beregner produktion/kamp)
        public List<Modifier> ModifiersInternal { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
