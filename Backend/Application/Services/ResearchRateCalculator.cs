using Application.Interfaces.IServices;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Entities;

namespace Application.Services;

public sealed class ResearchRateCalculator : IResearchRateCalculator
{
    private readonly BuildingDataReader _buildingData;
    private readonly IdeologyFocusDataReader _focusData;
    private readonly IModifierCollectorService _modifierCollector;
    private readonly IModifierService _modifierService;

    public ResearchRateCalculator(
        BuildingDataReader buildingData,
        IdeologyFocusDataReader focusData,
        IModifierCollectorService modifierCollector,
        IModifierService modifierService)
    {
        _buildingData = buildingData;
        _focusData = focusData;
        _modifierCollector = modifierCollector;
        _modifierService = modifierService;
    }

    public ResearchRateSnapshot Calculate(WorldPlayer player, DateTime asOfUtc)
    {
        double basePower = 0d;
        double effectivePower = 0d;
        DateTime? nextRateChange = null;

        foreach (var city in player.Cities)
        {
            var university = city.Buildings.FirstOrDefault(building =>
                building.Type == BuildingTypeEnum.University && building.Level > 0);
            if (university == null)
            {
                continue;
            }

            var contribution = CalculateCityPower(player, city, university.Level, asOfUtc);
            basePower += contribution.BaseResearchPower;
            effectivePower += contribution.EffectiveResearchPower;

            DateTime? cityChange = FindNextResearchModifierChange(city, asOfUtc);
            if (cityChange.HasValue && (!nextRateChange.HasValue || cityChange.Value < nextRateChange.Value))
            {
                nextRateChange = cityChange;
            }
        }

        double speedMultiplier = Math.Max(0d, effectivePower);

        return new ResearchRateSnapshot(basePower, effectivePower, speedMultiplier, nextRateChange);
    }

    public CityResearchPowerSnapshot CalculateCityPower(
        WorldPlayer player,
        City city,
        int universityLevel,
        DateTime asOfUtc)
    {
        if (universityLevel <= 0)
        {
            return new CityResearchPowerSnapshot(0d, 0d, 0d, 0d);
        }

        var levelData = _buildingData.GetConfig<UniversityLevelData>(BuildingTypeEnum.University, universityLevel);
        var providers = _modifierCollector.CollectAllProvidersForCity(city, player, asOfUtc);
        var result = _modifierService.CalculateEntityValueWithModifiers(
            levelData.ResearchPower,
            new[] { ModifierTagEnum.Research },
            providers);

        return new CityResearchPowerSnapshot(
            levelData.ResearchPower,
            result.FlatBonus,
            result.PercentageBonus,
            result.FinalValue);
    }

    private DateTime? FindNextResearchModifierChange(City city, DateTime asOfUtc)
    {
        DateTime? nextChange = null;

        foreach (var focus in city.ActiveFocuses)
        {
            var definition = _focusData.GetIdeology(focus.Name);
            if (!definition.ModifiersInternal.Any(modifier => modifier.Tag == ModifierTagEnum.Research))
            {
                continue;
            }

            if (focus.TimeOfIdeologyStarted is DateTime startedAt && startedAt > asOfUtc)
            {
                nextChange = Earlier(nextChange, startedAt);
            }

            if (focus.TimeOfIdeologyFinished is DateTime finishedAt && finishedAt > asOfUtc)
            {
                nextChange = Earlier(nextChange, finishedAt);
            }
        }

        return nextChange;
    }

    private static DateTime Earlier(DateTime? current, DateTime candidate) =>
        !current.HasValue || candidate < current.Value ? candidate : current.Value;
}
