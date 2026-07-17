using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Infrastructure.Workers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class UnitDeploymentWorkerTests
{
    [Fact]
    public async Task ProcessMilitaryMovementsAsync_UsesSameTravelTimeForReturnAndCompletesHomecoming()
    {
        var setup = CreateSetup();
        var expectedTravel = setup.MovementCalculator.CalculateTravelSeconds(0, 0, 2, 0, 2);
        setup.AttackerDeployment.DepartureTime = DateTime.UtcNow.AddSeconds(-(expectedTravel + 1));
        setup.AttackerDeployment.ArrivalTime = DateTime.UtcNow.AddSeconds(-1);

        await setup.Worker.ProcessMilitaryMovementsAsync();

        Assert.Equal(setup.TargetCity!.Id, setup.AttackerDeployment.TargetCityId);
        Assert.Equal(UnitDeploymentPhaseEnum.Returning, setup.AttackerDeployment.Phase);
        Assert.Equal(UnitDeploymentMovementStatusEnum.Moving, setup.AttackerDeployment.UnitDeploymentMovementStatus);
        Assert.InRange((setup.AttackerDeployment.ArrivalTime - DateTime.UtcNow).TotalSeconds, expectedTravel - 5, expectedTravel + 5);

        setup.AttackerDeployment.ArrivalTime = DateTime.UtcNow.AddSeconds(-1);

        await setup.Worker.ProcessMilitaryMovementsAsync();

        Assert.Empty(setup.DeploymentRepository.Deployments);
        Assert.Equal(10, setup.OriginCity.UnitStacks.Single(stack => stack.Type == UnitTypeEnum.Militia).Quantity);
    }

    [Fact]
    public async Task ProcessMilitaryMovementsAsync_RemovesDestroyedDeployment()
    {
        var setup = CreateSetup(targetStackQuantity: 100);
        setup.AttackerDeployment.DepartureTime = DateTime.UtcNow.AddHours(-2);
        setup.AttackerDeployment.ArrivalTime = DateTime.UtcNow.AddSeconds(-1);

        await setup.Worker.ProcessMilitaryMovementsAsync();

        Assert.Empty(setup.DeploymentRepository.Deployments);
        Assert.Equal(2, setup.BattleReports.AddedReports.Count);
        Assert.Contains(setup.BattleReports.AddedReports, report => report.ReportType == ReportTypeEnum.Attack);
        Assert.Contains(setup.BattleReports.AddedReports, report => report.ReportType == ReportTypeEnum.CityAttacked);
        Assert.Contains(setup.BattleReports.AddedReports, report => report.WorldPlayerId == setup.AttackerDeployment.WorldPlayerId);
        Assert.Contains(setup.BattleReports.AddedReports, report => report.WorldPlayerId == setup.TargetCity!.WorldPlayerId);
    }

    [Fact]
    public async Task ProcessMilitaryMovementsAsync_CreatesSupportHomecomingReport()
    {
        var setup = CreateSetup();
        setup.AttackerDeployment.Type = UnitDeploymentTypeEnum.Support;
        setup.AttackerDeployment.Phase = UnitDeploymentPhaseEnum.Returning;
        setup.AttackerDeployment.ArrivalTime = DateTime.UtcNow.AddSeconds(-1);

        await setup.Worker.ProcessMilitaryMovementsAsync();

        var report = Assert.Single(setup.BattleReports.AddedReports);
        Assert.Equal(ReportTypeEnum.SupportingUnitsReturned, report.ReportType);
        Assert.Equal(setup.AttackerDeployment.WorldPlayerId, report.WorldPlayerId);
    }

    [Fact]
    public async Task ProcessMilitaryMovementsAsync_ReturnsHomeWhenTargetCityIsMissing()
    {
        var setup = CreateSetup(includeTargetCity: false);
        setup.AttackerDeployment.DepartureTime = DateTime.UtcNow.AddHours(-2);
        setup.AttackerDeployment.ArrivalTime = DateTime.UtcNow.AddSeconds(-1);

        await setup.Worker.ProcessMilitaryMovementsAsync();

        Assert.Single(setup.DeploymentRepository.Deployments);
        Assert.Null(setup.AttackerDeployment.TargetCityId);
        Assert.Equal(UnitDeploymentPhaseEnum.Returning, setup.AttackerDeployment.Phase);
        Assert.Equal(UnitDeploymentMovementStatusEnum.Moving, setup.AttackerDeployment.UnitDeploymentMovementStatus);
    }

    [Fact]
    public async Task ProcessMilitaryMovementsAsync_DoesNotTreatUnknownTypeAsAttack()
    {
        var setup = CreateSetup(targetStackQuantity: 10);
        setup.AttackerDeployment.Type = UnitDeploymentTypeEnum.Support;
        setup.AttackerDeployment.ArrivalTime = DateTime.UtcNow.AddSeconds(-1);
        var originalDefenders = setup.TargetCity!.UnitStacks.Single().Quantity;

        await setup.Worker.ProcessMilitaryMovementsAsync();

        Assert.Equal(originalDefenders, setup.TargetCity.UnitStacks.Single().Quantity);
        Assert.Empty(setup.BattleReports.AddedReports);
        Assert.Single(setup.DeploymentRepository.Deployments);
    }

    [Fact]
    public async Task ProcessMilitaryMovementsAsync_ConsumesNPCDefendersWithoutCreatingOwnerReport()
    {
        var setup = CreateSetup(targetStackQuantity: 10);
        setup.TargetCity!.IsNPC = true;
        setup.TargetCity.WorldPlayerId = null;
        setup.TargetCity.WorldPlayer = null;
        int defendersBefore = setup.TargetCity.UnitStacks.Single().Quantity;
        setup.AttackerDeployment.ArrivalTime = DateTime.UtcNow.AddSeconds(-1);

        await setup.Worker.ProcessMilitaryMovementsAsync();

        Assert.True(setup.TargetCity.UnitStacks.Single().Quantity < defendersBefore);
        Assert.Contains(setup.BattleReports.AddedReports, report => report.ReportType == ReportTypeEnum.Attack);
        Assert.DoesNotContain(setup.BattleReports.AddedReports, report => report.ReportType == ReportTypeEnum.CityAttacked);
    }

    [Fact]
    public async Task ProcessMilitaryMovementsAsync_StationsSameWorldSupportAtNPCVillage()
    {
        var setup = CreateSetup();
        setup.TargetCity!.IsNPC = true;
        setup.TargetCity.WorldPlayerId = null;
        setup.TargetCity.WorldPlayer = null;
        setup.AttackerDeployment.Type = UnitDeploymentTypeEnum.Support;
        setup.AttackerDeployment.ArrivalTime = DateTime.UtcNow.AddSeconds(-1);

        await setup.Worker.ProcessMilitaryMovementsAsync();

        Assert.Equal(UnitDeploymentPhaseEnum.Stationed, setup.AttackerDeployment.Phase);
        Assert.Equal(UnitDeploymentMovementStatusEnum.Stationed, setup.AttackerDeployment.UnitDeploymentMovementStatus);
    }

    private static Setup CreateSetup(int targetStackQuantity = 0, bool includeTargetCity = true)
    {
        var attackerPlayer = Player("Attacker");
        var defenderPlayer = Player("Defender");
        defenderPlayer.WorldId = attackerPlayer.WorldId;

        var originCity = City(attackerPlayer, "Home", 0, 0, 7);
        var targetCity = includeTargetCity ? City(defenderPlayer, "Target", 2, 0, targetStackQuantity) : null;

        var attackerDeployment = new UnitDeployment
        {
            Id = Guid.NewGuid(),
            Name = "Attacker",
            Mobility = 2,
            Type = UnitDeploymentTypeEnum.Attack,
            UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving,
            Phase = UnitDeploymentPhaseEnum.Outbound,
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            DepartureTime = DateTime.UtcNow.AddHours(-1),
            OriginCity = originCity,
            OriginCityId = originCity.Id,
            OwnerWorldPlayer = attackerPlayer,
            WorldPlayerId = attackerPlayer.Id,
            WorldId = attackerPlayer.WorldId,
            UnitStacks =
            [
                new() { Id = Guid.NewGuid(), Type = UnitTypeEnum.Militia, Quantity = 3, WorldPlayerId = attackerPlayer.Id }
            ],
            TargetCity = targetCity,
            TargetCityId = targetCity?.Id,
            LegStartX = originCity.X,
            LegStartY = originCity.Y,
            LegEndX = targetCity?.X ?? originCity.X,
            LegEndY = targetCity?.Y ?? originCity.Y
        };

        var deploymentRepo = new TrackingUnitDeploymentRepository(attackerDeployment);
        var battleReports = new TrackingBattleReportRepository();
        var movementCalculator = new UnitMovementCalculator(new NoOpModifierService());

        var worker = new UnitDeploymentWorker(
            deploymentRepo,
            new NoOpCityRepository(originCity, targetCity),
            new CombatService(TestData.UnitReader(), new NoOpModifierService(), new FixedRandomService(0.5)),
            battleReports,
            new NoOpCityStatService(),
            movementCalculator,
            NullLogger<UnitDeploymentWorker>.Instance,
            new NoOpIdeologyFocusRepository(),
            new ImmediateTransactionManager(),
            new DeploymentPermissionService(new TestAllianceRepository()));

        return new Setup(worker, deploymentRepo, battleReports, movementCalculator, originCity, targetCity, attackerDeployment);
    }

    private static WorldPlayer Player(string name) => new()
    {
        Id = Guid.NewGuid(),
        WorldId = Guid.NewGuid(),
        PlayerProfileId = Guid.NewGuid(),
        PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = name }
    };

    private static City City(WorldPlayer owner, string name, int x, int y, int militiaCount) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        WorldId = owner.WorldId,
        WorldPlayerId = owner.Id,
        WorldPlayer = owner,
        X = x,
        Y = y,
        UnitStacks = militiaCount > 0
            ? [new() { Id = Guid.NewGuid(), Type = UnitTypeEnum.Militia, Quantity = militiaCount, WorldPlayerId = owner.Id, CityId = Guid.NewGuid() }]
            : []
    };

    private sealed record Setup(
        UnitDeploymentWorker Worker,
        TrackingUnitDeploymentRepository DeploymentRepository,
        TrackingBattleReportRepository BattleReports,
        UnitMovementCalculator MovementCalculator,
        City OriginCity,
        City? TargetCity,
        UnitDeployment AttackerDeployment);

    private sealed class TrackingUnitDeploymentRepository(UnitDeployment deployment) : IUnitDeploymentRepository
    {
        private readonly List<UnitDeployment> _deployments = [deployment];

        public List<UnitDeployment> Deployments => _deployments;

        public Task<List<UnitDeployment>> GetUnitDeploymentsWithStacksByListOfIdsAsync(List<Guid> ids) =>
            Task.FromResult(_deployments.Where(deployment => ids.Contains(deployment.Id)).ToList());

        public Task<List<UnitDeployment>> GetActiveDeploymentsByWorldPlayerIdAsync(Guid worldPlayerId) =>
            Task.FromResult(_deployments.Where(deployment => deployment.WorldPlayerId == worldPlayerId && deployment.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Moving).ToList());

        public Task AddAsync(UnitDeployment deployment)
        {
            _deployments.Add(deployment);
            return Task.CompletedTask;
        }

        public Task<List<UnitDeployment>> GetDueMovementsAsync(DateTime now, int batchSize) =>
            Task.FromResult(_deployments.Where(deployment => deployment.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Moving && deployment.ArrivalTime <= now).Take(batchSize).ToList());

        public Task UpdateAsync(UnitDeployment deployment) => Task.CompletedTask;

        public Task DeleteAsync(UnitDeployment deployment)
        {
            _deployments.Remove(deployment);
            return Task.CompletedTask;
        }

        public Task<UnitDeployment?> GetByIdAsync(Guid id) =>
            Task.FromResult(_deployments.SingleOrDefault(deployment => deployment.Id == id));

    }

    private sealed class NoOpCityRepository(params City[] cities) : ICityRepository
    {
        private readonly List<City> _cities = cities.Where(city => city != null).ToList()!;

        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) =>
            Task.FromResult(_cities.Where(city => ids.Contains(city.Id)).ToList());

        public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult(_cities.SingleOrDefault(city => city.Id == cityId));
        public Task UpdateAsync(City city) => Task.CompletedTask;
        public Task<List<City>> GetAllAsync() => Task.FromResult(_cities.ToList());
        public Task UpdateRangeAsync(List<City> cities) => Task.CompletedTask;
        public Task AddAsync(City city) => Task.CompletedTask;
        public Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities) => Task.CompletedTask;
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetByCoordinatesAsync(int x, int y) => Task.FromResult<City?>(_cities.SingleOrDefault(city => city.X == x && city.Y == y));
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) => Task.FromResult<Guid?>(_cities.SingleOrDefault(city => city.Id == cityId)?.WorldPlayerId);
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) => Task.FromResult(_cities.Where(city => city.WorldPlayerId == worldPlayerId).ToList());
    }

    private sealed class TrackingBattleReportRepository : IBattleReportRepository
    {
        public List<BattleReport> AddedReports { get; } = [];

        public Task AddAsync(BattleReport report)
        {
            AddedReports.Add(report);
            return Task.CompletedTask;
        }

        public Task<BattleReport?> GetByIdAsync(Guid reportId) => Task.FromResult<BattleReport?>(null);
        public Task<List<BattleReport>> GetByUserIdAsync(Guid userId) => Task.FromResult(new List<BattleReport>());
        public Task<int> GetUnreadCountAsync(Guid userId) => Task.FromResult(0);
        public Task MarkAsReadAsync(Guid reportId) => Task.CompletedTask;
        public Task DeleteAsync(Guid reportId) => Task.CompletedTask;
    }

    private sealed class NoOpCityStatService : ICityStatService
    {
        public double GetWarehouseCapacity(City city) => 1000;
        public int GetMaxPopulation(City city) => 0;
        public int GetCurrentPopulationUsage(City city, IEnumerable<BaseJob> activeJobs) => 0;
        public int GetAvailablePopulation(City city, IEnumerable<BaseJob> activeJobs) => 0;
    }

    private sealed class NoOpModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(double baseValue, IEnumerable<ModifierTagEnum> targetTags, IEnumerable<IModifierProvider> providers) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityUnitValue(City city, UnitData unit, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
    }

    private sealed class NoOpIdeologyFocusRepository : IIdeologyFocusRepository
    {
        public Task<List<IdeologyFocus>?> GetAll() => Task.FromResult<List<IdeologyFocus>?>(new List<IdeologyFocus>());
        public Task<List<IdeologyFocus>?> GetAllActive() => Task.FromResult<List<IdeologyFocus>?>(new List<IdeologyFocus>());
        public Task<List<IdeologyFocus>?> GetAllByCityPlayer(Guid cityId) => Task.FromResult<List<IdeologyFocus>?>(new List<IdeologyFocus>());
        public Task UpdateAsync(IdeologyFocus ideologyFocus) => Task.CompletedTask;
        public Task AddAsync(IdeologyFocus ideologyFocus) => Task.CompletedTask;
        public Task DeleteExpiredFocusesForCityAsync(Guid cityId) => Task.CompletedTask;
        public Task DeleteAsync(IdeologyFocus ideologyFocus) => Task.CompletedTask;
    }
}
