using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class UnitDeploymentServiceTests
{
    [Fact]
    public async Task AttackCityDeploymentAsync_RemovesUnitsAndCreatesOutboundDeployment()
    {
        var setup = CreateSetup();
        var request = new AttackCityDeploymentRequestDTO(
            setup.City.Id,
            setup.TargetCity.Id,
            [new UnitSelectionDTO(UnitTypeEnum.Militia, 3)]);

        var result = await setup.Service.AttackCityDeploymentAsync(request);

        Assert.Equal(7, setup.City.UnitStacks.Single(stack => stack.Type == UnitTypeEnum.Militia).Quantity);
        Assert.Equal(2, setup.DeploymentRepository.Deployments.Count);
        Assert.Equal(setup.City.Id, result.OriginCityId);
        Assert.Equal(setup.TargetCity.Id, result.TargetCityId);
        Assert.Equal(UnitDeploymentMovementStatusEnum.Moving, result.Status);
        Assert.True(result.ArrivalTime > DateTime.UtcNow);
    }

    [Fact]
    public async Task GetActiveDeploymentsAsync_ReturnsOnlyOwnedMovingDeployments()
    {
        var setup = CreateSetup();
        var activeDeployment = setup.DeploymentRepository.Deployments[0];
        activeDeployment.ArrivalTime = DateTime.UtcNow.AddMinutes(10);
        activeDeployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving;

        setup.DeploymentRepository.Deployments.Add(new UnitDeployment
        {
            Id = Guid.NewGuid(),
            Name = "Stationed",
            WorldPlayerId = setup.Player.Id,
            WorldId = setup.Player.WorldId,
            OriginCity = setup.City,
            OriginCityId = setup.City.Id,
            OwnerWorldPlayer = setup.Player,
            UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed,
            ArrivalTime = DateTime.UtcNow.AddMinutes(5),
            DepartureTime = DateTime.UtcNow.AddMinutes(-5),
            Mobility = 2,
            UnitStacks = [new UnitStack { Id = Guid.NewGuid(), Type = UnitTypeEnum.Militia, Quantity = 1, WorldPlayerId = setup.Player.Id }]
        });

        var deployments = await setup.Service.GetActiveDeploymentsAsync(setup.Player.Id);

        Assert.Single(deployments);
        Assert.Equal(activeDeployment.Id, deployments[0].Id);
    }

    [Fact]
    public async Task AttackCityDeploymentAsync_RejectsOwnCity()
    {
        var setup = CreateSetup();
        var request = new AttackCityDeploymentRequestDTO(
            setup.City.Id,
            setup.City.Id,
            [new UnitSelectionDTO(UnitTypeEnum.Militia, 1)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => setup.Service.AttackCityDeploymentAsync(request));
        Assert.Equal(10, setup.City.UnitStacks.Single().Quantity);
    }

    [Fact]
    public async Task AttackCityDeploymentAsync_RejectsDuplicateUnitSelectionsBeforeMutation()
    {
        var setup = CreateSetup();
        var request = new AttackCityDeploymentRequestDTO(
            setup.City.Id,
            setup.TargetCity.Id,
            [
                new UnitSelectionDTO(UnitTypeEnum.Militia, 2),
                new UnitSelectionDTO(UnitTypeEnum.Militia, 2)
            ]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => setup.Service.AttackCityDeploymentAsync(request));
        Assert.Equal(10, setup.City.UnitStacks.Single().Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task AttackCityDeploymentAsync_RejectsInvalidQuantityBeforeMutation(int quantity)
    {
        var setup = CreateSetup();
        var request = new AttackCityDeploymentRequestDTO(
            setup.City.Id,
            setup.TargetCity.Id,
            [new UnitSelectionDTO(UnitTypeEnum.Militia, quantity)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => setup.Service.AttackCityDeploymentAsync(request));
        Assert.Equal(10, setup.City.UnitStacks.Single().Quantity);
    }

    [Fact]
    public async Task AttackCityDeploymentAsync_RejectsEmptySelectionBeforeMutation()
    {
        var setup = CreateSetup();
        var request = new AttackCityDeploymentRequestDTO(setup.City.Id, setup.TargetCity.Id, []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => setup.Service.AttackCityDeploymentAsync(request));
        Assert.Equal(10, setup.City.UnitStacks.Single().Quantity);
    }

    [Fact]
    public async Task AttackCityDeploymentAsync_ValidatesEverySelectionBeforeMutation()
    {
        var setup = CreateSetup();
        var request = new AttackCityDeploymentRequestDTO(
            setup.City.Id,
            setup.TargetCity.Id,
            [
                new UnitSelectionDTO(UnitTypeEnum.Militia, 2),
                new UnitSelectionDTO(UnitTypeEnum.Bowmen, 1)
            ]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => setup.Service.AttackCityDeploymentAsync(request));
        Assert.Equal(10, setup.City.UnitStacks.Single().Quantity);
    }

    [Fact]
    public async Task RecallAsync_CreatesSupportRecallReport()
    {
        var setup = CreateSetup();
        setup.Deployment.Type = UnitDeploymentTypeEnum.Support;
        setup.Deployment.Phase = UnitDeploymentPhaseEnum.Stationed;

        await setup.Service.RecallAsync(setup.Deployment.Id);

        var report = Assert.Single(setup.ReportRepository.Reports);
        Assert.Equal(ReportTypeEnum.SupportingUnitsRecalled, report.ReportType);
        Assert.Equal(setup.Player.Id, report.WorldPlayerId);
    }

    private static Setup CreateSetup()
    {
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Player" }
        };
        player.PlayerProfileId = player.PlayerProfile.Id;
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Capital",
            WorldId = player.WorldId,
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            X = 0,
            Y = 0,
            UnitStacks =
            [
                new() { Id = Guid.NewGuid(), Type = UnitTypeEnum.Militia, Quantity = 10, WorldPlayerId = player.Id, CityId = Guid.NewGuid() }
            ]
        };
        var targetPlayer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = player.WorldId,
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Target" }
        };
        targetPlayer.PlayerProfileId = targetPlayer.PlayerProfile.Id;
        var targetCity = new City
        {
            Id = Guid.NewGuid(),
            Name = "Target",
            WorldId = player.WorldId,
            WorldPlayerId = targetPlayer.Id,
            WorldPlayer = targetPlayer,
            X = 2,
            Y = 0,
            UnitStacks = []
        };
        var deployment = new UnitDeployment
        {
            Id = Guid.NewGuid(),
            Name = "Retinue of Capital",
            Mobility = 2,
            Type = UnitDeploymentTypeEnum.Attack,
            UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving,
            ArrivalTime = DateTime.UtcNow.AddMinutes(30),
            DepartureTime = DateTime.UtcNow.AddMinutes(-30),
            OriginCity = city,
            OriginCityId = city.Id,
            OwnerWorldPlayer = player,
            WorldPlayerId = player.Id,
            WorldId = player.WorldId,
            UnitStacks =
            [
                new() { Id = Guid.NewGuid(), Type = UnitTypeEnum.Militia, Quantity = 3, WorldPlayerId = player.Id }
            ],
            TargetCity = targetCity,
            TargetCityId = targetCity.Id
        };

        var access = new TrackingPlayerAccessService(player, city, deployment);
        var deploymentRepository = new TrackingUnitDeploymentRepository(deployment);
        var reportRepository = new TrackingBattleReportRepository();
        var service = new UnitDeploymentService(
            NullLogger<UnitDeploymentService>.Instance,
            deploymentRepository,
            access,
            new MemoryCityRepository(city, targetCity),
            TestData.UnitReader(),
            new DeploymentModifierSnapshotService(new NoOpModifierService()),
            new UnitMovementCalculator(new NoOpModifierService()),
            new ImmediateTransactionManager(),
            new DeploymentPermissionService(new TestAllianceRepository()),
            reportRepository);

        return new Setup(player, city, targetCity, deployment, deploymentRepository, reportRepository, access, service);
    }

    private sealed record Setup(
        WorldPlayer Player,
        City City,
        City TargetCity,
        UnitDeployment Deployment,
        TrackingUnitDeploymentRepository DeploymentRepository,
        TrackingBattleReportRepository ReportRepository,
        TrackingPlayerAccessService Access,
        UnitDeploymentService Service);

    private sealed class TrackingBattleReportRepository : IBattleReportRepository
    {
        public List<BattleReport> Reports { get; } = [];
        public Task AddAsync(BattleReport report) { Reports.Add(report); return Task.CompletedTask; }
        public Task<BattleReport?> GetByIdAsync(Guid reportId) => Task.FromResult<BattleReport?>(null);
        public Task<List<BattleReport>> GetByUserIdAsync(Guid userId) => Task.FromResult(new List<BattleReport>());
        public Task<int> GetUnreadCountAsync(Guid userId) => Task.FromResult(0);
        public Task MarkAsReadAsync(Guid reportId) => Task.CompletedTask;
        public Task DeleteAsync(Guid reportId) => Task.CompletedTask;
    }

    private sealed class TrackingPlayerAccessService(WorldPlayer player, City city, UnitDeployment deployment) : IPlayerAccessService
    {
        public Guid GetAuthenticatedProfileId() => player.PlayerProfileId;
        public Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId) =>
            Task.FromResult(worldPlayerId == player.Id ? player : throw new KeyNotFoundException());
        public Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId) =>
            Task.FromResult(worldId == player.WorldId ? player : throw new UnauthorizedAccessException());
        public Task<City> RequireOwnedCityAsync(Guid cityId) =>
            Task.FromResult(cityId == city.Id ? city : throw new KeyNotFoundException());
        public Task<City> RequireOwnedCityForTownHallAsync(Guid cityId) =>
            Task.FromResult(cityId == city.Id ? city : throw new KeyNotFoundException());
        public Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId) =>
            Task.FromResult(unitDeploymentId == deployment.Id ? deployment : throw new KeyNotFoundException());
    }

    private sealed class TrackingUnitDeploymentRepository(UnitDeployment deployment) : IUnitDeploymentRepository
    {
        public List<UnitDeployment> Deployments { get; } = [deployment];

        public Task<List<UnitDeployment>> GetUnitDeploymentsWithStacksByListOfIdsAsync(List<Guid> ids) =>
            Task.FromResult(Deployments.Where(d => ids.Contains(d.Id)).ToList());

        public Task<List<UnitDeployment>> GetActiveDeploymentsByWorldPlayerIdAsync(Guid worldPlayerId) =>
            Task.FromResult(Deployments.Where(d => d.WorldPlayerId == worldPlayerId && d.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Moving).ToList());

        public Task AddAsync(UnitDeployment deployment)
        {
            Deployments.Add(deployment);
            return Task.CompletedTask;
        }

        public Task<List<UnitDeployment>> GetDueMovementsAsync(DateTime now, int batchSize) => Task.FromResult(new List<UnitDeployment>());

        public Task UpdateAsync(UnitDeployment deployment) => Task.CompletedTask;

        public Task DeleteAsync(UnitDeployment deployment)
        {
            Deployments.Remove(deployment);
            return Task.CompletedTask;
        }

        public Task<UnitDeployment?> GetByIdAsync(Guid id) => Task.FromResult(Deployments.SingleOrDefault(deployment => deployment.Id == id));

    }

    private sealed class MemoryCityRepository(params City[] cities) : ICityRepository
    {
        private readonly List<City> _cities = cities.ToList();

        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) =>
            Task.FromResult(_cities.Where(city => ids.Contains(city.Id)).ToList());

        public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult(_cities.SingleOrDefault(city => city.Id == cityId));
        public Task UpdateAsync(City city) => Task.CompletedTask;
        public Task<List<City>> GetAllAsync() => Task.FromResult(_cities.ToList());
        public Task UpdateRangeAsync(List<City> cities) => Task.CompletedTask;
        public Task AddAsync(City city) => Task.CompletedTask;
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetByCoordinatesAsync(int x, int y) => Task.FromResult<City?>(_cities.SingleOrDefault(city => city.X == x && city.Y == y));
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) => Task.FromResult<Guid?>(_cities.SingleOrDefault(city => city.Id == cityId)?.WorldPlayerId);
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) => Task.FromResult(_cities.Where(city => city.WorldPlayerId == worldPlayerId).ToList());
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
}
