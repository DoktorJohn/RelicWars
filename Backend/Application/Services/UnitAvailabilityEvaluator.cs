using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.StaticData.Data;

namespace Application.Services;

public sealed record UnitAvailability(bool IsUnlocked, List<string> UnmetRequirements);

public sealed class UnitAvailabilityEvaluator
{
    private readonly IUnitUnlockCatalog _unlockCatalog;

    public UnitAvailabilityEvaluator(IUnitUnlockCatalog unlockCatalog)
    {
        _unlockCatalog = unlockCatalog;
    }

    public UnitAvailability Evaluate(City city, UnitData unit)
    {
        var unmetRequirements = new List<string>();
        var worldPlayer = city.WorldPlayer
            ?? throw new InvalidOperationException("Unit availability requires the city's world player.");

        foreach (var prerequisite in unit.Prerequisites)
        {
            var currentLevel = city.Buildings.FirstOrDefault(building => building.Type == prerequisite.Type)?.Level ?? 0;
            if (currentLevel < prerequisite.RequiredLevel)
            {
                unmetRequirements.Add($"Requires {prerequisite.Type} level {prerequisite.RequiredLevel}.");
            }
        }

        var unlockResearch = _unlockCatalog.GetUnitUnlock(unit.Type);
        if (!unit.IsDefaultUnlocked &&
            (unlockResearch == null || !worldPlayer.CompletedResearches.Any(research => research.ResearchId == unlockResearch.Id)))
        {
            unmetRequirements.Add($"Requires {unlockResearch?.Name ?? $"unlock research for {unit.Type}"} research.");
        }

        return new UnitAvailability(unmetRequirements.Count == 0, unmetRequirements);
    }
}
