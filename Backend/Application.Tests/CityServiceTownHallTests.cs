using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class CityServiceTownHallTests
{
    [Fact]
    public async Task GetDetailedCityInformation_MapsProductionAndPopulationBreakdown()
    {
        var player = new WorldPlayer { Id = Guid.NewGuid(), Cities = new List<City>() };
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Capital",
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            Buildings = new List<Building> { new() { Type = BuildingTypeEnum.Housing, Level = 1 } },
            UnitStacks = new List<UnitStack> { new() { Type = UnitTypeEnum.Militia, Quantity = 10 } },
            OriginUnitDeployments = new List<UnitDeployment>()
        };
        player.Cities.Add(city);
        var recruitmentJob = new RecruitmentJob
        {
            CityId = city.Id,
            UnitType = UnitTypeEnum.Militia,
            TotalQuantity = 50,
            CompletedQuantity = 5
        };
        var populationModifiers = new PopulationBonusModifierService();
        var cityStats = new CityStatService(TestData.BuildingReader(), TestData.UnitReader(), populationModifiers);
        var service = new CityService(
            new MemoryCityRepository(city),
            new NoOpResourceService(),
            new NoOpWorldPlayerService(),
            new TestPlayerAccessService(cities: [city]),
            populationModifiers,
            cityStats,
            new NoOpExoticResourceService(),
            TestData.BuildingReader(),
            new RecruitmentJobRepository(recruitmentJob),
            NullLogger<CityService>.Instance,
            new ConstructionTimeCalculator(populationModifiers),
            new NoOpResistanceService());

        var result = await service.GetDetailedCityInformationByCityIdentifierAsync(city.Id);

        Assert.NotNull(result);
        int housing = TestData.BuildingReader().GetConfig<Domain.StaticData.Data.HousingLevelData>(BuildingTypeEnum.Housing, 1).Population;
        int militiaPopulation = TestData.UnitReader().GetUnit(UnitTypeEnum.Militia).PopulationCost;
        int expectedUsage = (10 + 45) * militiaPopulation;
        Assert.Equal(11, result.CoinsProductionPerHour);
        Assert.Equal(22, result.ResearchPointsPerHour);
        Assert.Equal(33, result.IdeologyFocusPointsPerHour);
        Assert.Equal(housing, result.Population.HousingCapacity);
        Assert.Equal(25, result.Population.ModifierBonus);
        Assert.Equal(housing + 25, result.Population.TotalCapacity);
        Assert.Equal(expectedUsage, result.Population.InUse);
        Assert.Equal(0, result.Population.Remaining);
        Assert.Equal(result.Population.InUse, result.CurrentPopulationUsage);
        Assert.Equal(result.Population.TotalCapacity, result.MaxPopulationCapacity);
    }

    [Fact]
    public async Task GetAvailableBuildingsForTownHallAsync_IncludesMissingBuildingsAsLevelOneOptions()
    {
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            Cities = new List<City>()
        };

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Capital",
            WorldId = player.WorldId,
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Wood = 10_000,
            Stone = 10_000,
            Metal = 10_000,
            Buildings = new List<Building>
            {
                new() { Type = BuildingTypeEnum.TownHall, Level = 20, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.Warehouse, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.Housing, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.TimberCamp, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.StoneQuarry, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.MetalMine, Level = 1, CityId = Guid.NewGuid() }
            }
        };
        player.Cities.Add(city);

        var service = new CityService(
            new MemoryCityRepository(city),
            new NoOpResourceService(),
            new NoOpWorldPlayerService(),
            new TestPlayerAccessService(cities: [city]),
            new NoOpModifierService(),
            new FixedCityStatService(),
            new NoOpExoticResourceService(),
            TestData.BuildingReader(),
            new EmptyJobRepository(),
            NullLogger<CityService>.Instance,
            new ConstructionTimeCalculator(new NoOpModifierService()),
            new NoOpResistanceService());

        var result = await service.GetAvailableBuildingsForTownHallAsync(city.Id);

        Assert.Equal(Enum.GetValues<BuildingTypeEnum>().Length, result.Count);
        Assert.Contains(result, item => item.BuildingType == BuildingTypeEnum.TownHall && item.IsConstructed && item.CurrentLevel == 20);
        Assert.Contains(result, item => item.BuildingType == BuildingTypeEnum.Barracks && !item.IsConstructed && item.CurrentLevel == null);
        Assert.All(result, item => Assert.Equal(20, item.MaximumLevel));
        Assert.All(result, item => Assert.True(item.WoodCost >= 0 && item.StoneCost >= 0 && item.MetalCost >= 0));
    }

    private sealed class NoOpWorldPlayerService : IWorldPlayerService
    {
        public Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid worldId) => throw new NotSupportedException();
        public Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId) => throw new NotSupportedException();
        public Task<WorldPlayerProfileDTO> UpdateWorldPlayerDescriptionAsync(Guid worldPlayerId, string description) => throw new NotSupportedException();
        public Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId) => throw new NotSupportedException();
        public Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query) => throw new NotSupportedException();
        public void SyncGlobalResources(WorldPlayer player, DateTime currentDateTime) { }
        public Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request) => throw new NotSupportedException();
    }

    private sealed class NoOpResourceService : IResourceService
    {
        public CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime) =>
            new(cityEntity.Wood, cityEntity.Stone, cityEntity.Metal, 0, 0, 0, currentDateTime);

        public CityProductionSnapshot CalculateCityProduction(WorldPlayer playerEntity, City cityEntity) => new(11, 22, 33);

        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime) =>
            new(0, 0, 0, 0, 0, 0, currentDateTime);
    }

    private sealed class NoOpModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(double baseValue, IEnumerable<ModifierTagEnum> targetTags, IEnumerable<Domain.Abstraction.IModifierProvider> providers) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityUnitValue(City city, Domain.StaticData.Data.UnitData unit, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
    }

    private sealed class NoOpExoticResourceService : IExoticResourceService
    {
        public Task<List<CityExoticResourceDTO>> SyncCityExoticResourcesAsync(City city, DateTime currentDateTime) => Task.FromResult(new List<CityExoticResourceDTO>());
        public Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesAsync(Guid islandId) => throw new NotSupportedException();
        public Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesForCityAsync(City city) => Task.FromResult(new List<WorldIslandExoticResourceDTO>());
        public Task<List<CityExoticResourceProductionDTO>> GetProductionBreakdownsForCityAsync(City city) => Task.FromResult(new List<CityExoticResourceProductionDTO>());
        public Task<ExoticResourceInvestmentResponseDTO> InvestAsync(Guid cityId, ExoticResourceInvestmentRequestDTO request) => throw new NotSupportedException();
    }

    private sealed class NoOpResistanceService : IResistanceService
    {
        public double CalculateRecoveryPerHour(City city) => 0;
        public void UpdateResistance(City city, DateTime now) { }
    }

    private sealed class RecruitmentJobRepository(RecruitmentJob job) : IJobRepository
    {
        public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) => Task.FromResult(cityId == job.CityId ? new List<RecruitmentJob> { job } : new());
        public Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId) => Task.FromResult(new List<BuildingJob>());
        public Task<BaseJob?> GetByIdAsync(Guid id) => throw new NotSupportedException();
        public Task<List<BaseJob>> GetDueJobsAsync(DateTime now, int batchSize) => throw new NotSupportedException();
        public Task AddAsync(BaseJob jobToAdd) => throw new NotSupportedException();
        public Task UpdateAsync(BaseJob jobToUpdate) => throw new NotSupportedException();
        public Task DeleteAsync(Guid jobId) => throw new NotSupportedException();
        public Task<ResearchJob?> GetResearchJobAsync(Guid userId) => throw new NotSupportedException();
        public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) => throw new NotSupportedException();
    }

    private sealed class PopulationBonusModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(double baseValue, IEnumerable<ModifierTagEnum> targetTags, IEnumerable<Domain.Abstraction.IModifierProvider> providers) =>
            new() { BaseValue = baseValue, FlatBonus = 25, FinalValue = baseValue + 25 };

        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityUnitValue(City city, Domain.StaticData.Data.UnitData unit, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
    }
}
