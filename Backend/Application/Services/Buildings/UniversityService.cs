using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Buildings
{
    public class UniversityService : IUniversityService
    {
        private readonly IResearchRateCalculator _researchRateCalculator;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly TimeProvider _timeProvider;

        public UniversityService(
            IResearchRateCalculator researchRateCalculator,
            IPlayerAccessService playerAccessService,
            TimeProvider timeProvider)
        {
            _researchRateCalculator = researchRateCalculator;
            _playerAccessService = playerAccessService;
            _timeProvider = timeProvider;
        }

        public async Task<List<UniversityInfoDTO>> GetUniversityInfoAsync(Guid cityId)
        {
            // 1. Hent byen for at få adgang til alle aktive modifier providers (Research, Ideology, etc.)
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);

            var universityBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.University);
            int currentBuildingLevel = universityBuilding?.Level ?? 0;

            var projectionList = new List<UniversityInfoDTO>();

            var player = cityEntity.WorldPlayer
                ?? throw new InvalidOperationException("Byens ejer blev ikke fundet.");
            DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

            // Vi looper: Nuværende level + de næste 5 (max level 20)
            for (int i = 0; i < 5; i++)
            {
                int levelToCheck = currentBuildingLevel + i;

                if (levelToCheck > 20) break;

                var power = _researchRateCalculator.CalculateCityPower(
                    player,
                    cityEntity,
                    levelToCheck,
                    now);

                projectionList.Add(new UniversityInfoDTO
                {
                    Level = levelToCheck,
                    ResearchPower = power.EffectiveResearchPower,
                    IsCurrentLevel = (levelToCheck == currentBuildingLevel)
                });
            }

            return projectionList;
        }
    }
}
