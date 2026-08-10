using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;

namespace Application.Utility
{
    public class ConstructionTimeCalculator
    {
        private readonly IModifierService _modifierService;

        public ConstructionTimeCalculator(IModifierService modifierService) => _modifierService = modifierService;

        public int CalculateSeconds(City city, double baseSeconds)
        {
            double speed = _modifierService.CalculateCityValue(city, 1, ModifierTagEnum.Construction).FinalValue;
            double speedAdjustedSeconds = baseSeconds / Math.Max(0.1, speed);
            double finalSeconds = _modifierService.CalculateCityValue(city, speedAdjustedSeconds, ModifierTagEnum.ConstructionTime).FinalValue;
            return (int)Math.Max(1, Math.Floor(finalSeconds));
        }
    }
}
