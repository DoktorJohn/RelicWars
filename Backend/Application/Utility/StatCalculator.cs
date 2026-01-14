using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utility
{
    public static class StatCalculator
    {
        public static double ApplyModifiers(double baseValue, IEnumerable<ModifierTagEnum> targetTags, IEnumerable<Modifier>? allModifiers)
        {
            // 1. Sikkerhedstjek: Hvis listen er null eller tom, returneres basisværdien med det samme.
            // Vi bruger 'Any()' til at tjekke for indhold på en effektiv måde.
            if (allModifiers == null || !allModifiers.Any())
            {
                return baseValue;
            }

            // 2. Filtrering: Find alle relevante modifiers baseret på de medsendte tags.
            // Vi konverterer til en liste med det samme for at undgå at iterere over allModifiers flere gange.
            var relevantModifiersList = allModifiers
                .Where(modifier => targetTags.Contains(modifier.Tag))
                .ToList();

            // Hvis ingen af de fundne modifiers matcher de relevante tags, returneres basisværdien.
            if (relevantModifiersList.Count == 0)
            {
                return baseValue;
            }

            // 3. Beregn flade tillæg (Flat)
            // Summerer værdier som f.eks. +10 træ produktion.
            double totalFlatBonusValue = relevantModifiersList
                .Where(modifier => modifier.Type == ModifierTypeEnum.Flat)
                .Sum(modifier => modifier.Value);

            // 4. Beregn procentvise ændringer (Increased og Decreased)
            // Summerer alle 'Increased' (f.eks. +0.10 for 10%) og trækker 'Decreased' fra.
            double totalIncreasedPercentage = relevantModifiersList
                .Where(modifier => modifier.Type == ModifierTypeEnum.Increased)
                .Sum(modifier => modifier.Value);

            double totalDecreasedPercentage = relevantModifiersList
                .Where(modifier => modifier.Type == ModifierTypeEnum.Decreased)
                .Sum(modifier => modifier.Value);

            // Multiplier starter på 1.0 (svarende til 100%). 
            // En samlet stigning på 20% resulterer i en multiplier på 1.20.
            double calculatedTotalMultiplier = 1.0 + totalIncreasedPercentage - totalDecreasedPercentage;

            // 5. Konsolidering af resultat
            // Vi sikrer, at multiplieren aldrig kommer under 0.1 (10%), så produktion/stats aldrig bliver negative eller nul.
            double finalMultiplierClamped = Math.Max(0.1, calculatedTotalMultiplier);

            // Den matematiske formel for beregningen:
            // $$ \text{Resultat} = (\text{baseValue} + \text{totalFlatBonusValue}) \times \text{finalMultiplierClamped} $$
            return (baseValue + totalFlatBonusValue) * finalMultiplierClamped;
        }
    }
}
