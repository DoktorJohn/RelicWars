using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Services.Buildings;
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

public class UnitUnlockTests
{
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

            Assert.Equal(Serialize(TestData.UnitReader().GetAll().OrderBy(unit => unit.Type)), Serialize(generatedUnits.GetAll().OrderBy(unit => unit.Type)));
            Assert.Equal(Serialize(TestData.ResearchReader().GetAll().OrderBy(node => node.Id)), Serialize(generatedResearch.GetAll().OrderBy(node => node.Id)));
        }
        finally
        {
            File.Delete(unitPath);
            File.Delete(researchPath);
        }
    }

    [Fact]
    public void Catalog_ContainsEveryAdvancedUnitAndSingleSubjugationUnlock()
    {
        var units = TestData.UnitReader();
        var research = TestData.ResearchReader();
        var catalog = new UnitUnlockCatalog(units, research);

        Assert.Equal(
            new[] { UnitTypeEnum.Militia, UnitTypeEnum.LightCavalry, UnitTypeEnum.Ballista, UnitTypeEnum.Longship }.Order(),
            units.GetAll().Where(unit => unit.IsDefaultUnlocked).Select(unit => unit.Type).Order());

        var advancedUnits = units.GetAll().Where(unit => !unit.IsDefaultUnlocked).ToList();
        Assert.Equal(15, advancedUnits.Count);
        Assert.All(advancedUnits, unit => Assert.NotNull(catalog.GetUnitUnlock(unit.Type)));

        var unitEffects = research.GetAll()
            .SelectMany(node => node.Effects.Where(effect => effect.Type == ResearchEffectType.UnitRecruitment))
            .ToList();
        Assert.Equal(15, unitEffects.Count);
        Assert.Equal(15, unitEffects.Select(effect => effect.UnitType).Distinct().Count());

        var expectedParents = new Dictionary<string, string?>
        {
            ["UNLOCK_UNIT_BOWMEN"] = null,
            ["UNLOCK_UNIT_SPEARMEN"] = "UNLOCK_UNIT_BOWMEN",
            ["UNLOCK_UNIT_AXEMEN"] = "UNLOCK_UNIT_SPEARMEN",
            ["UNLOCK_UNIT_SWORDSMEN"] = "UNLOCK_UNIT_AXEMEN",
            ["UNLOCK_UNIT_CROSSBOWMEN"] = "UNLOCK_UNIT_SWORDSMEN",
            ["UNLOCK_UNIT_MEN_AT_ARMS"] = "UNLOCK_UNIT_CROSSBOWMEN",
            ["UNLOCK_UNIT_KNIGHTS"] = null,
            ["UNLOCK_UNIT_CATAPHRACTS"] = "UNLOCK_UNIT_KNIGHTS",
            ["UNLOCK_UNIT_CATAPULT"] = null,
            ["UNLOCK_UNIT_TREBUCHET"] = "UNLOCK_UNIT_CATAPULT",
            ["UNLOCK_UNIT_ENGINEERS"] = "UNLOCK_UNIT_TREBUCHET",
            ["UNLOCK_UNIT_CANNON"] = "UNLOCK_UNIT_ENGINEERS",
            ["UNLOCK_UNIT_TRANSPORT"] = null,
            ["UNLOCK_UNIT_WAR_GALLEY"] = "UNLOCK_UNIT_TRANSPORT",
            ["UNLOCK_UNIT_GRAND_TRANSPORT"] = "UNLOCK_UNIT_WAR_GALLEY"
        };
        Assert.All(expectedParents, pair => Assert.Equal(pair.Value, research.GetNode(pair.Key).ParentId));

        var subjugation = research.GetNode("UNLOCK_SUBJUGATION");
        Assert.Single(subjugation.Effects, effect => effect.Type == ResearchEffectType.Subjugation);
        var player = new WorldPlayer { CompletedResearches = new() };
        Assert.False(catalog.HasSubjugationUnlock(player));
        player.CompletedResearches.Add(new Research { ResearchId = subjugation.Id });
        Assert.True(catalog.HasSubjugationUnlock(player));
    }

    [Fact]
    public void ResearchDisplayNamesDescriptionsAndModifierSources_AreConsistent()
    {
        var research = TestData.ResearchReader();
        var unitResearch = research.GetAll()
            .Where(node => node.Effects.Any(effect => effect.Type == ResearchEffectType.UnitRecruitment))
            .ToList();

        Assert.Equal(15, unitResearch.Count);
        Assert.All(unitResearch, node =>
        {
            Assert.Equal($"Unlocks {node.Name} recruitment.", node.Description);
            Assert.DoesNotContain("requires", node.Description, StringComparison.OrdinalIgnoreCase);
            Assert.All(Enum.GetNames<BuildingTypeEnum>(), buildingName =>
                Assert.DoesNotContain(buildingName, node.Description, StringComparison.OrdinalIgnoreCase));
        });

        Assert.Equal("Crossbowmen", research.GetNode("UNLOCK_UNIT_CROSSBOWMEN").Name);
        Assert.Equal("Men At Arms", research.GetNode("UNLOCK_UNIT_MEN_AT_ARMS").Name);
        Assert.Equal("Grand Transport", research.GetNode("UNLOCK_UNIT_GRAND_TRANSPORT").Name);
        Assert.Equal("Efficient Bureaucracy", research.GetNode("UTIL_COINS_1").Name);
        Assert.Equal("Research: Efficient Bureaucracy", Assert.Single(research.GetNode("UTIL_COINS_1").ModifiersInternal).Source);
        Assert.Equal("Imperial Messengers", research.GetNode("UTIL_ALLIED_SPEED").Name);
        Assert.Equal("Research: Imperial Messengers", Assert.Single(research.GetNode("UTIL_ALLIED_SPEED").ModifiersInternal).Source);
        Assert.All(research.GetAll(), node => Assert.True(node.Name.Length <= 22, $"{node.Id} display name exceeds 22 characters."));
    }

    [Fact]
    public void Availability_ReportsBuildingAndResearchRequirementsIndependently()
    {
        var units = TestData.UnitReader();
        var evaluator = new UnitAvailabilityEvaluator(new UnitUnlockCatalog(units, TestData.ResearchReader()));
        var player = new WorldPlayer { CompletedResearches = new() };
        var city = new City
        {
            WorldPlayer = player,
            Buildings = new() { new Building { Type = BuildingTypeEnum.Barracks, Level = 1 } }
        };

        var bowmen = evaluator.Evaluate(city, units.GetUnit(UnitTypeEnum.Bowmen));
        Assert.False(bowmen.IsUnlocked);
        Assert.Equal(2, bowmen.UnmetRequirements.Count);
        Assert.Equal("Requires Barracks level 2.", bowmen.UnmetRequirements[0]);
        Assert.DoesNotContain("current:", bowmen.UnmetRequirements[0], StringComparison.OrdinalIgnoreCase);

        city.Buildings[0].Level = 2;
        var missingResearch = evaluator.Evaluate(city, units.GetUnit(UnitTypeEnum.Bowmen));
        Assert.Single(missingResearch.UnmetRequirements);
        Assert.Equal("Requires Bowmen research.", missingResearch.UnmetRequirements[0]);

        player.CompletedResearches.Add(new Research { ResearchId = "UNLOCK_UNIT_BOWMEN" });
        Assert.True(evaluator.Evaluate(city, units.GetUnit(UnitTypeEnum.Bowmen)).IsUnlocked);
        Assert.True(evaluator.Evaluate(city, units.GetUnit(UnitTypeEnum.Militia)).IsUnlocked);
    }

    [Fact]
    public async Task MilitaryOverviews_ReturnAuthoritativeUnlockStateAndRequirements()
    {
        var units = TestData.UnitReader();
        var catalog = new UnitUnlockCatalog(units, TestData.ResearchReader());
        var evaluator = new UnitAvailabilityEvaluator(catalog);
        var player = new WorldPlayer { Id = Guid.NewGuid(), CompletedResearches = new() };
        var city = new City
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Buildings = new()
            {
                new Building { Type = BuildingTypeEnum.Barracks, Level = 25 },
                new Building { Type = BuildingTypeEnum.Stable, Level = 25 },
                new Building { Type = BuildingTypeEnum.Workshop, Level = 25 },
                new Building { Type = BuildingTypeEnum.Harbor, Level = 25 }
            },
            UnitStacks = new()
        };
        var access = new TestPlayerAccessService([player], [city]);
        var repository = new MemoryCityRepository(city);
        var modifiers = new IdentityModifierService();

        var barracks = await new BarracksService(repository, units, modifiers, access, evaluator).GetBarracksOverviewAsync(player.Id, city.Id);
        var stable = await new StableService(repository, units, modifiers, access, evaluator).GetStableOverviewAsync(player.Id, city.Id);
        var workshop = await new WorkshopService(repository, units, modifiers, access, evaluator).GetWorkshopOverviewAsync(player.Id, city.Id);
        var harbor = await new HarborService(repository, units, modifiers, access, evaluator).GetHarborOverviewAsync(player.Id, city.Id);

        Assert.True(barracks.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Militia).IsUnlocked);
        Assert.False(barracks.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Bowmen).IsUnlocked);
        Assert.False(stable.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Knights).IsUnlocked);
        Assert.False(workshop.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Catapult).IsUnlocked);
        Assert.False(harbor.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Transport).IsUnlocked);
        Assert.All(new[]
        {
            barracks.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Bowmen).UnmetRequirements,
            stable.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Knights).UnmetRequirements,
            workshop.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Catapult).UnmetRequirements,
            harbor.AvailableUnits.Single(unit => unit.UnitType == UnitTypeEnum.Transport).UnmetRequirements
        }, requirements => Assert.Single(requirements));

        foreach (var unit in units.GetAll().Where(unit => !unit.IsDefaultUnlocked))
        {
            player.CompletedResearches.Add(new Research { ResearchId = catalog.GetUnitUnlock(unit.Type)!.Id });
        }

        Assert.All((await new BarracksService(repository, units, modifiers, access, evaluator).GetBarracksOverviewAsync(player.Id, city.Id)).AvailableUnits, unit => Assert.True(unit.IsUnlocked));
        Assert.All((await new StableService(repository, units, modifiers, access, evaluator).GetStableOverviewAsync(player.Id, city.Id)).AvailableUnits, unit => Assert.True(unit.IsUnlocked));
        Assert.All((await new WorkshopService(repository, units, modifiers, access, evaluator).GetWorkshopOverviewAsync(player.Id, city.Id)).AvailableUnits, unit => Assert.True(unit.IsUnlocked));
        Assert.All((await new HarborService(repository, units, modifiers, access, evaluator).GetHarborOverviewAsync(player.Id, city.Id)).AvailableUnits, unit => Assert.True(unit.IsUnlocked));
    }

    private sealed class IdentityModifierService : IModifierService
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

    private static string Serialize<T>(IEnumerable<T> values) => JsonSerializer.Serialize(
        values,
        new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
}
