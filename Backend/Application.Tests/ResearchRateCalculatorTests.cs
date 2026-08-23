using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;
using Domain.Workers;

namespace Application.Tests;

public class ResearchRateCalculatorTests
{
    [Theory]
    [InlineData(1, 1.00)]
    [InlineData(10, 1.54)]
    [InlineData(11, 1.60)]
    [InlineData(20, 2.14)]
    public void UniversityLevels_ExposeAuthoredResearchPower(int level, double expected)
    {
        var data = TestData.BuildingReader()
            .GetConfig<UniversityLevelData>(BuildingTypeEnum.University, level);

        Assert.Equal(expected, data.ResearchPower, 6);
    }

    [Fact]
    public void Calculate_SumsUniversitiesAndUsesPowerAsDirectSpeed()
    {
        var player = PlayerWithUniversities(6, 6, 4);

        var rate = TestData.ResearchRateCalculator().Calculate(player, TestData.Now);

        Assert.Equal(3.78d, rate.BaseResearchPower, 6);
        Assert.Equal(3.78d, rate.EffectiveResearchPower, 6);
        Assert.Equal(3.78d, rate.SpeedMultiplier, 6);
    }

    [Fact]
    public void Calculate_HasNoUpperSpeedCap()
    {
        var player = PlayerWithUniversities(Enumerable.Repeat(20, 20).ToArray());

        var rate = TestData.ResearchRateCalculator().Calculate(player, TestData.Now);

        Assert.True(rate.SpeedMultiplier > 1.6d);
    }

    [Fact]
    public void Progress_ThreePowerCompletesSeventyTwoHoursOfWorkInTwentyFourHours()
    {
        var player = PlayerWithUniversities(1, 1, 1);
        var progress = new ResearchProgressService(TestData.ResearchRateCalculator());
        var job = new ResearchJob { WorldPlayerId = player.Id, ResearchId = "TEST" };

        progress.Initialize(job, player, TimeSpan.FromHours(72).TotalSeconds, TestData.Now);

        Assert.Equal(3d, job.AppliedSpeedMultiplier, 6);
        Assert.Equal(TestData.Now.AddHours(24), job.ExecutionTime);
    }

    [Fact]
    public void Calculate_PreservesGlobalDemocracyAndCityScopedCivicInitiative()
    {
        var player = PlayerWithUniversities(1, 1);
        player.Ideology = IdeologyTypeEnum.Democracy;
        player.Cities[0].ActiveFocuses.Add(new IdeologyFocus
        {
            Name = IdeologyFocusNameEnum.CivicInitiative,
            TimeOfIdeologyStarted = TestData.Now.AddMinutes(-1),
            TimeOfIdeologyFinished = TestData.Now.AddHours(2)
        });

        var rate = TestData.ResearchRateCalculator().Calculate(player, TestData.Now);

        Assert.Equal(2d, rate.BaseResearchPower, 6);
        Assert.Equal(2.2d, rate.EffectiveResearchPower, 6);
        Assert.Equal(2.2d, rate.SpeedMultiplier, 6);
        Assert.Equal(TestData.Now.AddHours(2), rate.NextRateChangeAtUtc);
    }

    [Fact]
    public void Calculate_WithoutUniversityReturnsZero()
    {
        var player = new WorldPlayer { Id = Guid.NewGuid(), Cities = [] };

        var rate = TestData.ResearchRateCalculator().Calculate(player, TestData.Now);

        Assert.Equal(0d, rate.BaseResearchPower);
        Assert.Equal(0d, rate.SpeedMultiplier);
    }

    [Fact]
    public void Progress_RebasesAtUniversityUpgradeWithoutLosingEarnedWork()
    {
        var player = PlayerWithUniversities(1);
        var progress = new ResearchProgressService(TestData.ResearchRateCalculator());
        var job = new ResearchJob { WorldPlayerId = player.Id, ResearchId = "TEST" };
        progress.Initialize(job, player, 600d, TestData.Now);
        DateTime upgradedAt = TestData.Now.AddSeconds(100);

        progress.AdvanceTo(job, player, upgradedAt);
        player.Cities[0].Buildings.Single().Level = 10;
        progress.RefreshRateAndSchedule(job, player, upgradedAt);

        Assert.Equal(500d, job.RemainingWorkSeconds, 6);
        Assert.True(job.AppliedSpeedMultiplier > 1d);
        Assert.True(job.ExecutionTime < TestData.Now.AddSeconds(600));
    }

    [Fact]
    public void Progress_RebasesAtCivicInitiativeStartWithoutLosingEarnedWork()
    {
        var player = PlayerWithUniversities(1);
        var progress = new ResearchProgressService(TestData.ResearchRateCalculator());
        var job = new ResearchJob { WorldPlayerId = player.Id, ResearchId = "TEST" };
        progress.Initialize(job, player, 600d, TestData.Now);
        DateTime focusStartedAt = TestData.Now.AddSeconds(100);

        progress.AdvanceTo(job, player, focusStartedAt);
        player.Cities[0].ActiveFocuses.Add(new IdeologyFocus
        {
            Name = IdeologyFocusNameEnum.CivicInitiative,
            TimeOfIdeologyStarted = focusStartedAt,
            TimeOfIdeologyFinished = focusStartedAt.AddHours(2)
        });
        progress.RefreshRateAndSchedule(job, player, focusStartedAt);

        Assert.Equal(500d, job.RemainingWorkSeconds, 6);
        Assert.True(job.AppliedSpeedMultiplier > 1d);
        Assert.Equal(focusStartedAt.AddHours(2),
            TestData.ResearchRateCalculator().Calculate(player, focusStartedAt).NextRateChangeAtUtc);
    }

    [Fact]
    public void Progress_DelayedWorkerCrossesCivicExpiryAndCompletesOnlyOnce()
    {
        var player = PlayerWithUniversities(1);
        player.Cities[0].ActiveFocuses.Add(new IdeologyFocus
        {
            Name = IdeologyFocusNameEnum.CivicInitiative,
            TimeOfIdeologyStarted = TestData.Now,
            TimeOfIdeologyFinished = TestData.Now.AddHours(2)
        });
        var progress = new ResearchProgressService(TestData.ResearchRateCalculator());
        var job = new ResearchJob { WorldPlayerId = player.Id, ResearchId = "TEST" };
        progress.Initialize(job, player, 10_800d, TestData.Now);

        progress.AdvanceTo(job, player, TestData.Now.AddHours(3));
        DateTime completedAt = job.LastProgressAt;
        progress.AdvanceTo(job, player, TestData.Now.AddHours(4));

        Assert.True(job.IsCompleted);
        Assert.Equal(0d, job.RemainingWorkSeconds, 6);
        Assert.InRange(completedAt, TestData.Now.AddHours(2), TestData.Now.AddHours(3));
        Assert.Equal(completedAt, job.LastProgressAt);
    }

    private static WorldPlayer PlayerWithUniversities(params int[] levels)
    {
        var player = new WorldPlayer { Id = Guid.NewGuid(), Cities = [] };
        foreach (int level in levels)
        {
            var city = new City
            {
                Id = Guid.NewGuid(),
                WorldPlayer = player,
                WorldPlayerId = player.Id,
                Buildings = [new Building { Type = BuildingTypeEnum.University, Level = level }],
                ActiveFocuses = []
            };
            player.Cities.Add(city);
        }
        return player;
    }
}
