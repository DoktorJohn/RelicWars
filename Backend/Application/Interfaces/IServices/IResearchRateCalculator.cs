using Domain.Entities;
using Domain.User;

namespace Application.Interfaces.IServices;

public sealed record CityResearchPowerSnapshot(
    double BaseResearchPower,
    double FlatBonus,
    double PercentageBonus,
    double EffectiveResearchPower);

public sealed record ResearchRateSnapshot(
    double BaseResearchPower,
    double EffectiveResearchPower,
    double SpeedMultiplier,
    DateTime? NextRateChangeAtUtc);

public interface IResearchRateCalculator
{
    ResearchRateSnapshot Calculate(WorldPlayer player, DateTime asOfUtc);
    CityResearchPowerSnapshot CalculateCityPower(WorldPlayer player, City city, int universityLevel, DateTime asOfUtc);
}
