using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Services.Buildings;
using Application.Utility;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
using Domain.User;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Tests;

public class UnitAvailabilityTests
{
    [Fact]
    public void ResearchCatalog_ContainsCompleteResearchableProgressionGraph()
    {
        var research = TestData.ResearchReader().GetAll();

        Assert.Equal(74, research.Count);
        Assert.All(research, node =>
        {
            Assert.True(node.IsResearchable);
            Assert.True(node.ResearchTimeInSeconds > 0);
        });
        Assert.Equal(23, research.Count(node => node.ResearchType == ResearchTypeEnum.Economy));
        Assert.Equal(29, research.Count(node => node.ResearchType == ResearchTypeEnum.War));
        Assert.Equal(22, research.Count(node => node.ResearchType == ResearchTypeEnum.Utility));
        Assert.Equal(new[] { "Economy", "War", "Utility" }, Enum.GetNames<ResearchTypeEnum>());
        Assert.DoesNotContain(research, node => node.Id.StartsWith("UNLOCK_", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(7, research.Count(node => node.PrerequisiteRule == ResearchPrerequisiteRule.RequiresAny));
        Assert.Equal(74, research.Select(node => node.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var ids = research.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(research, node =>
            Assert.All(node.PrerequisiteIds, prerequisiteId => Assert.Contains(prerequisiteId, ids)));
        Assert.All(
            research.Where(node => node.PrerequisiteRule == ResearchPrerequisiteRule.Start),
            node => Assert.Equal(43_200, node.ResearchTimeInSeconds));
        Assert.Equal(5, research.Count(node => node.PrerequisiteRule == ResearchPrerequisiteRule.Start));
        AssertAcyclic(research);

        string rawJson = File.ReadAllText(TestData.GameFile("research.json"));
        string embeddedDefault = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../Domain/StaticData/Defaults/research.json")));
        Assert.Equal(rawJson, embeddedDefault);
        Assert.DoesNotContain("\"Effects\"", rawJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ResearchType\": \"Unlocks\"", rawJson, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeStaticData_MatchesGeneratorsSemantically()
    {
        var unitPath = Path.GetTempFileName();
        var researchPath = Path.GetTempFileName();
        try
        {
            UnitDataGenerator.GenerateDefaultJson(unitPath);
            ResearchDataGenerator.GenerateDefaultJson(researchPath);

            var generatedUnits = new UnitDataReader();
            generatedUnits.Load(unitPath);
            var generatedResearch = new ResearchDataReader();
            generatedResearch.Load(researchPath);

            Assert.Equal(
                Serialize(TestData.UnitReader().GetAll().OrderBy(unit => unit.Type)),
                Serialize(generatedUnits.GetAll().OrderBy(unit => unit.Type)));
            Assert.Equal(
                Serialize(TestData.ResearchReader().GetAll().OrderBy(node => node.Id)),
                Serialize(generatedResearch.GetAll().OrderBy(node => node.Id)));
        }
        finally
        {
            File.Delete(unitPath);
            File.Delete(researchPath);
        }
    }

    [Fact]
    public void UnitCatalog_HasExactlyOneValidBuildingRequirementPerUnit()
    {
        var expected = new Dictionary<UnitTypeEnum, (BuildingTypeEnum Building, int Level)>
        {
            [UnitTypeEnum.Militia] = (BuildingTypeEnum.Barracks, 1),
            [UnitTypeEnum.Bowmen] = (BuildingTypeEnum.Barracks, 2),
            [UnitTypeEnum.Spearmen] = (BuildingTypeEnum.Barracks, 3),
            [UnitTypeEnum.MenAtArms] = (BuildingTypeEnum.Barracks, 5),
            [UnitTypeEnum.Axemen] = (BuildingTypeEnum.Barracks, 7),
            [UnitTypeEnum.Swordsmen] = (BuildingTypeEnum.Barracks, 10),
            [UnitTypeEnum.Crossbowmen] = (BuildingTypeEnum.Barracks, 12),
            [UnitTypeEnum.LightCavalry] = (BuildingTypeEnum.Stable, 1),
            [UnitTypeEnum.Knights] = (BuildingTypeEnum.Stable, 10),
            [UnitTypeEnum.Cataphracts] = (BuildingTypeEnum.Stable, 18),
            [UnitTypeEnum.Ballista] = (BuildingTypeEnum.Workshop, 1),
            [UnitTypeEnum.Catapult] = (BuildingTypeEnum.Workshop, 5),
            [UnitTypeEnum.Trebuchet] = (BuildingTypeEnum.Workshop, 10),
            [UnitTypeEnum.Engineers] = (BuildingTypeEnum.Workshop, 15),
            [UnitTypeEnum.Cannon] = (BuildingTypeEnum.Workshop, 20),
            [UnitTypeEnum.Longship] = (BuildingTypeEnum.Harbor, 1),
            [UnitTypeEnum.Transport] = (BuildingTypeEnum.Harbor, 3),
            [UnitTypeEnum.WarGalley] = (BuildingTypeEnum.Harbor, 5),
            [UnitTypeEnum.GrandTransport] = (BuildingTypeEnum.Harbor, 12)
        };

        var units = TestData.UnitReader().GetAll();
        Assert.Equal(expected.Count, units.Count);

        Assert.All(units, unit =>
        {
            var requirement = Assert.Single(unit.Prerequisites);
            Assert.Equal(expected[unit.Type].Building, requirement.Type);
            Assert.Equal(expected[unit.Type].Level, requirement.RequiredLevel);
            Assert.InRange(requirement.RequiredLevel, 1, 20);
        });
    }

    [Fact]
    public void Availability_IsDeterminedByEachCityAtTheExactRequiredLevel()
    {
        var units = TestData.UnitReader();
        var evaluator = new UnitAvailabilityEvaluator();
        var unit = units.GetUnit(UnitTypeEnum.MenAtArms);
        var belowRequirement = new City
        {
            Buildings = [new Building { Type = BuildingTypeEnum.Barracks, Level = 4 }]
        };
        var meetsRequirement = new City
        {
            Buildings = [new Building { Type = BuildingTypeEnum.Barracks, Level = 5 }]
        };

        var locked = evaluator.Evaluate(belowRequirement, unit);
        var unlocked = evaluator.Evaluate(meetsRequirement, unit);

        Assert.False(locked.IsUnlocked);
        Assert.Equal("Requires Barracks level 5.", Assert.Single(locked.UnmetRequirements));
        Assert.True(unlocked.IsUnlocked);
        Assert.Empty(unlocked.UnmetRequirements);
    }

    [Fact]
    public async Task MilitaryOverviews_ReturnBuildingBasedAvailability()
    {
        var units = TestData.UnitReader();
        var evaluator = new UnitAvailabilityEvaluator();
        var player = new WorldPlayer { Id = Guid.NewGuid() };
        var city = new City
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Buildings =
            [
                new Building { Type = BuildingTypeEnum.Barracks, Level = 1 },
                new Building { Type = BuildingTypeEnum.Stable, Level = 1 },
                new Building { Type = BuildingTypeEnum.Workshop, Level = 1 },
                new Building { Type = BuildingTypeEnum.Harbor, Level = 1 }
            ],
            UnitStacks = []
        };
        player.Cities.Add(city);

        var access = new TestPlayerAccessService([player], [city]);
        var repository = new MemoryCityRepository(city);
        var modifiers = new IdentityModifierService();

        var barracksService = new BarracksService(repository, units, modifiers, access, evaluator);
        var stableService = new StableService(repository, units, modifiers, access, evaluator);
        var workshopService = new WorkshopService(repository, units, modifiers, access, evaluator);
        var harborService = new HarborService(repository, units, modifiers, access, evaluator);

        var barracks = await barracksService.GetBarracksOverviewAsync(player.Id, city.Id);
        var stable = await stableService.GetStableOverviewAsync(player.Id, city.Id);
        var workshop = await workshopService.GetWorkshopOverviewAsync(player.Id, city.Id);
        var harbor = await harborService.GetHarborOverviewAsync(player.Id, city.Id);

        Assert.True(barracks.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Militia).IsUnlocked);
        Assert.Equal(
            "Requires Barracks level 2.",
            Assert.Single(barracks.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Bowmen).UnmetRequirements));
        Assert.False(stable.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Knights).IsUnlocked);
        Assert.False(workshop.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Catapult).IsUnlocked);
        Assert.False(harbor.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Transport).IsUnlocked);

        foreach (var building in city.Buildings)
        {
            building.Level = 20;
        }

        Assert.All((await barracksService.GetBarracksOverviewAsync(player.Id, city.Id)).AvailableUnits, unit => Assert.True(unit.IsUnlocked));
        Assert.All((await stableService.GetStableOverviewAsync(player.Id, city.Id)).AvailableUnits, unit => Assert.True(unit.IsUnlocked));
        Assert.All((await workshopService.GetWorkshopOverviewAsync(player.Id, city.Id)).AvailableUnits, unit => Assert.True(unit.IsUnlocked));
        Assert.All((await harborService.GetHarborOverviewAsync(player.Id, city.Id)).AvailableUnits, unit => Assert.True(unit.IsUnlocked));
    }

    private sealed class IdentityModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(
            double baseValue,
            IEnumerable<ModifierTagEnum> targetTags,
            IEnumerable<IModifierProvider> providers) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityValue(
            City city,
            double baseValue,
            params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculatePlayerValue(
            WorldPlayer player,
            double baseValue,
            params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityUnitValue(
            City city,
            UnitData unit,
            double baseValue,
            params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
    }

    private static string Serialize<T>(IEnumerable<T> values) => JsonSerializer.Serialize(
        values,
        new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });

    private static void AssertAcyclic(IReadOnlyCollection<ResearchData> nodes)
    {
        var byId = nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ResearchData node in nodes)
        {
            Visit(node.Id);
        }

        void Visit(string id)
        {
            if (visited.Contains(id)) return;
            Assert.True(visiting.Add(id), $"Research prerequisite cycle detected at {id}.");
            foreach (string prerequisiteId in byId[id].PrerequisiteIds)
            {
                Visit(prerequisiteId);
            }
            visiting.Remove(id);
            visited.Add(id);
        }
    }
}
