using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utility
{
    public static class MilitaryUnitModifierHelper
    {
        public static (int wood, int stone, int metal) GetModifiedCosts(
            IModifierService modifierService, City city, UnitData unitStaticData)
        {
            var categoryCostTag = unitStaticData.Category switch
            {
                UnitCategoryEnum.Infantry => ModifierTagEnum.InfantryCost,
                UnitCategoryEnum.Cavalry => ModifierTagEnum.CavalryCost,
                UnitCategoryEnum.Siege => ModifierTagEnum.SiegeCost,
                UnitCategoryEnum.Naval => ModifierTagEnum.NavalCost,
                _ => ModifierTagEnum.Placeholder
            };

            // Vi sender både det generelle UnitCost (hvis det findes) og det specifikke kategori-tag
            double wood = modifierService.CalculateCityValue(city, unitStaticData.WoodCost, categoryCostTag).FinalValue;
            double stone = modifierService.CalculateCityValue(city, unitStaticData.StoneCost, categoryCostTag).FinalValue;
            double metal = modifierService.CalculateCityValue(city, unitStaticData.MetalCost, categoryCostTag).FinalValue;

            return ((int)Math.Floor(wood), (int)Math.Floor(stone), (int)Math.Floor(metal));
        }

        public static (int power, int armor, int discipline, int mobility, int reach, int loot) GetModifiedStats(
            IModifierService modifierService, City city, UnitData unitStaticData)
        {
            var categoryStatTag = unitStaticData.Category switch
            {
                UnitCategoryEnum.Infantry => ModifierTagEnum.InfantryStats,
                UnitCategoryEnum.Cavalry => ModifierTagEnum.CavalryStats,
                UnitCategoryEnum.Siege => ModifierTagEnum.SiegeStats,
                UnitCategoryEnum.Naval => ModifierTagEnum.NavalStats,
                _ => ModifierTagEnum.Placeholder
            };

            // Vi udregner som double for at få den præcise procentvise beregning
            // Vi kombinerer det specifikke stat-tag (f.eks. Power) med kategori-tagget (f.eks. InfantryStats)
            double power = modifierService.CalculateCityValue(city, unitStaticData.Power, ModifierTagEnum.Power, categoryStatTag).FinalValue;
            double armor = modifierService.CalculateCityValue(city, unitStaticData.Armor, ModifierTagEnum.Armor, categoryStatTag).FinalValue;
            double discipline = modifierService.CalculateCityValue(city, unitStaticData.Discipline, ModifierTagEnum.Discipline, categoryStatTag).FinalValue;
            double mobility = modifierService.CalculateCityValue(city, unitStaticData.Mobility, ModifierTagEnum.TravelSpeed, categoryStatTag).FinalValue;
            double reach = modifierService.CalculateCityValue(city, unitStaticData.Reach, categoryStatTag).FinalValue;
            double loot = modifierService.CalculateCityValue(city, unitStaticData.LootCapacity, ModifierTagEnum.LootCapacity, categoryStatTag).FinalValue;

            return (
                (int)Math.Floor(power),
                (int)Math.Floor(armor),
                (int)Math.Floor(discipline),
                (int)Math.Floor(mobility),
                (int)Math.Floor(reach),
                (int)Math.Floor(loot)
            );
        }

        public static int GetModifiedRecruitmentTime(
            IModifierService modifierService, City city, UnitData unitStaticData)
        {
            // Find det specifikke tag baseret på kategorien
            var categoryRecruitmentTag = unitStaticData.Category switch
            {
                UnitCategoryEnum.Infantry => ModifierTagEnum.InfantryRecruitmentSpeed,
                UnitCategoryEnum.Cavalry => ModifierTagEnum.CavalryRecruitmentSpeed,
                UnitCategoryEnum.Siege => ModifierTagEnum.SiegeRecruitmentSpeed,
                UnitCategoryEnum.Naval => ModifierTagEnum.NavalRecruitmentSpeed,
                _ => ModifierTagEnum.Placeholder
            };

            // Vi beregner multiplikatoren ved at samle både generel RecruitmentSpeed og den kategori-specifikke
            // BaseValue er 1.0, så hvis vi har +10% på begge, ender vi på en FinalValue på 1.20
            var speedResult = modifierService.CalculateCityValue(city, 1.0, ModifierTagEnum.RecruitmentSpeed, categoryRecruitmentTag);

            // Vi sikrer, at multiplikatoren aldrig er så lav, at rekrutteringstiden stikker af (division med nul)
            double multiplier = Math.Max(0.1, speedResult.FinalValue);

            // Formel: Tid / (1 + bonus)
            double finalTime = unitStaticData.RecruitmentTimeInSeconds / multiplier;

            // Returner resultatet, dog minimum 1 sekund
            return (int)Math.Max(1.0, Math.Floor(finalTime));
        }
    }
}
