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
        private readonly BuildingDataReader _dataReader;
        private readonly IResearchService _userResearchModifierService;

        public RecruitmentTimeCalculationService(
            BuildingDataReader dataReader,
            IResearchService userResearchModifierService)
        {
            _dataReader = dataReader;
            _userResearchModifierService = userResearchModifierService;
        }

        public async Task<double> CalculateFinalRecruitmentTimeAsync(Guid userId, City city, UnitData unit)
        {
            List<ModifierTagEnum> applicableModifierTags = new List<ModifierTagEnum>(unit.ModifiersThatAffectsThis);

            if (!applicableModifierTags.Contains(ModifierTagEnum.Recruitment))
            {
                applicableModifierTags.Add(ModifierTagEnum.Recruitment);
            }

            List<Modifier> applicableModifiersGathered = new List<Modifier>();

            // Bestem produktionsbygning baseret på enhedens kategori
            BuildingTypeEnum productionBuildingType = unit.Category switch
            {
                UnitCategoryEnum.Infantry => BuildingTypeEnum.Barracks,
                UnitCategoryEnum.Cavalry => BuildingTypeEnum.Stable,
                UnitCategoryEnum.Siege => BuildingTypeEnum.Workshop,
                _ => BuildingTypeEnum.Barracks
            };

            // Hent modifikatorer fra bygningsniveau
            Building buildingEntityInCity = city.Buildings.FirstOrDefault(b => b.Type == productionBuildingType);

            if (buildingEntityInCity != null && buildingEntityInCity.Level > 0)
            {
                BuildingLevelData buildingLevelConfiguration = _dataReader
                    .GetConfig<BuildingLevelData>(productionBuildingType, buildingEntityInCity.Level);

                if (buildingLevelConfiguration != null)
                {
                    applicableModifiersGathered.AddRange(buildingLevelConfiguration.ModifiersInternal);
                }
            }

            // Hent modifikatorer fra brugerens forskning
            IEnumerable<Modifier> userResearchModifiers = await _userResearchModifierService
                .GetUserResearchModifiersAsync(userId);

            applicableModifiersGathered.AddRange(userResearchModifiers);

            // Beregn den endelige multiplikator via StatCalculator (antaget stadig statisk utility)
            double finalRecruitmentSpeedMultiplier = StatCalculator.ApplyModifiers(
                1.0,
                applicableModifierTags,
                applicableModifiersGathered);

            // Sikr mod division med nul og beregn tid
            double calculatedFinalRecruitmentTimeSeconds = unit.RecruitmentTimeInSeconds / Math.Max(0.1, finalRecruitmentSpeedMultiplier);

            // En rekruttering kan aldrig tage mindre end 1 sekund (gameplay balance)
            return Math.Max(calculatedFinalRecruitmentTimeSeconds, 1.0);
        }
    }
}
