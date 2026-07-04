using Application.Services;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using System.Text.Json;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;

namespace Application.Tests;

public class IdeologyFocusModifierTests
{
    [Fact]
    public void RuntimeDataContainsEveryFocusAndValidContracts()
    {
        var focuses = TestData.FocusReader().GetAll();
        Assert.Equal(Enum.GetValues<IdeologyFocusNameEnum>().Distinct().Count(), focuses.Count);
        Assert.All(focuses, focus => Assert.False(string.IsNullOrWhiteSpace(focus.Description)));
        Assert.True(focuses.Single(x => x.Name == IdeologyFocusNameEnum.LordsLevy).CanRepeat);
        Assert.True(focuses.Single(x => x.Name == IdeologyFocusNameEnum.RoyalMedics).ConsumesOnTrigger);
        using var document = JsonDocument.Parse(File.ReadAllText(TestData.GameFile("ideologyFocus.json")));
        Assert.All(document.RootElement.EnumerateArray(), item =>
        {
            Assert.True(item.TryGetProperty("EffectKind", out _));
            Assert.True(item.TryGetProperty("TargetScope", out _));
            Assert.True(item.TryGetProperty("CanRepeat", out _));
            Assert.True(item.TryGetProperty("ConsumesOnTrigger", out _));
        });
    }

    [Fact]
    public void GeneratorProducesSameCompleteFocusSet()
    {
        string path = Path.Combine(Path.GetTempPath(), $"focus-{Guid.NewGuid():N}.json");
        try
        {
            IdeologyFocusDataGenerator.GenerateDefaultJson(path);
            var generated = new IdeologyFocusDataReader();
            generated.Load(path);
            Assert.Equal(TestData.FocusReader().GetAll().Select(x => x.Name).OrderBy(x => x),
                generated.GetAll().Select(x => x.Name).OrderBy(x => x));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public static IEnumerable<object[]> ModifierCases()
    {
        yield return Case(IdeologyFocusNameEnum.FeudalMuster, ModifierTagEnum.Armor, 10);
        yield return Case(IdeologyFocusNameEnum.OathOfBlood, ModifierTagEnum.Casualties, -5);
        yield return Case(IdeologyFocusNameEnum.NobleClemency, ModifierTagEnum.ResistanceRecovery, 10);
        yield return Case(IdeologyFocusNameEnum.RoyalLogistics, ModifierTagEnum.TravelSpeed, 8);
        yield return Case(IdeologyFocusNameEnum.RoyalDecree, ModifierTagEnum.Construction, 15);
        yield return Case(IdeologyFocusNameEnum.RoyalMedics, ModifierTagEnum.Revival, 10);
        yield return Case(IdeologyFocusNameEnum.CrownTax, ModifierTagEnum.Coins, 100);
        yield return Case(IdeologyFocusNameEnum.EnhancedWorkshop, ModifierTagEnum.SiegeRecruitmentSpeed, 10);
        yield return Case(IdeologyFocusNameEnum.PrivateSecurity, ModifierTagEnum.MerchantDefense, 15);
        yield return Case(IdeologyFocusNameEnum.MarketSurge, ModifierTagEnum.Coins, 20);
        yield return Case(IdeologyFocusNameEnum.CivicInitiative, ModifierTagEnum.Research, 10);
        yield return Case(IdeologyFocusNameEnum.PublicWorks, ModifierTagEnum.ConstructionCost, -10);
        yield return Case(IdeologyFocusNameEnum.PublicWorks, ModifierTagEnum.RepairCost, -10);
        yield return Case(IdeologyFocusNameEnum.EconomicTransparency, ModifierTagEnum.BuildingUpkeep, -20);
        yield return Case(IdeologyFocusNameEnum.AcceleratedConscription, ModifierTagEnum.RecruitmentSpeed, 15);
        yield return Case(IdeologyFocusNameEnum.IronDiscipline, ModifierTagEnum.UnitUpkeep, -10);
        yield return Case(IdeologyFocusNameEnum.FortifiedCity, ModifierTagEnum.Wall, 10);
    }

    private static object[] Case(IdeologyFocusNameEnum focus, ModifierTagEnum tag, double expected) => new object[] { focus, tag, expected };

    [Theory]
    [MemberData(nameof(ModifierCases))]
    public void FocusModifierChangesAuthoritativeValue(IdeologyFocusNameEnum focus, ModifierTagEnum tag, double expected)
    {
        var service = TestData.ModifierService(out _);
        var city = TestData.CityWithFocus(focus);
        double withFocus = service.CalculateCityValue(city, 100, tag).FinalValue;
        city.ActiveFocuses.Clear();
        double baseline = service.CalculateCityValue(city, 100, tag).FinalValue;
        Assert.Equal(expected, withFocus - baseline, 6);
    }

    [Fact]
    public void ExpiredFocusIsNotCollected()
    {
        var service = TestData.ModifierService(out _);
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.CrownTax);
        city.ActiveFocuses[0].TimeOfIdeologyFinished = TestData.Now.AddSeconds(-1);
        double expired = service.CalculateCityValue(city, 100, ModifierTagEnum.Coins).FinalValue;
        city.ActiveFocuses.Clear();
        Assert.Equal(service.CalculateCityValue(city, 100, ModifierTagEnum.Coins).FinalValue, expired);
    }

    [Fact]
    public async Task RecruitmentAndConstructionUseSpeedSemantics()
    {
        var modifiers = TestData.ModifierService(out _);
        var units = TestData.UnitReader();
        var recruitment = new RecruitmentTimeCalculationService(modifiers);
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.AcceleratedConscription);
        Assert.Equal(20 / 1.15, await recruitment.CalculateFinalRecruitmentTimeAsync(Guid.NewGuid(), city, units.GetUnit(UnitTypeEnum.Militia)), 6);

        city = TestData.CityWithFocus(IdeologyFocusNameEnum.RoyalDecree);
        Assert.Equal(86, new ConstructionTimeCalculator(modifiers).CalculateSeconds(city, 100));
    }

