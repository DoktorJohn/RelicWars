using Application.Interfaces.IRepositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Application.Interfaces.IServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;
    public FixedTimeProvider(DateTimeOffset now) => _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
}

internal sealed class FixedRandomService : Application.Interfaces.IServices.IRandomService
{
    private readonly double _value;
    public FixedRandomService(double value = 0.5) => _value = value;
    public int Next(int maxValue) => 0;
    public double NextDouble() => _value;
}

internal static class TestData
{
    public static readonly DateTime Now = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);
    public static string GameFile(string name) => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Game", name));

    public static IdeologyFocusDataReader FocusReader()
    {
        var reader = new IdeologyFocusDataReader();
        reader.Load(GameFile("ideologyFocus.json"));
        return reader;
    }

    public static UnitDataReader UnitReader()
    {
        var reader = new UnitDataReader();
        reader.Load(GameFile("units.json"));
        return reader;
    }

    public static BuildingDataReader BuildingReader()
    {
        var reader = new BuildingDataReader(); reader.Load(GameFile("buildings.json")); return reader;
    }

    public static ResearchDataReader ResearchReader()
    {
        var reader = new ResearchDataReader(); reader.Load(GameFile("research.json")); return reader;
    }

    public static IdeologyDataReader IdeologyReader()
    {
        var reader = new IdeologyDataReader(); reader.Load(GameFile("ideologies.json")); return reader;
    }

    public static ModifierService ModifierService(out ModifierCollectorService collector)
    {
        var buildings = new BuildingDataReader(); buildings.Load(GameFile("buildings.json"));
        var research = new ResearchDataReader(); research.Load(GameFile("research.json"));
        var ideology = new IdeologyDataReader(); ideology.Load(GameFile("ideologies.json"));
        collector = new ModifierCollectorService(buildings, research, ideology, FocusReader(), new FixedTimeProvider(Now));
        return new ModifierService(NullLogger<ModifierService>.Instance, collector);
    }

    public static City CityWithFocus(IdeologyFocusNameEnum focusName)
    {
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            CompletedResearches = new(),
            Cities = new(),
            Ideology = IdeologyTypeEnum.Feudalism
        };
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Test City",
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            Buildings = new(),
            UnitStacks = new(),
            ActiveFocuses = new()
            {
                new IdeologyFocus
                {
                    Name = focusName,
                    TimeOfIdeologyStarted = Now.AddMinutes(-1),
                    TimeOfIdeologyFinished = Now.AddHours(1)
                }
            }
        };
        player.Cities.Add(city);
        return city;
    }
}

internal sealed class MemoryCityRepository : ICityRepository
{
    private readonly City _city;
    public MemoryCityRepository(City city) => _city = city;
    public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult<City?>(cityId == _city.Id ? _city : null);
    public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
    public Task UpdateAsync(City city) => Task.CompletedTask;
    public Task AddAsync(City city) => Task.CompletedTask;
    public Task<List<City>> GetAllAsync() => Task.FromResult(new List<City> { _city });
    public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) => Task.FromResult(ids.Contains(_city.Id) ? new List<City> { _city } : new());
    public Task UpdateRangeAsync(List<City> cities) => Task.CompletedTask;
    public Task<City?> GetByCoordinatesAsync(int x, int y) => Task.FromResult<City?>(_city.X == x && _city.Y == y ? _city : null);
    public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) => Task.FromResult(cityId == _city.Id ? _city.WorldPlayerId : null);
    public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) => Task.FromResult(_city.WorldPlayerId == worldPlayerId ? new List<City> { _city } : new());
    public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
}

internal sealed class FixedCityStatService : ICityStatService
{
    public int Available { get; set; }
    public double GetWarehouseCapacity(City city) => 1000;
    public int GetMaxPopulation(City city) => Available;
    public int GetCurrentPopulationUsage(City city, IEnumerable<BaseJob> activeJobs) => 0;
    public int GetAvailablePopulation(City city, IEnumerable<BaseJob> activeJobs) => Available - city.UnitStacks.Sum(x => TestData.UnitReader().GetUnit(x.Type).PopulationCost * x.Quantity);
}

internal sealed class EmptyJobRepository : IJobRepository
{
    public Task<BaseJob?> GetByIdAsync(Guid id) => Task.FromResult<BaseJob?>(null);
    public Task<List<BaseJob>> GetDueJobsAsync(DateTime now, int batchSize) => Task.FromResult(new List<BaseJob>());
    public Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId) => Task.FromResult(new List<BuildingJob>());
    public Task AddAsync(BaseJob job) => Task.CompletedTask;
    public Task UpdateAsync(BaseJob job) => Task.CompletedTask;
    public Task DeleteAsync(Guid jobId) => Task.CompletedTask;
    public Task<ResearchJob?> GetResearchJobAsync(Guid userId) => Task.FromResult<ResearchJob?>(null);
    public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) => Task.FromResult(new List<RecruitmentJob>());
    public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) => Task.FromResult(new List<ResearchJob>());
}
