using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RecruitmentService : IRecruitmentService
    {
        private readonly ICityRepository _cityRepository;
        private readonly IJobRepository _jobRepository;
        private readonly IResourceService _resourceService;
        private readonly IResearchService _researchService;
        private readonly ICityStatService _cityStatService;
        private readonly UnitDataReader _unitDataReader;
        private readonly BuildingDataReader _buildingDataReader;

        public RecruitmentService(
            ICityRepository cityRepository,
            IJobRepository jobRepository,
            IResourceService resourceService,
            IResearchService researchService,
            UnitDataReader unitDataReader,
            BuildingDataReader buildingDataReader,
            ICityStatService cityStatService)
        {
            _cityRepository = cityRepository;
            _jobRepository = jobRepository;
            _resourceService = resourceService;
            _researchService = researchService;
            _unitDataReader = unitDataReader;
            _buildingDataReader = buildingDataReader;
            _cityStatService = cityStatService;
        }

        public async Task<StableFullViewDTO> GetStableOverviewAsync(Guid userId, Guid cityId)
        {
            var cityEntity = await _cityRepository.GetByIdAsync(cityId);
            if (cityEntity == null) throw new Exception("City not found");

            var stableBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Stable);
            int currentBuildingLevel = stableBuilding?.Level ?? 0;

            var stableResponse = new StableFullViewDTO
            {
                BuildingLevel = currentBuildingLevel
            };

            // 1. Hent og filtrer Jobs til Køen (Kun Cavalry)
            var allActiveJobs = await _jobRepository.GetJobsByCityAsync(cityId);
            var recruitmentJobsForStable = allActiveJobs.OfType<RecruitmentJob>()
                .OrderBy(job => job.ExecutionTime)
                .ToList();

            foreach (var recruitmentJob in recruitmentJobsForStable)
            {
                if (recruitmentJob.UnitType == UnitTypeEnum.None) continue;

                var unitInformation = _unitDataReader.GetUnit(recruitmentJob.UnitType);

                // FILTER: Vis kun kavaleri i staldens kø
                if (unitInformation.Category != UnitCategoryEnum.Cavalry) continue;

                int remainingUnitsInJob = recruitmentJob.TotalQuantity - recruitmentJob.CompletedQuantity;
                double timeUntilNextUnitCalculatedInSeconds = Math.Max(0, (recruitmentJob.ExecutionTime - DateTime.UtcNow).TotalSeconds);
                double totalRemainingTimeInSeconds = timeUntilNextUnitCalculatedInSeconds + ((remainingUnitsInJob - 1) * recruitmentJob.SecondsPerUnit);

                stableResponse.RecruitmentQueue.Add(new RecruitmentQueueItemDTO
                {
                    QueueId = recruitmentJob.Id,
                    UnitType = recruitmentJob.UnitType,
                    Amount = remainingUnitsInJob,
                    TimeRemainingSeconds = totalRemainingTimeInSeconds,
                    TotalDurationSeconds = (int)(recruitmentJob.TotalQuantity * recruitmentJob.SecondsPerUnit)
                });
            }

            // 2. Hent tilgængelige enheder til rekruttering (Kun Cavalry)
            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Cavalry) continue;

                double calculatedRecruitmentTimePerUnit = await CalculateFinalTime(userId, cityEntity, unitStaticData);
                int currentUnitInventoryCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;
                bool isUnitTypeUnlocked = currentBuildingLevel > 0;

                stableResponse.AvailableUnits.Add(new StableUnitInfoDTO
                {
                    UnitType = unitTypeCandidate,
                    UnitName = unitStaticData.Type.ToString(),
                    CurrentInventoryCount = currentUnitInventoryCount,
                    CostWood = unitStaticData.WoodCost,
                    CostStone = unitStaticData.StoneCost,
                    CostMetal = unitStaticData.MetalCost,
                    RecruitmentTimeInSeconds = (int)calculatedRecruitmentTimePerUnit,
                    IsUnlocked = isUnitTypeUnlocked
                });
            }

            return stableResponse;
        }

        public async Task<WorkshopFullViewDTO> GetWorkshopOverviewAsync(Guid userId, Guid cityId)
        {
            var cityEntity = await _cityRepository.GetByIdAsync(cityId);
            if (cityEntity == null) throw new Exception("City not found");

            var workshopBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Workshop);
            int currentBuildingLevel = workshopBuilding?.Level ?? 0;

            var workshopResponse = new WorkshopFullViewDTO { BuildingLevel = currentBuildingLevel };

            // 1. Hent og filtrer Jobs til Køen (Kun Siege)
            var allActiveJobs = await _jobRepository.GetJobsByCityAsync(cityId);
            var recruitmentJobsForWorkshop = allActiveJobs.OfType<RecruitmentJob>()
                .OrderBy(job => job.ExecutionTime)
                .ToList();

            foreach (var recruitmentJob in recruitmentJobsForWorkshop)
            {
                if (recruitmentJob.UnitType == UnitTypeEnum.None) continue;

                var unitInformation = _unitDataReader.GetUnit(recruitmentJob.UnitType);

                // FILTER: Vis kun belejringsvåben i værkstedets kø
                if (unitInformation.Category != UnitCategoryEnum.Siege) continue;

                int remainingUnitsInJob = recruitmentJob.TotalQuantity - recruitmentJob.CompletedQuantity;
                double timeUntilNextUnitCalculatedInSeconds = Math.Max(0, (recruitmentJob.ExecutionTime - DateTime.UtcNow).TotalSeconds);
                double totalRemainingTimeInSeconds = timeUntilNextUnitCalculatedInSeconds + ((remainingUnitsInJob - 1) * recruitmentJob.SecondsPerUnit);

                workshopResponse.RecruitmentQueue.Add(new RecruitmentQueueItemDTO
                {
                    QueueId = recruitmentJob.Id,
                    UnitType = recruitmentJob.UnitType,
                    Amount = remainingUnitsInJob,
                    TimeRemainingSeconds = totalRemainingTimeInSeconds,
                    TotalDurationSeconds = (int)(recruitmentJob.TotalQuantity * recruitmentJob.SecondsPerUnit)
                });
            }

            // 2. Hent tilgængelige enheder (Siege)
            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Siege) continue;

                double calculatedRecruitmentTimePerUnit = await CalculateFinalTime(userId, cityEntity, unitStaticData);
                int currentUnitInventoryCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;

                workshopResponse.AvailableUnits.Add(new WorkshopUnitInfoDTO
                {
                    UnitType = unitTypeCandidate,
                    UnitName = unitStaticData.Type.ToString(),
                    CurrentInventoryCount = currentUnitInventoryCount,
                    CostWood = unitStaticData.WoodCost,
                    CostStone = unitStaticData.StoneCost,
                    CostMetal = unitStaticData.MetalCost,
                    RecruitmentTimeInSeconds = (int)calculatedRecruitmentTimePerUnit,
                    IsUnlocked = currentBuildingLevel > 0
                });
            }

            return workshopResponse;
        }

        public async Task<BarracksFullViewDTO> GetBarracksOverviewAsync(Guid userId, Guid cityId)
        {
            var cityEntity = await _cityRepository.GetByIdAsync(cityId);
            if (cityEntity == null) throw new Exception("City not found");

            var barracksBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Barracks);
            int currentBuildingLevel = barracksBuilding?.Level ?? 0;

            var barracksResponse = new BarracksFullViewDTO
            {
                BuildingLevel = currentBuildingLevel
            };

            // 1. Hent og filtrer Jobs til Køen (Kun Infantry)
            var allActiveJobs = await _jobRepository.GetRecruitmentJobsByCityAsync(cityId);

            foreach (var recruitmentJob in allActiveJobs)
            {
                if (recruitmentJob.UnitType == UnitTypeEnum.None) continue;

                var unitInformation = _unitDataReader.GetUnit(recruitmentJob.UnitType);

                // FILTER: Vis kun infanteri i kasernens kø
                if (unitInformation.Category != UnitCategoryEnum.Infantry) continue;

                int remainingUnitsInJob = recruitmentJob.TotalQuantity - recruitmentJob.CompletedQuantity;
                double timeUntilNextUnitCalculatedInSeconds = Math.Max(0, (recruitmentJob.ExecutionTime - DateTime.UtcNow).TotalSeconds);
                double totalRemainingTimeInSeconds = timeUntilNextUnitCalculatedInSeconds + ((remainingUnitsInJob - 1) * recruitmentJob.SecondsPerUnit);

                barracksResponse.RecruitmentQueue.Add(new RecruitmentQueueItemDTO
                {
                    QueueId = recruitmentJob.Id,
                    UnitType = recruitmentJob.UnitType,
                    Amount = remainingUnitsInJob,
                    TimeRemainingSeconds = totalRemainingTimeInSeconds,
                    TotalDurationSeconds = (int)(recruitmentJob.TotalQuantity * recruitmentJob.SecondsPerUnit)
                });
            }

            // 2. Hent tilgængelige enheder (Infantry)
            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Infantry) continue;

                double calculatedRecruitmentTimePerUnit = await CalculateFinalTime(userId, cityEntity, unitStaticData);
                int currentUnitInventoryCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;
                bool isUnitTypeUnlocked = currentBuildingLevel > 0;

                barracksResponse.AvailableUnits.Add(new BarracksUnitInfoDTO
                {
                    UnitType = unitTypeCandidate,
                    UnitName = unitStaticData.Type.ToString(),
                    CurrentInventoryCount = currentUnitInventoryCount,
                    CostWood = unitStaticData.WoodCost,
                    CostStone = unitStaticData.StoneCost,
                    CostMetal = unitStaticData.MetalCost,
                    RecruitmentTimeInSeconds = (int)calculatedRecruitmentTimePerUnit,
                    IsUnlocked = isUnitTypeUnlocked
                });
            }

            return barracksResponse;
        }

        public async Task<BuildingResult> QueueRecruitmentAsync(Guid userId, Guid cityId, UnitTypeEnum type, int quantity)
        {
            if (quantity <= 0) return new BuildingResult(false, "Antal skal være større end 0.");

            var cityEntity = await _cityRepository.GetByIdAsync(cityId);
            var unitStaticData = _unitDataReader.GetUnit(type);

            var activeJobsInCity = await _jobRepository.GetJobsByCityAsync(cityId);

            // --- POPULATION CHECK ---
            int availablePopulationCalculated = _cityStatService.GetAvailablePopulation(cityEntity, activeJobsInCity);
            int totalPopulationRequired = quantity * unitStaticData.PopulationCost;

            if (totalPopulationRequired > availablePopulationCalculated)
                return new BuildingResult(false, $"Ikke nok boliger. Kræver {totalPopulationRequired}, har {availablePopulationCalculated}.");

            // --- RESOURCES ---
            var resourceSnapshot = _resourceService.CalculateCurrent(cityEntity, DateTime.UtcNow);

            if (resourceSnapshot.Wood < (unitStaticData.WoodCost * quantity) ||
                resourceSnapshot.Stone < (unitStaticData.StoneCost * quantity) ||
                resourceSnapshot.Metal < (unitStaticData.MetalCost * quantity))
                return new BuildingResult(false, "Ikke nok ressourcer.");

            // --- EXECUTION TIME ---
            double calculatedSecondsPerUnit = await CalculateFinalTime(userId, cityEntity, unitStaticData);

            cityEntity.Wood = resourceSnapshot.Wood - (unitStaticData.WoodCost * quantity);
            cityEntity.Stone = resourceSnapshot.Stone - (unitStaticData.StoneCost * quantity);
            cityEntity.Metal = resourceSnapshot.Metal - (unitStaticData.MetalCost * quantity);
            cityEntity.LastResourceUpdate = DateTime.UtcNow;

            await _cityRepository.UpdateAsync(cityEntity);

            await _jobRepository.AddAsync(new RecruitmentJob
            {
                UserId = userId,
                CityId = cityId,
                UnitType = type,
                TotalQuantity = quantity,
                SecondsPerUnit = calculatedSecondsPerUnit,
                LastTickTime = DateTime.UtcNow,
                ExecutionTime = DateTime.UtcNow.AddSeconds(calculatedSecondsPerUnit),
                CompletedQuantity = 0
            });

            return new BuildingResult(true, $"Træning af {quantity}x {type} startet.");
        }

        private async Task<double> CalculateFinalTime(Guid userId, City city, UnitData unit)
        {
            var modifierTags = new List<ModifierTagEnum>(unit.ModifiersThatAffects);
            if (!modifierTags.Contains(ModifierTagEnum.Recruitment)) modifierTags.Add(ModifierTagEnum.Recruitment);

            var applicableModifiers = new List<Modifier>();

            BuildingTypeEnum productionBuildingType = unit.Category switch
            {
                UnitCategoryEnum.Infantry => BuildingTypeEnum.Barracks,
                UnitCategoryEnum.Cavalry => BuildingTypeEnum.Stable,
                UnitCategoryEnum.Siege => BuildingTypeEnum.Workshop,
                _ => BuildingTypeEnum.Barracks
            };

            var buildingEntity = city.Buildings.FirstOrDefault(b => b.Type == productionBuildingType);
            if (buildingEntity != null && buildingEntity.Level > 0)
            {
                var buildingLevelConfiguration = _buildingDataReader.GetConfig<BuildingLevelData>(productionBuildingType, buildingEntity.Level);
                if (buildingLevelConfiguration != null) applicableModifiers.AddRange(buildingLevelConfiguration.ModifiersInternal);
            }

            var userResearchModifiers = await _researchService.GetUserResearchModifiersAsync(userId);
            applicableModifiers.AddRange(userResearchModifiers);

            double finalRecruitmentSpeedMultiplier = StatCalculator.ApplyModifiers(1.0, modifierTags, applicableModifiers);
            double calculatedFinalRecruitmentTime = unit.RecruitmentTimeInSeconds / Math.Max(0.1, finalRecruitmentSpeedMultiplier);

            return Math.Max(calculatedFinalRecruitmentTime, 1.0);
        }
    }
}