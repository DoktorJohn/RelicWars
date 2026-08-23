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
    public enum ResearchPrerequisiteRule
    {
        Start,
        RequiresAll,
        RequiresAny
    }

    public enum ResearchNodeKind
    {
        Origin,
        Notable,
        Keystone
    }

    public class ResearchData : IModifierProvider
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ResearchTypeEnum ResearchType { get; set; }

        // Lænken: Hvilken research skal være færdig før denne kan startes?
        public string? ParentId { get; set; }

        // The authored tree may contain more than one prerequisite. ParentId remains
        // as a compatibility field for the existing single-parent research flow.
        public List<string> PrerequisiteIds { get; set; } = new();
        public ResearchPrerequisiteRule PrerequisiteRule { get; set; } = ResearchPrerequisiteRule.Start;
        public int Tier { get; set; }
        public ResearchNodeKind NodeKind { get; set; } = ResearchNodeKind.Notable;
        public bool IsResearchable { get; set; } = true;

        // Work required to complete the research at a 1.00x speed multiplier.
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
