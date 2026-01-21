using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ModifierDTO
    {
        public double Value { get; set; }
        public ModifierTypeEnum ModifierType { get; set; }
        public ModifierTagEnum ModifierTag { get; set; }
    }

    public class ModifierCalculationResult
    {
        public double BaseValue { get; init; }
        public double FlatBonus { get; init; }
        public double PercentageBonus { get; init; }
        public double FinalValue { get; init; }
        public List<Modifier> AppliedModifiers { get; init; } = new();

        public void Deconstruct(out double finalValue, out List<Modifier> modifiers)
        {
            finalValue = FinalValue;
            modifiers = AppliedModifiers;
        }
    }
}
