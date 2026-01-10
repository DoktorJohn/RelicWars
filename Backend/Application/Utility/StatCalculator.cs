using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utility
{
    public static class StatCalculator
    {
        public static double ApplyModifiers(double baseValue, IEnumerable<ModifierTagEnum> targetTags, IEnumerable<ModifierData> allModifiers)
        {
            // 1. Find alle modifiers, hvis tag matcher et af de tags, vi leder efter
            // (F.eks. find alt der har tagget 'Infantry' ELLER 'Recruitment')
            var relevantMods = allModifiers
                .Where(m => targetTags.Contains(m.Tag))
                .ToList();

            // 2. Beregn flade tillæg (Additive)
            double flatBonus = relevantMods
                .Where(m => m.Type == ModifierTypeEnum.Additive)
                .Sum(m => m.Value);

            // 3. Beregn den samlede procentvise ændring (Increased - Decreased)
            double totalIncreased = relevantMods
                .Where(m => m.Type == ModifierTypeEnum.Increased)
                .Sum(m => m.Value);

            double totalDecreased = relevantMods
                .Where(m => m.Type == ModifierTypeEnum.Decreased)
                .Sum(m => m.Value);

            // Multiplier starter på 1 (100%)
            double multiplier = 1 + totalIncreased - totalDecreased;

            // 4. Returner resultatet
            // Bemærk: Hvis vi beregner TID (som i rekruttering), bruger vi ofte division:
            // (Base + Flat) / Multiplier. 
            // Hvis vi beregner PRODUKTION (træ/sten), bruger vi multiplikation:
            // (Base + Flat) * Multiplier.

            return (baseValue + flatBonus) * Math.Max(0.1, multiplier);
        }
    }
}
