using Application.Interfaces.IRepositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Application.Utility;
using Microsoft.Extensions.Logging.Abstractions;
using Domain.User;

namespace Application.Tests;

public class IdeologyFocusGameplayTests
{
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
        ResourceService CreateService() => new(TestData.BuildingReader(), TestData.ResearchReader(),
            TestData.IdeologyReader(), TestData.UnitReader(), stats, modifiers, NullLogger<ResourceService>.Instance);

        var city = TestData.CityWithFocus(focus);
        city.WorldPlayer!.LastResourceUpdate = TestData.Now;
        city.Buildings.Add(new Building { Type = BuildingTypeEnum.University, Level = 1, City = city, CityId = city.Id });
        city.Buildings.Add(new Building { Type = BuildingTypeEnum.TownHall, Level = 1, City = city, CityId = city.Id });
        city.UnitStacks.Add(new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 10 });
        var withFocus = CreateService().CalculateGlobalResources(city.WorldPlayer, TestData.Now);
        city.ActiveFocuses.Clear();
        var baseline = CreateService().CalculateGlobalResources(city.WorldPlayer, TestData.Now);

        if (focus == IdeologyFocusNameEnum.CivicInitiative)
            Assert.True(withFocus.ResearchPointsPerHour > baseline.ResearchPointsPerHour);
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
        Assert.Equal(7200.0 / 108, calculator.CalculateSecondsPerHex(snapshot), 6);
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

}
