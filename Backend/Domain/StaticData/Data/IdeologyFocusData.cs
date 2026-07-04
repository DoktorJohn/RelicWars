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
    public class IdeologyFocusData : IModifierProvider
    {
        public IdeologyFocusNameEnum Name { get; set; }
        public IdeologyTypeEnum RequiredIdeology { get; set; }
        public TimeSpan? TimeActive { get; set; }
        public bool SpecialFlag { get; set; }
        public IdeologyFocusEffectKindEnum EffectKind { get; set; } = IdeologyFocusEffectKindEnum.Modifier;
        public IdeologyFocusTargetScopeEnum TargetScope { get; set; } = IdeologyFocusTargetScopeEnum.City;
        public bool CanRepeat { get; set; } = true;
        public bool ConsumesOnTrigger { get; set; }
        public double IdeologyFocusPointCost { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<Modifier> ModifiersInternal { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
