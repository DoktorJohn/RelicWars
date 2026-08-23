using Domain.Entities;
using Domain.StaticData.Data;

namespace Application.Services;

public sealed record UnitAvailability(bool IsUnlocked, List<string> UnmetRequirements);

public sealed class UnitAvailabilityEvaluator
{
    public UnitAvailability Evaluate(City city, UnitData unit)
    {
        var unmetRequirements = new List<string>();

        foreach (var prerequisite in unit.Prerequisites)
        {
            var currentLevel = city.Buildings.FirstOrDefault(building => building.Type == prerequisite.Type)?.Level ?? 0;
            if (currentLevel < prerequisite.RequiredLevel)
            {
                unmetRequirements.Add($"Requires {prerequisite.Type} level {prerequisite.RequiredLevel}.");
            }
        }

        return new UnitAvailability(unmetRequirements.Count == 0, unmetRequirements);
    }
}
