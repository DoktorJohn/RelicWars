using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Application.Utility;
using Microsoft.Extensions.Logging.Abstractions;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;

namespace Application.Tests;

public class IdeologyFocusGameplayTests
{
    [Theory]
    [InlineData(IdeologyFocusNameEnum.FeudalMuster)]
    [InlineData(IdeologyFocusNameEnum.LordsLevy)]
    public async Task SuccessfulFocusEnactmentCreatesUnreadPlayerReport(IdeologyFocusNameEnum focusName)
    {
        var city = TestData.CityWithFocus(focusName);
        city.Name = "Ravenhold";
        city.ActiveFocuses.Clear();
        city.WorldPlayer!.IdeologyFocusPoints = 100;

        var focusData = TestData.FocusReader().GetIdeology(focusName);
        city.WorldPlayer.Ideology = focusData.RequiredIdeology;

        var reports = new RecordingBattleReportRepository();
        var service = CreateEnactmentService(city, reports);

        var result = await service.EnactIdeologyFocus(new(focusName, city.Id));

        Assert.NotNull(result);
        Assert.True(result.Success);
        var report = Assert.Single(reports.Reports);
        Assert.Equal(city.WorldPlayer.Id, report.WorldPlayerId);
        Assert.Equal(ReportTypeEnum.FocusEnacted, report.ReportType);
        Assert.False(report.IsRead);
        Assert.Equal(TestData.Now, report.OccurredAt);
        Assert.Contains("Focus enacted:", report.Title);
        Assert.Contains("Ravenhold", report.Body);
        Assert.Contains($"{focusData.IdeologyFocusPointCost:0.##} ideology points", report.Body);
    }

    [Fact]
    public async Task RejectedFocusEnactmentDoesNotCreateReport()
    {
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.FeudalMuster);
        city.ActiveFocuses.Clear();
        city.WorldPlayer!.IdeologyFocusPoints = 0;
        city.WorldPlayer.Ideology = TestData.FocusReader()
            .GetIdeology(IdeologyFocusNameEnum.FeudalMuster).RequiredIdeology;

        var reports = new RecordingBattleReportRepository();
        var service = CreateEnactmentService(city, reports);