    [Theory]
    [InlineData(UnitTypeEnum.Militia)]
    [InlineData(UnitTypeEnum.LightCavalry)]
    [InlineData(UnitTypeEnum.Ballista)]
    public async Task AcceleratedConscriptionAffectsEveryRecruitmentCategory(UnitTypeEnum type)
    {
        var modifiers = TestData.ModifierService(out _);
        var unit = TestData.UnitReader().GetUnit(type);
        var calculator = new RecruitmentTimeCalculationService(modifiers);
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.AcceleratedConscription);
        double modified = await calculator.CalculateFinalRecruitmentTimeAsync(Guid.NewGuid(), city, unit);
        city.ActiveFocuses.Clear();
        double baseline = await calculator.CalculateFinalRecruitmentTimeAsync(Guid.NewGuid(), city, unit);
        Assert.True(modified < baseline);
    }

    [Fact]
    public async Task EnhancedWorkshopOnlyAffectsSiegeRecruitment()
    {
        var modifiers = TestData.ModifierService(out _);
        var units = TestData.UnitReader();
        var calculator = new RecruitmentTimeCalculationService(modifiers);
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.EnhancedWorkshop);
        double siege = await calculator.CalculateFinalRecruitmentTimeAsync(Guid.NewGuid(), city, units.GetUnit(UnitTypeEnum.Ballista));
        double infantry = await calculator.CalculateFinalRecruitmentTimeAsync(Guid.NewGuid(), city, units.GetUnit(UnitTypeEnum.Militia));
        city.ActiveFocuses.Clear();
        double siegeBaseline = await calculator.CalculateFinalRecruitmentTimeAsync(Guid.NewGuid(), city, units.GetUnit(UnitTypeEnum.Ballista));
        double infantryBaseline = await calculator.CalculateFinalRecruitmentTimeAsync(Guid.NewGuid(), city, units.GetUnit(UnitTypeEnum.Militia));
        Assert.True(siege < siegeBaseline);
        Assert.Equal(infantryBaseline, infantry);
    }

    [Fact]
    public void CitizenMoraleOnlyAffectsNonEliteInfantry()
    {
        var modifiers = TestData.ModifierService(out _);
        var units = TestData.UnitReader();
        var city = TestData.CityWithFocus(IdeologyFocusNameEnum.CitizenMorale);
        Assert.Equal(105, modifiers.CalculateCityUnitValue(city, units.GetUnit(UnitTypeEnum.Militia), 100, ModifierTagEnum.Discipline).FinalValue);
        Assert.Equal(100, modifiers.CalculateCityUnitValue(city, units.GetUnit(UnitTypeEnum.Knights), 100, ModifierTagEnum.Discipline).FinalValue);
        Assert.Equal(100, modifiers.CalculateCityUnitValue(city, units.GetUnit(UnitTypeEnum.Ballista), 100, ModifierTagEnum.Discipline).FinalValue);
    }
}
