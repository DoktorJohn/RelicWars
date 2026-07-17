using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utility
{
    public class RecruitmentTimeCalculationService 
    {
        private readonly IModifierService _modifierService;

        public RecruitmentTimeCalculationService(
            IModifierService modifierService)
        {
            _modifierService = modifierService;
        }

        public async Task<double> CalculateFinalRecruitmentTimeAsync(Guid userId, City city, UnitData unit)
        {
            ModifierTagEnum categoryTag = unit.Category switch
            {
                UnitCategoryEnum.Infantry => ModifierTagEnum.InfantryRecruitmentSpeed,
                UnitCategoryEnum.Cavalry => ModifierTagEnum.CavalryRecruitmentSpeed,
                UnitCategoryEnum.Siege => ModifierTagEnum.SiegeRecruitmentSpeed,
                UnitCategoryEnum.Naval => ModifierTagEnum.NavalRecruitmentSpeed,
                _ => ModifierTagEnum.Placeholder
            };
            double finalRecruitmentSpeedMultiplier = _modifierService.CalculateCityValue(
                city, 1.0, ModifierTagEnum.RecruitmentSpeed, categoryTag).FinalValue;

            // Sikr mod division med nul og beregn tid
            double calculatedFinalRecruitmentTimeSeconds = unit.RecruitmentTimeInSeconds / Math.Max(0.1, finalRecruitmentSpeedMultiplier);

            // En rekruttering kan aldrig tage mindre end 1 sekund (gameplay balance)
            return Math.Max(calculatedFinalRecruitmentTimeSeconds, 1.0);
        }
    }
}