        var result = await service.EnactIdeologyFocus(new(IdeologyFocusNameEnum.FeudalMuster, city.Id));

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Empty(reports.Reports);
    }

    [Fact]
    public async Task CivicInitiative_RebasesActiveResearchAtEnactmentTime()
    {
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.CivicInitiative);
        city.ActiveFocuses.Clear();
        city.Buildings.Add(new Building { Type = BuildingTypeEnum.University, Level = 1, City = city, CityId = city.Id });
        city.WorldPlayer!.Ideology = IdeologyTypeEnum.Democracy;
        city.WorldPlayer.IdeologyFocusPoints = 100;

        var progress = new ResearchProgressService(TestData.ResearchRateCalculator());
        var job = new ResearchJob { WorldPlayerId = city.WorldPlayer.Id, ResearchId = "TEST" };
        progress.Initialize(job, city.WorldPlayer, 600d, TestData.Now.AddSeconds(-100));
        double expectedRemainingWork = 600d - 100d * job.AppliedSpeedMultiplier;
        var jobs = new SingleResearchJobRepository(job);
        var service = CreateEnactmentService(city, new RecordingBattleReportRepository(), jobs);

        var result = await service.EnactIdeologyFocus(new(IdeologyFocusNameEnum.CivicInitiative, city.Id));

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(expectedRemainingWork, job.RemainingWorkSeconds, 6);
        Assert.True(job.AppliedSpeedMultiplier > 1d);
        Assert.Equal(1, jobs.UpdateCount);
    }

    [Fact]
    public void NobleClemencyIncreasesElapsedResistanceRecovery()
    {
        var modifiers = TestData.ModifierService(out _);
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.NobleClemency);
        city.Resistance = 50;
        city.ResistanceTarget = 100;
        city.LastResistanceUpdate = TestData.Now.AddHours(-10);
        new ResistanceService(modifiers).UpdateResistance(city, TestData.Now);
        Assert.Equal(61, city.Resistance, 6);
    }

    [Fact]
    public void InstantFocusCannotRepeatButExpiredTimedFocusCan()
    {
        var reader = TestData.FocusReader();
        var policy = new FocusEnactmentPolicy();
        var instant = reader.GetIdeology(IdeologyFocusNameEnum.NewRecruits);
        var timed = reader.GetIdeology(IdeologyFocusNameEnum.CrownTax);
        Assert.False(policy.CanEnact(instant, new[] { new IdeologyFocus { Name = instant.Name } }, TestData.Now));
        Assert.True(policy.CanEnact(timed, new[] { new IdeologyFocus
        {
            Name = timed.Name,
            TimeOfIdeologyStarted = TestData.Now.AddHours(-3),
            TimeOfIdeologyFinished = TestData.Now.AddHours(-1)
        } }, TestData.Now));
    }

    [Fact]
    public void RepeatableInstantFocusIgnoresHistoricalRecordsAndIsNeverPersisted()
    {
        var data = TestData.FocusReader().GetIdeology(IdeologyFocusNameEnum.LordsLevy);
        var policy = new FocusEnactmentPolicy();
        var historical = new[] { new IdeologyFocus { Name = data.Name } };

        Assert.True(data.CanRepeat);
        Assert.True(policy.CanEnact(data, historical, TestData.Now));
        Assert.False(policy.ShouldPersist(data));
    }

    [Theory]
    [InlineData(IdeologyFocusNameEnum.CrownTax)]
    [InlineData(IdeologyFocusNameEnum.MarketSurge)]
    [InlineData(IdeologyFocusNameEnum.CivicInitiative)]
    [InlineData(IdeologyFocusNameEnum.EconomicTransparency)]
    [InlineData(IdeologyFocusNameEnum.IronDiscipline)]
    public void EconomyFocusesChangeGlobalRate(IdeologyFocusNameEnum focus)
    {
        var modifiers = TestData.ModifierService(out _);
        var stats = new FixedCityStatService { Available = 100 };
        ResourceService CreateService() => new(TestData.BuildingReader(),
            TestData.IdeologyReader(), stats, modifiers, NullLogger<ResourceService>.Instance);

        var city = TestData.CityWithFocus(focus);
        city.WorldPlayer!.LastResourceUpdate = TestData.Now;
        city.Buildings.Add(new Building { Type = BuildingTypeEnum.University, Level = 1, City = city, CityId = city.Id });
        city.Buildings.Add(new Building { Type = BuildingTypeEnum.TownHall, Level = 1, City = city, CityId = city.Id });
        city.UnitStacks.Add(new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 10 });
        var withFocus = CreateService().CalculateGlobalResources(city.WorldPlayer, TestData.Now);
        var researchWithFocus = TestData.ResearchRateCalculator().Calculate(city.WorldPlayer, TestData.Now);
        city.ActiveFocuses.Clear();
        var baseline = CreateService().CalculateGlobalResources(city.WorldPlayer, TestData.Now);
        var baselineResearch = TestData.ResearchRateCalculator().Calculate(city.WorldPlayer, TestData.Now);

        if (focus == IdeologyFocusNameEnum.CivicInitiative)
            Assert.True(researchWithFocus.EffectiveResearchPower > baselineResearch.EffectiveResearchPower);
        else
            Assert.NotEqual(withFocus.CoinsProductionPerHour, baseline.CoinsProductionPerHour);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(99, 0)]
    [InlineData(100, 8)]
    [InlineData(250, 16)]
    public async Task LordsLevyUsesCompletedPopulationBreakpoints(int population, int expected)
    {
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.LordsLevy);
        city.ActiveFocuses.Clear();
        var stats = new FixedCityStatService { Available = population };
        var jobs = new EmptyJobRepository();
        var units = TestData.UnitReader();
        var utility = new InstantUtility(new MemoryCityRepository(city), jobs, stats, units);
        var service = new InstantFocusGrantService(utility, stats, jobs, units, new FixedRandomService());
        var result = await service.GrantLordsLevy(city);
        Assert.Equal(expected, result.GrantedQuantity);
    }

    [Fact]
    public async Task NewRecruitsAreNonEliteAndRespectPopulationCap()
    {
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.NewRecruits);
        city.ActiveFocuses.Clear();
        var stats = new FixedCityStatService { Available = 30 };
        var jobs = new EmptyJobRepository();
        var units = TestData.UnitReader();
        var utility = new InstantUtility(new MemoryCityRepository(city), jobs, stats, units);
        var service = new InstantFocusGrantService(utility, stats, jobs, units, new FixedRandomService());
        var result = await service.GrantNewRecruits(city);
        Assert.True(result.GrantedQuantity <= 10);
        Assert.All(result.GrantedUnits, stack => Assert.False(units.GetUnit(stack.Type).IsElite));
    }

    [Fact]
    public void PrivateSecuritySnapshotsOnlyTradeDeployments()
    {
        var modifiers = TestData.ModifierService(out _);
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.PrivateSecurity);
        var service = new DeploymentModifierSnapshotService(modifiers);
        var trade = new UnitDeployment { OriginCity = city, Type = UnitDeploymentTypeEnum.Trade };
        var attack = new UnitDeployment { OriginCity = city, Type = UnitDeploymentTypeEnum.Attack };
        service.ApplyOutgoingModifiers(city, trade);
        service.ApplyOutgoingModifiers(city, attack);
        Assert.Equal(0.15, trade.ModifiersInternal.Single().Value, 6);
        Assert.Empty(attack.ModifiersInternal);

        CombatResult Fight(UnitDeployment defender) => new CombatService(TestData.UnitReader(), modifiers, new FixedRandomService()).ResolveBattle(new CombatContext(
            new() { new UnitStack { Type = UnitTypeEnum.Swordsmen, Quantity = 20 } },
            new() { new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 100 } }, null, null,
            null, null, null, defender));
        var protectedLosses = Fight(trade).DefenderLosses.Sum(x => x.Quantity);
        var unprotectedLosses = Fight(attack).DefenderLosses.Sum(x => x.Quantity);
        Assert.True(protectedLosses < unprotectedLosses);
    }

    [Fact]
    public void RoyalLogisticsProducesStableMovementSnapshot()
    {
        var modifiers = TestData.ModifierService(out _);
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.RoyalLogistics);
        var calculator = new UnitMovementCalculator(modifiers);
        int snapshot = calculator.CalculateMobilitySnapshot(city, 100);
        Assert.Equal(108, snapshot);
        city.ActiveFocuses.Clear();
        Assert.Equal(720.0 / 108, calculator.CalculateSecondsPerHex(snapshot), 6);
    }

    [Fact]
    public void CombatFocusesChangeDefenderOutcomeAndReviveLosses()
    {
        var units = TestData.UnitReader();
        var baselineModifiers = TestData.ModifierService(out _);
        var random = new FixedRandomService(0.5);

        var baselineCity = TestData.CityWithFocus(IdeologyFocusNameEnum.CivicInitiative);
        var baseline = new CombatService(units, baselineModifiers, random).ResolveBattle(new CombatContext(
            new() { new UnitStack { Type = UnitTypeEnum.Swordsmen, Quantity = 20 } },
            new() { new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 100 } }, null, baselineCity));

        var fortifiedCity = TestData.CityWithFocus(IdeologyFocusNameEnum.FortifiedCity);
        var fortified = new CombatService(units, baselineModifiers, random).ResolveBattle(new CombatContext(
            new() { new UnitStack { Type = UnitTypeEnum.Swordsmen, Quantity = 20 } },
            new() { new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 100 } }, null, fortifiedCity));
        Assert.True(fortified.DefenderLosses.Sum(x => x.Quantity) < baseline.DefenderLosses.Sum(x => x.Quantity));

        var medicCity = TestData.CityWithFocus(IdeologyFocusNameEnum.RoyalMedics);
        var medic = new CombatService(units, baselineModifiers, random).ResolveBattle(new CombatContext(
            new() { new UnitStack { Type = UnitTypeEnum.Swordsmen, Quantity = 20 } },
            new() { new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 100 } }, null, medicCity));
        Assert.NotEmpty(medic.RevivedDefenders);
        Assert.Equal((int)Math.Floor((medic.DefenderLosses.Sum(x => x.Quantity) + medic.RevivedDefenders.Sum(x => x.Quantity)) * 0.1), medic.RevivedDefenders.Sum(x => x.Quantity));
    }

    [Fact]
    public void DefenderCombatModifiersAffectTheirIntendedStage()
    {
        var units = TestData.UnitReader();
        var modifiers = TestData.ModifierService(out _);
        var random = new FixedRandomService(0.5);
        CombatResult Fight(IdeologyFocusNameEnum focus) => new CombatService(units, modifiers, random).ResolveBattle(new CombatContext(
            new() { new UnitStack { Type = UnitTypeEnum.Swordsmen, Quantity = 20 } },
            new() { new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 100 } }, null, TestData.CityWithFocus(focus)));

        var baseline = Fight(IdeologyFocusNameEnum.CivicInitiative);
        Assert.True(Fight(IdeologyFocusNameEnum.FeudalMuster).DefenderLosses.Sum(x => x.Quantity) < baseline.DefenderLosses.Sum(x => x.Quantity));
        Assert.True(Fight(IdeologyFocusNameEnum.OathOfBlood).DefenderLosses.Sum(x => x.Quantity) < baseline.DefenderLosses.Sum(x => x.Quantity));
        Assert.True(Fight(IdeologyFocusNameEnum.CitizenMorale).AttackerLosses.Sum(x => x.Quantity) >= baseline.AttackerLosses.Sum(x => x.Quantity));
    }

    [Fact]
    public void OathOfBloodOnlyAppliesToOwnerOrAllianceDefenders()
    {
        var units = TestData.UnitReader();
        var modifiers = TestData.ModifierService(out _);
        var random = new FixedRandomService(0.5);
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.OathOfBlood);
        CombatResult Fight(WorldPlayer defender) => new CombatService(units, modifiers, random).ResolveBattle(new CombatContext(
            new() { new UnitStack { Type = UnitTypeEnum.Swordsmen, Quantity = 20 } },
            new() { new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 100 } }, null, city, null, defender));
        var ownerLosses = Fight(city.WorldPlayer!).DefenderLosses.Sum(x => x.Quantity);
        var enemyLosses = Fight(new WorldPlayer { Id = Guid.NewGuid(), CompletedResearches = new(), Cities = new() }).DefenderLosses.Sum(x => x.Quantity);
        Assert.True(ownerLosses < enemyLosses);
    }

    private static IdeologyFocusService CreateEnactmentService(
        City city,
        RecordingBattleReportRepository reports,
        IJobRepository? jobRepository = null)
    {
        var cityRepository = new MemoryCityRepository(city);
        var jobs = jobRepository ?? new EmptyJobRepository();
        var stats = new FixedCityStatService { Available = 100 };
        var units = TestData.UnitReader();
        var instantUtility = new InstantUtility(cityRepository, jobs, stats, units);

        return new IdeologyFocusService(
            new NoOpWorldPlayerRepository(),
            new NoOpWorldPlayerService(),
            new TestPlayerAccessService([city.WorldPlayer!], [city]),
            cityRepository,
            TestData.FocusReader(),
            TestData.IdeologyReader(),
            instantUtility,
            stats,
            jobs,
            new NoOpResourceService(),
            new MemoryIdeologyFocusRepository(city),
            units,
            new FixedRandomService(),
            new FixedTimeProvider(TestData.Now),
            new InstantFocusGrantService(instantUtility, stats, jobs, units, new FixedRandomService()),
            new FocusEnactmentPolicy(),
            reports,
            new ImmediateTransactionManager(),
            new ResearchProgressService(TestData.ResearchRateCalculator()));
    }

    private sealed class SingleResearchJobRepository(ResearchJob job) : IJobRepository
    {
        public int UpdateCount { get; private set; }
        public Task<BaseJob?> GetByIdAsync(Guid id) => Task.FromResult<BaseJob?>(job.Id == id ? job : null);
        public Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId) => Task.FromResult(new List<BuildingJob>());
        public Task AddAsync(BaseJob newJob) => Task.CompletedTask;
        public Task UpdateAsync(BaseJob updatedJob) { UpdateCount++; return Task.CompletedTask; }
        public Task DeleteAsync(Guid jobId) => Task.CompletedTask;
        public void DeletePending(BaseJob deletedJob) => throw new InvalidOperationException("Research should not complete in this test.");
        public Task<ResearchJob?> GetResearchJobAsync(Guid userId) =>
            Task.FromResult<ResearchJob?>(job.WorldPlayerId == userId && !job.IsCompleted ? job : null);
        public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) => Task.FromResult(new List<RecruitmentJob>());
        public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) =>
            Task.FromResult(job.WorldPlayerId == id ? new List<ResearchJob> { job } : new List<ResearchJob>());
    }

    private sealed class RecordingBattleReportRepository : IBattleReportRepository
    {
        public List<BattleReport> Reports { get; } = [];
        public Task AddAsync(BattleReport report) { Reports.Add(report); return Task.CompletedTask; }
        public Task<BattleReport?> GetByIdAsync(Guid reportId) => throw new NotSupportedException();
        public Task<List<BattleReport>> GetByUserIdAsync(Guid userId) => throw new NotSupportedException();
        public Task<int> GetUnreadCountAsync(Guid userId) => throw new NotSupportedException();
        public Task MarkAsReadAsync(Guid reportId) => throw new NotSupportedException();
        public Task DeleteAsync(Guid reportId) => throw new NotSupportedException();
    }

    private sealed class MemoryIdeologyFocusRepository(City city) : IIdeologyFocusRepository
    {
        public Task<List<IdeologyFocus>?> GetAll() => Task.FromResult<List<IdeologyFocus>?>(city.ActiveFocuses);
        public Task<List<IdeologyFocus>?> GetAllActive() => GetAll();
        public Task<List<IdeologyFocus>?> GetAllByCityPlayer(Guid cityId) => GetAll();
        public Task UpdateAsync(IdeologyFocus ideologyFocus) => Task.CompletedTask;
        public Task AddAsync(IdeologyFocus ideologyFocus) { city.ActiveFocuses.Add(ideologyFocus); return Task.CompletedTask; }
        public Task DeleteExpiredFocusesForCityAsync(Guid cityId) => Task.CompletedTask;
        public Task DeleteAsync(IdeologyFocus ideologyFocus) { city.ActiveFocuses.Remove(ideologyFocus); return Task.CompletedTask; }
    }

    private sealed class NoOpWorldPlayerRepository : IWorldPlayerRepository
    {
        public Task<WorldPlayer?> GetByIdAsync(Guid id) => throw new NotSupportedException();
        public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) => throw new NotSupportedException();
        public Task AddAsync(WorldPlayer user) => throw new NotSupportedException();
        public Task UpdateAsync(WorldPlayer user) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => throw new NotSupportedException();
        public Task<List<WorldPlayer>>? GetAllAsync() => throw new NotSupportedException();
        public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) => throw new NotSupportedException();
        public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) => throw new NotSupportedException();
        public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery) => throw new NotSupportedException();
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
        public CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime) => throw new NotSupportedException();
        public CityProductionSnapshot CalculateCityProduction(WorldPlayer playerEntity, City cityEntity) => throw new NotSupportedException();
        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime) => throw new NotSupportedException();
    }

}
