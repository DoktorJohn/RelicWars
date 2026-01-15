using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers;

namespace Application.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly ICityRepository _cityRepo;
        private readonly IJobRepository _jobRepo;
        private readonly IResourceService _resService;
        private readonly IResearchService _researchService;
        private readonly ICityStatService _statService;
        private readonly BuildingDataReader _dataReader;

        public BuildingService(
            ICityRepository cityRepo,
            IJobRepository jobRepo,
            IResourceService resService,
            IResearchService researchService,
            BuildingDataReader dataReader,
            ICityStatService statService)
        {
            _cityRepo = cityRepo;
            _jobRepo = jobRepo;
            _resService = resService;
            _researchService = researchService;
            _dataReader = dataReader;
            _statService = statService;
        }

        public async Task<List<WarehouseProjectionDTO>> GetWarehouseProjectionAsync(Guid cityId)
        {
            // 1. Hent byen for at finde nuværende warehouse level
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var warehouse = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Warehouse);
            int currentLevel = warehouse?.Level ?? 0;

            var resultList = new List<WarehouseProjectionDTO>();

            // 2. Loop: Nuværende level + 5 næste
            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;


                int capacity = 0;

                if (levelToCheck == 0)
                {
                    capacity = 500;
                }
                else
                {
                    var config = _dataReader.GetConfig<WarehouseLevelData>(BuildingTypeEnum.Warehouse, levelToCheck);

                    if (config == null) break;

                    capacity = config.Capacity;
                }

                resultList.Add(new WarehouseProjectionDTO
                {
                    Level = levelToCheck,
                    Capacity = capacity,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }

        public async Task<List<ResourceBuildingInfoDTO>> GetResourceBuildingInfoAsync(Guid cityId, BuildingTypeEnum resourceBuildingType)
        {
            var targetCity = await _cityRepo.GetByIdAsync(cityId);
            if (targetCity == null) throw new Exception($"City with ID {cityId} not found");

            var existingBuilding = targetCity.Buildings.FirstOrDefault(b => b.Type == resourceBuildingType);
            int currentBuildingLevel = existingBuilding?.Level ?? 0;

            Func<int, int> getProductionStrategy = resourceBuildingType switch
            {
                BuildingTypeEnum.TimberCamp => (level) =>
                    _dataReader.GetConfig<TimberCampLevelData>(BuildingTypeEnum.TimberCamp, level)?.ProductionPerHour ?? 0,

                BuildingTypeEnum.StoneQuarry => (level) =>
                    _dataReader.GetConfig<StoneQuarryLevelData>(BuildingTypeEnum.StoneQuarry, level)?.ProductionPerHour ?? 0,

                BuildingTypeEnum.MetalMine => (level) =>
                    _dataReader.GetConfig<MetalMineLevelData>(BuildingTypeEnum.MetalMine, level)?.ProductionPerHour ?? 0,

                _ => (level) => 0
            };

            var buildingProjectionList = new List<ResourceBuildingInfoDTO>();

            int levelsToProject = 5;

            for (int i = 0; i <= levelsToProject; i++)
            {
                int levelToCheck = currentBuildingLevel + i;
                int calculatedProduction = 0;

                if (levelToCheck > 0)
                {
                    calculatedProduction = getProductionStrategy(levelToCheck);
                }

                buildingProjectionList.Add(new ResourceBuildingInfoDTO
                {
                    Level = levelToCheck,
                    ProductionPrHour = calculatedProduction,
                    IsCurrentLevel = (levelToCheck == currentBuildingLevel)
                });
            }

            return buildingProjectionList;
        }

        public async Task<BuildingResult> QueueUpgradeAsync(Guid cityId, BuildingTypeEnum type)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null || !city.WorldPlayerId.HasValue)
                return new BuildingResult(false, "Byen eller ejeren blev ikke fundet.");

            var activeJobs = await _jobRepo.GetJobsByCityAsync(cityId);
            var buildingJobs = activeJobs.OfType<BuildingJob>().ToList();

            if (buildingJobs.Count >= 5)
                return new BuildingResult(false, "Byggekøen er fuld.");

            var currentBuilding = city.Buildings.FirstOrDefault(b => b.Type == type);
            int currentLevel = currentBuilding?.Level ?? 0;
            int nextLevel = currentLevel + buildingJobs.Count(j => j.BuildingType == type) + 1;

            if (nextLevel > 30) return new BuildingResult(false, "Maksimum niveau nået.");

            var config = _dataReader.GetConfig<BuildingLevelData>(type, nextLevel);

            // --- POPULATION CHECK ---
            int availablePop = _statService.GetAvailablePopulation(city, activeJobs);
            int currentPopCost = currentLevel > 0
                ? _dataReader.GetConfig<BuildingLevelData>(type, currentLevel).PopulationCost
                : 0;
            int additionalNeeded = config.PopulationCost - currentPopCost;

            if (additionalNeeded > availablePop)
                return new BuildingResult(false, $"Mangler population: {additionalNeeded - availablePop} flere frie borgere påkrævet.");

            // --- PREREQUISITES & RESOURCES ---
            foreach (var req in config.Prerequisites)
            {
                var baseLevel = city.Buildings.FirstOrDefault(b => b.Type == req.Type)?.Level ?? 0;
                if ((baseLevel + buildingJobs.Count(j => j.BuildingType == req.Type)) < req.RequiredLevel)
                    return new BuildingResult(false, $"Mangler krav: {req.Type} lvl {req.RequiredLevel}.");
            }

            var accountModifiers = await _researchService.GetUserResearchModifiersAsync(city.WorldPlayerId.Value);
            var snapshot = _resService.CalculateCurrent(city, DateTime.UtcNow, accountModifiers);

            if (snapshot.Wood < config.WoodCost || snapshot.Stone < config.StoneCost || snapshot.Metal < config.MetalCost)
                return new BuildingResult(false, "Ikke nok ressourcer.");

            // --- EXECUTION ---
            DateTime startTime = buildingJobs.Any() ? buildingJobs.Last().ExecutionTime : DateTime.UtcNow;

            city.Wood = snapshot.Wood - config.WoodCost;
            city.Stone = snapshot.Stone - config.StoneCost;
            city.Metal = snapshot.Metal - config.MetalCost;
            city.LastResourceUpdate = DateTime.UtcNow;

            await _cityRepo.UpdateAsync(city);
            await _jobRepo.AddAsync(new BuildingJob
            {
                UserId = city.WorldPlayerId.Value,
                CityId = cityId,
                BuildingType = type,
                TargetLevel = nextLevel,
                ExecutionTime = startTime.Add(config.BuildTime)
            });

            return new BuildingResult(true, $"{type} lvl {nextLevel} i kø.");
        }

        public async Task<List<HousingInfoDTO>> GetHousingInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var housing = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Housing);
            int currentLevel = housing?.Level ?? 0;

            var resultList = new List<HousingInfoDTO>();

            // 2. Loop: Nuværende level + 5 næste
            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;

                int population = 0;

                if (levelToCheck == 0)
                {
                    population = 100;
                }
                else
                {
                    var config = _dataReader.GetConfig<HousingLevelData>(BuildingTypeEnum.Housing, levelToCheck);

                    if (config == null) break;

                    population = config.Population;
                }

                resultList.Add(new HousingInfoDTO
                {
                    Level = levelToCheck,
                    Population = population,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }

        public async Task<List<WallInfoDTO>> GetWallInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var wall = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Wall);
            int currentLevel = wall?.Level ?? 0;

            var resultList = new List<WallInfoDTO>();

            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;

                ModifierDTO modifier = new();

                if (levelToCheck == 0)
                {
                    modifier.ModifierTag = ModifierTagEnum.Wall;
                    modifier.ModifierType = ModifierTypeEnum.Increased;
                    modifier.Value = 0;
                }

                else
                {
                    var config = _dataReader.GetConfig<WallLevelData>(BuildingTypeEnum.Wall, levelToCheck);

                    if (config == null) break;

                    modifier.ModifierTag = ModifierTagEnum.Wall;
                    modifier.ModifierType = ModifierTypeEnum.Increased;
                    modifier.Value = config.ModifiersInternal.FirstOrDefault(x => x.Tag == ModifierTagEnum.Wall)?.Value ?? 0;
                }

                resultList.Add(new WallInfoDTO
                {
                    Level = levelToCheck,
                    DefensiveModifier = modifier,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }
        public async Task<List<StableInfoDTO>> GetStableInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var building = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Stable);
            int currentLevel = building?.Level ?? 0;

            var result = new List<StableInfoDTO>();

            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;
                if (levelToCheck == 0) levelToCheck = 1;

                var config = _dataReader.GetConfig<StableLevelData>(BuildingTypeEnum.Stable, levelToCheck);
                if (config == null && levelToCheck > 1) break;

                result.Add(new StableInfoDTO
                {
                    Level = levelToCheck,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return result;
        }

        public async Task<List<AcademyInfoDTO>> GetAcademyInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var building = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Academy);
            int currentLevel = building?.Level ?? 0;

            var result = new List<AcademyInfoDTO>();

            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;
                if (levelToCheck == 0) levelToCheck = 1;

                var config = _dataReader.GetConfig<AcademyLevelData>(BuildingTypeEnum.Academy, levelToCheck);
                if (config == null && levelToCheck > 1) break;

                result.Add(new AcademyInfoDTO
                {
                    Level = levelToCheck,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return result;
        }

        public async Task<List<WorkshopInfoDTO>> GetWorkshopInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var building = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Workshop);
            int currentLevel = building?.Level ?? 0;

            var result = new List<WorkshopInfoDTO>();

            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;
                if (levelToCheck == 0) levelToCheck = 1;

                var config = _dataReader.GetConfig<WorkshopLevelData>(BuildingTypeEnum.Workshop, levelToCheck);
                if (config == null && levelToCheck > 1) break;

                result.Add(new WorkshopInfoDTO
                {
                    Level = levelToCheck,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return result;
        }
    }
}